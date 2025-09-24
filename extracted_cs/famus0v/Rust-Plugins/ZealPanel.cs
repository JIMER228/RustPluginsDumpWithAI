using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Globalization;
using Oxide.Core;
using Color = UnityEngine.Color;

namespace Oxide.Plugins
{
    [Info("ZealPanel", "Kira", "1.0.0")]
    [Description("Панель онлайна и ивентов для сервера Rust.")]
    public class ZealPanel : RustPlugin
    {
        #region Reference 

        [PluginReference] Plugin ImageLibrary;

        private string GetImg(string name)
        {
            return (string) ImageLibrary?.Call("GetImage", name) ?? "";
        }

        public string GetImage(string shortname, ulong skin = 0) =>
            (string) ImageLibrary?.Call("GetImage", shortname, skin);

        #endregion

        #region Vars
 
        private TOD_Sky Sky;
        private string Sharp = "assets/content/ui/ui.background.tile.psd";
        private string Blur = "assets/content/ui/uibackgroundblur.mat";
        private string currTime;
        int BradleyIC = 0;
        int AirIC = 0;
        int HeliIC = 0;
        int ShipIC = 0;
        
        CuiElementContainer Online = new CuiElementContainer();

        #endregion

        #region Dictionary

        readonly Dictionary<string, Timer> timers = new Dictionary<string, Timer>
        {
            {"clockTimer", null},
            {"events", null}
        };

        Dictionary<string, string> Images = new Dictionary<string, string>
        {
            ["OnlineIcon"] = "https://i.imgur.com/lWoUIw2.png",
            ["MenuIcon"] = "https://i.imgur.com/C9fX540.png",
            ["TimeIcon"] = "https://i.imgur.com/CBKTj8B.png",
            ["AirIC"] = "https://i.imgur.com/CL41EJS.png",
            ["BradleyIC"] = "https://i.imgur.com/j5cMDpt.png",
            ["HeliIC"] = "https://i.imgur.com/HtAifod.png",
            ["ShipIC"] = "https://i.imgur.com/xU8IUWO.png"
        };

        #endregion

        #region GUI

        void MainGUI(BasePlayer player)
        {
            CuiElementContainer GUI = new CuiElementContainer();

            GUI.Add(new CuiElement
            {
                Name = "BoxPanel",
                Parent = "Hud",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0",
                        Material = Sharp,
                        FadeIn = 0.5f
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0",
                        AnchorMax = "0.5 0",
                        OffsetMin = "245 17",
                        OffsetMax = "409 98"
                    }
                }
            });

            GUI.Add(new CuiElement
            {
                Name = "OnlineBG",
                Parent = "BoxPanel",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#B9B0A10E"),
                        Material = Sharp,
                        FadeIn = 0.5f
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1600003 0.0330578",
                        AnchorMax = "0.9800003 0.3160037",
                        OffsetMin = "0 0",
                        OffsetMax = "1 1"
                    }
                }
            });

            GUI.Add(new CuiElement
            {
                Name = "TimeBG",
                Parent = "BoxPanel",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#B9B0A10E"),
                        Material = Sharp,
                        FadeIn = 0.5f
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1600003 0.3553723",
                        AnchorMax = "0.9800003 0.642194",
                        OffsetMin = "0 0",
                        OffsetMax = "1 1"
                    }
                }
            });

            GUI.Add(new CuiElement
            {
                Name = "TimeBGIcon",
                Parent = "BoxPanel",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#B9B0A10E"),
                        Material = Sharp,
                        FadeIn = 0.5f
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.0046667 0.3553723",
                        AnchorMax = "0.1436668 0.6421941",
                        OffsetMin = "0 0",
                        OffsetMax = "1 1"
                    }
                }
            });

            GUI.Add(new CuiElement
            {
                Name = "OnlineBGIcon",
                Parent = "BoxPanel",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#B9B0A10E"),
                        Material = Sharp,
                        FadeIn = 0.5f
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.0046667 0.0330578",
                        AnchorMax = "0.1436668 0.3160037",
                        OffsetMin = "0 0",
                        OffsetMax = "1 1"
                    }
                }
            });

            GUI.Add(new CuiElement
            {
                Name = "MenuBGIcon",
                Parent = "BoxPanel",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#B9B0A10E"),
                        Material = Sharp,
                        FadeIn = 0.5f
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.0046667 0.677687",
                        AnchorMax = "0.1436668 0.958679",
                        OffsetMin = "0 0",
                        OffsetMax = "1 1"
                    }
                }
            });

            GUI.Add(new CuiElement
            {
                Name = "OnlineIcon",
                Parent = "OnlineBGIcon",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Color = HexToRustFormat("#c2b6a6"),
                        Png = GetImage("OnlineIcon"),
                        FadeIn = 0.5f
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1167559 0.08823527",
                        AnchorMax = "0.8520507 0.8235293"
                    }
                }
            });

            GUI.Add(new CuiElement
            {
                Name = "MenuIcon",
                Parent = "MenuBGIcon",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Color = HexToRustFormat("#c2b6a6"),
                        Png = GetImage("MenuIcon"),
                        FadeIn = 0.5f
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.03030304 0.02941176",
                        AnchorMax = "0.9393943 0.9669434"
                    }
                }
            });

            GUI.Add(new CuiButton
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
            }, "MenuIcon", "ButtonMenu");

            GUI.Add(new CuiElement
            {
                Name = "TimeIcon",
                Parent = "TimeBGIcon",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Color = HexToRustFormat("#c2b6a6"),
                        Png = GetImage("TimeIcon"),
                        FadeIn = 0.5f
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1167559 0.08823527",
                        AnchorMax = "0.8520507 0.8235293"
                    }
                }
            });

            GUI.Add(new CuiElement
            {
                Name = "BGEventAir",
                Parent = "BoxPanel",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#B9B0A10E"),
                        Material = Sharp,
                        FadeIn = 0.5f
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.16 0.6776871",
                        AnchorMax = "0.35 0.958679",
                        OffsetMin = "0 0",
                        OffsetMax = "1 1"
                    }
                }
            });

            GUI.Add(new CuiElement
            {
                Name = "BGEventTank",
                Parent = "BoxPanel",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#B9B0A10E"),
                        Material = Sharp,
                        FadeIn = 0.5f
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.362 0.6776871",
                        AnchorMax = "0.562 0.958679",
                        OffsetMin = "0 0",
                        OffsetMax = "1 1"
                    }
                }
            });

            GUI.Add(new CuiElement
            {
                Name = "BGEventHeli",
                Parent = "BoxPanel",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#B9B0A10E"),
                        Material = Sharp,
                        FadeIn = 0.5f
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.576 0.6776871",
                        AnchorMax = "0.77 0.958679",
                        OffsetMin = "0 0",
                        OffsetMax = "1 1"
                    }
                }
            });

            GUI.Add(new CuiElement
            {
                Name = "BGEventShip",
                Parent = "BoxPanel",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#B9B0A10E"),
                        Material = Sharp,
                        FadeIn = 0.5f
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.7840015 0.6776871",
                        AnchorMax = "0.9810017 0.958679",
                        OffsetMin = "0 0",
                        OffsetMax = "1 1"
                    }
                }
            });

            CuiHelper.AddUi(player, GUI);
        }

        void ButtonHide(BasePlayer player)
        {
            CuiElementContainer GUI = new CuiElementContainer();
            CuiHelper.DestroyUi(player, "ButtonOpen");
            CuiHelper.DestroyUi(player, "ButtonHide");
            GUI.Add(new CuiButton
            {
                Button =
                {
                    Color = HexToRustFormat("#446181CA"),
                    Material = Sharp,
                    Command = "chat.say /panelhide"
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = HexToRustFormat("#B9B0A1"),
                    FontSize = 20,
                    Text = ">"
                },
                RectTransform =
                {
                    AnchorMin = "0.8310937 0.02222222",
                    AnchorMax = "0.8454166 0.1342592",
                    OffsetMin = "-15 4",
                    OffsetMax = "-14 -1"
                }
            }, "Hud", "ButtonHide");

            CuiHelper.AddUi(player, GUI);
        }

        void ButtonOpen(BasePlayer player)
        {
            CuiElementContainer GUI = new CuiElementContainer();
            CuiHelper.DestroyUi(player, "ButtonOpen");
            CuiHelper.DestroyUi(player, "ButtonHide");
            GUI.Add(new CuiButton
            {
                Button =
                {
                    Color = HexToRustFormat("#446181CA"),
                    Material = Sharp,
                    Command = "chat.say /panelopen"
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = HexToRustFormat("#B9B0A1"),
                    FontSize = 20,
                    Text = "<"
                },
                RectTransform =
                {
                    AnchorMin = "0.8310937 0.02222222",
                    AnchorMax = "0.8454166 0.1342592",
                    OffsetMin = "-15 4",
                    OffsetMax = "-14 -1"
                }
            }, "Hud", "ButtonOpen");

            CuiHelper.AddUi(player, GUI);
        }

        #endregion 

        #region Hooks

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity is BradleyAPC)
            {
                BradleyIC++;
            }

            if (entity is CargoPlane)
            {
                AirIC++;
            }

            if (entity is CargoShip)
            {
                ShipIC++;
            }

            if (entity is BaseHelicopter)
            {
                HeliIC++;
            }
        }

        void OnEntityKill(BaseNetworkable entity)
        {
            if (entity is BradleyAPC)
            {
                BradleyIC--;
            }

            if (entity is SupplyDrop)
            {
                AirIC--;
            }

            if (entity is CargoShip)
            {
                ShipIC--;
            }

            if (entity is BaseHelicopter)
            {
                HeliIC--;
            }
        }

        private void OnServerInitialized()
        {
            Sky = TOD_Sky.Instance;

            if (!ImageLibrary)
            {
                PrintError("На сервере не установлена ImageLibrary, плагин выгружен");
                Interface.Oxide.UnloadPlugin(Title);
            }
            else
            {
                Puts("Плагин удачно загружен !");
                Puts("Приятного пользования ^.^");
            }

            BuildUIAll();
            foreach (var Image in Images) ImageLibrary.Call("AddImage", Image.Value, Image.Key);
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (!player.IsConnected) return;
             if (player.IsReceivingSnapshot)
            {
                timer.Once(2, () => PanelOpen(player));
            }
        }

        void Launch(BasePlayer player)
        {
            timer.Every(1f, () =>
            {
                CuiElementContainer GUI = new CuiElementContainer();
                CuiHelper.DestroyUi(player, "AirIC");
                if (AirIC != 0)
                {
                    GUI.Add(new CuiElement
                    {
                        Name = "AirIC",
                        Parent = "BGEventAir",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Color = HexToRustFormat("#DF9948"),
                                Png = GetImage("AirIC")
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.05 0.1",
                                AnchorMax = "0.9 0.9"
                            }
                        }
                    });
                }
                else
                {
                    GUI.Add(new CuiElement
                    {
                        Name = "AirIC",
                        Parent = "BGEventAir",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Color = HexToRustFormat("#c2b6a6"),
                                Png = GetImage("AirIC")
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.05 0.1",
                                AnchorMax = "0.9 0.9"
                            }
                        }
                    });
                }

                CuiHelper.DestroyUi(player, "BradleyIC");
                if (BradleyIC != 0)
                {
                    GUI.Add(new CuiElement
                    {
                        Name = "BradleyIC",
                        Parent = "BGEventTank",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Color = HexToRustFormat("#DF9948"),
                                Png = GetImage("BradleyIC")
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.05 0.1",
                                AnchorMax = "0.9 0.9"
                            }
                        }
                    });
                }
                else
                {
                    GUI.Add(new CuiElement
                    {
                        Name = "BradleyIC",
                        Parent = "BGEventTank",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Color = HexToRustFormat("#c2b6a6"),
                                Png = GetImage("BradleyIC")
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.05 0.1",
                                AnchorMax = "0.9 0.9"
                            }
                        }
                    });
                }

                CuiHelper.DestroyUi(player, "HeliIC");
                if (HeliIC != 0)
                {
                    GUI.Add(new CuiElement
                    {
                        Name = "HeliIC",
                        Parent = "BGEventHeli",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Color = HexToRustFormat("#DF9948"),
                                Png = GetImage("HeliIC")
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.05 0.1",
                                AnchorMax = "0.9 0.9"
                            }
                        }
                    });
                }
                else
                {
                    GUI.Add(new CuiElement
                    {
                        Name = "HeliIC",
                        Parent = "BGEventHeli",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Color = HexToRustFormat("#c2b6a6"),
                                Png = GetImage("HeliIC")
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.05 0.1",
                                AnchorMax = "0.9 0.9"
                            }
                        }
                    });
                }

                CuiHelper.DestroyUi(player, "ShipIC");
                if (ShipIC != 0)
                {
                    GUI.Add(new CuiElement
                    {
                        Name = "ShipIC",
                        Parent = "BGEventShip",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Color = HexToRustFormat("#DF9948"),
                                Png = GetImage("ShipIC")
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.05 0.1",
                                AnchorMax = "0.9 0.9"
                            }
                        }
                    });
                }
                else
                {
                    GUI.Add(new CuiElement
                    {
                        Name = "ShipIC",
                        Parent = "BGEventShip",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Color = HexToRustFormat("#c2b6a6"),
                                Png = GetImage("ShipIC")
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.05 0.1",
                                AnchorMax = "0.9 0.9"
                            }
                        }
                    });
                }

                var timeDay = Sky.Cycle?.DateTime.ToString("HH:mm");
                if (timeDay == null) return;
                currTime = timeDay;

                var timeHour = Sky.Cycle.DateTime.Hour;
                int timeHours = timeHour;

                var timeMinutes = Sky.Cycle.DateTime.Minute;
                int timeMin = timeMinutes;

                var result = (double) 1 / 1440;
                result = result - 0.00001;
                timeMin = timeMin + (60 * timeHours);
                CuiHelper.DestroyUi(player, "TimeProgress");
                GUI.Add(new CuiElement
                {
                    Name = "TimeProgress",
                    Parent = "TimeBG",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#444881FF"),
                            Material = Sharp
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0} {0.08823527}",
                            AnchorMax = $"{0 + (result * timeMin)} {0.8823528}"
                        }
                    }
                });
                CuiHelper.DestroyUi(player, "TimeValue");
                GUI.Add(new CuiElement
                {
                    Name = "TimeValue",
                    Parent = "TimeBG",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#DEDEDE"),
                            FontSize = 20,
                            Text = currTime
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    }
                });
                var math = (double) 1 / ConVar.Server.maxplayers;
                var o = math - 0.00003;
                int x = Player.Players.Count;
                CuiHelper.DestroyUi(player, "OnlineValue");
                GUI.Add(new CuiElement
                {
                    Name = "OnlineValue",
                    Parent = "BoxPanel",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            Color = HexToRustFormat("#DEDEDE"),
                            FontSize = 14,
                            Text = (x + "/" + ConVar.Server.maxplayers)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.1840004 0.0330578",
                            AnchorMax = "0.9800003 0.3140503"
                        }
                    }
                });
                CuiHelper.DestroyUi(player, "Online");
                GUI.Add(new CuiElement
                {
                    Name = "Online",
                    Parent = "OnlineBG",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#4598d0"),
                            Material = Sharp
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0} {0.08823527}",
                            AnchorMax = $"{0 + (o * x)} {0.8823528}"
                        }
                    }
                });

                CuiHelper.AddUi(player, GUI);
            });

            ButtonOpen(player);
        }

        private static void DestroyTimers(IEnumerable<Timer> dtimers)
        {
            foreach (var tmr in dtimers)
            {
                if (tmr == null || tmr.Destroyed) return;
                tmr.Destroy();
            }
        }

        private static List<BasePlayer> GetActivePlayers()
        {
            var ret = new List<BasePlayer>();

            foreach (var p in BasePlayer.activePlayerList)
                if (p.IsValid() && p.net.connection != null)
                    ret.Add(p);

            return ret;
        }

        static void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "BoxPanel");
            CuiHelper.DestroyUi(player, "ButtonOpen");
            CuiHelper.DestroyUi(player, "ButtonHide");
        }

        void BuildUIAll(BasePlayer pl = null)
        {
            if (pl == null)
                foreach (var player in GetActivePlayers())
                {
                    DestroyUI(player);
                    DestroyTimers(timers.Values.ToList());
                    ButtonOpen(player);
                    Launch(player);
                }
        }

        private static void DestroyUIAll(BasePlayer pl = null)
        {
            if (pl == null)
                foreach (var player in GetActivePlayers())
                    DestroyUI(player);
            else
                DestroyUI(pl);
        }

        void Unload()
        {
            
            DestroyTimers(timers.Values.ToList());
            DestroyUIAll();
        }

        #endregion

        #region ChatCommand

        [ChatCommand("panelhide")]
        void PanelHide(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "BoxPanel");
            ButtonOpen(player);
        }

        [ChatCommand("panelopen")]
        private void PanelOpen(BasePlayer player)
        {
            MainGUI(player);
            ButtonHide(player);
        }

        #endregion

        #region Helpers

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