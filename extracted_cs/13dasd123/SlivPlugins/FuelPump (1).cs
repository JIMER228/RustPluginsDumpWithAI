// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using CompanionServer.Handlers;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("FuelPump", "Pwenill", "1.0.9")]
    [Description("This plugin was fixed by Инкуб to order [Rust Plugin Sliv]: https://discord.gg/pFgKw6Dyyq")]
    class FuelPump : RustPlugin
    {
        #region Variables
        [PluginReference]
        private Plugin ImageLibrary;

        private Dictionary<ulong, InstanceFuel> _instancesFuel = new Dictionary<ulong, InstanceFuel>();

        List<ulong> showing_UI = new List<ulong>();

        // UI Variables
        public string FuelPumpUI = "FuelPump.Info.UI";
        public string FuelPumpMainUI = "FuelPump.Main.UI";
        public string FuelPumpLoadingUI = "FuelPump.Loading.UI";

        public float distanceTarget = 3f;

        #endregion

        #region Config
        private PluginConfig config;

        private class PluginConfig
        {
            public int Pricing;
            public int FillingLimit;
            public string Title;
            public string ImageLogo;
            public int PayItemID;
            public int TimeFilling;
            public float DistanceOfQuit;
            public string Currency;
        }

        private void Init()
        {
            config = Config.ReadObject<PluginConfig>();
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(GetDefaultConfig(), true);
        }

        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                Title = "Fuel Pump",
                Currency = "SCRAP",
                Pricing = 5,
                FillingLimit = 1000,
                ImageLogo = "https://gspics.org/images/2024/01/15/0lrCKQ.png",
                PayItemID = -932201673,
                TimeFilling = 10,
                DistanceOfQuit = 5f,
            };
        }
        #endregion

        #region Class
        private class InstanceFuel
        {
            public int Price;
            public int BasedFuel;
            public Timer timer;
            public Vector3 Position;

            public InstanceFuel(Vector3 position)
            {
                Position = position;
            }
        }
        #endregion

        #region Hooks
        private void OnServerInitialized()
        {
            if (ImageLibrary != null)
            {
                if (!ImageLibrary.IsLoaded)
                    PrintWarning("ImageLibrary is not loaded");

                if (!(bool)ImageLibrary.Call("HasImage", "GasPump_Close_Logo"))
                    ImageLibrary.Call("AddImage", "https://gspics.org/images/2024/01/15/0lrKnx.png", "GasPump_Close_Logo");

                if (!(bool)ImageLibrary.Call("HasImage", "GasPump_Icon"))
                    ImageLibrary.Call("AddImage", "https://gspics.org/images/2024/01/15/0lrVpw.png", "GasPump_Icon");

                if (!(bool)ImageLibrary.Call("HasImage", "GasPump_UI_Logo"))
                    ImageLibrary.Call("AddImage", config.ImageLogo, "GasPump_UI_Logo");
            }
            else
            {
                PrintWarning("ImageLibrary is not installed");
            }
        }
        void OnPlayerTick(BasePlayer player)
        {
            HideUI(player);

            var entity = FindTransform(new Ray(player.eyes.position, player.eyes.HeadForward()), distanceTarget);
            if (entity != null)
            {
                if (!player.isMounted)
                    return;

                if (entity.name.Contains("gas_pump"))
                    ShowUI(player);
            }

            if (showing_UI.Contains(player.userID))
                return;

            InstanceFuel _instancePlayer;
            if (_instancesFuel.TryGetValue(player.userID, out _instancePlayer))
            {
                if (Vector3.Distance(_instancePlayer.Position, player.transform.position) > config.DistanceOfQuit)
                {
                    SendReply(player, lang.GetMessage("PlayerQuitZone", this));
                    DestroyAllUI(player);
                }
            }
        }
        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (input.WasJustPressed(BUTTON.USE))
            {
                var entity = FindTransform(new Ray(player.eyes.position, player.eyes.HeadForward()), distanceTarget);
                if (entity != null)
                {
                    if (!player.isMounted)
                        return;

                    if (!entity.name.Contains("gas_pump"))
                        return;

                    if (!_instancesFuel.ContainsKey(player.userID))
                    {
                        _instancesFuel.Add(player.userID, new InstanceFuel(entity.position));
                        FuelUI(player);
                    }
                }
            }
        }
        #endregion

        #region UI
        void FuelUI(BasePlayer player, int amount = -1, string errorMessage = "")
        {
            CuiHelper.DestroyUi(player, FuelPumpMainUI);

            if (!player.isMounted)
                return;

            BaseVehicle vehicle = player.GetMountedVehicle();
            EntityFuelSystem fuel = vehicle.GetFuelSystem();

            var elements = new CuiElementContainer();

            var blur_main = elements.Add(new CuiPanel
            {
                Image =
                    {
                        Color = "0 0 0 0.5",
                        Sprite = "assets/content/materials/highlight.png",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"
                    },

                RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                    },
                CursorEnabled = true,
            }, "Overlay", FuelPumpMainUI);

            var panel = elements.Add(GuiHelper.CreatePanel("0.5 0.5", "0.5 0.5", "0 0 0 0", cursor: true, offsetMin: "-200 -200", offsetMax: "200 230"), blur_main);

            // Header
            var header = elements.Add(GuiHelper.CreatePanel("0 0.9", "1 1", "0 0 0 0"), panel);
            GuiHelper.CreateImage(ref elements, header, "0.01 0.08", "0.11 0.98", (string)ImageLibrary.Call("GetImage", "GasPump_UI_Logo"));
            GuiHelper.CreateLabel(ref elements, "qd".ToUpper(), header, "0.14 0", "1 1", alignement: TextAnchor.MiddleLeft, fontSize: 30);

            // Close Button
            GuiHelper.CreateButton(ref elements, header, "", "fuelui.quit", "0.91 0.2", "0.97 0.8", "FuelPump.Button.UI", "0 0 0 0");
            GuiHelper.CreateImage(ref elements, "FuelPump.Button.UI", "0 0", "1 1", (string)ImageLibrary.Call("GetImage", "GasPump_Close_Logo"));

            // Content
            var content = elements.Add(GuiHelper.CreatePanel("0 0", "1 0.9", "0 0 0 0.5"), panel);

            // Info
            Dictionary<string, string> displayCard = new Dictionary<string, string>();
            displayCard.Add(lang.GetMessage("CurrentFuel", this), $"{fuel.GetFuelAmount()}L / {config.FillingLimit}L");
            displayCard.Add(lang.GetMessage("SalesRate", this), $"{config.Pricing} {config.Currency}");
            FuelUICardInfo(ref elements, player, content, displayCard);

            // Input
            string amountLabel = "";
            string secondAmountLabel = "0";
            if (amount != -1)
                amountLabel = amount.ToString();

            var input_box = elements.Add(GuiHelper.CreatePanel("0.05 0.58", "0.5 0.7", "0 0 0 0"), content);
            GuiHelper.CreateLabel(ref elements, lang.GetMessage("AmountUI", this).ToUpper(), input_box, "0 0.6", "1 1", alignement: TextAnchor.UpperLeft);

            var input = elements.Add(GuiHelper.CreatePanel("0 0", "0.95 0.67", "0 0 0 1"), input_box);
            GuiHelper.CreateLabel(ref elements, amountLabel, input, "0.01 0", "1 1", color: "1 1 1 0.4", alignement: TextAnchor.MiddleLeft);
            GuiHelper.CreateInput(ref elements, input, "", "fuel.amount.set", "0.01 0", "1 1", fontSize: 18);

            // Totality Text
            if (amount != -1)
                secondAmountLabel = (amount * config.Pricing).ToString();

            GuiHelper.CreateLabel(ref elements, string.Format("Total: {0} {1}", secondAmountLabel.ToString(), config.Currency).ToUpper(), content, "0.05 0.48", "1 0.58", alignement: TextAnchor.MiddleLeft, fontSize: 20);

            GuiHelper.CreateLabel(ref elements, string.Format(lang.GetMessage("FullTotal", this).ToUpper(), (config.FillingLimit - fuel.GetFuelAmount()) * config.Pricing, config.Currency).ToUpper(), content, "0.05 0.4", "1 0.48", alignement: TextAnchor.MiddleLeft, fontSize: 20);

            // Button
            GuiHelper.CreateButton(ref elements, content, lang.GetMessage("Filling", this).ToUpper(), $"fuel.fill {amount}", "0.05 0.28", "0.5 0.38", color: GuiHelper.HexToRGBA("#728843", 1f));
            GuiHelper.CreateButton(ref elements, content, lang.GetMessage("RefuelButton", this).ToUpper(), "fuel.refuel", "0.05 0.16", "0.5 0.26", color: GuiHelper.HexToRGBA("#680D7F", 0.5f));

            // Error Message
            GuiHelper.CreateLabel(ref elements, errorMessage, content, "0.05 0", "1 0.15", alignement: TextAnchor.MiddleLeft, color: "1 0 0 1", fontSize: 18);

            CuiHelper.AddUi(player, elements);
        }
        void FuelUICardInfo(ref CuiElementContainer elements, BasePlayer player, string parent, Dictionary<string, string> list)
        {
            var body = elements.Add(GuiHelper.CreatePanel("0.05 0.78", "0.95 0.95", "0 0 0 0"), parent);
            double min = 0.0;

            foreach (var item in list)
            {
                double max = min + ((1.0 / list.Count) - 0.15);
                var content = elements.Add(GuiHelper.CreatePanel($"{min} 0", $"{max} 1", "0 0 0 0"), body);
                GuiHelper.CreateLabel(ref elements, item.Key.ToUpper(), content, "0 0.7", "1 1", alignement: TextAnchor.MiddleLeft, fontSize: 15);

                var content_text = elements.Add(GuiHelper.CreatePanel($"0 0.3", $"1 0.7", "0 0 0 1"), content);
                GuiHelper.CreateLabel(ref elements, item.Value.ToUpper(), content_text, "0.05 0", "1 1", alignement: TextAnchor.MiddleLeft, fontSize: 15, color: "1 1 1 0.5");

                min = min + ((1.0 / list.Count) - 0.1);
            }
        }
        void ShowUI(BasePlayer player)
        {
            if (showing_UI.Contains(player.userID))
                return;

            CuiHelper.DestroyUi(player, FuelPumpUI);

            var elements = new CuiElementContainer();
            var panel = elements.Add(GuiHelper.CreatePanel("0.5 0.5", "0.5 0.5", "0 0 0 0", offsetMin: "-50 -50", offsetMax: "50 50"), "Hud", FuelPumpUI);
            GuiHelper.CreateImage(ref elements, panel, "0.2 0.4", "0.8 0.9", (string)ImageLibrary.Call("GetImage", "GasPump_Icon"));
            GuiHelper.CreateLabel(ref elements, lang.GetMessage("Filling", this).ToUpper(), panel, "0 0", "1 0.4");

            showing_UI.Add(player.userID);
            CuiHelper.AddUi(player, elements);
        }
        void HideUI(BasePlayer player)
        {
            showing_UI.Remove(player.userID);
            CuiHelper.DestroyUi(player, FuelPumpUI);
        }
        void FuelLoadingUI(BasePlayer player, double width = 0.0, double second = 0)
        {
            CuiHelper.DestroyUi(player, FuelPumpLoadingUI);
            var elements = new CuiElementContainer();

            var panel = elements.Add(GuiHelper.CreatePanel("0.5 0", "0.5 0", "0 0 0 0", offsetMin: "-200 80", offsetMax: "181 130"), "Overlay", FuelPumpLoadingUI);
            GuiHelper.CreateLabel(ref elements, $"{second}s", panel, "0 0.5", "1 1", fontSize: 20);

            var bar = elements.Add(GuiHelper.CreatePanel("0 0", "1 0.4", "1 0.96 0.88 0.15"), panel);
            var loading = elements.Add(GuiHelper.CreatePanel("0 0", $"{width} 1", GuiHelper.HexToRGBA("#FFD500", 1f)), bar);

            CuiHelper.AddUi(player, elements);
        }
        #endregion

        #region Console Command
        [ConsoleCommand("fuelui.quit")]
        private void QuiUICommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;

            DestroyAllUI(player);
        }

        [ConsoleCommand("fuel.refuel")]
        private void RefuelCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;

            if (!_instancesFuel.ContainsKey(player.userID))
                return;

            FillFuel(player);
        }

        [ConsoleCommand("fuel.fill")]
        private void FillFuelCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;

            if (!_instancesFuel.ContainsKey(player.userID))
                return;

            int fuelAmount;
            if (!int.TryParse(arg.Args[0], out fuelAmount))
                return;

            if (fuelAmount == -1)
                return;

            FillFuel(player, fuelAmount);
        }

        [ConsoleCommand("fuel.amount.set")]
        private void FuelAmountSetCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;

            if (!_instancesFuel.ContainsKey(player.userID))
                return;

            string argJoin = string.Join(" ", arg.Args);
            int amount;

            if (!int.TryParse(argJoin, out amount))
                return;

            FuelUI(player, amount);
        }
        #endregion

        #region Functions
        void FillFuel(BasePlayer player, int fuelAmount = -1)
        {
            if (!player.isMounted)
                return;

            BaseVehicle vehicle = player.GetMountedVehicle();
            EntityFuelSystem fuel = vehicle.GetFuelSystem();

            int basedFuel = fuel.GetFuelAmount();

            if (fuelAmount == -1)
                fuelAmount = config.FillingLimit - basedFuel;
            int finalFuel = basedFuel + fuelAmount;

            if (basedFuel >= config.FillingLimit)
            {
                FuelUI(player, fuelAmount, lang.GetMessage("TankLimit", this));
                return;
            }

            if (fuelAmount == 0)
            {
                FuelUI(player, fuelAmount, lang.GetMessage("AmountUnvalidate", this));
                return;
            }

            if (finalFuel > config.FillingLimit)
            {
                FuelUI(player, fuelAmount, lang.GetMessage("AmountTotalLimit", this));
                return;
            }

            int amountInventory = 0;
            int price = fuelAmount * config.Pricing;

            foreach (var item in player.inventory.FindItemsByItemID(config.PayItemID))
                amountInventory = amountInventory + item.amount;

            if (price > amountInventory)
            {
                FuelUI(player, fuelAmount, lang.GetMessage("NoMoney", this));
                return;
            }

            player.inventory.Take(new List<Item>(), config.PayItemID, price);
            player.Command("note.inv", config.PayItemID, -price);

            // If fuel exist in container -> delete the item
            Item item_fuel = fuel.GetFuelItem();
            if (item_fuel != null)
            {
                item_fuel.RemoveFromContainer();
                item_fuel.Remove(0f);
            }

            vehicle.SetFlag(BaseEntity.Flags.On, false);

            CuiHelper.DestroyUi(player, FuelPumpMainUI);

            // Timer for filling the fuel
            int repeats = config.TimeFilling * 20;
            double start = 0.0;
            double seeSeconds = Convert.ToDouble(repeats);

            InstanceFuel _ipl;
            if (!_instancesFuel.TryGetValue(player.userID, out _ipl))
                return;

            _ipl.timer = timer.Repeat(0.05f, repeats, () =>
            {
                InstanceFuel _instancePlayer;
                if (_instancesFuel.TryGetValue(player.userID, out _instancePlayer))
                {
                    if (!player.isMounted)
                    {
                        CancelTimer(player, fuel, vehicle, basedFuel, price);
                        _instancePlayer.timer.Destroy();
                        return;
                    }

                    if (Vector3.Distance(_instancePlayer.Position, player.transform.position) > config.DistanceOfQuit)
                    {
                        CancelTimer(player, fuel, vehicle, basedFuel, price);
                        _instancePlayer.timer.Destroy();
                        return;
                    }
                    else
                    {
                        if (seeSeconds <= 1.0)
                        {
                            DestroyAllUI(player);

                            fuel.AddStartingFuel(finalFuel);
                            _instancePlayer.timer.Destroy();
                            SendReply(player, lang.GetMessage("SuccessFilling", this), price, config.Currency);
                            return;
                        }
                        else
                        {
                            start += Convert.ToDouble(1 / Convert.ToDouble(repeats));
                            seeSeconds--;

                            FuelLoadingUI(player, start, seeSeconds / 20.0);
                        }
                    }
                }
            });
        }
        void CancelTimer(BasePlayer player, EntityFuelSystem fuel, BaseVehicle vehicle, int basedfuel, int price)
        {
            DestroyAllUI(player);

            fuel.AddStartingFuel(basedfuel);
            vehicle.SetFlag(BaseEntity.Flags.On, true);
            player.GiveItem(ItemManager.CreateByItemID(config.PayItemID, price));

            SendReply(player, lang.GetMessage("InterruptedFilling", this));
        }
        void DestroyAllUI(BasePlayer player)
        {
            _instancesFuel.Remove(player.userID);
            CuiHelper.DestroyUi(player, FuelPumpMainUI);
            CuiHelper.DestroyUi(player, FuelPumpLoadingUI);
        }
        static Transform FindTransform(Ray ray, float distance)
        {
            RaycastHit hit;
            return !Physics.Raycast(ray, out hit, distance) ? null : hit.GetTransform();
        }
        #endregion

        #region Localization
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Filling"] = "Fill",
                ["AmountUI"] = "Amount Fuel",
                ["CurrentFuel"] = "Current Fuel",
                ["SalesRate"] = "Sales Rate",
                ["FullTotal"] = "Full Total: {0} {1}",
                ["RefuelButton"] = "Refuel",
                ["TankLimit"] = "Your gas tank is already full",
                ["AmountUnvalidate"] = "The amount must be greater than 0L",
                ["AmountTotalLimit"] = "Your tank will be full please reduce the amount",
                ["NoScrap"] = "You don’t get enough {0}",
                ["SuccessFilling"] = "Your storeroom has been filled, and {0} {1} has been removed",
                ["PlayerQuitZone"] = "You got away from the gas pump",
                ["InterruptedFilling"] = "Filling your tank has been prevented",
            }, this);
        }
        #endregion

        #region GuiHelper
        static class GuiHelper
        {
            public static void CreateImage(ref CuiElementContainer container, string parent, string AnchorMin, string AnchorMax, string imgPng, string color = "1.0 1.0 1.0 1.0")
            {
                var image = new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = parent,
                    Components = {
                    new CuiRawImageComponent {
                        Png = imgPng,
                        Color = color
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = AnchorMin,
                        AnchorMax = AnchorMax,
                        OffsetMin = "0 0",
                        OffsetMax = "0 0"
                    }
                }
                };
                container.Add(image);
            }
            public static void CreateButton(ref CuiElementContainer container, string panel, string text, string command, string AnchorMin, string AnchorMax, string name = "", string color = "0.31 0.31 0.31 1", int size = 14, string textColor = "1 1 1 1")
            {
                container.Add(new CuiButton
                {
                    Button =
                {
                    Command = command,
                    Color = color
                },
                    RectTransform =
                {
                    AnchorMin = AnchorMin,
                    AnchorMax = AnchorMax,
                    OffsetMin = "0 0",
                    OffsetMax = "0 0"
                },
                    Text =
                {
                    Text = text,
                    FontSize = size,
                    Color = textColor,
                    Align = TextAnchor.MiddleCenter
                }
                }, panel, name);
            }
            public static void CreateLabel(ref CuiElementContainer container, string text, string panel, string AnchorMin, string AnchorMax, string color = "1 1 1 1", int fontSize = 15, TextAnchor alignement = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text =
                {
                    Text = text,
                    Align = alignement,
                    Color = color,
                    FontSize = fontSize
                },
                    RectTransform = {
                    AnchorMin = AnchorMin,
                    AnchorMax = AnchorMax,
                    OffsetMin = "0 0",
                    OffsetMax = "0 0"
                }
                }, panel, CuiHelper.GetGuid());
            }
            public static CuiPanel CreatePanel(string AnchorMin, string AnchorMax, string color = "1 1 1 1", bool cursor = false, string offsetMin = "0 0", string offsetMax = "0 0")
            {
                CuiPanel panel = new CuiPanel
                {
                    Image =
                {
                    Color = color,
                },

                    RectTransform =
                {
                    AnchorMin = AnchorMin,
                    AnchorMax = AnchorMax,
                    OffsetMin = offsetMin,
                    OffsetMax = offsetMax,
                },
                    CursorEnabled = cursor,
                };
                return panel;
            }
            public static void CreateInput(ref CuiElementContainer container, string panel, string text, string command, string AnchorMin, string AnchorMax, string color = "1 1 1 1", int fontSize = 13, int charsLimit = 40, bool password = false, string font = "robotocondensed-regular.ttf")
            {
                var input = new CuiElement
                {
                    Name = "Input",
                    Parent = panel,
                    Components =
                {
                    new CuiInputFieldComponent
                    {
                        Text = text,
                        CharsLimit = charsLimit,
                        Color = color,
                        IsPassword = password,
                        Command = command,
                        Font =font ,
                        FontSize = fontSize,
                        Align = TextAnchor.MiddleLeft
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = AnchorMin,
                        AnchorMax = AnchorMax,
                        OffsetMin = "0 0",
                        OffsetMax = "0 0"
                    }
                }
                };
                container.Add(input);
            }
            public static void CreateButtonImage(ref CuiElementContainer container, string parent, string command, string AnchorMin, string AnchorMax, string imgPng, string color = "0 0 0 0", string colorBg = "0 0 0 0")
            {
                GuiHelper.CreateButton(ref container, parent, "", command, AnchorMin, AnchorMax, "buttonImg", "0 0 0 0");
                GuiHelper.CreateImage(ref container, "buttonImg", "0 0", "1 1", imgPng);
            }
            public static string HexToRGBA(string hex, float alpha)
            {
                if (hex.StartsWith("#"))
                {
                    hex = hex.TrimStart('#');
                }

                int red = int.Parse(hex.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hex.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hex.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }
        }
        #endregion
    }
}
