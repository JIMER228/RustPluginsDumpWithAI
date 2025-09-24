using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Random = Oxide.Core.Random;

namespace Oxide.Plugins
{
    [Info("ZealClanSystem", "Kira", "1.0.0")]
    public class ZealClanSystem : RustPlugin
    {
        [PluginReference] private Plugin ImageLibrary;
        private string Sharp = "assets/content/ui/ui.background.tile.psd";
        private string Blur = "assets/content/ui/uibackgroundblur.mat";
        private string BlurInMenu = "assets/content/ui/uibackgroundblur-ingamemenu.mat";
        private string radial = "assets/content/ui/ui.background.transparent.radial.psd";
        private string regular = "robotocondensed-regular.ttf";
        public string UI_Parent_Main = "UI_MainParent";

        public class SkinInClan
        {
            public string ShortName; 
            public ulong SkinID;
        }

        public List<SkinInClan> SkinInClans = new List<SkinInClan>
        {
            new SkinInClan
            {
                ShortName = "metal.facemask",
                SkinID = 0
            },
            new SkinInClan
            {
                ShortName = "metal.plate.torso",
                SkinID = 0
            },
            new SkinInClan
            {
                ShortName = "roadsign.kilt",
                SkinID = 0
            },
            new SkinInClan
            {
                ShortName = "shoes.boots",
                SkinID = 0
            },
            new SkinInClan
            {
                ShortName = "tactical.gloves",
                SkinID = 0
            },
            new SkinInClan
            {
                ShortName = "rifle.ak",
                SkinID = 0
            },
            new SkinInClan
            {
                ShortName = "rifle.lr300",
                SkinID = 0
            },
            new SkinInClan
            {
                ShortName = "rifle.bolt",
                SkinID = 0
            },
            new SkinInClan
            {
                ShortName = "smg.thompson",
                SkinID = 0
            },
            new SkinInClan
            {
                ShortName = "rifle.semiauto",
                SkinID = 0
            },
        };

        public List<string> Resource = new List<string>
        {
            "sulfur.ore",
            "stones",
            "wood",
            "hq.metal.ore",
            "metal.ore",
            "fat.animal",
            "scrap",
            "leather",
            "cloth",
            "roadsigns"
        };

        public void DrawUI_Main(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UI_Parent_Main);
            CuiElementContainer UI = new CuiElementContainer();

            UI.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image =
                {
                    Color = HexToRustFormat("#000000AE"),
                    Material = BlurInMenu,
                    Sprite = radial
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, "Overlay", UI_Parent_Main);

            UI.Add(new CuiButton
            {
                Button =
                {
                    Close = UI_Parent_Main,
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
            }, UI_Parent_Main);

            #region [Buttons] / [Кнопки]

            UI.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image =
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = "0 0.9353705",
                    AnchorMax = "0.999 0.9953705"
                }
            }, UI_Parent_Main, UI_Parent_Main + ".ButtonLayer");

            for (int i = 0; i < 3; i++)
            {
                UI.Add(new CuiElement
                {
                    Name = $"Button{i}",
                    Parent = UI_Parent_Main + ".ButtonLayer",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage($"Button{i + 1}")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.003125 + (0.3322917 * i)} {0.081}",
                            AnchorMax = $"{0.331 + (0.3322917 * i)} {0.918}"
                        }
                    }
                });

                UI.Add(new CuiElement
                {
                    Name = "ButtonTXT",
                    Parent = $"Button{i}",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 25,
                            Text = "ГЛАВНАЯ",
                            Color = HexToRustFormat("#9C8E82")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#000000AE"),
                            Distance = "0.5 0.5"
                        }
                    }
                });
            }

            #endregion

            #region [Skins] / [Скины]

            UI.Add(new CuiElement
            {
                Name = "SkinDesc",
                Parent = UI_Parent_Main,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.UpperCenter,
                        Color = "1 1 1 1",
                        FontSize = 15,
                        Text =
                            "<B>\nУНИФОРМА КЛАНА</B>\n<size=12><color=#E5E5E5>Данный скин одежда будет автоматически применена на соклановцев</color></size>",
                        Font = "robotocondensed-regular.ttf"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.007812271 0.8175926",
                        AnchorMax = "0.39 0.8907321"
                    }
                }
            });

            int id = 0, xs = 0, ys = 0;
            foreach (var skin in SkinInClans)
            {
                if (xs == 5)
                {
                    xs = 0;
                    ys--;
                }

                UI.Add(new CuiElement
                {
                    Name = $"Skin{id}",
                    Parent = UI_Parent_Main,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#4038286E"),
                            Sprite = radial,
                            Material = Blur,
                            FadeIn = 0.1f + (id * 0.1f)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.00781227 + (xs * 0.0785)} {0.6935207 + (ys * 0.1393)}",
                            AnchorMax = $"{0.07552351 + (xs * 0.0785)} {0.8138911 + (ys * 0.1393)}"
                        }
                    }
                });

                UI.Add(new CuiElement
                {
                    Name = $"SkinIC{id}",
                    Parent = $"Skin{id}",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage($"{skin.ShortName}_128_{skin.SkinID}"),
                            Color = "1 1 1 0.95",
                            FadeIn = 0.1f + (id * 0.1f)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0.05 0.05",
                            AnchorMax = $"0.94 0.94"
                        }
                    }
                });
                xs++;
            }

            UI.Add(new CuiElement
            {
                Name = "ResDesc",
                Parent = UI_Parent_Main,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.UpperCenter,
                        Color = "1 1 1 1",
                        FontSize = 15,
                        Text =
                            "<B>\nЕЖЕДНЕВНАЯ ЗАДАЧА</B>\n<size=12><color=#E5E5E5>Каждый день соклановцы должны выполнять норму, по добыче ресурсов</color></size>",
                        Font = "robotocondensed-regular.ttf"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5968711 0.8175926",
                        AnchorMax = "0.9942633 0.8907321"
                    }
                }
            });

            int xr = 0, yr = 0, ir = 0;
            foreach (var resource in Resource)
            {
                if (xr == 5)
                {
                    xr = 0;
                    yr--;
                }

                UI.Add(new CuiElement
                {
                    Name = $"Resource{ir}",
                    Parent = UI_Parent_Main,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#4038286E"),
                            Sprite = radial,
                            Material = Blur,
                            FadeIn = 0.1f + (ir * 0.1f)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.5947878 + (xr * 0.0785)} {0.6935207 + (yr * 0.1393)}",
                            AnchorMax = $"{0.6624961 + (xr * 0.0785)} {0.8138911 + (yr * 0.1393)}"
                        }
                    }
                });

                UI.Add(new CuiElement
                {
                    Name = $"ResourceProgress{ir}",
                    Parent = $"Resource{ir}",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#61AB5DD0"),
                            Material = Sharp,
                            FadeIn = 0.1f + (ir * 0.1f)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0 0",
                            AnchorMax = $"0.989 {Random.Range(0.1, 0.9)}"
                        }
                    }
                });

                UI.Add(new CuiElement
                {
                    Name = $"ResourceIC{ir}",
                    Parent = $"Resource{ir}",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage($"{resource}_128"),
                            Color = "1 1 1 0.95",
                            FadeIn = 0.1f + (ir * 0.1f)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0.05 0.05",
                            AnchorMax = $"0.95 0.95"
                        }
                    }
                });

                UI.Add(new CuiElement
                {
                    Name = "ResourceProgressTXT",
                    Parent = $"Resource{ir}",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 20,
                            Text = "90%",
                            FadeIn = 0.1f + (ir * 0.1f)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#0000008A"),
                            Distance = "0.9 0.9"
                        }
                    }
                });
                xr++;
            }

            #endregion

            #region [Avatar] / [Аватар клана]

            UI.Add(new CuiElement
            {
                Name = "Line222",
                Parent = UI_Parent_Main,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "1 1 1 0.1"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.4994792 0",
                        AnchorMax = "0.5 1"
                    }
                }
            });

            UI.Add(new CuiElement
            {
                Name = "Line22",
                Parent = UI_Parent_Main,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "1 1 1 0.1"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.499",
                        AnchorMax = "1 0.5"
                    }
                }
            });

            UI.Add(new CuiElement
            {
                Name = "Avatar",
                Parent = UI_Parent_Main,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "Avatar")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.4223958 0.5666698",
                        AnchorMax = "0.5786459 0.8444473"
                    }
                }
            });

            UI.Add(new CuiElement
            {
                Name = "AvatarSprite",
                Parent = "Avatar",
                Components =
                {
                    new CuiImageComponent
                    {
                        Sprite = radial,
                        Color = HexToRustFormat("#000000AE")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "0.9945 0.9945"
                    }
                }
            });

            UI.Add(new CuiElement
            {
                Name = "MemberName",
                Parent = "Avatar",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.UpperCenter,
                        FontSize = 25,
                        Color = HexToRustFormat("#EFEFEFFF"),
                        Text = $"<B>{player.displayName}</B>\n<size=19>Звание : Рядовой</size>",
                        Font = regular
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "-0.5 -0.3533376",
                        AnchorMax = "1.5 -0.01333703"
                    }
                }
            });

            UI.Add(new CuiElement
            {
                Name = "ClanName",
                Parent = UI_Parent_Main,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#EFEFEFFF"),
                        FontSize = 35,
                        Text = "SAMURAIS <size=15><color=#F7EF45><b>#1</b></color></size>"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.85",
                        AnchorMax = "1 0.92"
                    }
                }
            });

            #endregion

            #region [Members] / [Соклановцы]

            UI.Add(new CuiElement
            {
                Name = "ClanPlayers",
                Parent = UI_Parent_Main,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1",
                        FontSize = 14,
                        Text =
                            "<B>ОБЩИЙ СОСТАВ КЛАНА</B>\n<color=#C0C0C0><size=12>В данном разделе указаны игроки этого клана</size></color>",
                        Font = "robotocondensed-regular.ttf"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.2171884 0.2055642",
                        AnchorMax = "0.7838541 0.2787037"
                    }
                }
            });

            for (int i = 0, x = 0, y = 0; i < 20; i++, x++)
            {
                if (x == 10)
                {
                    x = 0;
                    y--;
                }

                bool IsOnline = Random.Range(0, 50) < 25;

                UI.Add(new CuiElement
                {
                    Name = $"Member{i}",
                    Parent = UI_Parent_Main,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#4038286E"),
                            Sprite = radial,
                            Material = Blur,
                            FadeIn = 0.1f + (i * 0.1f)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.2171907 + (x * 0.057)} {0.109259 + (y * 0.1009259)}",
                            AnchorMax = $"{0.2692742 + (x * 0.057)} {0.2018518 + (y * 0.1009259)}"
                        },
                        new CuiOutlineComponent
                        {
                            Color = IsOnline ? HexToRustFormat("#8ebf5b") : HexToRustFormat("#BF5B5B"),
                            Distance = "0.75 0.75"
                        }
                    }
                });

                if (IsOnline == true)
                {
                    UI.Add(new CuiElement
                    {
                        Name = "MemberIC",
                        Parent = $"Member{i}",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = GetImage(player.UserIDString),
                                FadeIn = 0.1f + (i * 0.1f)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.011 0.011",
                                AnchorMax = "0.989 0.989"
                            }
                        }
                    });
                }
                else
                {
                    UI.Add(new CuiElement
                    {
                        Name = "AddMember",
                        Parent = $"Member{i}",
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Sprite = "assets/icons/add.png",
                                Color = HexToRustFormat("#A0A0A0FF"),
                                FadeIn = 0.1f + (i * 0.1f)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.35 0.35",
                                AnchorMax = "0.65 0.65"
                            }
                        }
                    });
                }
            }

            #endregion

            CuiHelper.AddUi(player, UI);
        }

        #region [Commands] / [Команды]

        [ConsoleCommand("clantest")]
        private void Open_ClanView(ConsoleSystem.Arg args)
        {
            DrawUI_Main(args.Player());
        }

        [ConsoleCommand("clantest.close")]
        private void Close_ClanView(ConsoleSystem.Arg args)
        {
            CuiHelper.DestroyUi(args.Player(), UI_Parent_Main);
        }

        #endregion

        #region [Hooks] / [Крюки]

        private void OnServerInitialized()
        {
            ServerMgr.Instance.StartCoroutine(LoadImages());
            ImageLibrary.Call("AddImage",
                "https://sun1-95.userapi.com/B8BSDTGCj7zGDvvsHk8Q8XFsxAV38_CXeNY42A/0YAk2Mgq_DE.jpg", "Avatar");
            ImageLibrary.Call("AddImage",
                "https://i.imgur.com/VC7DPPR.png", "Button1");
            ImageLibrary.Call("AddImage",
                "https://i.imgur.com/SlaDYPL.png", "Button2");
            ImageLibrary.Call("AddImage",
                "https://i.imgur.com/5FcYUvN.png", "Button3");

            DrawUI_Main(BasePlayer.Find("Kira"));
        }

        #endregion

        #region [Helpers] / [Вспомогательный код]

        IEnumerator LoadImages()
        {
            foreach (var skin in SkinInClans)
            {
                if (skin.SkinID != 0)
                    ImageLibrary.CallHook("AddImage", $"http://rust.skyplugins.ru/getskin/{skin.SkinID}/",
                        $"{skin.ShortName}_{128}_{skin.SkinID}");
                else
                    ImageLibrary.CallHook("AddImage",
                        $"http://rust.skyplugins.ru/getimage/{skin.ShortName}/{128}",
                        $"{skin.ShortName}_{128}_{skin.SkinID}");

                yield return new WaitForSeconds(0.5f);
            }

            foreach (var Resource in Resource)
            {
                ImageLibrary.CallHook("AddImage",
                    $"http://api.hougan.space/rust/item/getImage/{Resource}/{128}",
                    $"{Resource}_{128}");

                yield return new WaitForSeconds(0.5f);
            }
        }

        private string GetImage(string name)
        {
            return (string) ImageLibrary?.Call("GetImage", name) ?? "";
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