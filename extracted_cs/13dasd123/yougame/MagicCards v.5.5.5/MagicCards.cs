// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using UnityEngine;
using System;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Newtonsoft.Json.Linq;

namespace Oxide.Plugins
{
    [Info("MagicCards", "SHooTeR/Magic Rust/F5", "5.5.5")]
    [Description("Magic cards")]
    class MagicCards : RustPlugin
    {
        [PluginReference]
        Plugin ImageLibrary;

        public string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary.Call("GetImage", shortname, skin);
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);

        #region Config
        public int shopid = 0;
        public string secretkey = "secretkey";
        public string BackCard = "";
        public float CardsTimer = 21600f;
        public int MaxCards = 3;

        protected override void LoadDefaultConfig()
        {
            GetConfig("[MagicCards]", "ShopID Gamestores", ref shopid);
            GetConfig("[MagicCards]", "SecretKey Gamestores", ref secretkey);
            GetConfig("[MagicCards]", "Рубашка карты (Картинка)", ref BackCard);
            GetConfig("[MagicCards]", "Выдавать карту всем игрокам (оффлайн и онлайн) каждые N сек", ref CardsTimer);
            GetConfig("[MagicCards]", "Максимальное количество накапливаемых карты", ref MaxCards);
            SaveConfig();
        }

        private void GetConfig<T>(string menu, string Key, ref T var)
        {
            if (Config[menu, Key] != null)
            {
                var = (T)Convert.ChangeType(Config[menu, Key], typeof(T));
            }

            Config[menu, Key] = var;
        }
        #endregion

        #region Cards Class
        public List<Card> Cards = new List<Card>();
        public class Card
        {
            public int CardID { get; set; }

            public string ImageUrl { get; set; }
            public string ImageName { get; set; }
            public string DispayName { get; set; }
            public string DisplayDescription { get; set; }

            public int CardType { get; set; }
            public int Money { get; set; }
            public int CardsForUse { get; set; }

            public Dictionary<string, int> Items { get; set; }
            public List<string> GrantCommands { get; set; }
        }

        public List<PlayerCard> PlayerCards = new List<PlayerCard>();
        public class PlayerCard
        {
            public Dictionary<int, int> Cards { get; set; }

            public ulong SteamID { get; set; }
            public int CardsCount { get; set; }
        }

        public List<ulong> OpenCardNow = new List<ulong>();
        #endregion

        #region UI
        [ChatCommand("cards")]
        void ChatCards(BasePlayer player, string cmd, string[] args)
        {
            CardsUI(player);
        }

        [ConsoleCommand("cardsclose")]
        void CmdCardsClose(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, "MagicCards");
            CuiHelper.DestroyUi(player, "MagicMyCards");
        }

        [ConsoleCommand("cards")]
        void CmdCards(ConsoleSystem.Arg arg)
        {
            if (arg.HasArgs())
            {
                if (!arg.IsServerside) return;
                switch(arg.Args[0]){
                    case "add":
                        {
                            Card card = new Card
                            {
                                 CardID = Convert.ToInt32(arg.Args[1]),
                                 DispayName = "Имя карты",
                                 DisplayDescription = "Описание карты",
                                 CardsForUse = 1,
                                 Items = new Dictionary<string, int>
                                 {
                                     {"rifle.ak", 1 }
                                 },
                                 GrantCommands = new List<string>
                                 {
                                     "addgroup {0} vip 1d"
                                 },
                                 Money = 10,
                                 ImageUrl = "ImageFilepath",
                                 ImageName = "ImageName",
                                 CardType = 0
                            };
                            Cards.Add(card);
                            PrintWarning("Карта добавлена, выгрузите плагин и отредактируйте ее в файле /data/MagicCards/Cards");
                            Interface.Oxide.DataFileSystem.WriteObject("MagicCards/Cards", Cards);
                            break;
                        }
                    case "remove":
                        {
                            Card card = Cards.Find(x => x.CardID == Convert.ToInt32(arg.Args[1]));
                            if(card != null)
                            {
                                Cards.Remove(card);
                                PrintWarning($"Карта c CardID: {arg.Args[1]} удалена");

                                foreach(var pcard in PlayerCards)
                                {
                                    pcard.Cards.Remove(card.CardID);
                                }
                            }
                            break;
                        }
                }
            }
            else
            {
                BasePlayer player = arg.Player();
                if (player == null) return;
                CardsUI(player);
            }
        }

        void CardsUI(BasePlayer player)
        {
            CuiElementContainer Container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, "MagicCards");
            CuiHelper.DestroyUi(player, "MagicCardsHelp");
            CuiHelper.DestroyUi(player, "MagicMyCards");

            if(Cards.Count == 0)
            {
                SendReply(player, "Не создано не одной MAGIC карты");
                return;
            }

            PlayerCard PCard = PlayerCards.Find(z => z.SteamID == player.userID);

            CreatePanel(Container, "MagicCards", "Hud", "0 0 0 0.5", "0.175 0.125", "0.825 0.975");
            CreateTitle(Container, "MagicCardsTitle", "MagicCards", "<size=70>Магическая колода карт</size>\n<size=22>Испытай свою удачу! Открывай карты каждый день и получай призы!</size>", TextAnchor.UpperCenter, "0 0.6", "1 0.975", "0 0 0 1", "1 -1", true);
            CreateTitle(Container, "MagicCardsCountTitle", "MagicCards", $"<size=22>Доступно карт для открытия: {PCard.CardsCount}</size>", TextAnchor.MiddleCenter, "0 0.085", "1 0.135", "0 0 0 1", "1 -1", true);
            Container.Add(new CuiElement
            {
                Name = "MagicCardsCursor",
                Parent = "MagicCards",
                Components =
                    {
                    new CuiNeedsCursorComponent()
                    }
            });
            
            List<int> CardID = new List<int>(); int RandomCardID = 0;
            foreach(var card in Cards)
            {
                CardID.Add(card.CardID);
            }
            int[] CardsID = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            for (int i = 0, x = 0, y = 0; i < 10; i++, x++)
            {
                int Random = UnityEngine.Random.Range(0, CardID.Count); RandomCardID = CardID[Random];
                Card card = Cards.Find(v => v.CardID == RandomCardID);
                CreateImage(Container, $"MagicCard{i}", "MagicCards", GetImage("BackCard"), null, "1 1 1 1", $"{0.1 + (x * 0.16)} {0.475 - (y * 0.325)}", $"{0.25 + (x * 0.16)} {0.775 - (y * 0.325)}", 0.1f);
                CreateButton(Container, $"MagicCard{i}Btn", $"MagicCard{i}", "0 0 0 0", $"cardsopen {card.CardID} {i}", "0 0", "1 1", "");
                CardsID[i] = card.CardID;
                if (x == 4)
                {
                    x = -1;
                    y++;
                }
            }

            CreateButton(Container, "MagicCardBtnCards", "MagicCards", "0.00 0.75 1.00 1.00", "mycards", "0.135 0.02", "0.265 0.07", "ВАШИ КАРТЫ");
            CreateButton(Container, "MagicCardBtnOpenAll", "MagicCards", "0.3 1.00 0.00 1.00", $"cardsopen {CardsID[0]} {CardsID[1]} {CardsID[2]} {CardsID[3]} {CardsID[4]} {CardsID[5]} {CardsID[6]} {CardsID[7]} {CardsID[8]} {CardsID[9]}", "0.285 0.02", "0.415 0.07", "ВСКРЫТЬ КАРТЫ");
            CreateButton(Container, "MagicCardBtnRefresh", "MagicCards", "1.00 1.00 0.00 1.00", "cards", "0.435 0.02", "0.565 0.07", "ПЕРЕРАЗДАТЬ");
            CreateButton(Container, "MagicCardBtnHelp", "MagicCards", "1.00 0.00 0.00 1.00", "cardhelp 1", "0.585 0.02", "0.715 0.07", "ПОМОЩЬ");
            CreateButton(Container, "MagicCardBtnClose", "MagicCards", "1.00 0.00 1.00 1.00", "cardsclose", "0.735 0.02", "0.865 0.07", "ЗАКРЫТЬ");

            CuiHelper.AddUi(player, Container);
        }

        [ConsoleCommand("cardsopen")]
        void CardOpen(ConsoleSystem.Arg arg)
        {
            if (!arg.HasArgs()) return;
            if (arg.Args.Length == 10)
            {
                CuiElementContainer Container = new CuiElementContainer();
                for (int i = 0; i < 10; i++)
                {
                    Card card = Cards.Find(z => z.CardID == Convert.ToInt32(arg.Args[i]));
                    CreateImage(Container, $"MagicCard{i}Image", $"MagicCard{i}", GetImage(card.ImageName), null, "1 1 1 1", "0 0", "1 1", 0.5f);
                }
                CuiHelper.AddUi(arg.Player(), Container);
            }
            else
            {
                if (OpenCardNow.Contains(arg.Player().userID))
                {
                    SendReply(arg.Player(), $"Нельзя открывать карты так часто");
                    return;
                }
                else
                {
                    OpenCardNow.Add(arg.Player().userID);
                    timer.Once(1f, () => { OpenCardNow.Remove(arg.Player().userID); });
                }
                PlayerCard PCard = PlayerCards.Find(x => x.SteamID == arg.Player().userID);
                if (PCard.CardsCount == 0) return;
                Card card = Cards.Find(z => z.CardID == Convert.ToInt32(arg.Args[0]));
                int i = Convert.ToInt32(arg.Args[1]);

                if (PCard.Cards.ContainsKey(card.CardID))
                {
                    PCard.Cards[card.CardID]++;
                }
                else
                {
                    PCard.Cards.Add(card.CardID, 1);
                }
                PCard.CardsCount--;

                CuiHelper.DestroyUi(arg.Player(), "MagicCardsCountTitle");
                CuiHelper.DestroyUi(arg.Player(), $"MagicCard{i}Btn");
                CuiElementContainer Container = new CuiElementContainer();
                CreateTitle(Container, "MagicCardsCountTitle", "MagicCards", $"<size=22>Доступно карт для открытия: {PCard.CardsCount}</size>", TextAnchor.MiddleCenter, "0 0.085", "1 0.135", "0 0 0 1", "1 -1", true);
                CreateImage(Container, $"MagicCard{i}Image", $"MagicCard{i}", GetImage(card.ImageName), null, "1 1 1 1", "0 0", "1 1", 0.5f);
                CuiHelper.AddUi(arg.Player(), Container);
            }
        }

        [ConsoleCommand("mycards")]
        void CmdMyCards(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;

            CuiHelper.DestroyUi(arg.Player(), "MagicCards");
            CuiHelper.DestroyUi(arg.Player(), "MagicMyCards");

            CuiElementContainer Container = new CuiElementContainer();
            PlayerCard PCard = PlayerCards.Find(z => z.SteamID == arg.Player().userID);

            CreatePanel(Container, "MagicMyCards", "Hud", "0 0 0 0.5", "0.175 0.125", "0.825 0.975");
            CreateTitle(Container, "MagicMyCardsTitle", "MagicMyCards", "<size=70>Ваши собранные карты</size>", TextAnchor.UpperCenter, "0 0.8", "1 0.975", "0 0 0 1", "1 -1", true);
            Container.Add(new CuiElement
            {
                Name = "MagicMyCardsCursor",
                Parent = "MagicMyCards",
                Components =
                    {
                    new CuiNeedsCursorComponent()
                    }
            });

            int CardsSkip = 0;
            int Arg = 0;
            double d = Cards.Count / 39;
            int MaxArg = Convert.ToInt32(Math.Floor(d));
            if (arg.HasArgs())
            {
                Arg = int.Parse(arg.Args[0]);
            }
            CardsSkip = Arg * 39;

            int i = 0, x = 0, y = 0, v = 0;
            foreach (var cards in PCard.Cards)
            {
                if(i < CardsSkip)
                {
                    i++;
                    continue;
                }
                if (v > 38) continue;

                Card card = Cards.Find(z => z.CardID == cards.Key);
                if(cards.Value >= card.CardsForUse) CreateImage(Container, $"MagicMyCard{i}", "MagicMyCards", GetImage(card.ImageName), null, "1 1 1 1", $"{0.01 + (x * 0.076)} {0.675 - (y * 0.165)}", $"{0.076 + (x * 0.076)} {0.825 - (y * 0.165)}", 0.1f);
                else CreateImage(Container, $"MagicMyCard{i}", "MagicMyCards", GetImage(card.ImageName), null, "1 1 1 0.5", $"{0.01 + (x * 0.076)} {0.675 - (y * 0.165)}", $"{0.076 + (x * 0.076)} {0.825 - (y * 0.165)}", 0.1f);
                CreateButton(Container, $"MagicMyCard{i}Btn", $"MagicMyCard{i}", "0 0 0 0", $"cardinfo {card.CardID}", "0 0", "1 1", "");

                if (x == 12)
                {
                    x = -1;
                    y++;
                }

                if (i == CardsSkip) rust.RunClientCommand(arg.Player(), $"cardinfo {card.CardID}");
                i++; x++; v++;
            }

            if(Arg > 0)
            {
                CreateButton(Container, "MagicMyCardsBack", "MagicMyCards", "0.00 0.75 1.00 1.00", $"mycards {Arg - 1}", "0.47 0.3", "0.495 0.33", "<");
            }

            if(Arg < MaxArg)
            {
                CreateButton(Container, "MagicMyCardsNext", "MagicMyCards", "0.00 0.75 1.00 1.00", $"mycards {Arg + 1}", "0.505 0.3", "0.53 0.33", ">");
            }

            CuiHelper.AddUi(arg.Player(), Container);
        }

        [ConsoleCommand("cardinfo")]
        void CmdMyCard(ConsoleSystem.Arg arg)
        {
            if (!arg.HasArgs()) return;
            if (arg.Player() == null) return;
            PlayerCard Pcard = PlayerCards.Find(x => x.SteamID == arg.Player().userID);
            int CardsCount = Pcard.Cards[Convert.ToInt32(arg.Args[0])];
            Card card = Cards.Find(x => x.CardID == Convert.ToInt32(arg.Args[0]));

            CuiHelper.DestroyUi(arg.Player(), "MyCardInfo");
            CuiElementContainer Container = new CuiElementContainer();
            CreatePanel(Container, "MyCardInfo", "MagicMyCards", "1 1 1 0", "0 0", "1 0.3");
            CreateImage(Container, $"MyCardInfoImg", "MyCardInfo", GetImage(card.ImageName), null, "1 1 1 1", $"0.15 0.1", $"0.275 0.9", 0.1f);
            CreateTitle(Container, $"MyCardInfoTitle", "MyCardInfo", $"<size=24>{card.DispayName}</size>", TextAnchor.UpperLeft, "0.3 0.75", "0.85 0.9", "0 0 0 1", "1 -1", true);
            CreateTitle(Container, $"MyCardInfoTitleDesc", "MyCardInfo", $"<size=16>{card.DisplayDescription}</size>", TextAnchor.UpperLeft, "0.3 0.35", "0.85 0.75", "0 0 0 1", "1 -1", true);
            CreateTitle(Container, $"MyCardInfoTitle", "MyCardInfo", $"<size=16>Собрано: {CardsCount}/{card.CardsForUse}</size>", TextAnchor.UpperLeft, "0.3 0.1", "0.85 0.2", "0 0 0 1", "1 -1", true);
            if(CardsCount >= card.CardsForUse)
            {
                CreateButton(Container, "MyCardGet", "MyCardInfo", "0.00 0.75 1.00 1.00", $"cardget {card.CardID}", "0.625 0.1", "0.725 0.3", "ПОЛУЧИТЬ");
            }
            CreateButton(Container, "MyCardClose", "MyCardInfo", "1.00 0.00 1.00 1.00", "cardsclose", "0.75 0.1", "0.85 0.3", "ЗАКРЫТЬ");
            CuiHelper.AddUi(arg.Player(), Container);
        }

        [ConsoleCommand("cardget")]
        void CmdCardGet(ConsoleSystem.Arg arg)
        {
            if (!arg.HasArgs()) return;
            if (arg.Player() == null) return;

            Card card = Cards.Find(x => x.CardID == Convert.ToInt32(arg.Args[0]));
            PlayerCard Pcard = PlayerCards.Find(z => z.SteamID == arg.Player().userID);

            int CardCount = Pcard.Cards[Convert.ToInt32(arg.Args[0])];
            if (CardCount < card.CardsForUse) return;

            Pcard.Cards[Convert.ToInt32(arg.Args[0])] = CardCount - card.CardsForUse;

            switch (card.CardType)
            {
                case 0:
                    {
                        foreach(var carditem in card.Items)
                        {
                            Item item = ItemManager.CreateByName(carditem.Key, carditem.Value);
                            arg.Player().GiveItem(item);
                        }
                        break;
                    }
                case 1:
                    {
                        foreach (var cardcommand in card.GrantCommands)
                        {
                            rust.RunServerCommand(String.Format(cardcommand, arg.Player().userID));
                        }
                        break;
                    }
                case 2:
                    {
                        webrequest.EnqueueGet($"https://gamestores.ru/api?shop_id={shopid}&secret={secretkey}&action=moneys&type=plus&steam_id={arg.Player().userID}&amount={card.Money}&mess=MagicCards: Карта пополнения на {card.Money} рублей", (i, s) =>
                        {
                            if(i != 200)
                            {
                                LogToFile("MagicCardsGM", $"{arg.Player().userID} ОШИБКА GAMESTORES НЕ ОТВЕЧАЕТ Сумма:{card.Money}", this);
                            }
                            else
                            {
                                JObject jObject = JObject.Parse(s);
                                if(jObject["result"].ToString() == "fail")
                                {
                                    LogToFile("MagicCardsGM", $"{arg.Player().userID} FAIL Сумма: {card.Money}, Ошибка: {jObject["message"].ToString()}", this);
                                }
                                else
                                {
                                    LogToFile("MagicCardsGM", $"{arg.Player().userID} пополнен счет на {card.Money} рублей", this);
                                }
                            }
                        }, this);
                        break;
                    }
            }

            if(CardCount - card.CardsForUse < card.CardsForUse) rust.RunClientCommand(arg.Player(), $"mycards");
            else rust.RunClientCommand(arg.Player(), $"cardinfo {card.CardID}");
        }

        [ConsoleCommand("cardhelp")]
        void CardHelp(ConsoleSystem.Arg arg)
        {
            if (!arg.HasArgs()) return;
            if (arg.Player() == null) return;

            CuiHelper.DestroyUi(arg.Player(), "MagicCardsHelp");
            CuiHelper.DestroyUi(arg.Player(), "MagicCards");
            CuiElementContainer Container = new CuiElementContainer();
            switch (arg.Args[0])
            {
                case "1":
                    {
                        CreatePanel(Container, "MagicCardsHelp", "Overlay", "0 0 0 0.5", "0.175 0.125", "0.825 0.975");
                        CreateTitle(Container, "MagicCardsHelpTitle", "MagicCardsHelp", "<size=70>Магическая колода карт</size>\n<size=22>Испытай свою удачу! Открывай карты каждый день и получай призы!</size>", TextAnchor.UpperCenter, "0 0.6", "1 0.975", "0 0 0 1", "1 -1", true);
                        Container.Add(new CuiElement
                        {
                            Name = "MagicHelpCursor",
                            Parent = "MagicCardsHelp",
                            Components =
                        {
                             new CuiNeedsCursorComponent()
                          }
                        });

                        List<int> CardID = new List<int>(); int RandomCardID = 0;

                        foreach (var card in Cards)
                        {
                            CardID.Add(card.CardID);
                        }
                        int[] CardsID = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                        for (int i = 0, x = 0, y = 0; i < 10; i++, x++)
                        {
                            int Random = UnityEngine.Random.Range(0, CardID.Count); RandomCardID = CardID[Random];
                            Card card = Cards.Find(v => v.CardID == RandomCardID);
                            CreateImage(Container, $"MagicCardsHelp{i}", "MagicCardsHelp", GetImage("BackCard"), null, "1 1 1 1", $"{0.1 + (x * 0.16)} {0.475 - (y * 0.325)}", $"{0.25 + (x * 0.16)} {0.775 - (y * 0.325)}", 0.1f);
                            CardsID[i] = card.CardID;
                            if (x == 4)
                            {
                                x = -1;
                                y++;
                            }
                        }

                        CreatePanel(Container, "MagicCardsHelpFon", "MagicCardsHelp", "0 0 0 0.99", "0 0.775", "1 1");
                        CreatePanel(Container, "MagicCardsHelpFon2", "MagicCardsHelp", "0 0 0 0.99", "0 0", "1 0.15");
                        CreateTitle(Container, "MagicCardsHelpCountTitle", "MagicCardsHelp", $"<size=16>Это карты, которые вы можете открыть. Просто нажмите на любую и через секунду она будет открыта.</size>", TextAnchor.MiddleCenter, "0 0.085", "1 0.135", "0 0 0 1", "1 -1", true);

                        CreateButton(Container, "MagiccardsAccept", "MagicCardsHelp", "1 0 0 1", "cardhelp 2", "0.45 -0.05", "0.55 0", "Понятно");

                        CuiHelper.AddUi(arg.Player(), Container);
                        break;
                    }
                case "2":
                    {
                        CreatePanel(Container, "MagicCardsHelp", "Overlay", "0 0 0 0.5", "0.175 0.125", "0.825 0.975");
                        CreateTitle(Container, "MagicCardsHelpTitle", "MagicCardsHelp", "<size=70>Магическая колода карт</size>\n<size=22>Испытай свою удачу! Открывай карты каждый день и получай призы!</size>", TextAnchor.UpperCenter, "0 0.6", "1 0.975", "0 0 0 1", "1 -1", true);
                        CreateTitle(Container, "MagicCardsCountTitle", "MagicCardsHelp", $"<size=22>Доступно карт для открытия: 3</size>", TextAnchor.MiddleCenter, "0 0.085", "1 0.135", "0 0 0 1", "1 -1", true);

                        Container.Add(new CuiElement
                        {
                            Name = "MagicHelpCursor",
                            Parent = "MagicCardsHelp",
                            Components =
                        {
                             new CuiNeedsCursorComponent()
                          }
                        });
                        CreatePanel(Container, "MagicCardsHelpFon", "MagicCardsHelp", "0 0 0 0.99", "0 0.135", "1 1");
                        CreatePanel(Container, "MagicCardsHelpFon2", "MagicCardsHelp", "0 0 0 0.99", "0 0", "1 0.085");
                        CreateTitle(Container, "MagicCardsHelpCountTitle", "MagicCardsHelp", $"<size=16>Тут показано количество карт, которое вы можете открыть. По одной карте дается всем каждые 6 часов</size>", TextAnchor.MiddleCenter, "0 0.0", "1 0.1", "0 0 0 1", "1 -1", true);

                        CreateButton(Container, "MagiccardsAccept", "MagicCardsHelp", "1 0 0 1", "cardhelp 3", "0.45 -0.05", "0.55 0", "Понятно");
                        CuiHelper.AddUi(arg.Player(), Container);
                        break;
                    }
                case "3":
                    {
                        CreatePanel(Container, "MagicCardsHelp", "Overlay", "0 0 0 0.5", "0.175 0.125", "0.825 0.975");
                        CreateTitle(Container, "MagicCardsHelpTitle", "MagicCardsHelp", "<size=70>Магическая колода карт</size>\n<size=22>Испытай свою удачу! Открывай карты каждый день и получай призы!</size>", TextAnchor.UpperCenter, "0 0.6", "1 0.975", "0 0 0 1", "1 -1", true);
                        Container.Add(new CuiElement
                        {
                            Name = "MagicHelpCursor",
                            Parent = "MagicCardsHelp",
                            Components =
                        {
                             new CuiNeedsCursorComponent()
                          }
                        });
                        CreatePanel(Container, "MagicCardsHelpFon", "MagicCardsHelp", "0 0 0 0.99", "0 0", "1 1");
                        CreateButton(Container, "MagicCardsHelpBtnRefresh", "MagicCardsHelp", "1.00 1.00 0.00 1.00", "", "0.435 0.02", "0.565 0.07", "ПЕРЕРАЗДАТЬ");
                        CreateTitle(Container, "MagicCardsHelpCountTitle", "MagicCardsHelp", $"<size=16>Если вдруг у вас есть дар предсказывать будущее и вы знаете, что под разложенными картами окажется какая-то хрень, можете перераздать карты.\nСделать это можно в любой момент, даже когда вы уже открыли одну или две карты.\nПри перераздаче вы не потеряете уже открытые карты, а счетчик карт не будет сброшен в исходное состояние</size>", TextAnchor.MiddleCenter, "0 0.4", "1 0.6", "0 0 0 1", "1 -1", true);

                        CreateButton(Container, "MagiccardsAccept", "MagicCardsHelp", "1 0 0 1", "cardhelp 4", "0.45 -0.05", "0.55 0", "Понятно");
                        CuiHelper.AddUi(arg.Player(), Container);
                        break;
                    }
                case "4":
                    {
                        CreatePanel(Container, "MagicCardsHelp", "Overlay", "0 0 0 0.5", "0.175 0.125", "0.825 0.975");
                        CreateTitle(Container, "MagicCardsHelpTitle", "MagicCardsHelp", "<size=70>Магическая колода карт</size>\n<size=22>Испытай свою удачу! Открывай карты каждый день и получай призы!</size>", TextAnchor.UpperCenter, "0 0.6", "1 0.975", "0 0 0 1", "1 -1", true);
                        Container.Add(new CuiElement
                        {
                            Name = "MagicHelpCursor",
                            Parent = "MagicCardsHelp",
                            Components =
                        {
                             new CuiNeedsCursorComponent()
                          }
                        });
                        CreatePanel(Container, "MagicCardsHelpFon", "MagicCardsHelp", "0 0 0 0.99", "0 0", "1 1");
                        CreateButton(Container, "MagicCardsHelpBtnOpenAll", "MagicCardsHelp", "0.3 1.00 0.00 1.00", $"", "0.285 0.02", "0.415 0.07", "ВСКРЫТЬ КАРТЫ");
                        CreateTitle(Container, "MagicCardsHelpCountTitle", "MagicCardsHelp", $"<size=16>Если вам интересно, что же осталось под закрытыми картами, нажмите эту кнопку и все карты будут открыты. Все закрытые карты, вскрытые таким способом, не будут вам засчитаны. Этим способом вы просто смотрите карты</size>", TextAnchor.MiddleCenter, "0 0.4", "1 0.6", "0 0 0 1", "1 -1", true);

                        CreateButton(Container, "MagiccardsAccept", "MagicCardsHelp", "1 0 0 1", "cardhelp 5", "0.45 -0.05", "0.55 0", "Понятно");
                        CuiHelper.AddUi(arg.Player(), Container);
                        break;
                    }
                case "5":
                    {
                        CreatePanel(Container, "MagicCardsHelp", "Overlay", "0 0 0 0.5", "0.175 0.125", "0.825 0.975");
                        CreateTitle(Container, "MagicCardsHelpTitle", "MagicCardsHelp", "<size=70>Магическая колода карт</size>\n<size=22>Испытай свою удачу! Открывай карты каждый день и получай призы!</size>", TextAnchor.UpperCenter, "0 0.6", "1 0.975", "0 0 0 1", "1 -1", true);
                        Container.Add(new CuiElement
                        {
                            Name = "MagicHelpCursor",
                            Parent = "MagicCardsHelp",
                            Components =
                        {
                             new CuiNeedsCursorComponent()
                          }
                        });
                        CreatePanel(Container, "MagicCardsHelpFon", "MagicCardsHelp", "0 0 0 0.99", "0 0", "1 1");
                        CreateButton(Container, "MagicCardsHelpBtnCards", "MagicCardsHelp", "0.00 0.75 1.00 1.00", "cardhelp 6", "0.135 0.02", "0.265 0.07", "ВАШИ КАРТЫ");
                        CreateTitle(Container, "MagicCardsHelpCountTitle", "MagicCardsHelp", $"<size=16>Тут вы можете посмотреть все карты, которые вы насобирали за все время\nДля продолжения обучения нажмите эту кнопку</size>", TextAnchor.MiddleCenter, "0 0.4", "1 0.6", "0 0 0 1", "1 -1", true);

                        CuiHelper.AddUi(arg.Player(), Container);
                        break;
                    }
                case "6":
                    {
                        CreatePanel(Container, "MagicCardsHelp", "Overlay", "0 0 0 0.5", "0.175 0.125", "0.825 0.975");
                        CreateTitle(Container, "MagicMyCardsHelpTitle", "MagicCardsHelp", "<size=70>Ваши собранные карты</size>", TextAnchor.UpperCenter, "0 0.8", "1 0.975", "0 0 0 1", "1 -1", true);
                        Container.Add(new CuiElement
                        {
                            Name = "MagicHelpCursor",
                            Parent = "MagicCardsHelp",
                            Components =
                        {
                             new CuiNeedsCursorComponent()
                          }
                        });
                        int x = 0, y = 0;
                        for(int i = 0; i < 39; i++)
                        {
                            CreateImage(Container, $"MagicMyCardHelp{i}", "MagicCardsHelp", GetImage("BackCard"), null, "1 1 1 1", $"{0.01 + (x * 0.076)} {0.675 - (y * 0.165)}", $"{0.076 + (x * 0.076)} {0.825 - (y * 0.165)}", 0.1f);
                            CreateButton(Container, "MagiccardsAccept", $"MagicMyCardHelp{i}", "1 0 0 0", "cardhelp 7", "0 0", "1 1", "");
                            if (x == 12)
                            {
                                x = -1;
                                y++;
                            }

                            x++;
                        }
                        CreatePanel(Container, "MagicCardsHelpFon", "MagicCardsHelp", "0 0 0 0.99", "0 0.825", "1 1");
                        CreatePanel(Container, "MagicCardsHelpFon2", "MagicCardsHelp", "0 0 0 0.99", "0 0", "1 0.325");
                        CreateTitle(Container, "MagicCardsHelpCountTitle", "MagicCardsHelp", $"<size=16>Тут будут все ваши собранные карты\nДля продолжение нажмите на любую карту</size>", TextAnchor.MiddleCenter, "0 0.1", "1 0.3", "0 0 0 1", "1 -1", true);

                        CuiHelper.AddUi(arg.Player(), Container);
                        break;
                    }
                case "7":
                    {
                        CreatePanel(Container, "MagicCardsHelp", "Overlay", "0 0 0 0.5", "0.175 0.125", "0.825 0.975");
                        CreateTitle(Container, "MagicMyCardsHelpTitle", "MagicCardsHelp", "<size=70>Ваши собранные карты</size>", TextAnchor.UpperCenter, "0 0.8", "1 0.975", "0 0 0 1", "1 -1", true);
                        Container.Add(new CuiElement
                        {
                            Name = "MagicHelpCursor",
                            Parent = "MagicCardsHelp",
                            Components =
                        {
                             new CuiNeedsCursorComponent()
                          }
                        });
                        CreatePanel(Container, "MagicCardsHelpFon", "MagicCardsHelp", "0 0 0 0.99", "0 0.325", "1 1");
                        CreateTitle(Container, "MagicCardsHelpCountTitle", "MagicCardsHelp", $"<size=16>Здесь показана информация о выбранной карте. Ее название, описание, сколько собранно, и сколько нужно собрать для получения</size>", TextAnchor.MiddleCenter, "0 0.325", "1 0.4", "0 0 0 1", "1 -1", true);
                        CreatePanel(Container, "MagicCardsHelpInfo", "MagicCardsHelp", "1 1 1 0", "0 0", "1 0.325");
                        CreateImage(Container, $"MyCardInfoImg", "MagicCardsHelpInfo", GetImage("BackCard"), null, "1 1 1 1", $"0.15 0.1", $"0.275 0.9", 0.1f);
                        CreateTitle(Container, $"MyCardInfoTitle", "MagicCardsHelpInfo", $"<size=24>НАЗВАНИЕ</size>", TextAnchor.UpperLeft, "0.3 0.75", "0.85 0.9", "0 0 0 1", "1 -1", true);
                        CreateTitle(Container, $"MyCardInfoTitleDesc", "MagicCardsHelpInfo", $"<size=16>Описание</size>", TextAnchor.UpperLeft, "0.3 0.35", "0.85 0.75", "0 0 0 1", "1 -1", true);
                        CreateTitle(Container, $"MyCardInfoTitle", "MagicCardsHelpInfo", $"<size=16>Собрано: 6/10</size>", TextAnchor.UpperLeft, "0.3 0.1", "0.85 0.2", "0 0 0 1", "1 -1", true);
                        CreateButton(Container, "MagiccardsAccept", "MagicCardsHelp", "1 0 0 1", "cardhelp 8", "0.45 -0.05", "0.55 0", "Понятно");
                        CuiHelper.AddUi(arg.Player(), Container);
                        break;
                    }
                case "8":
                    {
                        CreatePanel(Container, "MagicCardsHelp", "Overlay", "0 0 0 0.5", "0.175 0.125", "0.825 0.975");
                        CreateTitle(Container, "MagicMyCardsHelpTitle", "MagicCardsHelp", "<size=70>Ваши собранные карты</size>", TextAnchor.UpperCenter, "0 0.8", "1 0.975", "0 0 0 1", "1 -1", true);
                        Container.Add(new CuiElement
                        {
                            Name = "MagicHelpCursor",
                            Parent = "MagicCardsHelp",
                            Components =
                        {
                             new CuiNeedsCursorComponent()
                          }
                        });
                        CreatePanel(Container, "MagicCardsHelpFon", "MagicCardsHelp", "0 0 0 0.99", "0 0.325", "1 1");
                        CreateTitle(Container, "MagicCardsHelpCountTitle", "MagicCardsHelp", $"<size=16>Как только вы соберете нужное количество карты, вы сможете получить награду</size>", TextAnchor.MiddleCenter, "0 0.325", "1 0.4", "0 0 0 1", "1 -1", true);
                        CreatePanel(Container, "MagicCardsHelpInfo", "MagicCardsHelp", "1 1 1 0", "0 0", "1 0.325");
                        CreateImage(Container, $"MyCardInfoImg", "MagicCardsHelpInfo", GetImage("BackCard"), null, "1 1 1 1", $"0.15 0.1", $"0.275 0.9", 0.1f);
                        CreateTitle(Container, $"MyCardInfoTitle", "MagicCardsHelpInfo", $"<size=24>НАЗВАНИЕ</size>", TextAnchor.UpperLeft, "0.3 0.75", "0.85 0.9", "0 0 0 1", "1 -1", true);
                        CreateTitle(Container, $"MyCardInfoTitleDesc", "MagicCardsHelpInfo", $"<size=16>Описание</size>", TextAnchor.UpperLeft, "0.3 0.35", "0.85 0.75", "0 0 0 1", "1 -1", true);
                        CreateTitle(Container, $"MyCardInfoTitle", "MagicCardsHelpInfo", $"<size=16>Собрано: 10/10</size>", TextAnchor.UpperLeft, "0.3 0.1", "0.85 0.2", "0 0 0 1", "1 -1", true);
                        CreateButton(Container, "MagiccardsAccept", "MagicCardsHelp", "1 0 0 1", "cards", "0.45 -0.05", "0.55 0", "Понятно");
                        CreateButton(Container, "MyCardGet", "MagicCardsHelpInfo", "0.00 0.75 1.00 1.00", $"", "0.625 0.1", "0.725 0.3", "ПОЛУЧИТЬ");
                        CreateButton(Container, "MyCardClose", "MagicCardsHelpInfo", "1.00 0.00 1.00 1.00", "", "0.75 0.1", "0.85 0.3", "ЗАКРЫТЬ");
                        CuiHelper.AddUi(arg.Player(), Container);
                        break;
                    }
            }
        }
        #endregion

        #region Hook
        void OnServerInitialized()
        {
            LoadDefaultConfig();
            PlayerCards = Interface.Oxide.DataFileSystem.ReadObject<List<PlayerCard>>("MagicCards/Players");
            Cards = Interface.Oxide.DataFileSystem.ReadObject<List<Card>>("MagicCards/Cards");

            AddImage(BackCard, "BackCard");
            foreach(var card in Cards)
            {
                AddImage(card.ImageUrl, card.ImageName);
            }

            foreach (var player in BasePlayer.activePlayerList)
            {
                PlayerCard PCard = PlayerCards.Find(x => x.SteamID == player.userID);
                if (PCard == null)
                {
                    PCard = new PlayerCard
                    {
                        SteamID = player.userID,
                        CardsCount = 3,

                        Cards = new Dictionary<int, int>()
                    };
                    PlayerCards.Add(PCard);
                }
            }

            timer.Repeat(CardsTimer, 0, () => 
            {
                foreach(var pcard in PlayerCards)
                {
                    if (pcard.CardsCount < MaxCards) pcard.CardsCount++;
                }
            });
        }

        void OnPlayerInit(BasePlayer player)
        { 
            PlayerCard PCard = PlayerCards.Find(x => x.SteamID == player.userID);
            if (PCard == null)
            {
                PCard = new PlayerCard
                {
                    SteamID = player.userID,
                    CardsCount = 3,

                    Cards = new Dictionary<int, int>()
                };
                PlayerCards.Add(PCard);
            }
        }

        void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "MagicCards");
                CuiHelper.DestroyUi(player, "MagicMyCards");
                CuiHelper.DestroyUi(player, "MagicCardsHelp");
            }

            SaveData();
        }

        void OnServerSave()
        {
            SaveData();
        }

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("MagicCards/Players", PlayerCards);
        }
        #endregion

        #region GUI Template
        private void CreatePanel(CuiElementContainer Container, string Name, string Parent, string Color, string AnchorMin, string AnchorMax)
        {
            Container.Add(new CuiElement
            {
                Name = Name,
                Parent = Parent,
                Components = {
                        new CuiImageComponent {
                            Color = Color
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = AnchorMin,
                            AnchorMax = AnchorMax
                        }
                    }
            });
        }

        private void CreateImage(CuiElementContainer Container, string Name, string Parent, string Png, string Url, string Color, string AnchorMin, string AnchorMax, float fadein)
        {
            Container.Add(new CuiElement
            {
                Name = Name,
                Parent = Parent,
                Components = {
                        new CuiRawImageComponent {
                            Png = Png,
                            Url = Url,
                            Color = Color,
                            FadeIn = fadein
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = AnchorMin,
                            AnchorMax = AnchorMax
                        }
                    }
            });
        }

        private void CreateButton(CuiElementContainer Container, string Name, string Parent, string Color, string Command, string AnchorMin, string AnchorMax, string Text)
        {
            Container.Add(new CuiButton
            {
                Button = { Color = Color, Command = Command },
                RectTransform = { AnchorMin = AnchorMin, AnchorMax = AnchorMax },
                Text = { Text = Text, Align = TextAnchor.MiddleCenter }
            }, Parent, Name);
        }

        private void CreateTitle(CuiElementContainer Container, string Name, string Parent, string Text, TextAnchor Align, string AnchorMin, string AnchorMax, string OutlineColor, string OutlineDistance, bool UseAlpha)
        {
            Container.Add(new CuiElement
            {
                Name = Name,
                Parent = Parent,
                Components = {
                        new CuiTextComponent {
                            Text = Text,
                            Align = Align
                        },
                        new CuiRectTransformComponent {
                        AnchorMin = AnchorMin,
                        AnchorMax = AnchorMax
                        },
                        new CuiOutlineComponent
                        {
                             Color = OutlineColor,
                             Distance = OutlineDistance,
                             UseGraphicAlpha = UseAlpha
                        }
                    }
            });
        }
        #endregion
    }
}
