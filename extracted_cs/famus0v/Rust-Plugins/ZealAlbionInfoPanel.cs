using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConVar;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ZealAlbionInfoPanel", "Kira", "1.0.0")]
    [Description("Инфопанель для AlbionRust")]
    public class ZealAlbionInfoPanel : RustPlugin
    {
        #region [References] / [Ссылки]

        [PluginReference] private Plugin ImageLibrary;

        #endregion

        #region [Vars] / [Переменные]

        private TOD_Sky _sky;
        private static ZealAlbionInfoPanel _;

        private const string UILayer = "UI_MainPanel";


        #region [Sprites] / [Спрайты]

        private const string BlurInMenu = "assets/content/ui/uibackgroundblur-ingamemenu.mat";

        #endregion

        #endregion

        #region [Classes] / [Классы]

        public class Event
        {
            public string Name;
            public Type Type;
        }

        private class InfoPanelDataBase
        {
            public bool Events;
        }

        #endregion

        #region [Lists] / [Списки]

        private readonly Dictionary<string, string> _images = new Dictionary<string, string>
        {
            ["BGPanel"] = "https://i.imgur.com/Ed1jVI0.png",
            ["OnlineElem"] = "https://i.imgur.com/wDgEKJk.png",
            ["SleepersElem"] = "https://i.imgur.com/PePH1Xm.png",
            ["BGPanelLeft"] = "https://i.imgur.com/8pPvtWA.png",
            ["BGPanelRight"] = "https://i.imgur.com/XfUTCTz.png",
            ["BGElementEvents"] = "https://i.imgur.com/BMCGpSI.png",
            ["CargoShip"] = "https://i.imgur.com/xU8IUWO.png",
            ["CargoPlane"] = "https://i.imgur.com/CL41EJS.png",
            ["CH47"] = "https://i.imgur.com/jRIF5y8.png",
            ["Helicopter"] = "https://i.imgur.com/HtAifod.png",
            ["BradleyAPC"] = "https://i.imgur.com/j5cMDpt.png",
            ["ButtonRight"] = "https://i.imgur.com/YVEAst4.png",
            ["BGRemoveUI"] = "https://i.imgur.com/YCT4Sny.png"
        };

        public List<Event> Events = new List<Event>
        {
            new Event
            {
                Name = "CargoPlane",
                Type = typeof(CargoPlane)
            },
            new Event
            {
                Name = "CargoShip",
                Type = typeof(CargoShip)
            },
            new Event
            {
                Name = "BradleyAPC",
                Type = typeof(BradleyAPC)
            },
            new Event
            {
                Name = "CH47",
                Type = typeof(CH47Helicopter)
            },
            new Event
            {
                Name = "Helicopter",
                Type = typeof(PatrolHelicopter)
            }
        };

        private List<string> Tickers = new List<string>
        {
            "Вы можете получить набор через команду <color=#cd4228>/kit</color>",
            "Отправить жалобу <color=#cd4228>/report</color>",
            "Автопринятие тп <color=#cd4228>/atp add ник</color>",
            "Вызвать игрока на дуэль <color=#cd4228>/duel ник</color>",
            "Добавить в друзья <color=#cd4228>/friend add</color> ник",
            "Забрать предметы с магазина <color=#cd4228>/store</color>",
            "Группа сервера <color=#cd4228>https://vk.com/albionrust</color>",
            "Удалить свою постройку <color=#cd4228>/remove</color>",
            "Правила сервера <color=#cd4228>/info</color>",
            "Список команд сервера <color=#cd4228>/info</color>",
            "Подключи оповещение о рейде  <color=#cd4228>/ra vk vk.com/id</color>",
            "Включить огонь по друзьям  <color=#cd4228>/clan ff /id</color>",
            "Вставить картинку  <color=#cd4228>/sil url</color>",
            "Скрафтить LR300, коптер, гроб и многое другое <color=#cd4228>/craft</color>"
        };

        private readonly Dictionary<ulong, InfoPanelDataBase> _dataBasePlayers =
            new Dictionary<ulong, InfoPanelDataBase>();

        private static readonly List<string> EventsOnServer = new List<string> ();

        #endregion

        #region [MonoBehaviours]

        public class InfoPanelPlayer : MonoBehaviour
        {
            public BasePlayer player;
            public int tickerNum;

            public void Awake()
            {
                tickerNum = 0;
                player = GetComponent<BasePlayer>();
                if (!player.IsAdmin) Destroy(this);
                Invoke(nameof(DrawUI_MainPanel), 1f);
                InvokeRepeating(nameof(RefreshPanel), 1f, 3f);
                InvokeRepeating(nameof(TickerTimer), 10, 10);
            }

            public void DrawUI_MainPanel()
            {
                var ui = new CuiElementContainer();

                ui.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image =
                    {
                        Color = "0 0 0 0"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.34401 0.1129629",
                        AnchorMax = "0.64 0.1472222"
                    }
                }, "Hud", UILayer);

                ui.Add(new CuiElement
                {
                    Name = "PanelBG",
                    Parent = UILayer,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = _.GetImage("BGPanel"),
                            Sprite = BlurInMenu
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = "PanelOnlineBG",
                    Parent = UILayer,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = _.GetImage("BGPanelLeft"),
                            Sprite = BlurInMenu
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "-0.08051494 -2.513508",
                            AnchorMax = "0.09748507 1.004001"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = "PanelEventsBG",
                    Parent = UILayer,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = _.GetImage("BGPanelRight"),
                            Sprite = BlurInMenu
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.8998195 -2.513508",
                            AnchorMax = "1.082791 1.004001"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = "BGButtonRight",
                    Parent = "PanelEventsBG",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = _.GetImage("ButtonRight"),
                            Sprite = BlurInMenu
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.8452537 0",
                            AnchorMax = "1 0.713065"
                        }
                    }
                });

                ui.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image =
                    {
                        Color = HexToRustFormat("#9C8E8217"),
                        Material = BlurInMenu
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.000881514 -3.297291",
                        AnchorMax = "0.999 -2.648643"
                    }
                }, UILayer, UILayer + "Ticker");

                CuiHelper.AddUi(player, ui);
                if (!_._dataBasePlayers.ContainsKey(player.userID))
                    _._dataBasePlayers.Add(player.userID, new InfoPanelDataBase
                    {
                        Events = false
                    });
                DrawUI_EventButton();
                DrawUI_Ticker();
            }

            public void DrawUI_Time()
            {
                var time = _._sky.Cycle?.DateTime.ToString("HH:mm");
                var ui = new CuiElementContainer();

                ui.Add(new CuiElement
                {
                    Name = "TimeTXT",
                    Parent = "PanelBG",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Text = time,
                            Color = HexToRustFormat("#A8A6A5"),
                            FontSize = 20
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.3743411 0",
                            AnchorMax = "0.6186292 1"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#00000042"),
                            Distance = "0.5 0.5"
                        }
                    }
                });

                CuiHelper.DestroyUi(player, "TimeTXT");
                CuiHelper.AddUi(player, ui);
            }

            public void DrawUI_Online()
            {
                for (var i = 0; i < 10; i++) CuiHelper.DestroyUi(player, $"ElementO{i}");
                for (var i = 0; i < 10; i++) CuiHelper.DestroyUi(player, $"ElementS{i}");
                CuiHelper.DestroyUi(player, "OnlineTXT");
                CuiHelper.DestroyUi(player, "SleepersTXT");

                var ui = new CuiElementContainer();

                for (var i = 0; i < 10; i++)
                {
                    ui.Add(new CuiElement
                    {
                        Name = $"ElementO{i}",
                        Parent = UILayer,
                        FadeOut = i * 0.2f,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = _.GetImage("OnlineElem"),
                                Color = "1 1 1 0.8",
                                FadeIn = i * 0.5f
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0.2126548 - (i * 0.0238471)} 0",
                                AnchorMax = $"{0.3568023 - (i * 0.0238471)} 1"
                            }
                        }
                    });
                }

                for (var i = 0; i < 10; i++)
                {
                    ui.Add(new CuiElement
                    {
                        Name = $"ElementS{i}",
                        Parent = UILayer,
                        FadeOut = i * 0.2f,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = _.GetImage("SleepersElem"),
                                Color = "1 1 1 0.8",
                                FadeIn = i * 0.5f
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0.6432332 + (i * 0.0238471)} 0",
                                AnchorMax = $"{0.7873831 + (i * 0.0238471)} 1"
                            }
                        }
                    });
                }

                ui.Add(new CuiElement
                {
                    Name = "OnlineTXT",
                    Parent = UILayer,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Text = $"{BasePlayer.activePlayerList.Count}",
                            Color = HexToRustFormat("#9C8E82D5"),
                            FontSize = 16
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0.3818403 1"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#00000027"),
                            Distance = "1 1"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = "SleepersTXT",
                    Parent = UILayer,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Text = $"{BasePlayer.sleepingPlayerList.Count + 23}",
                            Color = HexToRustFormat("#9C8E82D5"),
                            FontSize = 16
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.6185122 0",
                            AnchorMax = "1 1"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#00000027"),
                            Distance = "1 1"
                        }
                    }
                });

                CuiHelper.AddUi(player, ui);
            }

            public void DrawUI_Events()
            {
                var ui = new CuiElementContainer();

                ui.Add(new CuiElement
                {
                    Name = "EventBG",
                    Parent = UILayer,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = _.GetImage("BGElementEvents")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "1.094903 -2.513508",
                            AnchorMax = "1.157093 0.004001856"
                        }
                    }
                });

                if (EventsOnServer.Count == 0)
                {
                    ui.Add(new CuiElement
                    {
                        Name = "EventNull",
                        Parent = "EventBG",
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Align = TextAnchor.MiddleCenter,
                                FontSize = 10,
                                Text = "П\nУ\nС\nТ\nО",
                                Color = HexToRustFormat("#A8A6A5")
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "1 1"
                            },
                            new CuiOutlineComponent
                            {
                                Color = HexToRustFormat("#0000005C"),
                                Distance = "0.5 0.5"
                            }
                        }
                    });
                }
                else
                {
                    var y = 0;
                    foreach (var @event in EventsOnServer)
                    {
                        ui.Add(new CuiElement
                        {
                            Name = $"Event{@event}",
                            Parent = "EventBG",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = _.GetImage(@event),
                                    Color = HexToRustFormat("#A8A6A5")
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"0.0847789 {0.8064414 - (y * 0.1935587)}",
                                    AnchorMax = $"0.85 {0.9677932 - (y * 0.1935587)}"
                                },
                                new CuiOutlineComponent
                                {
                                    Color = HexToRustFormat("#0000005C"),
                                    Distance = "0.4 0.4"
                                }
                            }
                        });
                        y++;
                    }
                }

                CuiHelper.DestroyUi(player, "EventBG");
                CuiHelper.AddUi(player, ui);
            }

            public void DrawUI_Ticker()
            {
                var ui = new CuiElementContainer();

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#9C8E82D5"),
                        FontSize = 12,
                        Text = _.Tickers[tickerNum]
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0.2",
                        AnchorMax = "1 1"
                    }
                }, UILayer + "Ticker", "Ticker");

                CuiHelper.DestroyUi(player, "Ticker");
                CuiHelper.AddUi(player, ui);
                tickerNum++;
            }

            public void TickerTimer()
            {
                if (tickerNum > (_.Tickers.Count - 1)) tickerNum = 0;
                DrawUI_Ticker();
            }

            public void RefreshPanel()
            {
                if (player == null) Destroy(this);
                DrawUI_Time();
                DrawUI_Online();
            }

            public void DrawUI_Remove()
            {
                if (!player.IsAdmin) return;
                var ui = new CuiElementContainer();

                ui.Add(new CuiElement
                {
                    Name = "BGRemoveUI",
                    Parent = UILayer,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = (string) _.ImageLibrary.Call("GetImage", "BGRemoveUI")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "-0.162747 0.08108227",
                            AnchorMax = "0.00989224 0.9916216"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = "RemoveIC",
                    Parent = "BGRemoveUI",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Sprite = "assets/icons/clear.png",
                            Color = HexToRustFormat("#A8A6A5D7")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.04205611 0.2141322",
                            AnchorMax = "0.2523366 0.8259386"
                        }
                    }
                });

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = "<b>30 с</b>",
                        FontSize = 11,
                        Font = "robotocondensed-regular.ttf",
                        Align = TextAnchor.MiddleLeft,
                        Color = HexToRustFormat("#A8A6A5D7")
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.3 0.05",
                        AnchorMax = "1 1"
                    }
                }, "BGRemoveUI");

                CuiHelper.AddUi(player, ui);
            }

            public void DrawUI_EventButton()
            {
                CuiHelper.DestroyUi(player, "ButtonRight");
                var ui = new CuiElementContainer();

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Command = "infopanel.events",
                        Color = "0 0 0 0"
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#A8A6A5D5"),
                        Text = _._dataBasePlayers[player.userID].Events ? "<" : ">",
                        FontSize = 14
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, "BGButtonRight", "ButtonRight");

                CuiHelper.AddUi(player, ui);
            }

            public void OnDestroy()
            {
                if (_._dataBasePlayers.ContainsKey(player.userID)) _._dataBasePlayers.Remove(player.userID);
                CuiHelper.DestroyUi(player, UILayer);
                Destroy(this);
            }
        }

        #endregion

        #region [Hooks] / [Крюки]

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            StartCoroutine(Load_Images());
            GetActiveEvents();
            _sky = TOD_Sky.Instance;
            _ = this;
            StartCoroutine(Load_Component_InfoPanel());
        }

        // ReSharper disable once UnusedMember.Local
        private void OnPlayerConnected(BasePlayer player)
        {
            NextTick(() =>
            {
                player.gameObject.AddComponent<InfoPanelPlayer>();
                PrintToChat($"Подключился игрок <color=#c877d8>{player.displayName}</color>");
            });
        }

        // ReSharper disable once UnusedMember.Local
        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            NextTick(() =>
            {
                if (player.IsAdmin) return;
                PrintToChat($"Отключился игрок <color=#c877d8>{player.displayName}</color>");
                UnityEngine.Object.Destroy(player.GetComponent<InfoPanelPlayer>());
            });
        }

        private static void GetActiveEvents()
        {
            if (UnityEngine.Object.FindObjectOfType(typeof(BaseHelicopter)) != null)
                if (!EventsOnServer.Contains("Helicopter")) EventsOnServer.Add("Helicopter");
                else if (EventsOnServer.Contains("Helicopter")) EventsOnServer.Remove("Helicopter");

            if (UnityEngine.Object.FindObjectOfType(typeof(BradleyAPC)) != null)
                if (!EventsOnServer.Contains("BradleyAPC")) EventsOnServer.Add("BradleyAPC");
                else if (EventsOnServer.Contains("BradleyAPC")) EventsOnServer.Remove("BradleyAPC");

            if (UnityEngine.Object.FindObjectOfType(typeof(CargoPlane)) != null)
                if (!EventsOnServer.Contains("CargoPlane")) EventsOnServer.Add("CargoPlane");
                else if (EventsOnServer.Contains("CargoPlane")) EventsOnServer.Remove("CargoPlane");

            if (UnityEngine.Object.FindObjectOfType(typeof(CargoShip)) != null)
                if (!EventsOnServer.Contains("CargoShip")) EventsOnServer.Add("CargoShip");
                else if (EventsOnServer.Contains("CargoShip")) EventsOnServer.Remove("CargoShip");

            if (UnityEngine.Object.FindObjectOfType(typeof(CH47Helicopter)) != null)
                if (!EventsOnServer.Contains("CH47")) EventsOnServer.Add("CH47");
                else if (EventsOnServer.Contains("CH47")) EventsOnServer.Remove("CH47");
        }

        // ReSharper disable once UnusedMember.Local
        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity is BaseHelicopter)
                if (!EventsOnServer.Contains("Helicopter"))
                    ChangeEventList("Helicopter", false);

            if (entity is BradleyAPC)
                if (!EventsOnServer.Contains("BradleyAPC"))
                    ChangeEventList("BradleyAPC", false);

            if (entity is CargoPlane)
                if (!EventsOnServer.Contains("CargoPlane"))
                    ChangeEventList("CargoPlane", false);

            if (entity is CargoShip)
                if (!EventsOnServer.Contains("CargoShip"))
                    ChangeEventList("CargoShip", false);

            if (entity is CH47Helicopter)
                if (!EventsOnServer.Contains("CH47"))
                    ChangeEventList("CH47", false);
        }

        // ReSharper disable once UnusedMember.Local
        private void OnEntityKill(BaseNetworkable entity)
        {
            if (entity is BaseHelicopter)
                if (EventsOnServer.Contains("Helicopter"))
                    ChangeEventList("Helicopter", true);

            if (entity is BradleyAPC)
                if (EventsOnServer.Contains("BradleyAPC"))
                    ChangeEventList("BradleyAPC", true);

            if (entity is CargoPlane)
                if (EventsOnServer.Contains("CargoPlane"))
                    ChangeEventList("CargoPlane", true);

            if (entity is CargoShip)
                if (EventsOnServer.Contains("CargoShip"))
                    ChangeEventList("CargoShip", true);

            if (entity is CH47Helicopter)
                if (EventsOnServer.Contains("CH47"))
                    ChangeEventList("CH47", true);
        }

        // ReSharper disable once UnusedMember.Local
        private void Unload()
        {
            StartCoroutine(Destroy_Component_InfoPanel());
            foreach (var obj in UnityEngine.Object.FindObjectsOfType(typeof(InfoPanelPlayer)))
                UnityEngine.Object.Destroy(obj);
        }

        #endregion

        #region [ConsoleCommands] / [Консольные команды]

        [ConsoleCommand("infopanel.events")]
        // ReSharper disable once UnusedMember.Local
        private void Switch_Events(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            _dataBasePlayers[player.userID].Events = Switch(_dataBasePlayers[player.userID].Events);
            if (_dataBasePlayers[player.userID].Events) player.GetComponent<InfoPanelPlayer>().DrawUI_Events();
            else CuiHelper.DestroyUi(player, "EventBG");

            player.GetComponent<InfoPanelPlayer>().DrawUI_EventButton();
        }

        [ConsoleCommand("closed")]
        // ReSharper disable once UnusedMember.Local
        private void Closed(ConsoleSystem.Arg args)
        {
            foreach (var plobj in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(plobj, UILayer);
            }
        }

        [ConsoleCommand("panel.test")]
        // ReSharper disable once UnusedMember.Local
        private void Test(ConsoleSystem.Arg args)
        {
            Puts(
                $"Online : {BasePlayer.activePlayerList.Count} | Sleepers : {BasePlayer.sleepingPlayerList.Count + 23} | Users : {covalence.Players.All.Count()}");
        }

        #endregion

        #region [Helpers] / [Вспомогательный код]

        private static bool Switch(bool name)
        {
            var switchedBool = !name;
            return switchedBool;
        }

        private IEnumerator UpdateEventPanel()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (_dataBasePlayers[player.userID].Events) player.GetComponent<InfoPanelPlayer>().DrawUI_Events();

                yield return new WaitForSeconds(0.1f);
            }

            yield return 0;
        }

        private void ChangeEventList(string name, bool remove)
        {
            if (!remove) EventsOnServer.Add(name);
            else EventsOnServer.Remove(name);

            ServerMgr.Instance.StartCoroutine(UpdateEventPanel());
        }

        private static void StartCoroutine(IEnumerator name)
        {
            ServerMgr.Instance.StartCoroutine(name);
        }

        private IEnumerator Load_Images()
        {
            var countImages = 0;
            foreach (var img in _images)
            {
                ImageLibrary.Call("AddImage", img.Value, img.Key);
                countImages++;

                yield return new WaitForSeconds(0f);
            }

            PrintWarning($"Загружено {countImages} из {_images.Count} изображений");
            yield return 0;
        }

        private static IEnumerator Load_Component_InfoPanel()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.GetComponent<InfoPanelPlayer>() != null) 
                    player.GetComponent<InfoPanelPlayer>().OnDestroy();
                player.gameObject.AddComponent<InfoPanelPlayer>();
                yield return new WaitForSeconds(0.5f);
            }

            yield return 0;
        }

        private static IEnumerator Destroy_Component_InfoPanel()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.gameObject.GetComponent<InfoPanelPlayer>() != null)
                    UnityEngine.Object.Destroy(player.GetComponent<InfoPanelPlayer>());
                yield return new WaitForSeconds(0.1f);
            }

            yield return 0;
        }

        private string GetImage(string name)
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