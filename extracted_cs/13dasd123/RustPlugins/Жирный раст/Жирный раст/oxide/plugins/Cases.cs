using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Cases", "VooDoo", "1.0.0")]
    public class Cases : RustPlugin
    {
        public static Cases instance;
        [PluginReference] Plugin XMenu;
        [PluginReference] Plugin Notifications;
        [PluginReference] Plugin ImageLibrary;
        bool AddImage(string url, string imageName, ulong imageId, Action callback = null) => (bool)ImageLibrary.Call("AddImage", url, imageName, imageId, callback);
        string GetImage(string imageName, ulong imageId = 0, bool returnUrl = false) => (string)ImageLibrary.Call("GetImage", imageName, imageId, returnUrl);

        #region ClassCase
        private static Dictionary<string, string> shortname2Sprite = new Dictionary<string, string>();
        private Dictionary<ulong, int> Users = new Dictionary<ulong, int>();
        private List<Case> CaseList = new List<Case>();

        public class Case
        {
            public string Name { get; set; }
            public string DisplayName { get; set; }

            public bool Type { get; set; }
            public int Cost { get; set; }

            public string Png { get; set; }
            public string Command { get; set; }
            public List<CaseItems> Items { get; set; }

            public List<ulong> UsersOpen { get; set; }
        }

        public class CaseItems
        {
            public string ShortName { get; set; }
            public string Container { get; set; }

            public int Amount { get; set; }
        }
        #endregion

        #region uMod Hook's
        Timer TimerInitialize;
        void OnServerInitialized()
        {
            instance = this;
            shortname2Sprite = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, string>>("shortname2Sprite");
            CaseList = Interface.Oxide.DataFileSystem.ReadObject<List<Case>>("Cases");
            Users = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, int>>("Cases_Users");

            foreach (var _case in CaseList)
                AddImage("file://" + Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar + "Case" + Path.DirectorySeparatorChar + _case.Png, _case.Name, 0);

            foreach (var player in BasePlayer.activePlayerList) { OnPlayerConnected(player); }
            timer.Repeat(60, 0, () =>
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    if (!Users.ContainsKey(player.userID)) continue;
                    Users[player.userID] += 1;
                }
            });

            TimerInitialize = timer.Every(5f, () =>
            {
                if (XMenu.IsLoaded)
                {
                    XMenu.Call("API_RegisterMenu", this.Name, "Cases", "assets/icons/open.png", "RenderCases", null);
                    cmd.AddChatCommand("case", this, (p, cmd, args) => rust.RunClientCommand(p, "custommenu true Cases"));
                    TimerInitialize.Destroy();
                }
            });

        }

        void Unload()
        {
            Interface.Oxide.DataFileSystem.WriteObject("Cases", CaseList);
            Interface.Oxide.DataFileSystem.WriteObject("Cases_Users", Users);
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player == null) return;
            if (player.IsReceivingSnapshot)
            {
                timer.Once(1f, () => OnPlayerConnected(player));
                return;
            }
            if (!Users.ContainsKey(player.userID))
                Users.Add(player.userID, 0);
        }


        private void OnServerSave()
        {
            Interface.Oxide.DataFileSystem.WriteObject("Cases", CaseList);
            Interface.Oxide.DataFileSystem.WriteObject("Cases_Users", Users);
        }
        #endregion

        #region UI
        public const string MenuLayer = "XMenu";
        public const string MenuItemsLayer = "XMenu.MenuItems";
        public const string MenuSubItemsLayer = "XMenu.MenuSubItems";
        public const string MenuContent = "XMenu.Content";

        private void RenderCases(ulong userID, object[] objects)
        {
            CuiElementContainer Container = (CuiElementContainer)objects[0];
            bool FullRender = (bool)objects[1];
            string Name = (string)objects[2];
            int ID = (int)objects[3];
            int Page = (int)objects[4];
            string Args = (string)objects[5];

            BasePlayer player = BasePlayer.FindByID(userID);
            int UserTime = GetUserTime(userID);
            int UserMoney = GetUserMoney(player);

            Container.Add(new CuiElement
            {
                Name = MenuContent,
                Parent = MenuLayer,
                Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0 0 0 0",
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-430 -250",
                            OffsetMax = "490 270"
                        },
                    }
            });

            Container.Add(new CuiElement
            {
                Name = MenuContent + ".Balance",
                Parent = MenuContent,
                Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0 0 0 0.5",
                        },
                        new CuiRectTransformComponent
                        {
                             AnchorMin = "0 1",
                             AnchorMax = "0 1",
                             OffsetMin = $"{40} {0 - 4 * 125}",
                             OffsetMax = $"{880} {60 - 4 * 125}"
                        }
                    }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + $".UserMoney",
                Parent = MenuContent + ".Balance",
                Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = $"<color=#fff9f9AA><size=8>Для новичков: Чтобы получить бонус, Вам необходимо быть авторизованным в нашем магазине - TRASHRUST.RU и совершить какое-нибудь действие! Например, покрутить бесплатную рулетку!</size></color>\n<b><color=#fff9f9AA>ОБЩЕЕ ПРОВЕДЁННОЕ ВРЕМЯ ВАШЕЙ СЛУЖБЫ НА СЕРВЕРЕ:</color> <color=#FFAA00>{TimeExtensions.FormatTime(TimeSpan.FromMinutes(UserTime))}</color></b>",
                                    Align = TextAnchor.MiddleCenter,
                                    FontSize = 16,
                                    Font = "robotocondensed-regular.ttf",
                                    FadeIn = 0.5f,
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0.1 0.05",
                                    AnchorMax = "0.9 0.95",
                                }
                            }
            });
            for (int i = 0, x = 0, y = 0; i < CaseList.Count; i++, x++)
            {
                if (x == 5)
                {
                    x = 0;
                    y++;
                }
                string BtnColor = "1 0.25 0 0.75";
                string MessageCost = "", MessageGet = "";
                string Time = TimeExtensions.FormatShortTime(TimeSpan.FromMinutes(CaseList.ElementAt(i).Cost - UserTime));
                bool CanTake = false;

                if (CaseList.ElementAt(i).Type)
                {
                    MessageCost = $"<size=10>Стоимость: \n{CaseList.ElementAt(i).Cost} монет</size>";
                    MessageGet = $"Купить";
                    if (CaseList.ElementAt(i).Cost <= UserMoney)
                    {
                        BtnColor = "0.25 1 0 0.6";
                        CanTake = true;
                    }
                    else
                    {
                        MessageGet = $"Недостаточно";
                    }
                }
                else
                {
                    MessageCost = $"<size=10>Можно брать</size>";
                    MessageGet = $"Взять";
                    if (CaseList.ElementAt(i).Cost <= UserTime)
                    {
                        BtnColor = "0.25 1 0 0.6";
                        CanTake = true;
                    }
                    else
                    {
                        MessageCost = $"<size=10>Через: {Time}</size>";
                        MessageGet = $"Подождите";
                    }
                }

                if (CaseList.ElementAt(i).UsersOpen.Contains(player.userID))
                {
                    BtnColor = "0.77 0.48 0.00 1.00";
                    MessageCost = $"";
                    MessageGet = $"Получено";
                    CanTake = false;
                }

                /*if (!IsStoreAuthed(userID))
                {
                    BtnColor = "1 0.25 0 0.75";
                    MessageGet = $"<size=7>Авторизуйся в магазине</size>";
                    CanTake = false;
                }*/

                Container.Add(new CuiElement
                {
                    Name = MenuContent + $".Case.{i}",
                    Parent = MenuContent,
                    Components =
                            {
                                new CuiImageComponent
                                {
                                    Color = "0 0 0 0.5",
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = $"{40 + x * 165} {-100 - y * 85}",
                                    OffsetMax = $"{200 + x * 165} {-20 - y * 85}"
                                }
                            }
                });

                Container.Add(new CuiElement
                {
                    Name = MenuContent + $".Case.{i}.Background",
                    Parent = MenuContent + $".Case.{i}",
                    Components =
                                {
                                    new CuiImageComponent
                                    {
                                        Color = "1 1 1 0.05",
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0.025 0.05",
                                        AnchorMax = "0.475 0.95",
                                    }
                                }
                });
                Container.Add(new CuiElement
                {
                    Name = MenuContent + $".Case.{i}.Img",
                    Parent = MenuContent + $".Case.{i}",
                    Components =
                                {
                                    new CuiRawImageComponent
                                    {
                                        Png = instance.GetImage(CaseList.ElementAt(i).Name),
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0.025 0.05",
                                        AnchorMax = "0.475 0.95",
                                    }
                                }
                });

                Container.Add(new CuiButton
                {
                    Button = { Color = "1 1 1 0" },
                    RectTransform = { AnchorMin = "0.025 0.05", AnchorMax = "0.475 0.95", },
                    Text = { Text = "", Align = TextAnchor.MiddleCenter }
                }, MenuContent + $".Case.{i}", MenuContent + $".Content.Case.{i}.BtnInfo");
                Container.Add(new CuiElement
                {
                    Name = MenuContent + $".Case.{i}.Title",
                    Parent = MenuContent + $".Case.{i}",
                    Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = $"<color=#fff9f9AA>{CaseList.ElementAt(i).DisplayName}</color>",
                                    Align = TextAnchor.MiddleCenter,
                                    FontSize = 10,
                                    Font = "robotocondensed-regular.ttf",
                                    FadeIn = 0.5f,
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0.5 0.7",
                                    AnchorMax = "1 1",
                                },
                                new CuiOutlineComponent
                                {
                                         Color = "0 0 0 1",
                                         Distance = "-0.5 0.5"
                                }
                            }
                });
                Container.Add(new CuiElement
                {
                    Name = MenuContent + $".Case.{i}.BtnBackground",
                    Parent = MenuContent + $".Case.{i}",
                    Components =
                            {
                                new CuiImageComponent
                                {
                                    Color = BtnColor,
                                    FadeIn = 0.5f,
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0.525 0.05",
                                    AnchorMax = "0.95 0.3",
                                },
                            }
                });
                Container.Add(new CuiElement
                {
                    Name = MenuContent + $".Case.{i}.Timer",
                    Parent = MenuContent + $".Case.{i}",
                    Components =
                    {
                        new CuiTextComponent
                        {
                             Text = $"<color=#fff9f9AA>{MessageCost}</color>",
                             Align = TextAnchor.LowerCenter,
                             FontSize = 11,
                             Font = "robotocondensed-regular.ttf",
                             FadeIn = 0.5f,
                        },
                        new CuiRectTransformComponent
                        {
                             AnchorMin = "0.5 0.35",
                             AnchorMax = "1 0.7",
                        }
                    }
                });
                Container.Add(new CuiElement
                {
                    Name = MenuContent + $".Case.{i}.BtnTitle",
                    Parent = MenuContent + $".Case.{i}",
                    Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"<color=#fff9f9AA>{MessageGet}</color>",
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 11,
                                        Font = "robotocondensed-regular.ttf",
                                        FadeIn = 0.5f,
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0.525 0.05",
                                        AnchorMax = "0.95 0.3",
                                    }
                                }
                });

                if (CanTake)
                {
                    Container.Add(new CuiButton
                    {
                        Button = { Color = "1 1 1 0", Command = $"case {CaseList.ElementAt(i).Name} {i} {x} {y}" },
                        RectTransform = { AnchorMin = "0.525 0.05", AnchorMax = "0.95 0.3" },
                        Text = { Text = "", Align = TextAnchor.MiddleCenter }
                    }, MenuContent + $".Case.{i}", MenuContent + $".Content.Case.{i}.BtnTitle");
                }
            }
        }

        private static void ShowCaseInfo(BasePlayer player, Case _case)
        {
            CuiHelper.DestroyUi(player, MenuContent);
            CuiElementContainer Container = new CuiElementContainer();
            Container.Add(new CuiElement
            {
                Name = MenuContent,
                Parent = MenuLayer,
                Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0 0 0 0",
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-430 -290",
                            OffsetMax = "490 290"
                        },
                    }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + $".Title",
                Parent = MenuContent,
                Components =
                    {
                        new CuiTextComponent
                        {
                            Text = $"<color=#fffffAA>Содержимое кейса <color=#FFAA00AA>{_case.DisplayName}</color></color>",
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 36,
                            Font = "robotocondensed-regular.ttf",
                            FadeIn = 0.5f,
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = $"0 -50",
                            OffsetMax = $"920 0",
                        }
                    }
            });
            for (int i = 0, x = 0, y = 0; i < _case.Items.Count; i++, x++)
            {
                if (x == 12)
                {
                    x = 0; y++;
                }
                Container.Add(new CuiElement
                {
                    Name = MenuContent + $".Item.{i}",
                    Parent = MenuContent,
                    Components =
                                {
                                    new CuiImageComponent
                                    {
                                        Color = "1 1 1 0.1"
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"{25 + x * 55} {-125 - y * 55}",
                                        OffsetMax = $"{75 + x * 55} {-75 - y * 55}"
                                    }
                                }
                });
                Container.Add(new CuiElement
                {
                    Name = MenuContent + $".Item.{i}",
                    Parent = MenuContent,
                    Components =
                                {
                                    new CuiRawImageComponent
                                    {
                                        Png = instance.GetImage(_case.Items.ElementAt(i).ShortName),
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"{25 + x * 55} {-125 - y * 55}",
                                        OffsetMax = $"{75 + x * 55} {-75 - y * 55}"
                                    }
                                }
                });
                Container.Add(new CuiElement
                {
                    Name = MenuContent + $".Item.{i}",
                    Parent = MenuContent,
                    Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"<color=#fff9f9AA>{_case.Items.ElementAt(i).Amount}</color>",
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 14,
                                        Font = "robotocondensed-regular.ttf",
                                        FadeIn = 0.5f,
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0 1",
                                        AnchorMax = "0 1",
                                        OffsetMin = $"{25 + x * 55} {-125 - y * 55}",
                                        OffsetMax = $"{75 + x * 55} {-75 - y * 55}"
                                    }
                                }
                });
            }
            CuiHelper.AddUi(player, Container);
        }

        private static void ReRenderCase(BasePlayer player, Case _case, int number, int positionX, int positionY)
        {
            CuiHelper.DestroyUi(player, MenuContent + $".Case.{number}");

            CuiElementContainer Container = new CuiElementContainer();
            int UserTime = instance.GetUserTime(player.userID);
            int UserMoney = instance.GetUserMoney(player);
            string BtnColor = "1 0.25 0 0.75";
            string MessageCost = "", MessageGet = "";
            string Time = TimeExtensions.FormatShortTime(TimeSpan.FromMinutes(instance.CaseList.ElementAt(number).Cost - UserTime));
            bool CanTake = false;

            if (instance.CaseList.ElementAt(number).Type)
            {
                MessageCost = $"<size=10>Стоимость: \n{instance.CaseList.ElementAt(number).Cost} монет</size>";
                MessageGet = $"Купить";
                if (instance.CaseList.ElementAt(number).Cost <= UserMoney)
                {
                    BtnColor = "0.25 1 0 0.6";
                    CanTake = true;
                }
                else
                {
                    MessageGet = $"Недостаточно";
                }
            }
            else
            {
                MessageCost = $"<size=10>Можно брать</size>";
                MessageGet = $"Взять";
                if (instance.CaseList.ElementAt(number).Cost <= UserTime)
                {
                    BtnColor = "0.25 1 0 0.6";
                    CanTake = true;
                }
                else
                {
                    MessageCost = $"<size=10>Таймер: {Time}</size>";
                    MessageGet = $"Подождите";
                }
            }

            if (instance.CaseList.ElementAt(number).UsersOpen.Contains(player.userID))
            {
                BtnColor = "1 0.25 0 0.75";
                MessageCost = $"";
                MessageGet = $"Уже взят";
                CanTake = false;
            }

            Container.Add(new CuiElement
            {
                Name = MenuContent + $".Case.{number}",
                Parent = MenuContent,
                Components =
                            {
                                new CuiImageComponent
                                {
                                    Color = "0 0 0 0.5",
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = $"{40 + positionX * 165} {-100 - positionY * 85}",
                                    OffsetMax = $"{200 + positionX * 165} {-20 - positionY * 85}"
                                }
                            }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + $".Case.{number}.Background",
                Parent = MenuContent + $".Case.{number}",
                Components =
                                {
                                    new CuiImageComponent
                                    {
                                        Color = "1 1 1 0.05",
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0.025 0.05",
                                        AnchorMax = "0.475 0.95",
                                    }
                                }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + $".Case.{number}.Img",
                Parent = MenuContent + $".Case.{number}",
                Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = instance.GetImage(_case.Name),
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0.025 0.05",
                                    AnchorMax = "0.475 0.95",
                                }
                            }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + $".Case.{number}.Title",
                Parent = MenuContent + $".Case.{number}",
                Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = $"<color=#fff9f9AA>{instance.CaseList.ElementAt(number).DisplayName}</color>",
                                    Align = TextAnchor.MiddleCenter,
                                    FontSize = 10,
                                    Font = "robotocondensed-regular.ttf",
                                    FadeIn = 0.5f,
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0.5 0.7",
                                    AnchorMax = "1 1",
                                }
                            }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + $".Case.{number}.BtnBackground",
                Parent = MenuContent + $".Case.{number}",
                Components =
                            {
                                new CuiImageComponent
                                {
                                    Color = BtnColor,
                                    FadeIn = 0.5f,
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0.525 0.05",
                                    AnchorMax = "0.95 0.3",
                                },
                            }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + $".Case.{number}.Timer",
                Parent = MenuContent + $".Case.{number}",
                Components =
                    {
                        new CuiTextComponent
                        {
                             Text = $"<color=#fff9f9AA>{MessageCost}</color>",
                             Align = TextAnchor.LowerCenter,
                             FontSize = 11,
                             Font = "robotocondensed-regular.ttf",
                             FadeIn = 0.5f,
                        },
                        new CuiRectTransformComponent
                        {
                             AnchorMin = "0.5 0.35",
                             AnchorMax = "1 0.7",
                        }
                    }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + $".Case.{number}.BtnTitle",
                Parent = MenuContent + $".Case.{number}",
                Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"<color=#fff9f9AA>{MessageGet}</color>",
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 11,
                                        Font = "robotocondensed-regular.ttf",
                                        FadeIn = 0.5f,
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0.525 0.05",
                                        AnchorMax = "0.95 0.3",
                                    }
                                }
            });

            if (CanTake)
            {
                Container.Add(new CuiButton
                {
                    Button = { Color = "1 1 1 0", Command = $"case {instance.CaseList.ElementAt(number).Name} {number} {positionX} {positionY}" },
                    RectTransform = { AnchorMin = "0.525 0.05", AnchorMax = "0.95 0.3" },
                    Text = { Text = "", Align = TextAnchor.MiddleCenter }
                }, MenuContent + $".Case.{number}", MenuContent + $".Content.Case.{number}.BtnTitle");
            }
            CuiHelper.AddUi(player, Container);
        }

        #endregion

        #region CMD
        [ConsoleCommand("case")]
        void CaseCmd(ConsoleSystem.Arg arg)
        {
            if (arg.HasArgs()) //$"case {CaseList.ElementAt(i).Name} {i} {x} {y}"
            {
                Case _case = CaseList.Find(x => x.Name == arg.Args[0]);
                if (_case == null)
                {
                    Notifications.Call("API_AddUINote", arg.Connection.userid, $"Не найден кейс с таким именем");
                    return;
                }
                int number = int.Parse(arg.Args[1]);
                int positionX = int.Parse(arg.Args[2]);
                int positionY = int.Parse(arg.Args[3]);

                GiveCase(arg.Player(), _case, number, positionX, positionY);
            }
        }

        [ConsoleCommand("casegivemoney")]
        void CaseGiveMoney(ConsoleSystem.Arg arg)
        {
            if (arg.IsServerside)
            {
                if (arg.HasArgs())
                {
                    ulong userID = ulong.Parse(arg.Args[0]);
                    int amount = int.Parse(arg.Args[1]);

                    webrequest.Enqueue($"https://api.gamestores.ru/api?shop_id=8050&secret=d2359b013c542026d0cd2fa179761b27&action=moneys&type=plus&steam_id={userID}&amount={amount}&mess=Cases", null, (code, response) =>
                    {
                        PrintWarning("GAMESTORESMONEY " + userID.ToString() + " AMOUNT" + amount.ToString() + " CODE" + code.ToString() + " Убери текст потом");
                    }, this, Core.Libraries.RequestMethod.GET);
                }
            }
        }

        [ConsoleCommand("caseshow")]
        void CaseShowCmd(ConsoleSystem.Arg arg)
        {
            if (arg.HasArgs())
            {
                if (arg.Args[0] == "close")
                {
                    CuiHelper.DestroyUi(arg.Player(), MenuContent + $".CaseInfo");

                    return;
                }
                Case _case = CaseList.Find(x => x.Name == arg.Args[0]);
                if (_case == null)
                {
                    Notifications.Call("API_AddUINote", arg.Connection.userid, $"Не найден кейс с таким именем");
                    return;
                }
                ShowCaseInfo(arg.Player(), _case);
            }
        }
        #endregion

        #region GiveCase
        void GiveCase(BasePlayer player, Case _case, int number, int positionX, int positionY)
        {
            webrequest.Enqueue($"https://api.gamestores.ru/api?shop_id=8050&secret=d2359b013c542026d0cd2fa179761b27&method=basket&steam_id={player.userID}", null, (code, response) =>
            {
                switch (code)
                {
                    case 200:
                        {
                            var firstInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(response, new KeyValuesConverter());
                            if (firstInfo.ContainsKey("result"))
                            {
                                if (firstInfo["result"].ToString() == "fail")
                                {
                                    if (firstInfo["code"].ToString() == "104")
                                    {
                                        if (!_case.Type)
                                        {
                                            if (_case.UsersOpen.Contains(player.userID))
                                            {
                                                Notifications.Call("API_AddUINote", player.userID, $"Вы уже брали этот кейс");
                                                return;
                                            }

                                            if (GetUserTime(player.userID) < _case.Cost)
                                            {
                                                Notifications.Call("API_AddUINote", player.userID, $"Не хватает игрового времени для открытия кейса");
                                                return;
                                            }

                                            if (player.inventory.containerWear.capacity - player.inventory.containerWear.itemList.Count < _case.Items.Where(x => x.Container == "wear").Count())
                                            {
                                                Notifications.Call("API_AddUINote", player.userID, $"Не хватает места для одежды");
                                                return;
                                            }

                                            if (player.inventory.containerBelt.capacity - player.inventory.containerBelt.itemList.Count < _case.Items.Where(x => x.Container == "belt").Count())
                                            {
                                                Notifications.Call("API_AddUINote", player.userID, $"Не хватает места на поясе");
                                                return;
                                            }
                                            if (player.inventory.containerMain.capacity - player.inventory.containerMain.itemList.Count < _case.Items.Where(x => x.Container == "main").Count())
                                            {
                                                Notifications.Call("API_AddUINote", player.userID, $"Не хватает места в рюкзаке");
                                                return;
                                            }

                                            foreach (var _item in _case.Items)
                                            {
                                                Item item;
                                                item = ItemManager.CreateByName(_item.ShortName, _item.Amount);
                                                switch (_item.Container)
                                                {
                                                    case "belt":
                                                        {
                                                            if (!item.MoveToContainer(player.inventory.containerBelt))
                                                                item.Drop(player.transform.position, Vector3.up);
                                                            break;
                                                        }
                                                    case "main":
                                                        {
                                                            if (!item.MoveToContainer(player.inventory.containerMain))
                                                                item.Drop(player.transform.position, Vector3.up);
                                                            break;
                                                        }
                                                    case "wear":
                                                        {
                                                            if (!item.MoveToContainer(player.inventory.containerWear))
                                                                item.Drop(player.transform.position, Vector3.up);
                                                            break;
                                                        }
                                                }
                                            }

                                            _case.UsersOpen.Add(player.userID);

                                            Notifications.Call("API_AddUINote", player.userID, $"Зарплата выплачена вам в магазин");
                                        }
                                        else
                                        {
                                            if (GetUserMoney(player) < _case.Cost)
                                            {
                                                Notifications.Call("API_AddUINote", player.userID, $"Не хватает монет для открытия кейса");
                                                return;
                                            }

                                            BaseEntity entity = GameManager.server.CreateEntity("assets/prefabs/misc/supply drop/supply_drop.prefab", new Vector3(player.transform.position.x, player.transform.position.y + 250, player.transform.position.z), new Quaternion());
                                            entity.OwnerID = 1;
                                            entity.Spawn();
                                            StorageContainer container = entity.GetComponent<StorageContainer>();
                                            container.inventory.Clear();
                                            foreach (var _item in _case.Items)
                                            {
                                                Item item;
                                                item = ItemManager.CreateByName(_item.ShortName, _item.Amount);
                                                item.MoveToContainer(container.inventory);
                                            }

                                            if (GetUserMoney(player) == _case.Cost)
                                                player.inventory.FindItemID("sticks").Remove();
                                            else
                                            {
                                                player.inventory.FindItemID("sticks").amount -= _case.Cost;
                                                player.inventory.FindItemID("sticks").MarkDirty();
                                            }

                                            Notifications.Call("API_AddUINote", player.userID, $"Вы заказали доставку кейса. Он прибудет в течение минуты на вашу позицию");
                                        }

                                        if (_case.Command != "null")
                                        {
                                            rust.RunServerCommand(String.Format(_case.Command, player.userID));
                                        }
                                        ReRenderCase(player, _case, number, positionX, positionY);
                                        return;
                                    }
                                    if (firstInfo["code"].ToString() == "105")
                                    {
                                        Notifications.Call("API_AddUINote", player.userID, $"Вы не авторизованы в магазине");
                                        return;
                                    }
                                }
                                else
                                {
                                    if (!_case.Type)
                                    {
                                        if (_case.UsersOpen.Contains(player.userID))
                                        {
                                            Notifications.Call("API_AddUINote", player.userID, $"Вы уже брали этот кейс");
                                            return;
                                        }

                                        if (GetUserTime(player.userID) < _case.Cost)
                                        {
                                            Notifications.Call("API_AddUINote", player.userID, $"Не хватает игрового времени для открытия кейса");
                                            return;
                                        }

                                        if (player.inventory.containerWear.capacity - player.inventory.containerWear.itemList.Count < _case.Items.Where(x => x.Container == "wear").Count())
                                        {
                                            Notifications.Call("API_AddUINote", player.userID, $"Не хватает места для одежды");
                                            return;
                                        }

                                        if (player.inventory.containerBelt.capacity - player.inventory.containerBelt.itemList.Count < _case.Items.Where(x => x.Container == "belt").Count())
                                        {
                                            Notifications.Call("API_AddUINote", player.userID, $"Не хватает места на поясе");
                                            return;
                                        }

                                        if (player.inventory.containerMain.capacity - player.inventory.containerMain.itemList.Count < _case.Items.Where(x => x.Container == "main").Count())
                                        {
                                            Notifications.Call("API_AddUINote", player.userID, $"Не хватает места в рюкзаке");
                                            return;
                                        }

                                        foreach (var _item in _case.Items)
                                        {
                                            Item item;
                                            item = ItemManager.CreateByName(_item.ShortName, _item.Amount);
                                            switch (_item.Container)
                                            {
                                                case "belt":
                                                    {
                                                        if (!item.MoveToContainer(player.inventory.containerBelt))
                                                            item.Drop(player.transform.position, Vector3.up);
                                                        break;
                                                    }
                                                case "main":
                                                    {
                                                        if (!item.MoveToContainer(player.inventory.containerMain))
                                                            item.Drop(player.transform.position, Vector3.up);
                                                        break;
                                                    }
                                                case "wear":
                                                    {
                                                        if (!item.MoveToContainer(player.inventory.containerWear))
                                                            item.Drop(player.transform.position, Vector3.up);
                                                        break;
                                                    }
                                            }
                                        }

                                        _case.UsersOpen.Add(player.userID);

                                        Notifications.Call("API_AddUINote", player.userID, $"Зарплата выплачена вам в магазин");
                                    }
                                    else
                                    {
                                        if (GetUserMoney(player) < _case.Cost)
                                        {
                                            Notifications.Call("API_AddUINote", player.userID, $"Не хватает монет для открытия кейса");
                                            return;
                                        }

                                        BaseEntity entity = GameManager.server.CreateEntity("assets/prefabs/misc/supply drop/supply_drop.prefab", new Vector3(player.transform.position.x, player.transform.position.y + 250, player.transform.position.z), new Quaternion());
                                        entity.OwnerID = 1;
                                        entity.Spawn();
                                        StorageContainer container = entity.GetComponent<StorageContainer>();
                                        container.inventory.Clear();
                                        foreach (var _item in _case.Items)
                                        {
                                            Item item;
                                            item = ItemManager.CreateByName(_item.ShortName, _item.Amount);
                                            item.MoveToContainer(container.inventory);
                                        }

                                        if (GetUserMoney(player) == _case.Cost)
                                            player.inventory.FindItemID("sticks").Remove();
                                        else
                                        {
                                            player.inventory.FindItemID("sticks").amount -= _case.Cost;
                                            player.inventory.FindItemID("sticks").MarkDirty();
                                        }

                                        Notifications.Call("API_AddUINote", player.userID, $"Вы заказали доставку кейса. Он прибудет в течение минуты на вашу позицию");
                                    }

                                    if (_case.Command != "null")
                                    {
                                        rust.RunServerCommand(String.Format(_case.Command, player.userID));
                                    }
                                    ReRenderCase(player, _case, number, positionX, positionY);
                                }
                            }

                            break;
                        }
                }
            }, this, Core.Libraries.RequestMethod.GET);
        }
        #endregion

        #region API
        int GetUserMoney(BasePlayer player)
        {
            if (player.inventory.FindItemID("sticks") != null)
                return player.inventory.FindItemID("sticks").amount;
            return 0;
        }

        [PluginReference] private Plugin IQRankSystem;
        int GetUserTime(ulong userID)
        {
            int time = (int)IQRankSystem?.Call("API_GET_SECONDGAME", userID);
            return time / 60;
        }

        bool GetCases(List<Dictionary<string, object>> obj, ulong userID)
        {
            foreach (var _case in CaseList)
            {
                Dictionary<string, object> Case = new Dictionary<string, object>()
                {
                    {"Name", _case.Name },
                    {"DisplayName", _case.DisplayName },
                    {"Cost", _case.Cost },
                    {"Type", _case.Type },
                    {"IsOpen", _case.UsersOpen.Contains(userID) },
                };
                obj.Add(Case);
            }
            return true;
        }
        #endregion

        #region Helpers
        public static class TimeExtensions
        {
            public static string FormatShortTime(TimeSpan time)
            {
                string result = string.Empty;
                if (time.Days != 0)
                    result += $"{time.Days}д ";

                if (time.Hours != 0)
                    result += $"{time.Hours}ч ";

                if (time.Minutes != 0)
                    result += $"{time.Minutes}м ";

                if (time.Seconds != 0)
                    result += $"{time.Seconds}с ";

                return result;
            }

            public static string FormatTime(TimeSpan time)
            {
                string result = string.Empty;
                if (time.Days != 0)
                    result += $"{Format(time.Days, "ДНЕЙ", "ДНЯ", "ДЕНЬ")} ";

                if (time.Hours != 0)
                    result += $"{Format(time.Hours, "ЧАСОВ", "ЧАСА", "ЧАС")} ";

                if (time.Minutes != 0)
                    result += $"{Format(time.Minutes, "МИНУТ", "МИНУТЫ", "МИНУТА")} ";

                if (time.Seconds != 0)
                    result += $"{Format(time.Seconds, "СЕКУНД", "СЕКУНДЫ", "СЕКУНДА")} ";

                return result;
            }

            private static string Format(int units, string form1, string form2, string form3)
            {
                var tmp = units % 10;

                if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                    return $"{units} {form1}";

                if (tmp >= 2 && tmp <= 4)
                    return $"{units} {form2}";

                return $"{units} {form3}";
            }
        }
        #endregion
    }
}