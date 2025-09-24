using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Globalization;
using WebSocketSharp;
using Color = UnityEngine.Color;

namespace Oxide.Plugins
{
    [Info("ZealTimeManager", "Kira", "1.0.0")]
    [Description("Управление днем и ночью")]
    class ZealTimeManager : RustPlugin
    {
        #region [Reference] / [Запросы]
  
        [PluginReference] Plugin ImageLibrary;

        private string GetImg(string name)
        {
            return (string) ImageLibrary?.Call("GetImage", name) ?? "";
        } 
 
        #endregion   
 
        #region [Dictionary/Vars] / [Словари/Переменные]

        private TOD_Sky Sky;
        private StoredData DataBase = new StoredData();
        private string Sharp = "assets/content/ui/ui.background.tile.psd";
        private string Blur = "assets/content/ui/uibackgroundblur.mat";
        private string radial = "assets/content/ui/ui.background.transparent.radial.psd";
        private string regular = "robotocondensed-regular.ttf";
        private string bold = "robotocondensed-bold.ttf";

        private string Layer = "BoxTimeManager";

        public bool VoteRunning;
        public int CountVoteYes;
        public int CountVoteNo;

        public Timer CheckTime;

        private List<ulong> VotedPlayers = new List<ulong>();

        #endregion

        #region [DrawUI] / [Показ UI]

        private void DrawManagePanel(BasePlayer player)
        {
            CuiElementContainer Gui = new CuiElementContainer();
            CuiHelper.DestroyUi(player, Layer);
            double part = (double) 1 / 100;
            int TotalDay = DataBase.TimeManagerStatistics.SkipedDays + DataBase.TimeManagerStatistics.NotSkipedDays;
            int TotalVoted = DataBase.TimeManagerStatistics.VoteSkipDay + DataBase.TimeManagerStatistics.NotVoteSkipDay;
            double percent_notskipday = MathPercent(DataBase.TimeManagerStatistics.NotSkipedDays, TotalDay);
            double percent_skipday = MathPercent(DataBase.TimeManagerStatistics.SkipedDays, TotalDay);
            double percent_votenotskipday = MathPercent(DataBase.TimeManagerStatistics.NotVoteSkipDay, TotalVoted);
            double percent_voteskipday = MathPercent(DataBase.TimeManagerStatistics.VoteSkipDay, TotalVoted);

            Gui.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image =
                {
                    Color = HexToRustFormat("#000000F9"),
                    Material = Blur,
                    Sprite = radial
                }, 
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, "Overlay", Layer);

            Gui.Add(new CuiButton
            {
                Button =
                {
                    Command = "close.timemanager",
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
            }, Layer, "CloseManagePanel");

            Gui.Add(new CuiElement
            {
                Name = "Zagolovok",
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 35,
                        Text = "ПАНЕЛЬ УПРАВЛЕНИЯ : <b>TIME MANAGER</b>",
                        Font = regular
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.9213709",
                        AnchorMax = "1 1"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "ZagolovokChangeTimeSet",
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 19,
                        Text = "УСТАНОВИ ВРЕМЯ ЗАПУСКА ГОЛОСОВАНИЯ ЗА ПРОПУСК НОЧИ",
                        Font = regular
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.2749993 0.8898146",
                        AnchorMax = "0.7255208 0.9259257"
                    }
                }
            });

            #region [Статистика пропущенных дней]

            Gui.Add(new CuiElement
            {
                Name = "ZagStatisticSkipDay",
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 20,
                        Text = "СТАТИСТИКА ПРОПУЩЕННЫХ ДНЕЙ"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.01302071 0.1648148",
                        AnchorMax = "0.271875 0.2064815"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "NotSkipedDay",
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 14,
                        Text = "НЕ ПРОПУЩЕННЫЙ ДЕНЬ",
                        Font = regular
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.0291665 0.126852",
                        AnchorMax = "0.2703125 0.1500001"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "SkipedDay",
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 14,
                        Text = "ПРОПУЩЕННЫЙ ДЕНЬ",
                        Font = regular
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.0291665 0.09629633",
                        AnchorMax = "0.2703125 0.1194445"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "MarkerNotSkipDay",
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#5EB96AFF"),
                        Material = Sharp
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.01302082 0.126852",
                        AnchorMax = "0.02604165 0.1500001"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "MarkerSkipDay",
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#B95E5EFF"),
                        Material = Sharp
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.01302082 0.09629633",
                        AnchorMax = "0.02604165 0.1194445"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "BoxPercentSkipDays",
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.01302071 0.06018525",
                        AnchorMax = "0.271875 0.08240744"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "NotSkipDays",
                Parent = "BoxPercentSkipDays",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#5EB96AFF"),
                        Material = Sharp
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"0 0",
                        AnchorMax = $"{percent_notskipday * part} 1"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "TXTPercentNotSkipDay",
                Parent = "NotSkipDays",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#297D34FF"),
                        FontSize = 14,
                        Text = $"{Convert.ToInt32(percent_notskipday)} %"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 0.9"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "SkipDays",
                Parent = "BoxPercentSkipDays",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#B95E5EFF"),
                        Material = Sharp
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"{percent_notskipday * part} 0",
                        AnchorMax = $"1 1"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "TXTPercentSkipDay",
                Parent = "SkipDays",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#873131FF"),
                        FontSize = 14,
                        Text = $"{Convert.ToInt32(percent_skipday)} %"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 0.9"
                    }
                }
            });

            #endregion

            #region [Статистика голосов]

            Gui.Add(new CuiElement
            {
                Name = "ZagStatisticVotedPlayers",
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 20,
                        Text = "СТАТИСТИКА ГОЛОСОВ ИГРОКОВ"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.731765 0.1648148",
                        AnchorMax = "0.9906188 0.2064815"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "VoteNotSkipedDay",
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleRight,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 14,
                        Text = "ГОЛОС ЗА ОТМЕНУ ПРОПУСКА НОЧИ",
                        Font = regular
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.7328125 0.126852",
                        AnchorMax = "0.9734266 0.1500001"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "VoteSkipedDay",
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleRight,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 14,
                        Text = "ГОЛОС ЗА ПРОПУСК НОЧИ",
                        Font = regular
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.7328125 0.09629633",
                        AnchorMax = "0.9734266 0.1194445"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "MarkerVoteNotSkipDay",
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#5EB96AFF"),
                        Material = Sharp
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.9781154 0.126852",
                        AnchorMax = "0.9911358 0.1500001"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "MarkerVoteSkipDay",
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#B95E5EFF"),
                        Material = Sharp
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.9781154 0.09629633",
                        AnchorMax = "0.9911358 0.1194445"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "BoxPercentVoteSkipDays",
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.731765 0.06018525",
                        AnchorMax = "0.9906188 0.08240744"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "VoteNotSkipDays",
                Parent = "BoxPercentVoteSkipDays",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#5EB96AFF"),
                        Material = Sharp
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"0 0",
                        AnchorMax = $"{percent_votenotskipday * part} 1"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "TXTPercentVoteNotSkipDay",
                Parent = "VoteNotSkipDays",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#297D34FF"),
                        FontSize = 14,
                        Text = $"{Convert.ToInt32(percent_votenotskipday)} %"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 0.9"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "VoteSkipDays",
                Parent = "BoxPercentVoteSkipDays",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#B95E5EFF"),
                        Material = Sharp
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"{percent_votenotskipday * part} 0",
                        AnchorMax = $"1 1"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "TXTPercentVoteSkipDay",
                Parent = "VoteSkipDays",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#873131FF"),
                        FontSize = 14,
                        Text = $"{Convert.ToInt32(percent_voteskipday)} %"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 0.9"
                    }
                }
            });

            #endregion

            CuiHelper.AddUi(player, Gui);
            DrawManageTimeSet(player);
        }

        private void DrawManageTimeSet(BasePlayer player)
        {
            CuiElementContainer Gui = new CuiElementContainer();
            CuiHelper.DestroyUi(player, "BoxTimeSet");

            #region [Панель управления времени запуска голосования]

            Gui.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image =
                {
                    Color = HexToRustFormat("#00000086")
                },
                RectTransform =
                {
                    AnchorMin = "0.241146 0.7944443",
                    AnchorMax = "0.759375 0.8296295"
                }
            }, Layer, "BoxTimeSet");

            for (int num = 1, x = 0; num <= 24; num++, x++)
            {
                string buttoncolor = GetColorGradient(0.69, 0.83, 0.98, 0.5, 0.013, 0.031, 0.01, 0.0155, num);
                string textcolor = GetColorGradient(1, 1, 1, 1, 0.01, 0.01, 0.01, 0.05, x);
                string stylefont = regular;
                double numberbut = TimeSpan.Parse(DataBase.TimeManagerCFG.TimeVoteStart).TotalHours;
                if (numberbut == 0.0)
                {
                    numberbut = 24;
                }

                if (num == numberbut)
                {
                    stylefont = bold;
                }

                Gui.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = $"settime {num}",
                            Color = buttoncolor,
                            Material = Sharp
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = textcolor,
                            FontSize = 13,
                            Text = $"{num}",
                            Font = stylefont
                        },
                        RectTransform =
                        {
                            AnchorMin = $"{0.002 + (x * 0.04154)} {0.05263153}",
                            AnchorMax = $"{0.041 + (x * 0.04154)} {0.9302}"
                        }
                    }, "BoxTimeSet", "Hour" + num);

                if (num == numberbut)
                {
                    Gui.Add(new CuiElement
                    {
                        Name = "MarkerHour",
                        Parent = "Hour" + numberbut,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = HexToRustFormat("#5EB96AFF"),
                                Material = Sharp
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "0.961215 0.1"
                            }
                        }
                    });

                    Gui.Add(new CuiElement
                    {
                        Name = "MarkerSetTime1",
                        Parent = "BoxTimeSet",
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = HexToRustFormat("#5EB96AFF"),
                                Material = Sharp
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0.02010097 + (x * 0.0415195)} {-0.6}",
                                AnchorMax = $"{0.02412124 + (x * 0.0415195)} {0.05}"
                            }
                        }
                    });

                    Gui.Add(new CuiElement
                    {
                        Name = "MarkerSetTime2",
                        Parent = "BoxTimeSet",
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = HexToRustFormat("#5EB96AFF"),
                                Material = Sharp
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{-0.0160807 + (x * 0.0415195)} {-0.6}",
                                AnchorMax = $"{0.02412124 + (x * 0.0415195)} {-0.5263117}"
                            }
                        }
                    });

                    Gui.Add(new CuiElement
                    {
                        Name = "MarkerSetTime3",
                        Parent = "BoxTimeSet",
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = HexToRustFormat("#5EB96AFF"),
                                Material = Sharp
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{-0.01608058 + (x * 0.0415195)} {-0.7894679}",
                                AnchorMax = $"{-0.01206048 + (x * 0.0415195)} {-0.6315733}"
                            }
                        }
                    });

                    Gui.Add(new CuiElement
                    {
                        Name = "SetTimeDescription",
                        Parent = "BoxTimeSet",
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Align = TextAnchor.MiddleCenter,
                                Color = HexToRustFormat("#EAEAEAFF"),
                                FontSize = 10,
                                Text = "ВРЕМЯ НАЧАЛА ГОЛОСОВАНИЯ",
                                Font = regular
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{-0.1256284 + (x * 0.0415195)} {-1.736836}",
                                AnchorMax = $"{0.09849225 + (x * 0.0415195)} {-0.5}"
                            },
                            new CuiOutlineComponent
                            {
                                Distance = "0.3 0.3",
                                Color = HexToRustFormat("#0000007C")
                            }
                        }
                    });
                }

                if (num == numberbut + 1)
                {
                    Gui.Add(new CuiElement
                    {
                        Name = "MarkerHour2",
                        Parent = "Hour" + numberbut + 1,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = HexToRustFormat("#5EB96AFF"),
                                Material = Sharp
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "0.961215 0.1"
                            }
                        }
                    });

                    Gui.Add(new CuiElement
                    {
                        Name = "MarkerSetTime4",
                        Parent = "BoxTimeSet",
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = HexToRustFormat("#5EB96AFF"),
                                Material = Sharp
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0.0613065 + (x * 0.0415195)} {0.9526329}",
                                AnchorMax = $"{0.0653267 + (x * 0.0415195)} {1.602632}"
                            }
                        }
                    });

                    Gui.Add(new CuiElement
                    {
                        Name = "MarkerSetTime5",
                        Parent = "BoxTimeSet",
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = HexToRustFormat("#5EB96AFF"),
                                Material = Sharp
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0.06130625 + (x * 0.0415195)} {1.531581}",
                                AnchorMax = $"{0.1015076 + (x * 0.0415195)} {1.605268}"
                            }
                        }
                    });

                    Gui.Add(new CuiElement
                    {
                        Name = "MarkerSetTime6",
                        Parent = "BoxTimeSet",
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = HexToRustFormat("#5EB96AFF"),
                                Material = Sharp
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0.09748719 + (x * 0.0415195)} {1.631587}",
                                AnchorMax = $"{0.1015073 + (x * 0.0415195)} {1.789481}"
                            }
                        }
                    });

                    Gui.Add(new CuiElement
                    {
                        Name = "SetTimeDescription",
                        Parent = "BoxTimeSet",
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Align = TextAnchor.MiddleCenter,
                                Color = HexToRustFormat("#EAEAEAFF"),
                                FontSize = 10,
                                Text = "ВРЕМЯ НАЧАЛА ГОЛОСОВАНИЯ",
                                Font = regular
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{-0.1256284 + (x * 0.0415195)} {-1.736836}",
                                AnchorMax = $"{0.09849225 + (x * 0.0415195)} {-0.5}"
                            },
                            new CuiOutlineComponent
                            {
                                Distance = "0.3 0.3",
                                Color = HexToRustFormat("#0000007C")
                            }
                        }
                    });
                }
            }

            #endregion

            CuiHelper.AddUi(player, Gui);
        }

        private void DrawVote(BasePlayer player)
        {
            CuiElementContainer Gui = new CuiElementContainer();
            const string layerVoting = "BoxVoting";
            CuiHelper.DestroyUi(player, layerVoting);
            var ColorBG = "#8D847D61";
            var text1 = DataBase.TimeManagerMessageCFG.TextYes;
            var text2 = DataBase.TimeManagerMessageCFG.TextNo;
            var command1 = "vote yes";
            var command2 = "vote no";
            if (VotedPlayers.Contains(player.userID))
            {
                text1 = $"{CountVoteYes}";
                text2 = $"{CountVoteNo}";
                command1 = " ";
                command2 = " ";
            }
 
            Gui.Add(new CuiElement
            {
                Name = "BoxVoting",
                Parent = "Overlay",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.2213538 0.02314814",
                        AnchorMax = "0.3151045 0.1111111"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "ZagBG",
                Parent = layerVoting,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat(ColorBG)
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.5888886",
                        AnchorMax = "1 1"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "ZagTXT",
                Parent = "ZagBG",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#8D847DAB"),
                        FontSize = 16,
                        Text = "ПРОПУСК НОЧИ"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            Gui.Add(new CuiButton
            {
                Button =
                {
                    Command = command1,
                    Color = HexToRustFormat(ColorBG)
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = HexToRustFormat(DataBase.TimeManagerMessageCFG.ColorYes),
                    FontSize = 25,
                    Text = text1
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "0.4888889 0.5257143"
                }
            }, layerVoting, "Button.Yes");

            Gui.Add(new CuiButton
            {
                Button =
                {
                    Command = command2,
                    Color = HexToRustFormat(ColorBG)
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = HexToRustFormat(DataBase.TimeManagerMessageCFG.ColorNo),
                    FontSize = 25,
                    Text = text2
                },
                RectTransform =
                {
                    AnchorMin = "0.5179037 0",
                    AnchorMax = "1 0.5257143"
                }
            }, layerVoting, "Button.No");

            CuiHelper.AddUi(player, Gui);
        }

        #endregion
 
        #region [ChatCommand] / [Чат команды]

        [ChatCommand("timemanager")]
        private void OpenManagePanel(BasePlayer player)
        {
            if (!player) return;
            DrawManagePanel(player);
        }

        [ConsoleCommand("close.timemanager")]
        private void CloseManagePanel(ConsoleSystem.Arg args)
        {
            if (!args.Player()) return;
            CuiHelper.DestroyUi(args.Player(), Layer);
        }

        [ConsoleCommand("settime")]
        private void ChangeVoteTimeStart(ConsoleSystem.Arg args)
        {
            if (!args.Player() & !args.Player().IsAdmin) return;
            int time = Convert.ToInt32(args.Args[0]);
            if (time == 24)
            {
                time = 0;
            }

            DataBase.TimeManagerCFG.TimeVoteStart = $"{time}:00";
            SaveData();
            DrawManageTimeSet(args.Player());
        }

        [ConsoleCommand("vote")]
        private void VoteYes(ConsoleSystem.Arg args)
        {
            if (!args.Player()) return;
            if (!VoteRunning) return;
            BasePlayer player = args.Player();
            if (VotedPlayers.Contains(player.userID))
            {
                SendReply(player, "Вы уже голосовали за пропуск ночи");
                return;
            }

            switch (args.Args[0])
            {
                case "yes":
                    CountVoteYes++;
                    DataBase.TimeManagerStatistics.VoteSkipDay++;
                    VotedPlayers.Add(player.userID);
                    SendReply(player,
                        $"<size=16>[<color={DataBase.TimeManagerMessageCFG.ColorPrefix}> {DataBase.TimeManagerMessageCFG.Prefix} </color>]</size>\n\n" +
                        $"Вы проголосовали за пропуск ночи");
                    DrawVote(player);
                    break;
                case "no":
                    CountVoteNo++;
                    DataBase.TimeManagerStatistics.NotVoteSkipDay++;
                    VotedPlayers.Add(player.userID);
                    SendReply(player,
                        $"<size=16>[<color={DataBase.TimeManagerMessageCFG.ColorPrefix}> {DataBase.TimeManagerMessageCFG.Prefix} </color>]</size>\n\n" +
                        $"Вы проголосовали за отмену пропуска ночи");
                    DrawVote(player);
                    break;
            }

            SaveData();
        }

        #endregion

        #region [Hooks] / [Крюки]

        private void OnServerInitialized()
        {
            LoadData();
            Sky = TOD_Sky.Instance;
            CheckTime = timer.Every(DataBase.TimeManagerCFG.CheckTime, () =>
            {
                if (Sky.Cycle.Hour >= (float) TimeSpan.Parse(DataBase.TimeManagerCFG.TimeVoteStart).TotalHours &
                    Sky.Cycle.Hour <= (float) TimeSpan.Parse(DataBase.TimeManagerCFG.TimeVoteEnd).TotalHours)
                {
                    StartVoting();
                }
            });
        }

        void Unload()
        {
            SaveData();
            foreach (BasePlayer p in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(p, "BoxVoting");
            }

            VotedPlayers.Clear();
        }

        #endregion

        #region [DataBase] / [Хранение данных]

        class StoredData
        {
            [JsonProperty(PropertyName = "Настройка системной части")]
            public TimeManager TimeManagerCFG = new TimeManager();

            [JsonProperty(PropertyName = "Настройка сообщений")]
            public TimeManagerMessage TimeManagerMessageCFG = new TimeManagerMessage();

            [JsonProperty(PropertyName = "Статистика")]
            public TimeManagerStatistic TimeManagerStatistics = new TimeManagerStatistic();

            public class TimeManager
            {
                [JsonProperty(PropertyName = "Время продолжительности голосования")]
                public int VoteTime = 60;

                [JsonProperty(PropertyName = "Период проверки на время (Чем меньше период, тем выше нагрузка.)")]
                public int CheckTime = 30;

                [JsonProperty(PropertyName = "Время начала голосования")]
                public string TimeVoteStart = "20:00";

                [JsonProperty(PropertyName = "Время конца голосования")]
                public string TimeVoteEnd = "21:00";

                [JsonProperty(PropertyName = "Процент проголосовавших для пропуска ночи")]
                public int PercentVotedYes = 60;

                [JsonProperty(PropertyName = "Установить время после голосования")]
                public string SetTime = "8:00";
            }

            public class TimeManagerMessage
            {
                [JsonProperty(PropertyName = "Префикс плагина")]
                public string Prefix = "TimeManager";

                [JsonProperty(PropertyName = "Цвет префикса")]
                public string ColorPrefix = "#BA3737FF";

                [JsonProperty(PropertyName = "Цвет кнопки [✔]")]
                public string ColorYes = "#8D847DAB";

                [JsonProperty(PropertyName = "Цвет кнопки [✖]")]
                public string ColorNo = "#8D847DAB";

                [JsonProperty(PropertyName = "Текст кнопки [✔]")]
                public string TextYes = "✔";

                [JsonProperty(PropertyName = "Текст кнопки [✖]")]
                public string TextNo = "✖";

                [JsonProperty(PropertyName =
                    "Сообщение после голосования, если ночь была пропущена (Для показа % проголосовавших за пропуск ночи : {VotedYes})")]
                public string MsgSkipNight = "По результатам голосования, ночь будет пропущена\n" +
                                             "Количество голосов за пропуск ночи : {VotedYes} %";

                [JsonProperty(PropertyName = "Сообщение после голосования, если ночь не была пропущена")]
                public string MsgNotSkipNight = "По результатам голосования, ночь не будет пропущена";
            }

            public class TimeManagerStatistic
            {
                [JsonProperty(PropertyName = "Кол-во пропущенных ночей")]
                public int SkipedDays = 1;

                [JsonProperty(PropertyName = "Кол-во не пропущенных ночей")]
                public int NotSkipedDays = 1;

                [JsonProperty(PropertyName = "Кол-во голосов за пропуск ночи")]
                public int VoteSkipDay = 1;

                [JsonProperty(PropertyName = "Кол-во голосов за отмену пропуска ночи")]
                public int NotVoteSkipDay = 1;
            }
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, DataBase);

        private void LoadData()
        {
            try
            {
                DataBase = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch (Exception e)
            {
                DataBase = new StoredData();
            }
        }

        #endregion

        #region [Helpers] / [Вспомогательный код]

        private static string GetColorGradient(double r, double g, double b, double a, double rcof, double gcof, double bcof,
            double acof, int number)
        {
            var rmath = r - (number * rcof);
            var gmath = g - (number * gcof);
            var bmath = b - (number * bcof);
            var amath = a + (number * acof);
            return $"{rmath} {gmath} {bmath} {amath}";
        }

        private void StartVoting()
        {
            if (VoteRunning == true) return;
            VoteRunning = true;
            foreach (var plobj in BasePlayer.activePlayerList)
            {
                DrawVote(plobj);
            }

            timer.Once(DataBase.TimeManagerCFG.VoteTime, () =>
            {
                SkipNight(CountVoteYes, CountVoteNo);
            });
        }

        private void SkipNight(int votedyes, int votedno)
        {
            var TimeManagerCFG = DataBase.TimeManagerCFG;
            var TimeManagerMessageCFG = DataBase.TimeManagerMessageCFG;

            if (votedyes >= votedno)
            {
                Sky.Cycle.Hour = (float) TimeSpan.Parse(TimeManagerCFG.SetTime).TotalHours;
                foreach (var plobj in BasePlayer.activePlayerList)
                {
                    PrintToChat(plobj,
                        $"<size=16>[ <color={TimeManagerMessageCFG.ColorPrefix}>{TimeManagerMessageCFG.Prefix}</color> ]</size>\n\n{TimeManagerMessageCFG.MsgSkipNight.Replace("{VotedYes}", $"{votedyes}")}");

                    CuiHelper.DestroyUi(plobj, "BoxVoting");
                }

                DataBase.TimeManagerStatistics.SkipedDays++; 
                VoteRunning = false;
                VotedPlayers.Clear();
                CountVoteNo = 0;
                CountVoteYes = 0;
            }
            else
            {
                Sky.Cycle.Hour = (float) TimeSpan.Parse(TimeManagerCFG.TimeVoteEnd).TotalHours;
                foreach (var plobj in BasePlayer.activePlayerList)
                {
                    PrintToChat(plobj,
                        $"<size=16>[ <color={TimeManagerMessageCFG.ColorPrefix}>{TimeManagerMessageCFG.Prefix}</color> ]</size>\n\n{TimeManagerMessageCFG.MsgNotSkipNight}");

                    CuiHelper.DestroyUi(plobj, "BoxVoting");
                }

                DataBase.TimeManagerStatistics.NotSkipedDays++;
                VoteRunning = false;
                VotedPlayers.Clear();
                CountVoteNo = 0;
                CountVoteYes = 0;
            }

            SaveData();
        }

        private static double MathPercent(int number1, int totalnumber)
        {
            var percent = (double) number1 / totalnumber * 100;
            return percent;
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