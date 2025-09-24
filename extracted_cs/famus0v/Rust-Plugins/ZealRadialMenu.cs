using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using WebSocketSharp;

namespace Oxide.Plugins
{
    [Info("ZealRadialMenu", "Kira", "1.0.0")]
    [Description("Radial Menu")]
    public class ZealRadialMenu : RustPlugin
    {
        #region [References] / [Ссылки]

        [PluginReference] Plugin ImageLibrary;

        #endregion

        #region [Classes] / [Классы]

        public class ButtonSettings
        {
            [JsonProperty(PropertyName = "Button Settings")]
            public readonly Button ButtonConfig = new Button();

            [JsonProperty(PropertyName = "Text Settings")]
            public readonly Text TextConfig = new Text();

            [JsonProperty(PropertyName = "Background Settings")]
            public readonly Background BackgroundConfig = new Background();

            [JsonProperty(PropertyName = "Icon Settings")]
            public readonly Icon IconConfig = new Icon();

            public class Button
            {
                [JsonProperty(PropertyName = "Button name (A unique name is required)")]
                public string Name;

                [JsonProperty(PropertyName = "Button command (Sent to the console)")]
                public string Command;

                [JsonProperty(PropertyName = "Button size")]
                public int Size;
            }

            public class Text
            {
                [JsonProperty(PropertyName = "Text value")]
                public string Value;

                [JsonProperty(PropertyName = "Text size")]
                public int Size;

                [JsonProperty(PropertyName = "Text color (HEX)")]
                public string Color;
            }

            public class Background
            {
                [JsonProperty(PropertyName = "Background image (URL/SPRITE)")]
                public string Value;

                [JsonProperty(PropertyName = "Background color (HEX)")]
                public string Color;
            }

            public class Icon
            {
                [JsonProperty(PropertyName = "Icon image (URL/SPRITE)")]
                public string Value;

                [JsonProperty(PropertyName = "Icon color (HEX)")]
                public string Color;

                [JsonProperty(PropertyName = "Icon size")]
                public int Size;
            }
        }

        #endregion

        #region [Configuration] / [Конфигурация]

        private static ConfigData _config;

        public class ConfigData
        {
            [JsonProperty(PropertyName = "ZealRadialMenu [Configuration]")]
            public Menu MenuConfig = new Menu();

            public class Menu
            {
                [JsonProperty(PropertyName = "Menu opening command")]
                public string Command;

                [JsonProperty(PropertyName = "Radius of buttons from center (0 = Auto)")]
                public int Radius;

                [JsonProperty(PropertyName = "Picture of the exit button from the menu (URL)")]
                public string ImageClose;

                [JsonProperty(PropertyName = "Exit menu button color (HEX)")]
                public string ImageCloseColor;

                [JsonProperty(PropertyName = "Buttons List")]
                public List<ButtonSettings> Buttons = new List<ButtonSettings>();
            }
        }

        private static ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                MenuConfig = new ConfigData.Menu
                {
                    Command = "menu",
                    Radius = 0,
                    ImageClose = "https://i.imgur.com/Fpkaxgp.png",
                    ImageCloseColor = "#BA3737FF",
                    Buttons = new List<ButtonSettings>
                    {
                        new ButtonSettings
                        {
                            ButtonConfig =
                            {
                                Name = "1",
                                Command = "chat.say [OK]",
                                Size = 50
                            },
                            IconConfig =
                            {
                                Value = "assets/icons/bite.png",
                                Color = "#D3D3D3",
                                Size = 20
                            },
                            TextConfig =
                            {
                                Value = "[TEXT]",
                                Color = "#FFFFFF",
                                Size = 20
                            },
                            BackgroundConfig =
                            {
                                Value = "https://i.imgur.com/FVeJbNJ.png",
                                Color = "#0000007A"
                            }
                        },
                        new ButtonSettings
                        {
                            ButtonConfig =
                            {
                                Name = "2",
                                Command = "chat.say [OK]",
                                Size = 50
                            },
                            TextConfig =
                            {
                                Value = "[TEXT]",
                                Size = 20,
                                Color = "#FFFFFF"
                            },
                            BackgroundConfig =
                            {
                                Value = "https://i.imgur.com/FVeJbNJ.png",
                                Color = "#0000007A"
                            }
                        },
                        new ButtonSettings
                        {
                            ButtonConfig =
                            {
                                Name = "3",
                                Command = "chat.say [OK]",
                                Size = 50
                            },
                            IconConfig =
                            {
                                Value = "assets/icons/store.png",
                                Color = "#D3D3D3",
                                Size = 20
                            },
                            BackgroundConfig =
                            {
                                Value = "https://i.imgur.com/FVeJbNJ.png",
                                Color = "#0000007A"
                            }
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
            PrintError("The config file is corrupted (or does not exist), a new one was created!");
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        #endregion

        #region [Vars] / [Переменные]

        private const string UILayer = "UI.Layer.Main";

        #endregion

        #region [DrawUI] / [Отрисовка UI]

        private void DrawUI_Menu(BasePlayer player)
        {
            var ui = new CuiElementContainer();
            var configuration = _config.MenuConfig;

            ui.Add(new CuiPanel
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
            }, "Hud", UILayer);

            ui.Add(new CuiElement
            {
                Name = $"{UILayer}.ImageClose",
                Parent = UILayer,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Color = HexToRustFormat(configuration.ImageCloseColor),
                        Png = GetImg("ZealRadialMenu.ImageClose")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "-24.00039 -25.00073",
                        AnchorMax = "26.00038 25.00075"
                    }
                }
            });

            ui.Add(new CuiButton
            {
                Button =
                {
                    Command = "zealradialmenu.close",
                    Color = "0 0 0 0",
                    Close = UILayer
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
            }, $"{UILayer}.ImageClose");

            var buttonNumber = 0;
            foreach (var button in configuration.Buttons)
            {
                var size = button.ButtonConfig.Size;
                var r = configuration.Radius + ((size / 1.7f) * configuration.Buttons.Count);
                var c = (double) configuration.Buttons.Count / 2;
                var rad = buttonNumber / c * 3.14;
                var x = r * Math.Cos(rad);
                var y = r * Math.Sin(rad);

                if (!button.BackgroundConfig.Value.IsNullOrEmpty())
                {
                    if (button.BackgroundConfig.Value.Contains("assets"))
                    {
                        ui.Add(new CuiElement
                        {
                            Name = $"Button.{buttonNumber}",
                            Parent = UILayer,
                            Components =
                            {
                                new CuiImageComponent
                                {
                                    Sprite = button.BackgroundConfig.Value,
                                    Color = HexToRustFormat(button.BackgroundConfig.Color),
                                    FadeIn = 0.3f * buttonNumber
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"{x - size} {y - size}",
                                    AnchorMax = $"{x + size} {y + size}"
                                }
                            }
                        });
                    }
                    else
                    {
                        ui.Add(new CuiElement
                        {
                            Name = $"Button.{buttonNumber}",
                            Parent = UILayer,
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = GetImg(button.BackgroundConfig.Value),
                                    Color = HexToRustFormat(button.BackgroundConfig.Color),
                                    FadeIn = 0.3f * buttonNumber
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"{x - size} {y - size}",
                                    AnchorMax = $"{x + size} {y + size}"
                                }
                            }
                        });
                    }
                }

                if (!button.IconConfig.Value.IsNullOrEmpty())
                {
                    if (button.IconConfig.Value.Contains("assets"))
                    {
                        ui.Add(new CuiElement
                        {
                            Name = $"Button.Icon.{buttonNumber}",
                            Parent = UILayer,
                            Components =
                            {
                                new CuiImageComponent
                                {
                                    Sprite = button.IconConfig.Value,
                                    Color = HexToRustFormat(button.IconConfig.Color),
                                    FadeIn = 0.3f * buttonNumber
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"{x - button.IconConfig.Size} {y - button.IconConfig.Size}",
                                    AnchorMax = $"{x + button.IconConfig.Size} {y + button.IconConfig.Size}"
                                }
                            }
                        });
                    }
                    else
                    {
                        ui.Add(new CuiElement
                        {
                            Name = $"Button.Icon.{buttonNumber}",
                            Parent = UILayer,
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = GetImg(button.IconConfig.Value),
                                    Color = HexToRustFormat(button.IconConfig.Color),
                                    FadeIn = 0.3f * buttonNumber
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"{x - button.IconConfig.Size} {y - button.IconConfig.Size}",
                                    AnchorMax = $"{x + button.IconConfig.Size} {y + button.IconConfig.Size}"
                                }
                            }
                        });
                    }
                }

                if (!button.TextConfig.Value.IsNullOrEmpty())
                {
                    ui.Add(new CuiElement
                    {
                        Name = $"Button.Text.{buttonNumber}",
                        Parent = UILayer,
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Align = TextAnchor.MiddleCenter,
                                Color = HexToRustFormat(button.TextConfig.Color),
                                Text = button.TextConfig.Value,
                                FontSize = button.TextConfig.Size,
                                FadeIn = 0.3f * buttonNumber
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{x - size} {y - size}",
                                AnchorMax = $"{x + size} {y + size}"
                            }
                        }
                    });
                }

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"sendcmd {button.ButtonConfig.Command}",
                        Color = "0 0 0 0",
                        Close = UILayer
                    },
                    Text =
                    {
                        Text = " "
                    },
                    RectTransform =
                    {
                        AnchorMin = $"{x - size} {y - size}",
                        AnchorMax = $"{x + size} {y + size}"
                    }
                }, UILayer);

                buttonNumber++;
            }

            CuiHelper.DestroyUi(player, UILayer);
            CuiHelper.AddUi(player, ui);
        }

        #endregion

        #region [Hooks] / [Крюки]

        private void OnServerInitialized()
        {
            if (!VerificationInstall()) return;
            ServerMgr.Instance.StartCoroutine(LoadImages());
            cmd.AddChatCommand(_config.MenuConfig.Command, this, nameof(OpenMenu));
        }

        #endregion

        #region [Commands] / [Команды]

        private void OpenMenu(BasePlayer player)
        {
            DrawUI_Menu(player);
        }

        [ConsoleCommand("zealradialmenu.close")]
        private void CloseMenu(ConsoleSystem.Arg args)
        {
            CuiHelper.DestroyUi(args.Player(), UILayer);
        }

        [ConsoleCommand("sendcmd")]
        private void SendCmd(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            args.Player()
                .SendConsoleCommand(
                    $"{args.Args[0]} \" {string.Join(" ", args.Args.ToList().GetRange(1, args.Args.Length - 1))}\"");
        }

        #endregion

        #region [Helpers] / [Вспомогательный код]

        private IEnumerator LoadImages()
        {
            ImageLibrary.Call("AddImage", _config.MenuConfig.ImageClose, "ZealRadialMenu.ImageClose");

            foreach (var button in _config.MenuConfig.Buttons)
            {
                var backgroundImage = button.BackgroundConfig.Value;
                var iconImage = button.IconConfig.Value;
                if (!string.IsNullOrEmpty(backgroundImage))
                    if (!backgroundImage.Contains("assets"))
                        ImageLibrary.Call("AddImage", backgroundImage, backgroundImage);
                if (!string.IsNullOrEmpty(iconImage))
                    if (!iconImage.Contains("assets"))
                        ImageLibrary.Call("AddImage", iconImage, iconImage);
                yield return CoroutineEx.waitForSeconds(0.2f);
            }

            yield return 0;
        }

        private bool VerificationInstall()
        {
            if (plugins.Find("ImageLibrary") != null) return true;
            PrintError("ImageLibrary plugin not installed");
            Interface.Oxide.UnloadPlugin(Name);
            return false;
        }

        private string GetImg(string name)
        {
            return (string) ImageLibrary?.Call("GetImage", name) ?? "";
        }

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
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);
            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }

        #endregion
    }
}