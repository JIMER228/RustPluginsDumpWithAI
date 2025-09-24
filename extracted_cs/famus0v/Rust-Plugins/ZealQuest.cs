using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Facepunch.Extend;
using Oxide.Game.Rust.Cui;
using UnityEngine;


namespace Oxide.Plugins
{
    [Info("ZealQuest", "Kira", "1.0.0")]
    [Description("Плагин на систему квестов")]
    public class ZealQuest : RustPlugin
    {
        public static string UI_Parent = "UI_Parent";
        public static string Sharp = "assets/content/ui/ui.background.tile.psd";
        public static string Blur = "assets/content/ui/uibackgroundblur.mat";
        public static string radial = "assets/content/ui/ui.background.transparent.radial.psd";
        public static string regular = "robotocondensed-regular.ttf";
  
        List<string> Test = new List<string>
        {
            "1",
            "2", 
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23",
            "24",
            "25",
            "26",
            "27"
        };
 
        Dictionary<int, string> UI_List = new Dictionary<int, string>
        {
            [1] = "0.4305555:0.5694443",
            [2] = "0.5787015:0.6944422",
            [3] = "0.3055552:0.4212959",
            [4] = "0.7036978:0.7962904",
            [5] = "0.2027771:0.2953697"
        };

        private void DrawUI_MainQuests(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UI_Parent);
            CuiElementContainer UI = new CuiElementContainer();

            UI.Add(new CuiElement
            {
                Name = UI_Parent,
                Parent = "Overlay",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#403929D7"),
                        Material = Sharp
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            UI.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image =
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = $"0 0",
                    AnchorMax = $"0 0"
                }
            }, UI_Parent);

            CuiHelper.AddUi(player, UI);
            DrawUI_ListQuest(player, 3, 1);
        }

        void DrawUI_ListQuest(BasePlayer player, int number, int type)
        {
            CuiElementContainer UI = new CuiElementContainer();
            CuiHelper.DestroyUi(player, "Butt--");
            CuiHelper.DestroyUi(player, "Butt++");

            for (int i = 0; i < 5; i++)
            {
                CuiHelper.DestroyUi(player, $"Quest{i}");
            }

            float up;
            float down;

            int IQuest1 = number - 2;
            if (IQuest1 < 0)
            {
                IQuest1 = Test.Count - 1;
            }

            int IQuest2 = number - 1;
            if (IQuest2 < 1)
            {
                IQuest2 = Test.Count;
            }

            int IQuest3 = number;
            int IQuest4 = number + 1;
            if (IQuest4 > Test.Count)
            {
                IQuest4 = 0;
            }

            int IQuest5 = number + 2;
            if (IQuest5 > (Test.Count - 1))
            {
                IQuest5 = 1;
            }

            if (type == 1) up = 0.35f;
            else up = 0f;
            if (type == 2) down = 0.35f;
            else down = 0f;

            UI.Add(new CuiButton
            {
                Button =
                {
                    Command = $"page- {number}",
                    Color = HexToRustFormat("#0000004E"),
                    Material = Blur
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 15,
                    Text = "▲"
                },
                RectTransform =
                {
                    AnchorMin = $"0.005208403 0.8018438",
                    AnchorMax = $"0.2395834 0.8379538"
                }
            }, UI_Parent, "Butt--");

            UI.Add(new CuiButton
            {
                Button =
                {
                    Command = $"page+ {number}",
                    Color = HexToRustFormat("#0000004E"),
                    Material = Blur
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 15,
                    Text = "▼"
                },
                RectTransform =
                {
                    AnchorMin = $"0.005208403 0.1601852",
                    AnchorMax = $"0.2395834 0.1962963"
                }
            }, UI_Parent, "Butt++");

            UI.Add(new CuiElement
            {
                Name = $"Quest0",
                Parent = UI_Parent,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#00000098"),
                        Sprite = Sharp,
                        Material = Blur,
                        FadeIn = up
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"0.005208403 0.7036978",
                        AnchorMax = $"0.2395834 0.7962904"
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
                    FontSize = 25,
                    Text = Test[IQuest1]
                }
            }, "Quest0");

            UI.Add(new CuiElement
            {
                Name = $"Quest1",
                Parent = UI_Parent,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#000000A7"),
                        Sprite = Sharp,
                        Material = Blur
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"0.005208403 0.5787015",
                        AnchorMax = $"0.2395834 0.6944422"
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
                    FontSize = 25,
                    Text = Test[IQuest2]
                }
            }, "Quest1");

            UI.Add(new CuiElement
            {
                Name = $"Quest2",
                Parent = UI_Parent,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#000000B0"),
                        Sprite = Sharp,
                        Material = Blur
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"0.005208403 0.4305555",
                        AnchorMax = $"0.2395834 0.5694443"
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
                    FontSize = 25,
                    Text = Test[IQuest3]
                }
            }, "Quest2");

            UI.Add(new CuiElement
            {
                Name = $"Quest3",
                Parent = UI_Parent,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#000000A7"),
                        Sprite = Sharp,
                        Material = Blur
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"0.005208403 0.3055552",
                        AnchorMax = $"0.2395834 0.4212959"
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
                    FontSize = 25,
                    Text = Test[IQuest4]
                }
            }, "Quest3");

            UI.Add(new CuiElement
            {
                Name = $"Quest4",
                Parent = UI_Parent,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#00000098"),
                        Sprite = Sharp,
                        Material = Blur,
                        FadeIn = down
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"0.005208403 0.2027771",
                        AnchorMax = $"0.2395834 0.2953697"
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
                    FontSize = 25,
                    Text = Test[IQuest5]
                }
            }, "Quest4");

            CuiHelper.AddUi(player, UI);
        }
  
        [ConsoleCommand("quests")]
        private void OpenQuestList(ConsoleSystem.Arg args)
        {
            DrawUI_MainQuests(args.Player());
        }

        [ConsoleCommand("quests.close")]
        private void CloseQuestList(ConsoleSystem.Arg args)
        {
            CuiHelper.DestroyUi(args.Player(), UI_Parent);
        }

        [ConsoleCommand("page-")]
        private void PageDOWN(ConsoleSystem.Arg args)
        {
            int page = args.Args[0].ToInt() - 1;

            if (page < 0)
            {
                page = 0;
            }

            DrawUI_ListQuest(args.Player(), page, 1);
            Effect Page = new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", args.Player(), 0,
                new Vector3(),
                new Vector3());
            EffectNetwork.Send(Page, args.Player().Connection);
        }

        [ConsoleCommand("page+")]
        private void PageUP(ConsoleSystem.Arg args)
        {
            int page = args.Args[0].ToInt() + 1;

            if (page > Test.Count)
            {
                page = Test.Count;
            }

            DrawUI_ListQuest(args.Player(), page, 2);
            Effect Page = new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", args.Player(), 0,
                new Vector3(),
                new Vector3());
            EffectNetwork.Send(Page, args.Player().Connection);
        }

        #region [Helpers] / [Вспомогательный код]

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