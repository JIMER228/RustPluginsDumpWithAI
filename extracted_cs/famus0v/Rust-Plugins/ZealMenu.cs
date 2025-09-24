using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Color = UnityEngine.Color;

namespace Oxide.Plugins
{
    [Info("ZealMenu", "Kira", "1.0.0")]
    public class ZealMenu : RustPlugin
    {
        #region [Reference] / [Ссылки]

        [PluginReference] private Plugin ImageLibrary, ZealMStatistics, ZealKarma;

        #endregion

        #region [Classes]

        private class Button
        {
            public string Name;
            public string Command;
            public string Url;
        }

        #endregion

        #region [Vars] / [Переменные]

        public const string Sharp = "assets/content/ui/ui.background.tile.psd";
        public const string Blur = "assets/content/ui/uibackgroundblur.mat";
        private const string BlurInMenu = "assets/content/ui/uibackgroundblur-ingamemenu.mat";
        public const string Radial = "assets/content/ui/ui.background.transparent.radial.psd";
        private static string Regular = "robotocondensed-regular.ttf";

        public const string UILayerMenu = "UI.Layer.Menu";
        public const string UILayerHud = "UI.Layer.Hud";
        public const string UILayerCategory = "UI.Layer.Category";

        private static ZealMenu _;

        private Dictionary<ulong, UICore> UICores = new Dictionary<ulong, UICore>();

        private static List<Button> Buttons = new List<Button>
        {
            new Button
            {
                Name = "Profile",
                Command = "profile",
                Url = "https://i.imgur.com/m6B8FqL.png"
            },
            new Button
            {
                Name = "Information",
                Command = "info",
                Url = "https://i.imgur.com/RH9KTPj.png"
            },
            new Button
            {
                Name = "Calendar",
                Command = "calendar",
                Url = "https://i.imgur.com/uejbXAv.png"
            },
            new Button
            {
                Name = "Statistics",
                Command = "statistics",
                Url = "https://i.imgur.com/bNmBxOJ.png"
            },
            new Button
            {
                Name = "Friends",
                Command = "friends",
                Url = "https://i.imgur.com/DUaIPP1.png"
            },
            new Button
            {
                Name = "Crafts",
                Command = "crafts",
                Url = "https://i.imgur.com/kOkE14p.png"
            },
            new Button
            {
                Name = "Clans",
                Command = "clans",
                Url = "https://i.imgur.com/mddxKUj.png"
            },
            new Button
            {
                Name = "Shop",
                Command = "shop",
                Url = "https://i.imgur.com/06lZV0E.png"
            },
            new Button
            {
                Name = "Kits",
                Command = "kits",
                Url = "https://i.imgur.com/cSHXYKE.png"
            },
            new Button
            {
                Name = "Wipeblock",
                Command = "wipeblock",
                Url = "https://i.imgur.com/4OvZSJC.png"
            },
            new Button
            {
                Name = "Store",
                Command = "store",
                Url = "https://i.imgur.com/VOmpk3m.png" 
            },
            new Button
            {
                Name = "Settings",
                Command = "settings",
                Url = "https://i.imgur.com/Orc1UKF.png"
            }
        };

        #endregion

        #region [MonoBehaviours]

        private class UICore : MonoBehaviour
        {
            public BasePlayer player;
            public float karma;
            public int karmaStage;
 
            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                karma = (float) _.ZealKarma.Call("GetKarma", player.userID);
                karmaStage = karma < 0 ? 1 : 2;
                DrawUI_HudIcon();
            }

            #region [HUD Menu]

            public void DrawUI_HudIcon()
            {
                var ui = new CuiElementContainer();

                ui.Add(new CuiElement
                {
                    Name = UILayerHud,
                    Parent = "Overlay",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = _.GetImage($"zealmenu.icon.{karmaStage}")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.005729167 0.9096273",
                            AnchorMax = "0.05652314 0.9898148"
                        }
                    }
                });

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Command = "chat.say /menu",
                        Color = "0 0 0 0"
                    },
                    Text = {Text = " "},
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, UILayerHud);

                CuiHelper.DestroyUi(player, UILayerHud);
                CuiHelper.AddUi(player, ui);
            }

            public void DrawUI_HudAlert()
            {
                var ui = new CuiElementContainer();

                ui.Add(new CuiElement
                {
                    Name = UILayerHud + ".Alert",
                    Parent = UILayerHud,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = _.GetImage($"zealmenu.bg.alert.{karmaStage}"),
                            FadeIn = 1f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.7075168 0",
                            AnchorMax = "4.674075 0.9845"
                        }
                    }
                });

                CuiHelper.DestroyUi(player, UILayerHud + ".Alert");
                CuiHelper.AddUi(player, ui);
            }

            #endregion

            #region [FullScreen Menu]

            public void DrawUI_Parent()
            {
                karma = (float) _.ZealKarma.Call("GetKarma", player.userID);
                karmaStage = karma < 0 ? 1 : 2;
                var ui = new CuiElementContainer();

                ui.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    Image =
                    {
                        Color = HexToRustFormat("#00000090"),
                        Material = BlurInMenu
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, "Overlay", UILayerMenu);

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Close = UILayerMenu,
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
                }, UILayerMenu);

                CuiHelper.DestroyUi(player, UILayerMenu);
                CuiHelper.AddUi(player, ui);
                DrawUI_HudIcon();
                DrawUI_HudAlert();
                Start_Process(DrawUI_Buttons_2());
            }

            private IEnumerator DrawUI_Buttons_2()
            {
                var y = 0;
                foreach (var button in Buttons)
                {
                    var ui = new CuiElementContainer();
                    ui.Add(new CuiElement
                    {
                        Name = $"{UILayerMenu}.button.{button.Name}",
                        Parent = UILayerMenu,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = _.GetImage($"zealmenu.{button.Name}"),
                                FadeIn = 0.3f
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0.01401844} {0.8429607 - (y * 0.0658)}",
                                AnchorMax = $"{0.1096449} {0.8985162 - (y * 0.0658)}"
                            }
                        }
                    });

                    ui.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = $"zealmenu.{button.Command}",
                            Color = "0 0 0 0"
                        },
                        Text = {Text = " "},
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    }, $"{UILayerMenu}.button.{button.Name}");

                    y++;
                    CuiHelper.AddUi(player, ui);
                    yield return new WaitForSeconds(0.05f);
                }

                yield return 0;
            }

            public void DrawUI_Buttons_1()
            {
                var ui = new CuiElementContainer();

                var y = 0;
                foreach (var button in Buttons)
                {
                    ui.Add(new CuiElement
                    {
                        Name = $"{UILayerMenu}.button.{button.Name}",
                        Parent = UILayerMenu,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = _.GetImage($"zealmenu.{button.Name}"),
                                FadeIn = 0.5f + (y * 0.1f)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0.01401844} {0.8429607 - (y * 0.0658)}",
                                AnchorMax = $"{0.1096449} {0.8985162 - (y * 0.0658)}"
                            }
                        }
                    });

                    ui.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = $"zealmenu.{button.Command}",
                            Color = "0 0 0 0"
                        },
                        Text = {Text = " "},
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    }, $"{UILayerMenu}.button.{button.Name}");

                    y++;
                }

                CuiHelper.AddUi(player, ui);
            }

            #endregion

            #region [Categories]

            public void DrawUI_Profiles()
            {
                var ui = new CuiElementContainer();

                #region [PlayerInfo Background]

                ui.Add(new CuiElement
                {
                    Name = $"{UILayerCategory}.PlayerInfo",
                    Parent = UILayerCategory,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = _.GetImage($"zealmenu.bg.playerinfo.{karmaStage}"),
                            FadeIn = 0.5f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.1194715 0.545369",
                            AnchorMax = "0.614002 0.9032792"
                        }
                    }
                });

                if (karmaStage == 1)
                {
                    ui.Add(new CuiElement
                    {
                        Name = $"{UILayerCategory}.PlayerInfo.Horns",
                        Parent = $"{UILayerCategory}.PlayerInfo",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = _.GetImage("zealmenu.playerinfo.horns"),
                                FadeIn = 0.5f
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.285 0.8537224",
                                AnchorMax = "0.714 1.101795"
                            }
                        }
                    });
                }

                ui.Add(new CuiElement
                {
                    Name = $"{UILayerCategory}.PlayerInfo.ClanFlag",
                    Parent = $"{UILayerCategory}.PlayerInfo",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = _.GetImage($"zealmenu.playerinfo.clanflag.{karmaStage}"),
                            FadeIn = 1f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.39 -0.4837753",
                            AnchorMax = "0.61 0.00610882"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UILayerCategory}.PlayerInfo.TrophyDecor",
                    Parent = $"{UILayerCategory}.PlayerInfo",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = _.GetImage($"zealmenu.playerinfo.trophy.decor.{karmaStage}"),
                            FadeIn = 1f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.2322446 -1.446154",
                            AnchorMax = "0.7650398 -0.91"
                        }
                    }
                });

                #endregion

                #region [PlayerInfo]

                ui.Add(new CuiElement
                {
                    Name = $"{UILayerCategory}.PlayerInfo.DisplayName",
                    Parent = $"{UILayerCategory}.PlayerInfo",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Text = NameProcess(player.displayName),
                            FontSize = 17,
                            FadeIn = 0.5f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.3630483 -0.002583307",
                            AnchorMax = "0.6353337 0.2017925"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UILayerCategory}.PlayerInfo.KillsValue",
                    Parent = $"{UILayerCategory}.PlayerInfo",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Text = $"{(int) _.ZealMStatistics.Call("GetKills")}",
                            FontSize = 17,
                            FadeIn = 0.5f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.001335649 0.5303462",
                            AnchorMax = "0.1855287 0.7295479"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UILayerCategory}.PlayerInfo.DeathsValue",
                    Parent = $"{UILayerCategory}.PlayerInfo",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Text = $"{(int) _.ZealMStatistics.Call("GetDeaths")}",
                            FontSize = 17,
                            FadeIn = 0.5f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.09877115 0.209554",
                            AnchorMax = "0.2829642 0.4087555"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UILayerCategory}.PlayerInfo.RatingValue",
                    Parent = $"{UILayerCategory}.PlayerInfo",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Text = $"{0}",
                            FontSize = 17,
                            FadeIn = 0.5f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.8141848 0.5303462",
                            AnchorMax = "0.9983776 0.7295479"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UILayerCategory}.PlayerInfo.PlayingTimeValue",
                    Parent = $"{UILayerCategory}.PlayerInfo",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Text = $"{((float) _.ZealMStatistics.Call("GetPlayingTime"))}",
                            FontSize = 17,
                            FadeIn = 0.5f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.7194193 0.209554",
                            AnchorMax = "0.9036123 0.4087555"
                        }
                    }
                });

                #endregion

                DrawUI_CategoryLayer();
                CuiHelper.AddUi(player, ui);
                DrawUI_Trophy();
            }

            public void DrawUI_Trophy()
            {
                var ui = new CuiElementContainer();

                ui.Add(new CuiPanel
                {
                    Image =
                    {
                        Color = "0 0 0 0"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.04137759 -1.52376",
                        AnchorMax = "0.9543346 -0.8666528"
                    }
                }, $"{UILayerCategory}.PlayerInfo", $"{UILayerCategory}.PlayerInfo.TrophyLayer");

                ui.Add(new CuiElement
                {
                    Name = $"{UILayerCategory}.PlayerInfo.Trophy.Button.Left",
                    Parent = $"{UILayerCategory}.PlayerInfo",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = _.GetImage($"zealmenu.playerinfo.trophy.button.left.{karmaStage}"),
                            FadeIn = 0.5f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.02002187 -1.443567",
                            AnchorMax = "0.1002305 -1.101"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UILayerCategory}.PlayerInfo.Trophy.Button.Right",
                    Parent = $"{UILayerCategory}.PlayerInfo",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = _.GetImage($"zealmenu.playerinfo.trophy.button.right.{karmaStage}"),
                            FadeIn = 0.5f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.8956095 -1.443567",
                            AnchorMax = "0.9758168 -1.101"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UILayerCategory}.PlayerInfo.TrophyLayer.TrophyBG.1",
                    Parent = $"{UILayerCategory}.PlayerInfo.TrophyLayer",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = _.GetImage($"zealmenu.playerinfo.bg.trophy.{karmaStage}"),
                            FadeIn = 0.5f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.01315787 0.1220472",
                            AnchorMax = "0.2320257 0.6409154"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UILayerCategory}.PlayerInfo.TrophyLayer.TrophyBG.2",
                    Parent = $"{UILayerCategory}.PlayerInfo.TrophyLayer",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = _.GetImage($"zealmenu.playerinfo.bg.trophy.{karmaStage}"),
                            FadeIn = 0.6f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.2017546 0.4094473",
                            AnchorMax = "0.4206223 0.9283162"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UILayerCategory}.PlayerInfo.TrophyLayer.TrophyBG.3",
                    Parent = $"{UILayerCategory}.PlayerInfo.TrophyLayer",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = _.GetImage($"zealmenu.playerinfo.bg.trophy.{karmaStage}"),
                            FadeIn = 0.7f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.3903511 0.1220465",
                            AnchorMax = "0.6092189 0.6409165"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UILayerCategory}.PlayerInfo.TrophyLayer.TrophyBG.4",
                    Parent = $"{UILayerCategory}.PlayerInfo.TrophyLayer",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = _.GetImage($"zealmenu.playerinfo.bg.trophy.{karmaStage}"),
                            FadeIn = 0.8f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5789459 0.4094477",
                            AnchorMax = "0.7978136 0.9283162"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UILayerCategory}.PlayerInfo.TrophyLayer.TrophyBG.5",
                    Parent = $"{UILayerCategory}.PlayerInfo.TrophyLayer",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = _.GetImage($"zealmenu.playerinfo.bg.trophy.{karmaStage}"),
                            FadeIn = 0.9f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.7675424 0.1220465",
                            AnchorMax = "0.9864102 0.6409154"
                        }
                    }
                });

                CuiHelper.DestroyUi(player, $"{UILayerCategory}.PlayerInfo.TrophyLayer");
                CuiHelper.AddUi(player, ui);
            }

            public void DrawUI_WipeCalendar()
            {
                var ui = new CuiElementContainer();

                ui.Add(new CuiElement
                {
                    Name = $"{UILayerCategory}_WipeCalendar_BG",
                    Parent = UILayerCategory,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = _.GetImage("zealmenu.wipecalendar.bg"),
                            FadeIn = 0.5f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.075247 0.8092514",
                            AnchorMax = "0.658256 0.9388967"
                        }
                    }
                });

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 33,
                        Text = "РАСПИСАНИЕ ВАЙПОВ",
                        FadeIn = 0.5f
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.1702971 0.8648148",
                        AnchorMax = "0.5636964 0.9379627"
                    }
                }, UILayerCategory);

                #region [Days on the Week]

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 10,
                        Text = "ПОНЕДЕЛЬНИК",
                        FadeIn = 0.5f,
                        Font = Regular
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.07527235 0.8092593",
                        AnchorMax = "0.1518629 0.845238"
                    }
                }, UILayerCategory);

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 10,
                        Text = "ВТОРНИК",
                        FadeIn = 0.5f,
                        Font = Regular
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.1604209 0.8092593",
                        AnchorMax = "0.2370109 0.845238"
                    }
                }, UILayerCategory);

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 10,
                        Text = "СРЕДА",
                        FadeIn = 0.5f,
                        Font = Regular
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.2449088 0.8092593",
                        AnchorMax = "0.3214988 0.845238"
                    }
                }, UILayerCategory);

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 10,
                        Text = "ЧЕТВЕРГ",
                        FadeIn = 0.5f,
                        Font = Regular
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.3287367 0.8092593",
                        AnchorMax = "0.4053267 0.845238"
                    }
                }, UILayerCategory);

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 10,
                        Text = "ПЯТНИЦА",
                        FadeIn = 0.5f,
                        Font = Regular
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.4132246 0.8092593",
                        AnchorMax = "0.4898146 0.845238"
                    }
                }, UILayerCategory);

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 10,
                        Text = "СУББОТА",
                        FadeIn = 0.5f,
                        Font = Regular
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.4977125 0.8092593",
                        AnchorMax = "0.5743025 0.845238"
                    }
                }, UILayerCategory);

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 10,
                        Text = "ВОСКРЕСЕНЬЕ",
                        FadeIn = 0.5f,
                        Font = Regular
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5815403 0.8092593",
                        AnchorMax = "0.6581303 0.845238"
                    }
                }, UILayerCategory);

                #endregion

                #region [Days on the Month]

                var date = DateTime.Now;
                var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
                int dayPosX = 0, dayPosY = 0;
                switch (firstDayOfMonth.DayOfWeek)
                {
                    case DayOfWeek.Monday:
                        dayPosX = 0;
                        break;
                    case DayOfWeek.Tuesday:
                        dayPosX = 1;
                        break;
                    case DayOfWeek.Wednesday:
                        dayPosX = 2;
                        break;
                    case DayOfWeek.Thursday:
                        dayPosX = 3;
                        break;
                    case DayOfWeek.Friday:
                        dayPosX = 4;
                        break;
                    case DayOfWeek.Saturday:
                        dayPosX = 5;
                        break;
                    case DayOfWeek.Sunday:
                        dayPosX = 6;
                        break;
                    default:
                        UI_Destroy();
                        return;
                }

                for (int i = 1; i <= lastDayOfMonth.Day; i++, dayPosX++)
                {
                    if (dayPosX == 7)
                    {
                        dayPosX = 0;
                        dayPosY++;
                    }

                    var dayColor = "#9191913E";
                    var presentDay = DateTime.Now.Day;

                    if (presentDay == i) dayColor = "#91919180";
                    if (i == 13) dayColor = "#007FFF80";
                    if (i == 16) dayColor = "#CE373780";

                    ui.Add(new CuiElement
                    {
                        Name = $"{UILayerCategory}_WipeCalendar_BG_Day_{i}",
                        Parent = UILayerCategory,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = HexToRustFormat(dayColor),
                                Material = Sharp,
                                FadeIn = 0.5f
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0.076 + (dayPosX * 0.0845)} {0.6956043 - (dayPosY * 0.1194443)}",
                                AnchorMax = $"{0.151 + (dayPosX * 0.0845)} {0.8015718 - (dayPosY * 0.1194443)}"
                            }
                        }
                    });

                    ui.Add(new CuiLabel
                    {
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 30,
                            Text = $"{i}",
                            FadeIn = 0.5f
                        },
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0.995 0.995"
                        }
                    }, $"{UILayerCategory}_WipeCalendar_BG_Day_{i}");
                }

                ui.Add(new CuiElement
                {
                    Name = $"{UILayerCategory}_WipeMap",
                    Parent = UILayerCategory,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#CE373780"),
                            Material = Sharp,
                            FadeIn = 0.5f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0.0752474 {0.6592557 - (dayPosY * 0.12)}",
                            AnchorMax = $"0.2224854 {0.6861067 - (dayPosY * 0.12)}"
                        }
                    }
                });

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 15,
                        Text = "ВАЙП КАРТЫ",
                        FadeIn = 0.5f
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, $"{UILayerCategory}_WipeMap");

                ui.Add(new CuiElement
                {
                    Name = $"{UILayerCategory}_WipeGlobal",
                    Parent = UILayerCategory,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#007FFF80"),
                            Material = Sharp,
                            FadeIn = 0.5f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0.2931321 {0.6592557 - (dayPosY * 0.12)}",
                            AnchorMax = $"0.4403699 {0.6861067 - (dayPosY * 0.12)}"
                        }
                    }
                });

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 15,
                        Text = "ГЛОБАЛЬНЫЙ ВАЙП",
                        FadeIn = 0.5f
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, $"{UILayerCategory}_WipeGlobal");

                ui.Add(new CuiElement
                {
                    Name = $"{UILayerCategory}_WipeGlobal",
                    Parent = UILayerCategory,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#92929280"),
                            Material = Sharp,
                            FadeIn = 0.5f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0.5102924 {0.6592557 - (dayPosY * 0.12)}",
                            AnchorMax = $"0.6575301 {0.6861067 - (dayPosY * 0.12)}"
                        }
                    }
                });

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 15,
                        Text = "ТЕКУЩИЙ ДЕНЬ",
                        FadeIn = 0.5f
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, $"{UILayerCategory}_WipeGlobal");

                #endregion

                DrawUI_CategoryLayer();
                CuiHelper.AddUi(player, ui);
            }

            #endregion

            public void DrawUI_CategoryLayer()
            {
                var ui = new CuiElementContainer();

                ui.Add(new CuiPanel
                {
                    Image =
                    {
                        Color = "0 0 0 0"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.2109375 0",
                        AnchorMax = "1 1"
                    }
                }, UILayerMenu, UILayerCategory);

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Close = UILayerMenu,
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
                }, UILayerCategory);

                CuiHelper.DestroyUi(player, UILayerCategory);
                CuiHelper.AddUi(player, ui);
            }

            public void UI_Destroy()
            {
                CuiHelper.DestroyUi(player, UILayerMenu);
                CuiHelper.DestroyUi(player, UILayerHud);
            }

            private void OnDestroy()
            {
                UI_Destroy();
            }
        }

        #endregion

        #region [Hooks] / [Крюки]

        private void OnServerInitialized()
        {
            _ = this;
            Start_Process(Load_Images());
            Start_Process(Load_UICore());
        }

        private void Unload()
        {
            Start_Process(Unload_UICore());
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            NextTick(() => Load_Component(player));
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            UnityEngine.Object.Destroy(UICores[player.userID]);
            UICores.Remove(player.userID);
        }

        #endregion

        #region [ChatCommands] / [Чат команды]

        [ChatCommand("menu")]
        private void OpenMainMenu(BasePlayer player)
        {
            if (!Check_UICore(player)) return;
            UICores[player.userID].DrawUI_Parent();
        }

        [ConsoleCommand("zealmenu.close")]
        private void CloseMainMenu(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!Check_UICore(player)) return;
            CuiHelper.DestroyUi(player, UILayerMenu);
            CuiHelper.DestroyUi(player, UILayerHud + ".Alert");
        }

        [ConsoleCommand("zealmenu.profile")]
        private void OpenProfiles(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!Check_UICore(player)) return;
            UICores[player.userID].DrawUI_Profiles();
        }

        [ConsoleCommand("zealmenu.calendar")]
        private void OpenWipeCalendar(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!Check_UICore(player)) return;
            UICores[player.userID].DrawUI_WipeCalendar();
        }

        [ConsoleCommand("zealmenu.wipeblock")]
        private void OpenWipeBlock(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!Check_UICore(player)) return;
        }

        #endregion

        #region [Helpers]

        private static string NameProcess(string name)
        {
            return name.Length <= 11 ? name : name.Remove(name.IndexOf("#", StringComparison.Ordinal));
        }

        private IEnumerator Load_UICore()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.GetComponent<UICore>() != null) yield return null;
                var component = player.gameObject.AddComponent<UICore>();
                UICores.Add(player.userID, component);
                PrintWarning($"Load UICore {player}");
            }

            yield return 0;
        }

        private IEnumerator Unload_UICore()
        {
            foreach (var obj in UICores)
            {
                PrintWarning($"Unload UICore {obj.Value.player}");
                UnityEngine.Object.Destroy(obj.Value);
            }

            UICores.Clear();
            yield return 0;
        }

        private IEnumerator Load_Images()
        {
            ImageLibrary.Call("AddImage", "https://i.imgur.com/74j0Vcy.png", "zealmenu.icon.1");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/xi8DgTP.png", "zealmenu.icon.2");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/1EsbwGi.png", "zealmenu.bg.alert.1");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/zoGmJ3b.png", "zealmenu.bg.alert.2");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/B7GYqGx.png", "zealmenu.bg.playerinfo.1");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/C2zqYMs.png", "zealmenu.bg.playerinfo.2");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/MUJwe1o.png", "zealmenu.playerinfo.horns");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/OZ2PaPf.png", "zealmenu.playerinfo.clanflag.1");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/oDYFchn.png", "zealmenu.playerinfo.clanflag.2");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/xmmdh8i.png", "zealmenu.playerinfo.bg.trophy.1");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/sMZsaZR.png", "zealmenu.playerinfo.bg.trophy.2");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/zDi7G7P.png", "zealmenu.playerinfo.trophy.decor.1");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/ycSsML8.png", "zealmenu.playerinfo.trophy.decor.2");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/wxhqqgD.png", "zealmenu.playerinfo.trophy.button.left.1");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/f7Xfqv6.png", "zealmenu.playerinfo.trophy.button.right.1");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/bAN27IB.png", "zealmenu.playerinfo.trophy.button.left.2");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/5K0ac9A.png", "zealmenu.playerinfo.trophy.button.right.2");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/vOAExL0.png", "zealmenu.wipecalendar.bg");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/ppUVy7N.png", "zealmenu.wipeblock.bg");

            foreach (var button in Buttons)
            {
                ImageLibrary.CallHook("AddImage", button.Url, $"zealmenu.{button.Name}");
                yield return new WaitForSeconds(0.1f);
            }

            yield return 0;
        }

        private static void Start_Process(IEnumerator obj)
        {
            ServerMgr.Instance.StartCoroutine(obj);
        }

        private void Load_Component(BasePlayer player)
        {
            var component = player.gameObject.AddComponent<UICore>();
            UICores.Add(player.userID, component);
            PrintWarning($"Load UICore : {player}");
        }

        private bool Check_UICore(BasePlayer player)
        {
            if (UICores.ContainsKey(player.userID)) return true;
            Load_Component(player);
            player.ChatMessage("Error code #1\nСвяжитесь с администратором");
            PrintError($"Error code #1 у игрока {player.userID}");
            return false;
        }

        private string GetImage(string name)
        {
            return (string) ImageLibrary.Call("GetImage", name);
        }

        public void AddImage(string url, string name)
        {
            ImageLibrary.Call("AddImage", url, name);
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