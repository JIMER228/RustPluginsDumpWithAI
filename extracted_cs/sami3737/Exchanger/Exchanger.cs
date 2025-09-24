//Requires: ImageLibrary

using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Exchanger", "Sami37", "1.0.0"), Description("Exchange your bills for scrap.")]
    class Exchanger : RustPlugin
    {
        #region References

        [PluginReference] ImageLibrary ImageLibrary;

        #endregion

        private string perm = "Exchanger.admin";
        private string ShopOverlayName = "ExchangerOverlay";
        private const string BackgroundImage = "ExchangerBackground";

        private ExchangerData _exData;

        private DynamicConfigFile data;

        private void SaveData()
        {
            data.WriteObject(_exData);
        }

        private class ExchangerData
        {
            public List<Info> ShopList = new List<Info>();
        }

        private class Info
        {
            public string ShopName;
            public List<ulong> NPCList = new List<ulong>();
            public List<ShopInfo> shops = new List<ShopInfo>();
        }

        private class ShopInfo
        {
            public string MoneyItemShortname;
            public int MoneyItemAmount;
            public string ItemShortname;
            public int ItemAmount;
        }

        private void LoadData()
        {
            data = Interface.Oxide.DataFileSystem.GetFile(Name);

            try
            {
                _exData = data.ReadObject<ExchangerData>();
            }
            catch
            {
                _exData = new ExchangerData();
            }
        }

        private void LoadImages()
        {
            Dictionary<string, string> imageList = new Dictionary<string, string>
            {
                {BackgroundImage, "http://screenup.cleanmeca.be/uploads/1635084187.jpg"}
            };

            LoadOrder(Title, imageList, 0, true);
        }

        private void LoadOrder(string title, Dictionary<string, string> importImageList, ulong skin, bool force) => ImageLibrary?.ImportImageList(title, importImageList, skin, force);

        void GuiDestroy(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, ShopOverlayName);
        }

        private string GetImage(string name) => ImageLibrary.GetImage(name);

        #region Oxide Hooks
        private void OnServerInitialized()
        {
            permission.RegisterPermission(perm, this);
            LoadData();
            LoadImages();
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(_messages, this);
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                GuiDestroy(player);
            }
        }

        private void OnUseNPC(BasePlayer npc, BasePlayer player)
        {
            if (player == null) return;

            var datas = _exData.ShopList.FindAll(x => x.NPCList.Contains(npc.userID));
            int i = 0;
            if (datas.Count != 0)
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    Image =
                    {
                        Color = "0 0 0 0"
                    },
                    RectTransform = { AnchorMin = "0.3 0.3", AnchorMax = "0.7 0.7" },
                    CursorEnabled = true
                },
                "Overlay", ShopOverlayName);

                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = ShopOverlayName,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage(BackgroundImage),
                            Color = "1 1 1 1"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    }
                });

                foreach (var itemData in datas)
                {
                    container.Add(new CuiLabel
                    {
                        Text =
                        {
                            Text = itemData.ShopName,
                            FontSize = 20,
                            Align = TextAnchor.MiddleCenter
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.05 0.85",
                            AnchorMax = "0.95 0.95"
                        }
                    }, ShopOverlayName);

                    foreach (var items in itemData.shops)
                    {
                        var pos = 0.75 - 0.75 * (i * 0.25);
                        var pos2 = 0.85 - 0.75 * (i * 0.25);

                        var money = ItemManager.CreateByPartialName(items.MoneyItemShortname, items.MoneyItemAmount);
                        var getItem = ItemManager.CreateByPartialName(items.ItemShortname, items.ItemAmount);

                        container.Add(new CuiLabel
                        {
                            Text =
                            {
                                Text = $"{getItem.amount} x {getItem.name ?? getItem.info.displayName.translated} each {money.amount} x {money.name ?? money.info.displayName.translated}",
                                FontSize = 16,
                                Align = TextAnchor.MiddleLeft
                            },
                            RectTransform =
                            {
                                AnchorMin = $"0.05 {pos}",
                                AnchorMax = $"0.75 {pos2}"
                            }
                        }, ShopOverlayName);

                        container.Add(new CuiButton
                        {
                            Text =
                            {
                                Text = "Exchange",
                                FontSize = 16,
                                Align = TextAnchor.MiddleCenter
                            },
                            Button =
                            {
                                Color = "0 1 0 0.4",
                                Command = $"ex {items.ItemShortname}-{items.ItemAmount} {items.MoneyItemShortname}-{items.MoneyItemAmount}"
                            },
                            RectTransform =
                            {
                                AnchorMin = $"0.75 {pos}",
                                AnchorMax = $"0.95 {pos2}"
                            }
                        }, ShopOverlayName);

                        i++;
                    }
                }

                container.Add(
                    new CuiButton
                    {
                        Button =
                        {
                            Command = "ExchangerDestroy",
                            Close = ShopOverlayName,
                            Color = "0.8 0.8 0.8 0.2"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.86 0.92",
                            AnchorMax = "0.97 0.98"
                        },
                        Text =
                        {
                            Text = "X",
                            FontSize = 16,
                            Align = TextAnchor.MiddleCenter
                        }
                    }, ShopOverlayName);

                CuiHelper.AddUi(player, container);
            }
        }
        #endregion

        #region Messaging
        private string Message(string key, string ID = null) => lang.GetMessage(key, this, ID);

        private readonly Dictionary<string, string> _messages = new Dictionary<string, string>
        {
            {"NoPerm", "You don't have permission to do that."},
            {"Created", "Sample data file created."},
            {"Dropped", "You don't have enough space in your inventory, just dropped to your feets." },
            {"Inventory", "Just added the reward to your inventory."},
            {"CantProcess", "Not enough money, can't afford."}
        };
        #endregion

        #region Command

        [ConsoleCommand("cmdDestroyUI")]
        void cmdDestroyUI(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            GuiDestroy(arg.Player());
        }

        [ConsoleCommand("ex")]
        private void cmdConsoleCommand(ConsoleSystem.Arg args)
        {
            var player = args.Player();

            if (player != null)
            {
                var getItemData = args.Args[0].Split('-');
                var moneyItemData = args.Args[1].Split('-');

                if (getItemData.Length == 2 && moneyItemData.Length == 2)
                {
                    var getitem = ItemManager.CreateByPartialName(getItemData[0], Convert.ToInt32(getItemData[1]));
                    var moneyitem = ItemManager.CreateByPartialName(moneyItemData[0], Convert.ToInt32(moneyItemData[1]));

                    if (player.inventory.FindItemID(moneyitem.info.itemid)?.amount >= moneyitem.amount)
                    {
                        player.inventory.Take(null, moneyitem.info.itemid, moneyitem.amount);
                        
                        if (!getitem.MoveToContainer(player.inventory.containerMain))
                        {
                            getitem.Drop(player.eyes.position, player.eyes.BodyForward() * 2f);
                            SendReply(player, Message("Dropped", player.UserIDString));
                        }
                        else
                        {
                            SendReply(player, Message("Inventory", player.UserIDString));
                        }
                    }
                    else
                    {
                        SendReply(player, Message("CantProcess", player.UserIDString));
                    }
                }

            }
        }

        [ChatCommand("ex")]
        private void cmdChatShopAdd(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, perm))
            {
                SendReply(player, Message("NoPerm", player.UserIDString));
                return;
            }
            SendReply(player, Message("Created", player.UserIDString));

            _exData = new ExchangerData();
            _exData.ShopList.Add(new Info
                {
                    ShopName = "Sample",
                    NPCList = new List<ulong>
                    {
                        103863,
                        297318
                    },
                    shops = new List<ShopInfo>
                    {
                        new ShopInfo
                        {
                            MoneyItemShortname = "paper",
                            ItemShortname = "scrap",
                            MoneyItemAmount = 10,
                            ItemAmount = 2
                        },
                        new ShopInfo
                        {
                            MoneyItemShortname = "hq.metal.ore",
                            ItemShortname = "scrap",
                            MoneyItemAmount = 3,
                            ItemAmount = 1
                        },
                        new ShopInfo
                        {
                            MoneyItemShortname = "metal.refined",
                            ItemShortname = "scrap",
                            MoneyItemAmount = 1,
                            ItemAmount = 2
                        }
                    }
            });
            SaveData();
        }

        #endregion
    }
}