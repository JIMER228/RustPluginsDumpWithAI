// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6

using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("UniversalShop", "BadMandarin", "1.0.3")]
      //  Слив плагинов server-rust by Apolo YouGame
    [Description("Универсальный магазин для вашего сервера!")]
    class UniversalShop : RustPlugin
    {
        #region Classes
        private class ShopSettings
        {
            [JsonProperty("Список страниц в магазине")]
            public Dictionary<string, List<ItemsInfo>> Tabs = new Dictionary<string, List<ItemsInfo>>();
      //  Слив плагинов server-rust by Apolo YouGame
            
            [JsonProperty("Стартовый баланс у игрока")]
            public int Shop_PlayerBalance = 1000;
            
            [JsonProperty("Название валюты")]
            public string Shop_MoneyName;
            [JsonProperty("Тип валюты (если хотите исп. ресурсы из игры впишите их shortname если нет оставьте поле пустым)")]
            public string Shop_MoneyType;


        }

        private class GuiSettings
        {
            [JsonProperty("Главный цвет магазина")]
            public string Gui_MainColor;
            [JsonProperty("Цвет текста в магазине")]
            public string Gui_MainTextColor;
            [JsonProperty("Цвет обводки текста в магазине")]
            public string Gui_MainTextOutLineColor;
            [JsonProperty("Название магазина")]
            public string Gui_ShopName;

            [JsonProperty("Цвет заднего фона")]
            public string Gui_BackColor;
            [JsonProperty("Прозрачность заднего фона")]
            public float Gui_BackAlpha;
            [JsonProperty("Эффект заднего фона (Подробней на странице https://darkplugins.ru/resources/universal-shop.76/field?field=5154)")]
            public string Gui_BackMaterial;

            [JsonProperty("Цвет фона предметов")]
            public string Gui_ItemsBackColor;
            [JsonProperty("Прозрачность фона предметов")]
            public float Gui_ItemsBackAlpha;
            [JsonProperty("Эффект фона предметов (Подробней на странице https://darkplugins.ru/resources/universal-shop.76/field?field=5154)")]
            public string Gui_ItemsBackMaterial;

            [JsonProperty("Местоположение ценника (Подробней на странице https://darkplugins.ru/resources/universal-shop.76/field?field=5154)")]
            public TextAnchor Gui_PriceAnchor;
            [JsonProperty("Местоположение кол-ва предметов  (Подробней на странице https://darkplugins.ru/resources/universal-shop.76/field?field=5154)")]
            public TextAnchor Gui_AmountAnchor;
            [JsonProperty("Размер Шрифта ценника")]
            public int Gui_FontSizeAmount;
            [JsonProperty("Размер Шрифта кол-во предметов")]
            public int Gui_FontSizePrice;
            [JsonProperty("Шрифт ценника")]
            public string Gui_FontAmount;
            [JsonProperty("Шрифт кол-во предметов")]
            public string Gui_FontPrice;
            [JsonProperty("Текст на информационной странице")]
            public string Gui_InfoText;
      //  Слив плагинов server-rust by Apolo YouGame
            [JsonProperty("Размер текста на информационной странице")]
            public int Gui_InfoTextSize;
      //  Слив плагинов server-rust by Apolo YouGame
        }

        private class ItemsInfo
      //  Слив плагинов server-rust by Apolo YouGame
        {
            [JsonProperty("ShortName предмета")]
            public string ShortName;
            [JsonProperty("Количество")]
            public int Amount;
            [JsonProperty("Цена предмета")]
            public int Price;
            [JsonProperty("Картинка предмета (оставьте поле пустым для обычного предмета)")]
            public string ItemImage;
            [JsonProperty("Уникальное название для картинки(Придумайте сами)")]
            public string ItemImageName;
            [JsonProperty("Команда для выдачи (оставьте поле пустым для обычного предмета)")]
            public string ItemCommand;
        }
        #endregion

        #region Variables

        [PluginReference]
        private Plugin ImageLibrary;
        

        [JsonProperty("Настройки магазина")]
        private ShopSettings shopSettings = new ShopSettings();
        
        [JsonProperty("Настройка GUI магазина")]
        private GuiSettings shopGuiSettings = new GuiSettings();

        [JsonProperty("Баланс игроков")]
        private Dictionary<ulong, int> playerBalance = new Dictionary<ulong, int>();

        private string UI_Layer = "BM_UShop";

        #endregion

        #region Oxide
        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (player == null) return;

            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                RectTransform = { AnchorMin = "0.001 0.89", AnchorMax = "0.001 0.89", OffsetMin = "0 -30", OffsetMax = "30 0" },

                Image = { Color = GetColor("#000000", 0f)}
            }, "Overlay", UI_Layer + ".Icon");

            string ID = (string)ImageLibrary?.Call("GetImage", "ShopIcon");
            if (ID == "")
                ID = (string)ImageLibrary?.Call("GetImage", "ShopIcon") ?? ID;

            container.Add(new CuiElement
            {
                Parent = UI_Layer + ".Icon",
                Components =
                    {
                        new CuiRawImageComponent { Png = ID },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0 0",
                            AnchorMax = $"1 1"
                        },
                    }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Color = "0 0 0 0", Command = "chat.say /shop" },
                Text = { Text = "" }
            }, UI_Layer + ".Icon");

            CuiHelper.DestroyUi(player, UI_Layer + $".Icon");
            CuiHelper.AddUi(player, container);
        }
        private void OnServerInitialized()
        {
                if (Interface.Oxide.DataFileSystem.ExistsDatafile("BM_UShop/ShopSettings"))
                    shopSettings = Interface.Oxide.DataFileSystem.ReadObject<ShopSettings>("BM_UShop/ShopSettings");
                else
                {
                    shopSettings = new ShopSettings
                    {
                        Tabs = new Dictionary<string, List<ItemsInfo>>
      //  Слив плагинов server-rust by Apolo YouGame
                        {
                            ["Ресурсы"] = new List<ItemsInfo>
      //  Слив плагинов server-rust by Apolo YouGame
                            {
                                new ItemsInfo { ShortName = "wood", Amount = 10000, Price = 100, ItemImage = "", ItemCommand = "", ItemImageName = "" },
      //  Слив плагинов server-rust by Apolo YouGame
                                new ItemsInfo { ShortName = "stones", Amount = 7000, Price = 150, ItemImage = "", ItemCommand = "", ItemImageName = "" },
      //  Слив плагинов server-rust by Apolo YouGame
                                new ItemsInfo { ShortName = "metal.fragments", Amount = 5000, Price = 250, ItemImage = "", ItemCommand = "", ItemImageName = "" },
      //  Слив плагинов server-rust by Apolo YouGame
                            },
                            ["Компоненты"] = new List<ItemsInfo>
      //  Слив плагинов server-rust by Apolo YouGame
                            {
                                new ItemsInfo { ShortName = "tarp", Amount = 10, Price = 100, ItemImage = "", ItemCommand = "", ItemImageName = "" },
      //  Слив плагинов server-rust by Apolo YouGame
                                new ItemsInfo { ShortName = "gears", Amount = 10, Price = 250, ItemImage = "", ItemCommand = "", ItemImageName = "" },
      //  Слив плагинов server-rust by Apolo YouGame
                                new ItemsInfo { ShortName = "rope", Amount = 25, Price = 150, ItemImage = "", ItemCommand = "", ItemImageName = "" },
      //  Слив плагинов server-rust by Apolo YouGame
                            }
                        },
                        Shop_PlayerBalance = 1000,
                        Shop_MoneyName = "руб",
                        Shop_MoneyType = ""
                    };
                    Interface.Oxide.DataFileSystem.WriteObject("BM_UShop/ShopSettings", shopSettings);
                    PrintWarning("Конфигурация для плагина Universal Shop успешно создана.");
                }

            if (Interface.Oxide.DataFileSystem.ExistsDatafile("BM_UShop/GUISettings"))
                shopGuiSettings = Interface.Oxide.DataFileSystem.ReadObject<GuiSettings>("BM_UShop/GUISettings");
            else
            {
                shopGuiSettings = new GuiSettings
                {
                    Gui_BackColor = "#606060",
                    Gui_BackAlpha = 0.9f,
                    Gui_BackMaterial = "assets/content/ui/uibackgroundblur.mat",

                    Gui_ItemsBackColor = "#999999",
                    Gui_ItemsBackAlpha = 1f,
                    Gui_ItemsBackMaterial = "assets/content/ui/uibackgroundblur.mat",

                    Gui_PriceAnchor = TextAnchor.LowerLeft,
                    Gui_AmountAnchor = TextAnchor.LowerRight,
                    Gui_FontSizeAmount = 12,
                    Gui_FontSizePrice = 12,
                    Gui_FontAmount = "robotocondensed-regular.ttf",
                    Gui_FontPrice = "robotocondensed-regular.ttf",

                    Gui_MainColor = "#999999",
                    Gui_MainTextColor = "White",
                    Gui_MainTextOutLineColor = "",
                    Gui_ShopName = "МИНИ-МАГАЗИН",
                    Gui_InfoText = $"<color=#5F04B4>МИНИ-МАГАЗИН</color>\nЗдесь вы можете купить ценные вещи которые помогут вам в развитии!",
      //  Слив плагинов server-rust by Apolo YouGame
                    Gui_InfoTextSize = 24,
      //  Слив плагинов server-rust by Apolo YouGame
                };
                Interface.Oxide.DataFileSystem.WriteObject("BM_UShop/GUISettings", shopGuiSettings);
                PrintWarning("Конфигурация для плагина Universal Shop успешно создана.");
            }
                if (Interface.Oxide.DataFileSystem.ExistsDatafile("BM_UShop/Player_Data"))
                playerBalance = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, int>>("BM_UShop/Player_Data");
            
            foreach(var player in BasePlayer.activePlayerList)
            {
                if (shopSettings.Shop_MoneyType.Length > 0)
                {
                    if (!playerBalance.ContainsKey(player.userID))
                    {
                        playerBalance.Add(player.userID, 0);
                    }
                    continue;
                }

                if (!playerBalance.ContainsKey(player.userID))
                {
                    playerBalance.Add(player.userID, shopSettings.Shop_PlayerBalance);
                }
            }

            Interface.Oxide.DataFileSystem.WriteObject("BM_UShop/Player_Data", playerBalance);
            bool exists = false;
            foreach (var tb in shopSettings.Tabs)
            {
                foreach(var itm in tb.Value)
                {
                    if(itm.ItemImage.Length > 0)
                    {
                        exists = (bool)ImageLibrary.Call("HasImage", itm.ItemImageName);
                        if (exists == false)
                        {
                            ImageLibrary?.Call("AddImage", itm.ItemImage, itm.ItemImageName);
                        }
                    }
                }
            }

            exists = (bool)ImageLibrary.Call("HasImage", "ShopIcon");
            if (exists == false)
            {
                ImageLibrary?.Call("AddImage", "https://gamestores.pictures/images/2019/02/13/MA4ca.png", "ShopIcon");
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (shopSettings.Shop_MoneyType.Length > 0)
            {
                if (!playerBalance.ContainsKey(player.userID))
                {
                    playerBalance.Add(player.userID, 0);
                }
                return;
            }

            if (!playerBalance.ContainsKey(player.userID))
            {
                playerBalance.Add(player.userID, shopSettings.Shop_PlayerBalance);
                Interface.Oxide.DataFileSystem.WriteObject("BM_UShop/Player_Data", playerBalance);
            }
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (shopSettings.Shop_MoneyType.Length > 0)
            {
                if (playerBalance.ContainsKey(player.userID))
                {
                    playerBalance.Remove(player.userID);
                }
                return;
            }
            Interface.Oxide.DataFileSystem.WriteObject("BM_UShop/Player_Data", playerBalance);
        }

        private void Unload()
        {
            if (shopSettings.Shop_MoneyType.Length > 0)
            {
                return;
            }
            Interface.Oxide.DataFileSystem.WriteObject("BM_UShop/Player_Data", playerBalance);
            PrintWarning("Данные сохранены.");
            foreach(var player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, UI_Layer + $".Icon");
        }
        #endregion

        #region Interface
        private void Draw_UIShopMain(BasePlayer player, int tab = 0, int page = 0, bool infopage = false)
        {
            if (player == null)
            {
                return;
            }

            CuiElementContainer container = new CuiElementContainer();
            if (shopSettings.Shop_MoneyType.Length > 0)
            {
                playerBalance[player.userID] = 0;
                foreach (var i in player.inventory.containerMain.itemList)
                {
                    if (i.info.shortname == shopSettings.Shop_MoneyType)
                        playerBalance[player.userID] += i.amount;
                }
                //Item item = player.inventory.containerMain.FindItemsByItemName(shopSettings.Shop_MoneyType);
                //playerBalance[player.userID] = item.amount;
            }
            

            #region Gui_Main
            if (shopGuiSettings.Gui_BackMaterial.Length > 0) { 
                container.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-400 -250", OffsetMax = "400 250" },

                    Image = { Color = GetColor(shopGuiSettings.Gui_BackColor, shopGuiSettings.Gui_BackAlpha), Material = shopGuiSettings.Gui_BackMaterial }
                }, "Hud", UI_Layer);
            }
            else
            {
                container.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-400 -300", OffsetMax = "400 300" },

                    Image = { Color = GetColor(shopGuiSettings.Gui_BackColor, shopGuiSettings.Gui_BackAlpha) }
                }, "Hud", UI_Layer);
            }

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "-100 -100", AnchorMax = "100 100" },
                Button = { Color = "0 0 0 0", Close = UI_Layer },
                Text = { Text = "" }
            }, UI_Layer);

            container.Add(new CuiElement
            {
                Parent = UI_Layer,
                Name = UI_Layer + ".Header",
                Components =
                {
                    new CuiImageComponent { Color = GetColor("#47484a") },
                    new CuiRectTransformComponent { AnchorMin = "0.01 0.92", AnchorMax = "0.9 0.98" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = UI_Layer + ".Header",
                Components =
                {
                    new CuiTextComponent { Text = $"<color={shopGuiSettings.Gui_MainTextColor}>{shopGuiSettings.Gui_ShopName} | У вас: {playerBalance[player.userID]}{shopSettings.Shop_MoneyName}</color>", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf"},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                    new CuiOutlineComponent { Distance = "0.155 0.155", Color = GetColor(shopGuiSettings.Gui_MainTextOutLineColor)}
                }
            });
            container.Add(new CuiElement
            {
                Parent = UI_Layer,
                Name = UI_Layer + ".Exit",
                Components =
                {
                    new CuiImageComponent { Color = GetColor("#738d45") },
                    new CuiRectTransformComponent { AnchorMin = "0.914 0.92", AnchorMax = "0.99 0.98" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = UI_Layer + ".Exit",
                Components =
                {
                    new CuiTextComponent { Text = $"<color={shopGuiSettings.Gui_MainTextColor}>Выход</color>", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf"},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                    new CuiOutlineComponent { Distance = "0.155 0.155", Color = GetColor(shopGuiSettings.Gui_MainTextOutLineColor)}
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Color = "0 0 0 0", Close = UI_Layer },
                Text = { Text = "" }
            }, UI_Layer + ".Exit");

            if (shopGuiSettings.Gui_ItemsBackMaterial.Length > 0)
            {
                container.Add(new CuiElement
                {
                    Parent = UI_Layer,
                    Name = UI_Layer + ".MainItems",
                    Components =
                {
                    new CuiImageComponent { Color = GetColor(shopGuiSettings.Gui_ItemsBackColor, shopGuiSettings.Gui_ItemsBackAlpha), Material = shopGuiSettings.Gui_ItemsBackMaterial },
                    new CuiRectTransformComponent { AnchorMin = "0.15 0.015", AnchorMax = "0.99 0.90" },
                }
                });
            }
            else
            {
                container.Add(new CuiElement
                {
                    Parent = UI_Layer,
                    Name = UI_Layer + ".MainItems",
                    Components =
                {
                    new CuiImageComponent { Color = GetColor(shopGuiSettings.Gui_ItemsBackColor, shopGuiSettings.Gui_ItemsBackAlpha) },
                    new CuiRectTransformComponent { AnchorMin = "0.15 0.015", AnchorMax = "0.99 0.90" },
                }
                });
            }

            #endregion
            
            int counter = 0;
            foreach(var t in shopSettings.Tabs)
            {
                if (counter > 9) continue;
                container.Add(new CuiElement
                {
                    Parent = UI_Layer, 
                    Name = UI_Layer + $".Tab_{counter}",
                    Components =
                    {
                        new CuiImageComponent { Color = GetColor(shopGuiSettings.Gui_MainColor, 1f), Material = "assets/content/ui/uibackgroundblur.mat" },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0.01 {0.831 - counter*0.08}",
                            AnchorMax = $"0.01 {0.831 - counter*0.08}",
                            OffsetMin = "0 0",
                            OffsetMax = (counter==tab && !infopage)?"115 35":"100 35"
                        },
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = UI_Layer + $".Tab_{counter}",
                    Components =
                        {
                            new CuiTextComponent { Text = $"<color={shopGuiSettings.Gui_MainTextColor}>{t.Key}</color>", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-bold.ttf"},
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                            new CuiOutlineComponent { Distance = "0.155 0.155", Color = GetColor(shopGuiSettings.Gui_MainTextOutLineColor)}
                        }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Color = "0 0 0 0", Command = $"UI_UpdateShop {counter} {page}" },
                    Text = { Text = "" }
                }, UI_Layer + $".Tab_{counter}");
                
                counter++;
            }

            #region InfoButton
      //  Слив плагинов server-rust by Apolo YouGame
            if (shopGuiSettings.Gui_InfoText.Length > 0)
      //  Слив плагинов server-rust by Apolo YouGame
            {
                container.Add(new CuiElement
                {
                    Parent = UI_Layer,
                    Name = UI_Layer + $".Tab_{counter}",
                    Components =
                    {
                        new CuiImageComponent { Color = GetColor("#718847", 1f), Material = "assets/content/ui/uibackgroundblur.mat" },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0.01 {0.831 - counter*0.08}",
                            AnchorMax = $"0.01 {0.831 - counter*0.08}",
                            OffsetMin = "0 0",
                            OffsetMax = infopage?"112 35":"100 35"
                        },
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = UI_Layer + $".Tab_{counter}",
                    Components =
                        {
                            new CuiTextComponent { Text = $"<color={shopGuiSettings.Gui_MainTextColor}>Информация</color>", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-bold.ttf"},
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                            new CuiOutlineComponent { Distance = "0.155 0.155", Color = GetColor(shopGuiSettings.Gui_MainTextOutLineColor)}
                        }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Color = "0 0 0 0", Command = $"UI_OpenInfoPage" },
      //  Слив плагинов server-rust by Apolo YouGame
                    Text = { Text = "" }
                }, UI_Layer + $".Tab_{counter}");

                if (infopage)
                {
                    container.Add(new CuiElement
                    {
                        Parent = UI_Layer + ".MainItems",
                        Components =
                    {
                        new CuiTextComponent
                        {
                            Text = shopGuiSettings.Gui_InfoText,
      //  Слив плагинов server-rust by Apolo YouGame
                            Align = TextAnchor.UpperCenter,
                            FontSize = shopGuiSettings.Gui_InfoTextSize,
      //  Слив плагинов server-rust by Apolo YouGame
                            Font = "robotocondensed-regular.ttf"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0.1 0.01",
                            AnchorMax = $"0.9 0.99"
                        },
                    }
                    });

                    CuiHelper.DestroyUi(player, UI_Layer);
                    CuiHelper.AddUi(player, container);
                    return;
                }
            }
            #endregion

            int itemsonpage = 24;
            counter = 0;
            int a = 0, b = 0;
            int allitemscount = shopSettings.Tabs.ElementAt(tab).Value.Count;
            int count = (allitemscount - 24 * page > 24) ? 24 : (allitemscount - 24 * page);
            for (var i = 0; i < count ; i++)
            {
                container.Add(new CuiElement
                {
                    Parent = UI_Layer + ".MainItems",
                    Name = UI_Layer + ".MainItems" + $".Item_{counter}",
                    Components =
                    {
                        new CuiImageComponent { Color = GetColor("#47484a", 0.8f) },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.02 + a*0.162} {0.975 - b*0.23}",
                            AnchorMax = $"{0.02 + a*0.162} {0.975 - b*0.23}",
                            OffsetMin = "0 -100",
                            OffsetMax = "100 0"
                        },
                    }
                });
                string ID = "";

                if(shopSettings.Tabs.ElementAt(tab).Value.ElementAt(i + page * itemsonpage).ItemImage.Length > 0)
                {
                    bool exist = (bool)ImageLibrary?.Call("HasImage", shopSettings.Tabs.ElementAt(tab).Value.ElementAt(i + page * itemsonpage).ItemImageName);
                    if (exist)
                    {
                        ID = (string)ImageLibrary?.Call("GetImage", shopSettings.Tabs.ElementAt(tab).Value.ElementAt(i + page * itemsonpage).ItemImageName);
                        if (ID == "")
                            ID = (string)ImageLibrary?.Call("GetImage", shopSettings.Tabs.ElementAt(tab).Value.ElementAt(i + page * itemsonpage).ItemImageName) ?? ID;
                    }
                }
                else
                {
                    ID = (string)ImageLibrary?.Call("GetImage", shopSettings.Tabs.ElementAt(tab).Value.ElementAt(i + page * itemsonpage).ShortName);
                    if (ID == "")
                        ID = (string)ImageLibrary?.Call("GetImage", shopSettings.Tabs.ElementAt(tab).Value.ElementAt(i + page * itemsonpage).ShortName) ?? ID;
                }
                /*
                container.Add(new CuiElement
                {
                    Parent = UI_Layer + ".MainItems" + $".Item_{counter}",
                    Components =
                    {
                        new CuiImageComponent { Color = GetColor("#000000", 1f) },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0.5 0.5",
                            AnchorMax = $"0.5 0.5",
                            OffsetMin = "-45 -45",
                            OffsetMax = "45 45"
                        },
                    }
                });
                */
                if (ID.Length > 0)
                {
                    container.Add(new CuiElement
                    {
                        Parent = UI_Layer + ".MainItems" + $".Item_{counter}",
                        Components =
                    {
                        new CuiRawImageComponent { Png = ID },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0.5 0.5",
                            AnchorMax = $"0.5 0.5",
                            OffsetMin = "-40 -35",
                            OffsetMax = "40 40"
                        },
                    }
                    });
                }
                container.Add(new CuiElement
                {
                    Parent = UI_Layer + ".MainItems" + $".Item_{counter}",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = "x" + shopSettings.Tabs.ElementAt(tab).Value.ElementAt(i + page*itemsonpage).Amount,
                            Align = shopGuiSettings.Gui_AmountAnchor,
                            FontSize = shopGuiSettings.Gui_FontSizeAmount,
                            Font = shopGuiSettings.Gui_FontAmount
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0.5 0.5",
                            AnchorMax = $"0.5 0.5",
                            OffsetMin = "-45 -45",
                            OffsetMax = "45 45"
                        },
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = UI_Layer + ".MainItems" + $".Item_{counter}",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = shopSettings.Tabs.ElementAt(tab).Value.ElementAt(i + page*itemsonpage).Price + shopSettings.Shop_MoneyName,
                            Align = shopGuiSettings.Gui_PriceAnchor,
                            FontSize = shopGuiSettings.Gui_FontSizePrice,
                            Font = shopGuiSettings.Gui_FontPrice
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0.5 0.5",
                            AnchorMax = $"0.5 0.5",
                            OffsetMin = "-45 -45",
                            OffsetMax = "45 45"
                        },
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Color = "0 0 0 0", Command = $"UI_GetItem {tab} {page} {i + page * itemsonpage}" },
                    Text = { Text = "" }
                }, UI_Layer + ".MainItems" + $".Item_{counter}");
                //Puts(counter.ToString() + "  " + shopSettings.Tabs.ElementAt(tab).Value.ElementAt(i + page * 23).ShortName);
                counter++;
                //if (counter > 23) continue;
                a++;
                if(a == 6)
                {
                    a = 0;
                    b++;
                }
            }
                
            if(shopSettings.Tabs.ElementAt(tab).Value.Count > 23 + (page * 23))
            {
                container.Add(new CuiElement
                {
                    Parent = UI_Layer + ".MainItems",
                    Name = UI_Layer + ".MainItems" + ".NextPage",
                    Components =
                    {
                        new CuiImageComponent { Color = GetColor("#718847", 1f) },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0.5 0.005",
                            AnchorMax = $"0.5 0.005",
                            OffsetMin = "4 0",
                            OffsetMax = "105 20"
                        },
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = UI_Layer + ".MainItems" + ".NextPage",
                    Components =
                        {
                            new CuiTextComponent
                            {
                                Text = "Далее",
                                Align = TextAnchor.MiddleCenter,
                                FontSize = 14,
                                Font = "robotocondensed-bold.ttf"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0 0",
                                AnchorMax = $"1 1"
                            },
                        }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Color = "0 0 0 0", Command = $"UI_UpdateShop {tab} {page + 1}" },
                    Text = { Text = "" }
                }, UI_Layer + ".MainItems" + ".NextPage");
            }

            if (page > 0)
            {
                container.Add(new CuiElement
                {
                    Parent = UI_Layer + ".MainItems",
                    Name = UI_Layer + ".MainItems" + ".BackPage",
                    Components =
                    {
                        new CuiImageComponent { Color = GetColor("#718847", 1f) },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0.5 0.005",
                            AnchorMax = $"0.5 0.005",
                            OffsetMin = "-105 0",
                            OffsetMax = "-4 20"
                        },
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = UI_Layer + ".MainItems" + ".BackPage",
                    Components =
                        {
                            new CuiTextComponent
                            {
                                Text = "Назад",
                                Align = TextAnchor.MiddleCenter,
                                FontSize = 14,
                                Font = "robotocondensed-bold.ttf"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0 0",
                                AnchorMax = $"1 1"
                            },
                        }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Color = "0 0 0 0", Command = $"UI_UpdateShop {tab} {page - 1}" },
                    Text = { Text = "" }
                }, UI_Layer + ".MainItems" + ".BackPage");
            }

            CuiHelper.DestroyUi(player, UI_Layer);
            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Commands
        [ChatCommand("shop")]
        void CMD_OpenShop(BasePlayer player, string command, string[] args)
        {
            Draw_UIShopMain(player);
        }

        [ConsoleCommand("UI_OpenInfoPage")]
      //  Слив плагинов server-rust by Apolo YouGame
        private void CMD_InfoPage(ConsoleSystem.Arg arg)
      //  Слив плагинов server-rust by Apolo YouGame
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            Draw_UIShopMain(player, 0, 0, true);
        }

        [ConsoleCommand("UI_GetItem")]
        private void CMD_BuyItem(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;

            if (arg.Args.Length > 0)
            {
                int page = 0, tab = 0, itemnum = 0;
                if (Int32.TryParse(arg.Args[0], out tab))
                {
                    if (arg.Args.Length < 2)
                    {
                        Draw_UIShopMain(player, tab);
                        return;
                    }
                    if (Int32.TryParse(arg.Args[1], out page))
                    {
                        if (arg.Args.Length < 3)
                        {
                            Draw_UIShopMain(player, tab, page);
                            return;
                        }
                        if (Int32.TryParse(arg.Args[2], out itemnum))
                        {
                            if (shopSettings.Shop_MoneyType.Length > 0)
                            {
                                List<Item> items = new List<Item>();
                                foreach (var i in player.inventory.containerMain.itemList)
                                {
                                    if (i.info.shortname == shopSettings.Shop_MoneyType)
                                        items.Add(i);
                                }
                                if (items.Count < 1) return;
                                Item iteminv = player.inventory.containerMain.FindItemsByItemName(shopSettings.Shop_MoneyType);
                                playerBalance[player.userID] = iteminv.amount;
                                if (shopSettings.Tabs.ElementAt(tab).Value.ElementAt(itemnum).Price <= playerBalance[player.userID])
                                {
                                    if(shopSettings.Tabs.ElementAt(tab).Value.ElementAt(itemnum).ItemCommand.Length > 0)
                                    {
                                        Server.Command(FormateSrt(shopSettings.Tabs.ElementAt(tab).Value.ElementAt(itemnum).ItemCommand, player, shopSettings.Tabs.ElementAt(tab).Value.ElementAt(itemnum).Amount));
                                    }
                                    else
                                    {
                                        Item item = ItemManager.CreateByName(shopSettings.Tabs.ElementAt(tab).Value.ElementAt(itemnum).ShortName, shopSettings.Tabs.ElementAt(tab).Value.ElementAt(itemnum).Amount);
                                        player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
                                    }
                                    
                                    //iteminv.amount -= shopSettings.Tabs.ElementAt(tab).Value.ElementAt(itemnum).Price;
                                    player.inventory.containerMain.Take(items, iteminv.info.itemid, shopSettings.Tabs.ElementAt(tab).Value.ElementAt(itemnum).Price);
                                    Draw_UIShopMain(player, tab, page);
                                    return;
                                }
                                return;
                            }
                            
                            if (shopSettings.Tabs.ElementAt(tab).Value.ElementAt(itemnum).Price <= playerBalance[player.userID])
                            {
                                if (shopSettings.Tabs.ElementAt(tab).Value.ElementAt(itemnum).ItemCommand.Length > 0)
                                {
                                    Server.Command(FormateSrt(shopSettings.Tabs.ElementAt(tab).Value.ElementAt(itemnum).ItemCommand, player, shopSettings.Tabs.ElementAt(tab).Value.ElementAt(itemnum).Amount));
                                }
                                else
                                {
                                    Item item = ItemManager.CreateByName(shopSettings.Tabs.ElementAt(tab).Value.ElementAt(itemnum).ShortName, shopSettings.Tabs.ElementAt(tab).Value.ElementAt(itemnum).Amount);
                                    player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
                                }
                                playerBalance[player.userID] -= shopSettings.Tabs.ElementAt(tab).Value.ElementAt(itemnum).Price;
                                Draw_UIShopMain(player, tab, page);
                                return;
                            }
                            else
                            {
                                return;
                            }
                        }
                        Draw_UIShopMain(player, tab, page);
                        return;
                    }
                    Draw_UIShopMain(player, tab);
                }
                else
                {
                    Draw_UIShopMain(player);
                }

            }
            else
            {
                Draw_UIShopMain(player);
            }
        }

        [ConsoleCommand("UI_UpdateShop")]
        private void CMD_UpdateShop(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;

            if (arg.Args.Length > 0)
            {
                int page = 0, tab = 0;
                if (Int32.TryParse(arg.Args[0], out tab))
                {
                    if (arg.Args.Length < 2)
                    {
                        Draw_UIShopMain(player, tab);
                        return;
                    }
                    if (Int32.TryParse(arg.Args[1], out page))
                    {
                        Draw_UIShopMain(player, tab, page);
                        return;
                    }
                    Draw_UIShopMain(player, tab);
                }
                else
                {
                    Draw_UIShopMain(player);
                }

            }
            else
            {
                Draw_UIShopMain(player);
            }
        }

        [ConsoleCommand("add.balance")]
        private void CMD_AddBalance(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (!player.IsAdmin)
            {
                SendReply(player, "Отказано в доступе!");
                return;
            }
            if (arg.Args.Length > 0)
            {
                ulong userid = 0; int money = 0;
                if (ulong.TryParse(arg.Args[0], out userid))
                {
                    if (arg.Args.Length < 2)
                    {
                        SendReply(player, "Ошибка синтаксиса!");
                        return;
                    }
                    if (Int32.TryParse(arg.Args[1], out money))
                    {
                        if(shopSettings.Shop_MoneyType.Length > 0)
                        {
                            SendReply(player, "Выдача средств отключена из за включеного параметра покупки за ресурсы!");
                            return;
                        }
                        playerBalance[userid] += money;
                        SendReply(BasePlayer.FindByID(userid), $"Админ выдал вам деньги на счёт({money}руб)");
                        return;
                    }
                    SendReply(player, "Ошибка синтаксиса!");
                }
                else
                {
                    SendReply(player, "Ошибка синтаксиса!");
                }

            }
            else
            {
                SendReply(player, "Ошибка синтаксиса!");
            }
        }

        [ConsoleCommand("rem.balance")]
        private void CMD_RemBalance(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (!player.IsAdmin)
            {
                SendReply(player, "Отказано в доступе!");
                return;
            }

            if (arg.Args.Length > 0)
            {
                ulong userid = 0; int money = 0;
                if (ulong.TryParse(arg.Args[0], out userid))
                {
                    if (arg.Args.Length < 2)
                    {
                        SendReply(player, "Ошибка синтаксиса!");
                        return;
                    }
                    if (Int32.TryParse(arg.Args[1], out money))
                    {
                        if (shopSettings.Shop_MoneyType.Length > 0)
                        {
                            SendReply(player, "Выдача средств отключена из за включеного параметра покупки за ресурсы!");
                            return;
                        }
                        if (playerBalance[userid] >= money)
                        {
                            playerBalance[userid] -= money;
                            SendReply(player, $"Админ отобрал у вас деньги({money})!");
                        }
                        else SendReply(player, "Баланс игрока меньше указанной суммы!");
                        return;
                    }
                    SendReply(player, "Ошибка синтаксиса!");
                }
                else
                {
                    SendReply(player, "Ошибка синтаксиса!");
                }

            }
            else
            {
                SendReply(player, "Ошибка синтаксиса!");
            }
        }
        #endregion
        
        #region Utils

        private string FormateSrt(string str, BasePlayer player, int itemnum = 0)
        {
            if (str.Contains("steamid"))
            {
                str = str.Replace("steamid", player.UserIDString);
            }
            if (str.Contains("itemnum"))
            {
                str = str.Replace("itemnum", itemnum.ToString());
            }
            Puts(str);
            return str;
        }
        public static string GetColor(string hex, float alpha = 1f)
                {
                    var color = ColorTranslator.FromHtml(hex);
                    var r = Convert.ToInt16(color.R) / 255f;
                    var g = Convert.ToInt16(color.G) / 255f;
                    var b = Convert.ToInt16(color.B) / 255f;

                    return $"{r} {g} {b} {alpha}";
                }
        #endregion
        
        #region API
        private int API_ShopGetBalance(ulong userid)
        {
            return playerBalance[userid];
        }
        private void API_ShopSetBalance(ulong userid, int balance)
        {
            playerBalance[userid] = balance;
        }
        private void API_ShopAddBalance(ulong userid, int balance)
        {
            playerBalance[userid] += balance;
        }
        private void API_ShopRemBalance(ulong userid, int balance)
        {
            if(playerBalance[userid] >= balance) playerBalance[userid] -= balance;
            else playerBalance[userid] = 0;
        }
        #endregion
    }
}
