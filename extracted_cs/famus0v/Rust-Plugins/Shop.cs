using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Facepunch.Extend;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Shop", "Kira", "1.0.0")]
    [Description("A unique store for GalaxyRust")]
    public class Shop : RustPlugin
    {
        [PluginReference] private Plugin ImageLibrary, BankSystem;

        #region [Vars] / [Переменные]

        private const string UIParent = "UI.Parent";
        private const string UIMain = "UI.Exchanger";
        private readonly WaitForSeconds Wait = new WaitForSeconds(0.1f);
        private StoredData _dataBase = new StoredData();
        private const string Regular = "robotocondensed-regular.ttf";

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

        public class Cart
        {
            public ulong SteamID;

            public class CartObj
            {
                public string Shortname;
                public int Price;
                public int FixCount;
                public ulong SkinID;
                public int Count = 1;
            }

            public Dictionary<string, CartObj> Objects = new Dictionary<string, CartObj>();
        }

        #endregion

        #region [Lang]

        protected override void LoadDefaultMessages()
        {
            var ru = new Dictionary<string, string>
            {
                ["ATTIRE"] = "Одежда",
                ["MISK"] = "Прочее",
                ["ITEMS"] = "Предметы",
                ["AMMUNATIONS"] = "Аммуниция",
                ["CONSTRUCTIONS"] = "Конструкции",
                ["COMPONENTS"] = "Компоненты",
                ["TRAPS"] = "Ловушки",
                ["ELECTRICAL"] = "Электрика",
                ["FUN"] = "Развлечение",
                ["FOOD"] = "Еда",
                ["RESOURCES"] = "Ресурсы",
                ["TOOLS"] = "Инструменты",
                ["WEAPONS"] = "Оружие",
                ["MEDICAL"] = "Медикаменты",
                ["SHOPPING_BAG"] = "КОРЗИНА",
                ["CATEGORIES"] = "КАТЕГОРИИ",
                ["BUY"] = "ОПЛАТИТЬ",
                ["SEARCH"] = "ПОИСК",
                ["COST"] = "СТОИМОСТЬ",
                ["AMOUNT"] = "Кол-во",
                ["EMPTY"] = "Пусто :(",
            };

            var en = new Dictionary<string, string>
            {
                ["ATTIRE"] = "Attire",
                ["MISK"] = "Misk",
                ["ITEMS"] = "Items",
                ["AMMUNATIONS"] = "Ammunations",
                ["CONSTRUCTIONS"] = "Constructions",
                ["COMPONENTS"] = "Components",
                ["TRAPS"] = "Traps",
                ["ELECTRICAL"] = "Electrical",
                ["FUN"] = "Fun",
                ["FOOD"] = "Food",
                ["RESOURCES"] = "Resources",
                ["TOOLS"] = "Tools",
                ["WEAPONS"] = "Weapons",
                ["MEDICAL"] = "Medical",
                ["SHOPPING_BAG"] = "SHOPPING BAG",
                ["CATEGORIES"] = "CATEGORIES",
                ["BUY"] = "BUY",
                ["SEARCH"] = "SEARCH",
                ["COST"] = "COST",
                ["AMOUNT"] = "Amount",
                ["EMPTY"] = "EMPTY :(",
            };
            lang.RegisterMessages(ru, this, "ru");
            lang.RegisterMessages(en, this);
        }

        #endregion

        #region [Configuraton] / [Конфигурация]

        private static ConfigData _config;

        public class ConfigData
        {
            [JsonProperty(PropertyName = "Shop")] public ShopCfg Shop = new ShopCfg();

            public class ShopCfg
            {
                [JsonProperty(PropertyName = "Комиссия при продаже")]
                public int SellCommision = 70;

                [JsonProperty(PropertyName = "Активные категории")]
                public List<string> Categories = new List<string>();

                [JsonProperty(PropertyName = "Бот ОПГ")]
                public Vector3 BotOCG;

                [JsonProperty(PropertyName = "Бот WSARMY")]
                public Vector3 BotWSARMY;

                [JsonProperty(PropertyName = "Бот нейтральный")]
                public Vector3 BotNEUTRAL;

                [JsonProperty(PropertyName = "Оружие")]
                public List<ItemShop> Gun = new List<ItemShop>();

                [JsonProperty(PropertyName = "Развлечения")]
                public List<ItemShop> Fun = new List<ItemShop>();

                [JsonProperty(PropertyName = "Постройки")]
                public List<ItemShop> Construction = new List<ItemShop>();

                [JsonProperty(PropertyName = "Предметы")]
                public List<ItemShop> Items = new List<ItemShop>();

                [JsonProperty(PropertyName = "Электрика")]
                public List<ItemShop> Electric = new List<ItemShop>();

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
            }
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                Shop = new ConfigData.ShopCfg
                {
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
                    Electric = _electrical,
                    Construction = _construction,
                    Fun = _misc,
                    Categories =
                    {
                        "Attire",
                        "Misk",
                        "Items",
                        "Ammunations",
                        "Constructions",
                        "Components",
                        "Traps",
                        "Electrical",
                        "Fun",
                        "Food",
                        "Resources",
                        "Weapons",
                        "Medical"
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

        #region [Dictionary] / [Словари]

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

        private readonly List<ItemShop> _electrical = new List<ItemShop>
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

        private readonly Dictionary<ulong, Cart> Carts = new Dictionary<ulong, Cart>();

        private readonly Dictionary<string, string> Images = new Dictionary<string, string>
        {
            ["UI.Category"] = "https://i.imgur.com/anVvC9P.png",
            ["UI.Category.Element"] = "https://i.imgur.com/Zi9aUyI.png",
            ["UI.Cart"] = "https://i.imgur.com/cF8QDEF.png",
            ["UI.Cart.Element"] = "https://i.imgur.com/1I9BxNb.png",
            ["UI.Shop.Element"] = "https://i.imgur.com/ytuZG0q.png",
            ["UI.Search"] = "https://i.imgur.com/NiOLTVa.png",
            ["UI.Balance"] = "https://i.imgur.com/cVO2iuV.png"
        };

        #endregion

        #region [DrawUI] / [Отрисовка UI]

        private void DrawUI_Main(BasePlayer player)
        {
            var ui = new CuiElementContainer
            {
                {
                    new CuiPanel
                    {
                        CursorEnabled = true,
                        Image =
                        {
                            Color = "0.13 0.13 0.15 1.00"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    },
                    "Overlay", UIParent
                }
            };

            ui.Add(new CuiButton
            {
                Button =
                {
                    Close = UIParent,
                    Color = "0 0 0 0"
                },
                Text = { Text = " " },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, UIParent);

            ui.Add(new CuiElement
            {
                Name = $"{UIParent}.Categories",
                Parent = UIParent,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage("UI.Category")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.04603646 0.1107693",
                        AnchorMax = "0.1841226 0.8892308"
                    }
                }
            });
            ui.Add(new CuiLabel
            {
                Text =
                {
                    Text = lang.GetMessage("CATEGORIES", this, player.UserIDString),
                    FontSize = 14,
                    Align = TextAnchor.MiddleCenter,
                    Color = "0.94 0.39 0.12 1.00"
                },
                RectTransform =
                {
                    AnchorMin = "0 0.9365209",
                    AnchorMax = "1 1"
                }
            }, $"{UIParent}.Categories", $"{UIParent}.Category.Name");


            ui.Add(new CuiElement
            {
                Name = $"{UIParent}.Cart",
                Parent = UIParent,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage("UI.Cart")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.78025 0.1107692",
                        AnchorMax = "0.95394 0.8892307"
                    }
                }
            });

            ui.Add(new CuiLabel
            {
                Text =
                {
                    Text = lang.GetMessage("SHOPPING_BAG", this, player.UserIDString),
                    FontSize = 14,
                    Align = TextAnchor.MiddleCenter,
                    Color = "0.94 0.39 0.12 1.00"
                },
                RectTransform =
                {
                    AnchorMin = "0 0.9365209",
                    AnchorMax = "1 1"
                }
            }, $"{UIParent}.Cart", $"{UIParent}.Cart.Name");

            ui.Add(new CuiButton
            {
                Button =
                {
                    Command = "shop.buy",
                    Color = "0 0 0 0"
                },
                Text =
                {
                    Text = lang.GetMessage("BUY", this, player.UserIDString),
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.66",
                    FontSize = 15
                },
                RectTransform =
                {
                    AnchorMin = "0.5515097 0.01828068",
                    AnchorMax = "0.95 0.0652"
                }
            }, $"{UIParent}.Cart");

            CuiHelper.DestroyUi(player, UIParent);
            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_Products(BasePlayer player, List<ItemShop> category, int page)
        {
            var ui = new CuiElementContainer();

            for (var i = 0; i <= 15; i++)
            {
                var element = $"{UIParent}.Shop.Element.{i}";
                CuiHelper.DestroyUi(player, element);
            }

            int num = 0, x = 0, y = 0;
            foreach (var item in category.Skip(15 * page).Take(15))
            {
                if (x == 3)
                {
                    x = 0;
                    y++;
                }

                var element = $"{UIParent}.Shop.Element.{num}";

                ui.Add(new CuiElement
                {
                    Name = element,
                    Parent = UIParent,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage("UI.Shop.Element"),
                            FadeIn = num * 0.1f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.2098125 + (x * 0.1904948)} {0.7717459 - (y * 0.165276025)}",
                            AnchorMax = $"{0.3732214 + (x * 0.1904948)} {0.8890553 - (y * 0.165276025)}"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UIParent}.Shop.Element.Icon.{num}",
                    Parent = element,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage($"{item.Shortname}_{128}_{item.SkinID}"),
                            FadeIn = num * 0.1f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.06915893 0.1671076",
                            AnchorMax = "0.3380075 0.8328924"
                        }
                    }
                });

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = $"<b>{item.Name}</b>\n" +
                               $"<size=9>{lang.GetMessage("AMOUNT", this, player.UserIDString)} : x{item.FixCount}</size>",
                        FontSize = 12,
                        Align = TextAnchor.UpperLeft,
                        Color = "0.39 0.40 0.43 1.00",
                        Font = Regular
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.4152625 0.303995",
                        AnchorMax = "1 0.8801864"
                    }
                }, element, $"{UIParent}.Shop.Element.Description.{num}");

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = $"{item.Price}$",
                        FontSize = 12,
                        Align = TextAnchor.MiddleRight,
                        Color = "0.39 0.40 0.44 1.00"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5359766 0.07509762",
                        AnchorMax = "0.7941478 0.2882091"
                    }
                }, element, $"{UIParent}.Shop.Element.Cost.{num}");

                ui.Add(new CuiElement
                {
                    Name = $"{UIParent}.Shop.Element.AddCart",
                    Parent = element,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Sprite = "assets/icons/store.png",
                            Color = "1 1 1 0.66"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.8605713 0.1105021",
                            AnchorMax = "0.92113 0.2604696"
                        }
                    }
                });

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"shop.addcart {item.Shortname} {item.FixCount} {item.Price} {item.SkinID} {page}",
                        Color = "0 0 0 0"
                    },
                    Text = { Text = " " },
                    RectTransform =
                    {
                        AnchorMin = "0.8164589 0.07509762",
                        AnchorMax = "0.9726366 0.2882091"
                    }
                }, element);

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"exchange {item.Shortname} {item.FixCount} {item.Price}",
                        Color = "0 0 0 0"
                    },
                    Text =
                    {
                        Text = "SELL",
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 10,
                        Color = "1 1 1 0.5"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.8164591 0.7144322",
                        AnchorMax = "0.9726368 0.92"
                    }
                }, element);

                x++;
                num++;
            }

            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_Categories(BasePlayer player)
        {
            var ui = new CuiElementContainer();

            var y = 0;
            foreach (var category in _config.Shop.Categories)
            {
                ui.Add(new CuiElement
                {
                    Name = $"{UIParent}.Category.{y}",
                    Parent = $"{UIParent}.Categories",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage("UI.Category.Element"),
                            FadeIn = y * 0.1f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0.05076629 {0.8675303 - (y * 0.0649608638461538)}",
                            AnchorMax = $"0.9492337 {0.9127247 - (y * 0.0649608638461538)}"
                        }
                    }
                });

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"shop.category {category}",
                        Color = "0 0 0 0"
                    },
                    Text =
                    {
                        Text = lang.GetMessage(category.ToUpper(), this, player.UserIDString),
                        FontSize = 14,
                        Align = TextAnchor.MiddleLeft,
                        Color = "0.34 0.34 0.35 1.00"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.1 0",
                        AnchorMax = "1 1"
                    }
                }, $"{UIParent}.Category.{y}");

                y++;
            }

            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_Search(BasePlayer player, string category)
        {
            var ui = new CuiElementContainer
            {
                new CuiElement
                {
                    Name = $"{UIParent}.Search",
                    Parent = UIParent,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage("UI.Search")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.3803281 0.02937963",
                            AnchorMax = "0.5955838 0.07783516"
                        }
                    }
                }
            };

            ui.Add(new CuiLabel
            {
                Text =
                {
                    Text = $"{lang.GetMessage("SEARCH", this, player.UserIDString)}",
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter,
                    Color = "0.39 0.40 0.44 0.25"
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, $"{UIParent}.Search");

            ui.Add(new CuiElement
            {
                Name = $"{UIParent}.Search.Input",
                Parent = $"{UIParent}.Search",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Color = "1 1 1 0.3",
                        FontSize = 18,
                        Align = TextAnchor.MiddleCenter,
                        Command = $"shop.search {category}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.09138834 0",
                        AnchorMax = "0.9080554 1"
                    }
                }
            });

            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_Pagination(BasePlayer player, int page, string category)
        {
            var ui = new CuiElementContainer
            {
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"shop.page {page - 1} {category}",
                            Color = "0 0 0 0"
                        },
                        Text = { Text = " " },
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0.09138834 1"
                        }
                    },
                    $"{UIParent}.Search", $"{UIParent}.Pages.Left"
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"shop.page {page + 1} {category}",
                            Color = "0 0 0 0"
                        },
                        Text = { Text = " " },
                        RectTransform =
                        {
                            AnchorMin = "0.9080554 0",
                            AnchorMax = "0.9994433 1"
                        }
                    },
                    $"{UIParent}.Search", $"{UIParent}.Pages.Right"
                }
            };


            CuiHelper.DestroyUi(player, $"{UIParent}.Pages.Left");
            CuiHelper.DestroyUi(player, $"{UIParent}.Pages.Right");
            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_ShoppingBag(BasePlayer player, int page)
        {
            CuiHelper.DestroyUi(player, $"{UIParent}.Cart.Empty");
            CuiHelper.DestroyUi(player, $"{UIParent}.Cart.Balance");
            CuiHelper.DestroyUi(player, $"{UIParent}.Cart.Cost");
            ClearCart(player);
            var ui = new CuiElementContainer();
            if (!Carts.ContainsKey(player.userID) || Carts[player.userID].Objects.Count == 0)
            {
                var empty = new CuiElementContainer
                {
                    {
                        new CuiLabel
                        {
                            Text =
                            {
                                Text = $"{lang.GetMessage("EMPTY", this, player.UserIDString)}",
                                FontSize = 14,
                                Align = TextAnchor.MiddleCenter,
                                Color = "0.39 0.40 0.44 0.25"
                            },
                            RectTransform =
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "1 1"
                            }
                        },
                        $"{UIParent}.Cart", $"{UIParent}.Cart.Empty"
                    }
                };
                ClearCart(player);
                CuiHelper.DestroyUi(player, $"{UIParent}.Cart.Empty");
                CuiHelper.AddUi(player, empty);
                return;
            }

            ClearCart(player);
            var cart = Carts[player.userID].Objects;
            if (cart == null) return;

            int num = 0, y = 0;
            foreach (var item in cart.Skip(page * 6).Take(6))
            {
                var obj = ItemManager.FindItemDefinition(item.Value.Shortname);
                ui.Add(new CuiElement
                {
                    Name = $"{UIParent}.Cart.Element.{num}",
                    Parent = $"{UIParent}.Cart",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage("UI.Cart.Element")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0.02959746 {0.7426091 - (y * 0.11666412)}",
                            AnchorMax = $"0.9704025 {0.8407729 - (y * 0.11666412)}"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UIParent}.Cart.Element.Icon.{num}",
                    Parent = $"{UIParent}.Cart.Element.{num}",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage($"{obj.shortname}_{128}_{item.Value.SkinID}")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.0486189 0.1774717",
                            AnchorMax = "0.2183013 0.8225283"
                        }
                    }
                });

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = $"<b>{obj.displayName.translated}</b>\n" +
                               $"<size=9>{lang.GetMessage("AMOUNT", this, player.UserIDString)} : x{item.Value.FixCount * item.Value.Count}</size>",
                        FontSize = 10,
                        Align = TextAnchor.UpperLeft,
                        Color = "0.39 0.40 0.43 1.00",
                        Font = Regular
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.2816691 0",
                        AnchorMax = "0.8 0.8225298"
                    }
                }, $"{UIParent}.Cart.Element.{num}", $"{UIParent}.Cart.Element.Description.{num}");

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = $"{item.Value.Count}",
                        FontSize = 10,
                        Align = TextAnchor.MiddleCenter,
                        Color = "0.39 0.40 0.43 1.00"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.7555507 0.3396243",
                        AnchorMax = "0.9563512 0.6304272"
                    }
                }, $"{UIParent}.Cart.Element.{num}", $"{UIParent}.Cart.Element.Count.{num}");

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Command =
                            $"shop.addcart {item.Value.Shortname} {item.Value.FixCount} {item.Value.Price} {item.Value.SkinID} {page}",
                        Color = "0 0 0 0"
                    },
                    Text = { Text = " " },
                    RectTransform =
                    {
                        AnchorMin = "0.8256635 0.6454015",
                        AnchorMax = "0.8894097 0.8877373"
                    }
                }, $"{UIParent}.Cart.Element.{num}");

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Command =
                            $"shop.remcart {item.Value.Shortname} {item.Value.FixCount} {item.Value.Price} {item.Value.SkinID} {page}",
                        Color = "0 0 0 0"
                    },
                    Text = { Text = " " },
                    RectTransform =
                    {
                        AnchorMin = "0.8256635 0.09728859",
                        AnchorMax = "0.8894097 0.3396243"
                    }
                }, $"{UIParent}.Cart.Element.{num}");

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Command =
                            $"shop.delcart {item.Value.Shortname} {item.Value.FixCount} {item.Value.Price} {item.Value.SkinID} {page}",
                        Color = "0 0 0 0"
                    },
                    Text = { Text = " " },
                    RectTransform =
                    {
                        AnchorMin = "0.9085423 0",
                        AnchorMax = "1 1"
                    }
                }, $"{UIParent}.Cart.Element.{num}");

                y++;
                num++;
            }

            var cost = cart.Sum(item => item.Value.Price * item.Value.Count);

            ui.Add(new CuiLabel
            {
                Text =
                {
                    Text = $"<b>{lang.GetMessage("COST", this, player.UserIDString).ToUpper()}</b>\n" +
                           $"<size=12>{cost} $</size>",
                    FontSize = 13,
                    Align = TextAnchor.UpperRight,
                    Color = "0.39 0.40 0.43 1.00",
                    Font = Regular
                },
                RectTransform =
                {
                    AnchorMin = "0.1047115 0.004007513",
                    AnchorMax = "0.5125253 0.06347904"
                }
            }, $"{UIParent}.Cart", $"{UIParent}.Cart.Cost");

            ui.Add(new CuiElement
            {
                Name = $"{UIParent}.Cart.Balance",
                Parent = $"{UIParent}.Cart",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage("UI.Balance")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 -0.09396505",
                        AnchorMax = "1 -0.01854637"
                    }
                }
            });

            var balance = GetBalance(player.userID);
            var remains = balance - cost;
            var color = remains > 0 ? "#63666f" : "#CA181880";
            ui.Add(new CuiLabel
            {
                Text =
                {
                    Text = $"{balance} ➜ <color={color}>{remains}</color>",
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter,
                    Color = "0.39 0.40 0.43 1.00"
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, $"{UIParent}.Cart.Balance");

            CuiHelper.AddUi(player, ui);
            DrawUI_ShoppingPagination(player, page);
        }

        private void DrawUI_ShoppingPagination(BasePlayer player, int page)
        {
            var ui = new CuiElementContainer();
            if (!Carts.ContainsKey(player.userID)) return;
            var cart = Carts[player.userID].Objects;
            if (cart == null) return;

            if (page > 0)
            {
                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"shop.cart {page - 1}",
                        Color = "0 0 0 0"
                    },
                    Text =
                    {
                        Text = "▲",
                        FontSize = 20,
                        Color = "0.39 0.40 0.44 1.00",
                        Align = TextAnchor.MiddleCenter
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.4250342 0.8421808",
                        AnchorMax = "0.5749662 0.9016523"
                    }
                }, $"{UIParent}.Cart", $"{UIParent}.Cart.Left");
            }

            if (Convert.ToInt32(cart.Count / 7) > page)
            {
                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"shop.cart {page + 1}",
                        Color = "0 0 0 0"
                    },
                    Text =
                    {
                        Text = "▼",
                        FontSize = 20,
                        Color = "0.39 0.40 0.44 1.00",
                        Align = TextAnchor.MiddleCenter
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.4250338 0.09996909",
                        AnchorMax = "0.5749658 0.1594406"
                    }
                }, $"{UIParent}.Cart", $"{UIParent}.Cart.Right");
            }

            CuiHelper.DestroyUi(player, $"{UIParent}.Cart.Left");
            CuiHelper.DestroyUi(player, $"{UIParent}.Cart.Right");
            CuiHelper.AddUi(player, ui);
        }

        #endregion

        #region [Hooks] / [Крюки]

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            LoadData();
            ServerMgr.Instance.StartCoroutine(LoadImages());
            ImageLibrary.Call("AddImage", "https://i.imgur.com/vMjsEjU.png", $"{UIMain}.Background");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/gUoJNvH.png", $"{UIMain}.ItemBG");
        }

        // ReSharper disable once UnusedMember.Local
        bool CanPickupEntity(BasePlayer player, BaseEntity entity)
        {
            if (entity.PrefabName != "assets/prefabs/deployable/bed/bed_deployed.prefab") return true;
            if (!_dataBase.BlockedItems.ContainsKey(player.userID))
                _dataBase.BlockedItems.Add(player.userID, new List<StoredData.ItemBlock>());
            var db1 = _dataBase.BlockedItems[player.userID].Find(x => x.ShortName == "bed");
            db1.Count = 0;
            return true;
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private object CanDeployItem(BasePlayer player, Deployer deployer, uint entityId)
        {
            if (deployer.GetModDeployable().entityPrefab.GetEntity().ShortPrefabName != "lock.code") return null;
            if (!_dataBase.BlockedItems.ContainsKey(player.userID))
                _dataBase.BlockedItems.Add(player.userID, new List<StoredData.ItemBlock>());
            var db1 = _dataBase.BlockedItems[player.userID].Find(x => x.ShortName == "lock.code");
            if (db1 == null)
            {
                _dataBase.BlockedItems[player.userID].Add(new StoredData.ItemBlock
                    { ShortName = "lock.code", Count = 0, Limit = 1 });
                db1 = _dataBase.BlockedItems[player.userID].Find(x => x.ShortName == "lock.code");
            }

            if (db1.Count < db1.Limit)
                db1.Count++;
            else return false;
            return null;
        }

        // ReSharper disable once UnusedMember.Local
        private bool CanPickupLock(BasePlayer player, BaseLock baseLock)
        {
            baseLock.Kill();
            if (!_dataBase.BlockedItems.ContainsKey(player.userID))
                _dataBase.BlockedItems.Add(player.userID, new List<StoredData.ItemBlock>());
            var db1 = _dataBase.BlockedItems[player.userID].Find(x => x.ShortName == "lock.code");
            if (db1 == null)
            {
                _dataBase.BlockedItems[player.userID].Add(new StoredData.ItemBlock
                    { ShortName = "lock.code", Count = 0, Limit = 1 });
                db1 = _dataBase.BlockedItems[player.userID].Find(x => x.ShortName == "lock.code");
            }

            if (db1.Count > 0) db1.Count = 0;

            var giveItem2 = ItemManager.CreateByName("lock.code");
            player.GiveItem(giveItem2);
            return false;
        }

        // ReSharper disable once UnusedMember.Local
        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            if (plan == null | go == null) return;
            var player = plan.GetOwnerPlayer();
            var obj = go.ToBaseEntity();
            PrintToChat(obj.PrefabName);
            switch (obj.PrefabName)
            {
                case "assets/prefabs/deployable/bed/bed_deployed.prefab":
                    if (!_dataBase.BlockedItems.ContainsKey(player.userID))
                        _dataBase.BlockedItems.Add(player.userID, new List<StoredData.ItemBlock>());
                    var db1 = _dataBase.BlockedItems[player.userID].Find(x => x.ShortName == "bed");
                    if (db1 == null)
                    {
                        _dataBase.BlockedItems[player.userID].Add(new StoredData.ItemBlock
                            { ShortName = "bed", Count = 0, Limit = 1 });
                        db1 = _dataBase.BlockedItems[player.userID].Find(x => x.ShortName == "bed");
                    }

                    if (db1.Count >= 1)
                    {
                        NextFrame(() => obj.Kill());
                        var giveItem2 = ItemManager.CreateByName("bed");
                        player.GiveItem(giveItem2);
                    }
                    else db1.Count++;

                    break;
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void Unload()
        {
            SaveData();
        }

        #endregion

        #region [Chat Commands] / [Чат команды]

        [ConsoleCommand("close")]
        // ReSharper disable once UnusedMember.Local
        private void asdd(ConsoleSystem.Arg args)
        {
            CuiHelper.DestroyUi(args.Player(), UIMain);
        }

        // ReSharper disable once UnusedMember.Local
        [ChatCommand("shop")]
        private void OpenShop(BasePlayer player)
        {
            DrawUI_Main(player);
            DrawUI_Products(player, _config.Shop.Attire, 0);
            DrawUI_Categories(player);
            DrawUI_Search(player, "Attire");
            DrawUI_Pagination(player, 0, "Attire");
            DrawUI_ShoppingBag(player, 0);
        }

        [ChatCommand("shop.close")]
        // ReSharper disable once UnusedMember.Local
        private void CloseShop(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UIParent);
        }

        [ConsoleCommand("shop.category")]
        // ReSharper disable once UnusedMember.Local
        private void Category(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            var category = GetCategory(args.Args[0]);
            DrawUI_Search(player, args.Args[0]);
            DrawUI_Products(player, category, 0);
            DrawUI_Pagination(player, 0, args.Args[0]);
            DrawUI_ShoppingBag(player, 0);
        }

        [ConsoleCommand("shop.search")]
        // ReSharper disable once UnusedMember.Local
        private void Search(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (args.Args.Length < 1) return;
            if (args.Args.Length < 2)
            {
                DrawUI_Products(player, GetCategory(args.Args[0]), 0);
                return;
            }

            var category = args.Args[0];
            var item = args.Args[1];

            DrawUI_Products(player, GetFindItem(item, category), 0);
            DrawUI_Search(player, category);
            DrawUI_Pagination(player, 0, category);
            DrawUI_ShoppingBag(player, 0);
        }

        [ConsoleCommand("shop.page")]
        // ReSharper disable once UnusedMember.Local
        private void Pagination(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            var page = args.Args[0].ToInt();
            if (page < 0) return;
            var category = GetCategory(args.Args[1]);
            if (!category.Skip(15 * page).Take(15).Any() & page > 0) return;

            DrawUI_Pagination(player, page, args.Args[1]);
            DrawUI_Products(player, category, page);
        }

        [ConsoleCommand("exchange")]
        // ReSharper disable once UnusedMember.Local
        private void ShopSell(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            var shortname = args.Args[0];
            var getitem = GetItem(shortname);
            if (getitem == null) return;
            var math = (double)getitem.Price / 100 * _config.Shop.SellCommision;
            var item = player.inventory.FindItemID(shortname);
            if (item == null) return;
            if (item.amount < getitem.FixCount) return;
            if (item.condition < (item._maxCondition / 2)) return;
            player.inventory.Take(null, ItemManager.FindItemDefinition(shortname).itemid, getitem.FixCount);
            GiveBalance(player.userID, Convert.ToInt32(math));
        }

        [ConsoleCommand("shop.addcart")]
        // ReSharper disable once UnusedMember.Local
        private void AddCart(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!Carts.ContainsKey(player.userID)) Carts.Add(player.userID, new Cart { SteamID = player.userID });
            var list = Carts[player.userID].Objects;
            var shortname = args.Args[0];
            var fixprice = args.Args[1];
            var price = args.Args[2];
            if (list.ContainsKey(args.Args[0]))
            {
                var item = list[shortname];
                item.Count++;
            }
            else
            {
                list.Add(shortname, new Cart.CartObj
                {
                    Shortname = shortname,
                    FixCount = fixprice.ToInt(),
                    Price = price.ToInt(),
                    SkinID = Convert.ToUInt64(args.Args[3])
                });
            }

            DrawUI_ShoppingBag(player, 0);
        }

        [ConsoleCommand("shop.remcart")]
        // ReSharper disable once UnusedMember.Local
        private void RemCart(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!Carts.ContainsKey(player.userID)) Carts.Add(player.userID, new Cart { SteamID = player.userID });
            var list = Carts[player.userID].Objects;
            if (!list.ContainsKey(args.Args[0])) return;

            var item = list[args.Args[0]];
            var page = args.Args[4].ToInt();
            if (item.Count <= 1)
            {
                list.Remove(item.Shortname);
                if (!list.Skip(6 * page).Take(6).Any() & page > 0)
                {
                    page--;
                    DrawUI_ShoppingBag(player, page);
                    return;
                }
            }

            item.Count--;
            DrawUI_ShoppingBag(player, page);
        }

        [ConsoleCommand("shop.delcart")]
        // ReSharper disable once UnusedMember.Local
        private void DelCart(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!Carts.ContainsKey(player.userID)) Carts.Add(player.userID, new Cart { SteamID = player.userID });
            var list = Carts[player.userID].Objects;
            if (!list.ContainsKey(args.Args[0])) return;
            var item = list[args.Args[0]];
            list.Remove(item.Shortname);
            DrawUI_ShoppingBag(player, 0);
        }

        [ConsoleCommand("shop.cart")]
        // ReSharper disable once UnusedMember.Local
        private void CartPagination(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            var page = args.Args[0].ToInt();
            if (page < 0) return;
            if (!Carts.ContainsKey(player.userID)) return;
            var cart = Carts[player.userID].Objects;
            if (Convert.ToInt32(cart.Count / 6) < page) return;
            DrawUI_ShoppingBag(player, page);
            DrawUI_ShoppingPagination(player, page);
        }

        [ConsoleCommand("shop.buy")]
        // ReSharper disable once UnusedMember.Local
        private void CartBuy(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            BuyCart(player);
        }

        #endregion

        #region [DataBase] / [Хранение данных]

        public class StoredData
        {
            // ReSharper disable once UnusedMember.Local
            public Dictionary<ulong, PlayerShopInfo> ListPlayers = new Dictionary<ulong, PlayerShopInfo>();
            public Dictionary<ulong, List<ItemBlock>> BlockedItems = new Dictionary<ulong, List<ItemBlock>>();

            public class ItemBlock
            {
                public string ShortName;
                public int Limit;
                public int Count;
            }
        }

        public class PlayerShopInfo
        {
            public string Name;
            public ulong SteamID;
            public List<ItemShop> Log = new List<ItemShop>();
        }

        // ReSharper disable once UnusedMember.Local
        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _dataBase);

        // ReSharper disable once UnusedMember.Local
        private void LoadData()
        {
            try
            {
                _dataBase = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch (Exception)
            {
                _dataBase = new StoredData();
            }
        }

        #endregion

        #region [Helpers] / [Вспомогательный код]

        private ItemShop GetItem(string shortname)
        {
            var list = new List<ItemShop>();
            list.AddRange(_config.Shop.Ammunations);
            list.AddRange(_config.Shop.Attire);
            list.AddRange(_config.Shop.Components);
            list.AddRange(_config.Shop.Construction);
            list.AddRange(_config.Shop.Electric);
            list.AddRange(_config.Shop.Food);
            list.AddRange(_config.Shop.Fun);
            list.AddRange(_config.Shop.Gun);
            list.AddRange(_config.Shop.Items);
            list.AddRange(_config.Shop.Medicals);
            list.AddRange(_config.Shop.Misc);
            list.AddRange(_config.Shop.Resources);
            list.AddRange(_config.Shop.Tools);
            list.AddRange(_config.Shop.Traps);
            var item = list.Find(x => x.Shortname.ToLower() == shortname.ToLower());
            return item;
        }

        private static void ClearCart(BasePlayer player)
        {
            for (var i = 0; i <= 6; i++) CuiHelper.DestroyUi(player, $"{UIParent}.Cart.Element.{i}");
        }

        private List<ItemShop> GetFindItem(string shortname, string category)
        {
            var categorylist = GetCategory(category);
            return categorylist.Where(x => x.Name.ToLower().Contains(shortname.ToLower())).ToList();
        }

        private string GetImage(string name)
        {
            return (string)ImageLibrary?.Call("GetImage", name);
        }

        private void BuyCart(BasePlayer player)
        {
            CheckDataBase(player);
            var balance = GetBalance(player.userID);
            var cart = Carts[player.userID].Objects;
            var cost = cart.Sum(item => item.Value.Price * item.Value.Count);

            if (balance < cost)
            {
                player.ChatMessage("Недостаточно средств");
            }
            else
            {
                if ((24 - player.inventory.containerMain.itemList.Count) < cart.Count)
                {
                    player.ChatMessage("Недостаточно места в инвентаре");
                    return;
                }

                foreach (var giveItem in cart.Select(item => ItemManager.CreateByName(item.Value.Shortname,
                             (item.Value.FixCount * item.Value.Count), item.Value.SkinID)))
                {
                    player.GiveItem(giveItem);
                    player.ChatMessage(giveItem.skin.ToString());
                }

                TakeBalance(player.userID, cost);
                Carts.Remove(player.userID);
                DrawUI_ShoppingBag(player, 0);
            }
        }

        private void CheckDataBase(BasePlayer player)
        {
            if (!_dataBase.ListPlayers.ContainsKey(player.userID)) AddPlayer(player);
        }

        private void AddPlayer(BasePlayer player)
        {
            _dataBase.ListPlayers.Add(player.userID, new PlayerShopInfo
            {
                Name = player.displayName,
                SteamID = player.userID,
                Log = new List<ItemShop>()
            });
            SaveData();
        }

        private static List<ItemShop> GetCategory(string name)
        {
            var category = new List<ItemShop>();
            switch (name)
            {
                case "Weapons":
                    category = _config.Shop.Gun;
                    break;
                case "Constructions":
                    category = _config.Shop.Construction;
                    break;
                case "Items":
                    category = _config.Shop.Items;
                    break;
                case "Resources":
                    category = _config.Shop.Resources;
                    break;
                case "Attire":
                    category = _config.Shop.Attire;
                    break;
                case "Tools":
                    category = _config.Shop.Tools;
                    break;
                case "Medical":
                    category = _config.Shop.Medicals;
                    break;
                case "Food":
                    category = _config.Shop.Food;
                    break;
                case "Ammunations":
                    category = _config.Shop.Ammunations;
                    break;
                case "Traps":
                    category = _config.Shop.Traps;
                    break;
                case "Misk":
                    category = _config.Shop.Misc;
                    break;
                case "Components":
                    category = _config.Shop.Components;
                    break;
                case "Electrical":
                    category = _config.Shop.Electric;
                    break;
                case "Fun":
                    category = _config.Shop.Fun;
                    break;
            }

            return category;
        }

        private IEnumerator LoadImages()
        {
            foreach (var image in Images)
            {
                ImageLibrary?.Call("AddImage", image.Value, image.Key);
                yield return Wait;
            }

            foreach (var item in _config.Shop.Gun)
            {
                if (item.SkinID != 0)
                {
                    ImageLibrary?.CallHook("AddImage", $"https://rustlabs.com/img/skins/324/{item.SkinID}",
                        $"{item.Shortname}_{128}_{item.SkinID}");
                }
                else
                {
                    ImageLibrary?.CallHook("AddImage",
                        $"https://rustlabs.com/img/items180/{item.Shortname}.png",
                        $"{item.Shortname}_{128}_{0}");
                }

                yield return Wait;
            }

            foreach (var item in _config.Shop.Resources)
            {
                if (item.SkinID != 0)
                {
                    ImageLibrary?.CallHook("AddImage", $"https://rustlabs.com/img/skins/324/{item.SkinID}",
                        $"{item.Shortname}_{128}_{item.SkinID}");
                }
                else
                {
                    ImageLibrary?.CallHook("AddImage",
                        $"https://rustlabs.com/img/items180/{item.Shortname}.png",
                        $"{item.Shortname}_{128}_{0}");
                }

                yield return Wait;
            }

            foreach (var item in _config.Shop.Construction)
            {
                if (item.SkinID != 0)
                {
                    ImageLibrary?.CallHook("AddImage", $"https://rustlabs.com/img/skins/324/{item.SkinID}",
                        $"{item.Shortname}_{128}_{item.SkinID}");
                }
                else
                {
                    ImageLibrary?.CallHook("AddImage", $"https://rustlabs.com/img/items180/{item.Shortname}.png",
                        $"{item.Shortname}_{128}_{0}");
                }

                yield return Wait;
            }

            foreach (var item in _config.Shop.Items)
            {
                if (item.SkinID != 0)
                {
                    ImageLibrary?.CallHook("AddImage", $"https://rustlabs.com/img/skins/324/{item.SkinID}",
                        $"{item.Shortname}_{128}_{item.SkinID}");
                }
                else
                {
                    ImageLibrary?.CallHook("AddImage",
                        $"https://rustlabs.com/img/items180/{item.Shortname}.png",
                        $"{item.Shortname}_{128}_{0}");
                }

                yield return Wait;
            }

            foreach (var item in _config.Shop.Attire)
            {
                if (item.SkinID != 0)
                {
                    ImageLibrary?.CallHook("AddImage", $"https://rustlabs.com/img/skins/324/{item.SkinID}",
                        $"{item.Shortname}_{128}_{item.SkinID}");
                }
                else
                {
                    ImageLibrary?.CallHook("AddImage",
                        $"https://rustlabs.com/img/items180/{item.Shortname}.png",
                        $"{item.Shortname}_{128}_{0}");
                }

                yield return Wait;
            }

            foreach (var item in _config.Shop.Tools)
            {
                if (item.SkinID != 0)
                {
                    ImageLibrary?.CallHook("AddImage", $"https://rustlabs.com/img/skins/324/{item.SkinID}",
                        $"{item.Shortname}_{128}_{item.SkinID}");
                }
                else
                {
                    ImageLibrary?.CallHook("AddImage",
                        $"https://rustlabs.com/img/items180/{item.Shortname}.png",
                        $"{item.Shortname}_{128}_{0}");
                }

                yield return Wait;
            }

            foreach (var item in _config.Shop.Medicals)
            {
                if (item.SkinID != 0)
                {
                    ImageLibrary?.CallHook("AddImage", $"https://rustlabs.com/img/skins/324/{item.SkinID}",
                        $"{item.Shortname}_{128}_{item.SkinID}");
                }
                else
                {
                    ImageLibrary?.CallHook("AddImage",
                        $"https://rustlabs.com/img/items180/{item.Shortname}.png",
                        $"{item.Shortname}_{128}_{0}");
                }

                yield return Wait;
            }

            foreach (var item in _config.Shop.Food)
            {
                if (item.SkinID != 0)
                {
                    ImageLibrary?.CallHook("AddImage", $"https://rustlabs.com/img/skins/324/{item.SkinID}",
                        $"{item.Shortname}_{128}_{item.SkinID}");
                }
                else
                {
                    ImageLibrary?.CallHook("AddImage",
                        $"https://rustlabs.com/img/items180/{item.Shortname}.png",
                        $"{item.Shortname}_{128}_{0}");
                }

                yield return Wait;
            }

            foreach (var item in _config.Shop.Ammunations)
            {
                if (item.SkinID != 0)
                {
                    ImageLibrary?.CallHook("AddImage", $"https://rustlabs.com/img/skins/324/{item.SkinID}",
                        $"{item.Shortname}_{128}_{item.SkinID}");
                }
                else
                {
                    ImageLibrary?.CallHook("AddImage",
                        $"https://rustlabs.com/img/items180/{item.Shortname}.png",
                        $"{item.Shortname}_{128}_{0}");
                }

                yield return Wait;
            }

            foreach (var item in _config.Shop.Traps)
            {
                if (item.SkinID != 0)
                {
                    ImageLibrary?.CallHook("AddImage", $"https://rustlabs.com/img/skins/324/{item.SkinID}",
                        $"{item.Shortname}_{128}_{item.SkinID}");
                }
                else
                {
                    ImageLibrary?.CallHook("AddImage",
                        $"https://rustlabs.com/img/items180/{item.Shortname}.png",
                        $"{item.Shortname}_{128}_{0}");
                }

                yield return Wait;
            }

            foreach (var item in _config.Shop.Misc)
            {
                if (item.SkinID != 0)
                {
                    ImageLibrary?.CallHook("AddImage", $"https://rustlabs.com/img/skins/324/{item.SkinID}",
                        $"{item.Shortname}_{128}_{item.SkinID}");
                }
                else
                {
                    ImageLibrary?.CallHook("AddImage",
                        $"https://rustlabs.com/img/items180/{item.Shortname}.png",
                        $"{item.Shortname}_{128}_{0}");
                }

                yield return Wait;
            }

            foreach (var item in _config.Shop.Components)
            {
                if (item.SkinID != 0)
                {
                    ImageLibrary?.CallHook("AddImage", $"https://rustlabs.com/img/skins/324/{item.SkinID}",
                        $"{item.Shortname}_{128}_{item.SkinID}");
                }
                else
                {
                    ImageLibrary?.CallHook("AddImage",
                        $"https://rustlabs.com/img/items180/{item.Shortname}.png",
                        $"{item.Shortname}_{128}_{0}");
                }

                yield return Wait;
            }

            foreach (var item in _config.Shop.Fun)
            {
                if (item.SkinID != 0)
                {
                    ImageLibrary?.CallHook("AddImage", $"https://rustlabs.com/img/skins/324/{item.SkinID}",
                        $"{item.Shortname}_{128}_{item.SkinID}");
                }
                else
                {
                    ImageLibrary?.CallHook("AddImage",
                        $"https://rustlabs.com/img/items180/{item.Shortname}.png",
                        $"{item.Shortname}_{128}_{0}");
                }

                yield return Wait;
            }

            foreach (var item in _config.Shop.Electric)
            {
                if (item.SkinID != 0)
                {
                    ImageLibrary?.CallHook("AddImage", $"https://rustlabs.com/img/skins/324/{item.SkinID}",
                        $"{item.Shortname}_{128}_{item.SkinID}");
                }
                else
                {
                    ImageLibrary?.CallHook("AddImage",
                        $"https://rustlabs.com/img/items180/{item.Shortname}.png",
                        $"{item.Shortname}_{128}_{0}");
                }

                yield return Wait;
            }

            PrintWarning("Images loaded");
            yield return 0;
        }

        #endregion

        #region [API]

        private int GetBalance(ulong player)
        {
            return (int)BankSystem.CallHook("GetBalance", player);
        }

        private void GiveBalance(ulong player, int money)
        {
            BankSystem.CallHook("GiveBalance", player, money);
        }

        private void TakeBalance(ulong player, int money)
        {
            BankSystem.CallHook("TakeBalance", player, money);
        }

        #endregion
    }
}