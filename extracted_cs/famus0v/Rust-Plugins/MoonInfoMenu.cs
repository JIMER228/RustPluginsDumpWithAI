using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Facepunch.Extend;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("MoonInfoMenu", "Kira", "1.0.2")]
    [Description("ads")]
    public class MoonInfoMenu : RustPlugin
    {
        [PluginReference] private Plugin ImageLibrary;

        #region [Vars]

        private const string UIMain = "UI.InfoMenu";
        private const string Regular = "robotocondensed-regular.ttf";

        public class Page
        {
            public string Name;
            public Dictionary<int, PageImg> PagesMap = new Dictionary<int, PageImg>();
        }

        #endregion

        #region [Classes]

        public class PageImg
        {
            public string ImageRU;
            public string ImageENG;
        }

        #endregion

        #region [Configuraton] / [Конфигурация]

        private ConfigData _config;

        public class ConfigData
        {
            [JsonProperty(PropertyName = "MoonInfoMenu - Config")]
            public InfoMenuCFG InfoMenuSettings = new InfoMenuCFG();

            public class InfoMenuCFG
            {
                [JsonProperty(PropertyName = "Задний фон")]
                public string Background;

                [JsonProperty(PropertyName = "Количество кнопок в одном ряду")]
                public int Buttons;

                [JsonProperty(PropertyName = "Кнопки")]
                public Dictionary<string, string> Categories = new Dictionary<string, string>();

                [JsonProperty(PropertyName = "Страницы кнопок")]
                public Dictionary<string, Page> Pages = new Dictionary<string, Page>();
            }
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                InfoMenuSettings = new ConfigData.InfoMenuCFG
                {
                    Buttons = 6,
                    Background = "https://i.imgur.com/2NY1J9G.png",
                    Pages =
                    {
                        ["RULES"] = new Page
                        {
                            Name = "RULES",
                            PagesMap = new Dictionary<int, PageImg>
                            {
                                [0] = new PageImg
                                {
                                    ImageRU = "link",
                                    ImageENG = "link"
                                }
                            }
                        }
                    },
                    Categories =
                    {
                        ["RULES"] = "https://i.imgur.com/EZoMuMq.png",
                        ["UNIQITEMS"] = "https://i.imgur.com/KNjibpZ.png",
                        ["PAINT"] = "https://i.imgur.com/VgrnLNr.png",
                        ["SERVERDESC"] = "https://i.imgur.com/PbNXveR.png",
                        ["STOCKS"] = "https://i.imgur.com/9aPImkc.png",
                        ["WHITENIGHT"] = "https://i.imgur.com/IU4UtT5.png",
                        ["GAMES"] = "https://i.imgur.com/lwBB7la.png",
                        ["COMMANDS"] = "https://i.imgur.com/TEOXLNh.png"
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
                    AnchorMin = "-0.005 -0.005",
                    AnchorMax = "1.007 1.007"
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
                        Png = GetImage($"{UIMain}.MoonBackground")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            ui.Add(new CuiButton
            {
                Button =
                {
                    Close = UIMain,
                    Color = "0 0 0 0"
                },
                Text =
                {
                    Text = " "
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "0.06770833 0.03240741"
                }
            }, UIMain);

            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.AddUi(player, ui);
            DrawUI_Buttons(player);
        }

        private void DrawUI_Buttons(BasePlayer player)
        {
            var ui = new CuiElementContainer();

            int num = 0, x = 0, y = 0;
            foreach (var category in _config.InfoMenuSettings.Categories)
            {
                if (!_config.InfoMenuSettings.Pages.ContainsKey(category.Key)) continue;
                CuiHelper.DestroyUi(player, $"{UIMain}.Window.{num}");
                if (y >= _config.InfoMenuSettings.Buttons)
                {
                    x++;
                    y = 0;
                }

                ui.Add(new CuiElement
                {
                    Name = $"{UIMain}.Button.{num}",
                    Parent = UIMain,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage(category.Key),
                            FadeIn = 0.5f * num
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.01822919 + (x * 0.05f)} {0.9370369 - (y * 0.1f)}",
                            AnchorMax = $"{0.04687502 + (x * 0.05f)} {0.9879628 - (y * 0.1f)}"
                        }
                    }
                });

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Color = "0 0 0 0",
                        Command = $"infomenu.category {category.Key}"
                    },
                    Text =
                    {
                        Align = TextAnchor.LowerCenter,
                        Color = "1 1 1 1",
                        FontSize = 9,
                        Text = _config.InfoMenuSettings.Pages[category.Key]?.Name,
                        Font = Regular,
                        FadeIn = 0.5f * num
                    },
                    RectTransform =
                    {
                        AnchorMin = "-0.3 -0.3",
                        AnchorMax = "1.3 1.3"
                    }
                }, $"{UIMain}.Button.{num}");
                y++;
                num++;
            }

            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_Window(BasePlayer player, Page page, int num, string key)
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
            }, UIMain, $"{UIMain}.Window");

            ui.Add(new CuiButton
            {
                Button =
                {
                    Close = $"{UIMain}.Window",
                    Color = "0 0 0 0"
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
            }, $"{UIMain}.Window");

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Window.Image",
                Parent = $"{UIMain}.Window",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage(lang.GetLanguage(player.UserIDString) == "en"
                            ? $"PageENG.{page.Name}.{num}"
                            : $"PageRU.{page.Name}.{num}")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-400 -300",
                        OffsetMax = "400 300"
                    }
                }
            });

            CuiHelper.DestroyUi(player, $"{UIMain}.Window");
            CuiHelper.AddUi(player, ui);
            DrawUI_Pagination(player, num, key);
        }

        private void DrawUI_Pagination(BasePlayer player, int page, string category)
        {
            var ui = new CuiElementContainer();
            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Button.Page",
                Parent = $"{UIMain}.Window",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage($"{UIMain}.Button.Page")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.460938 0.1212963",
                        AnchorMax = "0.5385412 0.1574073"
                    }
                }
            });

            ui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"infomenu.page {page - 1} {category}",
                    Color = "0 0 0 0"
                },
                Text = {Text = " "},
                RectTransform =
                {
                    AnchorMin = "0.460938 0.1212963",
                    AnchorMax = "0.5 0.1574073"
                }
            }, $"{UIMain}.Window", $"{UIMain}.Pages.Left");

            ui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"infomenu.page {page + 1} {category}",
                    Color = "0 0 0 0"
                },
                Text = {Text = " "},
                RectTransform =
                {
                    AnchorMin = "0.4994792 0.1212963",
                    AnchorMax = "0.5385412 0.1574073"
                }
            }, $"{UIMain}.Window", $"{UIMain}.Pages.Right");

            CuiHelper.DestroyUi(player, $"{UIMain}.Pages.Left");
            CuiHelper.DestroyUi(player, $"{UIMain}.Pages.Right");
            CuiHelper.AddUi(player, ui);
        }

        #endregion

        #region [Hooks] / [Крюки]

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            ImageLibrary.Call("AddImage", _config.InfoMenuSettings.Background, $"{UIMain}.MoonBackground");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/lm8Q8Ea.png", $"{UIMain}.Button.Page");
            ServerMgr.Instance.StartCoroutine(LoadImages());
        }

        #endregion

        #region [Commands] / [Команды]

        [ConsoleCommand("infomenu")]
        // ReSharper disable once UnusedMember.Local
        private void OpenMenu(ConsoleSystem.Arg args)
        {
            DrawUI_Main(args.Player());
        }

        [ChatCommand("infomenu")]
        // ReSharper disable once UnusedMember.Local
        private void OpenMenu(BasePlayer player)
        {
            DrawUI_Main(player);
        }

        [ConsoleCommand("infomenu.category")]
        // ReSharper disable once UnusedMember.Local
        private void CategoryMenu(ConsoleSystem.Arg args)
        {
            if (!_config.InfoMenuSettings.Pages.ContainsKey(args.Args[0])) return;
            DrawUI_Window(args.Player(), _config.InfoMenuSettings.Pages[args.Args[0]], 0, args.Args[0]);
        }

        [ConsoleCommand("infomenu.page")]
        // ReSharper disable once UnusedMember.Local
        private void InfoMenuPage(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var num = args.Args[0].ToInt();
            var category = args.Args[1];
            var page = _config.InfoMenuSettings.Pages[category];
            if (num < 0) return;
            if (num > page.PagesMap.Count - 1) return;
            DrawUI_Window(args.Player(), page, num, category);
        }

        #endregion
 
        #region [Helpers] / [Вспомогательный код]

        private IEnumerator LoadImages()
        {
            foreach (var category in _config.InfoMenuSettings.Categories)
                ImageLibrary.Call("AddImage", category.Value, category.Key);
            foreach (var pages in _config.InfoMenuSettings.Pages)
            {
                foreach (var image in pages.Value.PagesMap)
                {
                    ImageLibrary.Call("AddImage", image.Value.ImageRU, $"PageRU.{pages.Value.Name}.{image.Key}");
                    ImageLibrary.Call("AddImage", image.Value.ImageENG, $"PageENG.{pages.Value.Name}.{image.Key}");
                }
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