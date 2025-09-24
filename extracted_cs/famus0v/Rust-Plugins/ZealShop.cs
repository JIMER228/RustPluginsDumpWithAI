using System;
using System.Collections;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Globalization;
using System.Linq;
using Facepunch.Extend;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using Color = UnityEngine.Color;

namespace Oxide.Plugins
{
    [Info("ZealShop", "Kira", "1.0.0")]
    [Description("Game store")]
    public class ZealShop : RustPlugin
    {
        #region [Reference] / [Запросы]

        [PluginReference] Plugin ImageLibrary;
        private StoredData _dataBase = new StoredData();

        private string GetImg(string name)
        {
            return (string) ImageLibrary?.Call("GetImage", name) ?? "";
        }

        #endregion 
 
        #region [Configuraton] / [Конфигурация]

        private static ConfigData _config;

        public class ConfigData
        {
            [JsonProperty(PropertyName = "ZealShop")]
            public ShopCfg ZealShop = new ShopCfg();

            public class ShopCfg
            {
                [JsonProperty(PropertyName = "Кол-во рублей за 1 минуту наигранного времени")]
                public float PlayingTimeAdd;

                [JsonProperty(PropertyName = "Какая должна быить приписка к нику")]
                public string ServerName;

                [JsonProperty(PropertyName = "Предметы для обмена")]
                public readonly Dictionary<string, TradeItem> TradeItems = new Dictionary<string, TradeItem>();

                [JsonProperty(PropertyName = "Активные категории")]
                public readonly List<string> Categories = new List<string>();

                [JsonProperty(PropertyName = "Оружие")]
                public List<ItemShop> Gun = new List<ItemShop>();

                [JsonProperty(PropertyName = "Постройки")]
                public List<ItemShop> Construction = new List<ItemShop>();

                [JsonProperty(PropertyName = "Предметы")]
                public List<ItemShop> Items = new List<ItemShop>();

                [JsonProperty(PropertyName = "Ресурсы")]
                public List<ItemShop> Resources = new List<ItemShop>();

                [JsonProperty(PropertyName = "Одежда")]
                public List<ItemShop> Attire = new List<ItemShop>();

                [JsonProperty(PropertyName = "Еда")] public List<ItemShop> Food = new List<ItemShop>();

                [JsonProperty(PropertyName = "Инструменты")]
                public List<ItemShop> Tools = new List<ItemShop>();

                [JsonProperty(PropertyName = "Медицина")]
                public List<ItemShop> Medicals = new List<ItemShop>();

                [JsonProperty(PropertyName = "Аммуниция")]
                public List<ItemShop> Ammunations = new List<ItemShop>();

                [JsonProperty(PropertyName = "Ловушки")]
                public List<ItemShop> Traps = new List<ItemShop>();

                [JsonProperty(PropertyName = "Компоненты")]
                public List<ItemShop> Components = new List<ItemShop>();

                [JsonProperty(PropertyName = "Прочее")]
                public List<ItemShop> Misc = new List<ItemShop>();

                [JsonProperty(PropertyName = "Процент от баланса, который остается после вайпа")]
                public int WipeProcent;
            }
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                ZealShop = new ConfigData.ShopCfg
                {
                    PlayingTimeAdd = 1,
                    ServerName = "Galaxy",
                    Resources = _resources,
                    Food = _food,
                    Components = _components,
                    Medicals = _medicals,
                    Ammunations = _ammunations,
                    Attire = _attire,
                    Tools = _tools,
                    Traps = _traps,
                    Misc = _misc,
                    Gun = _gun,
                    Items = _items,
                    Construction = _construction,
                    Categories =
                    {
                        "ОРУЖИЕ",
                        "ПОСТРОЙКИ",
                        "ПРЕДМЕТЫ",
                        "ОДЕЖДА",
                        "ИНСТРУМЕНТЫ",
                        "МЕДИКАМЕНТЫ",
                        "ЕДА",
                        "СНАРЯДЫ",
                        "ЛОВУШКИ",
                        "РАЗНОЕ",
                        "КОМПОНЕНТЫ"
                    },
                    TradeItems =
                    {
                        ["rifle.ak"] = new TradeItem
                        {
                            Price = 350,
                            Stack = 1
                        }
                    }
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<ConfigData>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintError("Файл конфигурации поврежден (или не существует), создан новый!");
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        #endregion

        #region [MonoBehaviours]

        public class PlayingTime : MonoBehaviour
        {
            private BasePlayer _player;

            private void Awake()
            {
                _player = GetComponent<BasePlayer>();
                _.CheckDataBase(_player);
                InvokeRepeating(nameof(Timer), 60, 60);
            }

            public void Timer()
            {
                _._dataBase.ListPlayers[_player.userID].Balance += _config.ZealShop.PlayingTimeAdd;
            }

            private void OnDestroy()
            {
                _.PrintError("Dest");
            }
        }

        #endregion

        #region [Classes] / [Классы]

        public class ItemShop
        {
            public string Shortname;
            public string Name;
            public ulong SkinID;
            public int FixCount;
            public float Price;
        }

        public class TradeItem
        {
            public int Price;
            public int Stack;
        }

        #endregion

        #region [Dictionary/Vars] / [Словари/Переменные]

        private const string Sharp = "assets/content/ui/ui.background.tile.psd";
        private const string Blur = "assets/content/ui/uibackgroundblur.mat";
        private const string Radial = "assets/content/ui/ui.background.transparent.radial.psd";
        private const string Regular = "robotocondensed-regular.ttf";
        private static ZealShop _;
        private const string Perm = "ZealShop.use";

        #region [Categories]

        private readonly List<ItemShop> _gun = new List<ItemShop>
        {
            new ItemShop
            {
                Name = "Assault Rifle",
                Shortname = "rifle.ak",
                FixCount = 1,
                Price = 170
            },
            new ItemShop
            {
                Name = "LR-300 Assault Rifle",
                Shortname = "rifle.lr300",
                FixCount = 1,
                Price = 240
            },
            new ItemShop
            {
                Name = "M249",
                Shortname = "lmg.m249",
                FixCount = 1,
                Price = 700
            },
            new ItemShop
            {
                Name = "Bolt Action Rifle",
                Shortname = "rifle.bolt",
                FixCount = 1,
                Price = 200
            },
            new ItemShop
            {
                Name = "L96 Rifle",
                Shortname = "rifle.l96",
                FixCount = 1,
                Price = 350
            },
            new ItemShop
            {
                Name = "M39 Rifle",
                Shortname = "rifle.m39",
                FixCount = 1,
                Price = 350
            },
            new ItemShop
            {
                Name = "M92 Pistol",
                Shortname = "pistol.m92",
                FixCount = 1,
                Price = 120
            },
            new ItemShop
            {
                Name = "Python Revolver",
                Shortname = "pistol.python",
                FixCount = 1,
                Price = 100
            },
            new ItemShop
            {
                Name = "Semi-Automatic Pistol",
                Shortname = "pistol.semiauto",
                FixCount = 1,
                Price = 80
            },
            new ItemShop
            {
                Name = "Rocket Launcher",
                Shortname = "rocket.launcher",
                FixCount = 1,
                Price = 280
            },
            new ItemShop
            {
                Name = "Flame Thrower",
                Shortname = "flamethrower",
                FixCount = 1,
                Price = 130
            },
            new ItemShop
            {
                Name = "Muzzle Brake",
                Shortname = "weapon.mod.muzzlebrake",
                FixCount = 1,
                Price = 60
            },
            new ItemShop
            {
                Name = "Silencer",
                Shortname = "weapon.mod.silencer",
                FixCount = 1,
                Price = 45
            },
            new ItemShop
            {
                Name = "Simple Handmade Sight",
                Shortname = "weapon.mod.simplesight",
                FixCount = 1,
                Price = 60
            },
            new ItemShop
            {
                Name = "Weapon Flashlight",
                Shortname = "weapon.mod.flashlight",
                FixCount = 1,
                Price = 45
            },
            new ItemShop
            {
                Name = "Weapon Lasersight",
                Shortname = "weapon.mod.lasersight",
                FixCount = 1,
                Price = 45
            },
            new ItemShop
            {
                Name = "Holosight",
                Shortname = "weapon.mod.holosight",
                FixCount = 1,
                Price = 60
            },
            new ItemShop
            {
                Name = "4x Zoom Scope",
                Shortname = "weapon.mod.small.scope",
                FixCount = 1,
                Price = 70
            },
            new ItemShop
            {
                Name = "8x Zoom Scope",
                Shortname = "weapon.mod.8x.scope",
                FixCount = 1,
                Price = 85
            },
            new ItemShop
            {
                Name = "Beancan Grenade",
                Shortname = "grenade.beancan",
                FixCount = 10,
                Price = 140
            }
        };

        private readonly List<ItemShop> _construction = new List<ItemShop>
        {
            new ItemShop
            {
                Name = "ШКАФ",
                Shortname = "cupboard.tool",
                FixCount = 1,
                Price = 150
            }
        };

        private readonly List<ItemShop> _items = new List<ItemShop>
        {
            new ItemShop
            {
                Name = "СТУЛ",
                Shortname = "chair",
                FixCount = 1,
                Price = 150
            }
        };

        private readonly List<ItemShop> _resources = new List<ItemShop>
        {
            new ItemShop
            {
                Name = "Wood",
                Shortname = "wood",
                FixCount = 5000,
                Price = 75
            },
            new ItemShop
            {
                Name = "Stones",
                Shortname = "stones",
                FixCount = 5000,
                Price = 125
            },
            new ItemShop
            {
                Name = "Sulfur",
                Shortname = "sulfur",
                FixCount = 1000,
                Price = 110
            },
            new ItemShop
            {
                Name = "Metal Fragments",
                Shortname = "metal.fragments",
                FixCount = 1000,
                Price = 75
            },
            new ItemShop
            {
                Name = "High Quality Metal",
                Shortname = "metal.refined",
                FixCount = 100,
                Price = 150
            },
            new ItemShop
            {
                Name = "Animal Fat",
                Shortname = "fat.animal",
                FixCount = 100,
                Price = 25
            },
            new ItemShop
            {
                Name = "Low Grade Fuel",
                Shortname = "lowgradefuel",
                FixCount = 500,
                Price = 100
            },
            new ItemShop
            {
                Name = "Cloth",
                Shortname = "cloth",
                FixCount = 100,
                Price = 60
            },
            new ItemShop
            {
                Name = "Leather",
                Shortname = "leather",
                FixCount = 100,
                Price = 45
            },
            new ItemShop
            {
                Name = "Scrap",
                Shortname = "scrap",
                FixCount = 100,
                Price = 50
            },
            new ItemShop
            {
                Name = "Crude Oil",
                Shortname = "crude.oil",
                FixCount = 100,
                Price = 180
            }
        };

        private readonly List<ItemShop> _attire = new List<ItemShop>
        {
            new ItemShop
            {
                Name = "МВК МАСКА",
                Shortname = "metal.facemask",
                FixCount = 1,
                Price = 150
            }
        };

        private readonly List<ItemShop> _food = new List<ItemShop>
        {
            new ItemShop
            {
                Name = "ЧЕРНИЧЬКаА",
                Shortname = "blueberries",
                FixCount = 1,
                Price = 150
            }
        };

        private readonly List<ItemShop> _tools = new List<ItemShop>
        {
            new ItemShop
            {
                Name = "КИРКА",
                Shortname = "pickaxe",
                FixCount = 1,
                Price = 150
            }
        };

        private readonly List<ItemShop> _medicals = new List<ItemShop>
        {
            new ItemShop
            {
                Name = "ШПРИЦ",
                Shortname = "syringe.medical",
                FixCount = 1,
                Price = 150
            }
        };

        private readonly List<ItemShop> _ammunations = new List<ItemShop>
        {
            new ItemShop
            {
                Name = "ПАТРОНЫ 5x56",
                Shortname = "ammo.rifle",
                FixCount = 1,
                Price = 150
            }
        };

        private readonly List<ItemShop> _traps = new List<ItemShop>
        {
            new ItemShop
            {
                Name = "ГАН ТРАП",
                Shortname = "guntrap",
                FixCount = 1,
                Price = 150
            }
        };

        private readonly List<ItemShop> _misc = new List<ItemShop>
        {
            new ItemShop
            {
                Name = "МИШКА",
                Shortname = "pookie.bear",
                FixCount = 1,
                Price = 150
            }
        };

        private readonly List<ItemShop> _components = new List<ItemShop>
        {
            new ItemShop
            {
                Name = "НОУТБУК",
                Shortname = "targeting.computer",
                FixCount = 1,
                Price = 150
            }
        };

        #endregion

        private const string Layer = "Box_Marketplace";
        private const string LayerBuyMenu = "Box_BuyMenu";
        private const string UITrade = "TradeBG";

        #endregion

        #region [DrawUI] / [Показ UI]

        private void DrawUI_Shop(BasePlayer player, int page, List<ItemShop> category, string categoryname)
        {
            var gui = new CuiElementContainer();
            CuiHelper.DestroyUi(player, Layer);

            var db = _dataBase.ListPlayers[player.userID];

            gui.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image =
                {
                    Color = HexToRustFormat("#000000F9"),
                    Material = Blur,
                    Sprite = Radial
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, "Overlay", Layer);

            gui.Add(new CuiButton
            {
                Button =
                {
                    Command = "close.shop",
                    Color = "0 0 0 0"
                },
                Text =
                {
                    Text = " "
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, Layer, "Button.close.shop");

            #region [Box-ProfilePanel]

            const string layerProfile = "BoxProfile";

            gui.Add(new CuiElement
            {
                Name = "BoxProfile",
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#0000009E"),
                        Material = Blur,
                        Sprite = Radial
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.0088541 0.8916666",
                        AnchorMax = "0.9885417 0.9842592"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#8ebf5b"),
                        Distance = "0.9 0.9"
                    }
                }
            });

            gui.Add(new CuiElement
            {
                Name = "ShopTXT",
                Parent = layerProfile,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 40,
                        Text = "МАГАЗИН"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.23126 0",
                        AnchorMax = "0.83094 1"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            // gui.Add(new CuiElement
            // {
            //     Name = "ICTradeButton",
            //     Parent = layerProfile,
            //     Components =
            //     {
            //         new CuiImageComponent
            //         {
            //             Sprite = "assets/icons/refresh.png",
            //             Color = HexToRustFormat("#EAEAEAFF")
            //         },
            //         new CuiRectTransformComponent
            //         {
            //             AnchorMin = "0.9537478 0.18",
            //             AnchorMax = "0.9877723 0.8199999"
            //         },
            //         new CuiOutlineComponent
            //         {
            //             Color = HexToRustFormat("#8ebf5b"),
            //             Distance = "0.4 0.4"
            //         }
            //     }
            // });
            //
            // gui.Add(new CuiButton
            // {
            //     Button =
            //     {
            //         Command = "trademoney",
            //         Color = "0 0 0 0"
            //     },
            //     Text =
            //     {
            //         Text = " "
            //     },
            //     RectTransform =
            //     {
            //         AnchorMin = "0 0",
            //         AnchorMax = "1 1"
            //     }
            // }, "ICTradeButton");

            #region [Profile]

            gui.Add(new CuiElement
            {
                Name = "AvatarProfile",
                Parent = layerProfile,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage", player.UserIDString),
                        Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "0.053 0.98"
                    }
                }
            });

            gui.Add(new CuiElement
            {
                Name = "AvatarLine",
                Parent = layerProfile,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#8ebf5b")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.053 0",
                        AnchorMax = "0.05301 0.99"
                    }
                }
            });

            gui.Add(new CuiElement
            {
                Name = "Profile_Nick",
                Parent = layerProfile,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        Font = Regular,
                        FontSize = 14,
                        Text = "Ник"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.05582141 0.689",
                        AnchorMax = "0.1 0.939"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#00000081"),
                        Distance = "0.9 0.9"
                    }
                }
            });

            gui.Add(new CuiElement
            {
                Name = "Profile_Nick_Line",
                Parent = layerProfile,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Font = Regular,
                        FontSize = 14,
                        Text = "|"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.0978201 0.6899996",
                        AnchorMax = "0.1031366 0.9399999"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#00000081"),
                        Distance = "0.9 0.9"
                    }
                }
            });

            gui.Add(new CuiElement
            {
                Name = "Profile_Nick_Value",
                Parent = layerProfile,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        Font = Regular,
                        FontSize = 14,
                        Text = player.displayName
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1081366 0.6899995",
                        AnchorMax = "0.1939447 0.9399998"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#00000081"),
                        Distance = "0.9 0.9"
                    }
                }
            });

            gui.Add(new CuiElement
            {
                Name = "Profile_Balance",
                Parent = layerProfile,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        Font = Regular,
                        FontSize = 14,
                        Text = "Баланс"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.05582141 0.36",
                        AnchorMax = "0.1 0.6099994"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#00000081"),
                        Distance = "0.9 0.9"
                    }
                }
            });

            gui.Add(new CuiElement
            {
                Name = "Profile_Balance_Line",
                Parent = layerProfile,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Font = Regular,
                        FontSize = 14,
                        Text = "|"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.0978201 0.36",
                        AnchorMax = "0.1031366 0.61"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#00000081"),
                        Distance = "0.9 0.9"
                    }
                }
            });

            gui.Add(new CuiElement
            {
                Name = "Profile_Balance_Value",
                Parent = layerProfile,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        Font = Regular,
                        FontSize = 14,
                        Text = $"{db.Balance} RUB"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1081366 0.36",
                        AnchorMax = "0.1939447 0.61"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#00000081"),
                        Distance = "0.9 0.9"
                    }
                }
            });

            gui.Add(new CuiElement
            {
                Name = "Profile_SteamID",
                Parent = layerProfile,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        Font = Regular,
                        FontSize = 14,
                        Text = "SteamID"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.05582141 0.03",
                        AnchorMax = "0.1 0.28"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#00000081"),
                        Distance = "0.9 0.9"
                    }
                }
            });

            gui.Add(new CuiElement
            {
                Name = "Profile_SteamID_Line",
                Parent = layerProfile,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Font = Regular,
                        FontSize = 14,
                        Text = "|"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.0978201 0.03",
                        AnchorMax = "0.1031366 0.29"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#00000081"),
                        Distance = "0.9 0.9"
                    }
                }
            });

            gui.Add(new CuiElement
            {
                Name = "Profile_SteamID_Value",
                Parent = layerProfile,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        Font = Regular,
                        FontSize = 14,
                        Text = $"{player.userID}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1081366 0.03",
                        AnchorMax = "0.1939447 0.29"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#00000081"),
                        Distance = "0.9 0.9"
                    }
                }
            });

            #endregion

            #endregion

            #region [Box-Categories]

            string layerCategories = "BoxCategories";

            gui.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = "0.0088541 0.8444446",
                    AnchorMax = "0.9885417 0.8861113"
                }
            }, Layer, "BoxCategories");

            int xc = 0;
            double butanchor = (double) 1 / _config.ZealShop.Categories.Count;
            foreach (var categories in _config.ZealShop.Categories)
            {
                gui.Add(new CuiElement
                {
                    Name = $"Category{xc}",
                    Parent = layerCategories,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Color = HexToRustFormat("#00000062"),
                            Png = GetImg("CategoryBG"),
                            Material = Blur,
                            FadeIn = 0.1f + (xc * 0.1f)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0 + ((xc * butanchor) + 0.0025)} {0.0666666}",
                            AnchorMax = $"{butanchor + ((xc * butanchor) - 0.0025)} {0.9333334}"
                        }
                    }
                });

                gui.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"category {categories}",
                        Color = "0 0 0 0"
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 13,
                        Font = Regular,
                        Text = categories
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, $"Category{xc}", $"CategoryName{xc}");

                xc++;
            }

            #endregion

            gui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"page {page - 1}",
                    Color = "0 0 0 0"
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = HexToRustFormat("#EAEAEAFF"),
                    Font = Regular,
                    FontSize = 35,
                    Text = "<"
                },
                RectTransform =
                {
                    AnchorMin = "0.002 0.0009",
                    AnchorMax = "0.022 0.8444"
                }
            }, Layer, "Page--");

            gui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"page {page + 1}",
                    Color = "0 0 0 0"
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = HexToRustFormat("#EAEAEAFF"),
                    Font = Regular,
                    FontSize = 35,
                    Text = ">"
                },
                RectTransform =
                {
                    AnchorMin = "0.974 0.0009",
                    AnchorMax = "0.994 0.8444"
                }
            }, Layer, "Page++");

            CuiHelper.AddUi(player, gui);

            DrawUI_Category(player, page, category, categoryname);
        }

        private void DrawUI_Category(BasePlayer player, int page, List<ItemShop> category, string categoryname)
        {
            var gui = new CuiElementContainer();
            DestroyItems(player);

            #region [Box-Items]

            var countitem = 28;
            if (category.Count < 28)
            {
                countitem = category.Count;
            }

            int xi = 0, y = 0, i = 0;
            foreach (var item in category.Skip(page * 28).Take(28))
            {
                if (xi == 4)
                {
                    xi = 0;
                    y++;
                }

                gui.Add(new CuiPanel
                {
                    Image =
                    {
                        Color = "0 0 0 0"
                    },
                    RectTransform =
                    {
                        AnchorMin = $"{0.0427083 + (xi * 0.236)} {0.7370371 - (y * 0.1182292)}",
                        AnchorMax = $"{0.2479167 + (xi * 0.236)} {0.8296295 - (y * 0.1182292)}"
                    }
                }, Layer, $"Box_Item{i}");

                gui.Add(new CuiElement
                {
                    Name = $"ItemICBG{i}",
                    Parent = $"Box_Item{i}",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#00000051"),
                            Material = Blur,
                            Sprite = Radial,
                            FadeIn = 0.05f + (i * 0.05f)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0.2538071 1"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#8ebf5b"),
                            Distance = "0.82 0.82"
                        }
                    }
                });

                gui.Add(new CuiElement
                {
                    Name = $"ItemIC{i}",
                    Parent = $"ItemICBG{i}",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImg($"{item.Shortname}_{128}_{item.SkinID}"),
                            FadeIn = 0.05f + (i * 0.05f)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.05 0.05",
                            AnchorMax = "0.95 0.95"
                        }
                    }
                });

                gui.Add(new CuiElement
                {
                    Name = $"ItemStack{i}",
                    Parent = $"ItemICBG{i}",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleRight,
                            FontSize = 11,
                            Font = Regular,
                            Text = $"x{item.FixCount}",
                            FadeIn = 0.05f + (i * 0.05f)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.45",
                            AnchorMax = "0.96 0.24"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#00000051"),
                            Distance = "0.5 0.5"
                        }
                    }
                });

                gui.Add(new CuiElement
                {
                    Name = $"ItemDesc{i}",
                    Parent = $"Box_Item{i}",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#00000051"),
                            Material = Blur,
                            Sprite = Radial,
                            FadeIn = 0.05f + (i * 0.05f)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.28 0",
                            AnchorMax = "1 1"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#8ebf5b"),
                            Distance = "0.82 0.82"
                        }
                    }
                });

                gui.Add(new CuiElement
                {
                    Name = $"ItemDescTXT{i}",
                    Parent = $"ItemDesc{i}",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.UpperCenter,
                            FontSize = 12,
                            Font = Regular,
                            Text =
                                $"<b>{item.Name}</b>\n<size=10><color=#EAEAEA>СТОИМОСТЬ - {(item.Price / item.FixCount) * item.FixCount} RUB</color></size>",
                            FadeIn = 0.05f + (i * 0.05f)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 0.94"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#00000051"),
                            Distance = "0.5 0.5"
                        }
                    }
                });

                gui.Add(new CuiPanel
                {
                    Image =
                    {
                        Color = "1 1 1 1",
                        Sprite = "assets/icons/store.png"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.9024234 0.0499998",
                        AnchorMax = "0.9729254 0.2499993"
                    }
                }, $"ItemDesc{i}", $"CartIC{i}");

                gui.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"buymenu {categoryname} {item.Shortname} {1}",
                        Color = "0 0 0 0"
                    },
                    Text =
                    {
                        Text = " "
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, $"CartIC{i}", $"CartButton{i}");

                xi++;
                i++;
            }

            #endregion

            CuiHelper.AddUi(player, gui);
        }

        private void DrawUI_BuyMenu(BasePlayer player, ItemShop item, string categoryname, int count)
        {
            var gui = new CuiElementContainer();

            gui.Add(new CuiElement
            {
                Name = "BuyMenuBG",
                Parent = LayerBuyMenu,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#0000009C"),
                        Material = Blur,
                        Sprite = Radial
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.296874 0.5768518",
                        AnchorMax = "0.703125 0.7111106"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#8ebf5b"),
                        Distance = "0.82 0.82"
                    }
                }
            });

            gui.Add(new CuiElement
            {
                Name = "ZagBuyMenuBG",
                Parent = "BuyMenuBG",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#0000009C"),
                        Material = Blur
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.7655199",
                        AnchorMax = "0.998 1"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#8ebf5b"),
                        Distance = "0 0.82"
                    }
                }
            });

            gui.Add(new CuiElement
            {
                Name = "BuyMenuZagTXT",
                Parent = "ZagBuyMenuBG",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 20,
                        Text = "ПОКУПКА ТОВАРА"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            gui.Add(new CuiElement
            {
                Name = "ItemICBG",
                Parent = "BuyMenuBG",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#00000051"),
                        Material = Blur,
                        Sprite = Radial
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.0102566 0.0316285",
                        AnchorMax = "0.1384618 0.7212864"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#8ebf5b"),
                        Distance = "0.82 0.82"
                    }
                }
            });

            gui.Add(new CuiElement
            {
                Name = "ItemIC",
                Parent = "ItemICBG",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImg($"{item.Shortname}_{128}_{item.SkinID}")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.05 0.05",
                        AnchorMax = "0.95 0.95"
                    }
                }
            });

            gui.Add(new CuiElement
            {
                Name = "BuyCount",
                Parent = "BuyMenuBG",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 11,
                        Text = $"Купить : {count * item.FixCount} шт"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1538465 0.5724156",
                        AnchorMax = "0.4115387 0.7241406"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            double priceone = (double) item.Price / item.FixCount;
            double price = priceone * (item.FixCount * count);

            gui.Add(new CuiElement
            {
                Name = "BuyPrice",
                Parent = "BuyMenuBG",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 11,
                        Text = $"Стоимость : {price} RUB"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1538465 0.4137945",
                        AnchorMax = "0.4115387 0.5655191"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            gui.Add(new CuiElement
            {
                Name = "BuyButtonBG",
                Parent = "BuyMenuBG",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#0000004F"),
                        Material = Blur
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.8564103 0.03448237",
                        AnchorMax = "0.9935897 0.2068968"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#8ebf5b"),
                        Distance = "0 0.82"
                    }
                }
            });

            gui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"buymenu {categoryname} {item.Shortname} {count - 1}",
                    Color = "0 0 0 0"
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1",
                    FontSize = 14,
                    Text = "-"
                },
                RectTransform =
                {
                    AnchorMin = "0.7448725 0.03448237",
                    AnchorMax = "0.7769237 0.2068968"
                }
            }, "BuyMenuBG", "Count--");

            gui.Add(new CuiElement
            {
                Name = "CurrentCount",
                Parent = "BuyMenuBG",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1",
                        FontSize = 14,
                        Font = Regular,
                        Text = $"{count}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.7807695 0.03448237",
                        AnchorMax = "0.8128207 0.2068968"
                    }
                }
            });

            gui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"buymenu {categoryname} {item.Shortname} {count + 1}",
                    Color = "0 0 0 0"
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1",
                    FontSize = 14,
                    Text = "+"
                },
                RectTransform =
                {
                    AnchorMin = "0.8166665 0.03448237",
                    AnchorMax = "0.8487177 0.2068968"
                }
            }, "BuyMenuBG", "Count++");

            gui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"buy {categoryname} {item.Shortname} {count}",
                    Color = "0 0 0 0"
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1",
                    Font = Regular,
                    FontSize = 13,
                    Text = "КУПИТЬ"
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 0.96"
                }
            }, "BuyButtonBG", "BuyButton");

            CuiHelper.AddUi(player, gui);
        }

        private void DrawUI_ShowNotify(BasePlayer player, string message, bool error)
        {
            var gui = new CuiElementContainer();
            CuiHelper.DestroyUi(player, "NotifyBG");

            gui.Add(new CuiElement
            {
                Name = "NotifyBG",
                Parent = "Overlay",
                FadeOut = 1f,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#0000009C"),
                        Material = Blur,
                        Sprite = Radial
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.3574074",
                        AnchorMax = "0.1302083 0.4129631"
                    },
                    new CuiOutlineComponent
                    {
                        Color = error ? HexToRustFormat("#B33838") : HexToRustFormat("#468F3D"),
                        Distance = "1.5 1.5"
                    }
                }
            });

            gui.Add(new CuiElement
            {
                Name = "NotifyMessage",
                Parent = "NotifyBG",
                FadeOut = 1f,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1",
                        Font = Regular,
                        FontSize = 12,
                        Text = message
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#0000009C"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            CuiHelper.AddUi(player, gui);
            timer.Once(2.5f, () =>
            {
                CuiHelper.DestroyUi(player, "NotifyMessage");
                CuiHelper.DestroyUi(player, "NotifyBG");
            });
        }

        private void DrawUI_TradeMoney(BasePlayer player, int page1, int page2)
        {
            CuiHelper.DestroyUi(player, "ItemBG0");
            CuiHelper.DestroyUi(player, "ItemBG1");
            CuiHelper.DestroyUi(player, "ItemBG2");
            CuiHelper.DestroyUi(player, "ItemBG20");
            CuiHelper.DestroyUi(player, "ItemBG21");
            CuiHelper.DestroyUi(player, "ItemBG22");
            CuiHelper.DestroyUi(player, "GoLeft");
            CuiHelper.DestroyUi(player, "GoRight");
            CuiHelper.DestroyUi(player, "GoLeft2");
            CuiHelper.DestroyUi(player, "GoRight2");
            var gui = new CuiElementContainer();

            int x1 = 0, x2 = 0;
            if (GetAvalibleItems(player).Count > 0)
            {
                foreach (var item in GetAvalibleItems(player).Skip(3 * page1).Take(3))
                {
                    var info = _config.ZealShop.TradeItems[item];
                    gui.Add(new CuiElement
                    {
                        Name = $"ItemBG{x1}",
                        Parent = UITrade,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = HexToRustFormat("#0000009C"),
                                Material = Blur,
                                Sprite = Radial
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0.139059 + (0.2432255 * x1)} {0.5777777}",
                                AnchorMax = $"{0.374481 + (0.2432255 * x1)} {0.7120365}"
                            },
                            new CuiOutlineComponent
                            {
                                Color = HexToRustFormat("#8ebf5b"),
                                Distance = "0.82 0.82"
                            }
                        }
                    });

                    gui.Add(new CuiElement
                    {
                        Name = "ItemICBG",
                        Parent = $"ItemBG{x1}",
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = HexToRustFormat("#00000051"),
                                Material = Blur,
                                Sprite = Radial
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.01083512 0.0316285",
                                AnchorMax = "0.3095064 0.9557695"
                            },
                            new CuiOutlineComponent
                            {
                                Color = HexToRustFormat("#8ebf5b"),
                                Distance = "0.82 0.82"
                            }
                        }
                    });

                    gui.Add(new CuiElement
                    {
                        Name = "ItemIC",
                        Parent = "ItemICBG",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = GetImg(item + 128)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.05 0.05",
                                AnchorMax = "0.95 0.95"
                            }
                        }
                    });

                    gui.Add(new CuiElement
                    {
                        Name = "ItemStack",
                        Parent = "ItemICBG",
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Align = TextAnchor.MiddleRight,
                                FontSize = 11,
                                Font = Regular,
                                Text = $"x{info.Stack}"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.45",
                                AnchorMax = "0.96 0.24"
                            },
                            new CuiOutlineComponent
                            {
                                Color = HexToRustFormat("#00000051"),
                                Distance = "0.5 0.5"
                            }
                        }
                    });

                    gui.Add(new CuiElement
                    {
                        Name = "InfoItem",
                        Parent = $"ItemBG{x1}",
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Align = TextAnchor.UpperCenter,
                                Color = "1 1 1 1",
                                FontSize = 14,
                                Font = Regular,
                                Text =
                                    $"<b>{ItemManager.FindItemDefinition(item).displayName.translated}</b>\n<size=10><color=#EAEAEA>ВЫ ПОЛУЧИТЕ - {info.Price} RUB</color></size>"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.3141623 0.04137999",
                                AnchorMax = "0.8606201 0.9103485"
                            }
                        }
                    });

                    gui.Add(new CuiElement
                    {
                        Name = "ButtonBG",
                        Parent = $"ItemBG{x1}",
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = HexToRustFormat("#8EBF5B"),
                                Material = Sharp
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.9424782 0",
                                AnchorMax = "1 1"
                            }
                        }
                    });

                    gui.Add(new CuiElement
                    {
                        Name = "ICTrade",
                        Parent = "ButtonBG",
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = HexToRustFormat("#6a933d"),
                                Sprite = "assets/icons/refresh.png"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0.3931049",
                                AnchorMax = "0.9333332 0.5931056"
                            }
                        }
                    });

                    gui.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = $"trade.item {item}",
                            Color = "0 0 0 0"
                        },
                        Text = {Text = " "},
                        RectTransform =
                        {
                            AnchorMin = "0.9424782 0",
                            AnchorMax = "1 1"
                        }
                    }, $"ItemBG{x1}");

                    x1++;
                }
            }
            else
            {
                gui.Add(new CuiElement
                {
                    Name = "AvalibleItemsNull",
                    Parent = UITrade,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 20,
                            Text =
                                "У вас нет подходящих предметов для обмена, ниже представлены список предметов для обмена"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.1395833 0.575",
                            AnchorMax = "0.8588542 0.7138889"
                        }
                    }
                });
            }

            foreach (var item in _config.ZealShop.TradeItems.Skip(3 * page2).Take(3))
            {
                gui.Add(new CuiElement
                {
                    Name = $"ItemBG2{x2}",
                    Parent = UITrade,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#0000009C"),
                            Material = Blur,
                            Sprite = Radial
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.139059 + (0.2432255 * x2)} {0.2888905}",
                            AnchorMax = $"{0.374481 + (0.2432255 * x2)} {0.4231537}"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#8ebf5b"),
                            Distance = "0.82 0.82"
                        }
                    }
                });

                gui.Add(new CuiElement
                {
                    Name = "ItemICBG",
                    Parent = $"ItemBG2{x2}",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#00000051"),
                            Material = Blur,
                            Sprite = Radial
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.01083512 0.0316285",
                            AnchorMax = "0.3095064 0.9557695"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#8ebf5b"),
                            Distance = "0.82 0.82"
                        }
                    }
                });

                gui.Add(new CuiElement
                {
                    Name = "ItemIC",
                    Parent = "ItemICBG",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImg(item.Key + 128)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.05 0.05",
                            AnchorMax = "0.95 0.95"
                        }
                    }
                });

                gui.Add(new CuiElement
                {
                    Name = "ItemStack",
                    Parent = "ItemICBG",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleRight,
                            FontSize = 11,
                            Font = Regular,
                            Text = $"x{item.Value.Stack}"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.45",
                            AnchorMax = "0.96 0.24"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#00000051"),
                            Distance = "0.5 0.5"
                        }
                    }
                });

                gui.Add(new CuiElement
                {
                    Name = "InfoItem",
                    Parent = $"ItemBG2{x2}",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.UpperCenter,
                            Color = "1 1 1 1",
                            FontSize = 14,
                            Font = Regular,
                            Text =
                                $"<b>{ItemManager.FindItemDefinition(item.Key).displayName.translated}</b>\n<size=10><color=#EAEAEA>ВЫ ПОЛУЧИТЕ - {item.Value.Price} RUB</color></size>"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.3141623 0.04137999",
                            AnchorMax = "0.8606201 0.9103485"
                        }
                    }
                });

                x2++;
            }

            gui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"trademenu.page {page1 - 1}",
                    Color = "0 0 0 0"
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 19,
                    Font = Regular,
                    Text = "<"
                },
                RectTransform =
                {
                    AnchorMin = "0.1109374 0.5777777",
                    AnchorMax = "0.1291665 0.7111109"
                }
            }, UITrade, "GoLeft");

            gui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"trademenu.page {page1 + 1}",
                    Color = "0 0 0 0"
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 19,
                    Font = Regular,
                    Text = ">"
                },
                RectTransform =
                {
                    AnchorMin = "0.869263 0.5777777",
                    AnchorMax = "0.8874913 0.7111109"
                }
            }, UITrade, "GoRight");

            gui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"trademenu.npage {page2 - 1}",
                    Color = "0 0 0 0"
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 19,
                    Font = Regular,
                    Text = "<"
                },
                RectTransform =
                {
                    AnchorMin = "0.1109374 0.2888905",
                    AnchorMax = "0.1291665 0.4222281"
                }
            }, UITrade, "GoLeft2");

            gui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"trademenu.npage {page2 + 1}",
                    Color = "0 0 0 0"
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 19,
                    Font = Regular,
                    Text = ">"
                },
                RectTransform =
                {
                    AnchorMin = "0.869263 0.2888905",
                    AnchorMax = "0.8874913 0.4222281"
                }
            }, UITrade, "GoRight2");

            CuiHelper.AddUi(player, gui);
        }

        #endregion

        #region [Hooks] / [Крюки]

        private void OnNewSave(string filename)
        {
            LoadData();
            PrintWarning(
                $"Создана новая карта, Backup файл был сохранен (oxide/data/ZealShop_Backup/DataBase[{DateTime.Today.ToString("d").Replace("/", "_")}])");
            Interface.Oxide.DataFileSystem.WriteObject(
                $"ZealShop_Backup/DataBase[{DateTime.Today.ToString("d").Replace("/", "_")}]", _dataBase.ListPlayers);
            SaveData();
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            CheckDataBase(player);
            if (player.displayName.Contains(_config.ZealShop.ServerName))
                if (player.GetComponent<PlayingTime>() == null)
                player.gameObject.AddComponent<PlayingTime>();
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            var component = player.GetComponent<PlayingTime>();
            if (component != null) UnityEngine.Object.Destroy(component);
        }

        private void OnServerInitialized()
        {
            _ = this;
            LoadData();
            CheckPlayerForDataBase();
            ServerMgr.Instance.StartCoroutine(DownloadImages());
            permission.RegisterPermission(Perm, this);
            if (ImageLibrary) return;
            Puts("Плагин ImageLibrary не установлен");
            Interface.Oxide.UnloadPlugin(Name);
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                var component = player.GetComponent<PlayingTime>();
                if (component != null) UnityEngine.Object.Destroy(component);
            }

            SaveData();
        }

        #endregion

        #region [ChatCommand] / [Чат команды]

        [ConsoleCommand("wipe")]
        private void WipeBalance(ConsoleSystem.Arg args)
        {
            if (args.Player() != null) return;
            ServerMgr.Instance.StartCoroutine(Wipe_Balance());
        }

        [ChatCommand("shop")]
        private void DrawUI_Shops(BasePlayer player)
        {
            DrawUI_Shop(player, 0, _config.ZealShop.Gun, "ОРУЖИЕ");
            CuiHelper.DestroyUi(player, UITrade);
        }

        [ConsoleCommand("close.shop")]
        private void DestroyShop(ConsoleSystem.Arg args)
        {
            if (!args.Player()) return;
            CuiHelper.DestroyUi(args.Player(), Layer);
        }

        [ConsoleCommand("close.buymenu")]
        private void DestroyBuyMenu(ConsoleSystem.Arg args)
        {
            if (!args.Player()) return;
            CuiHelper.DestroyUi(args.Player(), LayerBuyMenu);
        }

        [ConsoleCommand("close.trademenu")]
        private void DestroyTradeMenu(ConsoleSystem.Arg args)
        {
            if (!args.Player()) return;
            CuiHelper.DestroyUi(args.Player(), UITrade);
        }

        [ConsoleCommand("buy")]
        private void BuyProduct(ConsoleSystem.Arg args)
        {
            if (!args.Player()) return;

            var category = GetCategory(args.Args[0]);
            var shortname = args.Args[1];
            var count = Convert.ToInt32(args.Args[2]);

            foreach (var item in category)
            {
                if (item.Shortname == shortname)
                {
                    BuyItem(args.Player(), item, count);
                }
            }
        }

        [ConsoleCommand("buymenu")]
        private void BuyMenu(ConsoleSystem.Arg args)
        {
            var gui = new CuiElementContainer();
            if (!args.Player()) return;
            var category = GetCategory(args.Args[0]);
            var shortname = args.Args[1];
            var count = Convert.ToInt32(args.Args[2]);
            var itemShop = category.FindIndex(s =>
                string.Equals(s.Shortname, shortname, StringComparison.CurrentCultureIgnoreCase));

            if (count <= 1)
            {
                count = 1;
            }

            if (count >= 24)
            {
                count = 24;
            }

            foreach (var item in category)
            {
                if (item.Shortname == shortname)
                {
                    CuiHelper.DestroyUi(args.Player(), LayerBuyMenu);
                    gui.Add(new CuiPanel
                    {
                        CursorEnabled = true,
                        Image =
                        {
                            Color = HexToRustFormat("#00000051"),
                            Material = Blur,
                            Sprite = Radial
                        },
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    }, Layer, LayerBuyMenu);

                    gui.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = "close.buymenu",
                            Color = "0 0 0 0"
                        },
                        Text =
                        {
                            Text = " "
                        },
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    }, LayerBuyMenu, "Button.close.buymenu");
                    CuiHelper.AddUi(args.Player(), gui);
                    DrawUI_BuyMenu(args.Player(), item, args.Args[0], count);
                }
            }
        }

        [ConsoleCommand("trademenu.page")]
        private void TradeMenu_AvalibleItems_Page(ConsoleSystem.Arg args)
        {
            if (!args.Player()) return;
            int page = args.Args[0].ToInt();
            if ((GetAvalibleItems(args.Player()).Count / 3) >= page)
            {
                DrawUI_TradeMoney(args.Player(), page, 0);
            }
        }

        [ConsoleCommand("trademenu.npage")]
        private void TradeMenu_NoAvalibleItems_Page(ConsoleSystem.Arg args)
        {
            if (!args.Player()) return;
            int page = args.Args[0].ToInt();
            if ((_config.ZealShop.TradeItems.Count / 3) >= page)
            {
                DrawUI_TradeMoney(args.Player(), 0, page);
            }
        }

        [ConsoleCommand("trade.item")]
        private void TradeMenu_Item(ConsoleSystem.Arg args)
        {
            if (!args.Player()) return;
            Trade_Item(args.Player(), args.Args[0]);
            DrawUI_TradeMoney(args.Player(), 0, 0);
        }

        [ConsoleCommand("trademoney")]
        private void DrawUI_TradeMoney(ConsoleSystem.Arg args)
        {
            if (!args.Player()) return;
            CuiHelper.DestroyUi(args.Player(), UITrade);
            CuiElementContainer gui = new CuiElementContainer();
            gui.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image =
                {
                    Color = HexToRustFormat("#00000051"),
                    Material = Blur,
                    Sprite = Radial
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, "Overlay", UITrade);

            gui.Add(new CuiButton
            {
                Button =
                {
                    Command = "close.trademenu",
                    Color = "0 0 0 0"
                },
                Text =
                {
                    Text = " "
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, UITrade, "Button.close.trademenu");

            gui.Add(new CuiElement
            {
                Name = "BoxZag",
                Parent = UITrade,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#0000009E"),
                        Material = Blur,
                        Sprite = Radial
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1390625 0.7712947",
                        AnchorMax = "0.8609375 0.8388879"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#8ebf5b"),
                        Distance = "0.9 0.9"
                    }
                }
            });

            gui.Add(new CuiElement
            {
                Name = "ZagTXT",
                Parent = "BoxZag",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 35,
                        Text = "ОБМЕН ПРЕДМЕТОВ"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.23126 0",
                        AnchorMax = "0.83094 1"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            gui.Add(new CuiElement
            {
                Name = "BoxDescAvalibleItems",
                Parent = UITrade,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#0000009E"),
                        Material = Blur,
                        Sprite = Radial
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1390625 0.7249997",
                        AnchorMax = "0.8609375 0.7638886"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#8ebf5b"),
                        Distance = "0.9 0.9"
                    }
                }
            });

            gui.Add(new CuiElement
            {
                Name = "DescTXT",
                Parent = "BoxDescAvalibleItems",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 15,
                        Font = Regular,
                        Text = "ДОСТУПНЫЕ ПРЕДМЕТЫ ДЛЯ ОБМЕНА"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.23126 0",
                        AnchorMax = "0.83094 1"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            gui.Add(new CuiElement
            {
                Name = "BoxDescNotAvalibleItem",
                Parent = UITrade,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#0000009E"),
                        Material = Blur,
                        Sprite = Radial
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1390625 0.4370432",
                        AnchorMax = "0.8609375 0.4759334"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#8ebf5b"),
                        Distance = "0.9 0.9"
                    }
                }
            });

            gui.Add(new CuiElement
            {
                Name = "DescTXT",
                Parent = "BoxDescNotAvalibleItem",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 15,
                        Font = Regular,
                        Text = "ВОЗМОЖНЫЕ ПРЕДМЕТЫ ДЛЯ ОБМЕНА"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.23126 0",
                        AnchorMax = "0.83094 1"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            CuiHelper.AddUi(args.Player(), gui);
            DrawUI_TradeMoney(args.Player(), 0, 0);
        }

        [ConsoleCommand("category")]
        private void DrawUI_Category(ConsoleSystem.Arg args)
        {
            if (!args.Player()) return;
            var category = GetCategory(args.Args[0]);
            DrawUI_Category(args.Player(), 0, category, args.Args[0]);
        }

        [ConsoleCommand("page")]
        private void PageRight(ConsoleSystem.Arg args)
        {
            if (!args.Player()) return;
            int page = Convert.ToInt32(args.Args[0]);
            if (page < -1)
            {
                SendReply(args.Player(), "Это первая страница");
                return;
            }

            if (page > (_config.ZealShop.Resources.Count / 28))
            {
                SendReply(args.Player(), "Это последняя страница");
                return;
            }

            DrawUI_Category(args.Player(), page, _config.ZealShop.Resources, "ОРУЖИЕ");
        }

        #endregion

        #region [DataBase] / [Хранение данных]

        public class StoredData
        {
            public Dictionary<ulong, PlayerShopInfo> ListPlayers = new Dictionary<ulong, PlayerShopInfo>();
        }

        public class PlayerShopInfo
        {
            public string Name;
            public ulong SteamID;
            public float Balance;
        }

        [HookMethod("SaveData")]
        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _dataBase);

        private void LoadData()
        {
            try
            {
                _dataBase = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch (Exception e)
            {
                _dataBase = new StoredData();
            }
        }

        #endregion

        #region [Helpers] / [Вспомогательный код]

        private IEnumerator Wipe_Balance()
        {
            foreach (var plobj in _dataBase.ListPlayers)
            {
                var balance = plobj.Value.Balance;
                var multiple = (double) balance / 100;
                float charge = Convert.ToInt32(multiple * _config.ZealShop.WipeProcent);
                if (charge < 0) charge = 0;
                plobj.Value.Balance = charge;
                yield return new WaitForSeconds(0.2f);
            }

            yield return 0;
        }

        private void Trade_Item(BasePlayer player, string shortname)
        {
            var tradeItem = _config.ZealShop.TradeItems[shortname];
            var item = ItemManager.FindItemDefinition(shortname);

            if (player.inventory.FindItemID(item.itemid).amount >= tradeItem.Stack)
            {
                player.inventory.Take(null, item.itemid, tradeItem.Stack);
                _dataBase.ListPlayers[player.userID].Balance += tradeItem.Price;
                DrawUI_ShowNotify(player, $"Обмен успешно завершён, вам начилено {tradeItem.Price} рублей", false);
            }
            else DrawUI_ShowNotify(player, "[Trade] Error #1", true);
        }

        private static List<string> GetAvalibleItems(BasePlayer player)
        {
            var items = new List<string>();

            foreach (var item in player.inventory.AllItems())
            {
                if (!_config.ZealShop.TradeItems.ContainsKey(item.info.shortname)) continue;
                if (!items.Contains(item.info.shortname))
                {
                    items.Add(item.info.shortname);
                }
            }

            return items;
        }

        private static List<ItemShop> GetCategory(string name)
        {
            var category = new List<ItemShop>();
            switch (name.ToUpper())
            {
                case "ОРУЖИЕ":
                    category = _config.ZealShop.Gun;
                    break;
                case "ПОСТРОЙКИ":
                    category = _config.ZealShop.Construction;
                    break;
                case "ПРЕДМЕТЫ":
                    category = _config.ZealShop.Items;
                    break;
                case "РЕСУРСЫ":
                    category = _config.ZealShop.Resources;
                    break;
                case "ОДЕЖДА":
                    category = _config.ZealShop.Attire;
                    break;
                case "ИНСТРУМЕНТЫ":
                    category = _config.ZealShop.Tools;
                    break;
                case "МЕДИКАМЕНТЫ":
                    category = _config.ZealShop.Medicals;
                    break;
                case "ЕДА":
                    category = _config.ZealShop.Food;
                    break;
                case "СНАРЯДЫ":
                    category = _config.ZealShop.Ammunations;
                    break;
                case "ЛОВУШКИ":
                    category = _config.ZealShop.Traps;
                    break;
                case "РАЗНОЕ":
                    category = _config.ZealShop.Misc;
                    break;
                case "КОМПОНЕНТЫ":
                    category = _config.ZealShop.Components;
                    break;
            }

            return category;
        }

        private void BuyItem(BasePlayer player, ItemShop item, int count)
        {
            var db = _dataBase.ListPlayers[player.userID];
            if (db.Balance < (item.Price * count))
            {
                DrawUI_ShowNotify(player, "Недостаточно средств", true);
            }
            else
            {
                if ((24 - player.inventory.containerMain.itemList.Count) < count)
                {
                    DrawUI_ShowNotify(player, "Недостаточно места в инвентаре", true);
                    return;
                }

                var info = ItemManager.FindItemDefinition(item.Shortname);
                var giveItem = ItemManager.Create(info, (item.FixCount * count));
                giveItem.skin = item.SkinID;

                player.GiveItem(giveItem, BaseEntity.GiveItemReason.Generic);

                db.Balance -= item.Price * count;
                DrawUI_ShowNotify(player, "Товар <color=#7fff6a>успешно</color> куплен !", false);
                CuiHelper.DestroyUi(player, LayerBuyMenu);
            }
        }

        private void CheckPlayerForDataBase()
        {
            var count = BasePlayer.activePlayerList.Count;
            for (int i = 0, num = 1; num <= count; num++, i++)
            {
                CheckDataBase(BasePlayer.activePlayerList[i]);
            }
        }

        private IEnumerator DownloadImages()
        {
            ImageLibrary.Call("AddImage", "https://i.imgur.com/smQzXMI.png", "CategoryBG");

            foreach (var item in _config.ZealShop.TradeItems)
            {
                if ((bool) ImageLibrary.CallHook("HasImage", item.Key + 128) != true)
                {
                    ImageLibrary.CallHook("AddImage", $"https://rustlabs.com/img/items180/{item.Key}.png",
                        item.Key + 128);
                }

                yield return new WaitForSeconds(0.5f);
            }

            foreach (var item in _config.ZealShop.Gun)
            {
                if (item.SkinID != 0)
                {
                    ImageLibrary.CallHook("AddImage", $"https://rustlabs.com/img/skins/324/{item.SkinID}",
                        $"{item.Shortname}_{128}_{item.SkinID}");
                }
                else
                {
                    ImageLibrary.CallHook("AddImage",
                        $"https://rustlabs.com/img/items180/{item.Shortname}.png",
                        $"{item.Shortname}_{128}_{0}");
                }

                yield return new WaitForSeconds(0.5f);
            }

            foreach (var item in _config.ZealShop.Resources)
            {
                if ((bool) ImageLibrary.CallHook("HasImage", $"{item.Shortname}_{128}_{item.SkinID}") != true)
                {
                    if (item.SkinID != 0)
                    {
                        ImageLibrary.CallHook("AddImage", $"https://rustlabs.com/img/skins/324/{item.SkinID}",
                            $"{item.Shortname}_{128}_{item.SkinID}");
                    }
                    else
                    {
                        ImageLibrary.CallHook("AddImage",
                            $"https://rustlabs.com/img/items180/{item.Shortname}.png",
                            $"{item.Shortname}_{128}_{0}");
                    }
                }

                yield return new WaitForSeconds(0.5f);
            }

            foreach (var item in _config.ZealShop.Construction)
            {
                if ((bool) ImageLibrary.CallHook("HasImage", $"{item.Shortname}_{128}_{item.SkinID}") != true)
                {
                    if (item.SkinID != 0)
                    {
                        ImageLibrary.CallHook("AddImage", $"https://rustlabs.com/img/skins/324/{item.SkinID}",
                            $"{item.Shortname}_{128}_{item.SkinID}");
                    }
                    else
                    {
                        ImageLibrary.CallHook("AddImage", $"https://rustlabs.com/img/items180/{item.Shortname}.png",
                            $"{item.Shortname}_{128}_{0}");
                    }
                }

                yield return new WaitForSeconds(0.5f);
            }

            foreach (var item in _config.ZealShop.Items)
            {
                if ((bool) ImageLibrary.CallHook("HasImage", $"{item.Shortname}_{128}_{item.SkinID}") != true)
                {
                    if (item.SkinID != 0)
                    {
                        ImageLibrary.CallHook("AddImage", $"https://rustlabs.com/img/skins/324/{item.SkinID}",
                            $"{item.Shortname}_{128}_{item.SkinID}");
                    }
                    else
                    {
                        ImageLibrary.CallHook("AddImage",
                            $"https://rustlabs.com/img/items180/{item.Shortname}.png",
                            $"{item.Shortname}_{128}_{0}");
                    }
                }

                yield return new WaitForSeconds(0.5f);
            }

            foreach (var item in _config.ZealShop.Attire)
            {
                if ((bool) ImageLibrary.CallHook("HasImage", $"{item.Shortname}_{128}_{item.SkinID}") != true)
                {
                    if (item.SkinID != 0)
                    {
                        ImageLibrary.CallHook("AddImage", $"https://rustlabs.com/img/skins/324/{item.SkinID}",
                            $"{item.Shortname}_{128}_{item.SkinID}");
                    }
                    else
                    {
                        ImageLibrary.CallHook("AddImage",
                            $"https://rustlabs.com/img/items180/{item.Shortname}.png",
                            $"{item.Shortname}_{128}_{0}");
                    }
                }

                yield return new WaitForSeconds(0.5f);
            }

            foreach (var item in _config.ZealShop.Tools)
            {
                if ((bool) ImageLibrary.CallHook("HasImage", $"{item.Shortname}_{128}_{item.SkinID}") != true)
                {
                    if (item.SkinID != 0)
                    {
                        ImageLibrary.CallHook("AddImage", $"https://rustlabs.com/img/skins/324/{item.SkinID}",
                            $"{item.Shortname}_{128}_{item.SkinID}");
                    }
                    else
                    {
                        ImageLibrary.CallHook("AddImage",
                            $"https://rustlabs.com/img/items180/{item.Shortname}.png",
                            $"{item.Shortname}_{128}_{0}");
                    }
                }

                yield return new WaitForSeconds(0.5f);
            }

            foreach (var item in _config.ZealShop.Medicals)
            {
                if ((bool) ImageLibrary.CallHook("HasImage", $"{item.Shortname}_{128}_{item.SkinID}") != true)
                {
                    if (item.SkinID != 0)
                    {
                        ImageLibrary.CallHook("AddImage", $"https://rustlabs.com/img/skins/324/{item.SkinID}",
                            $"{item.Shortname}_{128}_{item.SkinID}");
                    }
                    else
                    {
                        ImageLibrary.CallHook("AddImage",
                            $"https://rustlabs.com/img/items180/{item.Shortname}.png",
                            $"{item.Shortname}_{128}_{0}");
                    }
                }

                yield return new WaitForSeconds(0.5f);
            }

            foreach (var item in _config.ZealShop.Food)
            {
                if ((bool) ImageLibrary.CallHook("HasImage", $"{item.Shortname}_{128}_{item.SkinID}") != true)
                {
                    if (item.SkinID != 0)
                    {
                        ImageLibrary.CallHook("AddImage", $"https://rustlabs.com/img/skins/324/{item.SkinID}",
                            $"{item.Shortname}_{128}_{item.SkinID}");
                    }
                    else
                    {
                        ImageLibrary.CallHook("AddImage",
                            $"https://rustlabs.com/img/items180/{item.Shortname}.png",
                            $"{item.Shortname}_{128}_{0}");
                    }
                }

                yield return new WaitForSeconds(0.5f);
            }

            foreach (var item in _config.ZealShop.Ammunations)
            {
                if ((bool) ImageLibrary.CallHook("HasImage", $"{item.Shortname}_{128}_{item.SkinID}") != true)
                {
                    if (item.SkinID != 0)
                    {
                        ImageLibrary.CallHook("AddImage", $"https://rustlabs.com/img/skins/324/{item.SkinID}",
                            $"{item.Shortname}_{128}_{item.SkinID}");
                    }
                    else
                    {
                        ImageLibrary.CallHook("AddImage",
                            $"https://rustlabs.com/img/items180/{item.Shortname}.png",
                            $"{item.Shortname}_{128}_{0}");
                    }
                }

                yield return new WaitForSeconds(0.5f);
            }

            foreach (var item in _config.ZealShop.Traps)
            {
                if ((bool) ImageLibrary.CallHook("HasImage", $"{item.Shortname}_{128}_{item.SkinID}") != true)
                {
                    if (item.SkinID != 0)
                    {
                        ImageLibrary.CallHook("AddImage", $"https://rustlabs.com/img/skins/324/{item.SkinID}",
                            $"{item.Shortname}_{128}_{item.SkinID}");
                    }
                    else
                    {
                        ImageLibrary.CallHook("AddImage",
                            $"https://rustlabs.com/img/items180/{item.Shortname}.png",
                            $"{item.Shortname}_{128}_{0}");
                    }
                }

                yield return new WaitForSeconds(0.5f);
            }

            foreach (var item in _config.ZealShop.Misc)
            {
                if ((bool) ImageLibrary.CallHook("HasImage", $"{item.Shortname}_{128}_{item.SkinID}") != true)
                {
                    if (item.SkinID != 0)
                    {
                        ImageLibrary.CallHook("AddImage", $"https://rustlabs.com/img/skins/324/{item.SkinID}",
                            $"{item.Shortname}_{128}_{item.SkinID}");
                    }
                    else
                    {
                        ImageLibrary.CallHook("AddImage",
                            $"https://rustlabs.com/img/items180/{item.Shortname}.png",
                            $"{item.Shortname}_{128}_{0}");
                    }
                }

                yield return new WaitForSeconds(0.5f);
            }

            foreach (var item in _config.ZealShop.Components)
            {
                if ((bool) ImageLibrary.CallHook("HasImage", $"{item.Shortname}_{128}_{item.SkinID}") != true)
                {
                    if (item.SkinID != 0)
                    {
                        ImageLibrary.CallHook("AddImage", $"https://rustlabs.com/img/skins/324/{item.SkinID}",
                            $"{item.Shortname}_{128}_{item.SkinID}");
                    }
                    else
                    {
                        ImageLibrary.CallHook("AddImage",
                            $"https://rustlabs.com/img/items180/{item.Shortname}.png",
                            $"{item.Shortname}_{128}_{0}");
                    }
                }

                yield return new WaitForSeconds(0.5f);
            }

            yield return 0;
        }

        private void CheckDataBase(BasePlayer player)
        {
            if (!_dataBase.ListPlayers.ContainsKey(player.userID)) AddPlayer(player);
        }

        private void AddPlayer(BasePlayer player)
        {
            PrintWarning($"Игрок добавлен в базу : {player.displayName}");
            _dataBase.ListPlayers.Add(player.userID, new PlayerShopInfo
            {
                Name = player.displayName,
                SteamID = player.userID,
                Balance = 0
            });
            SaveData();
        }

        private static void DestroyItems(BasePlayer player)
        {
            for (var i = 0; i <= 28; i++)
            {
                CuiHelper.DestroyUi(player, $"Box_Item{i}");
            }
        }

        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');
            if (str.Length == 6)
                str += "FF";
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

        #region [API] / [АПИ]

        [HookMethod("GetBalance")]
        public float GetBalance(ulong steamID)
        {
            if (!_dataBase.ListPlayers.ContainsKey(steamID))
            {
                return 0;
            }
            else
            {
                return _dataBase.ListPlayers[steamID].Balance;
            }
        }

        #endregion
    }
}