using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Facepunch.Extend;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("InfoMenu","https://discord.gg/9vyTXsJyKR","1.0")]
    public class InfoMenu : RustPlugin
    {
        #region var

        [PluginReference] private Plugin ImageLibrary,BMenu;

        private double height = 377;
        private double weight = 205;
        private double upPadding = 65;
        private double gPadding = 125;
        private string _buttons = "Buttons_UI";
        private string _layer = "Info_Layer";
        private string _parent = "SubContent_UI";
        
        Dictionary<string,string>buttons = new Dictionary<string, string>
        {
            ["social"] = "<b>ТЕХНОЛОГИИ</b>",			
            ["rules"] = "<b>ПРАВИЛА</b>",			
            ["wiki"] = "<b>ВИКИПЕДИЯ</b>",		
            ["server"] = "<b>СЕРВЕР</b>",		
		
        };
        
        #endregion
        
        #region config

        static Configuration config = new Configuration();

        class Configuration
        {
            [JsonProperty("Навигационное меню")] public List<MenuItem> Menus;
            
            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    Menus = new List<MenuItem>
                    {
                       new MenuItem
                       {
                           Key = "social",
                           Pages = new Dictionary<int, Page>
                           {
                               [1] = new Page
                               {
                                   Blocks = new List<Block>
                                   {
                                       new Block
                                       {
                                           Text = "fd;lakhjfihwbqakjnasfdlkjaslfnjlsdkjfbjhbdkqhnjenfkjabkjdbflkjqnAWKJEBGKAKDNSKJFGNKANDHJSBDFKJAKJNgnskjdfnjkfnaksnajksn",
                                           Img = false,
                                           Alignment = TextAnchor.MiddleCenter,
                                           MinX = 5,
                                           MinY = 300,
                                           MaxX = 100,
                                           MaxY = 100,
                                           priority = 0
                                       },
                                       new Block
                                       {
                                           Text = "https://i.ibb.co/StT43Cf/zzz.png",
                                           Img = true,
                                           MinX = 120,
                                           MinY = 300,
                                           MaxX = 300,
                                           MaxY = 100,
                                           priority = 1
                                       }
                                   }
                               }
                           }
                       },
                       new MenuItem
                       {
                           Key = "blueprint",
                           Pages = new Dictionary<int, Page>
                           {
                               [1] = new Page
                               {
                                   Blocks = new List<Block>
                                   {
                                       new Block
                                       {
                                           Text = "fd;lakhjfihwbqakjnasfdlkjaslfnjlsdkjfbjhbdkqhnjenfkjabkjdbflkjqnAWKJEBGKAKDNSKJFGNKANDHJSBDFKJAKJNgnskjdfnjkfnaksnajksn",
                                           Img = false,
                                           Alignment = TextAnchor.MiddleCenter,
                                           MinX = 5,
                                           MinY = 300,
                                           MaxX = 100,
                                           MaxY = 100,
                                           priority = 0
                                       },
                                       new Block
                                       {
                                           Text = "https://i.ibb.co/StT43Cf/zzz.png",
                                           Img = true,
                                           MinX = 120,
                                           MinY = 300,
                                           MaxX = 300,
                                           MaxY = 100,
                                           priority = 1
                                       }
                                   }
                               }
                           }
                       },
                       new MenuItem
                       {
                           Key = "wiki",
                           Pages = new Dictionary<int, Page>
                           {
                               [1] = new Page
                               {
                                   Blocks = new List<Block>
                                   {
                                       new Block
                                       {
                                           Text = "fd;lakhjfihwbqakjnasfdlkjaslfnjlsdkjfbjhbdkqhnjenfkjabkjdbflkjqnAWKJEBGKAKDNSKJFGNKANDHJSBDFKJAKJNgnskjdfnjkfnaksnajksn",
                                           Img = false,
                                           Alignment = TextAnchor.MiddleCenter,
                                           MinX = 5,
                                           MinY = 300,
                                           MaxX = 100,
                                           MaxY = 100,
                                           priority = 0
                                       },
                                       new Block
                                       {
                                           Text = "https://i.ibb.co/StT43Cf/zzz.png",
                                           Img = true,
                                           MinX = 120,
                                           MinY = 300,
                                           MaxX = 300,
                                           MaxY = 100,
                                           priority = 1
                                       }
                                   }
                               }
                           }
                       },
                       new MenuItem
                       {
                           Key = "server",
                           Pages = new Dictionary<int, Page>
                           {
                               [1] = new Page
                               {
                                   Blocks = new List<Block>
                                   {
                                       new Block
                                       {
                                           Text = "fd;lakhjfihwbqakjnasfdlkjaslfnjlsdkjfbjhbdkqhnjenfkjabkjdbflkjqnAWKJEBGKAKDNSKJFGNKANDHJSBDFKJAKJNgnskjdfnjkfnaksnajksn",
                                           Img = false,
                                           Alignment = TextAnchor.MiddleCenter,
                                           MinX = 5,
                                           MinY = 300,
                                           MaxX = 100,
                                           MaxY = 100,
                                           priority = 0
                                       },
                                       new Block
                                       {
                                           Text = "https://i.ibb.co/StT43Cf/zzz.png",
                                           Img = true,
                                           MinX = 120,
                                           MinY = 300,
                                           MaxX = 300,
                                           MaxY = 100,
                                           priority = 1
                                       }
                                   }
                               }
                           }
                       },
                       new MenuItem
                       {
                           Pages = new Dictionary<int, Page>
                           {
                               [1] = new Page
                               {
                                   Blocks = new List<Block>
                                   {
                                       new Block
                                       {
                                           Text = "fd;lakhjfihwbqakjnasfdlkjaslfnjlsdkjfbjhbdkqhnjenfkjabkjdbflkjqnAWKJEBGKAKDNSKJFGNKANDHJSBDFKJAKJNgnskjdfnjkfnaksnajksn",
                                           Img = false,
                                           Alignment = TextAnchor.MiddleCenter,
                                           MinX = 5,
                                           MinY = 300,
                                           MaxX = 100,
                                           MaxY = 100,
                                           priority = 0
                                       },
                                       new Block
                                       {
                                           Text = "https://i.ibb.co/StT43Cf/zzz.png",
                                           Img = true,
                                           MinX = 120,
                                           MinY = 300,
                                           MaxX = 300,
                                           MaxY = 100,
                                           priority = 1
                                       }
                                   }
                               }
                           }
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
            public string Key;
            public Dictionary<int, Page> Pages;
        }

        class Block
        {
            [JsonProperty("Приоритет")]
            public int priority = 0;
            [JsonProperty("Текст")]
            public string Text;
            [JsonProperty("Картинка?(true/false)")]
            public bool Img;
            [JsonProperty("MinX")]
            public double MinX;
            [JsonProperty("MinY")]
            public double MinY = 920;
            [JsonProperty("MaxX")]
            public double MaxX;
            [JsonProperty("MaxY")]
            public double MaxY = 920;
            [JsonProperty("Цвет")] 
            public string color = "#FFFFFF";
            [JsonProperty("Размер шрифта")]
            public int FonSize = 18;
            [JsonProperty("Выравнивание текста")] 
            public TextAnchor Alignment = TextAnchor.MiddleCenter;

        }

        class Page
        {
            public List<Block> Blocks;
        }

        #endregion

        #region commands

        [ChatCommand("info")]
        void OpenInfo(BasePlayer player)
        {
            List<string>pLayers = new List<string>{"MainSkin","MainChat","UI_ReportLayer","UI.Clans","MainVK"};
            foreach (var var in pLayers)
            {
                CuiHelper.DestroyUi(player, var);
            }

            BMenu.Call("MainGUI", player);
            BMenu.Call("SetActiveButton", player, "About");
            ContentUI(player);
            ButtonUI(player);
            CuiHelper.DestroyUi(player, "MainSkin");
            //PrintWarning("Command Accept!");
        }

        [ConsoleCommand("wiki")]
        void OpenEVentsInfo(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            List<string>pLayers = new List<string>{"MainSkin","MainChat","UI_ReportLayer","UI.Clans","MainVK","MainReport"};
            foreach (var var in pLayers)
            {
                CuiHelper.DestroyUi(player, var);
            }
            if (arg.HasArgs(1))
            {
                PageUI(player,"wiki",arg.Args[0].ToInt());
                
            }
            else
            {
                PageUI(player,"wiki");
                
            }
            //PrintWarning($"Open event: {player.UserIDString} rules");
        }
        
        [ConsoleCommand("donateinfo")]
        void OpenDonate(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            List<string>pLayers = new List<string>{"MainSkin","MainChat","UI_ReportLayer","UI.Clans","MainVK"};
            foreach (var var in pLayers)
            {
                CuiHelper.DestroyUi(player, var);
            }
            if (arg.HasArgs(1))
            {
                PageUI(player,"donateinfo",arg.Args[0].ToInt());
                
            }
            else
            {
                PageUI(player,"donateinfo");
                
            }
            //($"Open donate: {player.UserIDString} rules");
        }
        
        [ConsoleCommand("rules")]
        void OpenRules(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            List<string>pLayers = new List<string>{"MainSkin","MainChat","UI_ReportLayer","UI.Clans","MainVK","MainReport"};
            foreach (var var in pLayers)
            {
                CuiHelper.DestroyUi(player, var);
            }
                if (arg.HasArgs(1))
            {
                PageUI(player,"rules",arg.Args[0].ToInt());
                
            }
            else
            {
                PageUI(player,"rules");
               //PrintWarning($"Open rules: {player.UserIDString} rules");
            }
            
        }
        
        [ConsoleCommand("reportopen")]
        void OpenRepots(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
            {
                return;
            }

            BasePlayer player = arg.Player();
            BMenu.Call("MainGUI",player);
            BMenu.Call("SetActiveButton", player, "Profile");
            player.SendConsoleCommand("report");
        }
        [ConsoleCommand("clanopen")]
        void OpenClan(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
            {
                return;
            }

            BasePlayer player = arg.Player();
            BMenu.Call("MainGUI",player);
            BMenu.Call("SetActiveButton", player, "Profile");
            player.SendConsoleCommand("openclans");
        }
        [ConsoleCommand("skinopen")]
        void OpenSkin(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
            {
                return;
            }

            BasePlayer player = arg.Player();
            BMenu.Call("MainGUI",player);
            BMenu.Call("SetActiveButton", player, "Profile");
            player.SendConsoleCommand("openskin");
        }
        [ConsoleCommand("blueprint")]
        void OpenBonus(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            if (arg.HasArgs(1))
            {
                PageUI(player,"blueprint",arg.Args[0].ToInt());
                
            }
            else
            {
                PageUI(player,"blueprint");
                
            }
            //PrintWarning($"Open bonus: {player.UserIDString} rules");

        }
        [ConsoleCommand("server")]
        void OpenClansInfo(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            if (arg.HasArgs(1))
            {
                PageUI(player,"server",arg.Args[0].ToInt());
                
            }
            else
            {
                PageUI(player,"server");
                
            }
            //PrintWarning($"Open clan: {player.UserIDString} rules");
        }
        [ConsoleCommand("social")]
        void OpenSocial(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            if (arg.HasArgs(1))
            {
                PageUI(player,"social",arg.Args[0].ToInt());
                
            }
            else
            {
                PageUI(player,"social");
                
            }
            
        }
        
        

        #endregion

        #region UI
        class ButtonData
        {
            public string button;
            public string text;
            public double widht;
            public double index;
            public double padding;
        }
        Dictionary<string,ButtonData>_button = new Dictionary<string, ButtonData>();
        void SetActiveSubButton(BasePlayer player, string button)
        {
            ButtonUI(player);
            ButtonData buttonData = new ButtonData();
            CuiHelper.DestroyUi(player, _parent+$"Btn{button}");
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
                Button = {Close = _parent,Color = "0 0 0 0",Command = $"{button}"},
                Text = {Text = text,Color = "0.929 0.882 0.847 1",Align = TextAnchor.MiddleCenter},
                RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = $"40 {120+widht*index+padding*index}",OffsetMax = $"225 {120+widht*index+padding*index+widht}"}
            },_buttons,_buttons+$"Btn{button}");
            container.Add(new CuiElement
            {
                Parent = _buttons+$"Btn{button}",
                Name = _buttons+$"Btn{button}" + ".Image",
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
            /*CuiHelper.DestroyUi(player, _buttons+$"{button}"+ ".Image");
            var container = new CuiElementContainer();
            container.Add(new CuiElement
            {
                Parent = _buttons+$"{button}",
                Name = _buttons+$"{button}" + ".Image",
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
            });
            CuiHelper.AddUi(player, container);*/
            
        }

        void PageUI(BasePlayer player,string key,int page = 1)
        {
            CuiHelper.DestroyUi(player, _parent);
            CuiHelper.DestroyUi(player, _layer);
            SetActiveSubButton(player,key);
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = {Color = "0 0 0 0"},
                RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "345 30",OffsetMax = "1265 660"},
            
            }, "Main_UI", _parent);
            container.Add(new CuiPanel
            {
                Image = {Color = "0 0 0 0"},
                RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 0",OffsetMax = "920 630"}
            }, _parent,_layer);
            MenuItem menu = config.Menus.Find(p => p.Key == key);
            /*if (menu == null)
            {
                PrintWarning($"null item {key} {page}");
            }*/
            if (menu == null) return;
            int minIndex = menu.Pages.Keys.Min();
            int maxIndex = menu.Pages.Keys.Max();
            //PrintWarning($"{minIndex} : {maxIndex} - {page}");

            Block[] blockArray = menu.Pages[page].Blocks.ToArray();
            //SortToArray(ref blockArray,menu.Pages[page].Blocks);
            
            foreach (var block in blockArray)
            {
                if (block.Img)
                {
                    container.Add(new CuiElement
                    {
                        Parent = _layer,
                        Name = _layer+".Block",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = (string) ImageLibrary.Call("GetImage",block.Text)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0", AnchorMax = "0 0",OffsetMin = $"{block.MinX} {block.MinY}",OffsetMax = $"{block.MaxX} {block.MaxY}"
                            }
                        }
                    });
                    
                }
                else
                {
                    container.Add(new CuiLabel
                    {
                        Text = {Text = block.Text,Align = block.Alignment,Color = HexToCuiColor(block.color),FontSize = block.FonSize},
                        RectTransform = {AnchorMin = "0 0", AnchorMax = "0 0",OffsetMin = $"{block.MinX} {block.MinY}",OffsetMax = $"{block.MaxX} {block.MaxY}"}
                    }, _layer, _layer + ".Block");
                }
                
            }
            

            /*if (page>minIndex)
            {
                container.Add(new CuiButton
                {
                    Button = {Close = _layer,Color = HexToCuiColor("#630606f4"),Command = $"{key} {page-1}"},
                    Text = {Text = "<",Align = TextAnchor.MiddleCenter},
                    RectTransform = {AnchorMin = "0 0",AnchorMax = "0.15 0.06"}
                }, _layer, _layer + ".Back");
            }
            if (page<maxIndex)
            {
                container.Add(new CuiButton
                {
                    Button = {Close = _layer,Color = HexToCuiColor("#630606f4"),Command = $"{key} {page+1}"},
                    Text = {Text = ">",Align = TextAnchor.MiddleCenter},
                    RectTransform = {AnchorMin = "0.85 0",AnchorMax = "1 0.06"}
                }, _layer, _layer + ".Forward");
            }*/
            if (page<maxIndex)
            {
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "717 20",OffsetMax = "890 60"},
                    Button = { Color = HexToCuiColor("#34405e"), Command = $"{key} {page+1}",Material = "assets/icons/greyout.mat" },
                    Text = { Text = "<b>ВПЕРЁД</b>", Align = TextAnchor.MiddleCenter, FontSize = 21, Font = "robotocondensed-regular.ttf",Color = HexToCuiColor("#ffffff") }
                },  _layer, _layer + ".Forward");
            }

            if (page>minIndex)
            {
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "28 20",OffsetMax = "205 60"},
                    Button = { Color = HexToCuiColor("#34405e"), Command = $"{key} {page-1}",Material = "assets/icons/greyout.mat" },
                    Text = { Text = "<b>НАЗАД</b>", Align = TextAnchor.MiddleCenter, FontSize = 21, Font = "robotocondensed-regular.ttf",Color = HexToCuiColor("#ffffff") }
                },  _layer, _layer + ".Back");
            }
            //PrintWarning($"item {key} {page}");
            //PrintWarning($"Key:{key} Page:{page} Blocks:{menu.Pages[page].Blocks.Count}");
            CuiHelper.AddUi(player, container);

        }

        void ContentUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, _parent);
            CuiHelper.DestroyUi(player, _layer);
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = {Color = "0 0 0 0"},
                RectTransform = {AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "345 15", OffsetMax = "1265 660"},

            }, "Main_UI", _parent);
            container.Add(new CuiPanel
            {
                Image = {Color = "0 0 0 0"},
                RectTransform = {AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0 0", OffsetMax = "325 620"}
            }, _parent, _layer);
            
            container.Add(new CuiElement
            {
                Parent = _parent,
                Name = _parent + ".Image",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "clan"),

                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0", AnchorMax = "1 1"
                    },
                }
            });
            
            /*container.Add(new CuiButton
            {
                Button = {Close = _layer, Color = "0 0 0 0",Command = "clanopen"},
                Text = {Text = ""},
                RectTransform =
                {
                    AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = $"{gPadding} {620 - upPadding - height}",
                    OffsetMax = $"{gPadding + weight} {620 - upPadding}"
                }
            }, _layer, _layer + ".Button_One");
            container.Add(new CuiElement
            {
                Parent = _layer + ".Button_One",
                Name = _layer + ".Button_One" + ".Image",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "clan"),

                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0", AnchorMax = "1 1"
                    },
                }
            });
            container.Add(new CuiButton
            {
                Button = {Close = _layer, Color = "0 0 0 0",Command = "skinopen"},
                Text = {Text = ""},
                RectTransform =
                {
                    AnchorMin = "0 0", AnchorMax = "0 0",
                    OffsetMin = $"{gPadding + weight + upPadding} {620 - upPadding - height}",
                    OffsetMax = $"{gPadding + weight + upPadding + weight} {620 - upPadding}"
                }
            }, _layer, _layer + ".Button_Two");
            container.Add(new CuiElement
            {
                Parent = _layer + ".Button_Two",
                Name = _layer + ".Button_Two" + ".Image",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "flag"),

                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0", AnchorMax = "1 1"
                    },
                }
            });
            container.Add(new CuiButton
            {
                Button = {Close = _layer, Color = "0 0 0 0",Command = "reportopen"},
                Text = {Text = ""},
                RectTransform =
                {
                    AnchorMin = "0 0", AnchorMax = "0 0",
                    OffsetMin = $"{gPadding + 2 * weight + 2 * upPadding} {620 - upPadding - height}",
                    OffsetMax = $"{gPadding + 2 * weight + 2 * upPadding + weight} {620 - upPadding}"
                }
            }, _layer, _layer + ".Button_Three");

            container.Add(new CuiElement
            {
                Parent = _layer + ".Button_Three",
                Name = _layer + ".Button_Three" + ".Image",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "donate"),

                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0", AnchorMax = "1 1"
                    },
                }
            });
            container.Add(new CuiLabel
            {
                Text = {Text = "БЫСТРЫЙ  ДОСТУП",Align = TextAnchor.MiddleCenter,FontSize = 20},
                RectTransform = {AnchorMin = "0 0", AnchorMax = "0 0",OffsetMin = $"{gPadding} 595",OffsetMax = $"{gPadding+2*weight+2*upPadding+weight} 620"}
            }, _layer, _layer + ".Title");*/
            CuiHelper.AddUi(player, container);

        }

        void ButtonUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, _buttons);
            CuiHelper.DestroyUi(player, "SubMenu_UI");
            //CuiHelper.DestroyUi(player, _parent);
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = {Color = "0 0 0 0"},
                RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "10 15",OffsetMax = "335 635"},
            }, "Main_UI", "SubMenu_UI");
            
            double index = 1;
            double widht = 40;
            double padding = 5;
            double def = 335;
            container.Add(new CuiPanel
            {
                Image = {Color = "0 0 0 0"},
                RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 0",OffsetMax = "325 620"}
            }, "SubMenu_UI", _buttons);
            foreach (var var in buttons)
            {
                container.Add(new CuiButton
                {
                    Button = {Close = _parent,Color = "0 0 0 0",Command = var.Key},
                    Text = {Text = var.Value,Color = "0.929 0.882 0.847 0.2",Align = TextAnchor.MiddleCenter},
                    RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = $"40 {120+widht*index+padding*index}",OffsetMax = $"225 {120+widht*index+padding*index+widht}"}
                },_buttons,_buttons+$"{var.Key}");
                container.Add(new CuiElement
                {
                    Parent = _buttons+$"{var.Key}",
                    Name = _buttons+$"{var.Key}" + ".Image",
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
                if (!_button.ContainsKey(var.Key))
                {
                    _button.Add(var.Key,new ButtonData
                    {
                        button = var.Key,
                        text = var.Value,
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

        #region hooks
        
        void OnServerInitialized()
        {
            ImageLibrary.Call("AddImage", "https://gspics.org/images/2022/06/04/0nWE0i.png", "flag");
            ImageLibrary.Call("AddImage", "https://gspics.org/images/2022/06/04/0nWE0i.png", "donate");
            ImageLibrary.Call("AddImage", "https://gspics.org/images/2022/06/04/0nWE0i.png", "clan");
            PrintWarning("Image loaded");
            foreach (var menuItem in config.Menus)
            {
                //PrintWarning($"Key:{menuItem.Key} Pages:{menuItem.Pages.Count} Blocks:{menuItem.Pages[1].Blocks.Count}");
                foreach (var pages in menuItem.Pages.Values)
                {
                    foreach (var block in pages.Blocks)
                    {
                        if (block.Img)
                        {
                            ImageLibrary.Call("AddImage", block.Text, block.Text);
                        }
                        
                    }
                }
            }
            
        }

        #endregion

        #region helper

        void SortToArray(ref Block[] array,List<Block> list)
        {
            int length = array.Length;
            Block[] buffer = new Block[length];
           
            
            int index = 0;
            while (list.Count>0)
            {
                int min = GetMinIndex(list);
                foreach (var block in list)
                {
                    if (block.priority==min)
                    {
                        buffer[index] = block;
                        index++;
                        list.Remove(block);
                    }
                }
            }

            array = buffer;
        }

        

        int GetMinIndex(List<Block> array)
        {
            int min = array.GetRandom().priority;
            foreach (var block in array)
            {
                if (min>block.priority)
                {
                    min = block.priority;
                }
            }

            return min;
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

        #endregion
    }
}