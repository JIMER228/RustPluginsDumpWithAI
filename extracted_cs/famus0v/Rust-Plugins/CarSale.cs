using System.Collections.Generic;
using Facepunch.Extend;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("CarSale", "Kira", "1.0.1")]
    [Description("d")]
    public class CarSale : RustPlugin
    {
        #region [Vars]

        [PluginReference] private Plugin ImageLibrary, BankSystem;

        private const string UIMain = "UI.CarSales";
        private const string Blur = "assets/content/ui/uibackgroundblur.mat";
        private Vector3 pos1 = new Vector3(-1920.7f, 5.2f, 1415.5f);
        private Quaternion rot1 = new Quaternion(0.0f, 0.5f, 0.0f, 0.0f);
        private Vector3 pos2 = new Vector3(1299.3f,5.8f,-1360.9f);
        private Quaternion rot2 = new Quaternion(0.0f, 0.9f, 0.0f, -0.4f);
        private Vector3 posv1 = new Vector3(-1915.8f, 5.2f, 1419.8f);
        private Quaternion rotv1 = new Quaternion(0.0f, 0.0f, 0.0f, -1.0f);
        private Vector3 posv2 = new Vector3(1292.1f, 5.7f, -1358.8f);
        private Quaternion rotv2 = new Quaternion(0.0f, 0.9f, 0.0f, -0.4f);
        private VendingMachine _vendingMachine1 = null;
        private VendingMachine _vendingMachine2 = null;

        #endregion

        #region [Lang]

        protected override void LoadDefaultMessages()
        {
            var ru = new Dictionary<string, string>
            {
                ["CARSHOP"] = "ПРОДАЖА АВТО",
                ["DESCRIPTION"] = "Здесь вы можете купить автомобиль за внутриигровую валюту SHREK",
                ["SHREK"] = "ШРЕКОВ",
                ["COST"] = "СТОИМОСТЬ",
                ["PURCHASE"] = "ПРИОБРЕСТИ",
                ["NOMONEY"] = "Не хватает шреков"
            };

            var en = new Dictionary<string, string>
            {
                ["CARSHOP"] = "CAR SHOP",
                ["DESCRIPTION"] = "Here you can buy a car for the in-game currency SHREK",
                ["SHREK"] = "SHREKS",
                ["COST"] = "COST",
                ["PURCHASE"] = "PURCHASE",
                ["NOMONEY"] = "Not enough shreks"
            };
            lang.RegisterMessages(ru, this, "ru");
            lang.RegisterMessages(en, this);
        }

        #endregion

        #region [Classes]

        private class CarSetting
        {
            public string prefab;
            public int cost;
        }

        #endregion

        #region [Lists]

        private List<CarSetting> CarSettings = new List<CarSetting>
        {
            new CarSetting
            {
                prefab = "assets/content/vehicles/modularcar/2module_car_spawned.entity.prefab",
                cost = 500
            },

            new CarSetting
            {
                prefab = "assets/content/vehicles/modularcar/3module_car_spawned.entity.prefab",
                cost = 1000
            },
            new CarSetting
            {
                prefab = "assets/content/vehicles/modularcar/4module_car_spawned.entity.prefab",
                cost = 1500
            }
        };

        #endregion

        #region [DrawUI]

        private void DrawUI_Main(BasePlayer player, string type)
        {
            var ui = new CuiElementContainer
            {
                {
                    new CuiPanel
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
                    },
                    "Overlay", UIMain
                },
                new CuiElement
                {
                    Name = $"{UIMain}.Background",
                    Parent = UIMain,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = (string) ImageLibrary.Call("GetImage", "UI.CarSales.Background")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    }
                }
            };

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
                        Text = lang.GetMessage("CARSHOP", this, player.UserIDString)
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
                        Text = lang.GetMessage("DESCRIPTION", this, player.UserIDString)
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
                Name = $"{UIMain}.CarName1",
                Parent = UIMain,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 20,
                        Color = "0.39 0.40 0.44 1.00",
                        Text = "<size=45><color=#ef631f>LVL 1</color></size>\n" +
                               $"{lang.GetMessage("COST", this, player.UserIDString)}➜ {CarSettings[0].cost} " +
                               $"{lang.GetMessage("SHREK", this, player.UserIDString)}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.08125 0.410186",
                        AnchorMax = "0.3520351 0.6045742"
                    }
                }
            });

            ui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"carsale.buy 0 {type}",
                    Color = "0 0 0 0",
                    Close = UIMain
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.8",
                    FontSize = 14,
                    Text = lang.GetMessage("PURCHASE", this, player.UserIDString)
                },
                RectTransform =
                {
                    AnchorMin = "0 -0.3191438",
                    AnchorMax = "1 -0.0663594"
                }
            }, $"{UIMain}.CarName1");

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.CarName2",
                Parent = UIMain,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 20,
                        Color = "0.39 0.40 0.44 1.00",
                        Text = "<size=45><color=#ef631f>LVL 2</color></size>\n" +
                               $"{lang.GetMessage("COST", this, player.UserIDString)}➜ {CarSettings[1].cost} " +
                               $"{lang.GetMessage("SHREK", this, player.UserIDString)}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.3645833 0.410186",
                        AnchorMax = "0.6353683 0.6045742"
                    }
                }
            });

            ui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"carsale.buy 1 {type}",
                    Color = "0 0 0 0",
                    Close = UIMain
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.8",
                    FontSize = 14,
                    Text = lang.GetMessage("PURCHASE", this, player.UserIDString)
                },
                RectTransform =
                {
                    AnchorMin = "0 -0.3191438",
                    AnchorMax = "1 -0.0663594"
                }
            }, $"{UIMain}.CarName2");

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.CarName3",
                Parent = UIMain,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 20,
                        Color = "0.39 0.40 0.44 1.00",
                        Text = "<size=45><color=#ef631f>LVL 3</color></size>\n" +
                               $"{lang.GetMessage("COST", this, player.UserIDString)}➜ {CarSettings[2].cost} " +
                               $"{lang.GetMessage("SHREK", this, player.UserIDString)}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.6484329 0.410186",
                        AnchorMax = "0.9192179 0.6045742"
                    }
                }
            });

            ui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"carsale.buy 2 {type}",
                    Color = "0 0 0 0",
                    Close = UIMain
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.8",
                    FontSize = 16,
                    Text = lang.GetMessage("PURCHASE", this, player.UserIDString)
                },
                RectTransform =
                {
                    AnchorMin = "0 -0.3191438",
                    AnchorMax = "1 -0.0663594"
                }
            }, $"{UIMain}.CarName3");

            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.AddUi(player, ui);
        }

        #endregion

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            ImageLibrary.Call("AddImage", "https://i.imgur.com/7nkR6Hf.png", "UI.CarSales.Background");
            SpawnNpc(1);
            SpawnNpc(2);
        }

        // ReSharper disable once UnusedMember.Local
        private bool CanUseVending(BasePlayer player, VendingMachine machine)
        {
            switch (machine.skinID)
            {
                case 2558:
                    DrawUI_Main(player, "1");
                    return false;
                case 2559:
                    DrawUI_Main(player, "2");
                    return false;
                case 0:
                    return true;
                default:
                    return false;
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void Unload()
        {
            _vendingMachine1.Kill();
            _vendingMachine2.Kill();
        }

        [ConsoleCommand("carsale.buy")]
        // ReSharper disable once UnusedMember.Local
        private void BuyCar(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            var carcfg = CarSettings[args.Args[0].ToInt()];
            if ((int) BankSystem.Call("GetBalance", player.userID) < carcfg.cost)
            {
                player.ChatMessage(lang.GetMessage("NOMONEY", this, player.UserIDString));
                return;
            }

            var type = args.Args[1];
            var car = (ModularCar) GameManager.server.CreateEntity(carcfg.prefab);
            switch (type)
            {
                case "1":
                    car.transform.SetPositionAndRotation(pos1, rot1);
                    break;
                case "2":
                    car.transform.SetPositionAndRotation(pos2, rot2);
                    break;
            }

            car.Spawn();
            car.SendNetworkUpdate();
            BankSystem.Call("TakeBalance", player.userID, carcfg.cost);
        }

        private void SpawnNpc(int num)
        {
            var npc = GameManager.server.CreateEntity(
                "assets/prefabs/deployable/vendingmachine/vendingmachine.deployed.prefab") as VendingMachine;
            npc.shopName = "CarSale";
            var transform = npc.transform;
            npc.SendNetworkUpdate();

            switch (num)
            {
                case 1:
                    transform.position = posv1;
                    transform.rotation = rotv1;
                    npc.skinID = 2558;
                    _vendingMachine1 = npc;
                    break;
                case 2:
                    transform.position = posv2;
                    transform.rotation = rotv2;
                    npc.skinID = 2559;
                    _vendingMachine2 = npc;
                    break;
            }

            npc.Spawn();
        }
    }
}