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
    [Info("InfoMenu", "Kira", "1.0.0")]
    [Description("")]
    public class InfoMenu : RustPlugin
    {
        #region [Vars]

        [PluginReference] private Plugin ImageLibrary;
        private const string UIMain = "UI.InfoMenu";
        private static readonly WaitForSeconds wait = new WaitForSeconds(0.1f);

        #endregion

        #region [Lang]

        protected override void LoadDefaultMessages()
        {
            var ru = new Dictionary<string, string>
            {
                ["QUESTS"] = "КВЕСТЫ",
                ["BANK"] = "БАНК",
                ["SHOP"] = "МАГАЗИН",
                ["APARTAMENTS"] = "КВАРТИРЫ",
                ["CARSHOP"] = "МАШИНЫ",
                ["GUILDSYSTEM"] = "ГИЛЬДИИ",
                ["QUEST.TEXT.0"] = "asdasd",
            };

            var en = new Dictionary<string, string>
            {
                ["QUESTS"] = "QUESTS",
                ["BANK"] = "BANK",
                ["SHOP"] = "SHOP",
                ["APARTAMENTS"] = "APARTAMENTS",
                ["CARSHOP"] = "CARSHOP",
                ["GUILDSYSTEM"] = "ГИЛЬДИИ",
                ["QUEST.TEXT.0"] = "asdasd",
                ["QUEST.TEXT.1"] = "asdasd",
                ["QUEST.TEXT.2"] = "asdasd",
            };
            lang.RegisterMessages(ru, this, "ru");
            lang.RegisterMessages(en, this);
        }

        #endregion

        #region [Configuraton] / [Конфигурация]

        private ConfigData _config;

        public class ConfigData
        {
            [JsonProperty(PropertyName = "InfoMenu - Config")]
            public InfoMenuCFG InfoMenuSettings = new InfoMenuCFG();

            public class InfoMenuCFG
            {
                [JsonProperty(PropertyName = "Активные категории")]
                public List<string> Categories = new List<string>();

                [JsonProperty(PropertyName = "Категории")]
                public Dictionary<string, Page> Pages = new Dictionary<string, Page>();
            }
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                InfoMenuSettings = new ConfigData.InfoMenuCFG
                {
                    Categories =
                    {
                        "QUESTS",
                        "BANK",
                        "SHOP",
                        "APARTAMENTS",
                        "CARSHOP",
                        "GUILDSYSTEM"
                    },
                    Pages =
                    {
                        ["QUESTS"] = new Page
                        {
                            Name = "QUESTS",
                            PagesMap = new Dictionary<int, Page.PageObj>
                            {
                                [0] = new Page.PageObj
                                {
                                    Text =
                                        "В каждой из гильдий есть NPC, у которых вы можете брать задания и получать за их выполнение ШРЕКИ",
                                    Images = new Dictionary<string, string>
                                    {
                                        ["1"] = "https://i.imgur.com/mBSlsG8.png",
                                        ["2"] = "https://i.imgur.com/cJYcApr.png",
                                        ["3"] = "https://i.imgur.com/xdye1VC.png"
                                    }
                                },
                                [1] = new Page.PageObj
                                {
                                    Text =
                                        "NPC разделяются на профессии, и у каждого из них разные задания",
                                    Images = new Dictionary<string, string>
                                    {
                                        ["4"] = "https://i.imgur.com/mBSlsG8.png",
                                        ["5"] = "https://i.imgur.com/cJYcApr.png",
                                        ["6"] = "https://i.imgur.com/xdye1VC.png"
                                    }
                                }
                            }
                        },
                        ["BANK"] = new Page
                        {
                            Name = "BANK",
                            PagesMap = new Dictionary<int, Page.PageObj>
                            {
                                [0] = new Page.PageObj
                                {
                                    Text =
                                        "На сервере присутствует система экономики, вы зарабатываете ШРЕКИ," +
                                        " и тратите их в магазине или у NPC. Чтобы положить деньги на карту и использовать," +
                                        " вы можете их перевести на карту в банкомате, который отображается на карте.",
                                    Images =
                                    {
                                        ["7"] = "https://i.imgur.com/faod396.png"
                                    }
                                }
                            }  
                        },
                        ["SHOP"] = new Page
                        {
                            Name = "SHOP",
                            PagesMap = new Dictionary<int, Page.PageObj>
                            {
                                [0] = new Page.PageObj
                                {
                                    Text =
                                        "На сервере есть команда /shop, это магазин в котором вы можете приобрести предметы за шреки",
                                    Images = new Dictionary<string, string>()
                                }
                            }
                        },
                        ["APARTAMENTS"] = new Page
                        {
                            Name = "APARTAMENTS",
                            PagesMap = new Dictionary<int, Page.PageObj>
                            {
                                [0] = new Page.PageObj
                                {
                                    Text =
                                        "Вы можете арендовать или приобрести квартиру, в которой вас не смогут убить или облутать. Вам нужно приобрести ключ карту в /shop, затем зайти в дом и провести карту по картридеру.",
                                    Images =
                                    {
                                        ["8"] = "https://i.imgur.com/qL4Bz74.png"
                                    }
                                }
                            }
                        },
                        ["GUILDSYSTEM"] = new Page
                        {
                            Name = "GUILDSYSTEM",
                            PagesMap = new Dictionary<int, Page.PageObj>
                            {
                                [0] = new Page.PageObj
                                {
                                    Text =
                                        "При первом входе на сервер, у вас есть на выбор 2 гильдии : 'ОПГ Берёзка' и 'WS ARMY' в зависимости от выбора, вы будете проживать и развиваться в одной из них, а так же при смерти спавниться в ней.",
                                    Images = new Dictionary<string, string>()
                                }
                            }
                        },
                        ["CARSHOP"] = new Page
                        {
                            Name = "CARSHOP",
                            PagesMap = new Dictionary<int, Page.PageObj>
                            {
                                [0] = new Page.PageObj
                                {
                                    Text =
                                        "На территории гильдии есть автомат в котором вы можете приобрести авто за ШРЕКИ",
                                    Images =
                                    {
                                        ["9"] = "https://i.imgur.com/S31vRFT.png"
                                    }
                                }
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
            PrintError("Файл конфигурации поврежден (или не существует), создан новый!");
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        #endregion

        #region [Classes]

        public class Page
        {
            [JsonProperty(PropertyName = "Название")]
            public string Name;

            [JsonProperty(PropertyName = "Страницы")]
            public Dictionary<int, PageObj> PagesMap = new Dictionary<int, PageObj>();

            public class PageObj
            {
                [JsonProperty(PropertyName = "Текст")] public string Text;

                [JsonProperty(PropertyName = "Изображение (от 1 до 3)")]
                public Dictionary<string, string> Images = new Dictionary<string, string>();
            }
        }

        #endregion

        #region [DrawUI]

        private void DrawUI_Main(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UIMain);
            var ui = new CuiElementContainer
            {
                {
                    new CuiPanel
                    {
                        CursorEnabled = true,
                        Image =
                        {
                            Color = "0.13 0.13 0.15 1.00"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    },
                    "Overlay", UIMain
                }
            };

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
                    AnchorMax = "1 1"
                }
            }, UIMain);

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Background",
                Parent = UIMain,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage($"{UIMain}.Background")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.02681679 0.07346347",
                        AnchorMax = "0.9731832 0.9265366"
                    }
                }
            });

            CuiHelper.AddUi(player, ui);
            DrawUI_Category(player);
        }

        private void DrawUI_Category(BasePlayer player)
        {
            var ui = new CuiElementContainer();

            var y = 0;
            foreach (var category in _config.InfoMenuSettings.Categories)
            {
                CuiHelper.DestroyUi(player, $"{UIMain}.Category.{y}");
                ui.Add(new CuiElement
                {
                    Name = $"{UIMain}.Category.{y}",
                    Parent = UIMain,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage($"{UIMain}.CategoryElement")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0.03697915 {0.8435184 - (y * 0.08f)}",
                            AnchorMax = $"0.1978707 {0.9008373 - (y * 0.08f)}"
                        }
                    }
                });

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Color = "0 0 0 0",
                        Command = $"infomenu.category {category}"
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 20,
                        Color = "0.34 0.34 0.35 1.00",
                        Text = lang.GetMessage(category, this, player.UserIDString)
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, $"{UIMain}.Category.{y}");

                y++;
            }

            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_Page(BasePlayer player, Page page, int pagenum)
        {
            var ui = new CuiElementContainer();
            var pageobj = page.PagesMap[pagenum];

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Page.Text",
                Parent = UIMain,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.UpperLeft,
                        Color = "0.39 0.40 0.44 1.00",
                        FontSize = 20,
                        Text = lang.GetMessage($"{page.Name}.TEXT.{pagenum}", this, player.UserIDString)
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.2453125 0.1657407",
                        AnchorMax = "0.8286458 0.9027778"
                    }
                }
            });

            var y = 0;
            CuiHelper.DestroyUi(player, $"{UIMain}.ImageBG.{0}");
            CuiHelper.DestroyUi(player, $"{UIMain}.ImageBG.{1}");
            CuiHelper.DestroyUi(player, $"{UIMain}.ImageBG.{2}");
            foreach (var img in pageobj.Images)
            {
                CuiHelper.DestroyUi(player, $"{UIMain}.ImageBG.{y}");
                ui.Add(new CuiElement
                {
                    Name = $"{UIMain}.ImageBG.{y}",
                    Parent = UIMain,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage($"{UIMain}.Image.Background"),
                            FadeIn = 0.1f * y
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0.8427083 {0.6953704 - (y * 0.26481235)}",
                            AnchorMax = $"0.9595415 {0.9030738 - (y * 0.26481235)}"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UIMain}.Image.{y}",
                    Parent = $"{UIMain}.ImageBG.{y}",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage(img.Key),
                            FadeIn = 0.1f * y
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.15 0.15",
                            AnchorMax = "0.85 0.85"
                        }
                    }
                });
                y++;
            }

            CuiHelper.DestroyUi(player, $"{UIMain}.Page.Text");
            CuiHelper.AddUi(player, ui);
            DrawUI_Pagination(player, pagenum, page.Name);
        }

        private void DrawUI_Pagination(BasePlayer player, int page, string category)
        {
            var ui = new CuiElementContainer
            {
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"infomenu.page {page - 1} {category}",
                            Color = "0 0 0 0"
                        },
                        Text = {Text = " "},
                        RectTransform =
                        {
                            AnchorMin = "0.5458337 0.1037037",
                            AnchorMax = "0.566667 0.1509259"
                        }
                    },
                    UIMain, $"{UIMain}.Pages.Left"
                },
                new CuiElement
                {
                    Name = $"{UIMain}.Page.num",
                    Parent = UIMain,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = "0.39 0.40 0.44 1.00",
                            FontSize = 20,
                            Text = (page + 1).ToString()
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5666667 0.1037037",
                            AnchorMax = "0.6364583 0.1509259"
                        }
                    }
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"infomenu.page {page + 1} {category}",
                            Color = "0 0 0 0"
                        },
                        Text = {Text = " "},
                        RectTransform =
                        {
                            AnchorMin = "0.6364573 0.1037037",
                            AnchorMax = "0.6572906 0.1509259"
                        }
                    },
                    UIMain, $"{UIMain}.Pages.Right"
                }
            };

            CuiHelper.DestroyUi(player, $"{UIMain}.Page.num");
            CuiHelper.DestroyUi(player, $"{UIMain}.Pages.Left");
            CuiHelper.DestroyUi(player, $"{UIMain}.Pages.Right");
            CuiHelper.AddUi(player, ui);
        }

        #endregion

        #region [Hooks]

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            ImageLibrary.Call("AddImage", "https://i.imgur.com/JUV8LF0.png", $"{UIMain}.Background");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/REX8ZkI.png", $"{UIMain}.CategoryElement");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/bo37lmA.png", $"{UIMain}.Image.Background");
            ServerMgr.Instance.StartCoroutine(LoadImages());
        }

        #endregion

        #region [Commands]

        [ChatCommand("info")]
        // ReSharper disable once UnusedMember.Local
        private void OpenInfoMenu(BasePlayer player)
        {
            DrawUI_Main(player);
            DrawUI_Page(player, _config.InfoMenuSettings.Pages["QUESTS"], 0);
        }

        [ConsoleCommand("infomenu.close")]
        // ReSharper disable once UnusedMember.Local
        private void CloseInfoMenu(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            CuiHelper.DestroyUi(args.Player(), UIMain);
        }

        [ConsoleCommand("infomenu.category")]
        // ReSharper disable once UnusedMember.Local
        private void Category(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var category = args.Args[0];
            DrawUI_Page(args.Player(), _config.InfoMenuSettings.Pages[category], 0);
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
            DrawUI_Page(args.Player(), page, num);
        }

        #endregion

        #region [Helpers]

        private IEnumerator LoadImages()
        {
            foreach (var page in _config.InfoMenuSettings.Pages)
            {
                foreach (var img in page.Value.PagesMap.SelectMany(pageObj => pageObj.Value.Images))
                    ImageLibrary.Call("AddImage", img.Value, img.Key);
                yield return wait;
            }

            yield return 0;
        }

        private string GetImage(string name)
        {
            return (string) ImageLibrary?.Call("GetImage", name);
        }

        #endregion
    }
}