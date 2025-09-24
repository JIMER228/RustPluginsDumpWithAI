using System;
using System.Collections.Generic;
using System.Globalization;
using Facepunch.Extend;
using Oxide.Core.Plugins;
using UnityEngine;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("ZealCraftSystem", "Kira", "1.0.0")]
    [Description("Crafting system")]
    class ZealCraftSystem : RustPlugin
    {
        [PluginReference] Plugin ImageLibrary;

        string Sharp = "assets/content/ui/ui.background.tile.psd";
        string Blur = "assets/content/ui/uibackgroundblur.mat";
        string radial = "assets/content/ui/ui.background.transparent.radial.psd";
        string regular = "robotocondensed-regular.ttf";
        string UI_Layer = "UI_Layer";

        public class Item
        {
            public string Shortname;
            public int Count;
            public ulong SkinID;
        }

        private List<Item> Tests = new List<Item>
        {
            new Item
            {
                Shortname = "stones",
                Count = 100,
                SkinID = 0
            },
            new Item
            {
                Shortname = "wood",
                Count = 100,
                SkinID = 0
            },
            new Item
            {
                Shortname = "metal.fragments",
                Count = 100,
                SkinID = 0
            },
            new Item
            {
                Shortname = "sulfur",
                Count = 100,
                SkinID = 0
            },
            new Item
            {
                Shortname = "rifle.ak",
                Count = 1,
                SkinID = 0
            },
        };

        void DrawUI_CraftSystem(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UI_Layer);
            CuiElementContainer Gui = new CuiElementContainer();

            Gui.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image =
                {
                    Color = HexToRustFormat("#000000F9"),
                    Material = Blur,
                    Sprite = radial
                }
            }, "Overlay", UI_Layer);

            Gui.Add(new CuiElement
            {
                Name = "Zagolovok",
                Parent = UI_Layer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 40,
                        Text = $"КРАФТ УНИКАЛЬНЫХ ПРЕДМЕТОВ"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.8999999",
                        AnchorMax = "1 0.9925926"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            #region [Backpack]

            Gui.Add(new CuiElement
            {
                Name = "Bacpack_BG",
                Parent = UI_Layer,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#00000078"),
                        Material = Blur
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"0.3052109 0.5046243",
                        AnchorMax = $"0.4335095 0.7323948"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "Bacpack_IC",
                Parent = UI_Layer,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage("Backpack_IC_1")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"0.3052109 0.5046243",
                        AnchorMax = $"0.4335095 0.7323948"
                    }
                }
            });

            for (int y = 0, num = 1; y < 3; y++, num++)
            {
                Gui.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"craft Backpack {num}",
                        Color = HexToRustFormat("#409ABE"),
                        Material = Blur
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 14,
                        Color = HexToRustFormat($"#E7E7E7FF"),
                        Text = $"УРОВЕНЬ {num}"
                    },
                    RectTransform =
                    {
                        AnchorMin = $"{0.3052109} {0.4620317 - (0.043 * y)}",
                        AnchorMax = $"{0.4335095} {0.4990688 - (0.043 * y)}"
                    }
                }, UI_Layer, $"Button");
            }

            #endregion

            #region [Phone]

            Gui.Add(new CuiElement
            {
                Name = "Phone_BG",
                Parent = UI_Layer,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#00000078"),
                        Material = Blur
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"0.4361137 0.5046243",
                        AnchorMax = $"0.5644123 0.7323948"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "Phone_IC",
                Parent = UI_Layer,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage("Phone_IC")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"0.4361137 0.5046243",
                        AnchorMax = $"0.5644123 0.7323948"
                    }
                }
            });

            Gui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"craft.phone",
                    Color = HexToRustFormat("#409ABE"),
                    Material = Blur
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 14,
                    Color = HexToRustFormat($"#E7E7E7FF"),
                    Text = $"КРАФТ"
                },
                RectTransform =
                {
                    AnchorMin = $"{0.4361137} {0.4620317}",
                    AnchorMax = $"{0.5644124} {0.4990688}"
                }
            }, UI_Layer, $"Button");

            #endregion

            #region [Recycler]

            Gui.Add(new CuiElement
            {
                Name = "Recycler_BG",
                Parent = UI_Layer,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#00000078"),
                        Material = Blur
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"0.5670165 0.5046243",
                        AnchorMax = $"0.6953151 0.7323948"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "Recycler_IC",
                Parent = UI_Layer,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage("Recycler_IC")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"0.5670165 0.5046243",
                        AnchorMax = $"0.6953151 0.7323948"
                    }
                }
            });

            Gui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"craft.recycler",
                    Color = HexToRustFormat("#409ABE"),
                    Material = Blur
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 14,
                    Color = HexToRustFormat($"#E7E7E7FF"),
                    Text = $"КРАФТ"
                },
                RectTransform =
                {
                    AnchorMin = $"{0.5670167} {0.4620317}",
                    AnchorMax = $"{0.6953154} {0.4990688}"
                }
            }, UI_Layer, $"Button");

            #endregion

            CuiHelper.AddUi(player, Gui);
        }

        void DrawUI_Craft_Backpack(BasePlayer player, int lvl)
        {
            CuiElementContainer Gui = new CuiElementContainer();

            CuiHelper.DestroyUi(player, UI_Layer);

            Gui.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image =
                {
                    Color = HexToRustFormat("#00000077"),
                    Material = Blur
                }
            }, "Overlay", UI_Layer);

            Gui.Add(new CuiElement
            {
                Name = "Zagolovok",
                Parent = UI_Layer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 40,
                        Font = regular,
                        Text = $"<b>КРАФТ РЮКЗАКА</b>\n" +
                               $"<size=25>УРОВЕНЬ [<b><color=#9D4C4C> {lvl} </color></b>]</size>"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.8138914",
                        AnchorMax = "1 1"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "Bacpack_BG",
                Parent = UI_Layer,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#0000005C"),
                        Material = Blur
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.4361137 0.5046243",
                        AnchorMax = "0.5644123 0.7323948"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#157CA6FF"),
                        Distance = "1.1 1.3"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = $"Backpack_IC_{lvl}",
                Parent = UI_Layer,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage($"Backpack_IC_{lvl}")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.4361137 0.5046243",
                        AnchorMax = "0.5644123 0.7323948"
                    }
                }
            });

            Gui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"crafting backpack {lvl}",
                    Color = HexToRustFormat("#409ABE"),
                    Material = Blur
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 14,
                    Color = HexToRustFormat($"#E7E7E7FF"),
                    Text = $"КРАФТ"
                },
                RectTransform =
                {
                    AnchorMin = $"{0.4361137} {0.4620317}",
                    AnchorMax = $"{0.5644124} {0.4990688}"
                }
            }, UI_Layer, $"Button");

            int x = 0, y = 0, num = 0, counts = Tests.Count / 2;
            foreach (var elem in Tests)
            {
                if (num == counts)
                {
                    y++;
                    x = 0;
                }

                Gui.Add(new CuiElement
                {
                    Name = $"Item_BG_{num}",
                    Parent = UI_Layer,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#0000004F"),
                            Material = Blur,
                            FadeIn = 0.1f + (0.1f * num)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.3781254 - (0.0549 * x)} {0.5259292 + (y * 0.0975)}",
                            AnchorMax = $"{0.4302087 - (0.0549 * x)} {0.6185218 + (y * 0.0975)}"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#409ABE"),
                            Distance = "0.68 0.69"
                        }
                    }
                });

                Gui.Add(new CuiElement
                {
                    Name = $"Item_IC_{num}",
                    Parent = $"Item_BG_{num}",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage($"{elem.Shortname}")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0.1 0.1",
                            AnchorMax = $"0.9 0.9"
                        }
                    }
                });

                Gui.Add(new CuiElement
                {
                    Name = "Text_Count",
                    Parent = $"Item_BG_{num}",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleRight,
                            Font = regular,
                            FontSize = 11,
                            Text = $"x {elem.Count}"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0.01999242 0.01999462",
                            AnchorMax = $"0.95 0.2199941"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#000000AE"),
                            Distance = "0.5 0.5"
                        }
                    }
                });
                x++;
                num++;
            }

            CuiHelper.AddUi(player, Gui);
        }

        void OnServerInitialized()
        { 
            ImageLibrary.Call("AddImage", "https://i.imgur.com/NDBTKPN.png", "Recycler_IC");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/CAxAQFV.png", "Phone_IC");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/YBP3Vz5.png", "Backpack_IC_1");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/NOJpwQU.png", "Backpack_IC_2");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/ZAdwfKv.png", "Backpack_IC_3");
        }

        [ChatCommand("craft")]
        private void DrawUI_CraftSystem(BasePlayer player, string command, string[] args)
        {
            DrawUI_CraftSystem(player);
        }

        [ConsoleCommand("craft")] 
        private void Craft_Open(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            switch (args.Args[0])
            {
                case "Backpack":
                    DrawUI_Craft_Backpack(args.Player(), args.Args[1].ToInt());
                    break;
            }
        }

        [ConsoleCommand("close.craftsystem")]
        private void UI_Destroy(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            CuiHelper.DestroyUi(args.Player(), UI_Layer);
        }

        string GetImage(string name)
        {
            return (string) ImageLibrary.Call("GetImage", name);
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
    }
}