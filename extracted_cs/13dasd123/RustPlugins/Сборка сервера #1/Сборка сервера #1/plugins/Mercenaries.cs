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
    [Info("Mercenaries","https://discord.gg/9vyTXsJyKR","1.0")]
    public class Mercenaries: RustPlugin
    {
        #region var

        [PluginReference] private Plugin ImageLibrary,Economics,BMenu,Inventory,BroadcastSystem;
        
        Dictionary<ulong,Timer> playerTimes = new Dictionary<ulong, Timer>();
        
        Dictionary<string,string> rarityColors = new Dictionary<string, string>
        {
            ["null"] = "#00000000",
            ["default"] = "#636365D9",
            ["rare"] = "#3e3eabD9",
            ["epic"] = "#673eabD9",
            ["legendary"] = "#dfb22fD9"
        };
        
        
        RewardItem voidItem = new RewardItem
        {
            DisplayName = "",
            Shortname = "void",
            IsCommand = true,
            Blueprint = false,
            Amount = 0,
            Category = "",
            Image = "void",
            Rarity = "null"
        };
        RewardItem fakeItem = new RewardItem
        {
            DisplayName = "",
            Shortname = "void",
            IsCommand = true,
            Blueprint = false,
            Amount = 0,
            Category = "",
            Image = "inv",
            Rarity = "null"
        };

        #endregion
        
        #region config

        class RewardItem
        {
            public string Shortname;
            public bool Blueprint;
            public bool IsCommand;
            public int Amount;
            public string Rarity;
            public ulong SkinId = 0;
            public string Image = "";
            public string Category = "Ресурс";
            public string DisplayName;
        }

        class Mercenary
        {
            public string Key;
            public string DisplayName;
            public string url;
            public double Time;
            public double Cooldown = 10;
            public double Cost;
            public double Buy = 1000;
            public List<RewardItem> RewardItems;
        }

         static Configuration config = new Configuration();

        class Configuration
        {
            [JsonProperty("Наемники")] 
            public List<Mercenary> Mercenaries;
            [JsonProperty("Настройка редкости")] 
            public Dictionary<string,double> Rarity;

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    Mercenaries = new List<Mercenary>
                    {
                        new Mercenary
                        {
                            Key = "lvl1",
                            DisplayName = "John One",
                            Cost = 100,
                            Time = 30,
                            Buy = 300,
                            url = "https://gspics.org/images/2022/01/14/0QCxDD.jpg",
                            RewardItems = new List<RewardItem>
                            {
                                new RewardItem
                                {
                                    Shortname = "sulfur",
                                    Blueprint = false,
                                    IsCommand = false,
                                    Amount = 10,
                                    Rarity = "default",
                                    DisplayName = "Сера"
                                },
                                new RewardItem
                                {
                                    Shortname = "rifle.ak",
                                    Blueprint = true,
                                    IsCommand = false,
                                    Amount = 1,
                                    Rarity = "rare",
                                    DisplayName = "Сера"
                                },
                                new RewardItem
                                {
                                    Shortname = "kick userid",
                                    Blueprint = false,
                                    IsCommand = true,
                                    Amount = 1,
                                    Rarity = "legendary",
                                    Image = "https://i.ibb.co/GdwsKkL/BP-For-Test.png",
                                    DisplayName = "Кик",
                                    
                                },
                            }
                        },
                        new Mercenary
                        {
                            Key = "lvl2",
                            DisplayName = "Test Two",
                            Cost = 100,
                            Time = 30,
                            Buy = 400,
                            url = "https://gspics.org/images/2021/12/25/0HQr2n.jpg",
                            RewardItems = new List<RewardItem>
                            {
                                new RewardItem
                                {
                                    Shortname = "sulfur",
                                    Blueprint = false,
                                    IsCommand = false,
                                    Amount = 10,
                                    Rarity = "rare",
                                    DisplayName = "Сера"
                                },
                                new RewardItem
                                {
                                    Shortname = "rifle.ak",
                                    Blueprint = true,
                                    IsCommand = false,
                                    Amount = 1,
                                    Rarity = "epic",
                                    DisplayName = "Рецепт АК"
                                },
                                new RewardItem
                                {
                                    Shortname = "kick userid",
                                    Blueprint = false,
                                    IsCommand = true,
                                    Amount = 1,
                                    Rarity = "legendary",
                                    Image = "https://i.ibb.co/GdwsKkL/BP-For-Test.png",
                                    DisplayName = "Кик"
                                },
                            }
                        },
                        new Mercenary
                        {
                            Key = "lvl3",
                            DisplayName = "Nigga free",
                            Cost = 500,
                            Buy = 500,
                            Time = 30,
                            url = "https://gspics.org/images/2021/12/25/0HQBuu.jpg",
                            RewardItems = new List<RewardItem>
                            {
                                new RewardItem
                                {
                                    Shortname = "sulfur",
                                    Blueprint = false,
                                    IsCommand = false,
                                    Amount = 10,
                                    Rarity = "rare",
                                    DisplayName = "Сера"
                                },
                                new RewardItem
                                {
                                    Shortname = "rifle.ak",
                                    Blueprint = true,
                                    IsCommand = false,
                                    Amount = 1,
                                    Rarity = "epic",
                                    DisplayName = "Рецепт АК"
                                },
                                new RewardItem
                                {
                                    Shortname = "kick userid",
                                    Blueprint = false,
                                    IsCommand = true,
                                    Amount = 1,
                                    Rarity = "legendary",
                                    Image = "https://i.ibb.co/GdwsKkL/BP-For-Test.png",
                                    DisplayName = "Кик"
                                },
                            }
                        },
                        new Mercenary
                        {
                            Key = "free",
                            DisplayName = "Негр",
                            Cost = 500,
                            Time = 30,
                            Buy = 0,
                            url = "https://gspics.org/images/2021/12/25/0HQViK.jpg",
                            RewardItems = new List<RewardItem>
                            {
                                new RewardItem
                                {
                                    Shortname = "sulfur",
                                    Blueprint = false,
                                    IsCommand = false,
                                    Amount = 10,
                                    Rarity = "rare",
                                    DisplayName = "Сера"
                                },
                                new RewardItem
                                {
                                    Shortname = "rifle.ak",
                                    Blueprint = true,
                                    IsCommand = false,
                                    Amount = 1,
                                    Rarity = "epic",
                                    DisplayName = "Рецепт АК"
                                },
                                new RewardItem
                                {
                                    Shortname = "kick userid",
                                    Blueprint = false,
                                    IsCommand = true,
                                    Amount = 1,
                                    Rarity = "legendary",
                                    Image = "https://i.ibb.co/GdwsKkL/BP-For-Test.png",
                                    DisplayName = "Кик"
                                },
                            }
                        },
                    },
                    Rarity = new Dictionary<string, double>
                    {
                        ["null"] = 20,
                        ["default"] = 40,
                        ["rare"] = 30,
                        ["epic"] = 15,
                        ["legendary"] = 5
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
        #endregion

        #region UI
        private string _layer = "MercUI";

        void MercUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, _layer);
            if ((string)BMenu.Call("GetPage", player.userID) != "merc")
                return;


                var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = {Color = "0 0 0 0"},
                RectTransform = {AnchorMin = "0 0",AnchorMax = "1 1"}
            }, "SubContent_UI", _layer);
            double height = 100;
            double maxy = 600;

            foreach (var mercenary in config.Mercenaries)
            {
                container.Add(new CuiPanel
                {
                    Image = {Color = "0 0 0 0"},
                    RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = $"0 {maxy-height}",OffsetMax = $"920 {maxy}"}
                }, _layer, _layer + $".Merc.{mercenary.Key}");
                container.Add(new CuiPanel
                {
                    Image = {Color = "0.24 0.24 0.24 1"},
                    RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = $"0 40",OffsetMax = $"920 125"}
                },_layer,_layer+".Info");
                container.Add(new CuiElement
                {
                    Name = _layer + ".Info" + ".Outline",
                    Parent = _layer + ".Info",
                    Components =
                    {
                        new CuiOutlineComponent
                            {Color = "0.69 0.84 0.95 1", Distance = "2 2", UseGraphicAlpha = true},
                        new CuiImageComponent{Color = "0 0 0 0.5"},
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                    }
                });
                container.Add(new CuiElement
                {
                    Name = _layer + ".Info" + ".Outline",
                    Parent = _layer + ".Info",
                    Components =
                    {
                        
                        new CuiImageComponent{Color = "0.11 0.17 0.27 1"},
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                    }
                });
                
                container.Add(new CuiLabel
                {
                    Text = {Font = "robotocondensed-regular.ttf", Text = "<color=#DDDEE0>ИНФОРМАЦИЯ:\n• Каждая структура специализируется на определенных вещах, имеет разную стоимость и время производства!\n• Постройки производят предметы, технологии или чертежи прямиком в ваше хранилище! При конце производства вы получите текстовое и звуковое уведомление.</color>\n",FontSize = 16},
                   RectTransform = {AnchorMin = "0.03 0.0", AnchorMax = "0.9 0.97", OffsetMin = "0 0", OffsetMax = "0 0"}
                }, _layer + ".Info", _layer + ".Info" + ".Text");
                container.Add(new CuiElement
                {
                    Parent = _layer + $".Merc.{mercenary.Key}",
                    Name = _layer + $".Merc.{mercenary.Key}" + ".BG",
                    Components =
                    {
                        new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage", "mercbg")},
                        new CuiRectTransformComponent
                            {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0"}
                    }
                });
                
                container.Add(new CuiElement
                {
                    Parent = _layer + $".Merc.{mercenary.Key}",
                    Name = _layer + $".Merc.{mercenary.Key}" + ".Img",
                    Components =
                    {
                        new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage", mercenary.Key)},
                        new CuiRectTransformComponent
                            {AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0 0", OffsetMax = "100 100"}
                    }
                });
                
                container.Add(new CuiLabel
                    {
                        Text = {FontSize = 12, Font = "robotocondensed-regular.ttf",Text = $"",Align = TextAnchor.MiddleLeft,Color = "0.929 0.882 0.847 0.7"},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "105 80",OffsetMax = "300 105"}
                    }, _layer + $".Merc.{mercenary.Key}",
                    _layer + $".Merc.{mercenary.Key}" + ".CostBuy");
                container.Add(new CuiLabel
                    {
                        Text = {FontSize = 12, Font = "robotocondensed-regular.ttf",Text = $"Цена производства: <b><color=#EBD4AE>{mercenary.Cost}</color> <color=#EDE1D8>RP</color></b>",Align = TextAnchor.MiddleLeft,Color = "0.929 0.882 0.847 0.7"},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "105 62",OffsetMax = "300 87"}
                    }, _layer + $".Merc.{mercenary.Key}",
                    _layer + $".Merc.{mercenary.Key}" + ".CostBuy");
                container.Add(new CuiLabel
                    {
                        Text = {FontSize = 12, Font = "robotocondensed-regular.ttf",Text = $"Время производства: <b><color=#EDE1D8>{mercenary.Time} сек</color></b>",Align = TextAnchor.MiddleLeft,Color = "0.929 0.882 0.847 0.7"},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "105 39",OffsetMax = "300 87"}
                    }, _layer + $".Merc.{mercenary.Key}",
                    _layer + $".Merc.{mercenary.Key}" + ".CostBuy");
                container.Add(new CuiLabel
                    {
                        Text = {FontSize = 12, Font = "robotocondensed-regular.ttf",Text = $"",Align = TextAnchor.MiddleLeft,Color = "0.929 0.882 0.847 0.7"},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "105 39",OffsetMax = "300 72"}
                    }, _layer + $".Merc.{mercenary.Key}",
                    _layer + $".Merc.{mercenary.Key}" + ".CostBuy");
                container.Add(new CuiButton
                    {
                        Button = {Color = HexToCuiColor("#34405e"),Close = _layer,Command = $"mercreward {mercenary.Key}",Material = "assets/icons/greyout.mat"},
                        Text = {Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf",FontSize = 12,Text = "ПРЕДМЕТЫ"},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "105 5",OffsetMax = "250 45"}
                    }, _layer + $".Merc.{mercenary.Key}",
                    _layer + $".Merc.{mercenary.Key}" + ".Btn");
                container.Add(new CuiLabel
                    {
                        Text = {FontSize = 20, Font = "robotocondensed-bold.ttf",Text = $"<color=#EDE1D8>ИНВЕНТАРЬ</color>",Align = TextAnchor.MiddleLeft},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "415 65",OffsetMax = "610 95"}
                    }, _layer + $".Merc.{mercenary.Key}",
                    _layer + $".Merc.{mercenary.Key}" + ".InvLabel");
                

                #region inv1
                container.Add(new CuiElement
                {
                    Parent = _layer + $".Merc.{mercenary.Key}",
                    Name = _layer + $".Merc.{mercenary.Key}"+"InvImage1",
                    Components =
                    {
                        new CuiImageComponent{Color = HexToCuiColor("#33322d")},
                        new CuiRectTransformComponent{AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "360 10",OffsetMax = "410 60"}
                    }
                });
                
                container.Add(new CuiElement
                {
                    Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage1",
                    Name = _layer + $".Merc.{mercenary.Key}"+"InvImage1"+".Item",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = (string) ImageLibrary.Call("GetImage","ibg")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",AnchorMax = "1 1"
                        }
                    }
                });
                
                if (_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[0] == null)
                {
                    container.Add(new CuiElement
                    {
                        Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage1",
                        Name = _layer + $".Merc.{mercenary.Key}"+"InvImage1"+".Item",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = (string) ImageLibrary.Call("GetImage","inv")
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",AnchorMax = "1 1"
                            }
                        }
                    });
                    
                }
                else
                {
                    if (_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[0].Blueprint)
                    {
                        container.Add(new CuiElement
                        {
                            Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage1",
                            Name = _layer + $".Merc.{mercenary.Key}"+"InvImage1"+".Item",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) ImageLibrary.Call("GetImage","bp")
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "1 1"
                                }
                            }
                        });
                    }

                    if (!_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[0].IsCommand)
                    {
                        container.Add(new CuiElement
                        {
                            Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage1",
                            Name = _layer + $".Merc.{mercenary.Key}"+"InvImage1"+".Item",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) ImageLibrary.Call("GetImage",_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[0].Shortname)
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "1 1"
                                }
                            }
                        });
                    }
                    else
                    {
                        container.Add(new CuiElement
                        {
                            Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage1",
                            Name = _layer + $".Merc.{mercenary.Key}"+"InvImage1"+".Item",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) ImageLibrary.Call("GetImage",_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[0].Image)
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "1 1"
                                }
                            }
                        });
                    }
                    
                    container.Add(new CuiPanel
                        {
                            Image = {Color = HexToCuiColor(rarityColors[_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[0].Rarity])},
                            RectTransform = {AnchorMin = "0 0",AnchorMax = "1 0",OffsetMin = "0 0",OffsetMax = $"0 10"}
                        }, _layer + $".Merc.{mercenary.Key}"+"InvImage1"+".Item",
                        _layer + $".Merc.{mercenary.Key}"+"InvImage1"+".Item" + ".Rare");
                }
                

                #endregion
                

                #region inv2

                container.Add(new CuiElement
                {
                    Parent = _layer + $".Merc.{mercenary.Key}",
                    Name = _layer + $".Merc.{mercenary.Key}"+"InvImage2",
                    Components =
                    {
                        new CuiImageComponent{Color = HexToCuiColor("#636365")},
                        new CuiRectTransformComponent{AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "415 10",OffsetMax = "465 60"}
                    }
                });
                
                container.Add(new CuiElement
                {
                    Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage2",
                    Name = _layer + $".Merc.{mercenary.Key}"+"InvImage2"+".Item",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = (string) ImageLibrary.Call("GetImage","ibg")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",AnchorMax = "1 1"
                        }
                    }
                });
                
                if (_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[1] == null)
                {
                    container.Add(new CuiElement
                    {
                        Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage2",
                        Name = _layer + $".Merc.{mercenary.Key}"+"InvImage2"+".Item",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = (string) ImageLibrary.Call("GetImage","inv")
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",AnchorMax = "1 1"
                            }
                        }
                    });
                }
                else
                {
                    if (_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[1].Blueprint)
                    {
                        container.Add(new CuiElement
                        {
                            Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage2",
                            Name = _layer + $".Merc.{mercenary.Key}"+"InvImage2"+".Item",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) ImageLibrary.Call("GetImage","bp")
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "1 1"
                                }
                            }
                        });
                    }

                    if (!_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[1].IsCommand)
                    {
                        container.Add(new CuiElement
                        {
                            Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage2",
                            Name = _layer + $".Merc.{mercenary.Key}"+"InvImage2"+".Item",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) ImageLibrary.Call("GetImage",_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[1].Shortname)
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "1 1"
                                }
                            }
                        });
                    }
                    else
                    {
                        container.Add(new CuiElement
                        {
                            Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage2",
                            Name = _layer + $".Merc.{mercenary.Key}"+"InvImage2"+".Item",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) ImageLibrary.Call("GetImage",_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[1].Image)
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "1 1"
                                }
                            }
                        });
                    }
                    
                    container.Add(new CuiPanel
                        {
                            Image = {Color = HexToCuiColor(rarityColors[_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[1].Rarity])},
                            RectTransform = {AnchorMin = "0 0",AnchorMax = "1 0",OffsetMin = "0 0",OffsetMax = $"0 10"}
                        }, _layer + $".Merc.{mercenary.Key}"+"InvImage2"+".Item",
                        _layer + $".Merc.{mercenary.Key}"+"InvImage2"+".Item" + ".Rare");
                }
                
                #endregion

                #region inv3

                container.Add(new CuiElement
                {
                    Parent = _layer + $".Merc.{mercenary.Key}",
                    Name = _layer + $".Merc.{mercenary.Key}"+"InvImage3",
                    Components =
                    {
                        new CuiImageComponent{Color = HexToCuiColor("#33322d")},
                        new CuiRectTransformComponent{AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "470 10",OffsetMax = "520 60"}
                    }
                });
                
                container.Add(new CuiElement
                {
                    Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage3",
                    Name = _layer + $".Merc.{mercenary.Key}"+"InvImage3"+".Item",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = (string) ImageLibrary.Call("GetImage","ibg")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",AnchorMax = "1 1"
                        }
                    }
                });
                
                if (_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[2] == null)
                {
                    container.Add(new CuiElement
                    {
                        Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage3",
                        Name = _layer + $".Merc.{mercenary.Key}"+"InvImage3"+".Item",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = (string) ImageLibrary.Call("GetImage","inv")
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",AnchorMax = "1 1"
                            }
                        }
                    });
                }
                else
                {
                    if (_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[2].Blueprint)
                    {
                        container.Add(new CuiElement
                        {
                            Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage3",
                            Name = _layer + $".Merc.{mercenary.Key}"+"InvImage3"+".Item",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) ImageLibrary.Call("GetImage","bp")
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "1 1"
                                }
                            }
                        });
                    }

                    if (!_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[2].IsCommand)
                    {
                        container.Add(new CuiElement
                        {
                            Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage3",
                            Name = _layer + $".Merc.{mercenary.Key}"+"InvImage3"+".Item",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) ImageLibrary.Call("GetImage",_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[2].Shortname)
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "1 1"
                                }
                            }
                        });
                    }
                    else
                    {
                        container.Add(new CuiElement
                        {
                            Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage3",
                            Name = _layer + $".Merc.{mercenary.Key}"+"InvImage3"+".Item",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) ImageLibrary.Call("GetImage",_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[2].Image)
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "1 1"
                                }
                            }
                        });
                    }
                    
                    container.Add(new CuiPanel
                        {
                            Image = {Color = HexToCuiColor(rarityColors[_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[2].Rarity])},
                            RectTransform = {AnchorMin = "0 0",AnchorMax = "1 0",OffsetMin = "0 0",OffsetMax = $"0 10"}
                        }, _layer + $".Merc.{mercenary.Key}"+"InvImage3"+".Item",
                        _layer + $".Merc.{mercenary.Key}"+"InvImage3"+".Item" + ".Rare");
                }

                #endregion

                #region inv4

                container.Add(new CuiElement
                {
                    Parent = _layer + $".Merc.{mercenary.Key}",
                    Name = _layer + $".Merc.{mercenary.Key}"+"InvImage4",
                    Components =
                    {
                        new CuiImageComponent{Color = HexToCuiColor("#33322d")},
                        new CuiRectTransformComponent{AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "525 10",OffsetMax = "575 60"}
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage4",
                    Name = _layer + $".Merc.{mercenary.Key}"+"InvImage4"+".Item",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = (string) ImageLibrary.Call("GetImage","ibg")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",AnchorMax = "1 1"
                        }
                    }
                });
                if (_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[3] == null)
                {
                    container.Add(new CuiElement
                    {
                        Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage4",
                        Name = _layer + $".Merc.{mercenary.Key}"+"InvImage4"+".Item",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = (string) ImageLibrary.Call("GetImage","inv")
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",AnchorMax = "1 1"
                            }
                        }
                    });
                }
                else
                {
                    if (_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[3].Blueprint)
                    {
                        container.Add(new CuiElement
                        {
                            Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage4",
                            Name = _layer + $".Merc.{mercenary.Key}"+"InvImage4"+".Item",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) ImageLibrary.Call("GetImage","bp")
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "1 1"
                                }
                            }
                        });
                    }
                    container.Add(new CuiElement
                    {
                        Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage4",
                        Name = _layer + $".Merc.{mercenary.Key}"+"InvImage4"+".Item",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = (string) ImageLibrary.Call("GetImage",_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[3].Shortname)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",AnchorMax = "1 1"
                            }
                        }
                    });
                    container.Add(new CuiPanel
                        {
                            Image = {Color = HexToCuiColor(rarityColors[_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[3].Rarity])},
                            RectTransform = {AnchorMin = "0 0",AnchorMax = "1 0",OffsetMin = "0 0",OffsetMax = $"0 10"}
                        }, _layer + $".Merc.{mercenary.Key}"+"InvImage4"+".Item",
                        _layer + $".Merc.{mercenary.Key}"+"InvImage4"+".Item" + ".Rare");
                }
                

                #endregion

                #region MercActive

                if (!_database[player.userID].Find(p => p.Key == mercenary.Key).Available)
                {
                    container.Add(new CuiElement
                    {
                        Parent = _layer + $".Merc.{mercenary.Key}" + ".Img",
                        Name = _layer + $".Merc.{mercenary.Key}" + ".Img"+".Lock",
                        Components =
                        {
                            new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage", "lock")},
                            new CuiRectTransformComponent
                                {AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0 0", OffsetMax = "100 100"}
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage4",
                        Name = _layer + $".Merc.{mercenary.Key}"+"InvImage4"+".Lock",
                        Components =
                        {
                            new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage", "lock")},
                            new CuiRectTransformComponent
                                {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0"}
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage3",
                        Name = _layer + $".Merc.{mercenary.Key}"+"InvImage3"+".Lock",
                        Components =
                        {
                            new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage", "lock")},
                            new CuiRectTransformComponent
                                {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0"}
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage2",
                        Name = _layer + $".Merc.{mercenary.Key}"+"InvImage2"+".Lock",
                        Components =
                        {
                            new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage", "lock")},
                            new CuiRectTransformComponent
                                {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0"}
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage1",
                        Name = _layer + $".Merc.{mercenary.Key}"+"InvImage1"+".Lock",
                        Components =
                        {
                            new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage", "lock")},
                            new CuiRectTransformComponent
                                {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0"}
                        }
                    });
                    container.Add(new CuiButton
                        {
                            Button = {Command = $"mercbuy {mercenary.Key}",Color = CanAccept(mercenary.Buy,player.UserIDString)?HexToCuiColor("#c27e17") : HexToCuiColor("#a1a1a1"),Material = "assets/icons/greyout.mat"},
                            Text = {Align = TextAnchor.MiddleCenter,FontSize = 18,Text = CanAccept(mercenary.Buy,player.UserIDString)?"АКТИВИРОВАТЬ":"НЕДОСТУПНО"},
                            RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "785 15",OffsetMax = "915 85"}
                        }, _layer + $".Merc.{mercenary.Key}",
                        _layer + $".Merc.{mercenary.Key}" + ".Btn");
                }
                else
                {
                    if (_database[player.userID].Find(p => p.Key == mercenary.Key).Active)
                    {
                        container.Add(new CuiPanel
                        {
                            Image = {Color = HexToCuiColor("#763f34"),Material = "assets/icons/greyout.mat"},
                            RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "600 20",OffsetMax = "750 40"}
                        },_layer + $".Merc.{mercenary.Key}",_layer + $".Merc.{mercenary.Key}"+".BarPanel");
                        container.Add(new CuiPanel
                        {
                            Image = {Color = HexToCuiColor("#2B6992"),Material = "assets/icons/greyout.mat"},
                            RectTransform = {AnchorMin = "0 0",AnchorMax = $"{1-GetTime(_database[player.userID].Find(m => m.Key == mercenary.Key).Cooldown-CurTime()).TotalSeconds/mercenary.Time} 0",OffsetMin = "0 0",OffsetMax = "0 20"}
                        },_layer + $".Merc.{mercenary.Key}"+".BarPanel",_layer + $".Merc.{mercenary.Key}"+".BarPanel"+".Progress");
                        
                        container.Add(new CuiElement
                        {
                            Parent = _layer + $".Merc.{mercenary.Key}",
                            Name = _layer + $".Merc.{mercenary.Key}"+".BarImage",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) ImageLibrary.Call("GetImage","run")
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "600 45",OffsetMax = "640 85"
                                }
                            }
                        });
                        container.Add(new CuiLabel
                            {
                                Text = {Align = TextAnchor.MiddleCenter,Text = $"{FormatTime(GetTime(_database[player.userID].Find(m => m.Key == mercenary.Key).Cooldown-CurTime()))}",FontSize = 16},
                                RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "645 45",OffsetMax = "720 85"}
                            }, _layer + $".Merc.{mercenary.Key}",
                            _layer + $".Merc.{mercenary.Key}" + $".Time");
                        container.Add(new CuiButton
                            {
                                Button = {Command = $"",Color = HexToCuiColor("#763f34"),Material = "assets/icons/greyout.mat"},
                                Text = {Align = TextAnchor.MiddleCenter,FontSize = 20,Text = "На вылазке"},
                                RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "785 15",OffsetMax = "915 85"}
                            }, _layer + $".Merc.{mercenary.Key}",
                            _layer + $".Merc.{mercenary.Key}" + ".Btn");
                    }
                    else
                    {
                        container.Add(new CuiLabel
                            {
                                Text = {Align = TextAnchor.MiddleCenter,Text =(CurTime()>_database[player.userID].Find(m => m.Key == mercenary.Key).Cooldown)? "Готов":$"{FormatTime(GetTime(_database[player.userID].Find(m => m.Key == mercenary.Key).Cooldown-CurTime()))}",FontSize = 16},
                                RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "645 45",OffsetMax = "720 85"}
                            }, _layer + $".Merc.{mercenary.Key}",
                            _layer + $".Merc.{mercenary.Key}" + $".Time");
                        container.Add(new CuiElement
                        {
                            Parent = _layer + $".Merc.{mercenary.Key}",
                            Name = _layer + $".Merc.{mercenary.Key}"+".BarImage",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) ImageLibrary.Call("GetImage","zzz")
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "600 45",OffsetMax = "640 85"
                                }
                            }
                        });
                        container.Add(new CuiButton
                            {
                                Button = {Color = (CanAccept(mercenary.Cost,player.UserIDString)&&_database[player.userID].Find(m => m.Key == mercenary.Key).Cooldown<=CurTime())?HexToCuiColor("#2B6992") : HexToCuiColor("#a1a1a1"),Command = $"mercstart {mercenary.Key}",Material = "assets/icons/greyout.mat"},
                                Text = {Align = TextAnchor.MiddleCenter,FontSize = 18,Text = "НАЧАТЬ ПРОИЗВОДСТВО"},
                                RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "785 15",OffsetMax = "915 85"}
                            }, _layer + $".Merc.{mercenary.Key}",
                            _layer + $".Merc.{mercenary.Key}" + ".Btn");
                    }
                }
                container.Add(new CuiPanel
                    {
                        Image = {Color = "0 0 0 0.98"},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 0", OffsetMax = "100 20"}
                    }, _layer + $".Merc.{mercenary.Key}" + ".Img",
                    _layer + $".Merc.{mercenary.Key}" + ".Img" + ".Name");
                container.Add(new CuiLabel
                    {
                        Text = {FontSize = 12,Text = mercenary.DisplayName,Align = TextAnchor.MiddleCenter},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "1 1"}
                    }, _layer + $".Merc.{mercenary.Key}" + ".Img" + ".Name",
                    _layer + $".Merc.{mercenary.Key}" + ".Img" + ".Name" + ".Text");
                
                if (HasItems(player,mercenary))
                {
                    container.Add(new CuiButton
                        {
                            Button = {Color = HexToCuiColor("#34405e"),Command = $"takereward {mercenary.Key}",Material = "assets/icons/greyout.mat"},
                            Text = {Align = TextAnchor.MiddleCenter,Text = "ЗАБРАТЬ",FontSize = 12},
                            RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "255 5",OffsetMax = "325 45"}
                        }, _layer + $".Merc.{mercenary.Key}",
                        _layer + $".Merc.{mercenary.Key}" + ".Take");
                }
                
                maxy -= height + 15;

                #endregion
            }
            CuiHelper.AddUi(player, container);
        }

        void RewardUI(BasePlayer player, string merc,int page = 0)
        {
            Mercenary mercenary = config.Mercenaries.Find(p => p.Key == merc);
            
            if (merc == null) return;
            if ((string)BMenu.Call("GetPage", player.userID) != "merc")
            {
                PrintError("Notmerc");
                return;
            }
            
            CuiHelper.DestroyUi(player, _layer);
            
            var container = new CuiElementContainer();
            
            container.Add(new CuiPanel
            {
                Image = {Color = "0 0 0 0"},
                RectTransform = {AnchorMin = "0 0",AnchorMax = "1 1"}
            }, "SubContent_UI", _layer);
            
            container.Add(new CuiPanel
            {
                Image = {Color = "0 0 0 0"},
                RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = $"0 500",OffsetMax = $"920 600"}
            }, _layer, _layer + $".Merc.{mercenary.Key}");
            
            /*container.Add(new CuiElement
            {
                Parent = _layer + $".Merc.{mercenary.Key}",
                Name = _layer + $".Merc.{mercenary.Key}" + ".BG",
                Components =
                {
                    new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage", "red")},
                    new CuiRectTransformComponent
                        {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0"}
                }
            });*/
            
            container.Add(new CuiElement
            {
                Parent = _layer + $".Merc.{mercenary.Key}",
                Name = _layer + $".Merc.{mercenary.Key}" + ".Img",
                Components =
                {
                    new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage", mercenary.Key)},
                    new CuiRectTransformComponent
                        {AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0 0", OffsetMax = "100 100"}
                }
            });
            
             container.Add(new CuiLabel
                    {
                        Text = {FontSize = 20, Font = "robotocondensed-bold.ttf",Text = $"<color=#EDE1D8>ИНВЕНТАРЬ</color>",Align = TextAnchor.MiddleLeft},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "415 65",OffsetMax = "610 95"}
                    }, _layer + $".Merc.{mercenary.Key}",
                    _layer + $".Merc.{mercenary.Key}" + ".InvLabel");
            
                container.Add(new CuiElement
                {
                    Parent = _layer + $".Merc.{mercenary.Key}",
                    Name = _layer + $".Merc.{mercenary.Key}"+"InvImage1",
                    Components =
                    {
                        new CuiImageComponent{Color = HexToCuiColor("#33322d")},
                        new CuiRectTransformComponent{AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "360 10",OffsetMax = "410 60"}
                    }
                });
                
                container.Add(new CuiElement
                {
                    Parent = _layer + $".Merc.{mercenary.Key}",
                    Name = _layer + $".Merc.{mercenary.Key}"+"InvImage2",
                    Components =
                    {
                        new CuiImageComponent{Color = HexToCuiColor("#33322d")},
                        new CuiRectTransformComponent{AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "415 10",OffsetMax = "465 60"}
                    }
                });
                
                container.Add(new CuiElement
                {
                    Parent = _layer + $".Merc.{mercenary.Key}",
                    Name = _layer + $".Merc.{mercenary.Key}"+"InvImage3",
                    Components =
                    {
                        new CuiImageComponent{Color = HexToCuiColor("#33322d")},
                        new CuiRectTransformComponent{AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "470 10",OffsetMax = "520 60"}
                    }
                });
                
                container.Add(new CuiElement
                {
                    Parent = _layer + $".Merc.{mercenary.Key}",
                    Name = _layer + $".Merc.{mercenary.Key}"+"InvImage4",
                    Components =
                    {
                        new CuiImageComponent{Color = HexToCuiColor("#33322d")},
                        new CuiRectTransformComponent{AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "525 10",OffsetMax = "575 60"}
                    }
                });
                container.Add(new CuiPanel
                {
                    Image = {Color = HexToCuiColor("#33322d")},
                    RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 480",OffsetMax = "920 483"}
                }, _layer, _layer + ".line");
                
                container.Add(new CuiLabel
                {
                    Text = {Align = TextAnchor.MiddleCenter,FontSize = 16,Text = "Возможные предметы"},
                    RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 450",OffsetMax = "920 480"}
                }, _layer, _layer + ".ChanceTitle");
                
                container.Add(new CuiPanel
                {
                    Image = {Color = HexToCuiColor("#33322d")},
                    RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 447",OffsetMax = "920 450"}
                }, _layer, _layer + ".lined");

                
                
                container.Add(new CuiPanel
                {
                    Image = {Color = "0 0 0 0"},
                    RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 0",OffsetMax = "920 435"}
                }, _layer, _layer + ".Items");
                /*container.Add(new CuiElement
                {
                    Parent = _layer + ".Items",
                    Name = _layer + ".Items"+".BG",
                    Components = {new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage", "grey")},
                        new CuiRectTransformComponent
                            {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0"}}
                });*/

                container.Add(new CuiButton
                {
                    Button = {Color = page >0 ? HexToCuiColor("#34405e"):HexToCuiColor("#34405e"),Command = page >0 ? $"mercreward {merc} {page-1}":"chat.say /merc",Material = "assets/icons/greyout.mat"},
                    Text = {Text = "← НАЗАД",Color = page >0 ? HexToCuiColor("#ffffff"):HexToCuiColor("#ffffff"),Align = TextAnchor.MiddleCenter,FontSize = 24},
                    RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "10 20",OffsetMax = "187 60"}
                }, _layer, _layer + ".Btn");

                if (mercenary.RewardItems.Skip(page*40).Count() > 40)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "680 20",OffsetMax = "853 60"},
                        Button = { Color = HexToCuiColor("#34405e"), Command = $"mercreward {merc} {page+1}",Material = "assets/icons/greyout.mat" },
                        Text = { Text = "<b>ВПЕРЁД →</b>", Align = TextAnchor.MiddleCenter, FontSize = 24, Font = "robotocondensed-regular.ttf",Color = HexToCuiColor("#ffffff") }
                    },  _layer, _layer + ".Forward");
                }
                
                int index = 0;
                int y = 1;
                double height = 80;
                
                foreach (var item in mercenary.RewardItems.Skip(page*40).Take(40))
                {
                    
                    if (index>=10)
                    {
                        y++;
                        index = 0;
                    }
                    container.Add(new CuiPanel
                    {
                        //Image = {Color = HexToCuiColor("#888888")},
                        Image = {Color = "0 0 0 0"},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = $"{5*index+index*height+10} {435-height*y-y*5}",OffsetMax = $"{5*index+index*height+height} {465-height*y-y*5+height}"}
                    }, _layer + ".Items", _layer + ".Items" + $"{index}" + $"{y}");
                    container.Add(new CuiElement
                    {
                        Parent = _layer + ".Items" + $"{index}" + $"{y}",
                        Name = _layer + ".Items" + $"{index}" + $"{y}"+".BG",
                        Components = {new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage", "ibg")},
                            new CuiRectTransformComponent
                                {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 0",OffsetMax = $"{height} {height}"}}
                    });
                    
                    if (item.Blueprint)
                    {
                        container.Add(new CuiElement
                        {
                            Parent = _layer + ".Items" + $"{index}" + $"{y}",
                            Name = _layer + ".Items" + $"{index}" + $"{y}"+".Bluprint",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string)ImageLibrary.Call("GetImage","bp"),
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 0",OffsetMax = $"{height} {height}"
                                }
                            }
                        });
                    }

                    if (item.IsCommand)
                    {
                        container.Add(new CuiElement
                        {
                            Parent = _layer + ".Items" + $"{index}" + $"{y}",
                            Name = _layer + ".Items" + $"{index}" + $"{y}"+".Img",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string)ImageLibrary.Call("GetImage",item.Image),
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 0",OffsetMax = $"{height} {height}"
                                }
                            }
                        });
                    }
                    else
                    {
                        container.Add(new CuiElement
                        {
                            Parent = _layer + ".Items" + $"{index}" + $"{y}",
                            Name = _layer + ".Items" + $"{index}" + $"{y}"+".Img",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string)ImageLibrary.Call("GetImage",item.Shortname),
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 0",OffsetMax = $"{height} {height}"
                                }
                            }
                        });
                    }

                    container.Add(new CuiPanel
                        {
                            Image = {Color = HexToCuiColor(rarityColors[item.Rarity])},
                            RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 0",OffsetMax = $"{height} 20"}
                        }, _layer + ".Items" + $"{index}" + $"{y}",
                        _layer + ".Items" + $"{index}" + $"{y}" + ".Rare");
                    container.Add(new CuiLabel
                        {
                            Text = {Align = TextAnchor.MiddleCenter,FontSize = 12,Text = item.DisplayName}
                        }, _layer + ".Items" + $"{index}" + $"{y}" + ".Rare",
                        _layer + ".Items" + $"{index}" + $"{y}" + ".Text");
                    index++;
                }
                
                #region inv1
                container.Add(new CuiElement
                {
                    Parent = _layer + $".Merc.{mercenary.Key}",
                    Name = _layer + $".Merc.{mercenary.Key}"+"InvImage1",
                    Components =
                    {
                        new CuiImageComponent{Color = "0 0 0 0"},
                        new CuiRectTransformComponent{AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "360 10",OffsetMax = "410 60"}
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage1",
                    Name = _layer + $".Merc.{mercenary.Key}"+"InvImage1"+".Item",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = (string) ImageLibrary.Call("GetImage","ibg")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",AnchorMax = "1 1"
                        }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage1",
                    Name = _layer + $".Merc.{mercenary.Key}"+"InvImage1"+".Item",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = (string) ImageLibrary.Call("GetImage","ibg")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",AnchorMax = "1 1"
                        }
                    }
                });
                if (_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[0] == null)
                {
                    container.Add(new CuiElement
                    {
                        Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage1",
                        Name = _layer + $".Merc.{mercenary.Key}"+"InvImage1"+".Item",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = (string) ImageLibrary.Call("GetImage","inv")
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",AnchorMax = "1 1"
                            }
                        }
                    });
                   
                }
                else
                {
                    if (_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[0].Blueprint)
                    {
                        container.Add(new CuiElement
                        {
                            Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage1",
                            Name = _layer + $".Merc.{mercenary.Key}"+"InvImage1"+".Item",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) ImageLibrary.Call("GetImage","bp")
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "1 1"
                                }
                            }
                        });
                    }

                    if (!_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[0].IsCommand)
                    {
                        container.Add(new CuiElement
                        {
                            Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage1",
                            Name = _layer + $".Merc.{mercenary.Key}"+"InvImage1"+".Item",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) ImageLibrary.Call("GetImage",_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[0].Shortname)
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "1 1"
                                }
                            }
                        });
                    }
                    else
                    {
                        container.Add(new CuiElement
                        {
                            Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage1",
                            Name = _layer + $".Merc.{mercenary.Key}"+"InvImage1"+".Item",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) ImageLibrary.Call("GetImage",_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[0].Image)
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "1 1"
                                }
                            }
                        });
                    }
                    
                    container.Add(new CuiPanel
                        {
                            Image = {Color = HexToCuiColor(rarityColors[_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[0].Rarity])},
                            RectTransform = {AnchorMin = "0 0",AnchorMax = "1 0",OffsetMin = "0 0",OffsetMax = $"0 10"}
                        }, _layer + $".Merc.{mercenary.Key}"+"InvImage1"+".Item",
                        _layer + $".Merc.{mercenary.Key}"+"InvImage1"+".Item" + ".Rare");
                }
                

                #endregion
                

                #region inv2

                container.Add(new CuiElement
                {
                    Parent = _layer + $".Merc.{mercenary.Key}",
                    Name = _layer + $".Merc.{mercenary.Key}"+"InvImage2",
                    Components =
                    {
                        new CuiImageComponent{Color = HexToCuiColor("#33322d")},
                        new CuiRectTransformComponent{AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "415 10",OffsetMax = "465 60"}
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage2",
                    Name = _layer + $".Merc.{mercenary.Key}"+"InvImage2"+".Item",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = (string) ImageLibrary.Call("GetImage","ibg")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",AnchorMax = "1 1"
                        }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage2",
                    Name = _layer + $".Merc.{mercenary.Key}"+"InvImage2"+".Item",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = (string) ImageLibrary.Call("GetImage","ibg")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",AnchorMax = "1 1"
                        }
                    }
                });
                
                if (_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[1] == null)
                {
                    container.Add(new CuiElement
                    {
                        Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage2",
                        Name = _layer + $".Merc.{mercenary.Key}"+"InvImage2"+".Item",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = (string) ImageLibrary.Call("GetImage","inv")
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",AnchorMax = "1 1"
                            }
                        }
                    });
                }
                else
                {
                    if (_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[1].Blueprint)
                    {
                        container.Add(new CuiElement
                        {
                            Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage2",
                            Name = _layer + $".Merc.{mercenary.Key}"+"InvImage2"+".Item",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) ImageLibrary.Call("GetImage","bp")
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "1 1"
                                }
                            }
                        });
                    }

                    if (!_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[1].IsCommand)
                    {
                        container.Add(new CuiElement
                        {
                            Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage2",
                            Name = _layer + $".Merc.{mercenary.Key}"+"InvImage2"+".Item",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) ImageLibrary.Call("GetImage",_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[1].Shortname)
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "1 1"
                                }
                            }
                        });
                    }
                    else
                    {
                        container.Add(new CuiElement
                        {
                            Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage2",
                            Name = _layer + $".Merc.{mercenary.Key}"+"InvImage2"+".Item",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) ImageLibrary.Call("GetImage",_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[1].Image)
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "1 1"
                                }
                            }
                        });
                    }
                    
                    container.Add(new CuiPanel
                        {
                            Image = {Color = HexToCuiColor(rarityColors[_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[1].Rarity])},
                            RectTransform = {AnchorMin = "0 0",AnchorMax = "1 0",OffsetMin = "0 0",OffsetMax = $"0 10"}
                        }, _layer + $".Merc.{mercenary.Key}"+"InvImage2"+".Item",
                        _layer + $".Merc.{mercenary.Key}"+"InvImage2"+".Item" + ".Rare");
                }
                
                #endregion

                #region inv3

                container.Add(new CuiElement
                {
                    Parent = _layer + $".Merc.{mercenary.Key}",
                    Name = _layer + $".Merc.{mercenary.Key}"+"InvImage3",
                    Components =
                    {
                        new CuiImageComponent{Color = "0 0 0 0"},
                        new CuiRectTransformComponent{AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "470 10",OffsetMax = "520 60"}
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage3",
                    Name = _layer + $".Merc.{mercenary.Key}"+"InvImage3"+".Item",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = (string) ImageLibrary.Call("GetImage","ibg")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",AnchorMax = "1 1"
                        }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage3",
                    Name = _layer + $".Merc.{mercenary.Key}"+"InvImage3"+".Item",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = (string) ImageLibrary.Call("GetImage","ibg")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",AnchorMax = "1 1"
                        }
                    }
                });
                if (_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[2] == null)
                {
                    container.Add(new CuiElement
                    {
                        Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage3",
                        Name = _layer + $".Merc.{mercenary.Key}"+"InvImage3"+".Item",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = (string) ImageLibrary.Call("GetImage","inv")
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",AnchorMax = "1 1"
                            }
                        }
                    });
                }
                else
                {
                    if (_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[2].Blueprint)
                    {
                        container.Add(new CuiElement
                        {
                            Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage3",
                            Name = _layer + $".Merc.{mercenary.Key}"+"InvImage3"+".Item",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) ImageLibrary.Call("GetImage","bp")
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "1 1"
                                }
                            }
                        });
                    }

                    if (!_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[2].IsCommand)
                    {
                        container.Add(new CuiElement
                        {
                            Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage3",
                            Name = _layer + $".Merc.{mercenary.Key}"+"InvImage3"+".Item",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) ImageLibrary.Call("GetImage",_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[2].Shortname)
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "1 1"
                                }
                            }
                        });
                    }
                    else
                    {
                        container.Add(new CuiElement
                        {
                            Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage3",
                            Name = _layer + $".Merc.{mercenary.Key}"+"InvImage3"+".Item",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) ImageLibrary.Call("GetImage",_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[2].Image)
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "1 1"
                                }
                            }
                        });
                    }
                    
                    container.Add(new CuiPanel
                        {
                            Image = {Color = HexToCuiColor(rarityColors[_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[2].Rarity])},
                            RectTransform = {AnchorMin = "0 0",AnchorMax = "1 0",OffsetMin = "0 0",OffsetMax = $"0 10"}
                        }, _layer + $".Merc.{mercenary.Key}"+"InvImage3"+".Item",
                        _layer + $".Merc.{mercenary.Key}"+"InvImage3"+".Item" + ".Rare");
                }

                #endregion

                #region inv4

                container.Add(new CuiElement
                {
                    Parent = _layer + $".Merc.{mercenary.Key}",
                    Name = _layer + $".Merc.{mercenary.Key}"+"InvImage4",
                    Components =
                    {
                        new CuiImageComponent{Color = "0 0 0 0"},
                        new CuiRectTransformComponent{AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "525 10",OffsetMax = "575 60"}
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage4",
                    Name = _layer + $".Merc.{mercenary.Key}"+"InvImage4"+".Item",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = (string) ImageLibrary.Call("GetImage","ibg")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",AnchorMax = "1 1"
                        }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage4",
                    Name = _layer + $".Merc.{mercenary.Key}"+"InvImage4"+".Item",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = (string) ImageLibrary.Call("GetImage","ibg")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",AnchorMax = "1 1"
                        }
                    }
                });
                if (_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[3] == null)
                {
                    container.Add(new CuiElement
                    {
                        Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage4",
                        Name = _layer + $".Merc.{mercenary.Key}"+"InvImage4"+".Item",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = (string) ImageLibrary.Call("GetImage","inv")
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",AnchorMax = "1 1"
                            }
                        }
                    });
                }
                else
                {
                    if (_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[3].Blueprint)
                    {
                        container.Add(new CuiElement
                        {
                            Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage4",
                            Name = _layer + $".Merc.{mercenary.Key}"+"InvImage4"+".Item",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) ImageLibrary.Call("GetImage","bp")
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "1 1"
                                }
                            }
                        });
                    }
                    container.Add(new CuiElement
                    {
                        Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage4",
                        Name = _layer + $".Merc.{mercenary.Key}"+"InvImage4"+".Item",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = (string) ImageLibrary.Call("GetImage",_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[3].Shortname)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",AnchorMax = "1 1"
                            }
                        }
                    });
                    container.Add(new CuiPanel
                        {
                            Image = {Color = HexToCuiColor(rarityColors[_database[player.userID].Find(p=> p.Key == mercenary.Key).Inventory[3].Rarity])},
                            RectTransform = {AnchorMin = "0 0",AnchorMax = "1 0",OffsetMin = "0 0",OffsetMax = $"0 10"}
                        }, _layer + $".Merc.{mercenary.Key}"+"InvImage4"+".Item",
                        _layer + $".Merc.{mercenary.Key}"+"InvImage4"+".Item" + ".Rare");
                }
                

                #endregion

                #region desc

                container.Add(new CuiLabel
                    {
                        Text = {FontSize = 12,Text = $"Цена: {mercenary.Buy}",Align = TextAnchor.MiddleLeft},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "105 75",OffsetMax = "200 95"}
                    }, _layer + $".Merc.{mercenary.Key}",
                    _layer + $".Merc.{mercenary.Key}" + ".CostBuy");
                container.Add(new CuiLabel
                    {
                        Text = {FontSize = 12,Text = $"Цена производства: {mercenary.Cost}",Align = TextAnchor.MiddleLeft},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "105 55",OffsetMax = "300 75"}
                    }, _layer + $".Merc.{mercenary.Key}",
                    _layer + $".Merc.{mercenary.Key}" + ".CostBuy");
                container.Add(new CuiLabel
                    {
                        Text = {FontSize = 12,Text = $"Время производства: {mercenary.Cooldown}",Align = TextAnchor.MiddleLeft},
                        RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "105 25",OffsetMax = "300 55"}
                    }, _layer + $".Merc.{mercenary.Key}",
                    _layer + $".Merc.{mercenary.Key}" + ".CostBuy");

                #endregion
                
                CuiHelper.AddUi(player, container);
        }

            #endregion

        #region MercHooks
        void UpdateTimer(BasePlayer player)
        {
            if ((string)BMenu.Call("GetPage", player.userID) != "merc")
                return;
            List<string>layers = new List<string>
            {
                ".Merc.lvl1" + ".Img"+".Lock",
                ".Merc.lvl2" + ".Img"+".Lock",
                ".Merc.lvl3" + ".Img"+".Lock",
                ".Merc.lvl1"+"InvImage1",
                ".Merc.lvl2"+"InvImage1",
                ".Merc.lvl3"+"InvImage1",
                ".Merc.lvl1"+"InvImage2",
                ".Merc.lvl2"+"InvImage2",
                ".Merc.lvl3"+"InvImage2",
                ".Merc.lvl1"+"InvImage3",
                ".Merc.lvl2"+"InvImage3",
                ".Merc.lvl3"+"InvImage3",
                ".Merc.lvl1"+"InvImage4",
                ".Merc.lvl2"+"InvImage4",
                ".Merc.lvl3"+"InvImage4",
                _layer + ".Merc.lvl1"+".BarPanel",
                _layer + ".Merc.lvl2"+".BarPanel",
                _layer + ".Merc.lvl3"+".BarPanel",
                _layer + ".Merc.lvl1" + ".Time",
                _layer + ".Merc.lvl2" + ".Time",
                _layer + ".Merc.lvl3" + ".Time",
                _layer + ".Merc.lvl1" + $".Time",
                _layer + ".Merc.lvl2" + $".Time",
                _layer + ".Merc.lvl3" + $".Time",
                _layer + ".Merc.lvl1"+".BarImage",
                _layer + ".Merc.lvl2"+".BarImage",
                _layer + ".Merc.lvl3"+".BarImage",
                _layer + ".Merc.free" + $".Time",
                _layer + ".Merc.free"+".BarImage",
                _layer + ".Merc.free"+".BarPanel",
                ".Merc.free" + ".Img"+".Lock",
                ".Merc.free"+"InvImage1",
                ".Merc.free"+"InvImage2",
                ".Merc.free"+"InvImage3",
                ".Merc.free"+"InvImage4",
            };
            foreach (var layer in layers)
            {
                CuiHelper.DestroyUi(player, layer);
            }
            var container = new CuiElementContainer();
            foreach (var mercenary in config.Mercenaries)
            {
                if (!_database[player.userID].Find(p => p.Key == mercenary.Key).Available)
                {
                    /*container.Add(new CuiElement
                    {
                        Parent = _layer + $".Merc.{mercenary.Key}" + ".Img",
                        Name = _layer + $".Merc.{mercenary.Key}" + ".Img"+".Lock",
                        Components =
                        {
                            new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage", "lock")},
                            new CuiRectTransformComponent
                                {AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0 0", OffsetMax = "100 100"}
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage4",
                        Name = _layer + $".Merc.{mercenary.Key}"+"InvImage4"+".Lock",
                        Components =
                        {
                            new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage", "lock")},
                            new CuiRectTransformComponent
                                {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0"}
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage3",
                        Name = _layer + $".Merc.{mercenary.Key}"+"InvImage3"+".Lock",
                        Components =
                        {
                            new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage", "lock")},
                            new CuiRectTransformComponent
                                {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0"}
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage2",
                        Name = _layer + $".Merc.{mercenary.Key}"+"InvImage2"+".Lock",
                        Components =
                        {
                            new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage", "lock")},
                            new CuiRectTransformComponent
                                {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0"}
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = _layer + $".Merc.{mercenary.Key}"+"InvImage1",
                        Name = _layer + $".Merc.{mercenary.Key}"+"InvImage1"+".Lock",
                        Components =
                        {
                            new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage", "lock")},
                            new CuiRectTransformComponent
                                {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0"}
                        }
                    });*/
                    container.Add(new CuiButton
                        {
                            Button = {Command = $"mercbuy {mercenary.Key}",Color = CanAccept(mercenary.Buy,player.UserIDString)?HexToCuiColor("#c27e17") : HexToCuiColor("#a1a1a1"),Material = "assets/icons/greyout.mat"},
                            Text = {Align = TextAnchor.MiddleCenter,FontSize = 18,Text = CanAccept(mercenary.Buy,player.UserIDString)?"АКТИВИРОВАТЬ":"НЕДОСТУПНО"},
                            RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "785 15",OffsetMax = "915 85"}
                        }, _layer + $".Merc.{mercenary.Key}",
                        _layer + $".Merc.{mercenary.Key}" + ".Btn");
                }
                else
                {
                    if (_database[player.userID].Find(p => p.Key == mercenary.Key).Active)
                    {
                        container.Add(new CuiPanel
                        {
                            Image = {Color = HexToCuiColor("#763f34"),Material = "assets/icons/greyout.mat"},
                            RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "600 20",OffsetMax = "750 40"}
                        },_layer + $".Merc.{mercenary.Key}",_layer + $".Merc.{mercenary.Key}"+".BarPanel");
                        container.Add(new CuiPanel
                        {
                            Image = {Color = HexToCuiColor("#2B6992"),Material = "assets/icons/greyout.mat"},
                            RectTransform = {AnchorMin = "0 0",AnchorMax = $"{1-GetTime(_database[player.userID].Find(m => m.Key == mercenary.Key).Cooldown-CurTime()).TotalSeconds/mercenary.Time} 0",OffsetMin = "0 0",OffsetMax = "0 20"}
                        },_layer + $".Merc.{mercenary.Key}"+".BarPanel",_layer + $".Merc.{mercenary.Key}"+".BarPanel"+".Progress");
                       
                        container.Add(new CuiElement
                        {
                            Parent = _layer + $".Merc.{mercenary.Key}",
                            Name = _layer + $".Merc.{mercenary.Key}"+".BarImage",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) ImageLibrary.Call("GetImage","run")
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "600 45",OffsetMax = "640 85"
                                }
                            }
                        });
                        container.Add(new CuiLabel
                            {
                                Text = {Align = TextAnchor.MiddleCenter,Text = $"{FormatTime(GetTime(_database[player.userID].Find(m => m.Key == mercenary.Key).Cooldown-CurTime()))}",FontSize = 16},
                                RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "645 45",OffsetMax = "720 85"}
                            }, _layer + $".Merc.{mercenary.Key}",
                            _layer + $".Merc.{mercenary.Key}" + $".Time");
                        container.Add(new CuiButton
                            {
                                Button = {Command = $"",Color = HexToCuiColor("#763f34"),Material = "assets/icons/greyout.mat"},
                                Text = {Align = TextAnchor.MiddleCenter,FontSize = 20,Text = "На вылазке"},
                                RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "785 15",OffsetMax = "915 85"}
                            }, _layer + $".Merc.{mercenary.Key}",
                            _layer + $".Merc.{mercenary.Key}" + ".Btn");
                    }
                    else
                    {
                        container.Add(new CuiLabel
                            {
                                Text = {Align = TextAnchor.MiddleCenter,Text =(CurTime()>_database[player.userID].Find(m => m.Key == mercenary.Key).Cooldown)? "Готов":$"{FormatTime(GetTime(_database[player.userID].Find(m => m.Key == mercenary.Key).Cooldown-CurTime()))}",FontSize = 16},
                                RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "645 45",OffsetMax = "720 85"}
                            }, _layer + $".Merc.{mercenary.Key}",
                            _layer + $".Merc.{mercenary.Key}" + $".Time");
                        container.Add(new CuiElement
                        {
                            Parent = _layer + $".Merc.{mercenary.Key}",
                            Name = _layer + $".Merc.{mercenary.Key}"+".BarImage",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) ImageLibrary.Call("GetImage","zzz")
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "600 45",OffsetMax = "640 85"
                                }
                            }
                        });
                        container.Add(new CuiButton
                            {
                                Button = {Color = (CanAccept(mercenary.Cost,player.UserIDString)&&_database[player.userID].Find(m => m.Key == mercenary.Key).Cooldown<=CurTime())?HexToCuiColor("#2B6992") : HexToCuiColor("#a1a1a1"),Command = $"mercstart {mercenary.Key}",Material = "assets/icons/greyout.mat"},
                                Text = {Align = TextAnchor.MiddleCenter,FontSize = 18,Text = "НАЧАТЬ ПРОИЗВОДСТВО"},
                                RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "785 15",OffsetMax = "915 85"}
                            }, _layer + $".Merc.{mercenary.Key}",
                            _layer + $".Merc.{mercenary.Key}" + ".Btn");
                    }
                }
                
            }
            CuiHelper.AddUi(player, container);
        }
        void BuyMerc(BasePlayer player, string key)
        {
            Mercenary mercenary = config.Mercenaries.Find(p => p.Key == key);
            if (mercenary == null) return;
            if (!CanAccept(mercenary.Buy,player.UserIDString)) return;
            PlayerMerc merc = _database[player.userID].Find(p => p.Key == mercenary.Key);
            merc.Available = true;
            Economics.Call("SetBalance", player.UserIDString,
                (double) Economics.Call("Balance", player.UserIDString) - mercenary.Buy);
            BMenu.Call("UpdateBalance", player);
            SaveDB();
            MercUI(player);
        }

        void RunMerc(BasePlayer player, string key)
        {
            Mercenary mercenary = config.Mercenaries.Find(p => p.Key == key);
            if (mercenary == null) return;
            TakeReawrd(player,key);
            if (!CanAccept(mercenary.Cost,player.UserIDString)) return;
            PlayerMerc merc = _database[player.userID].Find(p => p.Key == mercenary.Key);
            if (merc.Cooldown>CurTime())
            {
                
                return;
            }
            merc.Active = true;
            merc.Cooldown = CurTime() + mercenary.Time;
            Economics.Call("SetBalance", player.UserIDString,
                (double) Economics.Call("Balance", player.UserIDString) - mercenary.Cost);
            BMenu.Call("UpdateBalance", player);
            SaveDB();
            MercUI(player);
        }

        void EndRun(BasePlayer player, string key)
        {
            Mercenary mercenary = config.Mercenaries.Find(p => p.Key == key);
            //if (mercenary == null) return;
            //if (!CanAccept(mercenary.Cost,player.UserIDString)) return;
            PlayerMerc merc = _database[player.userID].Find(p => p.Key == mercenary.Key);
            string sound = "assets/prefabs/misc/halloween/lootbag/effects/bronze_open.prefab";
            BroadcastSystem?.Call("SendCustonNotification", player,"ИСКАТЕЛЬ ЗАВЕРШИЛ ЭКСПЕДИЦИЮ","ЗАБЕРИТЕ ВЕЩИ, КОТОРЫЕ БУДУТ ПЕРЕМЕЩЕНЫ В ЛИЧНОЕ УБЕЖИЩЕ!",sound,5f);

            
            merc.Active = false;
            merc.Cooldown = CurTime() + mercenary.Cooldown;
            CreatePrize(player.userID,key);
            SaveDB();
            MercUI(player);
        }

        void CreatePrize(ulong id, string key)
        {
            PlayerMerc data = _database[id].Find(p => p.Key == key);
            if (data == null)
            {
                
                return;
            }
            for (int i = 0; i < 4; i++)
            {
                
                data.Inventory[i] = GetRandomItem(key);
                while (data.Inventory[i] == null)
                {
                    
                    data.Inventory[i] = GetRandomItem(key);
                    continue;
                }
                if (data.Inventory[i].IsCommand)
                {
                    data.Inventory[i].Shortname = data.Inventory[i].Shortname.Replace("userid", id.ToString());
                }
            }
        }

        bool CanAccept(double price, string userid)
        {
            double balance = Economics.Call<double>("Balance", userid);
            return balance >= price;
        }

        void CheckTimer(ulong id)
        {
            if (!_database.ContainsKey(id))
            {
                InitPlayerDB(BasePlayer.FindByID(id));
            }
            foreach (var data in _database[id])
            {
                if (data.Active && data.Available)
                {
                    Mercenary mercenary = config.Mercenaries.Find(p => p.Key == data.Key);
                    	if (1-GetTime(data.Cooldown-CurTime()).TotalSeconds/mercenary.Time>=1 || FormatTime(GetTime(_database[id].Find(m => m.Key == mercenary.Key).Cooldown-CurTime())).Contains('-') || FormatTime(GetTime(_database[id].Find(m => m.Key == mercenary.Key).Cooldown-CurTime())).Contains("-"))
                    {
                        
                        BasePlayer player = BasePlayer.FindByID(id);
                        EndRun(player,data.Key);
                    }
                }
            }
        }
        bool HasItems(BasePlayer player, Mercenary mercenary)
        {
            RewardItem[] items = _database[player.userID].Find(p => p.Key == mercenary.Key).Inventory;
            foreach (var item in items)
            {
                if (item != null) 
                    return true;
            }
            return false;
        }


        void TakeReawrd(BasePlayer player, string key)
        {
            PlayerMerc mercenary = _database[player.userID].Find(p => p.Key == key);
            
            if (mercenary == null) return;
            foreach (var item in mercenary.Inventory)
            {
                
                if (item == null || item.Shortname == "void") continue;
                
                if (item.Category == "fragment")
                {
                    item.Shortname = item.Shortname.Replace("userid", player.UserIDString);
                    item.Shortname =  item.Shortname.Replace("X", item.Amount.ToString());
                    PrintWarning(item.Shortname);
                    Server.Command(item.Shortname);
                    continue;
                }
                Inventory?.Call("AddToInv", player, item.Shortname, item.Blueprint, item.IsCommand, item.Amount,item.Image,
                    item.DisplayName,item.SkinId,item.Category,item.Rarity);
                /*
                 * private void AddToInv(BasePlayer player, string shortname, bool isBluePrint, bool isCommand, int amount, string Image,
            string displayName,ulong skinId = 0,string category = "ВСЕ",string rare = "default")
                 */
                /*Inventory.Call("AddToInv", player, item.Shortname, item.Blueprint, item.IsCommand, item.Amount,
                    item.Image,
                    item.DisplayName, item.SkinId, item.Category, item.Rarity);*/

            }
            _database[player.userID].Find(p => p.Key == mercenary.Key).Inventory = new RewardItem[]
            {
                null, null, null, null
            };
            MercUI(player);
            
        }

        RewardItem GetRandomItem(string key)
        {
            Mercenary mercenary = config.Mercenaries.Find(p => p.Key == key);
            RewardItem item = voidItem;
            double random = Oxide.Core.Random.Range(0, 100);
            if (random>10 && random<=65)
            {
                if (mercenary.RewardItems.FindAll(p => p.Rarity == "default") == null)
                {
                    return GetRandomItem(key);
                }
                return item = mercenary.RewardItems.FindAll(p => p.Rarity == "default").GetRandom();
            }
            if (random>65 && random<=85)
            {
                if (mercenary.RewardItems.FindAll(p => p.Rarity == "rare") == null)
                {
                    return GetRandomItem(key);
                }
                return item = mercenary.RewardItems.FindAll(p => p.Rarity == "rare").GetRandom();
            }
            if (random>85 && random<=95)
            {
                if (mercenary.RewardItems.FindAll(p => p.Rarity == "epic") == null)
                {
                    return GetRandomItem(key);
                }
                return item = mercenary.RewardItems.FindAll(p => p.Rarity == "epic").GetRandom();
            }
            if (random>95 && random<=100)
            {
                if (mercenary.RewardItems.FindAll(p => p.Rarity == "legendary") == null)
                {
                    return GetRandomItem(key);
                }
                return item = mercenary.RewardItems.FindAll(p => p.Rarity == "legendary").GetRandom();
            }
            
            return item;

        }

        double MaxRange(Mercenary mercenary, out  List<string> rarity)
        {
            double max = 0;
            rarity = new List<string>();
            foreach (var VARIABLE in mercenary.RewardItems)
            {
                if (!rarity.Contains(VARIABLE.Rarity))
                {
                    rarity.Add(VARIABLE.Rarity);
                    max += config.Rarity[VARIABLE.Rarity];
                }
            }
            
            return max+config.Rarity["null"];
        }
        
        

        #endregion
        
        #region commands

        [ConsoleCommand("mercstart")]
        void PlayerStartEvent(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            BasePlayer player = arg.Player();
            
            if (!arg.HasArgs(1))
            {
                return;
            }
            RunMerc(player,arg.Args[0]);
        }

        [ConsoleCommand("takereward")]
        void PlayerTakeReward(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
           
            TakeReawrd(arg.Player(),arg.Args[0]);
        }
        
        [ConsoleCommand("mercbuy")]
        void BuyMerc(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            BasePlayer player = arg.Player();
            
            if (!arg.HasArgs(1))
            {
                return;
            }
            BuyMerc(player,arg.Args[0]);
        }

        [ConsoleCommand("mercreward")]
        void OpenMercReward(ConsoleSystem.Arg arg)
        {
           
            if (arg.Player() == null) return;
            BasePlayer player = arg.Player();
            
            if (!arg.HasArgs(1))
            {
                return;
            }
            
            if (arg.HasArgs(2))
            {
                string a = arg.Args[0];
                int page = arg.Args[1].ToInt();
                RewardUI(player,a,page);
                return;
            }
            
            if (arg.HasArgs(1))
            {
                string a = arg.Args[0];
                RewardUI(player,a);
                return;
            }
            
            
            
        }

        /*[ConsoleCommand("mr")]
        void TestRUI(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            RewardUI(player,arg.Args[0]);
        }*/
        #endregion

        #region oxidehooks

        void OnServerInitialized()
        {
            ImageLibrary.Call("AddImage", "https://rustlabs.com/img/items180/blueprintbase.png", "bp");
            ImageLibrary.Call("AddImage", "https://gspics.org/images/2022/06/13/0neENN.png", "lock");
            ImageLibrary.Call("AddImage", "https://gspics.org/images/2022/06/13/0neS0e.png", "zzz");
            ImageLibrary.Call("AddImage", "https://gspics.org/images/2022/06/14/0nYI87.png", "run");
            ImageLibrary.Call("AddImage", "https://gspics.org/images/2022/06/13/0ne4Xs.png", "ibg");
            ImageLibrary.Call("AddImage", "https://gspics.org/images/2022/06/13/0netwh.png", "mercbg");
            ImageLibrary.Call("AddImage", "https://gspics.org/images/2022/06/13/0ne4Xs.png", "void");
            ImageLibrary.Call("AddImage", "https://gspics.org/images/2022/06/13/0neQBv.png", "inv");
            ReInitImages();
            InitImages();
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("TimeLoot"))
            {
                ReadDB();
            }
            foreach (var player in BasePlayer.activePlayerList)
            {
                PlayerTimer(player);
            }
            SaveDB();
            
            timer.Once(1800f,() =>MakeBackup());
            

        }
        
        void PlayerTimer(BasePlayer player)
        {
            Timer ptimer = timer.Every(1f,(() =>
            {
                CheckTimer(player.userID);
            }));
            playerTimes.Add(player.userID,ptimer);
            /*timer.Every(1f, (() =>
            {
                CheckTimer(player.userID);
                UpdateTimer(player);
            }));*/
        }

        void OnPlayerConnected(BasePlayer player)
        {
            InitPlayerDB(player);
            PlayerTimer(player);
        }
        
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (playerTimes.ContainsKey(player.userID))
            {
                playerTimes[player.userID].Destroy();
                playerTimes.Remove(player.userID);
            }
            
        }

        void Unload()
        {
            SaveDB();
        }

        void InitCommandImages()
        {
            foreach (var mercenary in config.Mercenaries)
            {
                foreach (var rewardItem in mercenary.RewardItems)
                {
                    if (rewardItem.IsCommand)
                    {
                        ImageLibrary.Call("AddImage", rewardItem.Image, rewardItem.Shortname);
                    }
                }
            }
        }
        void InitImages()
        {
            foreach (var mercenary in config.Mercenaries)
            {
                ImageLibrary.Call("AddImage", mercenary.url, mercenary.Key);
                foreach (var item in mercenary.RewardItems)
                {
                    if (!item.IsCommand)
                    {
                        ImageLibrary.Call("AddImage", $"https://rustlabs.com/img/items180/{item.Shortname}.png", item.Shortname);
                    }
                    else
                    {
                        ImageLibrary.Call("AddImage", item.Image, item.Image);
                    }
                }
            }
            
        }

        #endregion
        
        #region data

        class PlayerMerc
        {
            public string Key;
            public bool Available;
            public bool Active;
            public double Cooldown;
            public RewardItem[] Inventory;
            
        }
        Dictionary<ulong,List<PlayerMerc>>_database = new Dictionary<ulong, List<PlayerMerc>>();


        void InitPlayerDB(BasePlayer player)
        {
            if (!_database.ContainsKey(player.userID))
            {
                _database.Add(player.userID,new List<PlayerMerc>());
                foreach (var mercenary in config.Mercenaries)
                {
                    _database[player.userID].Add(new PlayerMerc
                    {
                        Key = mercenary.Key,
                        Available = false,
                        Active = false,
                        Cooldown = CurTime(),
                        Inventory = new RewardItem[]
                        {
                            voidItem,voidItem,voidItem,voidItem 
                        }
                    });
                }
            }
        }

        void ReInitImages()
        {
            foreach (var mercenary in config.Mercenaries)
            {
                foreach (var rewardItem in mercenary.RewardItems)
                {
                    if (!rewardItem.Blueprint && !rewardItem.IsCommand)
                    {
                        rewardItem.Image = $"https://rustlabs.com/img/items180/{rewardItem.Shortname}.png";
                    }

                    
                }
            }
        }

        void MakeBackup()
        {
            SaveDB();
            timer.Once(1800f, () =>MakeBackup());
        }
        void SaveDB()
        {
            Interface.Oxide.DataFileSystem.WriteObject("TimeLoot",_database);
        }

        void ReadDB()
        {
            _database = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, List<PlayerMerc>>>("TimeLoot");
        }
        #endregion

        #region helper

        TimeSpan GetTime(double seconds)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        string FormatTime(TimeSpan time)
        {
            return $"{time.Hours}:{time.Minutes}:{time.Seconds}";
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
        
        double CurTime() => new TimeSpan(DateTime.UtcNow.Ticks).TotalSeconds;

        #endregion
    }
}