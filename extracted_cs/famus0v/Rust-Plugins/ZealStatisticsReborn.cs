using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ZealStatisticsReborn", "Kira", "1.0.0")]
    [Description("Interface for displaying player statistics")]
    public class ZealStatisticsReborn : RustPlugin
    {
        #region [Reference] / [Ссылки]

        [PluginReference] private Plugin ImageLibrary;

        #endregion

        #region [Vars] / [Переменные]

        private StoredData _dataBase = new StoredData();
        private ulong _lastDamagePlayer;
        private const string UILayerMain = "ZealStatisticsReborn_UI_Layer";
        private const string UILayerServer = "ZealStatisticsReborn_UI_LayerServer";
        private const string Sharp = "assets/content/ui/ui.background.tile.psd";
        private const string Blur = "assets/content/ui/uibackgroundblur.mat";
        private const string Radial = "assets/content/ui/ui.background.transparent.radial.psd";

        private class Filter
        {
            public string Name;
            public int Number;
        }

        private readonly List<Filter> _filters = new List<Filter>
        {
            new Filter
            {
                Name = "KILLS",
                Number = 0
            },
            new Filter
            {
                Name = "DEATHS",
                Number = 1
            },
            new Filter
            {
                Name = "ANIMAL_KILLS",
                Number = 2
            },
            new Filter
            {
                Name = "GATHER",
                Number = 3
            },
            new Filter
            {
                Name = "ENTITY_DESTROYING",
                Number = 4
            },
            new Filter
            {
                Name = "TIME_PLAYING",
                Number = 5
            },
        };

        #endregion 
 
        #region [Lang]

        protected override void LoadDefaultMessages()
        {
            var ru = new Dictionary<string, string>
            {
                ["KILL_NAME"] = "Убито игроков",
                ["DEATH_NAME"] = "Смертей от игроков",
                ["KILLANIMALS_NAME"] = "Убито животных",
                ["GATHERRESOURCES_NAME"] = "Добыто ресурсов",
                ["DESTROYINGOBJECTS_NAME"] = "Уничтожено объектов",
                ["PLAYEDTIME_NAME"] = "Наигранное время : ",
                ["General"] = "Общий счёт",
                ["Firearms"] = "Огнестрельное",
                ["Coldsteel"] = "Холодное",
                ["Bow"] = "Лук",
                ["Headshot"] = "В голову",
                ["Wounded"] = "Раненых",
                ["Sleeping"] = "Спящих",
                ["Bear"] = "Медведей",
                ["Boar"] = "Кабанов",
                ["Chicken"] = "Куриц",
                ["Stag"] = "Оленей",
                ["Horse"] = "Лошадей",
                ["Wolf"] = "Волков",
                ["Wood"] = "Дерева",
                ["Stones"] = "Камня",
                ["MetalOre"] = "Железа",
                ["SulfurOre"] = "Серы",
                ["Constructions"] = "Конструкций",
                ["Helicopter"] = "Вертолётов",
                ["BradleyApc"] = "Танков",
                ["AVATAR"] = "АВАТАР",
                ["NAME"] = "ИМЯ",
                ["KILLS"] = "УБИЙСТВ",
                ["DEATHS"] = "СМЕРТЕЙ",
                ["ANIMAL_KILLS"] = "УБИЙСТВ ЖИВОТНЫХ",
                ["GATHER"] = "ДОБЫТО РЕСУРСОВ",
                ["ENTITY_DESTROYING"] = "УНИЧТОЖЕНО ОБЪЕКТОВ",
                ["TIME_PLAYING"] = "НАИГРАННО ВРЕМЕНИ",
                ["GOTOP"] = "ПЕРЕЙТИ В ОБЩУЮ СТАТИСТИКУ\n▼",
                ["GOMAIN"] = "ПЕРЕЙТИ В ЛИЧНУЮ СТАТИСТИКУ\n▼",
                ["TOP"] = "ОБЩАЯ СТАТИСТИКА"
            };

            var en = new Dictionary<string, string>
            {
                ["KILL_NAME"] = "Killed players",
                ["DEATH_NAME"] = "Deaths for players",
                ["KILLANIMALS_NAME"] = "Killed animals",
                ["GATHERRESOURCES_NAME"] = "Gather resources",
                ["DESTROYINGOBJECTS_NAME"] = "Destroying objects",
                ["PLAYEDTIME_NAME"] = "Playing time : ",
                ["General"] = "General scores",
                ["Firearms"] = "Firearms",
                ["Coldsteel"] = "Coldsteel",
                ["Bow"] = "Bow",
                ["Headshot"] = "Headshot",
                ["Wounded"] = "Wounded",
                ["Sleeping"] = "Sleeping",
                ["Bear"] = "Bear",
                ["Boar"] = "Boar",
                ["Chicken"] = "Chicken",
                ["Stag"] = "Stag",
                ["Horse"] = "Horse",
                ["Wolf"] = "Wolf",
                ["Wood"] = "Wood",
                ["Stones"] = "Stones",
                ["MetalOre"] = "Metal",
                ["SulfurOre"] = "Sulfur",
                ["Constructions"] = "Constructions",
                ["Helicopter"] = "Helicopters",
                ["BradleyApc"] = "Tanks",
                ["AVATAR"] = "AVATAR",
                ["NAME"] = "NAME",
                ["KILLS"] = "KILLS",
                ["DEATHS"] = "DEATHS",
                ["ANIMAL_KILLS"] = "ANIMAL KILLS",
                ["GATHER"] = "GATHER RESOURCES",
                ["ENTITY_DESTROYING"] = "DESTROYING ENTITIES",
                ["TIME_PLAYING"] = "PLAYING TIME",
                ["GOTOP"] = "GO TO GENERAL STATISTICS\n▼",
                ["GOMAIN"] = "GO TO PERSONAL STATISTICS\n▼",
                ["TOP"] = "GENERAL STATISTICS"
            };
            lang.RegisterMessages(ru, this, "ru");
            lang.RegisterMessages(en, this);
        }

        #endregion
 
        #region [DrawUI] / [Отрисовка UI]

        private void DrawUI_MainStatistic(BasePlayer player, bool kills, bool killsAnimal, bool gather,
            bool objDestroying)
        {
            var ui = new CuiElementContainer
            {
                {
                    new CuiPanel
                    {
                        CursorEnabled = true,
                        Image =
                        {
                            Color = HexToRustFormat("#000000F9"),
                            Material = Blur,
                            Sprite = Radial
                        },
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    },
                    "Overlay", UILayerMain
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = "zstats.close",
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
                    },
                    UILayerMain, "ButtonCloseGUI"
                },
                new CuiElement
                {
                    Name = $"{UILayerMain}_ServerName",
                    Parent = UILayerMain,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#EAEAEAFF"),
                            FontSize = 40,
                            Text = ConVar.Server.hostname,
                            Font = "robotocondensed-regular.ttf"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.9213709",
                            AnchorMax = "1 1"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#000000AE"),
                            Distance = "0.5 0.5"
                        }
                    }
                },
                new CuiElement
                {
                    Name = $"{UILayerMain}_BGAvatar",
                    Parent = UILayerMain,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#FFFFFFA4")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.4360861 0.6925457",
                            AnchorMax = "0.5627829 0.9183521"
                        }
                    }
                },
                new CuiElement
                {
                    Name = $"{UILayerMain}_BGAvatar_Avatar",
                    Parent = $"{UILayerMain}_BGAvatar",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = (string) ImageLibrary.Call("GetImage", player.UserIDString)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.01 0.01",
                            AnchorMax = "0.988 0.989"
                        }
                    }
                },
                new CuiElement
                {
                    Name = $"{UILayerMain}_Avatar_Sprite",
                    Parent = $"{UILayerMain}_BGAvatar_Avatar",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#000000AA"),
                            Sprite = Radial
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0.99 0.99"
                        }
                    }
                },
                new CuiElement
                {
                    Name = $"{UILayerMain}_Name",
                    Parent = UILayerMain,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1",
                            FontSize = 23,
                            Text = player.displayName,
                            Font = "robotocondensed-regular.ttf"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.3212669 0.641129",
                            AnchorMax = "0.6804298 0.6875"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#000000AE"),
                            Distance = "0.5 0.5"
                        }
                    }
                },
                new CuiElement
                {
                    Name = $"{UILayerMain}_Name_Line",
                    Parent = UILayerMain,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#CBCBCBFF")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.3829193 0.6401247",
                            AnchorMax = "0.6193514 0.6411328"
                        }
                    }
                },
                new CuiElement
                {
                    Name = $"{UILayerMain}_SteamID",
                    Parent = UILayerMain,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1",
                            FontSize = 14,
                            Text = player.UserIDString,
                            Font = "robotocondensed-regular.ttf"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.3212669 0.6088712",
                            AnchorMax = "0.6804298 0.6421358"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#000000AE"),
                            Distance = "0.5 0.5"
                        }
                    }
                },
                {
                    new CuiPanel
                    {
                        Image =
                        {
                            Color = "0 0 0 0"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.08177081 0.4453703",
                            AnchorMax = "0.9197907 0.5740741"
                        }
                    },
                    UILayerMain, $"{UILayerMain}_Layer_Statistics"
                },
                new CuiElement
                {
                    Name = $"{UILayerMain}_PlayedTimeValue",
                    Parent = UILayerMain,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#CBCBCBFF"),
                            FontSize = 15,
                            Text =
                                $"{lang.GetMessage("PLAYEDTIME_NAME", this, player.UserIDString)}{Convert.ToInt32(TimeSpan.FromSeconds(_dataBase.PlayerDB[player.userID].ActiveTimeDB.Seconds).TotalHours)} H"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.3552083 0.5601852",
                            AnchorMax = "0.6453125 0.6064815"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#000000AE"),
                            Distance = "0.5 0.5"
                        }
                    }
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = "servertop",
                            Color = "0 0 0 0"
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#EAEAEAFF"),
                            FontSize = 18,
                            Text = lang.GetMessage("GOTOP", this, player.UserIDString)
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.3665158 0.01108896",
                            AnchorMax = "0.6329185 0.08165347"
                        }
                    },
                    UILayerMain, "GoTop"
                }
            };


            CuiHelper.DestroyUi(player, UILayerMain);
            CuiHelper.DestroyUi(player, UILayerServer);
            CuiHelper.AddUi(player, ui);
            DrawUI_KillStat(player, false);
            DrawUI_DeathsStat(player);
            DrawUI_KillAnimalsStat(player, false);
            DrawUI_GatherResourcesStat(player, false);
            DrawUI_ObjectDestroyingStat(player, false);
        }

        private void DrawUI_ServerStatistic(BasePlayer player)
        {
            var ui = new CuiElementContainer
            {
                {
                    new CuiPanel
                    {
                        CursorEnabled = true,
                        Image =
                        {
                            Color = HexToRustFormat("#000000F9"),
                            Material = Blur, Sprite = Radial
                        },
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    },
                    "Overlay", UILayerServer
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = "zstats.close",
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
                    },
                    UILayerServer, "ButtonCloseGUI"
                },
                new CuiElement
                {
                    Name = "ZagServerTOP",
                    Parent = UILayerServer,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#EAEAEAFF"),
                            FontSize = 40,
                            Text = lang.GetMessage("TOP", this, player.UserIDString)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.9354839",
                            AnchorMax = "1 1"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#000000AE"),
                            Distance = "0.5 0.5"
                        }
                    }
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"chat.say /stat",
                            Color = "0 0 0 0"
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#EAEAEAFF"),
                            FontSize = 18,
                            Text = lang.GetMessage("GOMAIN", this, player.UserIDString)
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.3665158 0.01108896",
                            AnchorMax = "0.6329185 0.08165347"
                        }
                    },
                    UILayerServer, "GoMainGui"
                },
                new CuiElement
                {
                    Name = $"{UILayerServer}_UI_Layer_Filters",
                    Parent = UILayerServer,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#00000086")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.1239583 0.8916631",
                            AnchorMax = "0.8760417 0.9333302"
                        }
                    }
                },
                {
                    new CuiLabel
                    {
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 11,
                            Text = lang.GetMessage("AVATAR", this, player.UserIDString)
                        },
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0.06666667 1"
                        }
                    },
                    $"{UILayerServer}_UI_Layer_Filters"
                },
                {
                    new CuiLabel
                    {
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 11,
                            Text = lang.GetMessage("NAME", this, player.UserIDString)
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.07132962 0",
                            AnchorMax = "0.1945983 1"
                        }
                    },
                    $"{UILayerServer}_UI_Layer_Filters"
                }
            };

            var x = 0;
            foreach (var obj in _filters)
            {
                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Color = "0 0 0 0",
                        Command = $"filter {obj.Number}"
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 11,
                        Text = lang.GetMessage(obj.Name, this, player.UserIDString),
                        FadeIn = 0.1f * x
                    },
                    RectTransform =
                    {
                        AnchorMin = $"{0.2119114 + (x * 0.134)} 0",
                        AnchorMax = $"{0.3185595 + (x * 0.134)} 1"
                    }
                }, $"{UILayerServer}_UI_Layer_Filters");
                x++;
            }

            CuiHelper.DestroyUi(player, UILayerMain);
            CuiHelper.DestroyUi(player, UILayerServer);
            CuiHelper.AddUi(player, ui);
            MathStates(player, 0);
        }

        private void DrawUI_KillStat(BasePlayer player, bool isopen)
        {
            var layer = $"{UILayerMain}_Layer_Statistics";
            var ui = new CuiElementContainer
            {
                {
                    new CuiPanel
                    {
                        Image =
                        {
                            Color = "0 0 0 0"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.01243009 0.1438849",
                            AnchorMax = "0.197514 0.8561152"
                        }
                    },
                    layer, $"{layer}_Kill"
                },
                new CuiElement
                {
                    Name = $"{layer}_KillTXT",
                    Parent = $"{layer}_Kill",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#CBCBCBFF"),
                            FontSize = 18,
                            Text = lang.GetMessage("KILL_NAME", this, player.UserIDString)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.5",
                            AnchorMax = "1 1"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#000000AE"),
                            Distance = "0.5 0.5"
                        }
                    }
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"kill_more {isopen}",
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
                    },
                    $"{layer}_KillTXT"
                },
                new CuiElement
                {
                    Name = $"{layer}_KillLine",
                    Parent = $"{layer}_Kill",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#CBCBCBFF")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.49",
                            AnchorMax = "1 0.5"
                        }
                    }
                }
            };

            if (!isopen)
            {
                ui.Add(new CuiElement
                {
                    Name = $"{layer}_KillValue",
                    Parent = $"{layer}_Kill",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#CBCBCBFF"),
                            FontSize = 16,
                            Text = $"{_dataBase.PlayerDB[player.userID].KillsDB.KillElements["General"]}"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 0.5"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#000000AE"),
                            Distance = "0.5 0.5"
                        }
                    }
                });
            }
            else
            {
                var y = 0;
                foreach (var obj in _dataBase.PlayerDB[player.userID].KillsDB.KillElements)
                {
                    ui.Add(new CuiElement
                    {
                        Name = $"{layer}_KillValue",
                        Parent = $"{layer}_Kill",
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Align = TextAnchor.MiddleLeft,
                                Color = HexToRustFormat("#CBCBCBFF"),
                                FontSize = 16,
                                Text =
                                    $"            {lang.GetMessage(obj.Key, this, player.UserIDString)} : {obj.Value}",
                                FadeIn = 0.1f * y
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0} {0 - (y * 0.5)}",
                                AnchorMax = $"{1} {0.5 - (y * 0.5)}"
                            },
                            new CuiOutlineComponent
                            {
                                Color = HexToRustFormat("#000000AE"),
                                Distance = "0.5 0.5"
                            }
                        }
                    });
                    y++;
                }
            }

            CuiHelper.DestroyUi(player, $"{layer}_Kill");
            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_DeathsStat(BasePlayer player)
        {
            var ui = new CuiElementContainer();
            var layer = $"{UILayerMain}_Layer_Statistics";

            ui.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = "0.2099441 0.1438849",
                    AnchorMax = "0.395028 0.8561152"
                }
            }, layer, $"{layer}_Deaths");

            ui.Add(new CuiElement
            {
                Name = $"{layer}_DeathsTXT",
                Parent = $"{layer}_Deaths",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        FontSize = 18,
                        Text = lang.GetMessage("DEATH_NAME", this, player.UserIDString)
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.5",
                        AnchorMax = "1 1"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{layer}_DeathsLine",
                Parent = $"{layer}_Deaths",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#CBCBCBFF")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.49",
                        AnchorMax = "1 0.5"
                    }
                }
            });
            ui.Add(new CuiElement
            {
                Name = $"{layer}_DeathsValue",
                Parent = $"{layer}_Deaths",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        FontSize = 16,
                        Text = $"{_dataBase.PlayerDB[player.userID].DeathsDB.Death}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 0.5"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            CuiHelper.DestroyUi(player, $"{layer}_Deaths");
            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_KillAnimalsStat(BasePlayer player, bool isopen)
        {
            var ui = new CuiElementContainer();
            var layer = $"{UILayerMain}_Layer_Statistics";

            ui.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = "0.4074581 0.1438849",
                    AnchorMax = "0.5925419 0.8561152"
                }
            }, layer, $"{layer}_KillAnimals");

            ui.Add(new CuiElement
            {
                Name = $"{layer}_KillAnimalsTXT",
                Parent = $"{layer}_KillAnimals",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        FontSize = 16,
                        Text = lang.GetMessage("KILLANIMALS_NAME", this, player.UserIDString)
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.5",
                        AnchorMax = "1 1"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            ui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"animals_more {isopen}",
                    Color = "0 0 0 0"
                },
                Text = {Text = " "},
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, $"{layer}_KillAnimalsTXT");

            ui.Add(new CuiElement
            {
                Name = $"{layer}_KillAnimalsLine",
                Parent = $"{layer}_KillAnimals",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#CBCBCBFF")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.49",
                        AnchorMax = "1 0.5"
                    }
                }
            });

            if (!isopen)
            {
                ui.Add(new CuiElement
                {
                    Name = $"{layer}_KillAnimalsValue",
                    Parent = $"{layer}_KillAnimals",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#CBCBCBFF"),
                            FontSize = 16,
                            Text = $"{_dataBase.PlayerDB[player.userID].AnimalDB.AnimalElements["General"]}"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 0.5"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#000000AE"),
                            Distance = "0.5 0.5"
                        }
                    }
                });
            }
            else
            {
                var y = 0;
                foreach (var obj in _dataBase.PlayerDB[player.userID].AnimalDB.AnimalElements)
                {
                    ui.Add(new CuiElement
                    {
                        Name = $"{layer}_KillAnimalsValue",
                        Parent = $"{layer}_KillAnimals",
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Align = TextAnchor.MiddleLeft,
                                Color = HexToRustFormat("#CBCBCBFF"),
                                FontSize = 16,
                                Text =
                                    $"            {lang.GetMessage(obj.Key, this, player.UserIDString)} : {obj.Value}",
                                FadeIn = 0.1f * y
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0} {0 - (y * 0.5)}",
                                AnchorMax = $"{1} {0.5 - (y * 0.5)}"
                            },
                            new CuiOutlineComponent
                            {
                                Color = HexToRustFormat("#000000AE"),
                                Distance = "0.5 0.5"
                            }
                        }
                    });
                    y++;
                }
            }

            CuiHelper.DestroyUi(player, $"{layer}_KillAnimals");
            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_GatherResourcesStat(BasePlayer player, bool isopen)
        {
            var ui = new CuiElementContainer();
            var layer = $"{UILayerMain}_Layer_Statistics";

            ui.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = "0.6049721 0.1438849",
                    AnchorMax = "0.790056 0.8561152"
                }
            }, layer, $"{layer}_GatherResources");

            ui.Add(new CuiElement
            {
                Name = $"{layer}_GatherResourcesTXT",
                Parent = $"{layer}_GatherResources",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        FontSize = 16,
                        Text = lang.GetMessage("GATHERRESOURCES_NAME", this, player.UserIDString)
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.5",
                        AnchorMax = "1 1"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            ui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"gather_more {isopen}",
                    Color = "0 0 0 0"
                },
                Text = {Text = " "},
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, $"{layer}_GatherResourcesTXT");

            ui.Add(new CuiElement
            {
                Name = $"{layer}_GatherResourcesLine",
                Parent = $"{layer}_GatherResources",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#CBCBCBFF")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.49",
                        AnchorMax = "1 0.5"
                    }
                }
            });

            if (!isopen)
            {
                ui.Add(new CuiElement
                {
                    Name = $"{layer}_GatherResourcesValue",
                    Parent = $"{layer}_GatherResources",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#CBCBCBFF"),
                            FontSize = 16,
                            Text = $"{_dataBase.PlayerDB[player.userID].ResourcesDB.ResourceElements["General"]}"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 0.5"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#000000AE"),
                            Distance = "0.5 0.5"
                        }
                    }
                });
            }
            else
            {
                var y = 0;
                foreach (var obj in _dataBase.PlayerDB[player.userID].ResourcesDB.ResourceElements)
                {
                    ui.Add(new CuiElement
                    {
                        Name = $"{layer}_GatherResourcesValue",
                        Parent = $"{layer}_GatherResources",
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Align = TextAnchor.MiddleLeft,
                                Color = HexToRustFormat("#CBCBCBFF"),
                                FontSize = 16,
                                Text =
                                    $"            {lang.GetMessage(obj.Key, this, player.UserIDString)} : {obj.Value}",
                                FadeIn = 0.1f * y
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0} {0 - (y * 0.5)}",
                                AnchorMax = $"{1} {0.5 - (y * 0.5)}"
                            },
                            new CuiOutlineComponent
                            {
                                Color = HexToRustFormat("#000000AE"),
                                Distance = "0.5 0.5"
                            }
                        }
                    });
                    y++;
                }
            }

            CuiHelper.DestroyUi(player, $"{layer}_GatherResources");
            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_ObjectDestroyingStat(BasePlayer player, bool isopen)
        {
            var layer = $"{UILayerMain}_Layer_Statistics";
            var ui = new CuiElementContainer
            {
                {
                    new CuiPanel
                    {
                        Image =
                        {
                            Color = "0 0 0 0" 
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.8024861 0.1438849",
                            AnchorMax = "0.9875699 0.8561152"
                        }
                    },
                    layer, $"{layer}_ObjectDestroying"
                },
                new CuiElement
                {
                    Name = $"{layer}_ObjectDestroyingTXT",
                    Parent = $"{layer}_ObjectDestroying",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#CBCBCBFF"),
                            FontSize = 16,
                            Text = lang.GetMessage("DESTROYINGOBJECTS_NAME", this, player.UserIDString)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.5",
                            AnchorMax = "1 1"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#000000AE"),
                            Distance = "0.5 0.5"
                        }
                    }
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"objects_more {isopen}",
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
                    },
                    $"{layer}_ObjectDestroyingTXT"
                },
                new CuiElement
                {
                    Name = $"{layer}_ObjectDestroyingLine",
                    Parent = $"{layer}_ObjectDestroying",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#CBCBCBFF")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.49",
                            AnchorMax = "1 0.5"
                        }
                    }
                }
            };

            if (!isopen)
            {
                ui.Add(new CuiElement
                {
                    Name = $"{layer}_ObjectDestroyingValue",
                    Parent = $"{layer}_ObjectDestroying",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#CBCBCBFF"),
                            FontSize = 19,
                            Text = $"{_dataBase.PlayerDB[player.userID].EntitiesDB.EntityElements["General"]}"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 0.5"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#000000AE"),
                            Distance = "0.5 0.5"
                        }
                    }
                });
            }
            else
            {
                var y = 0;
                foreach (var obj in _dataBase.PlayerDB[player.userID].EntitiesDB.EntityElements)
                {
                    ui.Add(new CuiElement
                    {
                        Name = $"{layer}_ObjectDestroyingValue",
                        Parent = $"{layer}_ObjectDestroying",
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Align = TextAnchor.MiddleLeft,
                                Color = HexToRustFormat("#CBCBCBFF"),
                                FontSize = 16,
                                Text =
                                    $"            {lang.GetMessage(obj.Key, this, player.UserIDString)} : {obj.Value}",
                                FadeIn = 0.1f * y
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0} {0 - (y * 0.5)}",
                                AnchorMax = $"{1} {0.5 - (y * 0.5)}"
                            },
                            new CuiOutlineComponent
                            {
                                Color = HexToRustFormat("#000000AE"),
                                Distance = "0.5 0.5"
                            }
                        }
                    });
                    y++;
                }
            }

            CuiHelper.DestroyUi(player, $"{layer}_ObjectDestroying");
            CuiHelper.AddUi(player, ui);
        }

        #endregion 

        #region [ConsoleCommand] / [Консольные команды]

        [ChatCommand("stat")]
        private void OpenMainTop(BasePlayer player)
        {
            CheckInDataBase(player);
            DrawUI_MainStatistic(player, false, false, false, false);
        }

        [ConsoleCommand("zstats.close")]
        private void CloseTop(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            CuiHelper.DestroyUi(args.Player(), UILayerMain);
            CuiHelper.DestroyUi(args.Player(), UILayerServer);
        }

        [ConsoleCommand("zstats.resetdb")]
        private void ResetDataBase(ConsoleSystem.Arg args)
        {
            if (args.Player() != null) return;
            _dataBase.PlayerDB.Clear();
            ServerMgr.Instance.StartCoroutine(CheckAllInDataBase());
        }

        [ConsoleCommand("servertop")]
        private void ServerStats(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            CheckInDataBase(args.Player());
            DrawUI_ServerStatistic(args.Player());
        }

        [ConsoleCommand("filter")]
        private void ServerStatsFilter(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            CheckInDataBase(args.Player());
            MathStates(args.Player(), Convert.ToInt32(args.Args[0]));
        }

        [ConsoleCommand("kill_more")]
        private void Kill_More(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            CheckInDataBase(args.Player());
            var isopen = Convert.ToBoolean(args.Args[0]);
            DrawUI_KillStat(args.Player(), !isopen);
        }

        [ConsoleCommand("animals_more")]
        private void KillAnimals_More(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            CheckInDataBase(args.Player());
            var isopen = Convert.ToBoolean(args.Args[0]);
            DrawUI_KillAnimalsStat(args.Player(), !isopen);
        }

        [ConsoleCommand("gather_more")]
        private void Gather_More(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            CheckInDataBase(args.Player());
            var isopen = Convert.ToBoolean(args.Args[0]);
            DrawUI_GatherResourcesStat(args.Player(), !isopen);
        }


        [ConsoleCommand("objects_more")]
        private void Obj_More(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            CheckInDataBase(args.Player());
            var isopen = Convert.ToBoolean(args.Args[0]);
            DrawUI_ObjectDestroyingStat(args.Player(), !isopen);
        }

        #endregion

        #region [Hooks] / [Крюки]

        private void OnServerInitialized()
        {
            if (!VerificationInstall()) return;
            ImageLibrary.Call("AddImage", "https://i.imgur.com/m11MLV7.png", "zstats.logo");
            LoadData();
            ServerMgr.Instance.StartCoroutine(CheckAllInDataBase());
        }

        private void OnNewSave(string filename)
        {
            LoadData();
            PrintWarning(
                $"Произошёл вайп, Backup файл был сохранен (oxide/data/ZealStatistics_Backup/ZealStatistics[{DateTime.Today.ToString("d").Replace("/", "_")}])");
            Backup_DataBase();
            _dataBase.PlayerDB.Clear();
            SaveData();
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            NextTick((() => CheckInDataBase(player)));
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player == null)
            {
                OnPlayerDisconnected(player, reason);
                return;
            }

            CheckInDataBase(player);
            _dataBase.PlayerDB[player.userID].ActiveTimeDB.Seconds +=
                Convert.ToInt32(player.Connection.GetSecondsConnected());
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is BaseHelicopter && info.Initiator is BasePlayer)
                _lastDamagePlayer = info.Initiator.ToPlayer().userID;
            if (entity is BradleyAPC && info.Initiator is BasePlayer)
                _lastDamagePlayer = info.Initiator.ToPlayer().userID;
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (info == null) return;
            if (entity == null) return;
            if (entity.GetComponent<BaseEntity>() is StabilityEntity)
            {
                if (info.Initiator == null) return;
                if (!(info.Initiator is BasePlayer)) return;
                var player = info.InitiatorPlayer;
                if (IsNpc(player)) return;
                CheckInDataBase(player);
                _dataBase.PlayerDB[player.userID].EntitiesDB.EntityElements["Constructions"]++;
                _dataBase.PlayerDB[player.userID].EntitiesDB.EntityElements["General"]++;
            }

            if (entity is BradleyAPC)
            {
                var player = BasePlayer.FindByID(_lastDamagePlayer);
                CheckInDataBase(player);
                _dataBase.PlayerDB[player.userID].EntitiesDB.EntityElements["BradleyApc"]++;
                _dataBase.PlayerDB[player.userID].EntitiesDB.EntityElements["General"]++;
            }

            if (entity is BaseHelicopter)
            {
                var player = BasePlayer.FindByID(_lastDamagePlayer);
                CheckInDataBase(player);
                _dataBase.PlayerDB[player.userID].EntitiesDB.EntityElements["Helicopter"]++;
                _dataBase.PlayerDB[player.userID].EntitiesDB.EntityElements["General"]++;
            }

            if (entity is BasePlayer)
            {
                if (!IsNpc(entity.ToPlayer()))
                    if (info.Initiator is BasePlayer)
                    {
                        if (info.InitiatorPlayer.userID == entity.ToPlayer().userID) return;
                        var player = entity.ToPlayer();
                        CheckInDataBase(player);
                        _dataBase.PlayerDB[player.userID].DeathsDB.Death++;
                    }
            }

            if (info.Initiator is BasePlayer)
            {
                var player = info.InitiatorPlayer;
                if (!IsNpc(player))
                {
                    CheckInDataBase(player);
                    if (entity.name.Contains("agents/"))
                    {
                        _dataBase.PlayerDB[player.userID].AnimalDB.AnimalElements["General"]++;
                        switch (entity.ShortPrefabName)
                        {
                            case "bear":
                                _dataBase.PlayerDB[player.userID].AnimalDB.AnimalElements["Bear"]++;
                                break;
                            case "boar":
                                _dataBase.PlayerDB[player.userID].AnimalDB.AnimalElements["Boar"]++;
                                break;
                            case "chicken":
                                _dataBase.PlayerDB[player.userID].AnimalDB.AnimalElements["Chicken"]++;
                                break;
                            case "horse":
                                _dataBase.PlayerDB[player.userID].AnimalDB.AnimalElements["Horse"]++;
                                break;
                            case "stag":
                                _dataBase.PlayerDB[player.userID].AnimalDB.AnimalElements["Stag"]++;
                                break;
                            case "wolf":
                                _dataBase.PlayerDB[player.userID].AnimalDB.AnimalElements["Wolf"]++;
                                break;
                        }
                    }
                }
            }

            if (info.Initiator is BasePlayer & entity is BasePlayer)
            {
                if (info.InitiatorPlayer.userID == entity.ToPlayer().userID) return;
                var killer = info.InitiatorPlayer;
                var killed = (BasePlayer) entity;
                if (IsNpc(killed)) return;
                if (IsNpc(killer)) return;
                if (killer.userID == killed.userID) return;
                CheckInDataBase(killer);
                CheckInDataBase(killed);
                var killerDB = _dataBase.PlayerDB[killer.userID];
                var damagetype = info.damageTypes.GetMajorityDamageType();
                if (info.isHeadshot) killerDB.KillsDB.KillElements["Headshot"]++;
                if (killed.IsWounded()) killerDB.KillsDB.KillElements["Wounded"]++;
                if (killed.IsSleeping()) killerDB.KillsDB.KillElements["Sleeping"]++;
                killerDB.KillsDB.KillElements["General"]++;
                switch (damagetype)
                {
                    case DamageType.Bullet:
                        killerDB.KillsDB.KillElements["Firearms"]++;
                        break;
                    case DamageType.Slash:
                        killerDB.KillsDB.KillElements["Coldsteel"]++;
                        break;
                    case DamageType.Blunt:
                        killerDB.KillsDB.KillElements["Coldsteel"]++;
                        break;
                    case DamageType.Stab:
                        killerDB.KillsDB.KillElements["Coldsteel"]++;
                        break;
                    case DamageType.Explosion:
                        killerDB.KillsDB.KillElements["Firearms"]++;
                        break;
                    case DamageType.Arrow:
                        killerDB.KillsDB.KillElements["Bow"]++;
                        break;
                    default:
                        throw new NullReferenceException();
                }
            }
        }

        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            var player = entity.ToPlayer();

            CheckInDataBase(player);
            switch (item.info.shortname)
            {
                case "stones":
                    _dataBase.PlayerDB[player.userID].ResourcesDB.ResourceElements["Stones"] += item.amount;
                    break;
                case "wood":
                    _dataBase.PlayerDB[player.userID].ResourcesDB.ResourceElements["Wood"] += item.amount;
                    break;
                case "metal.ore":
                    _dataBase.PlayerDB[player.userID].ResourcesDB.ResourceElements["MetalOre"] += item.amount;
                    break;
                case "sulfur.ore":
                    _dataBase.PlayerDB[player.userID].ResourcesDB.ResourceElements["SulfurOre"] += item.amount;
                    break;
            }

            _dataBase.PlayerDB[player.userID].ResourcesDB.ResourceElements["General"] += item.amount;
        }

        // ReSharper disable once UnusedMember.Local
        private void OnDispenserBonus(ResourceDispenser dispenser, BaseEntity entity, Item item) =>
            OnDispenserGather(dispenser, entity, item);


        // ReSharper disable once UnusedMember.Local
        private void OnCollectiblePickup(Item item, BasePlayer player)
        {
            CheckInDataBase(player);
            switch (item.info.shortname)
            {
                case "stones":
                    _dataBase.PlayerDB[player.userID].ResourcesDB.ResourceElements["Stones"] += item.amount;
                    break;
                case "wood":
                    _dataBase.PlayerDB[player.userID].ResourcesDB.ResourceElements["Wood"] += item.amount;
                    break;
                case "metal.ore":
                    _dataBase.PlayerDB[player.userID].ResourcesDB.ResourceElements["MetalOre"] += item.amount;
                    break;
                case "sulfur.ore":
                    _dataBase.PlayerDB[player.userID].ResourcesDB.ResourceElements["SulfurOre"] += item.amount;
                    break;
            }

            _dataBase.PlayerDB[player.userID].ResourcesDB.ResourceElements["General"] += item.amount;
        }

        // ReSharper disable once UnusedMember.Local
        private void Unload()
        {
            SaveData();
        }

        #endregion

        #region [DataBase] / [База данных]

        public class StoredData
        {
            public readonly Dictionary<ulong, PlayerData> PlayerDB = new Dictionary<ulong, PlayerData>();

            public class PlayerData
            {
                public string Name;
                public ulong SteamID;
                public readonly Kills KillsDB = new Kills();
                public readonly Deaths DeathsDB = new Deaths();
                public readonly ActiveTime ActiveTimeDB = new ActiveTime();
                public readonly Resources ResourcesDB = new Resources();
                public readonly Animals AnimalDB = new Animals();
                public readonly Entities EntitiesDB = new Entities();

                public class Kills
                {
                    public readonly Dictionary<string, int> KillElements = new Dictionary<string, int>
                    {
                        ["General"] = 0,
                        ["Firearms"] = 0,
                        ["Coldsteel"] = 0,
                        ["Bow"] = 0,
                        ["Headshot"] = 0,
                        ["Wounded"] = 0,
                        ["Sleeping"] = 0
                    };
                }

                public class Deaths
                {
                    public int Death;
                }

                public class ActiveTime
                {
                    public float Seconds;
                }

                public class Resources
                {
                    public readonly Dictionary<string, int> ResourceElements = new Dictionary<string, int>
                    {
                        ["General"] = 0,
                        ["Wood"] = 0,
                        ["Stones"] = 0,
                        ["MetalOre"] = 0,
                        ["SulfurOre"] = 0
                    };
                }

                public class Animals
                {
                    public readonly Dictionary<string, int> AnimalElements = new Dictionary<string, int>
                    {
                        ["General"] = 0,
                        ["Bear"] = 0,
                        ["Boar"] = 0,
                        ["Chicken"] = 0,
                        ["Stag"] = 0,
                        ["Horse"] = 0,
                        ["Wolf"] = 0
                    };
                }

                public class Entities
                {
                    public readonly Dictionary<string, int> EntityElements = new Dictionary<string, int>
                    {
                        ["General"] = 0,
                        ["Constructions"] = 0,
                        ["Helicopter"] = 0,
                        ["BradleyApc"] = 0
                    };
                }
            }
        }

        #endregion

        #region [Helpers] / [Вспомогательные методы]

        private bool VerificationInstall()
        {
            var zealStatistics = plugins.Find("ImageLibrary");
            if (zealStatistics != null) return true;
            PrintError("ImageLibrary plugin not installed");
            Interface.Oxide.UnloadPlugin(Name);
            return false;
        }

        private void MathStates(BasePlayer player, int filter)
        {
            var states = _dataBase.PlayerDB.OrderByDescending(u => u.Value.KillsDB.KillElements["General"]);
            switch (filter)
            {
                case 0:
                    states = _dataBase.PlayerDB.OrderByDescending(u => u.Value.KillsDB.KillElements["General"]);
                    break;
                case 1:
                    states = _dataBase.PlayerDB.OrderByDescending(u => u.Value.DeathsDB.Death);
                    break;
                case 2:
                    states = _dataBase.PlayerDB.OrderByDescending(u => u.Value.AnimalDB.AnimalElements["General"]);
                    break;
                case 3:
                    states = _dataBase.PlayerDB.OrderByDescending(u => u.Value.ResourcesDB.ResourceElements["General"]);
                    break;
                case 4:
                    states = _dataBase.PlayerDB.OrderByDescending(u => u.Value.EntitiesDB.EntityElements["General"]);
                    break;
                case 5:
                    states = _dataBase.PlayerDB.OrderByDescending(u => u.Value.ActiveTimeDB.Seconds);
                    break;
            }

            var ui = new CuiElementContainer
            {
                {
                    new CuiPanel
                    {
                        Image = {Color = "0 0 0 0"},
                        RectTransform = {AnchorMin = "0.121875 0.075", AnchorMax = "0.8791667 0.8898147"}
                    },
                    UILayerServer, $"{UILayerServer}_Layer_States"
                }
            };
            var y = 0;

            var numbers = new Dictionary<int, string>
            {
                [0] = "#ffd700",
                [1] = "#c0c0c0",
                [2] = "#cd7f32",
                [3] = "#000000",
                [4] = "#000000",
                [5] = "#000000",
                [6] = "#000000",
                [7] = "#000000",
                [8] = "#000000",
                [9] = "#000000"
            };
            foreach (var state in states.Take(10))
            {
                var layer = $"State{y}_BG";
                ui.Add(new CuiElement
                {
                    Name = $"State{y}_BG",
                    Parent = $"{UILayerServer}_Layer_States",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#00000051"),
                            FadeIn = 0.1f * y
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.0034387} {0.9005543 - (y * 0.1)}",
                            AnchorMax = $"{0.9965612} {0.9944568 - (y * 0.1)}"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"State{y}_Number",
                    Parent = layer,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat($"{numbers[y]}8D"),
                            Material = Sharp,
                            FadeIn = 0.1f * y
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "-0.01869806 0.01",
                            AnchorMax = "-0.001 0.98"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"State{y}_NumberValue",
                    Parent = $"State{y}_Number",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 18,
                            Text = $"{y + 1}",
                            FadeIn = 0.1f * y
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0.98 1"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#000000AE"),
                            Distance = "0.5 0.5"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"State{y}_Avatar",
                    Parent = layer,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = (string) ImageLibrary.Call("GetImage", $"{state.Key}"),
                            FadeIn = 0.1f * y
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0.05713291 0.98"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"State{y}_Name",
                    Parent = layer,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 18,
                            Text = state.Value.Name,
                            FadeIn = 0.1f * y
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.07132962 0",
                            AnchorMax = "0.1945983 1"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"State{y}_Kills",
                    Parent = layer,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 18,
                            Text = $"{state.Value.KillsDB.KillElements["General"]}",
                            FadeIn = 0.1f * y
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.2361492 0",
                            AnchorMax = "0.2948051 1"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"State{y}_Deaths",
                    Parent = layer,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 18,
                            Text = $"{state.Value.DeathsDB.Death}",
                            FadeIn = 0.1f * y
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.3698049 0",
                            AnchorMax = "0.4284606 1"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"State{y}_Animals",
                    Parent = layer,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 18,
                            Text = $"{state.Value.AnimalDB.AnimalElements["General"]}",
                            FadeIn = 0.1f * y
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.501383 0",
                            AnchorMax = "0.5600362 1"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"State{y}_Gather",
                    Parent = layer,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 18,
                            Text = $"{state.Value.ResourcesDB.ResourceElements["General"]}",
                            FadeIn = 0.1f * y
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.6 0",
                            AnchorMax = "0.7 1"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"State{y}_Entities",
                    Parent = layer,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 18,
                            Text = $"{state.Value.EntitiesDB.EntityElements["General"]}",
                            FadeIn = 0.1f * y
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.7714761 0",
                            AnchorMax = "0.8301293 1"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"State{y}_Time",
                    Parent = layer,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 18,
                            Text =
                                $"{Convert.ToInt32(TimeSpan.FromSeconds(state.Value.ActiveTimeDB.Seconds).TotalHours)} H",
                            FadeIn = 0.1f * y
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.9044451 0",
                            AnchorMax = "0.9630982 1"
                        }
                    }
                });
                y++;
            }

            CuiHelper.DestroyUi(player, $"{UILayerServer}_Layer_States");
            CuiHelper.AddUi(player, ui);
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

        private IEnumerator CheckAllInDataBase()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CheckInDataBase(player);
                yield return new WaitForSeconds(0.1f);
            }

            yield return 0;
        }

        private void Backup_DataBase()
        {
            Interface.Oxide.DataFileSystem.WriteObject(
                $"ZealStatistics_Backup/ZealStatisticsReborn[{DateTime.Today.ToString("d").Replace("/", "_")}]",
                _dataBase.PlayerDB);
        }

        private static bool IsNpc(BasePlayer player)
        {
            if (player.IsNpc) return true;
            if (!player.userID.IsSteamId()) return true;
            return player is NPCPlayer;
        }

        private void CheckInDataBase(BasePlayer player)
        {
            if (_dataBase.PlayerDB.ContainsKey(player.userID)) return;
            if (IsNpc(player)) return;
            _dataBase.PlayerDB.Add(player.userID, new StoredData.PlayerData
            {
                Name = player.displayName,
                SteamID = player.userID
            });
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _dataBase);

        private void LoadData()
        {
            try
            {
                _dataBase = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch (Exception)
            {
                _dataBase = new StoredData();
            }
        }

        #endregion

        #region [API]

        [HookMethod("GetKillsPlayer")]
        public int GetKillsPlayer(ulong userid)
        {
            return !_dataBase.PlayerDB.ContainsKey(userid)
                ? 0
                : _dataBase.PlayerDB[userid].KillsDB.KillElements["General"];
        }

        [HookMethod("GetDeathsPlayer")]
        public int GetDeathsPlayer(ulong userid)
        {
            return !_dataBase.PlayerDB.ContainsKey(userid) ? 0 : _dataBase.PlayerDB[userid].DeathsDB.Death;
        }

        [HookMethod("GetPlayingTimePlayer")]
        public float GetPlayingTimePlayer(ulong userid)
        {
            if (!_dataBase.PlayerDB.ContainsKey(userid)) return 0;
            return _dataBase.PlayerDB[userid].ActiveTimeDB.Seconds;
        }

        #endregion
    }
}