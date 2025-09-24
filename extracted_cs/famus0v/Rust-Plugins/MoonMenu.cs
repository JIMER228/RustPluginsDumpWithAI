using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins 
{
    [Info("MoonMenu", "Kira", "1.0.2")]
    [Description("Uniq menu for MoonRust")]
    public class MoonMenu : RustPlugin 
    {
        #region [Vars] / [Переменные]

        private const string UIMain = "UI.Menu";
        [PluginReference] private Plugin ImageLibrary;

        #endregion

        #region [Configuraton] / [Конфигурация]

        private ConfigData _config;
 
        public class ConfigData
        {
            [JsonProperty(PropertyName = "MoonMenu - Config")]
            public MenuCFG MenuSettings = new MenuCFG();

            public class MenuCFG
            {
                [JsonProperty(PropertyName = "Задний фон (RU)")]
                public string Background;

                [JsonProperty(PropertyName = "Задний фон (ENG)")]
                public string BackgroundENG;

                [JsonProperty(PropertyName = "Логотип")]
                public string Logo;

                [JsonProperty(PropertyName = "Кнопки")]
                public List<Button> Buttons = new List<Button>();
            }
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                MenuSettings = new ConfigData.MenuCFG
                {
                    Background = "https://i.imgur.com/wNs2VUe.png",
                    Logo = "https://cdn.discordapp.com/attachments/976817266433880134/987268242252910622/fhd.png",
                    Buttons =
                    {
                        new Button
                        {
                            Name = "ARMORY",
                            Command = "armory",
                            Color = "0.20 0.67 0.11 0.7",
                            Image = "https://i.imgur.com/cVtBrnQ.png",
                            AnchorMin = "0.0005193403 0.2074078",
                            AnchorMax = "0.4791682 0.6907402",
                            TextAnchorMin = "0.3367801 0.4042144",
                            TextAchorMax = "0.6632198 0.5957856"
                        },
                        new Button
                        {
                            Name = "SHOP",
                            Command = "shop",
                            Color = "0.35 0.72 0.81 0.7",
                            Image = "https://i.imgur.com/TEDHiWX.png",
                            AnchorMin = "0.4604167 0.1990744",
                            AnchorMax = "0.8338543 0.6907413",
                            TextAnchorMin = "0.2907951 0.4058381",
                            TextAchorMax = "0.7092049 0.5941619"
                        },
                        new Button
                        {
                            Name = "WIPE",
                            Command = "wipe",
                            Color = "0.92 0.31 0.89 0.7",
                            Image = "https://i.imgur.com/6WSh2nZ.png",
                            AnchorMin = "0.8593751 0.2009264",
                            AnchorMax = "1.000521 0.5351857",
                            TextAnchorMin = "0.2158668 0.5554016",
                            TextAchorMax = "0.8800735 0.6939058"
                        },
                        new Button
                        {
                            Name = "REPORT",
                            Command = "report",
                            Color = "0.18 0.47 0.83 0.7",
                            Image = "https://i.imgur.com/k2U4zpB.png",
                            AnchorMin = "0.01719601 0.8055563",
                            AnchorMax = "0.2432381 1.002779",
                            TextAnchorMin = "0.2926271 0.3826294",
                            TextAchorMax = "0.7073729 0.6173706"
                        },
                        new Button
                        {
                            Name = "KITS",
                            Command = "kits",
                            Color = "0.90 0.85 0.31 0.7",
                            Image = "https://i.imgur.com/TDeNB0g.png",
                            AnchorMin = "0.1984469 0.8064822",
                            AnchorMax = "0.4609469 1.001853",
                            TextAnchorMin = "0.3214286 0.3815168",
                            TextAchorMax = "0.6785715 0.6184832"
                        },
                        new Button
                        {
                            Name = "WIPEBLOCK",
                            Command = "wipeblock",
                            Color = "0.84 0.15 0.88 0.7",
                            Image = "https://i.imgur.com/COAEI4M.png",
                            AnchorMin = "0.5682344 0.8064822",
                            AnchorMax = "0.7651079 1.000927",
                            TextAnchorMin = "0.261903 0.3809526",
                            TextAchorMax = "0.738097 0.6190474"
                        },
                        new Button
                        {
                            Name = "INFO",
                            Command = "info",
                            Color = "0.36 0.45 0.97 0.7",
                            Image = "https://i.imgur.com/coGKxBD.png",
                            AnchorMin = "0.7494819 0.5842677",
                            AnchorMax = "1.000003 0.842601",
                            TextAnchorMin = "0.18815 0.3207885",
                            TextAchorMax = "0.8118501 0.6792115"
                        },
                        new Button
                        {
                            Name = "EXIT",
                            Command = "menu.close",
                            Color = "0.89 0.22 0.39 0.7",
                            Image = "https://i.imgur.com/eEu1Zh2.png",
                            AnchorMin = "0.4328198 0.8037045",
                            AnchorMax = "0.5968823 0.999074",
                            TextAnchorMin = "0.2142857 0.381516",
                            TextAchorMax = "0.7857143 0.618484"
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

        #region [Lang]

        protected override void LoadDefaultMessages()
        {
            var ru = new Dictionary<string, string>
            {
                ["ARMORY"] = "ОРУЖЕЙНАЯ",
                ["SHOP"] = "МАГАЗИH"
            };

            var en = new Dictionary<string, string>
            {
                ["ARMORY"] = "ARMORY",
                ["SHOP"] = "SHOP"
            };
            lang.RegisterMessages(ru, this, "ru");
            lang.RegisterMessages(en, this);
        }

        #endregion

        #region [Classes]

        public class Button
        {
            public string Name;
            public string Color;
            public string Command;
            public string Image;
            public string AnchorMin;
            public string AnchorMax;
            public string TextAnchorMin;
            public string TextAchorMax;
        }

        #endregion

        #region [DrawUI] / [Отрисовка UI]

        private void DrawUI_Main(BasePlayer player)
        {
            var ui = new CuiElementContainer();

            ui.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image =
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, "Overlay", UIMain);

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Background",
                Parent = UIMain,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage(lang.GetLanguage(player.UserIDString) == "en"
                            ? $"{UIMain}.BackgroundENG"
                            : $"{UIMain}.Background")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            foreach (var button in _config.MenuSettings.Buttons)
            {
                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"menu.category {button.Name}",
                        Color = "0 0 0 0"
                    },
                    Text =
                    {
                        Text = " "
                    },
                    RectTransform =
                    {
                        AnchorMin = button.AnchorMin,
                        AnchorMax = button.AnchorMax
                    }
                }, UIMain, $"{UIMain}.PredButton");
            }

            CuiHelper.AddUi(player, UIMain);
            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_Outline(BasePlayer player, Button button)
        {
            var ui = new CuiElementContainer();
            ui.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image =
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, UIMain, $"{UIMain}.PredView");

            ui.Add(new CuiButton
            {
                Button =
                {
                    Color = "0 0 0 0",
                    Close = $"{UIMain}.PredView"
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
            }, $"{UIMain}.PredView");

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Button.{button.Name}",
                Parent = $"{UIMain}.PredView",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage($"{button.Name}"),
                        FadeIn = 0.5f
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = button.AnchorMin,
                        AnchorMax = button.AnchorMax
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Button.{button.Name}.Text",
                Parent = $"{UIMain}.Button.{button.Name}",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = button.Color
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = button.TextAnchorMin,
                        AnchorMax = button.TextAchorMax
                    }
                }
            });

            ui.Add(new CuiLabel
            {
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 20,
                    Text = lang.GetMessage(button.Name, this, player.UserIDString)
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, $"{UIMain}.Button.{button.Name}.Text");

            ui.Add(new CuiButton
            {
                Button =
                {
                    Color = "0 0 0 0",
                    Command = button.Command
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
            }, $"{UIMain}.Button.{button.Name}");

            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_Hud(BasePlayer player)
        {
            var ui = new CuiElementContainer();

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Hud",
                Parent = "Overlay",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage($"{UIMain}.Hud")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.005729184 0.8574073",
                        AnchorMax = "0.08802083 0.9185187"
                    }
                }
            });

            ui.Add(new CuiButton
            {
                Button =
                {
                    Color = "0 0 0 0",
                    Command = "menu"
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
            }, $"{UIMain}.Hud");

            CuiHelper.DestroyUi(player, $"{UIMain}.Hud");
            CuiHelper.AddUi(player, ui);
        }

        #endregion

        #region [Hooks] / [Крюки]

        // ReSharper disable once UnusedMember.Local
        private void OnPlayerConnected(BasePlayer player)
        {
            NextFrame(() => DrawUI_Hud(player));
        }

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            ImageLibrary.Call("AddImage", _config.MenuSettings.Background, $"{UIMain}.Background");
            ImageLibrary.Call("AddImage", _config.MenuSettings.BackgroundENG, $"{UIMain}.BackgroundENG");
            ImageLibrary.Call("AddImage", _config.MenuSettings.Logo, $"{UIMain}.Hud");
            ServerMgr.Instance.StartCoroutine(LoadImages());
        }

        #endregion

        #region [Commands] / [Команды]

        [ConsoleCommand("menu")]
        // ReSharper disable once UnusedMember.Local
        private void OpenMenu(ConsoleSystem.Arg args)
        {
            DrawUI_Main(args.Player());
        }

        [ChatCommand("menu")]
        // ReSharper disable once UnusedMember.Local
        private void OpenMenu(BasePlayer player)
        {
            DrawUI_Main(player);
        }

        [ConsoleCommand("menu.category")]
        // ReSharper disable once UnusedMember.Local
        private void CategoryMenu(ConsoleSystem.Arg args)
        {
            var button = _config.MenuSettings.Buttons.Find(x => x.Name.ToLower() == args.Args[0].ToLower());
            DrawUI_Outline(args.Player(), button);
        }

        [ConsoleCommand("menu.close")]
        // ReSharper disable once UnusedMember.Local
        private void CloseMenu(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            CuiHelper.DestroyUi(args.Player(), UIMain);
        }

        #endregion

        #region [Helpers] / [Вспомогательный код]

        private IEnumerator LoadImages()
        {
            foreach (var player in BasePlayer.activePlayerList) DrawUI_Hud(player);

            foreach (var button in _config.MenuSettings.Buttons)
            {
                ImageLibrary.Call("AddImage", button.Image, button.Name);
            }

            yield return 0;
        }

        private string GetImage(string name)
        {
            return (string) ImageLibrary.Call("GetImage", name);
        }

        #endregion
    }
}