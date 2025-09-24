using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Facepunch;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ZealModerations", "Kira", "1.0.5")]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ZealModerations : RustPlugin
    {
        #region [References] / [Ссылки]
 
        [PluginReference] private Plugin ImageLibrary, ZealStatisticsReborn;
        private static ZealModerations _;

        #endregion 
 
        #region [Vars] / [Переменные]

        private StoredData _dataBase = new StoredData();
        private const string UILayerModeratorPanel = "UI_Layer_ModeratorPanel";
        private const string UILayerPlayerList = "UI_Layer_ModeratorPanel_PlayerList";
        private const string Sharp = "assets/content/ui/ui.background.tile.psd";
        private const string Blur = "assets/content/ui/uibackgroundblur.mat";
        private const string BlurInMenu = "assets/content/ui/uibackgroundblur-ingamemenu.mat";
        private const string Radial = "assets/content/ui/ui.background.transparent.radial.psd";
        private const string Regular = "robotocondensed-regular.ttf";

        private const int MAXReports = 9;

        #endregion

        #region [Dictionaries | Lists] / [Словари | Списки]

        private readonly Dictionary<BasePlayer, Check> _checks = new Dictionary<BasePlayer, Check>();

        private static readonly Dictionary<string, Verdict> Verdicts = new Dictionary<string, Verdict>
        {
            ["CHEATS"] = new Verdict
            {
                Name = "ЗАПРЕЩЁННОЕ ПО",
                BanDays = 999
            },
            ["PLAYING_WITH_CHEATER"] = new Verdict
            {
                Name = "ИГРА С ЧИТЕРОМ",
                BanDays = 30
            },
            ["REFUSAL_TO_CHECK"] = new Verdict
            {
                Name = "ОТКАЗ ОТ ПРОВЕРКИ",
                BanDays = 30
            },
            ["CLEAR"] = new Verdict
            {
                Name = "ИГРОК ЧИСТ",
                BanDays = 0
            }
        };

        private static readonly Dictionary<string, string> Menu = new Dictionary<string, string>
        {
            ["CHECK"] = "assets/icons/bullet.png",
            ["FAVORITE"] = "assets/icons/sign.png"
        };

        #endregion

        #region [Lang]

        protected override void LoadDefaultMessages()
        {
            var ru = new Dictionary<string, string>
            {
                ["CHECK_STATUS"] = "СОСТОЯНИЕ ПРОВЕРКИ",
                ["CHECK_DISCORD_REMAINING"] = "Вам осталось {0} с либо вам будет засчитан отказ от проверки",
                ["MODERATOR_DISCONNECTED"] = "Модератор отключился, у него осталось {0} для переподключения",
                ["VERIFIABLE_DISCONNECTED"] = "Проверяемый игрок отключился, у него осталось {0} для переподключения",
                ["CHECK_CALLED"] = "Вас вызвали на проверку",
                ["INPUT_DISCORD"] = "В данном поле введите пожалуйста ваш дискорд (NAME#ID)"
            };

            var en = new Dictionary<string, string>
            {
                ["CHECK_STATUS"] = "CHECK STATUS",
                ["CHECK_DISCORD_REMAINING"] = "You have {0} left or you will be given a refusal to check",
                ["MODERATOR_DISCONNECTED"] = "The moderator disconnected, he still has {0} to reconnect",
                ["VERIFIABLE_DISCONNECTED"] = "The checked player disconnected, he has {0} left to reconnect",
                ["CHECK_CALLED"] = "You have been called for a check",
                ["INPUT_DISCORD"] = "In this field, please enter your discord (NAME#ID)"
            };
            lang.RegisterMessages(ru, this, "ru");
            lang.RegisterMessages(en, this);
        }

        #endregion

        #region [Classes] / [Классы]

        private class Verdict
        {
            public string Name;
            public int BanDays;
        }

        #endregion

        #region [MonoBehaviours]

        public class Check : MonoBehaviour
        {
            #region [Fields]

            public BasePlayer moderator;
            public BasePlayer verifiablePlayer;
            public Verifiable verifiable;

            public bool moderatorIsOnline = true;
            public bool verifiableIsOnline = true;
            public bool inputDiscord;
            private bool _verdictisOpen;

            private const string UILayerCheckInput = "UI_Layer_CheckInput";
            private const string UILayerCheckStatus = "UI_Layer_CheckStatus";
            private const string Online = "assets/icons/vote_up.png";
            private const string Offline = "assets/icons/vote_down.png";
            private const string Wait = "assets/icons/stopwatch.png";

            public int timerDisconnectModerator = 300;
            public int timerDisconnectVerifiable = 300;
            public int timerInputRemaining = 300;
            public int timer;

            #endregion

            #region [Initialization]

            private void Awake()
            {
                moderator = GetComponent<BasePlayer>();
                Initialization();
            }

            public void Initialization()
            {
                if (verifiablePlayer == null || moderator == null)
                {
                    Invoke(nameof(Initialization), 0.1f);
                    return;
                }

                verifiable = verifiablePlayer.gameObject.AddComponent<Verifiable>();
                verifiable.check = this;
                verifiable.moderator = moderator;
                DrawUI_CheckStatus(moderator);
                InvokeRepeating(nameof(Timer), 1f, 1f);
            }

            public void StartCheck()
            {
                inputDiscord = true;
                CuiHelper.DestroyUi(verifiablePlayer, UILayerCheckInput);
                DrawUI_CheckStatus(moderator);
                DrawUI_CheckStatus(verifiablePlayer);
            }

            #endregion

            #region [UI]

            public void DrawUI_CheckStatus(BasePlayer player)
            {
                var ui = new CuiElementContainer
                {
                    {
                        new CuiPanel
                        {
                            Image =
                            {
                                Color = HexToRustFormat("#918E8E19"),
                                Material = Sharp
                            },
                            RectTransform =
                            {
                                AnchorMin = "0.3442701 0.1129631",
                                AnchorMax = "0.6411451 0.226852"
                            }
                        },
                        "Overlay", UILayerCheckStatus
                    },
                    {
                        new CuiLabel
                        {
                            Text =
                            {
                                Align = TextAnchor.MiddleCenter,
                                Color = HexToRustFormat("#918E8E"),
                                Text = _.lang.GetMessage("CHECK_STATUS", _, player.UserIDString),
                                FontSize = 14
                            },
                            RectTransform =
                            {
                                AnchorMin = "0 0.75",
                                AnchorMax = "1 1"
                            }
                        },
                        UILayerCheckStatus
                    },
                    new CuiElement
                    {
                        Name = $"{UILayerCheckStatus}_ModeratorIC",
                        Parent = UILayerCheckStatus,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = (string) _.ImageLibrary.Call("GetImage", moderator.UserIDString),
                                Color = "1 1 1 0.7"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.3245648 0.2488146",
                                AnchorMax = "0.4122826 0.6553186"
                            }
                        }
                    },
                    new CuiElement
                    {
                        Name = $"{UILayerCheckStatus}_PlayerIC",
                        Parent = UILayerCheckStatus,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = (string) _.ImageLibrary.Call("GetImage",
                                    verifiablePlayer.UserIDString),
                                Color = "1 1 1 0.7"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.5912311 0.2488146", AnchorMax = "0.6789504 0.6553186"
                            }
                        }
                    },
                    new CuiElement
                    {
                        Name = $"{UILayerCheckStatus}_StatusModerator",
                        Parent = $"{UILayerCheckStatus}_ModeratorIC",
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = HexToRustFormat(moderatorIsOnline ? "#59B65E" : "#B65959"),
                                Sprite = moderatorIsOnline ? Online : Offline
                            },
                            new CuiRectTransformComponent {AnchorMin = "0.2 0.2", AnchorMax = "0.8 0.8"},
                            new CuiOutlineComponent {Color = HexToRustFormat("#0000005D"), Distance = "1 1"}
                        }
                    },
                    {
                        new CuiButton
                        {
                            Button =
                            {
                                Command = "zmoderation.verdict",
                                Color = HexToRustFormat("#918E8EFF"),
                                Sprite = "assets/icons/examine.png"
                            },
                            Text = {Text = " "},
                            RectTransform = {AnchorMin = "0.4789498 0.3089416", AnchorMax = "0.5421077 0.6016245"}
                        },
                        UILayerCheckStatus, $"{UILayerCheckStatus}_StatusIC"
                    }
                };

                var icPlayer = Wait;
                var colorPlayer = "#B65959";
                if (inputDiscord)
                {
                    icPlayer = verifiableIsOnline ? Online : Offline;
                    colorPlayer = verifiableIsOnline ? "#59B65E" : "#B65959";
                }

                ui.Add(new CuiElement
                {
                    Name = $"{UILayerCheckStatus}_StatusPlayer",
                    Parent = $"{UILayerCheckStatus}_PlayerIC",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat(colorPlayer),
                            Sprite = icPlayer
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.2 0.2",
                            AnchorMax = "0.8 0.8"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#0000005D"),
                            Distance = "1 1"
                        }
                    }
                });

                CuiHelper.DestroyUi(player, UILayerCheckStatus);
                CuiHelper.AddUi(player, ui);
                DrawUI_Timer(player);
            }

            public void DrawUI_Timer(BasePlayer player)
            {
                if (player == null) return;
                var ui = new CuiElementContainer();
                var parseTime = TimeSpan.FromSeconds(timer);

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#918E8E"),
                        Text = $"{parseTime.Hours} h {parseTime.Minutes} m {parseTime.Seconds} s",
                        FontSize = 11
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.3245648 0.05691048",
                        AnchorMax = "0.6771959 0.2195109"
                    }
                }, UILayerCheckStatus, $"{UILayerCheckStatus}_Timer");

                CuiHelper.DestroyUi(player, $"{UILayerCheckStatus}_Timer");
                CuiHelper.AddUi(player, ui);
            }

            public void DrawUI_Verdict()
            {
                var ui = new CuiElementContainer();

                if (_verdictisOpen)
                {
                    foreach (var verdict in Verdicts)
                        CuiHelper.DestroyUi(moderator, $"{UILayerCheckStatus}_{verdict.Key}");
                    _verdictisOpen = false;
                    return;
                }

                _verdictisOpen = true;
                var y = 0;
                foreach (var verdict in Verdicts)
                {
                    ui.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = $"zmoderation.verdict {verdict.Key}",
                            Color = HexToRustFormat("#918E8E19"),
                            Material = Sharp
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#918E8E"),
                            Text = $"{verdict.Value.Name}",
                            FontSize = 14
                        },
                        RectTransform =
                        {
                            AnchorMin = $"0 {1.040649 + (y * 0.35)}",
                            AnchorMax = $"1 {1.333331 + (y * 0.35)}"
                        }
                    }, UILayerCheckStatus, $"{UILayerCheckStatus}_{verdict}");
                    y++;
                }

                CuiHelper.AddUi(moderator, ui);
            }

            public void DrawUI_VerifiableDisconnected()
            {
                if (moderator == null) return;
                var ui = new CuiElementContainer();
                var parseTime = TimeSpan.FromSeconds(timerDisconnectVerifiable);

                ui.Add(new CuiElement
                {
                    Name = $"{UILayerCheckStatus}_VerifiableDisconnect",
                    Parent = UILayerCheckStatus,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#FF000053"),
                            Material = Sharp
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1.04",
                            AnchorMax = "0.998 1.29"
                        }
                    }
                });

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 0.5",
                        Text = _.lang.GetMessage("VERIFIABLE_DISCONNECTED", _, moderator.UserIDString)
                            .Replace("{0}", $"{parseTime.Minutes} m {parseTime.Seconds} s"),
                        FontSize = 11
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, $"{UILayerCheckStatus}_VerifiableDisconnect", $"{UILayerCheckStatus}_TimerDisconnectVerifiable");

                CuiHelper.DestroyUi(moderator, $"{UILayerCheckStatus}_VerifiableDisconnect");
                CuiHelper.AddUi(moderator, ui);
            }

            #endregion

            #region [Logic]

            public void Timer()
            {
                timer++;
                if (timerInputRemaining <= 0) Destroy(this);
                if (!inputDiscord & verifiableIsOnline)
                {
                    timerInputRemaining--;
                    verifiable.DrawUI_TimerInput();
                }

                if (!verifiableIsOnline)
                {
                    if (BasePlayer.activePlayerList.Contains(verifiablePlayer))
                    {
                        OnVerifiableConnected();
                        return;
                    }

                    timerDisconnectVerifiable--;
                    if (timerDisconnectVerifiable <= 0) Destroy(this);
                    DrawUI_VerifiableDisconnected();
                }

                if (!moderatorIsOnline)
                {
                    if (BasePlayer.activePlayerList.Contains(moderator))
                    {
                        OnModeratorConnected();
                        return;
                    }

                    timerDisconnectModerator--;
                    if (timerDisconnectModerator <= 0) Destroy(this);
                    verifiable.DrawUI_ModeratorDisconnected();
                }

                if (moderatorIsOnline)
                    DrawUI_Timer(moderator);
                if (verifiableIsOnline & inputDiscord)
                    DrawUI_Timer(verifiablePlayer);
            }

            #endregion

            #region [Iternal Hooks]

            public void OnVerdictConfirm(string verdict)
            {
                if (!Verdicts.ContainsKey(verdict)) return;
                var verdictDB = Verdicts[verdict];
                moderator.ChatMessage($"Проверка окончена\nВы вынесли вердикт {verdict}");
                verifiablePlayer.ChatMessage($"Проверка окончена\nВам вынесли вердикт {verdict}");
                _.CheckInDataBaseModerators(moderator.userID);
                _.CheckInDataBaseReports(verifiablePlayer.userID);
                var database = _._dataBase;
                var checkOutput = new StoredData.Checks();
                var stat = new StoredData.Checks.Statistic
                {
                    Kills = (int) _.ZealStatisticsReborn.Call("GetKillsPlayer", verifiablePlayer.userID),
                    Deaths = (int) _.ZealStatisticsReborn.Call("GetDeathsPlayer", verifiablePlayer.userID),
                    Time = (float) _.ZealStatisticsReborn.Call("GetPlayingTimePlayer", verifiablePlayer.userID),
                    Reports = database.Reports[verifiablePlayer.userID].Reports.Count
                };
                checkOutput.Time = DateTime.Now;
                checkOutput.CheckDuration = timer;
                checkOutput.Verifiable = verifiablePlayer.userID;
                checkOutput.Verifier = moderator.userID;
                checkOutput.VerifierName = moderator.displayName;
                checkOutput.VerifiableName = verifiablePlayer.displayName;
                checkOutput.Stat = stat;
                checkOutput.Result = verdict;

                if (Verdicts[verdict].BanDays > 0)
                {
                    ServerUsers.Set(verifiablePlayer.userID, ServerUsers.UserGroup.Banned, verifiablePlayer.displayName,
                        $"{verdictDB.Name}");
                    ServerUsers.Save();
                    verifiablePlayer.Kick($"{verdictDB.Name}");
                }

                database.ModeratorChecks[moderator.userID].ChecksDB.Add(checkOutput);
                database.ModeratorChecks[moderator.userID].ChecksCount++;
                database.Reports[verifiablePlayer.userID].ChecksDB.Add(checkOutput);

                database.Reports[verifiablePlayer.userID].Reports.Clear();

                Destroy(this);
            }

            public void OnModeratorDisconnected()
            {
                moderatorIsOnline = false;
                timerDisconnectModerator = 200;
                DrawUI_CheckStatus(verifiablePlayer);
            }

            public void OnModeratorConnected()
            {
                timerDisconnectModerator = 200;
                moderatorIsOnline = true;
                DrawUI_CheckStatus(moderator);
                DrawUI_CheckStatus(verifiablePlayer);
            }

            public void OnVerifiableDisconnected()
            {
                Destroy(verifiable);
                verifiableIsOnline = false;
                timerDisconnectVerifiable = 200;
                DrawUI_CheckStatus(moderator);
            }

            public void OnVerifiableConnected()
            {
                verifiable = verifiablePlayer.gameObject.AddComponent<Verifiable>();
                verifiable.check = this;
                verifiable.moderator = moderator;
                timerDisconnectVerifiable = 200;
                verifiableIsOnline = true;
                if (inputDiscord) DrawUI_CheckStatus(moderator);
                else verifiable.DrawUI_CheckInput();
            }

            private void OnDestroy()
            {
                _._checks.Remove(moderator);
                Destroy(verifiable);
                CuiHelper.DestroyUi(moderator, UILayerCheckStatus);
            }

            #endregion

            public class Verifiable : MonoBehaviour
            {
                #region [Fields]

                private BasePlayer _verifiablePlayer;
                public BasePlayer moderator;
                public Check check;

                #endregion

                #region [Initialization]

                private void Awake()
                {
                    _verifiablePlayer = GetComponent<BasePlayer>();
                    Initialization();
                }

                public void Initialization()
                {
                    if (_verifiablePlayer == null || moderator == null)
                    {
                        Invoke(nameof(Initialization), 0.1f);
                        return;
                    }

                    if (!check.inputDiscord)
                    {
                        DrawUI_CheckInput();
                        DrawUI_TimerInput();
                    }
                    else check.DrawUI_CheckStatus(_verifiablePlayer);
                }

                #endregion

                #region [UI]

                public void DrawUI_CheckInput()
                {
                    var ui = new CuiElementContainer
                    {
                        {
                            new CuiPanel
                            {
                                Image = {Color = HexToRustFormat("#918E8E19"), Material = Sharp},
                                RectTransform =
                                {
                                    AnchorMin = "0.3442701 0.1129631", AnchorMax = "0.6411451 0.226852"
                                }
                            },
                            "Overlay", UILayerCheckInput
                        },
                        {
                            new CuiLabel
                            {
                                Text =
                                {
                                    Align = TextAnchor.MiddleCenter,
                                    Color = HexToRustFormat("#918E8E"),
                                    Text = _.lang.GetMessage("CHECK_CALLED", _, _verifiablePlayer.UserIDString),
                                    FontSize = 14
                                },
                                RectTransform = {AnchorMin = "0 0.75", AnchorMax = "1 1"}
                            },
                            UILayerCheckInput
                        },
                        {
                            new CuiLabel
                            {
                                Text =
                                {
                                    Align = TextAnchor.MiddleCenter,
                                    Color = HexToRustFormat("#918E8E7B"),
                                    Text = _.lang.GetMessage("INPUT_DISCORD", _, _verifiablePlayer.UserIDString),
                                    FontSize = 11
                                },
                                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 0.3414623"}
                            },
                            UILayerCheckInput
                        },
                        {
                            new CuiPanel
                            {
                                Image = {Color = HexToRustFormat("#918E8E16"), Material = Sharp},
                                RectTransform =
                                {
                                    AnchorMin = "0.3315814 0.3658522", AnchorMax = "0.6701779 0.6585351"
                                }
                            },
                            UILayerCheckInput, $"{UILayerCheckInput}_InputBG"
                        },
                        new CuiElement
                        {
                            Name = $"{UILayerCheckInput}_InputElem",
                            Parent = $"{UILayerCheckInput}_InputBG",
                            Components =
                            {
                                new CuiInputFieldComponent
                                {
                                    Align = TextAnchor.MiddleCenter,
                                    CharsLimit = 50,
                                    Command = "zmoderation.input ",
                                    Font = Regular,
                                    FontSize = 11
                                },
                                new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                            }
                        }
                    };

                    CuiHelper.DestroyUi(_verifiablePlayer, UILayerCheckInput);
                    CuiHelper.AddUi(_verifiablePlayer, ui);
                    DrawUI_TimerInput();
                }

                public void DrawUI_TimerInput()
                {
                    var ui = new CuiElementContainer();
                    var parseTime = TimeSpan.FromSeconds(check.timerInputRemaining);

                    ui.Add(new CuiElement
                    {
                        Name = $"{UILayerCheckInput}_InputRemaining",
                        Parent = UILayerCheckInput,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = HexToRustFormat("#FF000053"),
                                Material = Sharp
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 1.04065",
                                AnchorMax = "0.998 1.292681"
                            }
                        }
                    });

                    ui.Add(new CuiLabel
                    {
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 0.5",
                            Text = _.lang.GetMessage("CHECK_DISCORD_REMAINING", _, _verifiablePlayer.UserIDString)
                                .Replace("{0}", $"{parseTime.Minutes} m {parseTime.Seconds} s"),
                            FontSize = 10
                        },
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    }, $"{UILayerCheckInput}_InputRemaining");

                    CuiHelper.DestroyUi(_verifiablePlayer, $"{UILayerCheckInput}_InputRemaining");
                    CuiHelper.AddUi(_verifiablePlayer, ui);
                }

                public void DrawUI_ModeratorDisconnected()
                {
                    if (_verifiablePlayer == null) return;
                    var ui = new CuiElementContainer();
                    var parseTime = TimeSpan.FromSeconds(check.timerDisconnectModerator);

                    ui.Add(new CuiElement
                    {
                        Name = $"{UILayerCheckStatus}_ModeratorDisconnect",
                        Parent = UILayerCheckStatus,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = HexToRustFormat("#FF000053"),
                                Material = Sharp
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 1.04065",
                                AnchorMax = "0.998 1.292681"
                            }
                        }
                    });

                    ui.Add(new CuiLabel
                    {
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 0.5",
                            Text = _.lang.GetMessage("MODERATOR_DISCONNECTED", _, _verifiablePlayer.UserIDString)
                                .Replace("{0}", $"{parseTime.Minutes} m {parseTime.Seconds} s"),
                            FontSize = 11
                        },
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    }, $"{UILayerCheckStatus}_ModeratorDisconnect", $"{UILayerCheckStatus}_TimerDisconnectModerator");

                    CuiHelper.DestroyUi(_verifiablePlayer, $"{UILayerCheckStatus}_ModeratorDisconnect");
                    CuiHelper.AddUi(_verifiablePlayer, ui);
                }

                #endregion

                #region [Iternal Hooks]

                public void OnInputField(string input)
                {
                    if (input == null) return;
                    const string message = "Введён некорректный дискорд, повторите попытку";
                    if (!input.Contains("#"))
                    {
                        _verifiablePlayer.ChatMessage(message);
                        return;
                    }

                    if (input.Split('#')[1].Length != 4)
                    {
                        _verifiablePlayer.ChatMessage(message);
                        return;
                    }

                    moderator.ChatMessage($"Дискорд проверяемого : {input}");
                    check.StartCheck();
                }

                private void OnDestroy()
                {
                    CuiHelper.DestroyUi(_verifiablePlayer, UILayerCheckStatus);
                    CuiHelper.DestroyUi(_verifiablePlayer, UILayerCheckInput);
                }

                #endregion
            }
        }

        #endregion

        #region [DrawUI] / [Отрисовка UI]

        private void DrawUI_ModeratorPanel(BasePlayer player)
        {
            var ui = new CuiElementContainer();
            const float fadeIn = 0.3f;

            ui.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image =
                {
                    Color = HexToRustFormat("#000000B2"),
                    Material = Blur
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, "Overlay", UILayerModeratorPanel);

            ui.Add(new CuiElement
            {
                Name = $"{UILayerModeratorPanel}_PanelName",
                Parent = UILayerModeratorPanel,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 20,
                        Text = "СПИСОК ИГРОКОВ"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1651042 0.9213709",
                        AnchorMax = "1 1"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#0000004C"),
                        Distance = "0.9 0.9"
                    }
                }
            });

            #region [Profile]

            var layerProfile = $"{UILayerModeratorPanel}_Profile";

            ui.Add(new CuiPanel
            {
                Image =
                {
                    Color = HexToRustFormat("#000000AA"),
                    Material = Blur,
                    Sprite = Radial,
                    FadeIn = fadeIn
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "0.1651042 1"
                }
            }, UILayerModeratorPanel, layerProfile);

            ui.Add(new CuiPanel
            {
                Image =
                {
                    Color = HexToRustFormat("#8EBF5B77"),
                    Material = Sharp,
                    FadeIn = fadeIn
                },
                RectTransform =
                {
                    AnchorMin = ".999 0",
                    AnchorMax = "1 1"
                }
            }, layerProfile);

            ui.Add(new CuiElement
            {
                Name = $"{layerProfile}_PanelName",
                Parent = layerProfile,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 15,
                        Text = "ПАНЕЛЬ МОДЕРАТОРА",
                        FadeIn = fadeIn
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.94",
                        AnchorMax = "1 1"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#0000004C"),
                        Distance = "0.9 0.9"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{layerProfile}_Avatar",
                Parent = layerProfile,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage", player.UserIDString),
                        FadeIn = fadeIn
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.2618299 0.8092595",
                        AnchorMax = "0.7350159 0.9481484"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{layerProfile}_Nick",
                Parent = layerProfile,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Font = Regular,
                        FontSize = 15,
                        Text = player.displayName,
                        FadeIn = fadeIn
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.7629628",
                        AnchorMax = "1 0.7953702"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#0000004C"),
                        Distance = "0.9 0.9"
                    }
                }
            });

            #region [Statistic]

            var moderatorDB = _dataBase.ModeratorChecks[player.userID];

            ui.Add(new CuiElement
            {
                Name = $"{layerProfile}_ChecksCountIC",
                Parent = layerProfile,
                Components =
                {
                    new CuiImageComponent
                    {
                        Sprite = "assets/icons/examine.png",
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FadeIn = fadeIn
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1230283 0.6962962",
                        AnchorMax = "0.2334382 0.7287036"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#0000004C"),
                        Distance = "0.9 0.9"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{layerProfile}_ChecksCount",
                Parent = layerProfile,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 15,
                        Text = $"{moderatorDB.ChecksCount}",
                        FadeIn = fadeIn
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.2649842 0.6962962",
                        AnchorMax = "0.4384854 0.7287036"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#0000004C"),
                        Distance = "0.9 0.9"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{layerProfile}_ChecksTimeIC",
                Parent = layerProfile,
                Components =
                {
                    new CuiImageComponent
                    {
                        Sprite = "assets/icons/stopwatch.png",
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FadeIn = fadeIn
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1041008 0.6490754",
                        AnchorMax = "0.2145106 0.6814828"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#0000004C"),
                        Distance = "0.9 0.9"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{layerProfile}_ChecksTimeValue",
                Parent = layerProfile,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 15,
                        Text = "NULL",
                        FadeIn = fadeIn
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.2649842 0.6490754",
                        AnchorMax = "0.4384856 0.6814828"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#0000004C"),
                        Distance = "0.9 0.9"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{layerProfile}_ChecksSuccessIC",
                Parent = layerProfile,
                Components =
                {
                    new CuiImageComponent
                    {
                        Sprite = "assets/icons/target.png",
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FadeIn = fadeIn
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5741327 0.698148",
                        AnchorMax = "0.6687698 0.7259258"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#0000004C"),
                        Distance = "0.9 0.9"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{layerProfile}_ChecksSuccessValue",
                Parent = layerProfile,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 15,
                        Text = "NULL",
                        FadeIn = fadeIn
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.735016 0.6962962",
                        AnchorMax = "0.9085176 0.7287036"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#0000004C"),
                        Distance = "0.9 0.9"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{layerProfile}_RatingIC",
                Parent = layerProfile,
                Components =
                {
                    new CuiImageComponent
                    {
                        Sprite = "assets/icons/study.png",
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FadeIn = fadeIn
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5741327 0.6500013",
                        AnchorMax = "0.6750785 0.6796309"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#0000004C"),
                        Distance = "0.9 0.9"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{layerProfile}_RatingValue",
                Parent = layerProfile,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 15,
                        Text = "NULL",
                        FadeIn = fadeIn
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.735016 0.6490754",
                        AnchorMax = "0.9085176 0.6814828"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#0000004C"),
                        Distance = "0.9 0.9"
                    }
                }
            });

            #endregion

            #endregion

            CuiHelper.DestroyUi(player, UILayerModeratorPanel);
            CuiHelper.AddUi(player, ui);
            DrawUI_PlayerList(player, 0, GetSortedReports());
            DrawUI_InputFind(player);
        }

        private void DrawUI_PlayerList(BasePlayer player, int page,
            IEnumerable<KeyValuePair<ulong, StoredData.PObj>> players)
        {
            var ui = new CuiElementContainer();
            var uiButtons = new CuiElementContainer();
            const float fadeIn = 0.1f;

            ui.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = "0.1651042 0",
                    AnchorMax = "1 0.9213709"
                }
            }, UILayerModeratorPanel, UILayerPlayerList);

            ui.Add(new CuiButton
            {
                Button =
                {
                    Close = UILayerModeratorPanel,
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
            }, UILayerPlayerList);

            int x = 0, y = 0, i = 0;
            foreach (var index in players.Skip(page * 28).Take(28))
            {
                var plobj = BasePlayer.FindByID(index.Key);
                var isOnline = plobj == null;

                if (x >= 4)
                {
                    y++;
                    x = 0;
                }

                var element = $"{UILayerModeratorPanel}_PlayerList_Elem{i}";
                ui.Add(new CuiElement
                {
                    Name = element,
                    Parent = UILayerPlayerList,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#00000051"),
                            FadeIn = fadeIn + (i * fadeIn)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.011 + (x * 0.245)} {0.88 - (y * 0.128)}",
                            AnchorMax = $"{0.25 + (x * 0.245)} {0.998 - (y * 0.128)}"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{element}_Avatar",
                    Parent = element,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = (string) ImageLibrary.Call("GetImage", $"{index.Key}"),
                            FadeIn = fadeIn + (i * fadeIn)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.001 0.004",
                            AnchorMax = "0.305 0.98"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{element}_userid",
                    Parent = $"{element}_Avatar",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 8,
                            Text = $"{index.Key}",
                            FadeIn = fadeIn + (i * fadeIn)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 0.2"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#00000051"),
                            Distance = "0.9 0.9"
                        }
                    }
                });

                var colorIsOnline = !isOnline ? "#74D46AFF" : "#D46A6AFF";
                ui.Add(new CuiElement
                {
                    Name = $"{element}_IsOnline",
                    Parent = $"{element}_Avatar",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat(colorIsOnline),
                            FontSize = 30,
                            Text = "•",
                            FadeIn = fadeIn + (i * fadeIn)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.77 0.6",
                            AnchorMax = "1 1.18"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#00000051"),
                            Distance = "0.5 0.5"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{element}_displayname",
                    Parent = element,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#EAEAEAFF"),
                            FontSize = 12,
                            Text = index.Value.Displayname.Replace("\\", ""),
                            FadeIn = fadeIn + (i * fadeIn)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.3041825 0.7250001",
                            AnchorMax = "1 1"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{element}_CountryIC",
                    Parent = $"{element}_Avatar",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Sprite = $"assets/icons/flags/{lang.GetLanguage($"{index.Key}")}.png",
                            FadeIn = fadeIn + (i * fadeIn)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.0250001 0.78",
                            AnchorMax = "0.2583335 0.995"
                        }
                    }
                });

                var starCount = index.Value.Reports.Count / (MAXReports / 3);
                for (int j = 0, xi = 0, num = 1; j < 3; j++, xi++, num++)
                {
                    var color = HexToRustFormat("#FFFFFF7D");
                    if (num <= starCount) color = HexToRustFormat("#ffd700");

                    ui.Add(new CuiElement
                    {
                        Name = $"{element}_CheckIC",
                        Parent = element,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = color,
                                Sprite = "assets/icons/favourite_servers.png",
                                FadeIn = fadeIn + (i * fadeIn)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0.32 + (xi * 0.08)} {0.0415}",
                                AnchorMax = $"{0.4 + (xi * 0.08)} {0.308}"
                            }
                        }
                    });
                }

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Sprite = "assets/icons/examine.png",
                        Color = HexToRustFormat("#EAEAEAFF"),
                        Command = $"zmoderation.pinfo {index.Key}",
                        FadeIn = fadeIn + (i * fadeIn)
                    },
                    Text =
                    {
                        Text = " "
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.9 0.04166558",
                        AnchorMax = "0.985 0.3083323"
                    }
                }, element, $"{element}_CheckIC");

                x++;
                i++;
            }

            uiButtons.Add(new CuiButton
            {
                Button =
                {
                    Color = "0 0 0 0",
                    Command = $"zmoderation.page.playerlist {page - 1}"
                },
                Text =
                {
                    Text = "◀",
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 14,
                    Color = HexToRustFormat("#EAEAEAFF")
                },
                RectTransform =
                {
                    AnchorMin = "0.4653719 0.02210876",
                    AnchorMax = "0.4809675 0.04723236"
                }
            }, UILayerPlayerList, $"{UILayerPlayerList}_Left");

            uiButtons.Add(new CuiLabel
            {
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = $"{page + 1}",
                    FontSize = 15,
                    Color = HexToRustFormat("#EAEAEAFF")
                },
                RectTransform =
                {
                    AnchorMin = "0.4847103 0.02210876",
                    AnchorMax = "0.5159024 0.04723236"
                }
            }, UILayerPlayerList, $"{UILayerPlayerList}_PageNUM");

            uiButtons.Add(new CuiButton
            {
                Button =
                {
                    Color = "0 0 0 0",
                    Command = $"zmoderation.page.playerlist {page + 1}"
                },
                Text =
                {
                    Text = "▶",
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 14,
                    Color = HexToRustFormat("#EAEAEAFF")
                },
                RectTransform =
                {
                    AnchorMin = "0.5196489 0.02210876",
                    AnchorMax = "0.5352447 0.04723236"
                }
            }, UILayerPlayerList, $"{UILayerPlayerList}_Right");

            CuiHelper.DestroyUi(player, $"{UILayerPlayerList}_Right");
            CuiHelper.DestroyUi(player, $"{UILayerPlayerList}_PageNUM");
            CuiHelper.DestroyUi(player, $"{UILayerPlayerList}_Left");
            CuiHelper.DestroyUi(player, $"{UILayerPlayerList}_Player");

            for (var j = 0; j <= 28; j++) CuiHelper.DestroyUi(player, $"{UILayerModeratorPanel}_PlayerList_Elem{j}");

            CuiHelper.AddUi(player, ui);
            CuiHelper.AddUi(player, uiButtons);
        }

        private void DrawUI_Player(BasePlayer player, ulong suspect)
        {
            var ui = new CuiElementContainer();
            var suspectDB = _dataBase.Reports[suspect];
            var plobj = BasePlayer.FindByID(suspect);
            var isOnline = plobj == null;
            var colorIsOnline = !isOnline ? "#74D46AFF" : "#D46A6AFF";
            var team = !isOnline ? plobj.Team : null;
            const float fadeIn = 0.5f;

            #region [Player]

            var layerPlayer = $"{UILayerPlayerList}_Player";

            ui.Add(new CuiPanel
            {
                Image =
                {
                    Color = HexToRustFormat("#000000C6"),
                    Material = BlurInMenu,
                    Sprite = Radial,
                    FadeIn = fadeIn
                },
                RectTransform =
                {
                    AnchorMin = "0.167 0",
                    AnchorMax = "1 1"
                }
            }, UILayerModeratorPanel, layerPlayer);

            ui.Add(new CuiButton
            {
                Button =
                {
                    Close = layerPlayer,
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
            }, $"{UILayerPlayerList}_Player");

            ui.Add(new CuiElement
            {
                Name = $"{layerPlayer}_Avatar",
                Parent = layerPlayer,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage", $"{suspect}"),
                        FadeIn = fadeIn
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.4291946 0.7164053",
                        AnchorMax = "0.5701801 0.9258585"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{layerPlayer}_CountryIC",
                Parent = $"{layerPlayer}_Avatar",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Sprite = $"assets/icons/flags/{lang.GetLanguage($"{suspect}")}.png",
                        FadeIn = fadeIn
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.0250001 0.78",
                        AnchorMax = "0.2583335 0.995"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{layerPlayer}_IsOnline",
                Parent = $"{layerPlayer}_Avatar",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat(colorIsOnline),
                        FontSize = 40,
                        Text = "•",
                        FadeIn = fadeIn
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.77 0.6",
                        AnchorMax = "1 1.18"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#00000051"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            var starCount = suspectDB.Reports.Count / (MAXReports / 3);
            for (int j = 0, xi = 0, num = 1; j < 3; j++, xi++, num++)
            {
                var color = HexToRustFormat("#FFFFFF7D");
                if (num <= starCount) color = HexToRustFormat("#ffd700");

                ui.Add(new CuiElement
                {
                    Name = $"{layerPlayer}_CheckIC",
                    Parent = $"{layerPlayer}_Avatar",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = color,
                            Sprite = "assets/icons/favourite_servers.png",
                            FadeIn = fadeIn * num
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.3008856 + (xi * 0.13)} {0.05309755}",
                            AnchorMax = $"{0.4424781 + (xi * 0.13)} {0.1946905}"
                        }
                    }
                });
            }

            ui.Add(new CuiElement
            {
                Name = $"{layerPlayer}_displayname",
                Parent = layerPlayer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 17,
                        Text = suspectDB.Displayname,
                        FadeIn = fadeIn
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.4 0.68",
                        AnchorMax = "0.6 0.7164053"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{layerPlayer}_userid",
                Parent = layerPlayer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 10,
                        Text = $"{suspect}",
                        FadeIn = fadeIn
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.4 0.65",
                        AnchorMax = "0.6 0.68"
                    }
                }
            });

            #endregion

            #region [Statistic]

            ui.Add(new CuiElement
            {
                Name = $"{layerPlayer}_StatisticTxt",
                Parent = layerPlayer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 18,
                        Text = "Статистика",
                        FadeIn = fadeIn
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.0349344 0.5875807",
                        AnchorMax = "0.2732377 0.6190916"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{layerPlayer}_Kills",
                Parent = layerPlayer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 14,
                        Text = $"Убийств : {(int) ZealStatisticsReborn.Call("GetKillsPlayer", suspect)}",
                        Font = Regular,
                        FadeIn = fadeIn
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.0349344 0.5366078",
                        AnchorMax = "0.2732377 0.5681177"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{layerPlayer}_Deaths",
                Parent = layerPlayer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 14,
                        Text = $"Смертей : {(int) ZealStatisticsReborn.Call("GetDeathsPlayer", suspect)}",
                        Font = Regular,
                        FadeIn = fadeIn
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.0349344 0.503243",
                        AnchorMax = "0.2732377 0.534753"
                    }
                }
            });

            var hours = TimeSpan.FromSeconds((float) ZealStatisticsReborn.Call("GetPlayingTimePlayer", suspect))
                .TotalHours;

            ui.Add(new CuiElement
            {
                Name = $"{layerPlayer}_PlayingTime",
                Parent = layerPlayer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 14,
                        Text = $"Наигранно часов : {hours:F} час (-ов)",
                        Font = Regular,
                        FadeIn = fadeIn
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.0349344 0.4698785",
                        AnchorMax = "0.2732377 0.5013885"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{layerPlayer}_Reports",
                Parent = layerPlayer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 14,
                        Text = $"Кол-во репортов : {suspectDB.Reports.Count}",
                        Font = Regular,
                        FadeIn = fadeIn
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.0349344 0.436514",
                        AnchorMax = "0.2732377 0.468024"
                    }
                }
            });

            #endregion

            #region [Report List]

            if (suspectDB.Reports.Count != 0)
            {
                ui.Add(new CuiElement
                {
                    Name = $"{layerPlayer}_ReportTxt",
                    Parent = layerPlayer,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            Color = HexToRustFormat("#EAEAEAFF"),
                            FontSize = 18,
                            Text = "Список репортов",
                            FadeIn = fadeIn
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.0349344 0.3540289",
                            AnchorMax = "0.2732377 0.3855398"
                        }
                    }
                });
            }

            #region [Check List]

            if (suspectDB.ChecksDB.Count != 0)
            {
                ui.Add(new CuiElement
                {
                    Name = $"{layerPlayer}_ReportTxt",
                    Parent = layerPlayer,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            Color = HexToRustFormat("#EAEAEAFF"),
                            FontSize = 18,
                            Text = "История проверок",
                            FadeIn = fadeIn
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5464727 0.3540289",
                            AnchorMax = "0.7847719 0.3855398"
                        }
                    }
                });
            }

            #endregion

            #endregion

            #region [Team List]

            if (team != null)
            {
                int tx = 0;
                foreach (var member in team.members)
                {
                    if (member == suspect) continue;
                    var memberIsOnline = BasePlayer.FindByID(member) != null;
                    var memberObj = memberIsOnline
                        ? $"{BasePlayer.FindByID(member)}"
                        : $"{covalence.Players.FindPlayerById(member.ToString())}";

                    ui.Add(new CuiElement
                    {
                        Name = $"{layerPlayer}_teammember_{member}",
                        Parent = layerPlayer,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = (string) ImageLibrary.Call("GetImage", $"{member}")
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0.3349969 - (tx * 0.1)} {0.816497}",
                                AnchorMax = $"{0.3817842 - (tx * 0.1)} {0.8860058}"
                            }
                        }
                    });

                    ui.Add(new CuiElement
                    {
                        Name = $"{layerPlayer}_teammember_info{member}",
                        Parent = $"{layerPlayer}_teammember_{member}",
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Align = TextAnchor.UpperCenter,
                                FontSize = 8,
                                Text = memberObj
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "-0.5 -0.8400019",
                                AnchorMax = "1.5 -0.06666642"
                            }
                        }
                    });
                    tx++;
                }
            }

            #endregion

            CuiHelper.DestroyUi(player, $"{UILayerPlayerList}_Player");
            CuiHelper.AddUi(player, ui);
            DrawUI_ReportList(player, suspect, 0);
            DrawUI_CheckList(player, suspect, 0);
            DrawUI_Player_Menu(player, suspect, false);
        }

        private static void DrawUI_Player_Menu(BasePlayer player, ulong suspect, bool menuIsOpen)
        {
            var ui = new CuiElementContainer();
            const float fadeIn = 0.5f;
            var layerPlayer = $"{UILayerPlayerList}_Player";
            var invert = !menuIsOpen;

            ui.Add(new CuiButton
            {
                Button =
                {
                    Sprite = "assets/icons/study.png",
                    Color = HexToRustFormat("#EAEAEAFF"),
                    Command = $"zmoderation.menu {suspect} {invert}",
                    FadeIn = fadeIn
                },
                Text =
                {
                    Text = " "
                },
                RectTransform =
                {
                    AnchorMin = "0.9575795 0.9379058",
                    AnchorMax = "0.988771 0.9842451"
                }
            }, layerPlayer, $"{layerPlayer}_MenuIC");

            if (menuIsOpen)
            {
                var y = 0;
                foreach (var button in Menu)
                {
                    var command = "null";
                    switch (button.Key)
                    {
                        case "CHECK":
                            command = $"zmoderation.check {suspect}";
                            break;
                        case "FAVORITE":
                            command = "NULL";
                            break;
                    }

                    ui.Add(new CuiButton
                    {
                        Button =
                        {
                            Sprite = button.Value,
                            Color = HexToRustFormat("#EAEAEAFF"),
                            Command = command,
                            FadeIn = fadeIn
                        },
                        Text =
                        {
                            Text = " "
                        },
                        RectTransform =
                        {
                            AnchorMin = $"0.9606986 {0.8943465 - (y * 0.05)}",
                            AnchorMax = $"0.9856518 {0.9314179 - (y * 0.05)}"
                        }
                    }, layerPlayer, $"{layerPlayer}_MenuIC_{button.Key}");
                    y++;
                }
            }
            else
                foreach (var button in Menu)
                    CuiHelper.DestroyUi(player, $"{layerPlayer}_MenuIC_{button.Key}");

            CuiHelper.DestroyUi(player, $"{layerPlayer}_MenuIC");
            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_ReportList(BasePlayer player, ulong suspectID, int page)
        {
            CheckInDataBaseReports(suspectID);
            var reports = _dataBase.Reports[suspectID];
            if (reports.Reports.Count == 0) return;
            var ui = new CuiElementContainer();
            const float fadeIn = 0.5f;
            var layerPlayer = $"{UILayerPlayerList}_Player";
            int y = 0, num = 0;

            foreach (var report in reports.Reports.Skip(8 * page).Take(8))
            {
                var elem = $"{layerPlayer}_Report{num}";
                var icType = "assets/icons/lick.png";
                switch (report.Type)
                {
                    case "cheat":
                        icType = "assets/icons/target.png";
                        break;
                    case "abusive":
                        icType = "assets/icons/lick.png";
                        break;
                    case "spam":
                        icType = "assets/icons/demolish_immediate.png";
                        break;
                }

                ui.Add(new CuiElement
                {
                    Name = elem,
                    Parent = layerPlayer,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0 0 0 0"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.0349344} {0.302129 - (y * 0.035)}",
                            AnchorMax = $"{0.3462258} {0.333639 - (y * 0.035)}"
                        }
                    }
                });

                const double cof = 0.0214;
                var lenght = (report.InitiatiorName.Length + report.TargetName.Length) + 3.2;
                var anchor = cof * lenght;

                ui.Add(new CuiElement
                {
                    Name = $"{layerPlayer}_Report_Names{y}",
                    Parent = elem,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            Color = HexToRustFormat("#EAEAEAFF"),
                            FontSize = 12,
                            Text =
                                $"<color=#D1D1D1CA>{report.InitiatiorName}</color> <color=#797979CA>→</color> <color=#EAEAEACA>{report.TargetName}</color>"
                                    .ToUpper(),
                            FadeIn = fadeIn * y
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = $"{anchor} 1"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{layerPlayer}_TypeReport",
                    Parent = elem,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#D1D1D1CA"),
                            Sprite = icType,
                            FadeIn = fadeIn * y
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{anchor} 0.1176496",
                            AnchorMax = $"{anchor + 0.0480961} 0.823547"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{layerPlayer}_Report_Time{y}",
                    Parent = elem,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            Color = HexToRustFormat("#D1D1D1CA"),
                            FontSize = 12,
                            Text = $"{GetFormatTime(report.Time)}",
                            Font = Regular,
                            FadeIn = fadeIn * y
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{anchor + +0.08} 0",
                            AnchorMax = $"{(anchor + 0.06) + 0.4} 1"
                        }
                    }
                });

                y++;
                num++;
            }

            if ((reports.Reports.Count / 8) >= 1)
            {
                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Color = HexToRustFormat("#0000002D"),
                        Command = $"zmoderation.page {suspectID} {page - 1}"
                    },
                    Text =
                    {
                        Text = "◀",
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 14,
                        Color = HexToRustFormat("#EAEAEAFF")
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.06051119 0.02131684",
                        AnchorMax = "0.0792264 0.04911933"
                    }
                }, layerPlayer, $"{layerPlayer}_Left");

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Text = $"{page + 1}",
                        FontSize = 15,
                        Color = HexToRustFormat("#EAEAEAFF")
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.07922626 0.02131684",
                        AnchorMax = "0.09794146 0.04911933"
                    }
                }, layerPlayer, $"{layerPlayer}_PageNUM");

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Color = "0 0 0 0",
                        Command = $"zmoderation.page {suspectID} {page + 1}"
                    },
                    Text =
                    {
                        Text = "▶",
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 14,
                        Color = HexToRustFormat("#EAEAEAFF")
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.09794178 0.02131684",
                        AnchorMax = "0.1166561 0.04911933"
                    }
                }, layerPlayer, $"{layerPlayer}_Right");
            }

            CuiHelper.DestroyUi(player, $"{layerPlayer}_Right");
            CuiHelper.DestroyUi(player, $"{layerPlayer}_PageNUM");
            CuiHelper.DestroyUi(player, $"{layerPlayer}_Left");

            for (var i = 0; i <= 8; i++) CuiHelper.DestroyUi(player, $"{layerPlayer}_Report{i}");

            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_CheckList(BasePlayer player, ulong suspectID, int page)
        {
            var suspectDB = _dataBase.Reports[suspectID];
            if (suspectDB.ChecksDB.Count == 0) return;
            var ui = new CuiElementContainer();
            const float fadeIn = 0.5f;
            var layerPlayer = $"{UILayerPlayerList}_Player";
            int y = 0, num = 0;

            foreach (var check in suspectDB.ChecksDB.Skip(8 * page).Take(8))
            {
                var elem = $"{layerPlayer}_Check{num}";

                ui.Add(new CuiElement
                {
                    Name = elem,
                    Parent = layerPlayer,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0 0 0 0"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.5464727} {0.302129 - (y * 0.035)}",
                            AnchorMax = $"{1} {0.333639 - (y * 0.035)}"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{layerPlayer}_Check_Names{y}",
                    Parent = elem,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            Color = HexToRustFormat("#EAEAEAFF"),
                            FontSize = 12,
                            Text =
                                $"<color=#D1D1D1CA>{check.VerifierName}</color> <color=#797979CA>→</color> <color=#EAEAEACA>{check.VerifiableName}</color> <color=#D1D1D1CA> {Verdicts[check.Result].Name} [{check.Time:g}]</color>"
                                    .ToUpper(),
                            FadeIn = fadeIn * y
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    }
                });

                y++;
                num++;
            }

            if ((suspectDB.ChecksDB.Count / 8) >= 1)
            {
                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Color = HexToRustFormat("#0000002D"),
                        Command = $"zmoderation.checkpage {suspectID} {page - 1}"
                    },
                    Text =
                    {
                        Text = "◀",
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 14,
                        Color = HexToRustFormat("#EAEAEAFF")
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.6363037 0.02131684",
                        AnchorMax = "0.6550173 0.04911933"
                    }
                }, layerPlayer, $"{layerPlayer}_LeftCheck");

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Text = $"{page + 1}",
                        FontSize = 15,
                        Color = HexToRustFormat("#EAEAEAFF")
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.6550183 0.02131684",
                        AnchorMax = "0.673732 0.04911933"
                    }
                }, layerPlayer, $"{layerPlayer}_PageNUMCheck");

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Color = "0 0 0 0",
                        Command = $"zmoderation.checkpage {suspectID} {page + 1}"
                    },
                    Text =
                    {
                        Text = "▶",
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 14,
                        Color = HexToRustFormat("#EAEAEAFF")
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.673733 0.02131684",
                        AnchorMax = "0.6924466 0.04911933"
                    }
                }, layerPlayer, $"{layerPlayer}_RightCheck");
            }

            CuiHelper.DestroyUi(player, $"{layerPlayer}_RightCheck");
            CuiHelper.DestroyUi(player, $"{layerPlayer}_PageNUMCheck");
            CuiHelper.DestroyUi(player, $"{layerPlayer}_LeftCheck");

            for (var i = 0; i <= 8; i++) CuiHelper.DestroyUi(player, $"{layerPlayer}_Check{i}");

            CuiHelper.AddUi(player, ui);
        }

        private static void DrawUI_InputFind(BasePlayer player)
        {
            var layerProfile = $"{UILayerModeratorPanel}_Profile";
            const float fadeIn = 0.3f;
            var ui = new CuiElementContainer
            {
                new CuiElement
                {
                    Name = $"{layerProfile}_FindBG",
                    Parent = layerProfile,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#00000061"), Material = BlurInMenu, FadeIn = fadeIn
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.05 0.05185188", AnchorMax = "0.94 0.09074073"
                        },
                        new CuiOutlineComponent {Color = HexToRustFormat("#8EBF5B77"), Distance = "0.8 0"}
                    }
                },
                new CuiElement
                {
                    Name = $"{layerProfile}_FindICBG",
                    Parent = $"{layerProfile}_FindBG",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#00000061"), Material = BlurInMenu, FadeIn = fadeIn
                        },
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "0.145 0.98"},
                        new CuiOutlineComponent {Color = HexToRustFormat("#8EBF5B77"), Distance = "0.7 0"}
                    }
                },
                new CuiElement
                {
                    Name = $"{layerProfile}_FindIC",
                    Parent = $"{layerProfile}_FindBG",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Sprite = "assets/icons/web.png",
                            Color = HexToRustFormat("#EAEAEAFF"),
                            FadeIn = fadeIn
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.01034483 0.07142863", AnchorMax = "0.1344828 0.9285722"
                        }
                    }
                },
                new CuiElement
                {
                    Name = $"{layerProfile}_FindTXT",
                    Parent = $"{layerProfile}_FindBG",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#D1D1D10E"),
                            FontSize = 11,
                            Font = Regular,
                            Text = "ПОИСК ИГРОКА"
                        },
                        new CuiRectTransformComponent {AnchorMin = "0.14 0", AnchorMax = "1 1"}
                    }
                },
                new CuiElement
                {
                    Name = $"{layerProfile}_FindInput",
                    Parent = $"{layerProfile}_FindBG",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            CharsLimit = 50,
                            Command = "zmoderation.find",
                            FontSize = 14,
                            Font = Regular
                        },
                        new CuiRectTransformComponent {AnchorMin = "0.14 0", AnchorMax = "1 1"}
                    }
                }
            };


            CuiHelper.AddUi(player, ui);
        }

        #endregion

        #region [Hooks] / [Крюки]

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            _ = this;
            LoadData();
            PermissionsRegistration();
            StartProcess(CheckAllInDataBase(), "CheckAllInDataBase");
        }

        // ReSharper disable once UnusedMember.Local
        private void OnPlayerConnected(BasePlayer player)
        {
            CheckInDataBaseReports(player.userID, player.displayName);
            _dataBase.Reports[player.userID].Displayname = player.displayName;
        }

        // ReSharper disable once UnusedMember.Local
        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player.GetComponent<Check.Verifiable>() != null)
            {
                var component = player.GetComponent<Check.Verifiable>();
                component.check.OnVerifiableDisconnected();
            }

            if (player.GetComponent<Check>() != null)
            {
                var component = player.GetComponent<Check>();
                component.OnModeratorDisconnected();
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void OnPlayerReported(BasePlayer reporter, string targetName, string targetId, string subject,
            string message, string type)
        {
            CheckInDataBaseReports(reporter.userID, reporter.displayName);
            var targetIDUlong = Convert.ToUInt64(targetId);
            CheckInDataBaseReports(targetIDUlong, targetName);
            ImageLibrary.Call("GetPlayerAvatar", targetId);
            _dataBase.Reports[targetIDUlong].Reports.Add(new StoredData.Report
            {
                Initiatior = reporter.userID,
                InitiatiorName = reporter.displayName,
                TargetName = targetName,
                Target = targetIDUlong,
                Type = type,
                Time = DateTime.Now
            });
        }

        // ReSharper disable once UnusedMember.Local
        private void OnNewSave(string filename)
        {
            LoadData();
            PrintWarning(
                $"Произошёл вайп, Backup файл был сохранен (oxide/data/{Name}_Backup/{Name}[{DateTime.Today.ToString("d").Replace("/", "_")}])");
            Backup_DataBase();
            _dataBase.Reports.Clear();
            SaveData();
        }

        // ReSharper disable once UnusedMember.Local
        private void Unload()
        {
            SaveData();
            foreach (var obj in _checks) UnityEngine.Object.Destroy(obj.Value);
        }

        #endregion

        #region [Commands] / [Команды]

        [ChatCommand("moderation")]
        private void OpenModerPanel(BasePlayer player)
        {
            if (!IsModerator(player.UserIDString)) return;
            CheckInDataBaseModerators(player.userID);
            DrawUI_ModeratorPanel(player);
        }

        [ConsoleCommand("zmoderation.pinfo")]
        private void OpenPlayerInfo(ConsoleSystem.Arg args)
        {
            if (!IsModerator(args.Player().UserIDString)) return;
            if (args.Player() == null) return;
            DrawUI_Player(args.Player(), Convert.ToUInt64(args.Args[0]));
        }

        [ConsoleCommand("zmoderation.menu")]
        private void OpenPlayerMenu(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            if (!IsModerator(args.Player().UserIDString)) return;
            DrawUI_Player_Menu(args.Player(), Convert.ToUInt64(args.Args[0]),
                Convert.ToBoolean(args.Args[1]));
        }

        [ConsoleCommand("zmoderation.check")]
        private void CheckPlayer(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            if (!IsModerator(args.Player().UserIDString)) return;
            var moderator = args.Player();
            var verifiablePlayer = BasePlayer.FindByID(Convert.ToUInt64(args.Args[0]));
            if (verifiablePlayer == null)
            {
                moderator.ChatMessage("Вы не можете вызвать на проверку игрока, который оффлайн");
                return;
            }

            if (moderator.userID == verifiablePlayer.userID)
            {
                moderator.ChatMessage("Вы не можете вызвать на проверку самого себя");
                return;
            }

            if (verifiablePlayer.IsAttacking())
            {
                moderator.ChatMessage("Вы не можете вызвать на проверку игрока во время перестрелки");
                return;
            }

            if (verifiablePlayer.GetComponent<Check.Verifiable>() != null)
            {
                moderator.ChatMessage("Данный игрок уже проверяется");
                return;
            }

            if (!_checks.ContainsKey(moderator))
            {
                var component = moderator.gameObject.AddComponent<Check>();
                component.verifiablePlayer = verifiablePlayer;
                _checks.Add(moderator, component);
            }
            else
            {
                _checks.Remove(moderator);
                var oldComponent = moderator.GetComponent<Check>();
                UnityEngine.Object.Destroy(oldComponent);
            }

            CuiHelper.DestroyUi(moderator, UILayerModeratorPanel);
        }

        [ConsoleCommand("zmoderation.input")]
        private void CheckInput(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (player.GetComponent<Check.Verifiable>() == null) return;
            var component = player.GetComponent<Check.Verifiable>();
            component.OnInputField(args.FullString);
        }

        [ConsoleCommand("zmoderation.sleeper")]
        private void TeleportToSleeper(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!args.Player().IsAdmin) return;
            var convertid = Convert.ToUInt64(args.Args[0]);
            if (!BasePlayer.FindSleeping(convertid))
            {
                player.ChatMessage("Игрока не существует");
                return;
            }

            var findplayer = BasePlayer.FindSleeping(Convert.ToUInt64(args.Args[0]));
            player.Teleport(findplayer.transform.position);
        }

        [ConsoleCommand("zmoderation.findteam")]
        private void FindTeam(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!args.Player().IsAdmin) return;
            var convertid = Convert.ToUInt64(args.Args[0]);
            var team = RelationshipManager.ServerInstance.FindPlayersTeam(convertid);
            if (team == null)
            {
                player.ChatMessage("Не найдено подходящих команд");
                return;
            }

            foreach (var member in team.members) player.ChatMessage(member.ToString());
        }

        [ConsoleCommand("zmoderation.find")]
        private void FindPlayer(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (args.Args.Length < 1)
            {
                DrawUI_PlayerList(player, 0, GetSortedReports());
                return;
            }

            var list = GetFindPlayers(args.Args[0]);
            DrawUI_PlayerList(player, 0, list);
        }

        [ConsoleCommand("zmoderation.verdict")]
        private void CheckVerdict(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            if (!IsModerator(args.Player().UserIDString)) return;
            var player = args.Player();
            if (player.GetComponent<Check>() == null) return;
            var component = player.GetComponent<Check>();
            if (!args.HasArgs()) component.DrawUI_Verdict();
            else component.OnVerdictConfirm(args.Args[0]);
        }

        [ConsoleCommand("zmoderation.close")]
        private void CloseModerPanel(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            if (!IsModerator(args.Player().UserIDString)) return;
            CuiHelper.DestroyUi(args.Player(), UILayerModeratorPanel);
        }

        [ConsoleCommand("zmoderation.page")]
        private void Pagination(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            if (!IsModerator(args.Player().UserIDString)) return;
            var suspectID = Convert.ToUInt64(args.Args[0]);
            var page = Convert.ToInt32(args.Args[1]);
            var reports = _dataBase.Reports[suspectID].Reports.Count / 8;
            if (page < 0) return;
            if (page > reports) return;

            var player = args.Player();
            DrawUI_ReportList(player, suspectID, page);
        }

        [ConsoleCommand("zmoderation.page.playerlist")]
        private void PaginationPlayerList(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            if (!IsModerator(args.Player().UserIDString)) return;
            var page = Convert.ToInt32(args.Args[0]);
            var reports = _dataBase.Reports.Count / 28;
            if (page < 0) return;
            if (page > reports) return;

            var player = args.Player();
            DrawUI_PlayerList(player, page, GetSortedReports());
        }

        [ConsoleCommand("zmoderation.checkpage")]
        private void PaginationCheck(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            if (!IsModerator(args.Player().UserIDString)) return;
            var suspectID = Convert.ToUInt64(args.Args[0]);
            var page = Convert.ToInt32(args.Args[1]);
            var reports = _dataBase.Reports[suspectID].ChecksDB.Count / 8;
            if (page < 0) return;
            if (page > reports) return;

            var player = args.Player();
            DrawUI_CheckList(player, suspectID, page);
        }

        #endregion

        #region [DataBase] / [База данных]

        public class StoredData
        {
            public readonly Dictionary<ulong, PObj> Reports = new Dictionary<ulong, PObj>();
            public readonly Dictionary<ulong, Moderator> ModeratorChecks = new Dictionary<ulong, Moderator>();

            public class Moderator
            {
                public string Displayname;
                public ulong UserId;
                public int ChecksCount;
                public readonly List<Checks> ChecksDB = new List<Checks>();
            }

            public class PObj
            {
                public string Displayname;
                public ulong UserId;
                public readonly List<Report> Reports = new List<Report>();
                public readonly List<Checks> ChecksDB = new List<Checks>();
            }

            public class Checks
            {
                public ulong Verifiable;
                public string VerifiableName;
                public ulong Verifier;
                public string VerifierName;
                public string Result;
                public DateTime Time;
                public int CheckDuration;
                public Statistic Stat = new Statistic();

                public class Statistic
                {
                    public int Kills;
                    public int Deaths;
                    public float Time;
                    public int Reports;
                }
            }

            public class Report
            {
                public ulong Initiatior;
                public string InitiatiorName;
                public ulong Target;
                public string TargetName;
                public string Type;
                public DateTime Time;
            }
        }

        #endregion

        #region [Helpers] / [Вспомогательные методы]

        private void Backup_DataBase()
        {
            Interface.Oxide.DataFileSystem.WriteObject(
                $"{Name}_Backup/{Name}[{DateTime.Today.ToString("d").Replace("/", "_")}]",
                _dataBase.Reports);
            Interface.Oxide.DataFileSystem.WriteObject(
                $"{Name}_Backup/{Name}[{DateTime.Today.ToString("d").Replace("/", "_")}]",
                _dataBase.ModeratorChecks);
        }

        private IEnumerable<KeyValuePair<ulong, StoredData.PObj>> GetFindPlayers(string name)
        {
            return !name.IsNumeric()
                ? _dataBase.Reports.ToList().Where(p => p.Value.Displayname.ToLower().Contains(name.ToLower()))
                : _dataBase.Reports.ToList().Where(p => p.Value.UserId.ToString().ToLower().Contains(name.ToLower()));
        }

        private IEnumerable<KeyValuePair<ulong, StoredData.PObj>> GetSortedReports()
        {
            return _dataBase.Reports.OrderByDescending(p => p.Value.Reports.Count);
        }

        private bool VerificationInstall()
        {
            var zealStatistics = plugins.Find("ZealStatisticsReborn");
            if (zealStatistics == null)
            {
                PrintError("Плагин ZealStatisticsReborn не установлен");
                return false;
            }

            return true;
        }

        private void PermissionsRegistration()
        {
            if (!permission.PermissionExists("zealmoderations.moderator"))
                permission.RegisterPermission("zealmoderations.moderator", this);

            if (!permission.PermissionExists("zealmoderations.admin"))
                permission.RegisterPermission("zealmoderations.admin", this);
        }

        private bool IsModerator(string userid)
        {
            return permission.UserHasPermission(userid, "zealmoderations.moderator");
        }

        private bool IsAdmin(string userid)
        {
            return permission.UserHasPermission(userid, "zealmoderations.admin");
        }

        private static void StartProcess(IEnumerator coroutine, string name)
        {
            ServerMgr.Instance.StartCoroutine(coroutine);
        }

        private static string GetFormatTime(DateTime time)
        {
            var hours = (DateTime.Now - time).Hours;
            switch (hours)
            {
                case -2: return "Менее 30 минут назад";
                case -1: return "Менее 1 часа назад";
                case 0: return "Примерно 1 час назад";
                case 1: return "Примерно 2 часа назад";
                case 2: return "Примерно 3 часа назад";
                case 3: return "Примерно 4 часа назад";
                case 4: return "Примерно 5 часов назад";
                case 5: return "Примерно 6 часов назад";
                case 6: return "Примерно 7 часов назад";
                case 7: return "Примерно 8 часов назад";
                case 8: return "Примерно 9 часов назад";
                case 9: return "Примерно 10 часов назад";
                case 10: return "Примерно 11 часов назад";
                case 11: return "Примерно 12 часов назад";
                case 12: return "Примерно 13 часов назад";
                case 13: return "Примерно 14 часов назад";
                case 14: return "Примерно 15 часов назад";
                case 15: return "Примерно 16 часов назад";
                case 16: return "Примерно 17 часов назад";
                case 17: return "Примерно 18 часов назад";
                case 18: return "Примерно 19 часов назад";
                case 19: return "Примерно 20 часов назад";
                case 20: return "Примерно 21 час назад";
                case 21: return "Примерно 22 часа назад";
                case 22: return "Примерно 23 часа назад";
                case 23: return "Больше 1 дня";
            }

            return "Неизвестно";
        }

        private IEnumerator CheckAllInDataBase()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CheckInDataBaseReports(player.userID, player.displayName);
                if (IsModerator(player.UserIDString)) CheckInDataBaseModerators(player.userID);
                yield return new WaitForSeconds(0.1f);
            }

            yield return 0;
        }

        private void CheckInDataBaseReports(ulong userID, string displayname = null)
        {
            if (displayname == null)
            {
                var player = BasePlayer.FindByID(userID);
                displayname = (player != null) ? player.displayName : "OFFLINE";
            }

            if (_dataBase.Reports.ContainsKey(userID)) return;
            _dataBase.Reports.Add(userID, new StoredData.PObj
            {
                Displayname = displayname,
                UserId = userID
            });
        }

        private void CheckInDataBaseModerators(ulong userID)
        {
            var player = BasePlayer.FindByID(userID);
            if (player == null) return;
            if (_dataBase.ModeratorChecks.ContainsKey(userID)) return;
            _dataBase.ModeratorChecks.Add(userID, new StoredData.Moderator
            {
                Displayname = player.displayName,
                UserId = player.userID
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