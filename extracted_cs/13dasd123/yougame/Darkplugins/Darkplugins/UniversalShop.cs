// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6

using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("UniversalShop", "https://discord.gg/dNGbxafuJn", "1.1.0")]
    [Description("Universal shop for your server!")]
    class UniversalShop : RustPlugin
    {
        #region Classes
        private class ShopCore
        {
            [JsonProperty("Items and Tabs")]
            public Dictionary<string, List<ItemClass>> tabsList;
            [JsonProperty("Discount System")]
            public DiscountClass discounts;
            [JsonProperty("Interface (GUI)")]
            public GuiClass shopgui;
            [JsonProperty("ConfirmBlock (GUI)")]
            public ConfirmBlock confblock;
            [JsonProperty("MiniBlock Info")]
            public MiniBlock miniblock;
            [JsonProperty("New player start balance.")]
            public double startBalance = 100.0;
            
            public ShopCore() { }
        }

        private class ConfirmBlock
        {
            [JsonProperty("Confirm Block Enabled?")]
            public bool Enabled = true;
            [JsonProperty("Confirm Block Back color.")]
            public string MainColor = "#424242";
            [JsonProperty("Confirm Block Text color.")]
            public string TextColor = "#FFFFFF";
            [JsonProperty("Confirm Block Button color.")]
            public string ButColor = "#88b447";

            public ConfirmBlock() { }
        }

        private class MiniBlock
        {
            [JsonProperty("Minishop Block enabled?")]
            public bool EnableMiniInfo = true;
            [JsonProperty("Mini Block Back color?")]
            public string MainColor = "#88b447";
            [JsonProperty("MiniBlock Text color?")]
            public string TextColor = "#FFFFFF";
            [JsonProperty("Minishop Block AnchorMin.")]
            public string MiniBlockAnMin = "0 0";
            [JsonProperty("Minishop Block AnchorMax.")]
            public string MiniBlockAnMax = "0 0";
            [JsonProperty("Minishop Block OffsetMin?")]
            public string MiniBlockOffMin = "10 10";
            [JsonProperty("Minishop Block OffsetMax?")]
            public string MiniBlockOffMax = "200 50";

            public MiniBlock() { }
        }

        private class ItemClass
        {
            [JsonProperty("ShortName of item")] public string Name = "";
            [JsonProperty("Amount")] public int Amount = 1;
            [JsonProperty("Item price")] public double Price = 0;
            [JsonProperty("Type of money to buy (default,economic,resource(ShortName))")] public string Type = "default";
            [JsonProperty("Command to give (if you need)")] public string Command = "";
            [JsonProperty("Item image (if is empty plugin will use image from shortname)")] public string Image = "";
            [JsonProperty("Item skin (0 - default skin)")] public ulong Skin = 0;

            public ItemClass(string name = "", int amount = 1, double price = 0, string type = "default", string command = "", string image = "", ulong skin = 0)
            {
                Name = name; Amount = amount; Price = price; Type = type; Command = command; Image = image; Skin = skin;
            }
        }

        private class DiscountClass
        {
            [JsonProperty("Discount by userid")]
            public Dictionary<ulong, double> Users;
            [JsonProperty("Discount by Permission")]
            public Dictionary<string, double> Permissions;
            public DiscountClass() { }
        }

        private class GuiClass
        {

            [JsonProperty("Background Color")] public string BackColor = "#424242";
            [JsonProperty("Header Color")] public string HeaderColor = "#88b447";
            [JsonProperty("HeaderText Color")] public string HeaderTextColor = "#FFFFFF";
            [JsonProperty("TabsColor Color")] public string TabsColor = "#88b447";
            [JsonProperty("TabsText Color")] public string TabsTextColor = "#FFFFFF";
            [JsonProperty("ItemBlocks Color")] public string ItemBlockColor = "#88b447";
            [JsonProperty("ItemBlocksText Color")] public string ItemBlockTextColor = "#FFFFFF";
        }
        #endregion

        #region Variables
        [PluginReference]
        private Plugin ImageLibrary;
        [PluginReference]
        private Plugin Economics;

        string UI_Layer = "UI_UniversalShop";

        private ShopCore config;
        private Dictionary<ulong, double> usersData;
        #endregion

        #region Oxide
        private void OnServerInitialized()
        {
            PrintWarning("Start plugin UniversalShop. Author: BadMandarin.");
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("UShop/Configuration"))
                config = Interface.Oxide?.DataFileSystem?.ReadObject<ShopCore>("UShop/Configuration");
            else
            {
                config = new ShopCore()
                {
                    tabsList = new Dictionary<string, List<ItemClass>>()
                    {
                        ["Resources"] = new List<ItemClass>()
                        {
                            new ItemClass("wood", 1000, 100, "default"),
                            new ItemClass("stones", 1000, 100, "default"),
                            new ItemClass("metal.fragments", 1000, 150, "default"),
                            new ItemClass("metal.refined", 100, 200, "default"),
                            new ItemClass("scrap", 100, 50, "default")
                        },
                        ["Components"] = new List<ItemClass>()
                        {
                            new ItemClass("metalpipe", 15, 100, "default"),
                            new ItemClass("gears", 10, 150, "default"),
                            new ItemClass("rope", 20, 100, "default")
                        },
                    },
                    shopgui = new GuiClass(),
                    confblock = new ConfirmBlock(),
                    miniblock = new MiniBlock(),
                    discounts = new DiscountClass
                    {
                        Users = new Dictionary<ulong, double>()
                        {
                            [76561198869937617] = 0.1
                        },
                        Permissions = new Dictionary<string, double>()
                        {
                            ["universalshop.10"] = 0.1
                        }
                    },
                    startBalance = 100.0
                };
                Interface.Oxide.DataFileSystem.WriteObject("UShop/Configuration", config);
            }

            foreach (var perm in config.discounts.Permissions)
                permission.RegisterPermission(perm.Key, this);

            if (Interface.Oxide.DataFileSystem.ExistsDatafile("UShop/Data"))
                usersData = Interface.Oxide?.DataFileSystem?.ReadObject<Dictionary<ulong, double>>("UShop/Data");
            else
                usersData = new Dictionary<ulong, double>();

            foreach (BasePlayer player in BasePlayer.activePlayerList.ToList())
            {
                if (!usersData.ContainsKey(player.userID)) usersData.Add(player.userID, config?.startBalance ?? 0);
                if (config.miniblock.EnableMiniInfo)
                    LoadMiniBlock(player);
            }
            Interface.Oxide.DataFileSystem.WriteObject("UShop/Data", usersData);

            foreach (var defimage in config.tabsList)
            {
                foreach (var img in defimage.Value)
                {
                    if (string.IsNullOrEmpty(img.Image))
                        if (!(bool)ImageLibrary?.Call("HasImage", img.Name + 64)) ImageLibrary.Call("AddImage", $"http://api.hougan.space/rust/item/getImage/{img.Name}/128", img.Name + 64);
                    else
                        if (!(bool)ImageLibrary?.Call("HasImage", GetNameByURL(img.Image))) ImageLibrary.Call("AddImage", img.Image, GetNameByURL(img.Image));

                    if (img.Type != "default" && img.Type != "economic")
                        if (!(bool)ImageLibrary?.Call("HasImage", img.Type + 32)) ImageLibrary.Call("AddImage", $"http://api.hougan.space/rust/item/getImage/{img.Type}/32", img.Type + 32);

                    if (Economics == null && img.Type == "economic")
                    {
                        PrintError("There's no economics plugin on your server! You can't sell items using it.");
                        img.Type = "default";
                    }
                }
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                NextTick(() => OnPlayerInit(player));
                return;
            }
            if (!player.IsConnected)
            {
                return;
            }

            if (!usersData.ContainsKey(player.userID)) usersData.Add(player.userID, config?.startBalance ?? 0);

            if (config.miniblock.EnableMiniInfo)
                LoadMiniBlock(player);
        }

        private void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, UI_Layer);
                CuiHelper.DestroyUi(player, UI_Layer + ".Cursor");
                CuiHelper.DestroyUi(player, UI_Layer + ".MiniBlock");
            }
            Interface.Oxide.DataFileSystem.WriteObject("UShop/Data", usersData);
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ShopName"] = "Shop | Money: {0} | Discount: {1}% | Economics: %ECONOM%",
                ["MiniBlock"] = "Money: {0} | Discount: {1}%\nType: /shop",
                ["Error"] = "[UShop] Omg error! Tell about it admin!",
                ["NoAccess"] = "[UShop] You don't have access!",
                ["NoMoney"] = "[UShop] You don't have enough money!",
                ["Price"] = "Price: {0}",
                ["Amount"] = "x{0}",
            }, this, "en");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ShopName"] = "Магазин | Деньги: {0} | Скидка: {1}% | Экономика: %ECONOM%",
                ["MiniBlock"] = "Деньги: {0} | Скидка: {1}%\nВведите: /shop",
                ["Error"] = "[UShop] Ошибка! Сообщите администратору!",
                ["NoAccess"] = "[UShop] У вас нету доступа!",
                ["NoMoney"] = "[UShop] У вас недостаточно средств!",
                ["Price"] = "Цена: {0}",
                ["Amount"] = "x{0}",
            }, this, "ru");
        }
        #endregion

        #region Commands
        [ChatCommand("shop")]
        void CMD_Shop(BasePlayer player, string command, string[] args)
        {
            CuiHelper.DestroyUi(player, UI_Layer);
            double discount = 0;
            if (config?.discounts?.Users.ContainsKey(player.userID) ?? false)
                discount += config.discounts.Users[player.userID];
            //by perm todoconfig.shopgui.HeaderName + $" | Money: {usersData[player.userID]} | Discount: {(int)discount*100}%"
            LoadContainer(player);
        }

        [ConsoleCommand("UI_UShopToggle")]
        private void CMD_ShopToggle(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;

            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(player, lang.GetMessage("Error", this));
                return;
            }

            int tab = 0;
            if(!Int32.TryParse(arg.Args[0], out tab))
            {
                SendReply(player, lang.GetMessage("Error", this));
                return;
            }

            if(arg.Args.Length == 1)
            {
                LoadContainer(player, tab); return;
            }

            int itembuy = 0;
            if (!Int32.TryParse(arg.Args[1], out itembuy))
            {
                SendReply(player, lang.GetMessage("Error", this));
                return;
            }
            int ipage = 0;
            if (!Int32.TryParse(arg.Args[2], out ipage))
            {
                SendReply(player, lang.GetMessage("Error", this));
                return;
            }
            if (itembuy == -1)
            {
                LoadContainer(player, tab, ipage);
                return;
            }
            else
            {
                if(config.confblock.Enabled)
                    LoadConfirmBlock(player, tab, itembuy, ipage);
                else
                    BuyItem(player, tab, itembuy, ipage);
            }
        }

        [ConsoleCommand("UI_UShopConfirm")]
        private void CMD_ShopConfirm(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;

            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(player, lang.GetMessage("Error", this));
                return;
            }

            int tab = 0, itembuy = 0, ipage = 0;
            if (Int32.TryParse(arg.Args[0], out tab) && Int32.TryParse(arg.Args[1], out itembuy) && Int32.TryParse(arg.Args[2], out ipage))
            {
                BuyItem(player, tab, itembuy, ipage);
                return;
            }
            SendReply(player, lang.GetMessage("Error", this));
        }

        // shopmoney userid amount
        [ConsoleCommand("shopmoney")]
        private void CMD_ShopMoney(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin) return;
            
            if (arg.Args == null || arg.Args.Length != 2)
            {
                PrintError(lang.GetMessage("Error", this));
                return;
            }
            ulong userid = 0;
            int money = 0;
            if (!ulong.TryParse(arg.Args[0], out userid) || !Int32.TryParse(arg.Args[1], out money))
            {
                PrintError(lang.GetMessage("Error", this));
                return;
            }
            if (usersData.ContainsKey(userid))
            {
                usersData[userid] += money;
                if (usersData[userid] < 0) usersData[userid] = 0;
                return;
            }
            PrintError(lang.GetMessage("Error", this));
        }
        #endregion

        #region Utils
        public static string GetColor(string hex, float alpha = 1f)
        {
            var color = ColorTranslator.FromHtml(hex);
            var r = Convert.ToInt16(color.R) / 255f;
            var g = Convert.ToInt16(color.G) / 255f;
            var b = Convert.ToInt16(color.B) / 255f;
            //var a = Convert.ToInt16(color.A) / {DarkPluginsID}f;

            return $"{r} {g} {b} {alpha}";
        }

        private string GetNameByURL(string url)
        {
            var splitted = url.Split('/');
            var endUrl = splitted[splitted.Length - 1];
            var name = endUrl.Split('.')[0];
            return name;
        }

        public static class PermissionSystem
        {
            public static Permission perm = Interface.GetMod().GetLibrary<Permission>();

            public static bool HasPermission(ulong uid, string permissionName)
            {
                return !string.IsNullOrEmpty(permissionName) &&
                       perm.UserHasPermission(uid.ToString(), permissionName);
            }

            public static void RegisterPermissions(Plugin owner, List<string> permissions)
            {
                if (owner == null) throw new ArgumentNullException("owner");
                if (permissions == null) throw new ArgumentNullException("commands");
                foreach (var permissionName in permissions.Where(permissionName =>
                    !perm.PermissionExists(permissionName)))
                {
                    perm.RegisterPermission(permissionName, owner);
                }
            }
        }
        #endregion

        #region GuiCore
        private void LoadContainer(BasePlayer player, int page = 0, int ipage = 0)
        {
            double discount = GetUserDiscount(player);
            CuiElementContainer container = new CuiElementContainer()
            {
                {
                    new CuiPanel
                    {
                        CursorEnabled = true,
                        RectTransform =
                        {
                            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-400 -255", OffsetMax = "400 255"
                        },
                        Image = {Color = GetColor(config.shopgui.BackColor, 0.3f), Material = "assets/content/ui/uibackgroundblur.mat" }
                    }, "Overlay", UI_Layer
                },
                {
                    new CuiButton
                    {
                        RectTransform = { AnchorMin = "-100{DarkPluginsID} -100{DarkPluginsID}", AnchorMax = "100{DarkPluginsID} 100{DarkPluginsID}" },
                        Button = { Color = "0 0 0 0", Close = UI_Layer },
                        Text = { Text = "" }
                    }, UI_Layer
                },
                {
                    new CuiElement
                    {
                        Parent = UI_Layer,
                        Name = $"{UI_Layer}.Header",
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = GetColor(config.shopgui.HeaderColor, 1f), Material = "assets/content/ui/uibackgroundblur.mat"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0 1",
                                AnchorMax = $"1 1",
                                OffsetMin = $"5 -40",
                                OffsetMax = $"-5 -5"
                            },
                        }
                    }
                },
                {
                    new CuiElement
                    {
                        Parent = $"{UI_Layer}.Header",
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"{string.Format(lang.GetMessage("ShopName", this), usersData[player.userID], discount*100).Replace("%ECONOM%", Economics?.Call("Balance", player.userID).ToString())}", Color = GetColor(config.shopgui.HeaderTextColor, 1f), Align = TextAnchor.MiddleCenter
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0 0",
                                AnchorMax = $"1 1"
                            },
                            new CuiOutlineComponent { Distance = "0.5 -0.5", Color = GetColor("#000000", 1f)}
                        }
                    }
                }
            };
            int size = 40;
            for (int i = 0; i < config.tabsList.Count(); i++)
            {
                KeyValuePair<string, List<ItemClass>> tab = config.tabsList.ElementAt(i);
                container.Add(new CuiElement
                {
                    Parent = UI_Layer,
                    Name = $"{UI_Layer}.Tab.{i}",
                    Components =
                        {
                            new CuiImageComponent
                            {
                                Color = GetColor(config.shopgui.TabsColor, 1f), Material = "assets/content/ui/uibackgroundblur.mat"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0 1",
                                AnchorMax = $"0 1",
                                OffsetMin = $"5 {-80 - i*size}",
                                OffsetMax = $"135 {-45 - i*size}"
                            },
                        }
                });
                container.Add(new CuiElement
                {
                    Parent = $"{UI_Layer}.Tab.{i}",
                    Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"{tab.Key}", Color = GetColor(config.shopgui.TabsTextColor, 1f), Align = TextAnchor.MiddleCenter
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0 0",
                                AnchorMax = $"1 1"
                            },
                            new CuiOutlineComponent { Distance = "0.5 -0.5", Color = GetColor("#000000", 1f)}
                        }
                });
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Color = "0 0 0 0", Command = $"UI_UShopToggle {i}" },
                    Text = { Text = "" }
                }, $"{UI_Layer}.Tab.{i}");
                if (page == i)
                {
                    int a = 0;
                    int f = 0;
                    int s = 110;
                    int d = 110;
                    int itemsperpage = (tab.Value.Count - 24 * ipage > 24) ? 24 : (tab.Value.Count - 24 * ipage);
                    for (int r = 0; r < itemsperpage; r++)
                    {
                        ItemClass itm = tab.Value.ElementAt(r + ipage * 24);
                        container.Add(new CuiElement
                        {
                            Parent = UI_Layer,
                            Name = $"{UI_Layer}.Item.{f}.{a}",
                            Components =
                            {
                                new CuiImageComponent
                                {
                                    Color = GetColor(config.shopgui.ItemBlockColor, 1f), Material = "assets/content/ui/uibackgroundblur.mat"
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"0 1",
                                    AnchorMax = $"0 1",
                                    OffsetMin = $"{140 + f*s} {-150 - a*d}",
                                    OffsetMax = $"{245 + f*s} {-45 - a*d}"
                                }
                            }
                        });
                        string ID = "";
                        if (string.IsNullOrEmpty(itm.Image))
                        {
                            ID = (string)ImageLibrary?.Call("GetImage", itm.Name + 64);
                            if (ID == "")
                                ID = (string)ImageLibrary?.Call("GetImage", itm.Name + 64) ?? ID;
                        }
                        else
                        {
                            string name = GetNameByURL(itm.Image);
                            ID = (string)ImageLibrary?.Call("GetImage", name);
                            if (ID == "")
                                ID = (string)ImageLibrary?.Call("GetImage", name) ?? ID;
                        }
                        container.Add(new CuiElement
                        {
                            Parent = $"{UI_Layer}.Item.{f}.{a}",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = ID
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"0.15 0.15",
                                    AnchorMax = $"0.85 0.85"
                                },
                                //new CuiOutlineComponent { Distance = "0.5 -0.5", Color = GetColor("#000000", 1f)}
                            }
                        });
                        if (itm.Type != "default" && itm.Type != "economic")
                        {
                            container.Add(new CuiElement
                            {
                                Parent = $"{UI_Layer}.Item.{f}.{a}",
                                Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"{Math.Round(itm.Price - itm.Price * discount)}", Color = GetColor(config.shopgui.ItemBlockTextColor, 1f), FontSize = 12
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = $"0.45 0",
                                        AnchorMax = $"1 0.15",
                                    },
                                    new CuiOutlineComponent { Distance = "0.5 -0.5", Color = GetColor("#000000", 1f)}
                                }
                            });
                            ID = (string)ImageLibrary?.Call("GetImage", itm.Type + 32);
                            if (ID == "")
                                ID = (string)ImageLibrary?.Call("GetImage", itm.Type + 32) ?? ID;
                            container.Add(new CuiElement
                            {
                                Parent = $"{UI_Layer}.Item.{f}.{a}",
                                Components =
                                {
                                    new CuiRawImageComponent
                                    {
                                        Png = ID
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = $"0.3 0",
                                        AnchorMax = $"0.45 0.15",
                                    },
                                    //new CuiOutlineComponent { Distance = "0.5 -0.5", Color = GetColor("#000000", 1f)}
                                }
                            });
                        }
                        else
                        {
                            container.Add(new CuiElement
                            {
                                Parent = $"{UI_Layer}.Item.{f}.{a}",
                                Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"{string.Format(itm.Type=="economic"?"Economics: {0}":lang.GetMessage("Price", this), Math.Round(itm.Price - itm.Price * discount, 1))}", Color = GetColor(config.shopgui.ItemBlockTextColor, 1f), FontSize = 12, Align = TextAnchor.LowerCenter
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = $"0 0",
                                        AnchorMax = $"1 1",
                                    },
                                    new CuiOutlineComponent { Distance = "0.5 -0.5", Color = GetColor("#000000", 1f)}
                                }
                            });
                        }

                        container.Add(new CuiElement
                        {
                            Parent = $"{UI_Layer}.Item.{f}.{a}",
                            Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"x{itm.Amount}", Color = GetColor(config.shopgui.ItemBlockTextColor, 1f), FontSize = 12, Align = TextAnchor.UpperCenter
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = $"0 0",
                                        AnchorMax = $"1 1",
                                    },
                                    new CuiOutlineComponent { Distance = "0.5 -0.5", Color = GetColor("#000000", 1f)}
                                }
                        });

                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                            Button = { Color = "0 0 0 0", Command = $"UI_UShopToggle {page} {r + ipage * 24} {ipage}" },
                            Text = { Text = "" }
                        }, $"{UI_Layer}.Item.{f}.{a}");
                        f++;
                        if (f >= 6)
                        {
                            a++; f = 0;
                        }
                    }
                    if(tab.Value.Count > 20)
                    {
                        if(ipage > 0)
                        {
                            container.Add(new CuiElement
                            {
                                Parent = UI_Layer,
                                Name = $"{UI_Layer}.Switcher.Back",
                                Components =
                                {
                                    new CuiImageComponent
                                    {
                                        Color = GetColor(config.shopgui.ItemBlockColor, 1f), Material = "assets/content/ui/uibackgroundblur.mat"
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = $"0 0",
                                        AnchorMax = $"0 0",
                                        OffsetMin = "135 5",
                                        OffsetMax = "195 25"
                                    }
                                }
                            });

                            container.Add(new CuiElement
                            {
                                Parent = $"{UI_Layer}.Switcher.Back",
                                Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"Back", Color = GetColor(config.shopgui.ItemBlockTextColor, 1f), FontSize = 12, Align = TextAnchor.MiddleCenter
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = $"0 0",
                                        AnchorMax = $"1 1",
                                    },
                                    new CuiOutlineComponent { Distance = "0.5 -0.5", Color = GetColor("#000000", 1f)}
                                }
                            });
                            container.Add(new CuiButton
                            {
                                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                                Button = { Color = "0 0 0 0", Command = $"UI_UShopToggle {page} -1 {ipage-1}" },
                                Text = { Text = "" }
                            }, $"{UI_Layer}.Switcher.Back");
                        }
                        if (itemsperpage >= 24)
                        {
                            container.Add(new CuiElement
                            {
                                Parent = UI_Layer,
                                Name = $"{UI_Layer}.Switcher.Next",
                                Components =
                                {
                                    new CuiImageComponent
                                    {
                                        Color = GetColor(config.shopgui.ItemBlockColor, 1f), Material = "assets/content/ui/uibackgroundblur.mat"
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = $"1 0",
                                        AnchorMax = $"1 0",
                                        OffsetMin = "-65 5",
                                        OffsetMax = "-5 25"
                                    }
                                }
                            });
                            container.Add(new CuiElement
                            {
                                Parent = $"{UI_Layer}.Switcher.Next",
                                Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"Next", Color = GetColor(config.shopgui.ItemBlockTextColor, 1f), FontSize = 12, Align = TextAnchor.MiddleCenter
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = $"0 0",
                                        AnchorMax = $"1 1",
                                    },
                                    new CuiOutlineComponent { Distance = "0.5 -0.5", Color = GetColor("#000000", 1f)}
                                }
                            });
                            container.Add(new CuiButton
                            {
                                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                                Button = { Color = "0 0 0 0", Command = $"UI_UShopToggle {page} -1 {ipage + 1}" },
                                Text = { Text = "" }
                            }, $"{UI_Layer}.Switcher.Next");
                        }
                    }
                }
            }
            CuiHelper.DestroyUi(player, UI_Layer);
            CuiHelper.AddUi(player, container);
        }
        private void LoadMiniBlock(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer()
            {
                {
                    new CuiPanel
                    {
                        CursorEnabled = false,
                        RectTransform =
                        {
                            AnchorMin = config.miniblock.MiniBlockAnMin, AnchorMax = config.miniblock.MiniBlockAnMax, OffsetMin = config.miniblock.MiniBlockOffMin, OffsetMax = config.miniblock.MiniBlockOffMax
                        },
                        Image = {Color = GetColor(config.miniblock.MainColor, 0.3f), Material = "assets/content/ui/uibackgroundblur.mat" }
                    }, "Overlay", UI_Layer + ".MiniBlock"
                },
                {
                    new CuiElement
                    {
                        Parent = UI_Layer + ".MiniBlock",
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"{string.Format(lang.GetMessage("MiniBlock", this), usersData[player.userID], GetUserDiscount(player)*100).Replace("%ECONOM%", Economics?.Call("Balance", player.userID).ToString())}", Color = GetColor(config.miniblock.TextColor, 1f), Align = TextAnchor.MiddleCenter
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0 0",
                                AnchorMax = $"1 1"
                            },
                            new CuiOutlineComponent { Distance = "0.5 -0.5", Color = GetColor("#000000", 1f)}
                        }
                    }
                }
            };

            CuiHelper.DestroyUi(player, UI_Layer + ".MiniBlock");
            CuiHelper.AddUi(player, container);

        }
        private void LoadConfirmBlock(BasePlayer player, int tab, int itembuy, int ipage)
        {
            ItemClass itm = config.tabsList.ElementAt(tab).Value.ElementAt(itembuy);
            Item item = ItemManager.CreateByName(itm.Name);
            string ID = "";
            if (string.IsNullOrEmpty(itm.Image))
            {
                ID = (string)ImageLibrary?.Call("GetImage", itm.Name + 64);
                if (ID == "")
                    ID = (string)ImageLibrary?.Call("GetImage", itm.Name + 64) ?? ID;
            }
            else
            {
                string name = GetNameByURL(itm.Image);
                ID = (string)ImageLibrary?.Call("GetImage", name);
                if (ID == "")
                    ID = (string)ImageLibrary?.Call("GetImage", name) ?? ID;
            }
            CuiElementContainer container = new CuiElementContainer()
            {
                {
                    new CuiPanel
                    {
                        CursorEnabled = false,
                        RectTransform =
                        {
                            AnchorMin = "0 0", AnchorMax = "1 1"
                        },
                        Image = {Color = GetColor("#000000", 0.9f) }
                    }, UI_Layer, UI_Layer + ".ConfBlockBack"
                },
                {
                    new CuiButton
                    {
                        RectTransform = { AnchorMin = "-100 -100", AnchorMax = "100 100" },
                        Button = { Color = "0 0 0 0", Close =  UI_Layer + ".ConfBlockBack" },
                        Text = { Text = "" }
                    }, UI_Layer + ".ConfBlockBack"
                },
                {
                    new CuiElement
                    {
                        Parent = UI_Layer + ".ConfBlockBack",
                        Name = UI_Layer + ".ConfBlock",
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = GetColor(config.confblock.MainColor, 0.3f), Material = "assets/content/ui/uibackgroundblur.mat"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-120 -60", OffsetMax = "120 60"
                            }
                        }
                    }
                },
                {
                    new CuiElement
                    {
                        Parent = UI_Layer + ".ConfBlock",
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"Confirm buying this item", Color = GetColor(config.confblock.TextColor, 1f), Align = TextAnchor.UpperCenter
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0 0",
                                AnchorMax = $"1 1"
                            },
                            new CuiOutlineComponent { Distance = "0.5 -0.5", Color = GetColor("#000000", 1f)}
                        }
                    }
                },
                {
                    new CuiElement
                    {
                        Parent = UI_Layer + ".ConfBlock",
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"\n\nName: {item.info.displayName.english}\nAmount: {itm.Amount}\nPrice: {Math.Round(itm.Price - itm.Price * GetUserDiscount(player), 1)}", Color = GetColor(config.confblock.TextColor, 1f), Align = TextAnchor.UpperCenter
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0 0",
                                AnchorMax = $"1 1"
                            },
                            new CuiOutlineComponent { Distance = "0.5 -0.5", Color = GetColor("#000000", 1f)}
                        }
                    }
                },
                {
                    new CuiElement
                    {
                        Parent = UI_Layer + ".ConfBlock",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = ID
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0 0.5",
                                AnchorMax = $"0 0.5",
                                OffsetMin = "5 -25",
                                OffsetMax = "55 25"

                            },
                            new CuiOutlineComponent { Distance = "0.5 -0.5", Color = GetColor("#000000", 1f)}
                        }
                    }
                },
                {
                    new CuiElement
                    {
                        Parent = UI_Layer + ".ConfBlock",
                        Name = UI_Layer + ".ConfBlock" + ".ConfBut",
                        Components =
                        {
                            new CuiImageComponent
                            {
                                 Color = GetColor(config.confblock.ButColor, 1f), Material = "assets/content/ui/uibackgroundblur.mat"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0.5 0",
                                AnchorMax = $"0.5 0",
                                OffsetMin = "-25 5",
                                OffsetMax = "25 30"
                            },
                            new CuiOutlineComponent { Distance = "0.5 -0.5", Color = GetColor("#000000", 1f)}
                        }
                    }
                },
                {
                    new CuiElement
                    {
                        Parent = UI_Layer + ".ConfBlock" + ".ConfBut",
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"Buy", Color = GetColor(config.confblock.TextColor, 1f), Align = TextAnchor.MiddleCenter
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0 0",
                                AnchorMax = $"1 1"
                            },
                            new CuiOutlineComponent { Distance = "0.5 -0.5", Color = GetColor("#000000", 1f)}
                        }
                    }
                },
                {
                    new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Button = { Color = "0 0 0 0", Command = $"UI_UShopConfirm {tab} {itembuy} {ipage}" },
                        Text = { Text = "" }
                    }, UI_Layer + ".ConfBlock" + ".ConfBut"
                },
            };

            CuiHelper.DestroyUi(player, UI_Layer + ".ConfBlockBack");
            CuiHelper.AddUi(player, container);

        }
        #endregion

        #region OtherFunc
        private void BuyItem(BasePlayer player, int page, int itemid, int ipage)
        {
            ItemClass item = config?.tabsList?.ElementAt(page).Value?.ElementAt(itemid) ?? null;
            double price = Math.Round(item.Price - item.Price * GetUserDiscount(player), 1);
            if (item != null)
            {
                switch (item.Type)
                {
                    case "economic":
                        if((double)Economics?.Call("Balance", player.userID) >= price)
                        {
                            if((bool)Economics?.Call("Withdraw", player.userID, price))
                            {
                                GiveItem(player, item);
                                LoadContainer(player, page, ipage);
                            }
                            else SendReply(player, lang.GetMessage("NoMoney", this));
                        }
                        else SendReply(player, lang.GetMessage("NoMoney", this));
                        break;
                    case "default":
                        if(usersData[player.userID] >= price)
                        {
                            usersData[player.userID] -= price;
                            GiveItem(player, item);
                            LoadContainer(player, page, ipage);
                        }
                        else SendReply(player, lang.GetMessage("NoMoney", this));
                        break;
                    default:
                        ItemDefinition def = ItemManager.FindItemDefinition(item.Type);
                        if(player.inventory.containerMain.GetAmount(def.itemid, true) >= price)
                        {
                            var resource = player.inventory.containerMain.itemList.FindAll(x => x.info.shortname == item.Type);
                            player.inventory.containerMain.Take(resource, def.itemid, (int)price);
                            GiveItem(player, item);
                            LoadContainer(player, page, ipage);
                        }
                        else SendReply(player, lang.GetMessage("NoMoney", this));
                        break;
                }
            }else SendReply(player, lang.GetMessage("Error", this));
        }

        private void GiveItem(BasePlayer player, ItemClass item)
        {
            if (!string.IsNullOrEmpty(item.Command))
                Server.Command(item.Command.Replace("%STEAMID%", player.UserIDString));
            else
            {
                Item i = ItemManager.CreateByName(item.Name, item.Amount, item.Skin);
                player.GiveItem(i, BaseEntity.GiveItemReason.PickedUp);
            }
            if (config.miniblock.EnableMiniInfo)
                LoadMiniBlock(player);
        }

        private double GetUserDiscount(BasePlayer player)
        {
            double d = 0;

            if (config.discounts.Users.ContainsKey(player.userID))
                d += config.discounts.Users[player.userID];

            double high = 0;
            foreach(var perm in config.discounts.Permissions)
            {
                if (permission.UserHasPermission(player.UserIDString, perm.Key))
                    if (high < perm.Value)
                        high = perm.Value;
            }

            d += high;

            return d;
        }
        #endregion

        #region API
        private double API_ShopGetBalance(ulong userid)
        {
            return usersData[userid];
        }
        private void API_ShopSetBalance(ulong userid, int balance)
        {
            usersData[userid] = balance;
            LoadMiniBlock(BasePlayer.FindByID(userid));
        }
        private void API_ShopAddBalance(ulong userid, int balance)
        {
            usersData[userid] += balance;
            LoadMiniBlock(BasePlayer.FindByID(userid));
        }
        private void API_ShopRemBalance(ulong userid, int balance)
        {
            if (usersData[userid] >= balance) usersData[userid] -= balance;
            else usersData[userid] = 0;

            LoadMiniBlock(BasePlayer.FindByID(userid));
        }
        private void API_ShopSetBalance(ulong userid, double balance)
        {
            usersData[userid] = balance;
            LoadMiniBlock(BasePlayer.FindByID(userid));
        }
        private void API_ShopAddBalance(ulong userid, double balance)
        {
            usersData[userid] += balance;
            LoadMiniBlock(BasePlayer.FindByID(userid));
        }
        private void API_ShopRemBalance(ulong userid, double balance)
        {
            if (usersData[userid] >= balance) usersData[userid] -= balance;
            else usersData[userid] = 0;

            LoadMiniBlock(BasePlayer.FindByID(userid));
        }
        #endregion

    }
}
