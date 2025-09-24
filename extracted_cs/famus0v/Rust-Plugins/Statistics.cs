using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Facepunch.Extend;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Statistics", "Luminos", "1.0.5")]
    [Description("Interface for displaying game statistics")]
    public class Statistics : RustPlugin
    {
        #region [Reference]

        [PluginReference] private Plugin ImageLibrary;

        #endregion

        #region [Vars]

        private Data _dataBase = new Data();
        private const string UIMain = "UI.Statistic.Main";
        private const string UIMenu = "UI.Statistic.Main.Menu";
        private const string Regular = "robotocondensed-regular.ttf";
        private readonly WaitForSeconds _waitForSeconds = new WaitForSeconds(0.1f);

        private static readonly Dictionary<int, string> SortBy = new Dictionary<int, string>
        {
            [0] = "POINTS",
            [1] = "KILLS",
            [2] = "DEATHS",
            [3] = "SULFUR ORE",
            [4] = "METAL ORE",
            [5] = "WOOD",
            [6] = "STONES",
            [7] = "HQ METAL ORE",
            [8] = "BEAR",
            [9] = "BOAR",
            [10] = "STAG",
            [11] = "WOLF",
            [12] = "CHICKEN",
            [13] = "GAME TIME"
        };

        private static readonly Dictionary<string, string> MenuButtons = new Dictionary<string, string>
        {
            ["Main"] = "stats",
            ["Top"] = "stats.top"
        };

        private readonly Dictionary<string, string> _images = new Dictionary<string, string>
        {
            ["UI.Menu.Background"] = "https://i.imgur.com/9xAJphN.png",
            ["UI.Menu.Avatar.Frame"] = "https://i.imgur.com/K0MFjao.png",
            ["UI.Menu.Icon.Main"] = "https://i.imgur.com/oUWqzMC.png",
            ["UI.Menu.Icon.Top"] = "https://i.imgur.com/ntdJQLn.png",
            ["UI.Main.PlayerStatistic.Background"] = "https://i.imgur.com/eqMHPuA.png",
            ["UI.Main.PlayerStatistic.Avatar.Frame"] = "https://i.imgur.com/9bR9yYf.png",
            ["UI.Main.PlayerStatistic.StatisticElement.Background"] = "https://i.imgur.com/y92XAhR.png",
            ["UI.Main.Top.TopElement.Background"] = "https://i.imgur.com/I1vKiBV.png",
            ["UI.Main.Top.Filter.Background"] = "https://i.imgur.com/iI4Wk6M.png",
            ["UI.Main.Top.FilterElement.Background"] = "https://i.imgur.com/i2LrwXB.png",
            ["Boar"] = "https://i.imgur.com/lLo9fIv.png",
            ["Bear"] = "https://i.imgur.com/FQ72uOg.png",
            ["Chicken"] = "https://i.imgur.com/U2isPVm.png",
            ["Stag"] = "https://i.imgur.com/9WdFqat.png",
            ["Wolf"] = "https://i.imgur.com/lO0CD85.png",
            ["sulfur.ore"] = "https://rustlabs.com/img/items180/sulfur.ore.png",
            ["metal.ore"] = "https://rustlabs.com/img/items180/metal.ore.png",
            ["wood"] = "https://rustlabs.com/img/items180/wood.png",
            ["stones"] = "https://rustlabs.com/img/items180/stones.png",
            ["hq.metal.ore"] = "https://rustlabs.com/img/items180/hq.metal.ore.png"
        };

        private static readonly Dictionary<string, string> Resources = new Dictionary<string, string>
        {
            ["sulfur.ore"] = "SULFUR ORE",
            ["metal.ore"] = "METAL ORE",
            ["wood"] = "WOOD",
            ["stones"] = "STONES",
            ["hq.metal.ore"] = "HQ METAL ORE"
        };

        private static readonly Dictionary<string, string> Animals = new Dictionary<string, string>
        {
            ["Boar"] = "BOAR",
            ["Bear"] = "BEAR",
            ["Chicken"] = "CHICKEN",
            ["Stag"] = "STAG",
            ["Wolf"] = "WOLF"
        };

        #endregion

        #region [Configuration]

        private static ConfigData _config;

        public class ConfigData
        {
            [JsonProperty(PropertyName = "Statistics [Configuration]")]
            public StatisticsConfig StatisticsCfg = new StatisticsConfig();

            public class StatisticsConfig
            {
                [JsonProperty(PropertyName = "Point Coefficient")]
                public Dictionary<string, float> Pointcoefficient;
            }
        }

        private static ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                StatisticsCfg = new ConfigData.StatisticsConfig
                {
                    Pointcoefficient = new Dictionary<string, float>
                    {
                        ["Kills"] = 1,
                        ["Deaths"] = 1,
                        ["Mined"] = 1,
                        ["KillAnimal"] = 1,
                        ["GameTime"] = 1
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

        #region [UI]

        private void OpenUI_Main(BasePlayer player)
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            var ui = new CuiElementContainer();

            ui.Add(new CuiPanel
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
            }, "Overlay", UIMain);

            ui.Add(new CuiElement
            {
                Name = UIMenu,
                Parent = UIMain,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage("UI.Menu.Background")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "0.052 1"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.ServerName",
                Parent = UIMain,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 20,
                        Text = ConVar.Server.hostname.ToUpper(),
                        Color = "0.24 0.24 0.25 1.00"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.05208334 0.93",
                        AnchorMax = "1 0.9981343"
                    }
                }
            });

            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.AddUi(player, ui);
            OpenUI_Menu(player);
            OpenUI_PlayerStatistic(player);
        }

        private void OpenUI_Menu(BasePlayer player)
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            var ui = new CuiElementContainer();

            #region [Avatar]

            ui.Add(new CuiElement
            {
                Name = $"{UIMenu}.Avatar",
                Parent = UIMenu,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage(player.UserIDString)
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1300005 0.912963",
                        AnchorMax = "0.8300004 0.9777778"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMenu}.Avatar.Frame",
                Parent = $"{UIMenu}.Avatar",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage("UI.Menu.Avatar.Frame")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "-0.015 -0.015",
                        AnchorMax = "1.015 1.015"
                    }
                }
            });

            var y = 0;
            foreach (var button in MenuButtons)
            {
                ui.Add(new CuiElement
                {
                    Name = $"{UIMenu}.Button.{button.Key}",
                    Parent = $"{UIMenu}",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0.20 0.20 0.22 1.00"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0 {0.5064823 - (y * 0.11)}",
                            AnchorMax = $"0.98 {0.599072 - (y * 0.11)}"
                        },
                        new CuiOutlineComponent
                        {
                            Distance = "0 2",
                            Color = "0.39 0.40 0.44 0.70"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UIMenu}.Button.{button.Key}.Icon.{button.Key}",
                    Parent = $"{UIMenu}.Button.{button.Key}",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage($"UI.Menu.Icon.{button.Key}"),
                            Color = "0.39 0.40 0.44 1.00"
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
                        Color = "0 0 0 0",
                        Command = $"chat.say /{button.Value}"
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
                }, $"{UIMenu}.Button.{button.Key}.Icon.{button.Key}", $"{UIMenu}.Button.Command.{button.Key}");
                y++;
            }

            ui.Add(new CuiButton
            {
                Button =
                {
                    Color = "0.79 0.09 0.09 0.60",
                    Sprite = "assets/icons/occupied.png",
                    Close = UIMain
                },
                Text =
                {
                    Text = " "
                },
                RectTransform =
                {
                    AnchorMin = "0.1700002 0.01388893",
                    AnchorMax = "0.8100001 0.07314816"
                }
            }, UIMenu, $"{UIMenu}.Menu.Button.Close");

            #endregion

            CuiHelper.AddUi(player, ui);
        }

        private void OpenUI_PlayerStatistic(BasePlayer player)
        {
            var ui = new CuiElementContainer();

            var parent = $"{UIMain}.PlayerStatistics.Background";
            ui.Add(new CuiElement
            {
                Name = parent,
                Parent = UIMain,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage("UI.Main.PlayerStatistic.Background")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1651042 0.7629614",
                        AnchorMax = "0.8822339 0.92685"
                    }
                }
            });


            #region [Avatar]

            ui.Add(new CuiElement
            {
                Name = $"{parent}.Avatar",
                Parent = parent,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage(player.UserIDString)
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.02541872 0.1581946",
                        AnchorMax = "0.1149118 0.854366"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{parent}.Avatar.Frame",
                Parent = $"{parent}.Avatar",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage("UI.Main.PlayerStatistic.Avatar.Frame")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "-0.005 -0.005",
                        AnchorMax = "0.995 0.995"
                    }
                }
            });

            #endregion

            #region [Info]

            var playerData = _dataBase.PlayerDatas[player.userID];

            ui.Add(new CuiElement
            {
                Name = $"{parent}.UserName",
                Parent = parent,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 10,
                        Font = Regular,
                        Text = "<color=#555860>USER NAME</color>\n" +
                               "<b><color=#63666f><size=16>" +
                               $"{player.displayName.ToUpper()}" +
                               "</size></color></b>",
                        FadeIn = 0.5f
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1292769 0.5875736",
                        AnchorMax = "0.3507907 0.8713152"
                    }
                }
            });

            var gameTime = TimeSpan.FromSeconds(playerData.GameTime + player.Connection.GetSecondsConnected());

            ui.Add(new CuiElement
            {
                Name = $"{parent}.GameTime",
                Parent = parent,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 10,
                        Font = Regular,
                        Text = "<color=#555860>GAME TIME ON SERVER</color>\n" +
                               "<b><color=#63666f>" +
                               $"<size=18>{gameTime.Days}</size>    DAYS    " +
                               $"<size=18>{gameTime.Hours}</size>    HOURS    " +
                               $"<size=18>{gameTime.Minutes}</size>    MINUTS" +
                               "</color></b>",
                        FadeIn = 0.5f
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1292769 0.1751438",
                        AnchorMax = "0.3507907 0.4588866"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{parent}.Kills",
                Parent = parent,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 10,
                        Font = Regular,
                        Text = "<color=#555860>KILLS</color>\n" +
                               "<b><color=#63666f><size=18>" +
                               $"{playerData.Kills}" +
                               "</size></color></b>",
                        FadeIn = 0.5f
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.3711265 0.5875736",
                        AnchorMax = "0.43867 0.8713152"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{parent}.Deaths",
                Parent = parent,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 10,
                        Font = Regular,
                        Text = "<color=#555860>DEATHS</color>\n" +
                               "<b><color=#63666f><size=18>" +
                               $"{playerData.Deaths}" +
                               "</size></color></b>",
                        FadeIn = 0.5f
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.3711265 0.1751438",
                        AnchorMax = "0.43867 0.4588867"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{parent}.FavoriteWeapon",
                Parent = parent,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 10,
                        Font = Regular,
                        Text = "<color=#555860>FAVORITE WEAPON</color>\n" +
                               "<b><color=#63666f><size=18>" +
                               $"{ItemManager.FindItemDefinition(GetFavoriteWeapon(player.userID)).displayName.english}" +
                               "</size></color></b>",
                        FadeIn = 0.5f
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.4582804 0.5875736",
                        AnchorMax = "0.600629 0.8713152"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{parent}.TopPlace",
                Parent = parent,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 10,
                        Font = Regular,
                        Text = "<color=#555860>SERVER TOP</color>\n" +
                               "<b><color=#63666f><size=18>" +
                               $"{GetTopPlace(player.userID)} PLACE" +
                               "</size></color></b>",
                        FadeIn = 0.5f
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.4582804 0.1751437",
                        AnchorMax = "0.600629 0.4588866"
                    }
                }
            });

            #endregion

            CuiHelper.AddUi(player, ui);
        }

        private void OpenUI_Statistic(BasePlayer player)
        {
            var ui = new CuiElementContainer();
            if (!IsValidPlayer(player)) return;
            InDataBase(player.userID);
            var playerData = _dataBase.PlayerDatas[player.userID];

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.MinedResource",
                Parent = UIMain,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 20,
                        Text = "MINED RESOURCES",
                        Color = "0.24 0.24 0.25 1.00",
                        FadeIn = 0.5f
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.05208334 0.6842549",
                        AnchorMax = "1 0.7388828"
                    }
                }
            });

            var xr = 0;
            foreach (var resource in Resources)
            {
                var parent = $"{UIMain}.PlayerStatistic.Resource.StatisticElement.{xr}";
                ui.Add(new CuiElement
                {
                    Name = parent,
                    Parent = UIMain,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage("UI.Main.PlayerStatistic.StatisticElement.Background"),
                            FadeIn = 0.5f + (0.1f * xr)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.1651042 + (xr * 0.15371225)} 0.4546284",
                            AnchorMax = $"{0.2671876 + (xr * 0.15371225)} 0.6607994"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{parent}.Icon",
                    Parent = parent,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage(resource.Key),
                            FadeIn = 0.5f + (0.1f * xr)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.3214284 0.5568972",
                            AnchorMax = "0.6785716 0.8712713"
                        }
                    }
                });

                if (xr <= 3)
                {
                    ui.Add(new CuiElement
                    {
                        Name = $"{parent}.Point.Resource.{xr}",
                        Parent = UIMain,
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Align = TextAnchor.MiddleCenter,
                                FontSize = 35,
                                Color = "0.25 0.25 0.26 1.00",
                                Text = "•",
                                FadeIn = 0.5f + (0.1f * xr)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0.2671875 + (xr * 0.1526272333333333)} 0.4546284",
                                AnchorMax = $"{0.32103 + (xr * 0.1526272333333333)} 0.6607994"
                            }
                        }
                    });
                }

                ui.Add(new CuiElement
                {
                    Name = $"{parent}.TopPlace",
                    Parent = parent,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 16,
                            Color = "0.33 0.35 0.38 1.00",
                            Text = $"{resource.Value}\n\n{playerData.MinedResources[resource.Key]}",
                            FadeIn = 0.5f + (0.1f * xr)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.015",
                            AnchorMax = "1 0.480549"
                        }
                    }
                });

                xr++;
            }

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.KillingAnimals",
                Parent = UIMain,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 20,
                        Text = "KILLING ANIMALS",
                        Color = "0.24 0.24 0.25 1.00",
                        FadeIn = 0.5f
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.05208334 0.3750007",
                        AnchorMax = "1 0.4296287"
                    }
                }
            });

            var xa = 0;
            foreach (var animal in Animals)
            {
                var parent = $"{UIMain}.PlayerStatistic.Animals.StatisticElement.{xa}";

                ui.Add(new CuiElement
                {
                    Name = parent,
                    Parent = UIMain,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage("UI.Main.PlayerStatistic.StatisticElement.Background"),
                            FadeIn = 0.5f + (0.1f * xr)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.1651042 + (xa * 0.15371225)} 0.1462943",
                            AnchorMax = $"{0.2671876 + (xa * 0.15371225)} 0.3524704"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{parent}.Icon",
                    Parent = parent,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage(animal.Key),
                            Color = "0.39 0.40 0.44 1.00",
                            FadeIn = 0.5f + (0.1f * xr)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.3214284 0.5568972",
                            AnchorMax = "0.6785716 0.8712713"
                        }
                    }
                });

                if (xa <= 3)
                {
                    ui.Add(new CuiElement
                    {
                        Name = $"{parent}.Point.Animals.{xa}",
                        Parent = UIMain,
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Align = TextAnchor.MiddleCenter,
                                FontSize = 35,
                                Color = "0.25 0.25 0.26 1.00",
                                Text = "•",
                                FadeIn = 0.5f + (0.1f * xr)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0.2671875 + (xa * 0.1526272333333333)} 0.1462943",
                                AnchorMax = $"{0.32103 + (xa * 0.1526272333333333)} 0.3524704"
                            }
                        }
                    });
                }

                ui.Add(new CuiElement
                {
                    Name = $"{parent}.TopPlace",
                    Parent = parent,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 16,
                            Color = "0.33 0.35 0.38 1.00",
                            Text = $"{animal.Value}\n\n{playerData.KilledAnimals[animal.Key]}",
                            FadeIn = 0.5f + (0.1f * xr)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.015",
                            AnchorMax = "1 0.480549"
                        }
                    }
                });

                xa++;
            }

            CuiHelper.AddUi(player, ui);
        }

        private void OpenUI_TopStatistic(BasePlayer player, int filter)
        {
            var ui = new CuiElementContainer();

            int num = 0, y = 0;
            foreach (var obj in GetSortedData(filter).Take(5))
            {
                var playerData = _dataBase.PlayerDatas[obj.Key];
                var objInfo = covalence.Players.FindPlayerById(obj.Key.ToString());
                if (objInfo == null) continue;
                var parent = $"{UIMain}.Statistic.Top.{num}";

                ui.Add(new CuiElement
                {
                    Name = $"{UIMain}.Statistic.Top.{num}",
                    Parent = UIMain,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage("UI.Main.Top.TopElement.Background")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0.1651042 {0.6370386 - (y * 0.1271)}",
                            AnchorMax = $"0.619676 {0.7450764 - (y * 0.1271)}"
                        }
                    }
                });

                #region [Avatar]

                ui.Add(new CuiElement
                {
                    Name = $"{parent}.Avatar",
                    Parent = parent,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage(obj.Key.ToString())
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.06759879 0.1581946",
                            AnchorMax = "0.1581946 0.854366"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{parent}.Avatar.Frame",
                    Parent = $"{parent}.Avatar",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage("UI.Main.PlayerStatistic.Avatar.Frame")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "-0.005 -0.005",
                            AnchorMax = "1.005 1.005"
                        }
                    }
                });

                #endregion

                #region [Info]

                ui.Add(new CuiElement
                {
                    Name = $"{parent}.UserName",
                    Parent = parent,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            FontSize = 8,
                            Font = Regular,
                            Text = "<color=#555860>USER NAME</color>\n" +
                                   "<b><color=#63666f><size=12>" +
                                   $"{objInfo.Name.ToUpper()}" +
                                   "</size></color></b>",
                            FadeIn = 0.5f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.172816 0.5875736",
                            AnchorMax = "0.5522596 0.8713152"
                        }
                    }
                });

                var gameTime = TimeSpan.FromSeconds(playerData.GameTime);

                ui.Add(new CuiElement
                {
                    Name = $"{parent}.GameTime",
                    Parent = parent,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            FontSize = 8,
                            Font = Regular,
                            Text = "<color=#555860>GAME TIME ON SERVER</color>\n" +
                                   "<b><color=#63666f>" +
                                   $"<size=12>{gameTime.Days}</size>    DAYS    " +
                                   $"<size=12>{gameTime.Hours}</size>    HOURS    " +
                                   $"<size=12>{gameTime.Minutes}</size>    MINUTS" +
                                   "</color></b>",
                            FadeIn = 0.5f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.172816 0.1751438",
                            AnchorMax = "0.5522596 0.4588866"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{parent}.Points",
                    Parent = parent,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 30,
                            Text = obj.Value.Points.ToString(CultureInfo.InvariantCulture),
                            FadeIn = 0.5f,
                            Color = "0.39 0.40 0.44 1.00"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5556969 0",
                            AnchorMax = "1 1"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{parent}.Place.{num}",
                    Parent = parent,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 20,
                            Text = $"#{num + 1}",
                            FadeIn = 0.5f,
                            Color = "0.39 0.40 0.44 1.00"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0.06 1"
                        }
                    }
                });

                #endregion

                num++;
                y++;
            }

            CuiHelper.AddUi(player, ui);
        }

        private void OpenUI_Filters(BasePlayer player)
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            var ui = new CuiElementContainer();

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Top.Filter.Background",
                Parent = UIMain,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage("UI.Main.Top.Filter.Background"),
                        FadeIn = 0.1f
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.6291667 0.1296297",
                        AnchorMax = "0.8792246 0.7454598"
                    }
                }
            });

            int num = 0, x = 0, y = 0;
            foreach (var filter in SortBy)
            {
                var parent = $"{UIMain}.Top.Filter.Parent.{num}";

                if (x >= 2)
                {
                    x = 0;
                    y++;
                }

                ui.Add(new CuiElement
                {
                    Name = parent,
                    Parent = $"{UIMain}.Top.Filter.Background",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage("UI.Main.Top.FilterElement.Background"),
                            FadeIn = 0.1f + (num * 0.1f)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.07289934 + (x * 0.47)} {0.8074015 - (y * 0.1)}",
                            AnchorMax = $"{0.4848388 + (x * 0.47)} {0.8775646 - (y * 0.1)}"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{parent}.Name",
                    Parent = parent,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 14,
                            Text = filter.Value,
                            Color = "0.39 0.40 0.44 1.00",
                            FadeIn = 0.1f + (num * 0.1f)
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
                        Color = "0 0 0 0",
                        Command = $"stats.filter {filter.Key}"
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
                }, $"{parent}.Name");

                x++;
                num++;
            }

            CuiHelper.AddUi(player, ui);
        }

        #endregion

        #region [Hooks]

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            if (ImageLibrary == null)
            {
                PrintError("ImageLibrary not loaded");
                timer.Once(5f, () => Interface.Oxide.UnloadPlugin(Name));
                return;
            }

            LoadData();
            ServerMgr.Instance.StartCoroutine(LoadImages());
            cmd.AddChatCommand("stats", this, "OpenStatistic");
            cmd.AddChatCommand("stats.top", this, "OpenTopStatistic");
            cmd.AddConsoleCommand("stats.filter", this, "Filter");
        }

        // ReSharper disable once UnusedMember.Local
        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null) return;
            if (info == null) return;
            if (entity is BasePlayer)
            {
                var initiated = entity.ToPlayer();
                var initiator = info.InitiatorPlayer;
                if (!IsValidPlayer(initiated)) return;
                if (IsNpc(initiated)) return;
                InDataBase(initiated.userID);
                _dataBase.PlayerDatas[initiated.userID].Deaths++;
                if (!IsValidPlayer(initiator)) return;
                if (IsNpc(initiator)) return;
                InDataBase(initiator.userID);
                var initiatorData = _dataBase.PlayerDatas[initiator.userID];
                initiatorData.Kills++;
                initiatorData.Points += _config.StatisticsCfg.Pointcoefficient["Kills"];
            }

            if (info.InitiatorPlayer != null)
            {
                var initiator = info.InitiatorPlayer;
                if (IsNpc(initiator)) return;
                InDataBase(initiator.userID);
                var weapon = info.Weapon?.GetItem();
                if (weapon != null) AddFavoriteWeapon(weapon.info.shortname, initiator.userID);
                var playerData = _dataBase.PlayerDatas[initiator.userID];
                switch (entity.ShortPrefabName)
                {
                    case "bear":
                        _dataBase.PlayerDatas[initiator.userID].KilledAnimals["Bear"]++;
                        playerData.Points += _config.StatisticsCfg.Pointcoefficient["KillAnimal"];
                        break;
                    case "boar":
                        _dataBase.PlayerDatas[initiator.userID].KilledAnimals["Boar"]++;
                        playerData.Points += _config.StatisticsCfg.Pointcoefficient["KillAnimal"];
                        break;
                    case "chicken":
                        _dataBase.PlayerDatas[initiator.userID].KilledAnimals["Chicken"]++;
                        playerData.Points += _config.StatisticsCfg.Pointcoefficient["KillAnimal"];
                        break;
                    case "stag":
                        _dataBase.PlayerDatas[initiator.userID].KilledAnimals["Stag"]++;
                        playerData.Points += _config.StatisticsCfg.Pointcoefficient["KillAnimal"];
                        break;
                    case "wolf":
                        _dataBase.PlayerDatas[initiator.userID].KilledAnimals["Wolf"]++;
                        playerData.Points += _config.StatisticsCfg.Pointcoefficient["KillAnimal"];
                        break;
                }

                SaveData();
            }
        }

        // ReSharper disable once UnusedParameter.Local
        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (entity == null) return;
            var player = entity.ToPlayer();
            if (!IsValidPlayer(player)) return;
            InDataBase(player.userID);
            if (!_dataBase.PlayerDatas[player.userID].MinedResources.ContainsKey(item.info.shortname)) return;
            var playerData = _dataBase.PlayerDatas[player.userID];
            playerData.MinedResources[item.info.shortname] += item.amount;
            playerData.Points = item.amount * _config.StatisticsCfg.Pointcoefficient["Mined"];
        }

        // ReSharper disable once UnusedMember.Local
        private void OnDispenserBonus(ResourceDispenser dispenser, BaseEntity entity, Item item) =>
            OnDispenserGather(dispenser, entity, item);

        private void OnCollectiblePickup(Item item, BasePlayer player)
        {
            if (player == null) return;
            if (!IsValidPlayer(player)) return;
            InDataBase(player.userID);
            if (!_dataBase.PlayerDatas[player.userID].MinedResources.ContainsKey(item.info.shortname)) return;
            var playerData = _dataBase.PlayerDatas[player.userID];
            playerData.MinedResources[item.info.shortname] += item.amount;
            playerData.Points = item.amount * _config.StatisticsCfg.Pointcoefficient["Mined"];
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            InDataBase(player.userID);
            if (player.Connection == null) return;
            _dataBase.PlayerDatas[player.userID].GameTime += player.Connection.GetSecondsConnected();
        }

        // ReSharper disable once UnusedMember.Local
        private void Unload()
        {
            SaveData();
        }

        #endregion

        #region [Commands]

        // ReSharper disable once UnusedMember.Local
        private void OpenStatistic(BasePlayer player)
        {
            InDataBase(player.userID);
            OpenUI_Main(player);
            OpenUI_PlayerStatistic(player);
            OpenUI_Statistic(player);
        }

        // ReSharper disable once UnusedMember.Local
        private void OpenTopStatistic(BasePlayer player)
        {
            InDataBase(player.userID);
            OpenUI_Main(player);
            OpenUI_TopStatistic(player, 0);
            OpenUI_Filters(player);
        }

        // ReSharper disable once UnusedMember.Local
        private void Filter(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            InDataBase(player.userID);
            var filter = args.Args[0].ToInt();
            OpenUI_Main(player);
            OpenUI_TopStatistic(player, filter);
            OpenUI_Filters(player);
        }

        [ConsoleCommand("statistic.close")]
        // ReSharper disable once UnusedMember.Local
        private void CloseStatistic(ConsoleSystem.Arg args)
        {
            CuiHelper.DestroyUi(args.Player(), UIMain);
        }

        #endregion

        #region [DataBase]

        public class Data
        {
            public readonly Dictionary<ulong, PlayerData> PlayerDatas = new Dictionary<ulong, PlayerData>();

            // ReSharper disable once ClassNeverInstantiated.Global
            public class PlayerData
            {
                public int Kills;
                public int Deaths;
                public int TopPlace;
                public float Points;
                public float GameTime;

                public readonly Dictionary<string, int> FavoriteWeapon = new Dictionary<string, int>();

                public readonly Dictionary<string, int> MinedResources = new Dictionary<string, int>
                {
                    ["sulfur.ore"] = 0,
                    ["metal.ore"] = 0,
                    ["wood"] = 0,
                    ["stones"] = 0,
                    ["hq.metal.ore"] = 0
                };

                public readonly Dictionary<string, int> KilledAnimals = new Dictionary<string, int>
                {
                    ["Boar"] = 0,
                    ["Bear"] = 0,
                    ["Chicken"] = 0,
                    ["Stag"] = 0,
                    ["Wolf"] = 0
                };
            }
        }

        #endregion

        #region [Helpers]

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _dataBase);
        
        private void LoadData()
        {
            try
            {
                _dataBase = Interface.GetMod().DataFileSystem.ReadObject<Data>(Name);
                ServerMgr.Instance.StartCoroutine(SortData());
            }
            catch (Exception)
            {
                _dataBase = new Data();
            }
        }

        private string GetImage(string image)
        {
            return (string) ImageLibrary.Call("GetImage", image);
        }

        private IEnumerator LoadImages()
        {
            PrintWarning("Load images...");
            // ReSharper disable once UseDeconstruction
            foreach (var image in _images)
            {
                ImageLibrary.Call("AddImage", image.Value, image.Key);
                yield return _waitForSeconds;
            }

            PrintWarning("Image loaded");
            yield return 0;
        }

        private static bool IsNpc(BasePlayer player)
        {
            if (player.IsNpc) return true;
            if (!player.userID.IsSteamId()) return true;
            return player is NPCPlayer;
        }

        private static bool IsValidPlayer(BasePlayer player)
        {
            return player != null && player.IsValid();
        }

        private void InDataBase(ulong userId)
        {
            if (_dataBase.PlayerDatas.ContainsKey(userId)) return;
            _dataBase.PlayerDatas.Add(userId, new Data.PlayerData());
        }

        private void AddFavoriteWeapon(string shortname, ulong userId)
        {
            InDataBase(userId);
            var playerData = _dataBase.PlayerDatas[userId].FavoriteWeapon;
            if (playerData.ContainsKey(shortname))
                playerData[shortname]++;
            else
                playerData.Add(shortname, 1);
        }

        private string GetFavoriteWeapon(ulong userId)
        {
            InDataBase(userId);
            var db = _dataBase.PlayerDatas[userId].FavoriteWeapon;
            return db.Count == 0
                ? "rifle.ak"
                : _dataBase.PlayerDatas[userId].FavoriteWeapon.OrderByDescending(k => k.Value).ToList()[0].Key;
        }

        private int GetTopPlace(ulong userId)
        {
            InDataBase(userId);
            return _dataBase.PlayerDatas[userId].TopPlace;
        }

        private IEnumerable<KeyValuePair<ulong, Data.PlayerData>> GetSortedData(int filter)
        {
            IEnumerable<KeyValuePair<ulong, Data.PlayerData>> data;

            switch (filter)
            {
                case 0:
                    data = _dataBase.PlayerDatas.OrderByDescending(p => p.Value.Points);
                    break;
                case 1:
                    data = _dataBase.PlayerDatas.OrderByDescending(p => p.Value.Kills);
                    break;
                case 2:
                    data = _dataBase.PlayerDatas.OrderByDescending(p => p.Value.Deaths);
                    break;
                case 3:
                    data = _dataBase.PlayerDatas.OrderByDescending(p => p.Value.MinedResources["sulfur.ore"]);
                    break;
                case 4:
                    data = _dataBase.PlayerDatas.OrderByDescending(p => p.Value.MinedResources["metal.ore"]);
                    break;
                case 5:
                    data = _dataBase.PlayerDatas.OrderByDescending(p => p.Value.MinedResources["wood"]);
                    break;
                case 6:
                    data = _dataBase.PlayerDatas.OrderByDescending(p => p.Value.MinedResources["stones"]);
                    break;
                case 7:
                    data = _dataBase.PlayerDatas.OrderByDescending(p => p.Value.MinedResources["hq.metal.ore"]);
                    break;
                case 8:
                    data = _dataBase.PlayerDatas.OrderByDescending(p => p.Value.KilledAnimals["Boar"]);
                    break;
                case 9:
                    data = _dataBase.PlayerDatas.OrderByDescending(p => p.Value.KilledAnimals["Bear"]);
                    break;
                case 10:
                    data = _dataBase.PlayerDatas.OrderByDescending(p => p.Value.KilledAnimals["Stag"]);
                    break;
                case 11:
                    data = _dataBase.PlayerDatas.OrderByDescending(p => p.Value.KilledAnimals["Wolf"]);
                    break;
                case 12:
                    data = _dataBase.PlayerDatas.OrderByDescending(p => p.Value.KilledAnimals["Chicken"]);
                    break;
                case 13:
                    data = _dataBase.PlayerDatas.OrderByDescending(p => p.Value.GameTime);
                    break;
                default:
                    data = _dataBase.PlayerDatas.OrderByDescending(p => p.Value.Points);
                    break;
            }

            return data;
        }

        private IEnumerator SortData()
        {
            var sorted = _dataBase.PlayerDatas.OrderByDescending(p => p.Value.Points);
            var num = 1;
            foreach (var player in sorted)
            {
                player.Value.TopPlace = num;
                num++;
            }

            yield return 0;
        }

        #endregion
    }
}