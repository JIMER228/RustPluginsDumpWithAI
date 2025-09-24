using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Globalization;
using System.Linq;
using Facepunch.Extend;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using Color = UnityEngine.Color;

namespace Oxide.Plugins
{
    [Info("ZealMicroMenu", "Kira", "1.0.2")]
    [Description("Микро меню для сервера Rust")]
    public class 
        ZealMicroMenu : RustPlugin
    {
        #region [Reference] / [Запросы]

        [PluginReference] Plugin ImageLibrary;

        private string GetImg(string name)
        {
            return (string) ImageLibrary?.Call("GetImage", name) ?? "";
        } 

        #endregion

        #region [Classes] / [Классы]

        public class ButtonElement
        {
            [JsonProperty(PropertyName = "Текст кнопки")]
            public string Text;

            [JsonProperty(PropertyName = "Команда кнопки")]
            public string Command;

            [JsonProperty(PropertyName = "Цвет иконки кнопки, и текста")]
            public string Color;

            [JsonProperty(PropertyName = "Иконка кнопки (SPRITE/URL)")]
            public string Image;
        }

        #endregion

        #region [Configuraton] / [Конфигурация]

        private static ConfigData _config;

        public class ConfigData
        {
            [JsonProperty(PropertyName = "ZealMicroMenu")]
            public MicroMenu ZealMicroMenu = new MicroMenu();

            public class MicroMenu
            {
                [JsonProperty(PropertyName = "Текст кнопки открытия микро меню")]
                public string MenuTxt;

                [JsonProperty(PropertyName = "Логотип меню (URL/SPRITE)")]
                public string MenuIc;

                [JsonProperty(PropertyName = "Цвет иконки меню (HEX)")]
                public string ColorMenuIc;

                [JsonProperty(PropertyName = "Положение меню по X (AnchorMin)")]
                public string MPosXMin;

                [JsonProperty(PropertyName = "Положение меню по X (AnchorMax)")]
                public string MPosXMax;

                [JsonProperty(PropertyName = "Положение меню по Y (AnchorMin)")]
                public string MPosYMin;

                [JsonProperty(PropertyName = "Положение меню по Y (AnchorMax)")]
                public string MPosYMax;

                [JsonProperty(PropertyName = "Список кнопок")]
                public List<ButtonElement> ButtonElements = new List<ButtonElement>();
            }
        }

        public ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                ZealMicroMenu = new ConfigData.MicroMenu
                {
                    MenuTxt = $"<b>{ConVar.Server.hostname}</b>\n<size=11>НАЖМИ, ЧТОБЫ ОТКРЫТЬ МЕНЮ</size>",
                    MenuIc = "assets/icons/broadcast.png",
                    ColorMenuIc = "#C4C4C4FF",
                    MPosXMin = "0.003653084",
                    MPosXMax = "0.1718864",
                    MPosYMin = "0.925",
                    MPosYMax = "0.999",
                    ButtonElements =
                    {
                        new ButtonElement
                        {
                            Text = "МАГАЗИН",
                            Color = "#C4C4C4FF",
                            Command = "",
                            Image = "assets/icons/store.png"
                        },
                        new ButtonElement
                        {
                            Text = "КАРТА ПОКРЫТИЯ РАДИАЦИИ",
                            Color = "#C4C4C4FF",
                            Command = "",
                            Image = "assets/icons/radiation.png"
                        },
                        new ButtonElement
                        {
                            Text = "ЗОО-ПАРК",
                            Color = "#C4C4C4FF",
                            Command = "",
                            Image = "assets/icons/bite.png"
                        },
                        new ButtonElement
                        {
                            Text = "АПТЕКА",
                            Color = "#C4C4C4FF",
                            Command = "",
                            Image = "assets/icons/pills.png"
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
                _config = Config.ReadObject<ConfigData>();
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
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        #endregion

        #region [Dictionary/Vars] / [Словари/Переменные]

        private const string Sharp = "assets/content/ui/ui.background.tile.psd";
        private const string Blur = "assets/content/ui/uibackgroundblur.mat";
        private const string Radial = "assets/content/ui/ui.background.transparent.radial.psd";
        private const string Regular = "robotocondensed-regular.ttf";

        private const string Layer = "BoxMicroMenu";

        private readonly List<ulong> _activeMenu = new List<ulong>();

        #endregion

        #region [DrawUI] / [Показ UI]

        private void DrawUI_MicroMenu(BasePlayer player)
        {
            var gui = new CuiElementContainer();
            CuiHelper.DestroyUi(player, Layer);
            var cfg = _config.ZealMicroMenu;

            gui.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image =
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = $"{cfg.MPosXMin} {cfg.MPosYMin}",
                    AnchorMax = $"{cfg.MPosXMax} {cfg.MPosYMax}"
                }
            }, "Overlay", Layer);

            if (_config.ZealMicroMenu.MenuIc.Contains("assets"))
            {
                gui.Add(new CuiElement
                {
                    Name = "MenuIC",
                    Parent = Layer,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat(_config.ZealMicroMenu.ColorMenuIc),
                            Sprite = _config.ZealMicroMenu.MenuIc
                        }, 
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.01857769 0.1749995",
                            AnchorMax = "0.1795644 0.8249998"
                        }
                    }
                });
            }
            else
            {
                gui.Add(new CuiElement
                {
                    Name = "MenuIC",
                    Parent = Layer,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Color = HexToRustFormat("#C4C4C4FF"),
                            Png = GetImg(_config.ZealMicroMenu.MenuIc)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.01857769 0.1749995",
                            AnchorMax = "0.1795644 0.8249998"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#00000027"),
                            Distance = "1 1"
                        }
                    }
                });
            }

            gui.Add(new CuiButton
            {
                Button =
                {
                    Command = "micromenu",
                    Color = "0 0 0 0"
                },
                Text =
                {
                    Align = TextAnchor.MiddleLeft,
                    Color = HexToRustFormat("#C4C4C4FF"),
                    FontSize = 15,
                    Text = _config.ZealMicroMenu.MenuTxt,
                    Font = "robotocondensed-regular.ttf"
                },
                RectTransform =
                {
                    AnchorMin = "0.2024794 0",
                    AnchorMax = "1.5 1"
                }
            }, Layer, "ButtonOpen");

            if (!_activeMenu.Contains(player.userID))
            {
                int y = 0, num = 1;
                foreach (var button in _config.ZealMicroMenu.ButtonElements)
                {
                    if (button.Image.Contains("assets"))
                    {
                        gui.Add(new CuiElement
                        {
                            Name = "ButtonIC" + num,
                            Parent = Layer,
                            Components =
                            {
                                new CuiImageComponent
                                {
                                    Color = HexToRustFormat(button.Color),
                                    Sprite = button.Image,
                                    FadeIn = 0.1f + (num * 0.1f)
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"{0.05882474} {-0.36 - (y * 0.4)}",
                                    AnchorMax = $"{0.1300304} {-0.07221222 - (y * 0.4)}"
                                },
                                new CuiOutlineComponent
                                {
                                    Color = HexToRustFormat("#0000005C"),
                                    Distance = "1 1"
                                }
                            }
                        });
                    }
                    else
                    {
                        gui.Add(new CuiElement
                        {
                            Name = "ButtonIC" + num,
                            Parent = Layer,
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Color = HexToRustFormat(button.Color),
                                    Png = GetImg(button.Image),
                                    FadeIn = 0.1f + (num * 0.1f)
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"{0.05882474} {-0.36 - (y * 0.5)}",
                                    AnchorMax = $"{0.1300304} {-0.07221222 - (y * 0.5)}"
                                }
                            }
                        });
                    }

                    gui.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = button.Command,
                            Color = "0 0 0 0",
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleLeft,
                            Color = HexToRustFormat(button.Color),
                            FontSize = 12,
                            Text = $"{button.Text}",
                            Font = "robotocondensed-regular.ttf",
                            FadeIn = 0.1f + (num * 0.1f)
                        },
                        RectTransform =
                        {
                            AnchorMin = $"{0.17} {-0.48 - (y * 0.4)}",
                            AnchorMax = $"{1} {0.05 - (y * 0.4)}"
                        }
                    }, Layer, "but" + num);

                    y++;
                    num++;
                }

                _activeMenu.Add(player.userID);
            }
            else
            {
                for (int i = 0; i <= _config.ZealMicroMenu.ButtonElements.Count; i++)
                {
                    CuiHelper.DestroyUi(player, "but" + i);
                }

                _activeMenu.Remove(player.userID);
            }

            CuiHelper.AddUi(player, gui);
        }

        #endregion

        #region [Hooks] / [Крюки]

        private void OnServerInitialized()
        {
            if (!ImageLibrary)
            {
                PrintError("На сервере не установлен плагин [ImageLibrary]");
                return;
            }

            foreach (var button in _config.ZealMicroMenu.ButtonElements.Where(button => !button.Image.Contains("assets")))
            {
                ImageLibrary.Call("AddImage", button.Image, button.Image);
            }

            if (!_config.ZealMicroMenu.MenuIc.Contains("assets"))
            {
                ImageLibrary.Call("AddImage", _config.ZealMicroMenu.MenuIc, _config.ZealMicroMenu.MenuIc);
            }

            foreach (var player in BasePlayer.activePlayerList)
                    DrawUI_MicroMenu(player);
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
            }
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            NextTick(() =>
            {
                if (player.IsAdmin) return;
                DrawUI_MicroMenu(player);
            });
        }

        #endregion

        #region [ChatCommand] / [Чат команды]

        [ConsoleCommand("micromenu")]
        private void DrawUI(ConsoleSystem.Arg args)
        {
            if (!args.Player()) return;
            DrawUI_MicroMenu(args.Player());
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