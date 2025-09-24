using System;
using System.Linq;
using Facepunch.Extend;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ResourceExchanger", "Kira", "1.0.0")]
    [Description("asd")]
    public class ResourceExchanger : RustPlugin
    {
        [PluginReference] private Plugin ImageLibrary, BankSystem, Shop;
        private const string UIMain = "UI.Exchanger";
        private Vector3 pos1 = new Vector3(-1.1f, 10.8f, 136.4f);
        private Quaternion rot1 = new Quaternion(0.0f, 0.0f, 0.0f, -1.0f);
        private Vector3 pos2 = new Vector3(1292.5f, 5.7f, -1359.4f);
        private Quaternion rot2 = new Quaternion(0.0f, 0.9f, 0.0f, -0.4f);
        private VendingMachine _vendingMachine1 = null;
        private VendingMachine _vendingMachine2 = null;

        #region [DrawUI]

        private void DrawUI_Exchanger(BasePlayer player)
        {
            var ui = new CuiElementContainer();

            ui.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image =
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, "Overlay", UIMain);

            ui.Add(new CuiElement
            {
                Parent = UIMain,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage($"{UIMain}.Background")
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
                        Text = "RESOURCE EXCHANGER"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.91",
                        AnchorMax = "1 1"
                    }
                }
            });

            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.AddUi(player, ui);
            DrawUI_Items(player);
        }

        private void DrawUI_Items(BasePlayer player)
        {
            var ui = new CuiElementContainer();

            int num = 0, x = 0, y = 0;
            foreach (var item in player.inventory.AllItems().Take(32))
            {
                if (x >= 8)
                {
                    x = 0;
                    y++;
                }
                
                
                var parent = $"{UIMain}.ItemBG.{num}";
                CuiHelper.DestroyUi(player, parent);
                ui.Add(new CuiElement
                {
                    Name = parent,
                    Parent = UIMain,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage($"{UIMain}.ItemBG"),
                            FadeIn = num * 0.05f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.0411458 + (x * 0.118)} {0.7203704 - (y * 0.2)}",
                            AnchorMax = $"{0.1338542 + (x * 0.118)} {0.8851852 - (y * 0.2)}"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{parent}.Icon",
                    Parent = parent,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage(item.info.shortname),
                            FadeIn = num * 0.05f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.1741533 0.2808988",
                            AnchorMax = "0.8258467 0.9325843"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Parent = $"{parent}.Icon",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleRight,
                            FontSize = 13,
                            Text = $"x{item.amount}",
                            Color = "0.39 0.40 0.44 1.00",
                            FadeIn = num * 0.05f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 0.2672416"
                        }
                    }
                });

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"exchange {item.info.shortname} {item.amount}",
                        Color = "0 0 0 0"
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 13,
                        Text = $"EXCHANGE",
                        Color = "1 1 1 0.7",
                        FadeIn = num * 0.05f
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.1044929 0.07303338",
                        AnchorMax = "0.8966372 0.23"
                    }
                }, parent);
                num++;
                x++;
            }

            CuiHelper.AddUi(player, ui);
        }

        #endregion

        #region [Hooks]

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            ImageLibrary.Call("AddImage", "https://i.imgur.com/vMjsEjU.png", $"{UIMain}.Background");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/gUoJNvH.png", $"{UIMain}.ItemBG");
            SpawnNpc(pos1, rot1, 1);
            SpawnNpc(pos2, rot2, 2);
        }

        private string GetImage(string name)
        {
            return (string) ImageLibrary.Call("GetImage", name);
        }

        // ReSharper disable once UnusedMember.Local
        private bool CanUseVending(BasePlayer player, VendingMachine machine)
        {
            switch (machine.skinID)
            {
                case 2559:
                    DrawUI_Exchanger(player);
                    return false;
                case 0:
                    return true;
                default:
                    return false;
            }
        }

        #endregion

        #region [Commands]

        [ConsoleCommand("exchange")]
        private void ASD(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            var shortname = args.Args[0];
            var amount = args.Args[1].ToInt();
            ItemShop getitem = (ItemShop) Shop.Call("GetItem", shortname);
            var math = (double) getitem.Price / 100 * 25;
            var item = player.inventory.FindItemID(shortname);
            if (item == null) return;
            if (item.amount < getitem.FixCount) return;
            if (item.condition < (item._maxCondition / 2)) return;
            player.inventory.Take(null, ItemManager.FindItemDefinition(shortname).itemid, getitem.FixCount);
            GiveBalance(player.userID, Convert.ToInt32(math));
            DrawUI_Exchanger(args.Player());
        }

        [ConsoleCommand("close")]
        private void asdd(ConsoleSystem.Arg args)
        {
            CuiHelper.DestroyUi(args.Player(), UIMain);
        }

        #endregion

        #region [Helpers]

        public class ItemShop
        {
            public string Shortname;
            public string Name;
            public ulong SkinID;
            public int FixCount;
            public float Price;
        }

        private void GiveBalance(ulong player, int money)
        {
            BankSystem.CallHook("GiveBalance", player, money);
        }

        private void SpawnNpc(Vector3 pos, Quaternion rot, int num)
        {
            var npc = GameManager.server.CreateEntity(
                "assets/prefabs/deployable/vendingmachine/vendingmachine.deployed.prefab",
                pos) as VendingMachine;
            npc.shopName = "BankMachine";
            npc.skinID = 2559;
            npc.transform.rotation = rot;
            npc.Spawn();
            npc.SendNetworkUpdate();

            switch (num)
            {
                case 1:
                    _vendingMachine1 = npc;
                    break;
                case 2:
                    _vendingMachine2 = npc;
                    break;
            }
        }

        #endregion
    }
}