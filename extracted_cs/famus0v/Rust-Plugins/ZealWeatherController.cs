using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Facepunch.Extend;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ZealWeatherController", "Kira", "1.0.0")]
    public class ZealWeatherController : RustPlugin
    {
        #region [References] / [Ссылки]

        private StoredData DataBase = new StoredData();
        private TOD_Sky Sky;
        private static ZealWeatherController _;

        #endregion 

        #region [Vars] / [Переменные]

        public const string Sharp = "assets/content/ui/ui.background.tile.psd";
        public const string Blur = "assets/content/ui/uibackgroundblur.mat";
        public const string BlurInMenu = "assets/content/ui/uibackgroundblur-ingamemenu.mat";
        public const string Radial = "assets/content/ui/ui.background.transparent.radial.psd";
        public const string Regular = "robotocondensed-regular.ttf";

        public const string UILayer = "UI_Weather_Controller";
        public const string UILayerProfiles = "UI_Weather_Controller_Profiles";

        #endregion

        #region [MonoBehaviours]

        public class WeatherComponent : MonoBehaviour
        {
            private BasePlayer _player;

            private void Awake()
            {
                _player = GetComponent<BasePlayer>();
                DrawUI_Layer();
                DrawUI_WeatherController(true);
                DrawUI_WeatherProfiles(true);
                DrawUI_TimeController();
                InvokeRepeating("Timer", 1, 1);
            }

            private void Timer()
            {
                DrawUI_TimeController();
            }

            public void DrawUI_Layer()
            {
                var UI = new CuiElementContainer();

                UI.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    Image =
                    {
                        Color = HexToRustFormat("#000000A9"),
                        Material = BlurInMenu,
                        FadeIn = 0.1f
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.3322909 0.2212963",
                        AnchorMax = "0.6677073 0.7787038"
                    }
                }, "Hud", UILayer);

                UI.Add(new CuiPanel
                {
                    Image =
                    {
                        Color = HexToRustFormat("#000000A9"),
                        Material = BlurInMenu,
                        FadeIn = 0.1f
                    },
                    RectTransform =
                    {
                        AnchorMin = "-0.4720488 -0.06478405",
                        AnchorMax = "-0.006210029 1"
                    }
                }, UILayer, UILayerProfiles);

                UI.Add(new CuiPanel
                {
                    Image =
                    {
                        Color = "0 0 0 0",
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0.05304214",
                        AnchorMax = "1 0.9375973"
                    }
                }, UILayerProfiles, UILayerProfiles + ".Parent");

                UI.Add(new CuiElement
                {
                    Name = "UpperPanelProfiles",
                    Parent = UILayerProfiles,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#000000CF"),
                            Material = BlurInMenu,
                            FadeIn = 0.1f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.935",
                            AnchorMax = "0.995 0.997"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#8ebf5b"),
                            Distance = "0 0.9"
                        }
                    }
                });

                UI.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 18,
                        Text = "Profiles",
                        FadeIn = 0.1f
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, "UpperPanelProfiles");

                UI.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    Image =
                    {
                        Color = HexToRustFormat("#000000A9"),
                        Material = BlurInMenu,
                        FadeIn = 0.1f
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 -0.06478401",
                        AnchorMax = "0.997 -0.006644491"
                    }
                }, UILayer, UILayer + "TimeController");

                UI.Add(new CuiElement
                {
                    Name = "UpperPanel",
                    Parent = UILayer,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#000000CF"),
                            Material = BlurInMenu,
                            FadeIn = 0.1f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.9335546",
                            AnchorMax = "0.997 1"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#8ebf5b"),
                            Distance = "0 0.9"
                        }
                    }
                });

                UI.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 18,
                        Text = "Zeal Weather Controller",
                        FadeIn = 0.1f
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, "UpperPanel");

                UI.Add(new CuiPanel
                {
                    Image =
                    {
                        Color = "0 0 0 0"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "0.4984498 0.9335544"
                    }
                }, UILayer, UILayer + ".TXTParent");

                int i = 0, y = 0;
                foreach (var weathertype in _.DataBase.WeatherSettings.Select(weather => weather.Value))
                {
                    UI.Add(new CuiLabel
                    {
                        Text =
                        {
                            Align = TextAnchor.MiddleLeft,
                            FontSize = 11,
                            Text = weathertype.Name,
                            FadeIn = 0.1f * i,
                            Font = Regular
                        },
                        RectTransform =
                        {
                            AnchorMin = $"{0.03115251} {0.948305 - (y * 0.026)}",
                            AnchorMax = $"{0.9688475} {0.9822064 - (y * 0.026)}"
                        }
                    }, UILayer + ".TXTParent", $"WeatherName{i}");
                    i++;
                    y++;
                    y++;
                }

                CuiHelper.DestroyUi(_player, UILayer);
                CuiHelper.AddUi(_player, UI);
                DrawUI_WeatherProfiles(false);
            }

            public void DrawUI_WeatherController(bool isUpdate)
            {
                var id = 0;
                foreach (var weather in _.DataBase.WeatherSettings)
                {
                    CuiHelper.DestroyUi(_player, $"WeatherButton+_{id}");
                    CuiHelper.DestroyUi(_player, $"WeatherButton-_{id}");
                    CuiHelper.DestroyUi(_player, $"LinePercent{id}");
                    CuiHelper.DestroyUi(_player, $"TXTLinePercent{id}");
                    CuiHelper.DestroyUi(_player, $"Line{id}");
                    id++;
                }

                var UI = new CuiElementContainer();

                int i = 0, y = 0;
                foreach (var weather in _.DataBase.WeatherSettings)
                {
                    var weathertype = weather.Value;
                    var percent = weathertype.Percent;
                    var FadeIn = 0f;
                    if (isUpdate != true)
                        FadeIn = 0.1f;

                    UI.Add(new CuiButton
                    {
                        Button =
                        {
                            Color = HexToRustFormat("#00000074"),
                            Command = $"weathercontroller {weathertype.Command} {percent - 10}",
                            FadeIn = FadeIn * i,
                            Material = Sharp
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 13,
                            Text = "-"
                        },
                        RectTransform =
                        {
                            AnchorMin = $"{0.5} {0.8825 - (y * 0.02445)}",
                            AnchorMax = $"{0.5481367} {0.928 - (y * 0.02445)}"
                        }
                    }, UILayer, $"WeatherButton+_{i}");

                    UI.Add(new CuiButton
                    {
                        Button =
                        {
                            Color = HexToRustFormat("#00000074"),
                            Command = $"weathercontroller {weathertype.Command} {percent + 10}",
                            FadeIn = FadeIn * i
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 13,
                            Text = "+"
                        },
                        RectTransform =
                        {
                            AnchorMin = $"{0.949} {0.8825 - (y * 0.02445)}",
                            AnchorMax = $"{0.9975} {0.928 - (y * 0.02445)}"
                        }
                    }, UILayer, $"WeatherButton-_{i}");

                    UI.Add(new CuiElement
                    {
                        Name = $"LinePercent{i}",
                        Parent = UILayer,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = HexToRustFormat("#8ebf5bAD"),
                                Material = Sharp
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0.55} {0.8887039 - (y * 0.02443)}",
                                AnchorMax = $"{0.548 + percent * 0.00399} {0.9219267 - (y * 0.02443)}"
                            }
                        }
                    });

                    UI.Add(new CuiElement
                    {
                        Name = $"TXTLinePercent{i}",
                        Parent = UILayer,
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Align = TextAnchor.MiddleCenter,
                                FontSize = 11,
                                Text = $"{weathertype.Percent}"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0.55} {0.8887039 - (y * 0.02443)}",
                                AnchorMax = $"{0.948} {0.9219267 - (y * 0.02443)}"
                            }
                        }
                    });

                    UI.Add(new CuiElement
                    {
                        Name = $"Line{i}",
                        Parent = UILayer,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = HexToRustFormat("#FFFFFF0D"),
                                Material = BlurInMenu,
                                FadeIn = FadeIn * i
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0} {0.8803958 - (y * 0.02445)}",
                                AnchorMax = $"{0.9975} {0.8814757 - (y * 0.02445)}"
                            }
                        }
                    });
                    y++;
                    y++;
                    i++;
                }

                CuiHelper.AddUi(_player, UI);
            }

            public void DrawUI_WeatherProfiles(bool isUpdate)
            {
                var UI = new CuiElementContainer();

                for (var j = 0; j < 11; j++) CuiHelper.DestroyUi(_player, $"Profile{j}");
                int i = 0, y = 0;
                foreach (var profile in _.DataBase.WeatherProfiles)
                {
                    UI.Add(new CuiElement
                    {
                        Name = $"Profile{i}",
                        Parent = UILayerProfiles + ".Parent",
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = HexToRustFormat("#00000094"),
                                Material = BlurInMenu,
                                FadeIn = 0.1f
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0.002 {0.9114142 - (y * 0.0819)}",
                                AnchorMax = $"0.985 {0.9787733 - (y * 0.0819)}"
                            },
                            new CuiOutlineComponent
                            {
                                Color = HexToRustFormat("#8ebf5b"),
                                Distance = "0.9 0"
                            }
                        }
                    });

                    UI.Add(new CuiButton
                    {
                        Button =
                        {
                            Color = "0 0 0 0",
                            Command = $"weathercontroller.loadprofile {profile.Key}",
                            FadeIn = 0.1f,
                            Material = Blur
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 15,
                            Text = $"{profile.Key}"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    }, $"Profile{i}", $"ProfileButtonLoad{i}");
                    y++;
                    i++;
                }

                CuiHelper.AddUi(_player, UI);
            }

            public void DrawUI_TimeController()
            {
                CuiHelper.DestroyUi(_player, "LinePercentTime");
                CuiHelper.DestroyUi(_player, "PercentTimeTXT");
                CuiHelper.DestroyUi(_player, "TimeButton-");
                CuiHelper.DestroyUi(_player, "TimeButton+");

                var UI = new CuiElementContainer();
                var percent = _.Sky.Cycle.Hour;

                UI.Add(new CuiElement
                {
                    Name = $"LinePercentTime",
                    Parent = UILayer + "TimeController",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#8ebf5bAD"),
                            Material = Sharp
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.057} {0.09}",
                            AnchorMax = $"{0.057 + (percent * 0.0369)} {0.87}"
                        }
                    }
                });

                UI.Add(new CuiElement
                {
                    Name = $"PercentTimeTXT",
                    Parent = UILayer + "TimeController",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 11,
                            Text = $"{_.Sky.Cycle.DateTime.ToString("HH:mm")}"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0} {0.09}",
                            AnchorMax = $"{1} {0.87}"
                        }
                    }
                });

                UI.Add(new CuiButton
                {
                    Button =
                    {
                        Color = HexToRustFormat("#00000074"),
                        Command = $"weathercontroller.time -",
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 13,
                        Text = "-"
                    },
                    RectTransform =
                    {
                        AnchorMin = $"0 0",
                        AnchorMax = $"0.05426357 0.95"
                    }
                }, UILayer + "TimeController", $"TimeButton-");

                UI.Add(new CuiButton
                {
                    Button =
                    {
                        Color = HexToRustFormat("#00000074"),
                        Command = $"weathercontroller.time +",
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 13,
                        Text = "+"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.9457319 0",
                        AnchorMax = "0.998 0.95"
                    }
                }, UILayer + "TimeController", $"TimeButton+");

                CuiHelper.AddUi(_player, UI);
            }

            public void OnDestroy()
            {
                CuiHelper.DestroyUi(_player, UILayer);
                CancelInvoke("Timer");
            }
        }

        #endregion

        #region [Hooks] / [Крюки]

        private void OnServerInitialized()
        {
            Sky = TOD_Sky.Instance;
            _ = this;
            LoadData();
            ServerMgr.Instance.StopAllCoroutines();
        }

        private void Unload()
        {
            SaveData();
        }

        #endregion

        #region [DataBase] / [База данных]

        public class WeatherSetting
        {
            public string Name;
            public string Command;
            public int Percent;
        }

        public class WeatherProfile
        {
            public Dictionary<string, WeatherSetting> Settings = new Dictionary<string, WeatherSetting>();
        }

        public class StoredData
        {
            public readonly Dictionary<string, WeatherProfile> WeatherProfiles = new Dictionary<string, WeatherProfile>
            {
                ["Clear"] = new WeatherProfile
                {
                    Settings = new Dictionary<string, WeatherSetting>
                    {
                        ["weather.fog"] =
                            new WeatherSetting
                            {
                                Name = "Fog",
                                Command = "weather.fog",
                                Percent = 0
                            },
                        ["weather.rain"] =
                            new WeatherSetting
                            {
                                Name = "Rain",
                                Command = "weather.rain",
                                Percent = 0
                            },
                        ["weather.rainbow"] =
                            new WeatherSetting
                            {
                                Name = "Rainbow",
                                Command = "weather.rainbow",
                                Percent = 0
                            },
                        ["weather.thunder"] =
                            new WeatherSetting
                            {
                                Name = "Thunder",
                                Command = "weather.thunder",
                                Percent = 0
                            },
                        ["weather.wind"] =
                            new WeatherSetting
                            {
                                Name = "Wind",
                                Command = "weather.wind",
                                Percent = 0
                            },
                        ["weather.atmosphere_brightness"] =
                            new WeatherSetting
                            {
                                Name = "Atmosphere Brightness",
                                Command = "weather.atmosphere_brightness",
                                Percent = 100
                            },
                        ["weather.atmosphere_contrast"] =
                            new WeatherSetting
                            {
                                Name = "Atmosphere Contrast",
                                Command = "weather.atmosphere_contrast",
                                Percent = 100
                            },
                        ["weather.atmosphere_directionality"] =
                            new WeatherSetting
                            {
                                Name = "Atmosphere Directionality",
                                Command = "weather.atmosphere_directionality",
                                Percent = 100
                            },
                        ["weather.atmosphere_mie"] =
                            new WeatherSetting
                            {
                                Name = "Atmosphere Mie",
                                Command = "weather.atmosphere_mie",
                                Percent = 100
                            },
                        ["weather.atmosphere_rayleigh"] =
                            new WeatherSetting
                            {
                                Name = "Atmosphere Rayleigh",
                                Command = "weather.atmosphere_rayleigh",
                                Percent = 100
                            },
                        ["weather.cloud_attenuation"] =
                            new WeatherSetting
                            {
                                Name = "Cloud Attenuation",
                                Command = "weather.cloud_attenuation",
                                Percent = 100
                            },
                        ["weather.cloud_brightness"] =
                            new WeatherSetting
                            {
                                Name = "Cloud Brightness",
                                Command = "weather.cloud_brightness",
                                Percent = 100
                            },
                        ["weather.cloud_coloring"] =
                            new WeatherSetting
                            {
                                Name = "Cloud Coloring",
                                Command = "weather.cloud_coloring",
                                Percent = 100
                            },
                        ["weather.cloud_coverage"] =
                            new WeatherSetting
                            {
                                Name = "Cloud Coverage",
                                Command = "weather.cloud_coverage",
                                Percent = 100
                            },
                        ["weather.cloud_opacity"] =
                            new WeatherSetting
                            {
                                Name = "Cloud Opacity",
                                Command = "weather.cloud_opacity",
                                Percent = 0
                            },
                        ["weather.cloud_saturation"] =
                            new WeatherSetting
                            {
                                Name = "Cloud Saturation",
                                Command = "weather.cloud_saturation",
                                Percent = 100
                            },
                        ["weather.cloud_scattering"] =
                            new WeatherSetting
                            {
                                Name = "Cloud Scattering",
                                Command = "weather.cloud_scattering",
                                Percent = 100
                            },
                        ["weather.cloud_sharpness"] =
                            new WeatherSetting
                            {
                                Name = "Cloud Sharpness",
                                Command = "weather.cloud_sharpness",
                                Percent = 100
                            },
                        ["weather.cloud_size"] =
                            new WeatherSetting
                            {
                                Name = "Cloud Size",
                                Command = "weather.cloud_size",
                                Percent = 100
                            }
                    }
                }
            };

            public readonly Dictionary<string, WeatherSetting> WeatherSettings = new Dictionary<string, WeatherSetting>
            {
                ["weather.fog"] =
                    new WeatherSetting
                    {
                        Name = "Fog",
                        Command = "weather.fog",
                        Percent = 100
                    },
                ["weather.rain"] =
                    new WeatherSetting
                    {
                        Name = "Rain",
                        Command = "weather.rain",
                        Percent = 100
                    },
                ["weather.rainbow"] =
                    new WeatherSetting
                    {
                        Name = "Rainbow",
                        Command = "weather.rainbow",
                        Percent = 100
                    },
                ["weather.thunder"] =
                    new WeatherSetting
                    {
                        Name = "Thunder",
                        Command = "weather.thunder",
                        Percent = 100
                    },
                ["weather.wind"] =
                    new WeatherSetting
                    {
                        Name = "Wind",
                        Command = "weather.wind",
                        Percent = 100
                    },
                ["weather.atmosphere_brightness"] =
                    new WeatherSetting
                    {
                        Name = "Atmosphere Brightness",
                        Command = "weather.atmosphere_brightness",
                        Percent = 100
                    },
                ["weather.atmosphere_contrast"] =
                    new WeatherSetting
                    {
                        Name = "Atmosphere Contrast",
                        Command = "weather.atmosphere_contrast",
                        Percent = 100
                    },
                ["weather.atmosphere_directionality"] =
                    new WeatherSetting
                    {
                        Name = "Atmosphere Directionality",
                        Command = "weather.atmosphere_directionality",
                        Percent = 100
                    },
                ["weather.atmosphere_mie"] =
                    new WeatherSetting
                    {
                        Name = "Atmosphere Mie",
                        Command = "weather.atmosphere_mie",
                        Percent = 100
                    },
                ["weather.atmosphere_rayleigh"] =
                    new WeatherSetting
                    {
                        Name = "Atmosphere Rayleigh",
                        Command = "weather.atmosphere_rayleigh",
                        Percent = 100
                    },
                ["weather.cloud_attenuation"] =
                    new WeatherSetting
                    {
                        Name = "Cloud Attenuation",
                        Command = "weather.cloud_attenuation",
                        Percent = 100
                    },
                ["weather.cloud_brightness"] =
                    new WeatherSetting
                    {
                        Name = "Cloud Brightness",
                        Command = "weather.cloud_brightness",
                        Percent = 100
                    },
                ["weather.cloud_coloring"] =
                    new WeatherSetting
                    {
                        Name = "Cloud Coloring",
                        Command = "weather.cloud_coloring",
                        Percent = 100
                    },
                ["weather.cloud_coverage"] =
                    new WeatherSetting
                    {
                        Name = "Cloud Coverage",
                        Command = "weather.cloud_coverage",
                        Percent = 100
                    },
                ["weather.cloud_opacity"] =
                    new WeatherSetting
                    {
                        Name = "Cloud Opacity",
                        Command = "weather.cloud_opacity",
                        Percent = 100
                    },
                ["weather.cloud_saturation"] =
                    new WeatherSetting
                    {
                        Name = "Cloud Saturation",
                        Command = "weather.cloud_saturation",
                        Percent = 100
                    },
                ["weather.cloud_scattering"] =
                    new WeatherSetting
                    {
                        Name = "Cloud Scattering",
                        Command = "weather.cloud_scattering",
                        Percent = 100
                    },
                ["weather.cloud_sharpness"] =
                    new WeatherSetting
                    {
                        Name = "Cloud Sharpness",
                        Command = "weather.cloud_sharpness",
                        Percent = 100
                    },
                ["weather.cloud_size"] =
                    new WeatherSetting
                    {
                        Name = "Cloud Size",
                        Command = "weather.cloud_size",
                        Percent = 100
                    }
            };
        }

        #endregion

        #region [Commands]

        [ChatCommand("weather")]
        private void WeatherController(BasePlayer player)
        {
            if (player.GetComponent<WeatherComponent>() != null) return;
            player.gameObject.AddComponent<WeatherComponent>();
        }

        [ConsoleCommand("weathercontroller")]  
        private void WeatherControllerConsoleMinus(ConsoleSystem.Arg args)
        { 
            if (args.Player() == null) return;
            var player = args.Player();
            var convertpercent = 0.01 * args.Args[1].ToInt();
            if (args.Args[1].ToInt() < 0) return;
            if (convertpercent > 1) return;
            if (args.Args[1].ToInt() > DataBase.WeatherSettings[args.Args[0]].Percent)
                DataBase.WeatherSettings[args.Args[0]].Percent += 10;
            else DataBase.WeatherSettings[args.Args[0]].Percent -= 10;

            var convertcmd = $"{args.Args[0]} {convertpercent}";
            Server.Command(convertcmd);
            player.GetComponent<WeatherComponent>().DrawUI_WeatherController(true);
        }

        [ConsoleCommand("weathercontroller.loadprofile")]
        private void WeatherController_LoadProfile(ConsoleSystem.Arg args)
        {
            if (args.Args[0] == null) return;
            if (!DataBase.WeatherProfiles.ContainsKey(args.Args[0])) return;
            ServerMgr.Instance.StartCoroutine(LoadProfile(args.Args[0], args.Player()));
        }

        [ConsoleCommand("weathercontroller.close")]
        private void WeatherController_Close(ConsoleSystem.Arg args)
        {
            if (args.Player().GetComponent<WeatherComponent>() == null) return;
            UnityEngine.Object.Destroy(args.Player().GetComponent<WeatherComponent>());
        }

        [ConsoleCommand("weathercontroller.time")]
        private void WeatherController_Time(ConsoleSystem.Arg args)
        {
            if (args.Args[0] == "-")
                Server.Command($"env.time {Sky.Cycle.DateTime.Hour - 1}");
            else
                Server.Command($"env.time {Sky.Cycle.DateTime.Hour + 1}");

            args.Player().GetComponent<WeatherComponent>().DrawUI_TimeController();
        }

        #endregion

        #region [Helpers] / [Вспомогательный код]

        private static void UpdateUI(BasePlayer player, bool IsUpdate)
        {
            player.GetComponent<WeatherComponent>().DrawUI_TimeController();
            player.GetComponent<WeatherComponent>().DrawUI_WeatherController(IsUpdate);
            player.GetComponent<WeatherComponent>().DrawUI_WeatherProfiles(IsUpdate);
        }

        private IEnumerator LoadProfile(string name, BasePlayer player)
        {
            foreach (var setting in DataBase.WeatherProfiles[name].Settings)
            {
                var convertpercent = 0.01 * setting.Value.Percent;
                Server.Command($"{setting.Value.Command} {convertpercent}");
                DataBase.WeatherSettings[setting.Value.Command].Percent = setting.Value.Percent;
                yield return new WaitForSeconds(0.1f);
            }

            PrintWarning($"Загружен профиль {name}");
            UpdateUI(player, true);

            yield return 0;
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, DataBase);

        private void LoadData()
        {
            try
            {
                DataBase = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch (Exception)
            {
                DataBase = new StoredData();
            }
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