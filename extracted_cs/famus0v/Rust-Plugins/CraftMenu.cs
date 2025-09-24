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
    [Info("CraftMenu", "Kira", "1.0.5")]
    [Description("Уникальное крафт меню")]
    public class CraftMenu : RustPlugin
    {
        #region [Vars] / [Переменные]

#pragma warning disable CS0649
        [PluginReference] private Plugin ImageLibrary;
#pragma warning restore CS0649
        private const string UIMain = "UI.CraftMenu";
        private const string Sharp = "assets/content/ui/ui.background.tile.psd";
        private const string Blur = "assets/content/ui/uibackgroundblur.mat";
        private StoredData _dataBase = new StoredData();

        #endregion

        #region [Dictionary] / [Словари]

        private readonly Dictionary<string, string> Images = new Dictionary<string, string>
        {
            ["UI.CraftMenu.ItemBG"] = "https://i.imgur.com/a69bBXI.png",
            ["UI.CraftMenu.ItemCraft"] = "https://i.imgur.com/dkNYUVb.png",
            ["UI.CraftMenu.CategoryBG"] = "https://i.imgur.com/9vAK9el.png",
            ["UI.CraftMenu.ListBG"] = "https://i.imgur.com/zqYn3hI.png",
            ["UI.CraftMenu.Info"] = "https://i.imgur.com/GoVsecx.png",
            ["UI.CraftMenu.Like"] = "https://i.imgur.com/Oab3OFj.png",
            ["UI.CraftMenu.RItem"] = "https://i.imgur.com/KkLRQCU.png",
            ["UI.CraftMenu.RItemLike"] = "https://i.imgur.com/quIFZig.png",
            ["UI.CraftMenu.Pagination"] = "https://i.imgur.com/Oxiix4R.png",
            ["UI.CraftMenu.Info.IconBG"] = "https://i.imgur.com/HsK5pmB.png",
            ["UI.CraftMenu.Achievement.Green"] = "https://i.imgur.com/2zaOxhc.png",
            ["UI.CraftMenu.Achievement.Orange"] = "https://i.imgur.com/iOQPAnn.png",
            ["UI.CraftMenu.Achievement.Red"] = "https://i.imgur.com/WTrWryb.png",
            ["UI.CraftMenu.Achievement.Info"] = "https://i.imgur.com/Rh1yuXQ.png",
            ["UI.CraftMenu.Achievement.Red"] = "https://i.imgur.com/WTrWryb.png"
        };

        #endregion

        #region [Lang]

        protected override void LoadDefaultMessages()
        {
            var ru = new Dictionary<string, string>
            {
                ["RIFLE.AK9182635908612359086712908659086129035861230964590128635"] =
                    "ФЫВФЫВФЫВФЫВФВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВ",
                ["MECHANISM"] = "МЕХАНИЗМЫ",
                ["ITEMS"] = "ПРЕДМЕТЫ",
                ["NOCRAFT"] = "КРАФТ НЕДОСТУПЕН",
                ["CRAFT"] = "КРАФТ ДОСТУПЕН",
                ["ACHIEVEMENTS_COMPLETED"] = "Вы выполнили задание, нажимет на уведомление, чтобы узнать подробнее",
                ["COMPRESSOR_15"] = "хуй хуй",
                ["КОМПРЕССОР4_4"] = "asdasdasdasdasdasd"
            };

            var en = new Dictionary<string, string>
            {
                ["RIFLE.AK"] = "ФЫВФЫВФЫВФЫВФВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВВ",
                ["MECHANISM"] = "МЕХАНИЗМЫ",
                ["ITEMS"] = "ПРЕДМЕТЫ",
                ["NOCRAFT"] = "КРАФТ НЕДОСТУПЕН",
                ["CRAFT"] = "КРАФТ ДОСТУПЕН",
                ["ACHIEVEMENTS_COMPLETED"] = "Вы выполнили задание, нажмите на уведомление, чтобы узнать подробнее",
                ["COMPRESSOR_15"] = "huy huy",
                ["КОМПРЕССОР4_4"] = "asdasdasdasdasdasd"
            };
            lang.RegisterMessages(ru, this, "ru");
            lang.RegisterMessages(en, this);
        }

        #endregion

        #region [Configuraton] / [Конфигурация]

        private ConfigData _config;

        public class ConfigData
        {
            [JsonProperty(PropertyName = "CraftMenu - Config")]
            public CraftMenuConfig CraftMenuSettings = new CraftMenuConfig();

            public class CraftMenuConfig
            {
                [JsonProperty(PropertyName = "Список достижений")]
                public List<Achievement> Achievements;

                [JsonProperty(PropertyName = "Список крафтов предметов")]
                public List<CraftItem> CraftItems = new List<CraftItem>();
            }
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                CraftMenuSettings = new ConfigData.CraftMenuConfig
                {
                    CraftItems = new List<CraftItem>
                    {
                        new CraftItem
                        {
                            NameRU = "Автомат",
                            Shortname = "rifle.ak",
                            ID = 1,
                            SkinID = 0,
                            IsLike = true,
                            Type = TypeObject.UniqItem,
                            RequireItems = new List<RequireItem>
                            {
                                new RequireItem
                                {
                                    RUName = "Дерево",
                                    Shortname = "stones",
                                    Count = 1000,
                                    IsLike = false,
                                    SkinID = 0
                                },
                                new RequireItem
                                {
                                    RUName = "Дерево",
                                    Shortname = "wood",
                                    Count = 1000,
                                    IsLike = false,
                                    SkinID = 0
                                },
                                new RequireItem
                                {
                                    RUName = "Дерево",
                                    Shortname = "wood",
                                    Count = 1000,
                                    IsLike = false,
                                    SkinID = 0
                                },
                                new RequireItem
                                {
                                    RUName = "Дерево",
                                    Shortname = "wood",
                                    Count = 1000,
                                    IsLike = false,
                                    SkinID = 0
                                }
                            },
                            ProductionMechanism = new List<RequireItem>
                            {
                                new RequireItem
                                {
                                    Shortname = "box.wooden.large",
                                    RUName = "Компрессор",
                                    Count = 1,
                                    ID = 2,
                                    IsLike = true,
                                    SkinID = 1,
                                    TypeObject = TypeObject.Factory
                                }
                            },
                            Input = new List<RequireItem>
                            {
                                new RequireItem
                                {
                                    ENName = "1",
                                    RUName = "2",
                                    Count = 3,
                                    ID = 4,
                                    IsLike = true,
                                    Shortname = "dasdasda",
                                    SkinID = 123123123,
                                    TypeObject = TypeObject.Factory
                                }
                            }
                        },
                        new CraftItem
                        {
                            NameRU = "Компрессор",
                            Shortname = "rifle.ak",
                            ID = 2,
                            SkinID = 0,
                            IsLike = true,
                            Type = TypeObject.Factory,
                            RequireItems = new List<RequireItem>
                            {
                                new RequireItem
                                {
                                    RUName = "Дерево",
                                    Shortname = "stones",
                                    Count = 1000,
                                    IsLike = false,
                                    SkinID = 0
                                },
                                new RequireItem
                                {
                                    RUName = "Дерево",
                                    Shortname = "wood",
                                    Count = 1000,
                                    IsLike = false,
                                    SkinID = 0
                                },
                                new RequireItem
                                {
                                    RUName = "Дерево",
                                    Shortname = "wood",
                                    Count = 1000,
                                    IsLike = true,
                                    SkinID = 0
                                },
                                new RequireItem
                                {
                                    RUName = "Дерево",
                                    Shortname = "wood",
                                    Count = 1000,
                                    IsLike = true,
                                    SkinID = 0
                                }
                            },
                            Input = new List<RequireItem>
                            {
                                new RequireItem
                                {
                                    Shortname = "stones",
                                    RUName = "Камень",
                                    Count = 1,
                                    ID = 2,
                                    SkinID = 0,
                                    IsLike = false
                                }
                            },
                            Output = new List<RequireItem>
                            {
                                new RequireItem
                                {
                                    Shortname = "stones",
                                    RUName = "Сжатый камень",
                                    Count = 1,
                                    ID = 3,
                                    SkinID = 1,
                                    IsLike = false
                                }
                            }
                        }
                    },
                    Achievements = new List<Achievement>
                    {
                        new Achievement
                        {
                            RUName = "КОМПРЕССОР",
                            ENName = "COMPRESSOR",
                            ID = 15,
                            RequireItems = new List<RequireItem>
                            {
                                new RequireItem
                                {
                                    Shortname = "wood",
                                    Count = 1,
                                    TypeObject = TypeObject.Item
                                }
                            }
                        },
                        new Achievement
                        {
                            RUName = "КОМПРЕССОР2",
                            ID = 2,
                            RequireItems = new List<RequireItem>
                            {
                                new RequireItem
                                {
                                    Shortname = "wood",
                                    Count = 2,
                                    TypeObject = TypeObject.Item
                                }
                            }
                        },
                        new Achievement
                        {
                            RUName = "КОМПРЕССОР3",
                            ID = 3,
                            RequireItems = new List<RequireItem>
                            {
                                new RequireItem
                                {
                                    Shortname = "wood",
                                    Count = 3,
                                    TypeObject = TypeObject.Item
                                }
                            }
                        },
                        new Achievement
                        {
                            RUName = "КОМПРЕССОР4",
                            ID = 4,
                            RequireItems = new List<RequireItem>
                            {
                                new RequireItem
                                {
                                    Shortname = "wood",
                                    Count = 4,
                                    TypeObject = TypeObject.Item
                                }
                            }
                        },
                        new Achievement
                        {
                            RUName = "КОМПРЕССОР5",
                            ID = 5,
                            RequireItems = new List<RequireItem>
                            {
                                new RequireItem
                                {
                                    Shortname = "wood",
                                    Count = 5,
                                    TypeObject = TypeObject.Item
                                }
                            }
                        },
                        new Achievement
                        {
                            RUName = "КОМПРЕССОР6",
                            ID = 6,
                            RequireItems = new List<RequireItem>
                            {
                                new RequireItem
                                {
                                    Shortname = "wood",
                                    Count = 6,
                                    TypeObject = TypeObject.Item
                                }
                            }
                        },
                        new Achievement
                        {
                            RUName = "КОМПРЕССОР7",
                            ID = 7,
                            RequireItems = new List<RequireItem>
                            {
                                new RequireItem
                                {
                                    Shortname = "wood",
                                    Count = 7,
                                    TypeObject = TypeObject.Item
                                }
                            }
                        },
                        new Achievement
                        {
                            RUName = "КОМПРЕССОР8",
                            ID = 8,
                            RequireItems = new List<RequireItem>
                            {
                                new RequireItem
                                {
                                    Shortname = "wood",
                                    Count = 8,
                                    TypeObject = TypeObject.Item
                                }
                            }
                        },
                        new Achievement
                        {
                            RUName = "КОМПРЕССОР9",
                            ID = 9,
                            RequireItems = new List<RequireItem>
                            {
                                new RequireItem
                                {
                                    Shortname = "wood",
                                    Count = 1000,
                                    TypeObject = TypeObject.Item
                                }
                            }
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

        #region [Classes] / [Классы]

        public enum TypeObject
        {
            Factory = 0,
            Item = 1,
            Ingredient = 2,
            UniqItem = 3
        }

        public enum AchievementType
        {
            Hold = 0
        }

        public class Achievement
        {
            [JsonProperty(PropertyName = "Название ENG")]
            public string ENName;

            [JsonProperty(PropertyName = "Название RU")]
            public string RUName;

            [JsonProperty(PropertyName = "ID достижения")]
            public int ID;

            [JsonProperty(PropertyName = "Не трогать блять")]
            public bool Completed;

            [JsonProperty(PropertyName = "Предмет крутой (true/false)")]
            public bool IsLike;

            public AchievementType Type;
            public List<RequireItem> RequireItems = new List<RequireItem>();
            public List<RequireItem> CollectedItems = new List<RequireItem>();
        }

        private class AchievementDB
        {
            public string Name;
            public int ID;
            public bool IsLike;
            public bool Completed;
            public List<RequireItem> CollectedItems = new List<RequireItem>();
        }

        public class CraftItem
        {
            [JsonProperty(PropertyName = "Название ENG")]
            public string NameENG = "Введите ENG название";

            [JsonProperty(PropertyName = "Название RU")]
            public string NameRU = "Введите RU название";

            [JsonProperty(PropertyName = "ID предмета")]
            public int ID;

            [JsonProperty(PropertyName = "Ключ перевода описания ")]
            public string LANG_KEY = "Введите ключ";

            [JsonProperty(PropertyName = "Shortname")]
            public string Shortname;

            [JsonProperty(PropertyName = "SkinID")]
            public ulong SkinID;

            [JsonProperty(PropertyName = "Количество")]
            public int Count;

            [JsonProperty(PropertyName = "Предмет крутой (true/false)")]
            public bool IsLike;

            [JsonProperty(PropertyName =
                "Тип объекта (0 - Завод, 1 - Предмет, 2 - Ингредиент, 3 - Уникальный предмет)")]
            public TypeObject Type;

            [JsonProperty(PropertyName = "Механизм производства")]
            public List<RequireItem> ProductionMechanism = new List<RequireItem>();

            [JsonProperty(PropertyName = "Входящие предметы")]
            public List<RequireItem> Input = new List<RequireItem>();

            [JsonProperty(PropertyName = "Выходящие")]
            public List<RequireItem> Output = new List<RequireItem>();

            [JsonProperty(PropertyName = "Список требуемых предметов для крафта")]
            public List<RequireItem> RequireItems = new List<RequireItem>();
        }

        public class RequireItem
        {
            [JsonProperty(PropertyName = "Название RU")]
            public string RUName = "Введите RU название";

            [JsonProperty(PropertyName = "Название EN")]
            public string ENName = "Введите ENG название"; 

            [JsonProperty(PropertyName = "ID")] public int ID;

            [JsonProperty(PropertyName =
                "Тип объекта (0 - Завод, 1 - Предмет, 2 - Ингредиент, 3 - Уникальный предмет)")]
            public TypeObject TypeObject;

            [JsonProperty(PropertyName = "Shortname предмета")]
            public string Shortname;

            [JsonProperty(PropertyName = "SkinID предмета")]
            public ulong SkinID;

            [JsonProperty(PropertyName = "Количество")]
            public int Count;

            [JsonProperty(PropertyName = "Предмет крутой (true/false)")]
            public bool IsLike;
        }

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
                            Color = "0 0 0 0"
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
                    Parent = UIMain,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0.13 0.13 0.15 1.00"
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
                    Color = "0 0 0 0",
                    Close = UIMain
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
            }, UIMain);

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.ItemBG",
                Parent = UIMain,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage($"{UIMain}.ItemBG", 0)
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.3674439 0.2643448",
                        AnchorMax = "0.6325561 0.7356552"
                    }
                }
            });

            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.AddUi(player, ui);
            DrawUI_CraftItem(player, _config.CraftMenuSettings.CraftItems[0]);
            DrawUI_Pagination(player, TypeObject.Factory, 0);
            DrawUI_Achievements(player, 0);
        }

        private void DrawUI_CraftItem(BasePlayer player, CraftItem item)
        {
            var ui = new CuiElementContainer();
            var IsCraft = GetAvalibleItems(player, item);
            if (!_dataBase.Achievements[player.userID][item.ID].Completed) IsCraft = false;

            #region [CurrentItem]

            ui.Add(new CuiElement 
            {
                Name = $"{UIMain}.ItemIcon",
                Parent = $"{UIMain}.ItemBG",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage(item.Shortname, item.SkinID),
                        Material = Sharp
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.156199 0.1561989",
                        AnchorMax = "0.843801 0.8438011"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.ItemName",
                Parent = $"{UIMain}.ItemBG",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 19,
                        Color = "1 1 1 0.6",
                        Text = lang.GetLanguage(player.UserIDString) == "ru" ? item.NameRU : item.NameENG
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 0.16"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.CraftButton",
                Parent = $"{UIMain}.ItemBG",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage($"{UIMain}.ItemCraft", 0),
                        Color = IsCraft ? "0.26 0.79 0.09 0.6" : "0.79 0.09 0.09 0.6",
                        Material = Sharp
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 -0.16306",
                        AnchorMax = "1 -0.03861123"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.ItemInfo",
                Parent = $"{UIMain}.ItemBG",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage($"{UIMain}.Info", 0),
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.8614822 0.8536241",
                        AnchorMax = "0.9754277 0.9675696"
                    }
                }
            });

            ui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"craft.info {item.ID}",
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
            }, $"{UIMain}.ItemInfo");

            if (item.IsLike)
            {
                ui.Add(new CuiElement
                {
                    Name = $"{UIMain}.ItemLike",
                    Parent = $"{UIMain}.ItemBG",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage($"{UIMain}.Like", 0),
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.8614822 0.7239625",
                            AnchorMax = "0.9754277 0.837908"
                        }
                    }
                });
            }

            ui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"craft {item.ID}",
                    Color = "0 0 0 0"
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 19,
                    Color = "1 1 1 0.6",
                    Text = IsCraft
                        ? lang.GetMessage("CRAFT", this, player.UserIDString)
                        : lang.GetMessage("NOCRAFT", this, player.UserIDString)
                }
            }, $"{UIMain}.CraftButton");

            #endregion

            #region [RequireItems]

            int x = 0, y = 0, num = 0;
            foreach (var ritem in item.RequireItems.Take(4))
            {
                var parent = $"{UIMain}.RItem.{num}";
                CuiHelper.DestroyUi(player, parent);
                if (x >= 2)
                {
                    x = 0;
                    y++;
                }

                ui.Add(new CuiElement
                {
                    Name = parent,
                    Parent = UIMain,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage(ritem.IsLike ? $"{UIMain}.RItemLike" : $"{UIMain}.RItem", 0),
                            Material = Sharp,
                            FadeIn = 0.2f * num
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.722625 + (x * 0.1296719)} {0.2900185 - (y * 0.2296719)}",
                            AnchorMax = $"{0.842368 + (x * 0.1296719)} {0.5028954 - (y * 0.2296719)}"
                        }
                    }
                });

                if (ritem.TypeObject != TypeObject.Item)
                {
                    ui.Add(new CuiElement
                    {
                        Name = $"{UIMain}.RInfo",
                        Parent = parent,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = GetImage($"{UIMain}.Info", 0),
                                FadeIn = 0.2f * num
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.8244997 0.8124172",
                                AnchorMax = "0.9549874 0.9429047"
                            }
                        }
                    });

                    ui.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = $"craft.infoitem {ritem.ID}",
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
                    }, $"{UIMain}.RInfo");
                }

                ui.Add(new CuiElement
                {
                    Name = $"{UIMain}.RItemIcon",
                    Parent = parent,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage(ritem.Shortname, ritem.SkinID),
                            Material = Sharp,
                            FadeIn = 0.2f * num
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.1855128 0.1855128",
                            AnchorMax = "0.8144872 0.8144872"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UIMain}.RItemName",
                    Parent = parent,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 15,
                            Color = "1 1 1 0.2",
                            Text = lang.GetLanguage(player.UserIDString) == "en" ? ritem.ENName : ritem.RUName,
                            FadeIn = 0.2f * num
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 0.1991241"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UIMain}.RItemCount",
                    Parent = parent,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 15,
                            Color = IsCraft ? "0.34 0.34 0.34 1.00" : "0.79 0.09 0.09 0.3",
                            Text = $"X{ritem.Count}",
                            FadeIn = 0.2f * num
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.7993668",
                            AnchorMax = "0.4112968 1"
                        }
                    }
                });

                x++;
                num++;
            }

            #endregion

            DestroyCraftItem(player);
            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_Pagination(BasePlayer player, TypeObject type, int page)
        {
            var ui = new CuiElementContainer
            {
                new CuiElement
                {
                    Name = $"{UIMain}.Category.Mechanism",
                    Parent = UIMain,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage($"{UIMain}.CategoryBG", 0)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.02711459 0.4453148",
                            AnchorMax = "0.1476195 0.5039687"
                        }
                    }
                },
                new CuiElement
                {
                    Parent = $"{UIMain}.Category.Mechanism",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 15,
                            Color = "1 1 1 0.6",
                            Text = lang.GetMessage("MECHANISM", this, player.UserIDString)
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
                    Command = $"craft.category 0",
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
            }, $"{UIMain}.Category.Mechanism");

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Category.Items",
                Parent = UIMain,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage($"{UIMain}.CategoryBG", 0)
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1567708 0.4453148",
                        AnchorMax = "0.2772757 0.5039687"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Parent = $"{UIMain}.Category.Items",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 15,
                        Color = "1 1 1 0.6",
                        Text = lang.GetMessage("ITEMS", this, player.UserIDString)
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
                    Command = "craft.category 3",
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
            }, $"{UIMain}.Category.Items");

            var y = 0;
            foreach (var craftItem in _config.CraftMenuSettings.CraftItems.Skip(page * 4).Take(4))
            {
                if (craftItem.Type != type) continue;
                var parent = $"{UIMain}.CategoryElement.{y}";

                ui.Add(new CuiElement
                {
                    Name = parent,
                    Parent = UIMain,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage($"{UIMain}.ListBG", 0),
                            FadeIn = 0.2f * y
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0.0270833 {0.3676481 - (y * 0.0770061666666667)}",
                            AnchorMax = $"0.2765008 {0.4263021 - (y * 0.0770061666666667)}"
                        }
                    }
                });

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"craft.select {craftItem.ID}",
                        Color = "0 0 0 0"
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 16,
                        Color = "0.34 0.34 0.34 1.00",
                        Text = lang.GetLanguage(player.UserIDString) == "ru" ? craftItem.NameRU : craftItem.NameENG,
                        FadeIn = 0.2f * y
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, parent);
                y++;
            }

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Pagination",
                Parent = UIMain,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage($"{UIMain}.Pagination", 0)
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.07061979 0.0594537",
                        AnchorMax = "0.2345145 0.1181075"
                    }
                }
            });
            ui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"craft.page {type} {page - 1}",
                    Color = "0 0 0 0"
                },
                Text =
                {
                    Text = " "
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "0.1697292 1"
                }
            }, $"{UIMain}.Pagination", $"{UIMain}.Pagination-");

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Pagination.Num",
                Parent = $"{UIMain}.Pagination",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 20,
                        Color = "0.34 0.34 0.34 1.00",
                        Text = $"{page + 1}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1697292 0",
                        AnchorMax = "0.843434 1"
                    }
                }
            });

            ui.Add(new CuiButton
            {
                Button =
                {
                    Command = $"craft.page {type} {page + 1}",
                    Color = "0 0 0 0"
                },
                Text =
                {
                    Text = " "
                },
                RectTransform =
                {
                    AnchorMin = "0.843434 0",
                    AnchorMax = "1 1"
                }
            }, $"{UIMain}.Pagination", $"{UIMain}.Pagination+");

            DestroyPagination(player);
            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_InfoPanel(BasePlayer player, TypeObject type, CraftItem item)
        {
            var ui = new CuiElementContainer();
            var IsCraft = GetAvalibleItems(player, item);
            if (item.Type != TypeObject.Item)
            {
                var info = _config.CraftMenuSettings.CraftItems.Find(a => a.ID == item.ID);
                if (info != null)
                {
                    item = info;
                }
            }

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.InfoPanel",
                Parent = UIMain,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0.13 0.13 0.15 1"
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
                    Close = $"{UIMain}.InfoPanel",
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
            }, $"{UIMain}.InfoPanel");

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.ItemIconBG",
                Parent = $"{UIMain}.InfoPanel",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage("UI.CraftMenu.Info.IconBG", 0),
                        Material = Sharp
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.028125 0.4814815",
                        AnchorMax = "0.29375 0.9537036"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.ItemIcon",
                Parent = $"{UIMain}.ItemIconBG",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage(item.Shortname, item.SkinID),
                        Material = Sharp
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.156199 0.1561989",
                        AnchorMax = "0.843801 0.8438011"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.ItemName",
                Parent = $"{UIMain}.ItemIconBG",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 19,
                        Color = "1 1 1 0.6",
                        Text = lang.GetLanguage(player.UserIDString) == "ru" ? item.NameRU : item.NameENG
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 0.16"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.ItemDescription",
                Parent = $"{UIMain}.InfoPanel",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.UpperLeft,
                        FontSize = 19,
                        Color = "1 1 1 0.6",
                        Text = lang.GetMessage(item.LANG_KEY, this, player.UserIDString)
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.3015625 0.4814815",
                        AnchorMax = "0.9276042 0.9537036"
                    }
                }
            });

            string txt1;
            string txt2;

            switch (type)
            {
                case TypeObject.Factory:
                    txt1 = lang.GetLanguage(player.UserIDString) == "en"
                        ? "INGREDIENTS FOR BUILDING :"
                        : "ИНГРЕДИЕНТЫ ДЛЯ ПОСТРОЙКИ :";
                    txt2 = lang.GetLanguage(player.UserIDString) == "en" ? "PRODUCES :" : "ПРОИЗВОДИТ :";
                    break;
                case TypeObject.UniqItem:
                    txt1 = lang.GetLanguage(player.UserIDString) == "en"
                        ? "PRODUCTION MECHANISM :"
                        : "МЕХАНИЗМ ПРОИЗВОДСТВА :";
                    txt2 = lang.GetLanguage(player.UserIDString) == "en" ? "INGEDIENTS :" : "ИНГРЕДИЕНТЫ :";
                    break;
                case TypeObject.Ingredient:
                    txt1 = lang.GetLanguage(player.UserIDString) == "en"
                        ? "PRODUCTION MECHANISM :"
                        : "МЕХАНИЗМ ПРОИЗВОДСТВА :";
                    txt2 = lang.GetLanguage(player.UserIDString) == "en" ? "INGEDIENTS :" : "ИНГРЕДИЕНТЫ :";
                    break;
                case TypeObject.Item:
                    txt1 = lang.GetLanguage(player.UserIDString) == "en"
                        ? "PRODUCTION MECHANISM :"
                        : "МЕХАНИЗМ ПРОИЗВОДСТВА :";
                    txt2 = lang.GetLanguage(player.UserIDString) == "en" ? "INGEDIENTS :" : "ИНГРЕДИЕНТЫ :";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "ERROR : 903");
            }

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.RequireCraft",
                Parent = $"{UIMain}.InfoPanel",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 19,
                        Color = "1 1 1 0.6",
                        Text = txt1.ToUpper()
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.028125 0.4055555",
                        AnchorMax = "0.29375 0.4740741"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Production",
                Parent = $"{UIMain}.InfoPanel",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 19,
                        Color = "1 1 1 0.6",
                        Text = txt2
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.028125 0.1925926",
                        AnchorMax = "0.29375 0.2611111"
                    }
                }
            });

            #region [RequireItems]

            List<RequireItem> list1;
            List<RequireItem> list2;
            switch (type)
            {
                case TypeObject.Factory:
                    list1 = item.RequireItems;
                    list2 = item.Output;
                    break;
                case TypeObject.UniqItem:
                    list1 = item.ProductionMechanism;
                    list2 = item.RequireItems;
                    break;
                case TypeObject.Ingredient:
                    list1 = item.ProductionMechanism;
                    list2 = item.RequireItems;
                    break;
                case TypeObject.Item:
                    list1 = item.ProductionMechanism;
                    list2 = item.RequireItems;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "ERROR : 968");
            }

            int x = 0, x1 = 0, num1 = 1, num = 0;
            foreach (var ritem in list1.Take(8))
            {
                var parent = $"{UIMain}.InfoPanel.RItem1.{num}";
                CuiHelper.DestroyUi(player, parent);

                ui.Add(new CuiElement
                {
                    Name = parent,
                    Parent = $"{UIMain}.InfoPanel",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage(ritem.IsLike ? $"{UIMain}.RItemLike" : $"{UIMain}.RItem", 0),
                            Material = Sharp,
                            FadeIn = 0.2f * num
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.027083 + (x * 0.082)} {0.2617222}",
                            AnchorMax = $"{0.103125 + (x * 0.082)} {0.3969074}"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UIMain}.RItemIcon",
                    Parent = parent,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage(ritem.Shortname, ritem.SkinID),
                            Material = Sharp,
                            FadeIn = 0.2f * num
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.1855128 0.1855128",
                            AnchorMax = "0.8144872 0.8144872"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UIMain}.RItemName",
                    Parent = parent,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 13,
                            Color = "1 1 1 0.2",
                            Text = lang.GetLanguage(player.UserIDString) == "en" ? ritem.ENName : ritem.RUName,
                            FadeIn = 0.2f * num
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 0.1991241"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UIMain}.RItemCount",
                    Parent = parent,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 12,
                            Color = IsCraft ? "0.34 0.34 0.34 1.00" : "0.79 0.09 0.09 0.3",
                            Text = $"X{ritem.Count}",
                            FadeIn = 0.2f * num
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.7993668",
                            AnchorMax = "0.4112968 1"
                        }
                    }
                });

                if (ritem.TypeObject != TypeObject.Item)
                {
                    ui.Add(new CuiElement
                    {
                        Name = $"{UIMain}.Info1",
                        Parent = parent,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = GetImage($"{UIMain}.Info", 0),
                                Material = Sharp,
                                FadeIn = 0.2f
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.7465746 0.7328759",
                                AnchorMax = "0.9520532 0.9383546"
                            }
                        }
                    });

                    ui.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = $"craft.infoitem {ritem.ID}",
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
                    }, $"{UIMain}.Info1");
                }

                x++;
                num++;
            }

            foreach (var ritem in list2.Take(8))
            {
                var parent = $"{UIMain}.InfoPanel.RItem2.{num1}";
                CuiHelper.DestroyUi(player, parent);

                ui.Add(new CuiElement
                {
                    Name = parent,
                    Parent = $"{UIMain}.InfoPanel",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage(ritem.IsLike ? $"{UIMain}.RItemLike" : $"{UIMain}.RItem", 0),
                            Material = Sharp,
                            FadeIn = 0.2f * num1
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.027083 + (x1 * 0.082)} {0.0572037}",
                            AnchorMax = $"{0.103125 + (x1 * 0.082)} {0.1923889}"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UIMain}.RItemIcon",
                    Parent = parent,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage(ritem.Shortname, ritem.SkinID),
                            Material = Sharp,
                            FadeIn = 0.2f * num1
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.1855128 0.1855128",
                            AnchorMax = "0.8144872 0.8144872"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UIMain}.RItemName",
                    Parent = parent,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 13,
                            Color = "1 1 1 0.2",
                            Text = lang.GetLanguage(player.UserIDString) == "en" ? ritem.ENName : ritem.RUName,
                            FadeIn = 0.2f * num1
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 0.1991241"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Name = $"{UIMain}.RItemCount",
                    Parent = parent,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 12,
                            Color = IsCraft ? "0.34 0.34 0.34 1.00" : "0.79 0.09 0.09 0.3",
                            Text = $"X{ritem.Count}",
                            FadeIn = 0.2f * num
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.7993668",
                            AnchorMax = "0.4112968 1"
                        }
                    }
                });

                if (ritem.TypeObject != TypeObject.Item)
                {
                    ui.Add(new CuiElement
                    {
                        Name = $"{UIMain}.Info2",
                        Parent = parent,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = GetImage($"{UIMain}.Info", 0),
                                Material = Sharp,
                                FadeIn = 0.2f
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.7465746 0.7328759",
                                AnchorMax = "0.9520532 0.9383546"
                            }
                        }
                    });

                    ui.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = $"craft.infoitem {ritem.ID}",
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
                    }, $"{UIMain}.Info2");
                }

                x1++;
                num1++;
            }

            #endregion

            CuiHelper.DestroyUi(player, $"{UIMain}.InfoPanel");
            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_Achievements(BasePlayer player, int page)
        {
            var ui = new CuiElementContainer();

            var db = _dataBase.Achievements[player.userID];
            var pos = 0.515f - (db.Skip(8 * page).Take(8).Count() * 0.105f +
                                (db.Skip(8 * page).Take(8).Count() - 1) * 0.005f) / 2;

            var num = 1;
            for (var i = 1; i <= 8; i++)
            {
                CuiHelper.DestroyUi(player, $"{UIMain}.Achievement.{i}");
                CuiHelper.DestroyUi(player, $"{UIMain}.Achievement.{i}.Arrow.{i}");
            }


            foreach (var achievement in db.Skip(8 * page).Take(8))
            {
                var item = _config.CraftMenuSettings.CraftItems.Find(d => d.ID == achievement.ID) ??
                           _config.CraftMenuSettings.CraftItems[0];
                var parent = $"{UIMain}.Achievement.{num}";
                var type = "Red";
                switch (achievement.Completed)
                {
                    case true:
                        type = "Green";
                        break;
                    case false:
                        type = "Red";
                        break;
                }

                if (achievement.IsLike) type = "Orange";

                ui.Add(new CuiElement
                {
                    Name = parent,
                    Parent = UIMain,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage($"{UIMain}.Achievement.{type}", 0),
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{pos} 0.8624895",
                            AnchorMax = $"{pos + 0.07} 0.9782283"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Parent = parent,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage(item.Shortname, item.SkinID),
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.15 0.15",
                            AnchorMax = "0.85 0.85"
                        }
                    }
                });

                if (num <= db.Skip(8 * page).Take(8).Count() - 1)
                {
                    ui.Add(new CuiElement
                    {
                        Name = $"{parent}.Arrow.{num}",
                        Parent = UIMain,
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Align = TextAnchor.MiddleCenter,
                                FontSize = 18,
                                Color = "0.34 0.34 0.35 1.00",
                                Text = "➜"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{pos + 0.08} 0.8624895",
                                AnchorMax = $"{pos + 0.1} 0.9782283"
                            }
                        }
                    });
                }

                ui.Add(new CuiElement
                {
                    Parent = parent,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 11,
                            Color = "1 1 1 0.3",
                            Text = achievement.Name
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 0.3"
                        }
                    }
                });

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Color = "0 0 0 0",
                        Command = $"craft.achievementsinfo {achievement.ID}"
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
                }, parent);

                pos += 0.105f + 0.005f;
                num++;
            }

            CuiHelper.AddUi(player, ui);
            DrawUI_AchievementsPagination(player, page);
        }

        private void DrawUI_AchievementInfo(BasePlayer player, Achievement achievement)
        {
            var ui = new CuiElementContainer();
            var center = _config.CraftMenuSettings.CraftItems.Find(q => q.ID == achievement.ID);
            var parent = $"{UIMain}.Achievements.Info";
            ui.Add(new CuiElement
            {
                Name = parent,
                Parent = UIMain,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0.13 0.13 0.15 1"
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
                    Close = parent,
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
            }, parent);

            ui.Add(new CuiElement
            {
                Name = $"{parent}.Background.Icon",
                Parent = parent,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage($"{UIMain}.Achievement.Info", 0)
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.3666666 0.469445",
                        AnchorMax = "0.6333333 0.9435124"
                    }
                }
            });

            if (center != null)
            {
                ui.Add(new CuiElement
                {
                    Parent = parent,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage(center.Shortname, center.SkinID)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.3958333 0.5277782",
                            AnchorMax = "0.6041666 0.8981411"
                        }
                    }
                });
            }

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.ItemInfo",
                Parent = $"{parent}.Background.Icon",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage($"{UIMain}.Info", 0),
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.8614822 0.8536241",
                        AnchorMax = "0.9754277 0.9675696"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Parent = $"{parent}.Background.Icon",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 18,
                        Color = "0.34 0.34 0.35 1.00",
                        Text = lang.GetLanguage(player.UserIDString) == "en"
                            ? achievement.ENName.ToUpper()
                            : achievement.RUName.ToUpper()
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"0 0",
                        AnchorMax = $"1 0.1230475"
                    }
                }
            });

            if (center != null)
            {
                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"craft.info {center.ID}",
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
                }, $"{parent}.Background.Icon");

                if (center.IsLike)
                {
                    ui.Add(new CuiElement
                    {
                        Name = $"{UIMain}.ItemLike",
                        Parent = $"{parent}.Background.Icon",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = GetImage($"{UIMain}.Like", 0),
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.8614822 0.7239625",
                                AnchorMax = "0.9754277 0.837908"
                            }
                        }
                    });
                }
            }

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.IsCraft",
                Parent = $"{parent}.Background.Icon",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage($"{UIMain}.ItemCraft", 0),
                        Color = achievement.Completed ? "0.26 0.79 0.09 0.6" : "0.79 0.09 0.09 0.6",
                        Material = Sharp
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 -0.16306",
                        AnchorMax = "1 -0.03861123"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Parent = $"{UIMain}.IsCraft",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 18,
                        Color = "1 1 1 0.5",
                        Text = achievement.Completed
                            ? lang.GetMessage("CRAFT", this, player.UserIDString)
                            : lang.GetMessage("NOCRAFT", this, player.UserIDString)
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Parent = parent,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 18,
                        Color = "1 1 1 0.5",
                        Text = lang.GetMessage($"{achievement.ENName}_{achievement.ID}", this, player.UserIDString)
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.2041667 0.07777679",
                        AnchorMax = "0.7958333 0.4407463"
                    }
                }
            });

            CuiHelper.DestroyUi(player, parent);
            CuiHelper.AddUi(player, ui);
            DrawUI_AchievementsInfoPagination(player, achievement.ID);
        }

        private void DrawUI_AchievementsPagination(BasePlayer player, int page)
        {
            var ui = new CuiElementContainer
            {
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"craft.achievementpage {page - 1}",
                            Color = "0 0 0 0"
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = "0.34 0.34 0.35 1.00",
                            FontSize = 50,
                            Text = "‹"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.007553816 0.8624895",
                            AnchorMax = "0.0726608 0.9782283"
                        }
                    },
                    UIMain, $"{UIMain}.APagination-"
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"craft.achievementpage {page + 1}",
                            Color = "0 0 0 0"
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = "0.34 0.34 0.35 1.00",
                            FontSize = 50,
                            Text = "›"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.9283784 0.8624895",
                            AnchorMax = "0.9934826 0.9782283"
                        }
                    },
                    UIMain, $"{UIMain}.APagination+"
                }
            };

            CuiHelper.DestroyUi(player, $"{UIMain}.APagination+");
            CuiHelper.DestroyUi(player, $"{UIMain}.APagination-");
            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_AchievementsInfoPagination(BasePlayer player, int page)
        {
            var parent = $"{UIMain}.Achievements.Info";
            var ui = new CuiElementContainer
            {
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"craft.achievementinfopage {page - 1}",
                            Color = "0 0 0 0"
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = "0.34 0.34 0.35 1.00",
                            FontSize = 100,
                            Text = "‹"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.2619806 0.6037037",
                            AnchorMax = "0.3661442 0.7888889"
                        }
                    },
                    parent, $"{parent}.APagination-"
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"craft.achievementinfopage {page + 1}",
                            Color = "0 0 0 0"
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = "0.34 0.34 0.35 1.00",
                            FontSize = 100,
                            Text = "›"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.6346327 0.6037037",
                            AnchorMax = "0.7236952 0.7888889"
                        }
                    },
                    parent, $"{parent}.APagination+"
                }
            };

            CuiHelper.DestroyUi(player, $"{parent}.APagination+");
            CuiHelper.DestroyUi(player, $"{parent}.APagination-");
            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_AchievementCompleted(BasePlayer player, int ID)
        {
            var ui = new CuiElementContainer();
            var parent = "UI_Notify";
            ui.Add(new CuiButton
            {
                Button =
                {
                    Color = "0.13 0.15 0.29 0.7",
                    Command = $"achievement.completed {ID}",
                    Close = parent,
                    Material = Blur,
                    FadeIn = 1f
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1",
                    FontSize = 14,
                    FadeIn = 1f,
                    Text = lang.GetMessage("ACHIEVEMENTS_COMPLETED", this, player.UserIDString)
                },
                RectTransform =
                {
                    AnchorMin = "0.009895824 0.5490741",
                    AnchorMax = "0.2166667 0.7240741"
                }
            }, "Overlay", parent);

            ui.Add(new CuiButton
            {
                Button =
                {
                    Color = "0.81 0.18 0.04 0.9",
                    Close = parent,
                    FadeIn = 1f
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.8",
                    Text = "✖",
                    FontSize = 18,
                    FadeIn = 1f
                },
                RectTransform =
                {
                    AnchorMin = "0.8992443 0.7883595",
                    AnchorMax = "0.995 0.996"
                }
            }, parent);

            CuiHelper.DestroyUi(player, parent);
            CuiHelper.AddUi(player, ui);
            timer.Once(5f, () => CuiHelper.DestroyUi(player, parent));
        }

        #endregion

        #region [Hooks] / [Хуки]

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            LoadData();
            ServerMgr.Instance.StartCoroutine(LoadImages());
            foreach (var player in BasePlayer.activePlayerList) CheckData(player);
        }

        // ReSharper disable once UnusedMember.Local
        private void Unload()
        {
            SaveData();
        }

        // ReSharper disable once UnusedMember.Local
        private void CheckData(BasePlayer player)
        {
            if (BasePlayer.FindByID(player.userID) == null) return;
            if (_dataBase.Achievements.ContainsKey(player.userID)) return;
            _dataBase.Achievements.Add(player.userID, new List<AchievementDB>());
            foreach (var achievement in _config.CraftMenuSettings.Achievements)
            {
                _dataBase.Achievements[player.userID].Add(new AchievementDB
                {
                    Name = achievement.ENName,
                    Completed = achievement.Completed,
                    ID = achievement.ID,
                    IsLike = achievement.IsLike,
                    CollectedItems = new List<RequireItem>()
                });
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (container == null | item == null) return;
            if (container.GetOwnerPlayer() == null) return;
            if (container.HasFlag(ItemContainer.Flag.NoItemInput)) return;
            var player = container.GetOwnerPlayer();
            CheckData(player);
            if (!_dataBase.Achievements.ContainsKey(player.userID)) return;
            foreach (var achievement in _dataBase.Achievements[player.userID])
            {
                if (achievement.Completed) continue;
                var requireitems = _config.CraftMenuSettings.Achievements.Find(x => x.ID == achievement.ID);
                var finditem = requireitems.RequireItems.Find(x =>
                    x.Shortname == item.info.shortname &
                    x.SkinID == item.skin & x.Count <= item.amount);
                if (finditem == null) continue;
                if (achievement.CollectedItems.Contains(finditem)) continue;
                achievement.CollectedItems.Add(finditem);
                if (achievement.CollectedItems.Count < requireitems.RequireItems.Count) return;
                achievement.Completed = true;
                player.ChatMessage(
                    $" {achievement.Name} : {lang.GetMessage("ACHIEVEMENTS_COMPLETED", this, player.UserIDString)}");
                DrawUI_AchievementCompleted(player, achievement.ID);
            }
        }

        #endregion

        #region [Commands] / [Команды]

        [ChatCommand("craft")]
        // ReSharper disable once UnusedMember.Local
        private void Open_Main(BasePlayer player)
        {
            DrawUI_Main(player);
        }

        [ConsoleCommand("craft")]
        // ReSharper disable once UnusedMember.Local
        private void CraftCMD(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!args.HasArgs()) return;
            var list = _config.CraftMenuSettings.CraftItems;
            var item = list.Find(x => x.ID == args.Args[0].ToInt());
            if (item == null)
            {
                PrintWarning("ERROR : 2190 (Обратитесь к разработчику)");
                return;
            }

            Craft(player, item);
        }

        [ConsoleCommand("craft.info")]
        // ReSharper disable once UnusedMember.Local
        private void CraftInfoCMD(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!args.HasArgs()) return;
            var list = _config.CraftMenuSettings.CraftItems;
            var item = list.Find(x => x.ID == args.Args[0].ToInt());
            if (item == null)
            {
                PrintWarning("ERROR : 2208 (Обратитесь к разработчику)");
                return;
            }

            DrawUI_InfoPanel(player, item.Type, item);
        }

        [ConsoleCommand("craft.achievementsinfo")]
        // ReSharper disable once UnusedMember.Local
        private void CraftInfoAchievementsCMD(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!args.HasArgs()) return;
            var list = _config.CraftMenuSettings.Achievements;
            var item = list.Find(x => x.ID == args.Args[0].ToInt());
            if (item == null)
            {
                PrintWarning("ERROR : 2226 (Обратитесь к разработчику)");
                return;
            }

            DrawUI_AchievementInfo(player, item);
        }

        [ConsoleCommand("craft.infoitem")]
        // ReSharper disable once UnusedMember.Local
        private void CraftInfoItemCMD(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!args.HasArgs()) return;
            var list = new List<RequireItem>();
            foreach (var craftItem in _config.CraftMenuSettings.CraftItems)
            {
                list.AddRange(craftItem.Input);
                list.AddRange(craftItem.Output);
                list.AddRange(craftItem.ProductionMechanism);
                list.AddRange(craftItem.RequireItems);
            }

            var item = list.Find(x => x.ID == args.Args[0].ToInt());
            if (item == null)
            {
                PrintWarning("ERROR : 2252 (Обратитесь к разработчику)");
                return;
            }

            DrawUI_InfoPanel(player, item.TypeObject, new CraftItem
            {
                NameRU = item.ENName,
                SkinID = item.SkinID,
                Shortname = item.Shortname,
                Count = item.Count,
                ID = item.ID,
                Type = item.TypeObject
            });
        }

        [ConsoleCommand("craft.achievementpage")]
        // ReSharper disable once UnusedMember.Local
        private void AchievementPagination(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!args.HasArgs()) return;
            var page = args.Args[0].ToInt();
            var list = _config.CraftMenuSettings.Achievements;

            if (list == null)
            {
                PrintWarning("ERROR : 2279 (Обратитесь к разработчику)");
                return;
            }

            if (page < 0) return;
            if (page > Convert.ToInt32(list.Count / 8)) return;
            DrawUI_Achievements(player, page);
        }

        [ConsoleCommand("achievement.completed")]
        // ReSharper disable once UnusedMember.Local
        private void AchievementCompletePagination(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!args.HasArgs()) return;
            var id = args.Args[0].ToInt();
            var list = _config.CraftMenuSettings.Achievements;

            if (list == null)
            {
                PrintWarning("ERROR : 2300 (Обратитесь к разработчику)");
                return;
            }

            if (id < 0) return;
            if (id > Convert.ToInt32(list.Count / 8)) return;
            DrawUI_Main(player);
            DrawUI_AchievementInfo(player, list[id]);
            DrawUI_AchievementsInfoPagination(player, id);
        }

        [ConsoleCommand("craft.chestinfo")]
        // ReSharper disable once UnusedMember.Local
        private void ChestInfo(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!args.HasArgs()) return;
            var item = _config.CraftMenuSettings.CraftItems.Find(x => x.ID == args.Args[0].ToInt());
            if (item == null)
            {
                PrintWarning("ERROR : 2321 (Обратитесь к разработчику)");
                return;
            }

            DrawUI_Main(player);
            DrawUI_InfoPanel(player, item.Type, new CraftItem
            {
                NameRU = item.NameRU,
                NameENG = item.NameENG,
                SkinID = item.SkinID,
                Shortname = item.Shortname,
                Count = item.Count,
                ID = item.ID,
                Type = item.Type
            });
        }

        [ConsoleCommand("craft.achievementinfopage")]
        // ReSharper disable once UnusedMember.Local
        private void AchievementInfoPagination(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!args.HasArgs()) return;
            var page = args.Args[0].ToInt();
            var list = _config.CraftMenuSettings.Achievements.Find(f => f.ID == page);

            if (list == null)
            {
                PrintWarning("ERROR : 2358 (Обратитесь к разработчику)");
                return;
            }

            if (page < 0) return;
            if (page > _config.CraftMenuSettings.Achievements.Count - 1) return;
            DrawUI_AchievementInfo(player, list);
            DrawUI_AchievementsInfoPagination(player, page);
        }

        [ConsoleCommand("craft.page")]
        // ReSharper disable once UnusedMember.Local
        private void CraftPagination(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!args.HasArgs(2)) return;
            var category = args.Args[0].ToInt();
            var page = args.Args[1].ToInt();
            var list = _config.CraftMenuSettings.CraftItems;

            if (list == null)
            {
                PrintWarning("ERROR : 2381 (Обратитесь к разработчику)");
                return;
            }

            if (page < 0) return;
            if (page > Convert.ToInt32(list.Count / 4)) return;
            DrawUI_Pagination(player, (TypeObject)Enum.ToObject(typeof(TypeObject), category), page);
        }

        [ConsoleCommand("craft.category")]
        // ReSharper disable once UnusedMember.Local
        private void CraftCategory(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!args.HasArgs()) return;
            var list = _config.CraftMenuSettings.CraftItems;
            var category = args.Args[0].ToInt();

            if (list == null)
            {
                PrintWarning("ERROR : 2402 (Обратитесь к разработчику)");
                return;
            }

            DrawUI_Pagination(player, (TypeObject)Enum.ToObject(typeof(TypeObject), category), 0);
        }

        [ConsoleCommand("craft.select")]
        // ReSharper disable once UnusedMember.Local
        private void CraftSelect(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!args.HasArgs()) return;
            var list = _config.CraftMenuSettings.CraftItems;
            var item = list.Find(x => x.ID == args.Args[0].ToInt());
            if (item == null) return;
            DrawUI_CraftItem(player, item);
        }

        #endregion

        #region [DataBase]

        private class StoredData
        {
            public Dictionary<ulong, List<AchievementDB>> Achievements = new Dictionary<ulong, List<AchievementDB>>();
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _dataBase);

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

        private string GetImage(string image, ulong skin)
        {
            return (string)ImageLibrary.Call("GetImage", image, skin);
        }

        private void Craft(BasePlayer player, CraftItem craftItem)
        {
            CheckData(player);
            var achievement = _dataBase.Achievements[player.userID];
            if (achievement.Find(a => a.ID == craftItem.ID) != null) return;

            if (!GetAvalibleItems(player, craftItem)) return;
            foreach (var critem in craftItem.RequireItems)
                player.inventory.Take(null, ItemManager.FindItemDefinition(critem.Shortname).itemid, critem.Count);

            var item = ItemManager.CreateByName(craftItem.Shortname, craftItem.Count == 0 ? 1 : craftItem.Count);
            item.name = craftItem.NameRU;
            item.skin = craftItem.SkinID;
            player.GiveItem(item);
            CuiHelper.DestroyUi(player, UIMain);
        }

        private static bool GetAvalibleItems(BasePlayer player, CraftItem craftItem)
        {
            var list = craftItem.RequireItems;
            var listok = (from item in player.inventory.AllItems()
                where list.Exists(x => x.Shortname == item.info.shortname)
                let obj = list.Find(x => x.Shortname == item.info.shortname)
                where obj.SkinID == item.skin
                select item).Count();

            return listok >= list.Count;
        }

        private static void DestroyPagination(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, $"{UIMain}.Category.Items");
            CuiHelper.DestroyUi(player, $"{UIMain}.Pagination");
            CuiHelper.DestroyUi(player, $"{UIMain}.Pagination+");
            CuiHelper.DestroyUi(player, $"{UIMain}.Pagination-");
            CuiHelper.DestroyUi(player, $"{UIMain}.Pagination.Num");
            CuiHelper.DestroyUi(player, $"{UIMain}.Category.Mechanism");
            for (var i = 0; i <= 4; i++) CuiHelper.DestroyUi(player, $"{UIMain}.CategoryElement.{i}");
        }

        private static void DestroyCraftItem(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, $"{UIMain}.ItemIcon");
            CuiHelper.DestroyUi(player, $"{UIMain}.ItemName");
            CuiHelper.DestroyUi(player, $"{UIMain}.CraftButton");
            CuiHelper.DestroyUi(player, $"{UIMain}.Like");
            CuiHelper.DestroyUi(player, $"{UIMain}.CraftItemInfo");
        }

        private IEnumerator LoadImages()
        {
            foreach (var image in Images)
            {
                ImageLibrary.Call("AddImage", image.Value, image.Key);
            }

            yield return 0;
        }

        #endregion
    }
}