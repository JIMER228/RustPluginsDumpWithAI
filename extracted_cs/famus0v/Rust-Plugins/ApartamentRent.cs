using System;
using System.Collections.Generic;
using Facepunch.Extend;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace Oxide.Plugins
{
    [Info("ApartamentRent", "Kira", "1.0.2")]
    [Description("Renting apartments and buying houses")]
    public class ApartamentRent : RustPlugin
    {
        #region [Vars]

        [PluginReference] private Plugin ImageLibrary, BankSystem;
        private StoredData _dataBase = new StoredData();

        private const string UIMain = "UI.ApartamentRent";
        private const string Blur = "assets/content/ui/uibackgroundblur.mat";
        private const int Cost = 500;
        private const int CostS = 10000;

        private List<CardReader> Readers = new List<CardReader>();

        #endregion

        #region [Lang]

        protected override void LoadDefaultMessages()
        {
            var ru = new Dictionary<string, string>
            {
                ["APARTAMENT"] = "КВАРТИРА",
                ["APARTAMENTRENT"] = "АРЕНДА КВАРТИРЫ",
                ["RDESCRIPTION"] = "Вы можете снять квартиру на определенное количество дней, за ШРЕКИ",
                ["RENT"] = "<size=40><color=#ef631f>КВАРТИРА #{1}</color></size>\n" + "СТОИМОСТЬ ➜ {2} ШРЕКОВ",
                ["CALCULATE"] = "Стоимость за {1} дней аренды = {2} ШРЕКОВ",
                ["APARTAMENTSALE"] = "ПРОДАЖА ДОМА",
                ["SDESCRIPTION"] = "Вы можете купить дом за ШРЕКИ",
                ["SCOST"] = "СТОИМОСТЬ {1} ШРЕКОВ",
                ["RENTED"] = "Вы арендовали квартиру",
                ["HOUSEPURCHASED"] = "Вы приобрели дом",
                ["KEYRENT"] = "Ключ от квартиры #{1}",
                ["KEYSALE"] = "Ключ от дома #{1}"
            };

            var en = new Dictionary<string, string>
            {
                ["APARTAMENT"] = "APARTAMENT",
                ["APARTAMENTRENT"] = "APARTAMENT RENT",
                ["RDESCRIPTION"] = "You can rent an apartment for a certain number of days, for SHREKS",
                ["RENT"] = "<size=40><color=#ef631f>APARTAMENTS #{1}</color></size>\n" + $"COST ➜ {2} SHREKS",
                ["CALCULATE"] = "Cost for {1} days of rent = {2} SHREK",
                ["APARTAMENTSALE"] = "APARTAMENT SALE",
                ["SDESCRIPTION"] = "You can buy a house for SHREKS",
                ["SCOST"] = "COST {1} SHREKS",
                ["RENTED"] = "Have you rented an apartment",
                ["HOUSEPURCHASED"] = "You have purchased a house",
                ["KEYRENT"] = "The key to the apartment #{1}",
                ["KEYSALE"] = "The key to the house #{1}"
            };
            lang.RegisterMessages(ru, this, "ru");
            lang.RegisterMessages(en, this);
        }

        #endregion

        #region [DrawUI]

        private void DrawUI_Main(BasePlayer player, int id)
        {
            var ui = new CuiElementContainer();

            ui.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image =
                {
                    Color = "0 0 0 0.8",
                    Material = Blur
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, "Overlay", UIMain);

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Background",
                Parent = UIMain,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "UI.ApartamentRent.Background")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            ui.Add(new CuiButton
            {
                Button =
                {
                    Close = UIMain,
                    Color = "0 0 0 0"
                },
                Text = {Text = " "},
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, UIMain);

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Heading",
                Parent = UIMain,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 25,
                        Color = "0.39 0.40 0.44 1.00",
                        Text = lang.GetMessage("APARTAMENTRENT", this, player.UserIDString)
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.91",
                        AnchorMax = "1 1"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Heading",
                Parent = UIMain,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 20,
                        Color = "0.39 0.40 0.44 1.00",
                        Text = lang.GetMessage("RDESCRIPTION", this, player.UserIDString)
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 0.09"
                    }
                }
            });
            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Panel",
                Parent = UIMain,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.3651042 0.3490741",
                        AnchorMax = "0.6765625 0.6037037"
                    }
                }
            });

            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.AddUi(player, ui);
            DrawUI_UpdateCost(player, 1, id);
        }

        private void DrawUI_UpdateCost(BasePlayer player, int count, int id)
        {
            var ui = new CuiElementContainer();
            var parent = $"{UIMain}.Panel";
            var reader = _dataBase.Readers[id];

            ui.Add(new CuiElement
            {
                Name = $"{parent}.Text",
                Parent = parent,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 20,
                        Color = "0.39 0.40 0.44 1.00",
                        Text = lang.GetMessage("RENT", this, player.UserIDString).Replace("{1}", $"{id}")
                            .Replace("{2}", $"{reader.cost}")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.2363637",
                        AnchorMax = "0.8678929 1"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{parent}.Cost",
                Parent = parent,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 15,
                        Color = "0.39 0.40 0.44 1.00",
                        Text = lang.GetMessage("CALCULATE", this, player.UserIDString).Replace("{1}", $"{count}")
                            .Replace("{2}", $"{reader.cost * count}")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "0.8678929 0.1927272"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{parent}.Count",
                Parent = parent,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 20,
                        Color = "0.39 0.40 0.44 1.00",
                        Text = $"{count}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.8879598 0.4363636",
                        AnchorMax = "0.994 0.8"
                    }
                }
            });

            ui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"apartament {count} + {id}",
                    Color = "0 0 0 0"
                },
                Text = {Text = " "},
                RectTransform =
                {
                    AnchorMin = "0.8879598 0.8000001",
                    AnchorMax = "1 1"
                }
            }, parent, $"{parent}.+");

            ui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"apartament {count} - {id}",
                    Color = "0 0 0 0"
                },
                Text = {Text = " "},
                RectTransform =
                {
                    AnchorMin = "0.8879598 0.2363642",
                    AnchorMax = "1 0.4363642"
                }
            }, parent, $"{parent}.-");

            ui.Add(new CuiElement
            {
                Name = $"{parent}.Cart",
                Parent = $"{parent}",
                Components =
                {
                    new CuiImageComponent
                    {
                        Sprite = "assets/icons/store.png"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.905 0.02",
                        AnchorMax = "0.975 0.17"
                    }
                }
            });

            ui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"apartament.rent {count} {id}",
                    Color = "1 1 1 0"
                },
                Text = {Text = " "},
                RectTransform =
                {
                    AnchorMin = "0.8879598 0",
                    AnchorMax = "1 0.1927272"
                }
            }, parent, $"{parent}.Rent");

            CuiHelper.DestroyUi(player, $"{parent}.-");
            CuiHelper.DestroyUi(player, $"{parent}.+");
            CuiHelper.DestroyUi(player, $"{parent}.Count");
            CuiHelper.DestroyUi(player, $"{parent}.Cost");
            CuiHelper.DestroyUi(player, $"{parent}.Text");
            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_MainSale(BasePlayer player, int id)
        {
            var ui = new CuiElementContainer();

            ui.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image =
                {
                    Color = "0 0 0 0.8",
                    Material = Blur
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, "Overlay", UIMain);

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Background",
                Parent = UIMain,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "UI.ApartamentSale.Background")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            ui.Add(new CuiButton
            {
                Button =
                {
                    Close = UIMain,
                    Color = "0 0 0 0"
                },
                Text = {Text = " "},
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, UIMain);

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Heading",
                Parent = UIMain,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 25,
                        Color = "0.39 0.40 0.44 1.00",
                        Text = lang.GetMessage("APARTAMENTSALE", this, player.UserIDString)
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.91",
                        AnchorMax = "1 1"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Heading",
                Parent = UIMain,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 20,
                        Color = "0.39 0.40 0.44 1.00",
                        Text = lang.GetMessage("SDESCRIPTION", this, player.UserIDString)
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 0.09"
                    }
                }
            });
            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.PanelSale",
                Parent = UIMain,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.3651042 0.3490741",
                        AnchorMax = "0.6354166 0.6037037"
                    }
                }
            });

            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.AddUi(player, ui);
            DrawUI_UpdateCostSale(player, id);
        }

        private void DrawUI_UpdateCostSale(BasePlayer player, int id)
        {
            var ui = new CuiElementContainer();
            var parent = $"{UIMain}.PanelSale";
            var house = _dataBase.ReaderSales[id];

            ui.Add(new CuiElement
            {
                Name = $"{parent}.Text",
                Parent = parent,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 20,
                        Color = "0.39 0.40 0.44 1.00",
                        Text =
                            $"<size=40><color=#ef631f>{lang.GetMessage("APARTAMENT", this, player.UserIDString)} #{id}</color></size>"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.2363637",
                        AnchorMax = "1 1"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{parent}.Cost",
                Parent = parent,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 15,
                        Color = "0.39 0.40 0.44 1.00",
                        Text = lang.GetMessage("SCOST", this, player.UserIDString).Replace("{1}", house.cost.ToString())
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "0.8516378 0.1927272"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{parent}.Cart",
                Parent = $"{parent}",
                Components =
                {
                    new CuiImageComponent
                    {
                        Sprite = "assets/icons/store.png"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.89 0.02",
                        AnchorMax = "0.97 0.17"
                    }
                }
            });

            ui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"apartament.buy {id}",
                    Color = "1 1 1 0"
                },
                Text = {Text = " "},
                RectTransform =
                {
                    AnchorMin = "0.8728325 0",
                    AnchorMax = "1 0.1927272"
                }
            }, parent, $"{parent}.Sale");

            CuiHelper.DestroyUi(player, $"{parent}.-");
            CuiHelper.DestroyUi(player, $"{parent}.+");
            CuiHelper.DestroyUi(player, $"{parent}.Count");
            CuiHelper.DestroyUi(player, $"{parent}.Cost");
            CuiHelper.DestroyUi(player, $"{parent}.Text");
            CuiHelper.AddUi(player, ui);
        }

        #endregion

        #region [Hooks]

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            LoadData();
            ImageLibrary.Call("AddImage", "https://i.imgur.com/RWSTMyB.png", "UI.ApartamentRent.Background");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/B0QSW6H.png", "UI.ApartamentSale.Background");
            NextFrame(FindReader);
        }

        // ReSharper disable once UnusedMember.Local
        private void Unload()
        {
            SaveData();
            foreach (var r in Readers) r.Kill();
        }

        // ReSharper disable once UnusedMember.Local
        private object OnCardSwipe(CardReader cardReader, Keycard card, BasePlayer player)
        {
            if (cardReader.skinID == 0) return null;
            if (card.skinID == 0) return null;
            card.GetItem().RepairCondition(1000);
            if (cardReader.skinID <= 18)
            {
                var readerr = _dataBase.Readers[Convert.ToInt32(cardReader.skinID)];
                if (readerr.OwnerID == 0) DrawUI_Main(player, Convert.ToInt32(cardReader.skinID));
                if (cardReader.skinID != card.skinID) return null;
                var door = (Door) BaseNetworkable.serverEntities.Find(Convert.ToUInt32(readerr.DoorID));
                door.SetOpen(true);
                timer.Once(2f, () => door.SetOpen(false));
                if (IsRent(Convert.ToInt32(cardReader.skinID))) return null;
                readerr.OwnerID = 0;
                readerr.renttime = 0;
            }
            else
            {
                var readers = _dataBase.ReaderSales[Convert.ToInt32(cardReader.skinID)];
                if (readers.OwnerID == 0) DrawUI_MainSale(player, Convert.ToInt32(cardReader.skinID));
                if (cardReader.skinID != card.skinID) return null;
                var door = (Door) BaseNetworkable.serverEntities.Find(Convert.ToUInt32(readers.DoorID));
                door.SetOpen(true);
                timer.Once(2f, () => door.SetOpen(false));
                card.GetItem().RepairCondition(100);
            }

            return null;
        }

        #endregion

        #region [Helpers]

        private void FindReader()
        {
            foreach (var r in _dataBase.Readers)
            {
                var cardreader =
                    GameManager.server.CreateEntity("assets/prefabs/io/electric/switches/cardreader.prefab") as
                        CardReader;
                var transform = cardreader.transform;
                transform.position = r.Value.pos;
                transform.Rotate(Quaternion.Euler(0f, r.Value.rotx, 0f).eulerAngles);
                cardreader.UpdateHasPower(10, 0);
                cardreader.skinID = Convert.ToUInt64(r.Key);
                cardreader.Spawn();
                cardreader.SendNetworkUpdate();
                Readers.Add(cardreader);
            }

            foreach (var r in _dataBase.ReaderSales)
            {
                var cardreader =
                    GameManager.server.CreateEntity("assets/prefabs/io/electric/switches/cardreader.prefab") as
                        CardReader;
                var transform = cardreader.transform;
                transform.position = r.Value.pos;
                transform.Rotate(Quaternion.Euler(0f, r.Value.rotx, 0f).eulerAngles);
                cardreader.UpdateHasPower(10, 0);
                cardreader.skinID = Convert.ToUInt64(r.Key);
                cardreader.Spawn();
                cardreader.SendNetworkUpdate();
                Readers.Add(cardreader);
            }
        }

        private void GiveCard(BasePlayer player, int room, int days)
        {
            var card = ItemManager.CreateByName("keycard_green", 1, Convert.ToUInt64(room));
            card.name = lang.GetMessage("KEYRENT", this, player.UserIDString).Replace("{1}", $"{room}");
            card.skin = Convert.ToUInt64(room);
            var readerr = _dataBase.Readers[room];
            if ((int) BankSystem.Call("GetBalance", player.userID) < (days * readerr.cost)) return;
            readerr.OwnerID = player.userID;
            readerr.renttime = CurrentTime() + (86400 * days);
            BankSystem?.Call("TakeBalance", player.userID, days * readerr.cost);
            player.GiveItem(card);
            player.ChatMessage(lang.GetMessage("RENTED", this, player.UserIDString));
        }

        private void GiveCardSale(BasePlayer player, int room)
        {
            var card = ItemManager.CreateByName("keycard_green", 1, Convert.ToUInt64(room));
            card.name = lang.GetMessage("KEYSALE", this, player.UserIDString).Replace("{1}", $"{room}");
            card.skin = Convert.ToUInt64(room);
            var readers = _dataBase.ReaderSales[room];
            if ((int) BankSystem.Call("GetBalance", player.userID) < readers.cost) return;
            readers.OwnerID = player.userID;
            BankSystem.Call("TakeBalance", player.userID, readers.cost);
            player.GiveItem(card);
            player.ChatMessage(lang.GetMessage("HOUSEPURCHASED", this, player.UserIDString));
        }

        #endregion

        #region [Commands]

        private int reader;

        [ConsoleCommand("add.reader")]
        // ReSharper disable once UnusedMember.Local
        private void FindEntity(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!player.IsAdmin) return;
            RaycastHit h;
            Physics.Raycast(player.eyes.HeadRay(), out h, 1000);
            if (h.GetEntity() == null) return;
            var ent = h.GetEntity();
            if (ent == null) return;
            var message = $"Reader : {ent.skinID}";
            player.ChatMessage(message);
            PrintWarning(message);
            reader = Convert.ToInt32(ent.skinID);
        }

        [ConsoleCommand("add.doorrent")]
        // ReSharper disable once UnusedMember.Local
        private void FindEntitysr(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!player.IsAdmin) return;
            RaycastHit h;
            Physics.Raycast(player.eyes.HeadRay(), out h, 1000);
            if (h.GetEntity() == null) return;
            var ent = h.GetEntity();
            if (ent == null) return;
            var message = $"DoorID : {ent.prefabID}";
            player.ChatMessage(message);
            PrintWarning(message);
            if (!_dataBase.Readers.ContainsKey(reader)) _dataBase.Readers.Add(reader, new StoredData.Reader());
            _dataBase.Readers[reader].DoorID = ent.prefabID;
        }

        [ConsoleCommand("add.doorsales")]
        // ReSharper disable once UnusedMember.Local
        private void FindEntitys(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!player.IsAdmin) return;
            RaycastHit h;
            Physics.Raycast(player.eyes.HeadRay(), out h, 1000);
            if (h.GetEntity() == null) return;
            var ent = h.GetEntity();
            if (ent == null) return;
            var message = $"DoorID : {ent.prefabID}";
            player.ChatMessage(message);
            PrintWarning(message);
            if (!_dataBase.ReaderSales.ContainsKey(reader))
                _dataBase.ReaderSales.Add(reader, new StoredData.ReaderSale());
            _dataBase.ReaderSales[reader].DoorID = ent.prefabID;
        }

        [ConsoleCommand("apartament")]
        // ReSharper disable once UnusedMember.Local
        private void Counts(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            var count = args.Args[0].ToInt();
            switch (args.Args[1])
            {
                case "+":
                    count++;
                    break;
                case "-":
                    if (count <= 1) return;
                    count--;
                    break;
            }

            DrawUI_UpdateCost(player, count, 1);
        }

        [ConsoleCommand("apartament.rent")]
        // ReSharper disable once UnusedMember.Local
        private void Rent(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            var count = args.Args[0].ToInt();
            var room = args.Args[1].ToInt();
            GiveCard(args.Player(), room, count);
            CuiHelper.DestroyUi(player, UIMain);
        }

        [ConsoleCommand("apartament.buy")]
        // ReSharper disable once UnusedMember.Local
        private void Buy(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            var room = args.Args[0].ToInt();
            GiveCardSale(args.Player(), room);
            CuiHelper.DestroyUi(player, UIMain);
        }

        private bool IsRent(int room)
        {
            var playercooldown = _dataBase.Readers[room];
            PrintError((playercooldown.renttime - CurrentTime()).ToString());
            PrintError(((playercooldown.renttime - CurrentTime()) > 0).ToString());
            return (playercooldown.renttime - CurrentTime()) > 0;
        }

        [ConsoleCommand("apartamentrent.spawnnpc")]
        // ReSharper disable once UnusedMember.Local
        private void SpawnNpc(ConsoleSystem.Arg args)
        {
            if (!args.Player().IsAdmin) return;
            var player = args.Player();
            var npc = GameManager.server.CreateEntity(
                "assets/prefabs/deployable/vendingmachine/vendingmachine.deployed.prefab",
                player.transform.position) as VendingMachine;
            npc.shopName = "Car Sales";
            npc.skinID = 2558;
            var pos = player.GetNetworkRotation();
            npc.transform.rotation = new Quaternion(0, pos.y, 0, pos.w);
            npc.enableSaving = true;
            npc.Spawn();
            npc.SendNetworkUpdate();
        }

        [ConsoleCommand("apartament.clear")]
        // ReSharper disable once UnusedMember.Local
        private void ClearDataBase(ConsoleSystem.Arg args)
        {
            if (args.Player() != null) return;
            args.Player().GiveItem(ItemManager.CreateByName("keycard_green", 1, 1));
        }

        #endregion

        #region [DataBase]

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        private static int CurrentTime() => (int) DateTime.UtcNow.Subtract(Epoch).TotalSeconds;

        public class StoredData
        {
            public Dictionary<int, Reader> Readers = new Dictionary<int, Reader>();
            public Dictionary<int, ReaderSale> ReaderSales = new Dictionary<int, ReaderSale>();

            public class Reader
            {
                public Vector3 pos;
                public int rotx;
                public ulong OwnerID;
                public int renttime;
                public int cost;
                public ulong DoorID;
            }

            public class ReaderSale
            {
                public Vector3 pos;
                public int rotx;
                public ulong OwnerID;
                public int cost;
                public ulong DoorID;
            }
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

        #endregion
    }
}