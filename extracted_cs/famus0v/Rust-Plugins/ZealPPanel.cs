using System;
using System.Collections;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Globalization;
using Oxide.Game.Rust.Cui;
using Color = UnityEngine.Color;

namespace Oxide.Plugins
{
    [Info("ZealPPanel", "Kira", "1.0.0")]
    [Description("Инфо панель совмещенная с меню")]
    class ZealPPanel : RustPlugin
    {
        #region [Reference] / [Запросы]
 
        [PluginReference] Plugin ImageLibrary; 

        string GetImg(string name) 
        {
            return (string) ImageLibrary?.Call("GetImage", name) ?? "";
        }
 
        #endregion

        #region [Classes] / [Классы]

        public class MenuButton
        {
            public string Name;
            public string Command;
            public string Image;
            public string Color;
        }

        public class ActiveCategory
        {
            public bool hide_open = false;
            public bool hide_open_infopanel = false;
            public bool hide_open_menu = false;
        }

        #endregion

        #region [Dictionary/Vars] / [Словари/Переменные]

        string Sharp = "assets/content/ui/ui.background.tile.psd";
        string Blur = "assets/content/ui/uibackgroundblur.mat";
        string radial = "assets/content/ui/ui.background.transparent.radial.psd";
        string regular = "robotocondensed-regular.ttf";

        private bool Event_Air = false;
        private bool Event_Ship = false;
        private bool Event_Bradley = false;
        private bool Event_Helicopter = false;

        public List<string> Buttons = new List<string>
        {
            "InfoPanel",
            "Bonus",
            "Menu"
        };

        public Dictionary<ulong, ActiveCategory> PlayerActiveCategories = new Dictionary<ulong, ActiveCategory>();

        public List<MenuButton> MenuButtons = new List<MenuButton>
        {
            new MenuButton
            {
                Name = " ",
                Color = HexToRustFormat("#cc467d"),
                Command = "chat.say /store",
                Image = "assets/icons/store.png"
            },
            new MenuButton
            {
                Name = " ",
                Color = HexToRustFormat("#cc467d"),
                Command = "chat.say /shop",
                Image = "assets/icons/cart.png"
            }
        };

        public List<string> ActiveEvents = new List<string>();

        public string Layer = "Box_Panel";

        #endregion

        #region [DrawUI] / [Показ UI]

        void DrawUI_Panel(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "Logo");
            CuiElementContainer Gui = new CuiElementContainer();

            Gui.Add(new CuiElement
            {
                Name = "Logo",
                Parent = "Overlay",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Color = "1 1 1 1",
                        Png = GetImg("Logo")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.9074073",
                        AnchorMax = "0.05208334 0.9999999"
                    }
                }
            });

            Gui.Add(new CuiButton
            {
                Button =
                {
                    Command = "panel",
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
            }, "Logo", "ButtonPanel");

            Gui.Add(new CuiElement
            {
                Name = Layer,
                Parent = "Logo",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5"
                    }
                }
            });

            CuiHelper.AddUi(player, Gui);
        }

        void DrawUI_Button(BasePlayer player)
        {
            CuiElementContainer Gui = new CuiElementContainer();
            DestroyButtons(player);
            int butnum = 1, i = 0;
            foreach (var button in Buttons)
            {
                int r = 55;
                double c = (double) 3 / 2;
                double rad = (double) i / c * -1.18;
                double x = r * Math.Cos(rad);
                double y = r * Math.Sin(rad);

                int CurrentGradient = 1;
                if (button == "InfoPanel")
                {
                    CurrentGradient = Convert.ToInt32(MathPercent(BasePlayer.activePlayerList.Count, 200));
                }

                switch (button)
                {
                    case "InfoPanel":
                        Gui.Add(new CuiElement
                        {
                            Name = $"Button{butnum}",
                            Parent = Layer,
                            FadeOut = 0.01f + (butnum * 0.01f),
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Color = GetGradient2[CurrentGradient],
                                    Png = GetImg("ButtonBG"),
                                    FadeIn = 0.2f + (butnum * 0.2f)
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"{x - 20} {y - 20}",
                                    AnchorMax = $"{x + 20} {y + 20}"
                                }
                            }
                        });

                        Gui.Add(new CuiButton
                        {
                            Button =
                            {
                                Command = "infopanel",
                                Color = "0 0 0 0"
                            },
                            Text =
                            {
                                Align = TextAnchor.MiddleCenter,
                                Color = "1 1 1 1",
                                FontSize = 14,
                                Text = BasePlayer.activePlayerList.Count.ToString()
                            },
                            RectTransform =
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "0.965 0.95"
                            }
                        }, $"Button{butnum}", $"ButtonElem{butnum}");
                        break;
                    case "Bonus":
                        Gui.Add(new CuiElement
                        {
                            Name = $"Button{butnum}",
                            Parent = Layer,
                            FadeOut = 0.01f + (butnum * 0.01f),
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Color = HexToRustFormat("#ddcbda9e"),
                                    Png = GetImg("ButtonBG"),
                                    FadeIn = 0.2f + (butnum * 0.2f)
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"{x - 20} {y - 20}",
                                    AnchorMax = $"{x + 20} {y + 20}"
                                }
                            }
                        });

                        Gui.Add(new CuiButton
                        {
                            Button =
                            {
                                Command = "chat.say /block",
                                Color = "0 0 0 0"
                            },
                            Text =
                            {
                                Align = TextAnchor.MiddleCenter,
                                Color = "1 1 1 1",
                                FontSize = 14,
                                Text = "B"
                            },
                            RectTransform =
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "0.965 0.95"
                            }
                        }, $"Button{butnum}", $"ButtonElem{butnum}");
                        break;
                    case "Menu":
                        Gui.Add(new CuiElement
                        {
                            Name = $"Button{butnum}",
                            Parent = Layer,
                            FadeOut = 0.01f + (butnum * 0.01f),
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Color = HexToRustFormat("#ddcbda9e"),
                                    Png = GetImg("ButtonBG"),
                                    FadeIn = 0.2f + (butnum * 0.2f)
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"{x - 20} {y - 20}",
                                    AnchorMax = $"{x + 20} {y + 20}"
                                }
                            }
                        });

                        Gui.Add(new CuiButton
                        {
                            Button =
                            {
                                Command = "menu",
                                Color = "0 0 0 0"
                            },
                            Text =
                            {
                                Align = TextAnchor.MiddleCenter,
                                Color = "1 1 1 1",
                                FontSize = 14,
                                Text = "M"
                            },
                            RectTransform =
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "0.965 0.95"
                            }
                        }, $"Button{butnum}", $"ButtonElem{butnum}");
                        break;
                }

                butnum++;
                i++;
            }

            CuiHelper.AddUi(player, Gui);
        }

        void DrawUI_InfoPanel(BasePlayer player)
        {
            CuiElementContainer Gui = new CuiElementContainer();

            int butnum = 1, x = 0;
            foreach (var Event in ActiveEvents)
            {
                Gui.Add(new CuiElement
                {
                    Name = $"Events{butnum}",
                    Parent = Layer,
                    FadeOut = 0.1f + (butnum * 0.1f),
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Color = HexToRustFormat("#f4cbda9e"),
                            Png = GetImg("ButtonBG"),
                            FadeIn = 0.2f + (butnum * 0.2f)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{76 + (x * 44)} {-23}",
                            AnchorMax = $"{122 + (x * 44)} {23}"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#152235"),
                            Distance = "0.02 0.02"
                        }
                    }
                });

                Gui.Add(new CuiElement
                {
                    Name = $"Event_IMG{butnum}",
                    Parent = $"Events{butnum}",
                    FadeOut = 0.1f + (butnum * 0.1f),
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "1 1 1 1",
                            Png = GetImg(Event)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.26 0.28",
                            AnchorMax = "0.72 0.65"
                        }
                    }
                });

                butnum++;
                x++;
            }

            CuiHelper.AddUi(player, Gui);
        }

        void DrawUI_Menu(BasePlayer player)
        {
            CuiElementContainer Gui = new CuiElementContainer();

            int y = 1, butnum = 1;
            foreach (var mbutton in MenuButtons)
            {
                Gui.Add(new CuiElement
                {
                    Name = $"Button_Menu{butnum}",
                    Parent = Layer,
                    FadeOut = 0.1f + (butnum * 0.1f),
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Color = HexToRustFormat("#ddcbda9e"),
                            Png = GetImg("ButtonBG"),
                            FadeIn = 0.2f + (butnum * 0.2f)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{-20} {-75 + (y * -38)}",
                            AnchorMax = $"{19} {-35 + (y * -38)}"
                        }
                    }
                });

                if (mbutton.Image.Contains("assets"))
                {
                    Gui.Add(new CuiElement
                    {
                        Name = $"Button_Menu_IMG{butnum}",
                        Parent = $"Button_Menu{butnum}",
                        FadeOut = 0.1f + (butnum * 0.1f),
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = "1 1 1 1",
                                Sprite = mbutton.Image
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.25 0.25",
                                AnchorMax = "0.7 0.67"
                            }
                        }
                    });
                }
                else
                {
                    Gui.Add(new CuiElement
                    {
                        Name = $"Button_Menu_IMG{butnum}",
                        Parent = $"Button_Menu{butnum}",
                        FadeOut = 0.1f + (butnum * 0.1f),
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Color = "1 1 1 1",
                                Png = GetImg(mbutton.Image)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.25 0.25",
                                AnchorMax = "0.7 0.67"
                            }
                        }
                    });
                }

                Gui.Add(new CuiButton
                {
                    Button =
                    {
                        Command = mbutton.Command,
                        Color = "1 1 1 0"
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleLeft,
                        Color = "1 1 1 1",
                        FontSize = 14,
                        Text = $"              {mbutton.Name}"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "3.5 0.95"
                    },
                    FadeOut = 0.1f + (butnum * 0.1f),
                }, $"Button_Menu{butnum}", $"MButton{butnum}");
                y++;
                butnum++;
            }

            CuiHelper.AddUi(player, Gui);
        }

        #endregion
 
        #region [Hooks] / [Крюки]

        void OnPlayerInit(BasePlayer player)
        {
            NextTick(() => DrawUI_Panel(player));
        }

        private IEnumerator DrawUIAll()
        {
            foreach (var plobj in BasePlayer.activePlayerList)
            {
                DrawUI_Panel(plobj);
                yield return 0;
            }

            yield return 0;
        }

        private Coroutine Proccess;

        void OnServerInitialized()
        {
            GetActiveEvents();

            foreach (var button in MenuButtons)
            {
                if (!button.Image.Contains("assets"))
                {
                    ImageLibrary.Call("AddImage", button.Image, button.Image);
                }
            }

            ImageLibrary.Call("AddImage", "https://i.imgur.com/i1yjhkp.png", "Logo");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/oCGCnVz.png", "ButtonBG");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/HtAifod.png", "Helicopter");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/j5cMDpt.png", "BradleyAPC");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/CL41EJS.png", "CargoPlane");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/xU8IUWO.png", "CargoShip");
            Proccess = Rust.Global.Runner.StartCoroutine(DrawUIAll());
        }

        void Unload()
        {
            if (Proccess != null)
            {
                Rust.Global.Runner.StopCoroutine(Proccess);
            } 
 
            foreach (var plobj in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(plobj, "Logo");
            }
        }

        void GetActiveEvents()
        {
            if (UnityEngine.Object.FindObjectOfType(typeof(BaseHelicopter)) != null)
            {
                if (!ActiveEvents.Contains("Helicopter"))
                {
                    ActiveEvents.Add("Helicopter");
                }
            }
            else
            {
                if (ActiveEvents.Contains("Helicopter"))
                {
                    ActiveEvents.Remove("Helicopter");
                }
            }

            if (UnityEngine.Object.FindObjectOfType(typeof(BradleyAPC)) != null)
            {
                if (!ActiveEvents.Contains("BradleyAPC"))
                {
                    ActiveEvents.Add("BradleyAPC");
                }
            }
            else
            {
                if (ActiveEvents.Contains("BradleyAPC"))
                {
                    ActiveEvents.Remove("BradleyAPC");
                }
            }

            if (UnityEngine.Object.FindObjectOfType(typeof(CargoPlane)) != null)
            {
                if (!ActiveEvents.Contains("CargoPlane"))
                {
                    ActiveEvents.Add("CargoPlane");
                }
            }
            else
            {
                if (ActiveEvents.Contains("CargoPlane"))
                {
                    ActiveEvents.Remove("CargoPlane");
                }
            }

            if (UnityEngine.Object.FindObjectOfType(typeof(CargoShip)) != null)
            {
                if (!ActiveEvents.Contains("CargoShip"))
                {
                    ActiveEvents.Add("CargoShip");
                }
            }
            else
            {
                if (ActiveEvents.Contains("CargoShip"))
                {
                    ActiveEvents.Remove("CargoShip");
                }
            }
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity is BaseHelicopter)
            {
                if (!ActiveEvents.Contains("Helicopter"))
                {
                    ActiveEvents.Add("Helicopter");
                }
            }

            if (entity is BradleyAPC)
            {
                if (!ActiveEvents.Contains("BradleyAPC"))
                {
                    ActiveEvents.Add("BradleyAPC");
                }
            }

            if (entity is CargoPlane)
            {
                if (!ActiveEvents.Contains("CargoPlane"))
                {
                    ActiveEvents.Add("CargoPlane");
                }
            }

            if (entity is CargoShip)
            {
                if (!ActiveEvents.Contains("CargoShip"))
                {
                    ActiveEvents.Add("CargoShip");
                }
            }
        }

        void OnEntityKill(BaseNetworkable entity)
        {
            if (entity is BaseHelicopter)
            {
                if (ActiveEvents.Contains("Helicopter"))
                {
                    ActiveEvents.Remove("Helicopter");
                }
            }

            if (entity is BradleyAPC)
            {
                if (ActiveEvents.Contains("BradleyAPC"))
                {
                    ActiveEvents.Remove("BradleyAPC");
                }
            }

            if (entity is CargoPlane)
            {
                if (ActiveEvents.Contains("CargoPlane"))
                {
                    ActiveEvents.Remove("CargoPlane");
                }
            }

            if (entity is CargoShip)
            {
                if (ActiveEvents.Contains("CargoShip"))
                {
                    ActiveEvents.Remove("CargoShip");
                }
            }
        }

        #endregion

        #region [ChatCommand] / [Чат команды]

        [ConsoleCommand("panel")]
        private void Draw_Buttons(ConsoleSystem.Arg args)
        {
            CheckDictionary(args.Player());
            if (PlayerActiveCategories[args.Player().userID].hide_open != true)
            {
                DrawUI_Button(args.Player());
                PlayerActiveCategories[args.Player().userID].hide_open = true;
            }
            else
            {
                DestroyButtons(args.Player());
                DestroyMenuButton(args.Player());
                DestroyEvents(args.Player());
                PlayerActiveCategories[args.Player().userID].hide_open = false;
                PlayerActiveCategories[args.Player().userID].hide_open_infopanel = false;
                PlayerActiveCategories[args.Player().userID].hide_open_menu = false;
            }
        }

        [ConsoleCommand("infopanel")]
        private void Draw_InfoPanel(ConsoleSystem.Arg args)
        {
            CheckDictionary(args.Player());
            if (PlayerActiveCategories[args.Player().userID].hide_open_infopanel != true)
            {
                DestroyEvents(args.Player());
                DrawUI_InfoPanel(args.Player());
                PlayerActiveCategories[args.Player().userID].hide_open_infopanel = true;
            }

            else
            {
                DestroyEvents(args.Player());
                PlayerActiveCategories[args.Player().userID].hide_open_infopanel = false;
            }
        }

        [ConsoleCommand("menu")]
        private void Draw_Menu(ConsoleSystem.Arg args)
        {
            CheckDictionary(args.Player());
            if (PlayerActiveCategories[args.Player().userID].hide_open_menu != true)
            {
                DrawUI_Menu(args.Player());
                PlayerActiveCategories[args.Player().userID].hide_open_menu = true;
            }

            else
            {
                DestroyMenu(args.Player());
                PlayerActiveCategories[args.Player().userID].hide_open_menu = false;
            }
        }

        #endregion  

        #region [Helpers] / [Вспомогательный код]

        void CheckDictionary(BasePlayer player)
        {
            if (!PlayerActiveCategories.ContainsKey(player.userID))
            {
                PlayerActiveCategories.Add(player.userID, new ActiveCategory());
            }
        }

        void DestroyButtons(BasePlayer player)
        {
            for (int i = 1; i <= 3; i++)
            {
                CuiHelper.DestroyUi(player, $"Button{i}");
            }
        }

        void DestroyMenu(BasePlayer player)
        {
            for (int i = 1;
                i <= MenuButtons.Count;
                i++)
            {
                CuiHelper.DestroyUi(player, $"Button_Menu{i}");
            }
        }

        void DestroyMenuButton(BasePlayer player)
        {
            for (int i = 1;
                i <= MenuButtons.Count;
                i++)
            {
                CuiHelper.DestroyUi(player, $"Button_Menu{i}");
            }
        }

        void DestroyEvents(BasePlayer player)
        {
            for (int i = 1; i <= 4; i++)
            {
                CuiHelper.DestroyUi(player, $"Events{i}");
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
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        private string[] GetGradient2 = new string[100]
        {
            "0.05 1.00 0.05 1.00",
            "0.08 1.00 0.05 1.00",
            "0.12 1.00 0.05 1.00",
            "0.18 1.00 0.05 1.00",
            "0.36 1.00 0.05 1.00",
            "0.44 1.00 0.05 1.00",
            "0.50 1.00 0.05 1.00",
            "0.57 1.00 0.05 1.00",
            "0.64 1.00 0.05 1.00",
            "0.69 1.00 0.05 1.00",
            "0.72 1.00 0.05 1.00",
            "0.77 1.00 0.05 1.00",
            "0.80 1.00 0.05 1.00",
            "0.86 1.00 0.05 1.00",
            "0.92 1.00 0.05 1.00",
            "0.96 1.00 0.05 1.00",
            "1.00 1.00 0.05 1.00",
            "1.00 0.98 0.05 1.00",
            "1.00 0.97 0.05 1.00",
            "1.00 0.96 0.05 1.00",
            "1.00 0.95 0.05 1.00",
            "1.00 0.92 0.05 1.00",
            "1.00 0.88 0.05 1.00",
            "1.00 0.86 0.05 1.00",
            "1.00 0.84 0.05 1.00",
            "1.00 0.80 0.05 1.00",
            "1.00 0.78 0.05 1.00",
            "1.00 0.77 0.05 1.00",
            "1.00 0.76 0.05 1.00",
            "1.00 0.75 0.05 1.00",
            "1.00 0.73 0.05 1.00",
            "1.00 0.72 0.05 1.00",
            "1.00 0.71 0.05 1.00",
            "1.00 0.69 0.05 1.00",
            "1.00 0.68 0.05 1.00",
            "1.00 0.67 0.05 1.00",
            "1.00 0.66 0.05 1.00",
            "1.00 0.65 0.05 1.00",
            "1.00 0.64 0.05 1.00",
            "1.00 0.63 0.05 1.00",
            "1.00 0.62 0.05 1.00",
            "1.00 0.61 0.05 1.00",
            "1.00 0.60 0.05 1.00",
            "1.00 0.59 0.05 1.00",
            "1.00 0.58 0.05 1.00",
            "1.00 0.57 0.05 1.00",
            "1.00 0.56 0.05 1.00",
            "1.00 0.55 0.05 1.00",
            "1.00 0.54 0.05 1.00",
            "1.00 0.53 0.05 1.00",
            "1.00 0.52 0.05 1.00",
            "1.00 0.51 0.05 1.00",
            "1.00 0.50 0.05 1.00",
            "1.00 0.49 0.05 1.00",
            "1.00 0.48 0.05 1.00",
            "1.00 0.47 0.05 1.00",
            "1.00 0.46 0.05 1.00",
            "1.00 0.45 0.05 1.00",
            "1.00 0.44 0.05 1.00",
            "1.00 0.43 0.05 1.00",
            "1.00 0.42 0.05 1.00",
            "1.00 0.41 0.05 1.00",
            "1.00 0.40 0.05 1.00",
            "1.00 0.39 0.05 1.00",
            "1.00 0.38 0.05 1.00",
            "1.00 0.37 0.05 1.00",
            "1.00 0.36 0.05 1.00",
            "1.00 0.35 0.05 1.00",
            "1.00 0.34 0.05 1.00",
            "1.00 0.33 0.05 1.00",
            "1.00 0.32 0.05 1.00",
            "1.00 0.31 0.05 1.00",
            "1.00 0.30 0.05 1.00",
            "1.00 0.29 0.05 1.00",
            "1.00 0.28 0.05 1.00",
            "1.00 0.27 0.05 1.00",
            "1.00 0.26 0.05 1.00",
            "1.00 0.25 0.05 1.00",
            "1.00 0.24 0.05 1.00",
            "1.00 0.23 0.05 1.00",
            "1.00 0.23 0.05 1.00",
            "1.00 0.21 0.05 1.00",
            "1.00 0.20 0.05 1.00",
            "1.00 0.19 0.05 1.00",
            "1.00 0.18 0.05 1.00",
            "1.00 0.17 0.05 1.00",
            "1.00 0.16 0.05 1.00",
            "1.00 0.15 0.05 1.00",
            "1.00 0.14 0.05 1.00",
            "1.00 0.13 0.05 1.00",
            "1.00 0.12 0.05 1.00",
            "1.00 0.11 0.05 1.00",
            "1.00 0.09 0.05 1.00",
            "1.00 0.08 0.05 1.00",
            "1.00 0.07 0.05 1.00",
            "1.00 0.06 0.05 1.00",
            "1.00 0.05 0.05 1.00",
            "1.00 0.04 0.05 1.00",
            "1.00 0.03 0.05 1.00",
            "1.00 0.02 0.05 1.00"
        };

        double MathPercent(int number1, int totalnumber)
        {
            double percent = (double) number1 / totalnumber * 100;
            return percent;
        }

        #endregion
    }
} 