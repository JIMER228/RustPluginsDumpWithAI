using System;
using System.Collections;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ControlPanel", "Luminos", "1.0.0")]
    public class ControlPanel : RustPlugin
    {
        #region [References]

        [PluginReference] private Plugin ImageLibrary, PluginManager;

        #endregion

        #region [Vars]

        private const string PanelName = "Control Panel";

        private const string UIMenu = "UI.Menu";
        private const string UIMain = "UI.Main";
        private const string Regular = "robotocondensed-regular.ttf";
        private const double Step = 0.54 / 200;

        private static readonly WaitForSeconds WaitForSeconds = new WaitForSeconds(0.1f);
        private static ControlPanel _;

        private readonly Dictionary<string, string> _images = new Dictionary<string, string>
        {
            ["UI.Menu.Background"] = "https://i.imgur.com/wYR6D1E.png",
            ["UI.Menu.Logo"] = "https://i.imgur.com/cLMubVf.png",
            ["UI.Menu.Widget.QuickButtons.Background"] = "https://i.imgur.com/JyiQuPv.png",
            ["UI.Menu.Widget.QuickButtons.Button"] = "https://i.imgur.com/TiCaL2v.png",
            ["UI.Menu.Widget.MicroStatistic.Background"] = "https://i.imgur.com/fjiIXpL.png",
            ["UI.Menu.Button"] = "https://i.imgur.com/Ei7Afnt.png",
            ["UI.Menu.Widget.MicroStatistic.Line"] = "https://i.imgur.com/YEx4Gjf.png"
        };

        private class ConfigElement
        {
            public string Name;
            public string Description;
            public string Command;
            public bool IsBool;
            public bool RestartIsRequired;
        }

        private class Button
        {
            public string DisplayName;
            public string Command;
            public string AdditionName;
            public bool IsAddition;
        }

        private static readonly List<ConfigElement> ConfigElements = new List<ConfigElement>
        {
            new ConfigElement
            {
                Name = "Server Name",
                Description = "Enter server name",
                Command = "server.hostname",
                IsBool = false,
                RestartIsRequired = false
            },
            
            new ConfigElement
            {
                Name = "AI Move",
                Description = "Allow NPC movement",
                Command = "ai.move",
                IsBool = true,
                RestartIsRequired = false
            }
        };
        
        private static readonly List<Button> Buttons = new List<Button>
        {
            new Button
            {
                DisplayName = "Main",
                Command = "control"
            },
            new Button
            {
                DisplayName = "Server Statistic",
                Command = "control.statistic.server"
            },
            new Button
            {
                DisplayName = "Player Manager",
                Command = "control.manager.player",
                AdditionName = "PlayerManager",
                IsAddition = true
            },
            new Button
            {
                DisplayName = "Plugin Manager",
                Command = "control.manager.plugin",
                AdditionName = "PluginManager",
                IsAddition = true
            },
            new Button
            {
                DisplayName = "Personal Manager",
                Command = "control.manager.personal"
            }
        };

        private static readonly List<Button> Settings = new List<Button>
        {
            new Button
            {
                DisplayName = "Additions",
                Command = "settings.additions"
            },
            new Button
            {
                DisplayName = "Server",
                Command = "settings.server"
            },
            new Button
            {
                DisplayName = "Panel",
                Command = "settings.panel"
            }
        };

        #endregion

        #region [MonoBehaviours]

        public class UIControlPanel : MonoBehaviour
        {
            public BasePlayer player;
            private const float FadeIn = 0.1f;

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
            }

            private void Start()
            {
                InitializedUI();
            }

            #region [UI]

            private void OpenUIParent()
            {
                var ui = new CuiElementContainer();

                ui.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    Image =
                    {
                        Color = "0.13 0.13 0.15 1"
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
                            Png = (string) _.ImageLibrary.Call("GetImage", "UI.Menu.Background"),
                            FadeIn = FadeIn
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0.1601 1"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UIMenu}.Author",
                    Parent = UIMain,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 12,
                            Color = "0.25 0.25 0.26 1.00",
                            Text = "Development by Luminos",
                            FadeIn = FadeIn
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.1601 0",
                            AnchorMax = "1 0.02962963"
                        }
                    }
                });

                CuiHelper.DestroyUi(player, UIMain);
                CuiHelper.AddUi(player, ui);
            }

            private void OpenUIMenu()
            {
                var ui = new CuiElementContainer();

                ui.Add(new CuiElement
                {
                    Name = $"{UIMenu}.Logo",
                    Parent = UIMenu,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = (string) _.ImageLibrary.Call("GetImage", "UI.Menu.Logo"),
                            FadeIn = FadeIn
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.0323623 0.9527779",
                            AnchorMax = "0.152 0.9870372"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UIMenu}.Name",
                    Parent = UIMenu,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            FontSize = 19,
                            Color = "0.94 0.39 0.12 0.7",
                            Text = PanelName.ToUpper(),
                            FadeIn = FadeIn
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.1844662 0.953",
                            AnchorMax = "1 0.9861094"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UIMenu}.SeparateLine",
                    Parent = UIMenu,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0.94 0.39 0.12 0.53",
                            FadeIn = FadeIn
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.9416661",
                            AnchorMax = "0.995 0.942592"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UIMenu}.Header",
                    Parent = UIMenu,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 19,
                            Color = "0.25 0.25 0.26 1.00",
                            Text = "MENU",
                            FadeIn = FadeIn
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.6907408",
                            AnchorMax = "1 0.7333335"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UIMenu}.Settings",
                    Parent = UIMenu,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 19,
                            Color = "0.25 0.25 0.26 1.00",
                            Text = "SETTINGS",
                            FadeIn = FadeIn
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.1879667",
                            AnchorMax = "1 0.2305608"
                        }
                    }
                });

                CuiHelper.AddUi(player, ui);
            }

            #endregion [UI]

            #region [Widgets]

            public void UIQuickButtons()
            {
                var ui = new CuiElementContainer();
                var directory = $"{UIMenu}.Widget.QuickButtons";

                ui.Add(new CuiElement
                {
                    Name = directory,
                    Parent = UIMenu,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = (string) _.ImageLibrary.Call("GetImage",
                                "UI.Menu.Widget.QuickButtons.Background"),
                            FadeIn = FadeIn
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.02912652 0.8583332",
                            AnchorMax = "0.9632007 0.9324073"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{directory}.Name",
                    Parent = directory,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 11,
                            Color = "0.23 0.23 0.24 1.00",
                            Text = "QUICK BUTTONS",
                            FadeIn = FadeIn
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.6625036",
                            AnchorMax = "1 0.9875012"
                        }
                    }
                });

                for (var x = 0; x < 5; x++)
                {
                    ui.Add(new CuiElement
                    {
                        Name = $"{directory}.Button",
                        Parent = directory,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Color = "1 1 1 0.7",
                                Png = (string) _.ImageLibrary.Call("GetImage", "UI.Menu.Widget.QuickButtons.Button"),
                                FadeIn = FadeIn
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0.03811106 + (x * 0.191)} {0.1000018}",
                                AnchorMax = $"{0.1940206 + (x * 0.191)} {0.6625012}"
                            }
                        }
                    });
                }

                CuiHelper.DestroyUi(player, directory);
                CuiHelper.AddUi(player, ui);
            }

            public void UIMicroStatistic()
            {
                var ui = new CuiElementContainer();
                var directory = $"{UIMenu}.Widget.MicroStatistic";

                ui.Add(new CuiElement
                {
                    Name = directory,
                    Parent = UIMenu,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = (string) _.ImageLibrary.Call("GetImage",
                                "UI.Menu.Widget.MicroStatistic.Background"),
                            FadeIn = FadeIn
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.02912652 0.7333335",
                            AnchorMax = "0.9632007 0.8490767"
                        }
                    }
                });

                for (var x = 0; x <= 7; x++)
                {
                    var math = Core.Random.Range(0, 200);
                    var size = (Step * math) + 0.19;
                    ui.Add(new CuiElement
                    {
                        Name = $"{directory}.Line",
                        Parent = directory,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Color = "1 1 1 0.75",
                                Png = (string) _.ImageLibrary.Call("GetImage", "UI.Menu.Widget.MicroStatistic.Line"),
                                FadeIn = FadeIn
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0.06 + (x * 0.086)} {0.1805907}",
                                AnchorMax = $"{0.12 + (x * 0.086)} {size}"
                            }
                        }
                    });

                    ui.Add(new CuiElement
                    {
                        Name = $"{directory}.Count",
                        Parent = directory,
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Align = TextAnchor.MiddleCenter,
                                FontSize = 8,
                                Color = "0.34 0.34 0.35 1.00",
                                Text = $"{math}",
                                FadeIn = FadeIn
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0.055 + (x * 0.086)} {size}",
                                AnchorMax = $"{0.117 + (x * 0.086)} {size + 0.15}"
                            }
                        }
                    });
                }

                CuiHelper.DestroyUi(player, directory);
                CuiHelper.AddUi(player, ui);
            }

            #endregion [Widgets]

            #region [Helpers]

            public void UpdateWidgets()
            {
                UIQuickButtons();
                UIMicroStatistic();
            }

            public void InitializedUI()
            {
                StopAllCoroutines();
                OpenUIParent();
                OpenUIMenu();
                UpdateWidgets();
                StartCoroutine(UIMenu_Buttons());
            }

            #region [IEnumerators]

            private IEnumerator UIMenu_Buttons()
            {
                int by = 0, sy = 0;
                foreach (var button in Buttons)
                {
                    var uibuttons = new CuiElementContainer();
                    uibuttons.Add(new CuiElement
                    {
                        Name = $"{UIMenu}.Button.Background.{by}",
                        Parent = UIMenu,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = (string) _.ImageLibrary.Call("GetImage", "UI.Menu.Button"),
                                FadeIn = FadeIn
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0.0291263} {0.6490728 - (by * 0.0526)}",
                                AnchorMax = $"{0.9647981} {0.6917743 - (by * 0.0526)}"
                            }
                        }
                    });

                    uibuttons.Add(new CuiButton
                    {
                        Button =
                        {
                            Color = "0 0 0 0",
                            Command = $"{button.Command}",
                            FadeIn = 0.1f * by
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleLeft,
                            Color = "0.34 0.34 0.35 1.00",
                            FontSize = 14,
                            Text = button.DisplayName.ToUpper()
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.05 0",
                            AnchorMax = "1 0.95"
                        }
                    }, $"{UIMenu}.Button.Background.{by}", $"{UIMenu}.Button.{by}");
                    by++;
                    CuiHelper.AddUi(player, uibuttons);
                    yield return WaitForSeconds;
                }

                foreach (var setting in Settings)
                {
                    var uisettings = new CuiElementContainer();
                    uisettings.Add(new CuiElement
                    {
                        Name = $"{UIMenu}.Setting.Background.{sy}",
                        Parent = UIMenu,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = (string) _.ImageLibrary.Call("GetImage", "UI.Menu.Button"),
                                FadeIn = 0.1f * sy
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0.0291263} {0.1462985 - (sy * 0.0526)}",
                                AnchorMax = $"{0.9647981} {0.1890013 - (sy * 0.0526)}"
                            }
                        }
                    });

                    uisettings.Add(new CuiButton
                    {
                        Button =
                        {
                            Color = "0 0 0 0",
                            Command = setting.Command,
                            FadeIn = 0.1f * sy
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = "0.34 0.34 0.35 1.00",
                            FontSize = 14,
                            Text = setting.DisplayName.ToUpper()
                        },
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 0.95"
                        }
                    }, $"{UIMenu}.Setting.Background.{sy}", $"{UIMenu}.Setting.{sy}");
                    sy++;
                    CuiHelper.AddUi(player, uisettings);
                    yield return WaitForSeconds;
                }

                yield return 0;
            }

            #endregion [IEnumerators]

            #endregion [Helpers]

            public void OnDestroy()
            {
                CuiHelper.DestroyUi(player, UIMain);
            }
        }

        #endregion

        #region [Hooks]

        private void OnServerInitialized()
        {
            _ = this;
            ServerMgr.Instance.StartCoroutine(LoadImages());
            foreach (var player in BasePlayer.activePlayerList)
                player.gameObject.AddComponent<UIControlPanel>();
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
                if (player.GetComponent<UIControlPanel>() != null)
                    UnityEngine.Object.Destroy(player.GetComponent<UIControlPanel>());
        }

        #endregion

        #region [ConsoleCommands]

        [ConsoleCommand("control")]
        private void OpenControlPanel(ConsoleSystem.Arg args)
        {
            var component = args.Player().GetComponent<UIControlPanel>();
            component.InitializedUI();
        }

        [ConsoleCommand("control.exit")]
        private void ExitControlPanel(ConsoleSystem.Arg args)
        {
            var component = args.Player().GetComponent<UIControlPanel>();
            component.OnDestroy();
        }

        #endregion

        #region [Helpers]

        private IEnumerator LoadImages()
        {
            foreach (var image in _images)
            {
                ImageLibrary.Call("AddImage", image.Value, image.Key);
                yield return WaitForSeconds;
            }

            yield return 0;
        }

        #endregion

        #region [DataBase]

        public class Data
        {
            public Dictionary<DateTime, int> OnlineStatistic = new Dictionary<DateTime, int>();

            public enum DaysOfTheWeek
            {
                Monday,
                Tuesday,
                Wednesday,
                Thursday,
                Friday,
                Saturday,
                Sunday
            }
        }

        #endregion
    }
}