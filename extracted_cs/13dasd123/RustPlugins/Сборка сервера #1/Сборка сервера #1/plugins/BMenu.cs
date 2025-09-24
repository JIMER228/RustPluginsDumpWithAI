using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using ProtoBuf;
using Steamworks.ServerList;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BMenu", "https://discord.gg/9vyTXsJyKR", "1.0")]
    public class BMenu : RustPlugin
    {
        #region var

        [PluginReference] private Plugin ImageLibrary, Shop, Mercenaries, Economics, Profile, Batia, GameStoresRUST, BroadcastSystem, Kits, HitMarkersRu, CardsSystem;

        private string _textColor = "0.929 0.882 0.847 1";
        private int fontsize = 18;

        #endregion

        #region config

        static Configuration config = new Configuration();

        class Configuration
        {
            [JsonProperty("Навигационное меню")]
            public List<MenuItem> MenuItems;

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    MenuItems = new List<MenuItem>
                    {
                        new MenuItem
                        {
                            Name = "About",
                            Command = "menupage About",
                            Key = "About"
                        },
                        new MenuItem
                        {
                            Name = "Mercenaries",
                            Command = "menupage Mercenaries",
                            Key = "Mercenaries"
                        },
                        new MenuItem
                        {
                            Name = "Batia",
                            Command = "menupage Batia",
                            Key = "Batia"
                        },
                        new MenuItem
                        {
                            Name = "Store",
                            Command = "menupage Store",
                            Key = "Store"
                        },							
                        new MenuItem
                        {
                            Name = "Profile",
                            Command = "menupage Profile",
                            Key = "Profile"
                        },					
                        new MenuItem
                        {
                            Name = "Close",
                            Command = "closemenu",
                            Key = "Close"
                        },
                    }
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) PrintWarning("NULL");
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintError($"Не удалось найти конфигурацию 'oxide/config/{Name}', Создание конфига!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }
        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        class MenuItem
        {
            [JsonProperty("Заголовок пункта меню", Order = 1)]
            public string Name;
            [JsonProperty("Команда для доступа к пункту меню", Order = 2)]
            public string Command;
            [JsonProperty("Ключ", Order = 3)]
            public string Key;
        }


        #endregion

        #region data

        Dictionary<ulong, string> ActiveButton = new Dictionary<ulong, string>();

        #endregion

        #region hooks

        void OnServerInitialized()
        {
            ImageLibrary.Call("AddImage", "https://gspics.org/images/2022/06/13/0neGnK.png", "Basic");
            ImageLibrary.Call("AddImage", "https://gspics.org/images/2022/06/13/0nec8x.png", "white");
            ImageLibrary.Call("AddImage", "https://gspics.org/images/2022/06/13/0neDCT.png", "online");
            ImageLibrary.Call("AddImage", "https://gspics.org/images/2022/06/12/0neKWy.png", "red");
            ImageLibrary.Call("AddImage", "https://gspics.org/images/2022/06/13/0nec8x.png", "obg");
            InitializeLang();

        }

        MenuItem GetPage(string name)
        {
            MenuItem page = new MenuItem();
            page = config.MenuItems.FirstOrDefault(p => p.Key == name);
            return page;
        }

        #endregion

        #region UI

        private string _Layer = "Main_UI";
        private string _SubLayer = "SubMenu_UI";
        private string _SubContent = "SubContent_UI";

        void MainGUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, _Layer);
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = { Sprite = "", Material = "assets/content/ui/uibackgroundblur.mat", Color = "0.08 0.07 0.11 0.97" },
                CursorEnabled = true,
                RectTransform =
                {
                    AnchorMin = "0 0",AnchorMax = "1 1"
                },


            }, "Overlay", _Layer);

            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = $"-641 -55", OffsetMax = $"640 0" }
            }, _Layer, _Layer + ".NavBarUI");


            container.Add(new CuiElement
            {
                Parent = _Layer + ".NavBarUI",
                Name = _Layer + ".NavBarUI" + ".OutlineUI",
                Components =
                {
                    new CuiOutlineComponent{Color = HexToCuiColor("#2A2534"),Distance = "3 3"},
                    new CuiImageComponent{Color = "0.09 0.08 0.11 1"},
                    new CuiRectTransformComponent{AnchorMin = "0 0",AnchorMax = "1 1"}
                }
            });




            double startX = 10;


            foreach (var var in config.MenuItems)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = $"{var.Command}" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = $"{startX} 0", OffsetMax = $"{startX + (var.Name.Length + 4) * (fontsize * 0.6)} 55" },
                    Text = { Align = TextAnchor.MiddleCenter, Color = _textColor, FontSize = fontsize, Text = lang.GetMessage(var.Name, this, player.UserIDString) }
                }, _Layer + ".NavBarUI", _Layer + ".NavBarUI" + $".Button{var.Key}");

                /*if (var.Key == "Close")
                {
                    container.Add(new CuiElement
                    {
                        Parent = _Layer+".NavBarUI"+$".Button{var.Key}",
                        Name = _Layer+".NavBarUI"+$".Button{var.Key}"+".Image",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = (string) ImageLibrary.Call("GetImage", "red"),
                                Color = "1 0 0 0.3"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",AnchorMax = "1 1"
                            }
                        }
                    });
                }*/



                startX += 5 + (var.Name.Length + 4) * (fontsize * 0.6);
            }

            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0.5" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "950 0", OffsetMax = "1200 55" }
            }, _Layer + ".NavBarUI", _Layer + ".NavBarUI" + ".OnlineUI");

            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "840 5", OffsetMax = "1200 55" }
            }, _Layer + ".NavBarUI", _Layer + ".NavBarUI" + ".Panel");

            container.Add(new CuiElement
            {
                Parent = _Layer + ".NavBarUI" + ".Panel",
                Name = _Layer + ".NavBarUI" + ".Panel" + ".Image",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "obg"),

                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.01", AnchorMax = "1 0.9"
                    },
                }
            });

            /*container.Add(new CuiElement
            {
                Parent = _Layer + ".NavBarUI" + ".OnlineUI",
                Name = _Layer + ".NavBarUI" + ".OnlineUI" + ".Image",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "obg"),
                        
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.1", AnchorMax = "1 0.9"
                    },
                }
            });*/
            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "840 0", OffsetMax = "950 55" }
            }, _Layer + ".NavBarUI", _Layer + ".NavBarUI" + ".BalanceUI");

            container.Add(new CuiElement
            {
                Parent = _Layer + ".NavBarUI" + ".BalanceUI",
                Name = _Layer + ".NavBarUI" + ".BalanceUI" + ".Image",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "Basic"),

                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0", AnchorMax = "0 0",OffsetMin = "20 15",OffsetMax = "43 40"
                    },
                }
            });
            container.Add(new CuiLabel
            {
                Text = { Align = TextAnchor.MiddleLeft, Text = $"RP: {Economics.Call("Balance", player.UserIDString)}", FontSize = 18, Color = _textColor, Font = "robotocondensed-bold.ttf" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "52 0", OffsetMax = "200 55" }
            }, _Layer + ".NavBarUI" + ".BalanceUI",
               _Layer + ".NavBarUI" + ".BalanceUI" + ".Balance");
            container.Add(new CuiElement
            {
                Parent = _Layer + ".NavBarUI" + ".OnlineUI",
                Name = _Layer + ".NavBarUI" + ".OnlineUI" + ".Image",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "online"),

                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0", AnchorMax = "0 0",OffsetMin = "83 15",OffsetMax = "103 40"
                    },
                }
            });

            container.Add(new CuiLabel
                {
                    Text = { Align = TextAnchor.MiddleLeft, Text = $"                 КОМАНДА: 1/∞", FontSize = 18, Color = _textColor, Font = "robotocondensed-bold.ttf" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "46 0", OffsetMax = "250 55" }
                }, _Layer + ".NavBarUI" + ".OnlineUI",
                _Layer + ".NavBarUI" + ".OnlineUI" + ".Players");
            /*if (!(IsInTeam(player)))
            {
                container.Add(new CuiLabel
                {
                    Text = { Align = TextAnchor.MiddleLeft, Text = $"                 КОМАНДА: 1/∞", FontSize = 18, Color = _textColor, Font = "robotocondensed-bold.ttf" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "46 0", OffsetMax = "250 55" }
                }, _Layer + ".NavBarUI" + ".OnlineUI",
                    _Layer + ".NavBarUI" + ".OnlineUI" + ".Players");
            }
            else
            {
                container.Add(new CuiLabel
                {
                    Text = { Align = TextAnchor.MiddleLeft, Text = $"                 КОМАНДА: {player.Team.members.Count}/20", FontSize = 18, Color = _textColor, Font = "robotocondensed-bold.ttf" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "46 0", OffsetMax = "250 55" }
                }, _Layer + ".NavBarUI" + ".OnlineUI",
                    _Layer + ".NavBarUI" + ".OnlineUI" + ".Players");
            }*/



            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "345 15", OffsetMax = "1265 660" },

            }, _Layer, _SubContent);

            container.Add(new CuiElement
            {
                Parent = _SubContent,
                Name = _SubContent + ".Outline",
                Components =
                {
                    
                    new CuiImageComponent{Color = "0 0 0 0"},
                    new CuiRectTransformComponent{AnchorMin = "0 0",AnchorMax = "1 1"}
                }
            });
            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "10 15", OffsetMax = "335 635" },
            }, _Layer, _SubLayer);
            
            container.Add(new CuiButton
            {
                Button = { Close = _Layer, Sprite = "assets/icons/close.png", Color = _textColor },
                Text = { Text = "" },
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-45 -45", OffsetMax = "-5 -5" }
            }, _Layer + ".NavBarUI");
            CuiHelper.AddUi(player, container);
            
        }



        void SetActiveButton(BasePlayer player, string button)
        {
            CuiHelper.DestroyUi(player, _Layer + ".NavBarUI" + $".Active");
            var container = new CuiElementContainer();
            container.Add(new CuiElement
            {
                Parent = _Layer + ".NavBarUI" + $".Button{button}",
                Name = _Layer + ".NavBarUI" + $".Active",
                Components =
                {
                    new CuiRawImageComponent
                    {

                        Png = (string) ImageLibrary.Call("GetImage", "red"),


                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",AnchorMax = "1 1"
                    }
                }
            });
            CuiHelper.AddUi(player, container);
        }

        void SetActiveSubButton(BasePlayer player, string button)
        {
            DrawSupplyButtons(player);
            ButtonData buttonData = new ButtonData();
            CuiHelper.DestroyUi(player, _SubLayer + $"Btn{button}");
            if (_button.ContainsKey(button))
            {
                buttonData = _button[button];
            }

            string text = buttonData.text;
            double widht = buttonData.widht;
            double index = buttonData.index;
            double padding = buttonData.padding;
            
            var container = new CuiElementContainer();
            
            container.Add(new CuiButton
            {
                Button = { Close = _SubContent, Color = "0 0 0 0", Command = $"chat.say /{button}" },
                Text = { Text = text, Color = "0.929 0.882 0.847 1", Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = $"40 {200 + widht * index + padding * index}", OffsetMax = $"225 {200 + widht * index + padding * index + widht}" }
            }, _SubLayer, _SubLayer + $"Btn{button}");
            container.Add(new CuiElement
            {
                Parent = _SubLayer + $"Btn{button}",
                Name = _SubLayer + $"Btn{button}" + ".Image",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "white"),

                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0", AnchorMax = "1 1"
                    },
                }
            });

            CuiHelper.AddUi(player, container);

        }

        #endregion

        #region commands

        [ChatCommand("menu")]
        void Open(BasePlayer player,string command,string[] args)
        {
            MainGUI(player);
            player.SendConsoleCommand("chat.say /info");
        }

        [ConsoleCommand("menupage")]
        void PlayerOpenMenuPage(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null || !arg.HasArgs(1)) return;
            if (config.MenuItems.Exists(m => m.Key == arg.Args[0]))
            {
                SetActiveButton(arg.Player(), arg.Args[0]);
                switch (arg.Args[0])
                {
                    case "About":
                        arg.Player().SendConsoleCommand("chat.say /info");
                        break;
                    case "Mercenaries":
                        arg.Player().SendConsoleCommand("chat.say /merc");
                        break;
                    case "Batia":
                        arg.Player().SendConsoleCommand("chat.say /tech");
                        break;
                    case "Profile":
                        arg.Player().SendConsoleCommand("chat.say /profile");
                        break;
                    case "Store":
                        arg.Player().SendConsoleCommand("chat.say /store");
                        break;						
                }
            }

        }

        class ButtonData
        {
            public string button;
            public string text;
            public double widht;
            public double index;
            public double padding;
        }
        Dictionary<string, ButtonData> _button = new Dictionary<string, ButtonData>();
        /*[ChatCommand("profile")]
        void OpenProfie(BasePlayer player)
        {
            MainGUI(player);
            SetActiveButton(player,"Profile");
            Profile.Call("ProfileUI", player);
        }*/

        [ChatCommand("shop")]
        void OpenShop(BasePlayer player)
        {
            MainGUI(player);
            SetActiveButton(player, "Mercenaries");
            SetPage(player.userID, "shop");
            Shop.Call("ShopUI", player);
            DrawSupplyButtons(player);
            SetActiveSubButton(player, "shop");
        }
		
        [ChatCommand("merc")]
        void OpenMerc(BasePlayer player)
        {
            MainGUI(player);
            SetActiveButton(player, "Mercenaries");
            SetPage(player.userID, "merc");
            Mercenaries?.Call("MercUI", player);
            DrawSupplyButtons(player);
            SetActiveSubButton(player, "merc");
        }

        [ChatCommand("tech")]
        void OpenBatia(BasePlayer player)
        {
            MainGUI(player);
            SetActiveButton(player, "Batia");
            SetPage(player.userID, "batia");
            Batia.Call("DrawNPCUI", player, "", "");
        }

        [ChatCommand("store")]
        void OpenStore(BasePlayer player)
        {
            MainGUI(player);
            SetActiveButton(player, "Store");
            SetPage(player.userID,"store");
            GameStoresRUST.Call("CmdChatStore", player);
        }

        List<string> pLayers = new List<string> { "MainSkin", "UI.Kits", "UI.HitMarkers", "MainChat", "UI_ReportLayer", "Craft_UI", "UI.Clans", "MainVK", "MainReport", "UI.Clans.Modal", "SubContent_UI.Main", ".SubContent_UI.Second.Main" };
        [ConsoleCommand("closemenu")]
        void CloseMenu(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            CuiHelper.DestroyUi(arg.Player(), "MainSkin");
            CuiHelper.DestroyUi(arg.Player(), "UI.HitMarkers");			
            CuiHelper.DestroyUi(arg.Player(), "MainChat");
            CuiHelper.DestroyUi(arg.Player(), "UI.Kits");			
            CuiHelper.DestroyUi(arg.Player(), "Craft_UI");			
            CuiHelper.DestroyUi(arg.Player(), "UI_ReportLayer");
            CuiHelper.DestroyUi(arg.Player(), "Buttons_UI");
            List<string> pLayers = new List<string> { "MainSkin","UI.Kits", "UI.HitMarkers", "MainChat", "Craft_UI", "UI_ReportLayer", "UI.Clans", "MainVK", "MainReport", "UI.Clans.Modal", "SubContent_UI.Main", ".SubContent_UI.Second.Main" };
            foreach (var var in pLayers)
            {
                CuiHelper.DestroyUi(arg.Player(), var);
            }

            CuiHelper.DestroyUi(arg.Player(), _Layer);
            SetPage(arg.Player().userID, "closed");
        }


        #endregion

        #region PlayerPage

        Dictionary<ulong, string> _playerPages = new Dictionary<ulong, string>();

        string GetPage(ulong playerId)
        {
            if (_playerPages.ContainsKey(playerId))
            {

                return _playerPages[playerId];
            }
            else
            {
                return "null";
            }

        }

        void SetPage(ulong playerId, string page)
        {
            if (_playerPages.ContainsKey(playerId))
            {
                _playerPages[playerId] = page;
            }
            else
            {
                _playerPages.Add(playerId, page);
            }
        }

        #endregion

        #region ButtonPages

        Dictionary<string, string> _supplyButtons = new Dictionary<string, string>
        {
            ["<b>ХРАНИЛИЩЕ</b>"] = "openinv",
            ["<b>КАРТЫ</b>"] = "case",			
            ["<b>ТЕХНОЛОГИИ</b>"] = "fragment",		
            ["<b>МАГАЗИН</b>"] = "shop",
            ["<b>АВТОСАЛОН</b>"] = "vmenu",
            ["<b>ПРОМЫШЛЕННОСТЬ</b>"] = "merc"
        };
        void DrawSupplyButtons(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, _SubLayer);
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "10 15", OffsetMax = "235 635" },
            }, _Layer, _SubLayer);
            int i = 0;
            double index = 1;
            double widht = 40;
            double padding = 5;
            double def = 335;

            foreach (var var in _supplyButtons)
            {

                container.Add(new CuiButton
                {
                    Button = { Close = _SubContent, Color = "0 0 0 0", Command = $"chat.say /{var.Value}" },
                    Text = { Text = var.Key, Color = "0.929 0.882 0.847 0.2", Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = $"40 {200 + widht * index + padding * index}", OffsetMax = $"225 {200 + widht * index + padding * index + widht}" }
                }, _SubLayer, _SubLayer + $"Btn{var.Value}");
                container.Add(new CuiElement
                {
                    Parent = _SubLayer + $"Btn{var.Value}",
                    Name = _SubLayer + $"Btn{var.Value}" + ".Image",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = (string) ImageLibrary.Call("GetImage", "white"),

                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0", AnchorMax = "1 1"
                        },
                    }
                });
                if (!_button.ContainsKey(var.Value))
                {
                    _button.Add(var.Value, new ButtonData
                    {
                        button = var.Value,
                        text = var.Key,
                        index = index,
                        padding = padding,
                        widht = widht
                    });
                }
                index++;
            }

            CuiHelper.AddUi(player, container);

        }

        #endregion

        #region profile

        void ReloadLayer(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "SubContent_UI");
            var container = new CuiElementContainer();
            container.Add(
                new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Image = { Color = "0 0 0 1" },
                    CursorEnabled = false,
                    //RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0",OffsetMin = "0 0",OffsetMax = "1280 720"}
                }, "Main_UI", "SubContent_UI");
            CuiHelper.AddUi(player, container);
            SendReply(player, "Layer loaded");
        }
        [ChatCommand("profile")]
        void PlayerChat(BasePlayer player)
        {

            DrawProfileButtons(player);


            DestroyProfileLayers(player);
            //ReloadLayer(player);


            SetPage(player.userID, "chat");
            SetActivePButton(player, "openchat");
            player.SendConsoleCommand("chat.say /chat");

            //ProfileUI(player);
        }

        /*[ChatCommand("profile")]
        void PlayerChat(BasePlayer player, string command, string[] args)
        {

            DrawProfileButtons(player);


            DestroyProfileLayers(player);
            //ReloadLayer(player);


            SetPage(player.userID, "chat");
            SetActivePButton(player, "openchat");
            player.SendConsoleCommand("chat.say /chat");

            //ProfileUI(player);
        }*/

        [ChatCommand("openclan")]
        void OpenClan(BasePlayer player, string command, string[] args)
        {
            DestroyProfileLayers(player);
            //ReloadLayer(player);

            SetActivePButton(player, "openclan");
            SetPage(player.userID, "clan");
            player.SendConsoleCommand("chat.say /clan");
        }
		
        [ChatCommand("opencraft")]
        void OpenCraft(BasePlayer player, string command, string[] args)
        {
            DestroyProfileLayers(player);
            //ReloadLayer(player);

            SetActivePButton(player, "opencraft");
            SetPage(player.userID, "craft");
            player.SendConsoleCommand("chat.say /craft");
        }		

        [ChatCommand("openhit")]
        void OpenHit(BasePlayer player, string command, string[] args)
        {
            DestroyProfileLayers(player);
            //ReloadLayer(player);

            SetActivePButton(player, "openhit");
            SetPage(player.userID, "marker");
            player.SendConsoleCommand("chat.say /marker");
        }
		
        [ChatCommand("openkit")]
        void OpenKit(BasePlayer player, string command, string[] args)
        {
            DestroyProfileLayers(player);
            //ReloadLayer(player);

            SetActivePButton(player, "openkit");
            SetPage(player.userID, "kit");
            player.SendConsoleCommand("chat.say /kit");
        }
		
        [ChatCommand("openalert")]
        void OpenAlert(BasePlayer player, string command, string[] args)
        {
            DestroyProfileLayers(player);
            //ReloadLayer(player);


            SetPage(player.userID, "alert");
            SetActivePButton(player, "openalert");
            player.SendConsoleCommand("chat.say /raid");

        }

        [ChatCommand("openstat")]
        void OpenStat(BasePlayer player, string command, string[] args)
        {
            DestroyProfileLayers(player);
            //ReloadLayer(player);
            SetActivePButton(player, "openstat");
            SetPage(player.userID, "stat");
            player.SendConsoleCommand("chat.say /stat");
        }

        [ChatCommand("openreport")]
        void Openreport(BasePlayer player, string command, string[] args)
        {
            DestroyProfileLayers(player);
            //ReloadLayer(player);


            SetPage(player.userID, "report");
            SetActivePButton(player, "openreport");
            player.SendConsoleCommand("chat.say /reports");

        }

        [ChatCommand("openchat")]
        void OpenChat(BasePlayer player, string command, string[] args)
        {
            DestroyProfileLayers(player);
            //ReloadLayer(player);


            SetPage(player.userID, "chat");
            SetActivePButton(player, "openchat");
            player.SendConsoleCommand("chat.say /chat");

        }
        [ChatCommand("openskin")]
        void OpenSkin(BasePlayer player, string command, string[] args)
        {
            DestroyProfileLayers(player);
            //ReloadLayer(player);


            SetPage(player.userID, "skin");
            SetActivePButton(player, "openskin");
            player.SendConsoleCommand("chat.say /skin");

        }

        Dictionary<string, string> _profileButtons = new Dictionary<string, string>
        {          
            ["<b>РЕЙТИНГ</b>"] = "openstat",
            ["<b>ХИТМАРКЕР</b>"] = "openhit",	
            ["<b>СКИНЫ</b>"] = "openskin",						
            ["<b>ОТРЯДЫ</b>"] = "openclan",					
            ["<b>УВЕДОМЛЕНИЕ</b>"] = "raid",	
            ["<b>КРАФТ</b>"] = "opencraft",	
            ["<b>СНАРЯЖЕНИЕ</b>"] = "openkit",				
            ["<b>ЧАТ</b>"] = "openchat",			
        };

        void DestroyProfileLayers(BasePlayer player)
        {
            foreach (var var in pLayers)
            {
                CuiHelper.DestroyUi(player, var);
            }
        }

        void DrawProfileButtons(BasePlayer player)
        {
            //DestroyProfileLayers(player);
            CuiHelper.DestroyUi(player, "INTERFACE_STATS");
            CuiHelper.DestroyUi(player, _SubContent);
            CuiHelper.DestroyUi(player, _SubLayer);
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" },
            }, _Layer, _SubContent);
            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "10 15", OffsetMax = "235 635" },
            }, _Layer, _SubLayer);

            #region navbar

            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = $"-641 -55", OffsetMax = $"640 0" }
            }, _Layer, _Layer + ".NavBarUI");



            container.Add(new CuiElement
            {
                Parent = _Layer + ".NavBarUI",
                Name = _Layer + ".NavBarUI" + ".OutlineUI",
                Components =
                {
                    new CuiOutlineComponent{Color = HexToCuiColor("#2A2534"),Distance = "3 3"},
                    new CuiImageComponent{Color = "0.09 0.08 0.11 1"},
                    new CuiRectTransformComponent{AnchorMin = "0 0",AnchorMax = "1 1"}
                }
            });



            double startX = 10;

            container.Add(new CuiButton
            {
                Button = { Close = _Layer, Sprite = "assets/icons/close.png", Color = _textColor },
                Text = { Text = "" },
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-45 -45", OffsetMax = "-5 -5" }
            }, _Layer + ".NavBarUI");

            foreach (var var in config.MenuItems)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = $"{var.Command}" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = $"{startX} 0", OffsetMax = $"{startX + (var.Name.Length + 4) * (fontsize * 0.6)} 55" },
                    Text = { Align = TextAnchor.MiddleCenter, Color = _textColor, FontSize = fontsize, Text = lang.GetMessage(var.Name, this, player.UserIDString) }
                }, _Layer + ".NavBarUI", _Layer + ".NavBarUI" + $".Button{var.Key}");

                startX += 5 + (var.Name.Length + 4) * (fontsize * 0.6);
            }

            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "950 0", OffsetMax = "1200 55" }
            }, _Layer + ".NavBarUI", _Layer + ".NavBarUI" + ".OnlineUI");

            #region MoveOBG

            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "840 5", OffsetMax = "1200 55" }
            }, _Layer + ".NavBarUI", _Layer + ".NavBarUI" + ".Panel");

            container.Add(new CuiElement
            {
                Parent = _Layer + ".NavBarUI" + ".Panel",
                Name = _Layer + ".NavBarUI" + ".Panel" + ".Image",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "obg"),

                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.01", AnchorMax = "1 0.9"
                    },
                }
            });

            /*container.Add(new CuiPanel
            {
                Image = {Sprite = "assets/icons/close.png",Color ="1 1 1 1"},
                RectTransform = {AnchorMin = "0 1",AnchorMax = "0 1",OffsetMin = "1235 -45",OffsetMax = "1275 -5"}
            }, _Layer + ".NavBarUI", "CloseBtn");
            container.Add(new CuiButton
            {
                Button = {Close = _Layer,Sprite = "assets/icons/close.png",Color = "1 1 1 1"},
                Text = {Text = ""},
                RectTransform = {AnchorMin = "0 0",AnchorMax = "1 1"}
            }, "CloseBtn","BtnClose");*/

            #endregion
            



            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "840 0", OffsetMax = "950 55" }
            }, _Layer + ".NavBarUI", _Layer + ".NavBarUI" + ".BalanceUI");

            container.Add(new CuiElement
            {
                Parent = _Layer + ".NavBarUI" + ".BalanceUI",
                Name = _Layer + ".NavBarUI" + ".BalanceUI" + ".Image",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "Basic"),

                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0", AnchorMax = "0 0",OffsetMin = "20 15",OffsetMax = "43 40"
                    },
                }
            });
            container.Add(new CuiLabel
            {
                Text = { Align = TextAnchor.MiddleLeft, Text = $"RP: {Economics.Call("Balance", player.UserIDString)}", FontSize = 18, Color = _textColor, Font = "robotocondensed-bold.ttf" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "52 0", OffsetMax = "200 55" }
            }, _Layer + ".NavBarUI" + ".BalanceUI",
                _Layer + ".NavBarUI" + ".BalanceUI" + ".Balance");
            container.Add(new CuiElement
            {
                Parent = _Layer + ".NavBarUI" + ".OnlineUI",
                Name = _Layer + ".NavBarUI" + ".OnlineUI" + ".Image",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "online"),

                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0", AnchorMax = "0 0",OffsetMin = "83 15",OffsetMax = "103 40"
                    },
                }
            });

            if (!(IsInTeam(player)))
            {
                container.Add(new CuiLabel
                {
                    Text = { Align = TextAnchor.MiddleLeft, Text = $"                 КОМАНДА: 1/∞", FontSize = 18, Color = _textColor, Font = "robotocondensed-bold.ttf" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "46 0", OffsetMax = "250 55" }
                }, _Layer + ".NavBarUI" + ".OnlineUI",
                    _Layer + ".NavBarUI" + ".OnlineUI" + ".Players");
            }
            else
            {
                container.Add(new CuiLabel
                {
                    Text = { Align = TextAnchor.MiddleLeft, Text = $"                 КОМАНДА: {player.Team.members.Count}/∞", FontSize = 18, Color = _textColor, Font = "robotocondensed-bold.ttf" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "46 0", OffsetMax = "250 55" }
                }, _Layer + ".NavBarUI" + ".OnlineUI",
                    _Layer + ".NavBarUI" + ".OnlineUI" + ".Players");
            }



            #endregion

            int i = 0;
            double index = -1;
            double widht = 40;
            double padding = 5;
            double def = 335;

            foreach (var var in _profileButtons)
            {

                container.Add(new CuiButton
                {
                    Button = { Close = _SubContent, Color = "0 0 0 0", Command = $"chat.say /{var.Value}" },
                    Text = { Text = var.Key, Color = "0.929 0.882 0.847 0.2", Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = $"40 {200 + widht * index + padding * index}", OffsetMax = $"225 {200 + widht * index + padding * index + widht}" }
                }, _SubLayer, _SubLayer + $"Btn{var.Value}");
                container.Add(new CuiElement
                {
                    Parent = _SubLayer + $"Btn{var.Value}",
                    Name = _SubLayer + $"Btn{var.Value}" + ".Image",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = (string) ImageLibrary.Call("GetImage", "white"),

                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0", AnchorMax = "1 1"
                        },
                    }
                });
                if (!_button.ContainsKey(var.Value))
                {
                    _button.Add(var.Value, new ButtonData
                    {
                        button = var.Value,
                        text = var.Key,
                        index = index,
                        padding = padding,
                        widht = widht
                    });
                }
                index++;
            }

            CuiHelper.AddUi(player, container);
            SetActiveButton(player, "Profile");
        }

        void SetActivePButton(BasePlayer player, string button)
        {
            DrawProfileButtons(player);
            ButtonData buttonData = new ButtonData();
            CuiHelper.DestroyUi(player, _SubLayer + $"Btn{button}");
            if (_button.ContainsKey(button))
            {
                buttonData = _button[button];
            }

            string text = buttonData.text;
            double widht = buttonData.widht;
            double index = buttonData.index;
            double padding = buttonData.padding;
            //CuiHelper.DestroyUi(player, _SubLayer+$"Btn{button}" + ".Image");
            var container = new CuiElementContainer();
            /*container.Add(new CuiElement
            {
                Parent = _SubLayer+$"Btn{button}",
                Name = _SubLayer+$"Btn{button}" + ".Image",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "act")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",AnchorMax = "1 1"
                    }
                }
            });*/
            container.Add(new CuiButton
            {
                Button = { Close = _SubContent, Color = "0 0 0 0", Command = $"chat.say /{button}" },
                Text = { Text = text, Color = "0.929 0.882 0.847 1", Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = $"40 {200 + widht * index + padding * index}", OffsetMax = $"225 {200 + widht * index + padding * index + widht}" }
            }, _SubLayer, _SubLayer + $"Btn{button}");
            container.Add(new CuiElement
            {
                Parent = _SubLayer + $"Btn{button}",
                Name = _SubLayer + $"Btn{button}" + ".Image",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "white"),

                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0", AnchorMax = "1 1"
                    },
                }
            });

            CuiHelper.AddUi(player, container);

        }

        #endregion


        #region Lang

        void InitializeLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["About"] = "СЕРВЕР",
                ["Batia"] = "СНАБЖЕНИЕ",
                ["Mercenaries"] = "УБЕЖИЩЕ",
                ["Profile"] = "Profile",
                ["Store"] = "КОРЗИНА",				
                ["Close"] = "Close",
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["About"] = "СЕРВЕР",
                ["Batia"] = "СНАБЖЕНИЕ",
                ["Mercenaries"] = "УБЕЖИЩЕ",
                ["Profile"] = "Профиль",
                ["Store"] = "КОРЗИНА",				
                ["Close"] = "Закрыть",
            }, this, "ru");
        }

        #endregion

        #region Helper

        void UpdateBalance(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, _Layer + ".NavBarUI" + ".BalanceUI");
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "840 0", OffsetMax = "950 55" }
            }, _Layer + ".NavBarUI", _Layer + ".NavBarUI" + ".BalanceUI");

            container.Add(new CuiElement
            {
                Parent = _Layer + ".NavBarUI" + ".BalanceUI",
                Name = _Layer + ".NavBarUI" + ".BalanceUI" + ".Image",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "Basic"),

                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0", AnchorMax = "0 0",OffsetMin = "20 15",OffsetMax = "43 40"
                    },
                }
            });
            container.Add(new CuiLabel
            {
                Text = { Align = TextAnchor.MiddleLeft, Text = $"RP: {Economics.Call("Balance", player.UserIDString)}", FontSize = 18, Color = _textColor },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "52 0", OffsetMax = "200 55" }
            }, _Layer + ".NavBarUI" + ".BalanceUI",
                _Layer + ".NavBarUI" + ".BalanceUI" + ".Balance");
            Effect x = new Effect("assets/prefabs/deployable/vendingmachine/effects/vending-machine-purchase-human.prefab", player, 0, new Vector3(), new Vector3());
            EffectNetwork.Send(x, player.Connection);
            CuiHelper.AddUi(player, container);
        }

        object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            CuiHelper.DestroyUi(player, _Layer);
            return null;
        }

        private static string HexToCuiColor(string hex)
        {
            if (string.IsNullOrEmpty(hex)) hex = "#FFFFFFFF";
            var str = hex.Trim('#');
            if (str.Length == 6) str += "FF";
            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }
            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);
            Color color = new Color32(r, g, b, a);
            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }

        bool IsInTeam(BasePlayer player)
        {
            foreach (var teams in RelationshipManager.ServerInstance.teams.Values)
            {
                if (teams.members.Contains(player.userID))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}