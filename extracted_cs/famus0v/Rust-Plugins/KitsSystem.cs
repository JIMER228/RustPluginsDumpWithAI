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
    [Info("KitsSystem", "Kira", "1.1.4")]
    [Description("Uniq kit system")]
    public class KitsSystem : RustPlugin
    {
        #region [Vars]

        [PluginReference] private Plugin ImageLibrary;
        private IgnoredKits Ignored = new IgnoredKits();
        private KitCooldowns kitCooldowns = new KitCooldowns();
        private const string UIMain = "UI.KitsSystem";
        private const float UITransparency = 0.25f;

        public class Weapon
        {
            public string AmmoType { get; set; } 
            public int AmmoAmount { get; set; }
        }

        public class ItemContent
        {
            public string ShortName { get; set; }
            public float Condition { get; set; }
            public int Amount { get; set; }
        }

        public class Kit
        {
            public string Name;
            public string DisplayName;
            public string Image;
            public int CoolDown = 86400;
            public string Permission;
            public List<KitItem> Main = new List<KitItem>();
            public List<KitItem> Wear = new List<KitItem>();
            public List<KitItem> Belt = new List<KitItem>();
        }

        public class KitCooldown
        {
            public string KitName;
            public int Cooldown;
        }

        public class KitItem
        {
            public string ShortName;
            public int Amount;
            public ulong SkinID;
            public float Condition;
            public Weapon Weapon;
            public List<ItemContent> Contents = new List<ItemContent>();
        }

        #endregion

        #region [Configuraton] / [Конфигурация]

        private ConfigData _config;

        public class ConfigData
        {
            [JsonProperty(PropertyName = "KitsSystem - Настройка")]
            public KitCfg KitConfig = new KitCfg();

            public class KitCfg
            {
                [JsonProperty(PropertyName = "AutoWipe?")]
                public bool AutoWipe;

                [JsonProperty(PropertyName = "Standart")]
                public List<Kit> KitsStandart = new List<Kit>();

                [JsonProperty(PropertyName = "Discord")]
                public List<Kit> KitsDiscord = new List<Kit>();

                [JsonProperty(PropertyName = "Premium")]
                public List<Kit> KitsPremium = new List<Kit>();

                [JsonProperty(PropertyName = "AutoKits")]
                public List<Kit> AutoKits = new List<Kit>();
            }
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                KitConfig = new ConfigData.KitCfg
                {
                    KitsStandart = new List<Kit>
                    {
                        new Kit
                        {
                            Name = "123123",
                            DisplayName = "asdasd",
                            CoolDown = 0,
                            Permission = "kitssystem.standart"
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

        #region [Lang]

        protected override void LoadDefaultMessages()
        {
            var ru = new Dictionary<string, string>
            {
                ["DETAILS"] = "ДЕТАЛИ",
                ["STANDART"] = "СТАНДАРТНЫЕ",
                ["NOPERM"] = "НЕДОСТУПНО",
                ["GIVE"] = "ПОЛУЧИТЬ",
                ["NOACCESS"] = "У вас нет доступа к этому набору",
                ["CD"] = "Вы сможете взять этот комплект через",
                ["NOTFOUND"] = "Набор не найден",
                ["ENTERTITLE"] = "Введите параметры",
                ["NOSPACE"] = "Нет места в инвентаре"
            };

            var en = new Dictionary<string, string>
            {
                ["DETAILS"] = "DETAILS",
                ["STANDART"] = "STANDART",
                ["NOPERM"] = "UNAVAILABLE",
                ["GIVE"] = "GET",
                ["NOACCESS"] = "You don't have access to this set",
                ["CD"] = "You will be able to take this Kit through",
                ["NOTFOUND"] = "Kit not found",
                ["ENTERTITLE"] = "Enter title",
                ["NOSPACE"] = "There is no place in the inventory"
            };
            lang.RegisterMessages(ru, this, "ru");
            lang.RegisterMessages(en, this);
        }

        #endregion

        #region [UI]

        private void UI_Main(BasePlayer player)
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            var ui = new CuiElementContainer();

            ui.Add(new CuiPanel
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
            }, "Overlay", UIMain);

            ui.Add(new CuiElement
            {
                Parent = UIMain,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0.13 0.13 0.15 0.99"
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


            ui.Add(new CuiLabel
            {
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 25,
                    Color = "0.25 0.25 0.26 1.00",
                    Text = lang.GetMessage("STANDART", this, player.UserIDString)
                },
                RectTransform =
                {
                    AnchorMin = "0 0.8861006",
                    AnchorMax = "1 0.9916528"
                }
            }, UIMain);

            ui.Add(new CuiElement
            {
                Parent = UIMain,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage("UI.KitsSystem.Discord", 0),
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.4399733 0.5796282",
                        AnchorMax = "0.5600267 0.6851804"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Parent = UIMain,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage("UI.KitsSystem.Premium", 0),
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.4399733 0.2944449",
                        AnchorMax = "0.5600267 0.4000005"
                    }
                }
            });

            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.AddUi(player, ui);
            DrawUI_KitList1(player, 0);
            DrawUI_KitList2(player, 0);
            DrawUI_KitList3(player, 0);
            DrawUI_Pagination1(player, 0);
            DrawUI_Pagination2(player, 0);
            DrawUI_Pagination3(player, 0);
        }

        private void DrawUI_KitList1(BasePlayer player, int page)
        {
            var ui = new CuiElementContainer();

            var x = 0;
            var pos = 0.5f - (_config.KitConfig.KitsStandart.Skip(5 * page).Take(5).Count() * 0.17f +
                              (_config.KitConfig.KitsStandart.Skip(5 * page).Take(5).Count() - 1) * 0.005f) / 2;

            foreach (var kit in _config.KitConfig.KitsStandart.Skip(5 * page).Take(5))
            {
                var isLocked = !permission.UserHasPermission(player.UserIDString, kit.Permission) ||
                               IsCooldown(player, kit.Name);
                var parent = $"{UIMain}.Kits1.{x}";
                if (!kitCooldowns.KitCooldown.ContainsKey(player.userID))
                    kitCooldowns.KitCooldown.Add(player.userID, new List<KitCooldown>());
                var cooldown = kitCooldowns.KitCooldown[player.userID]
                    .Find(d => d.KitName.ToLower() == kit.Name.ToLower());

                var txt = lang.GetMessage("GIVE", this, player.UserIDString);
                if (isLocked) txt = lang.GetMessage("NOPERM", this, player.UserIDString);
                if (cooldown != null)
                {
                    txt = $"{ConvertTime(cooldown.Cooldown)}";
                }

                ui.Add(new CuiElement
                {
                    Name = parent,
                    Parent = UIMain,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage("UI.KitsSystem.Kit1", 0),
                            Color = isLocked ? $"1 1 1 {UITransparency}" : "1 1 1 1"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{pos} 0.7555556",
                            AnchorMax = $"{pos + 0.15545151f} 0.8777403"
                        }
                    }
                });

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 25,
                        Color = isLocked ? $"0.94 0.39 0.12 {UITransparency}" : "0.94 0.39 0.12 0.78",
                        Text = $"{kit.DisplayName.ToUpper()}\n" +
                               $"<size=13><color=#63666f>{lang.GetMessage("DETAILS", this, player.UserIDString)}</color></size>"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, $"{parent}");

                ui.Add(new CuiElement
                {
                    Name = $"{parent}.Get",
                    Parent = parent,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage("UI.KitsSystem.Get", 0),
                            Color = isLocked ? $"1 1 1 {UITransparency}" : "1 1 1 1"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 -0.5228875",
                            AnchorMax = "1 -0.07"
                        }
                    }
                });

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"kit.give {kit.Name} standart",
                        Color = "0 0 0 0"
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 15,
                        Text = txt,
                        Color = isLocked ? $"1 1 1 {UITransparency}" : "1 1 1 0.38"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, $"{parent}.Get");

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"kit.desc {kit.Name} standart",
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

                x++;
                pos += 0.175f + 0.005f;
            }

            DestroyKits(player, 1);
            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_Pagination1(BasePlayer player, int page)
        {
            var ui = new CuiElementContainer
            {
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"kit.page {page - 1} standart",
                            Color = "0 0 0 0"
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 35,
                            Text = "<",
                            Color = "1.00 1.00 1.00 0.38"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.0004323007 0.7541667",
                            AnchorMax = "0.05251792 0.8467579"
                        }
                    },
                    UIMain, $"{UIMain}.Left1"
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"kit.page {page + 1} standart",
                            Color = "0 0 0 0"
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 35,
                            Text = ">",
                            Color = "1.00 1.00 1.00 0.38"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.9474843 0.7541667",
                            AnchorMax = "0.99957 0.8467579"
                        }
                    },
                    UIMain, $"{UIMain}.Right1"
                }
            };

            CuiHelper.DestroyUi(player, $"{UIMain}.Left1");
            CuiHelper.DestroyUi(player, $"{UIMain}.Right1");
            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_KitList2(BasePlayer player, int page)
        {
            var ui = new CuiElementContainer();

            var list = new List<Kit>();
            foreach (var obj in _config.KitConfig.KitsDiscord)
            {
                if (!Ignored.IgnoredKit.ContainsKey(player.userID))
                    Ignored.IgnoredKit.Add(player.userID, new List<string>());
                if (!Ignored.IgnoredKit[player.userID].Contains(obj.Permission)) list.Add(obj);
            }

            var x = 0;
            var pos = 0.5f - (list.Skip(5 * page).Take(5).Count() * 0.175f +
                              (list.Skip(5 * page).Take(5).Count() - 1) * 0.005f) / 2;
            foreach (var kit in list.Skip(5 * page).Take(5))
            {
                var isLocked = !permission.UserHasPermission(player.UserIDString, kit.Permission) ||
                               IsCooldown(player, kit.Name);
                var parent = $"{UIMain}.Kits2.{x}";
                if (!kitCooldowns.KitCooldown.ContainsKey(player.userID))
                    kitCooldowns.KitCooldown.Add(player.userID, new List<KitCooldown>());
                var cooldown = kitCooldowns.KitCooldown[player.userID]
                    .Find(d => d.KitName.ToLower() == kit.Name.ToLower());
                var txt = lang.GetMessage("GIVE", this, player.UserIDString);
                if (isLocked) txt = lang.GetMessage("NOPERM", this, player.UserIDString);
                if (cooldown != null)
                {
                    txt = $"{ConvertTime(cooldown.Cooldown)}";
                }

                ui.Add(new CuiElement
                {
                    Name = parent,
                    Parent = UIMain,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage("UI.KitsSystem.Kit2", 0),
                            Color = isLocked ? $"1 1 1 {UITransparency}" : "1 1 1 1"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{pos} 0.4611111",
                            AnchorMax = $"{pos + 0.15545151f} 0.5832958"
                        }
                    }
                });

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 25,
                        Color = isLocked ? $"1 1 1 {UITransparency}" : "1 1 1 0.42",
                        Text = $"{kit.DisplayName.ToUpper()}\n" +
                               $"<size=13><color=#FFFFFF6B>{lang.GetMessage("DETAILS", this, player.UserIDString)}</color></size>"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, parent);

                ui.Add(new CuiElement
                {
                    Name = $"{parent}.Get",
                    Parent = parent,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage("UI.KitsSystem.Get", 0),
                            Color = isLocked ? $"1 1 1 {UITransparency}" : "1 1 1 1"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 -0.5228875",
                            AnchorMax = "1 -0.07"
                        }
                    }
                });

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"kit.give {kit.Name} discord",
                        Color = "0 0 0 0"
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 15,
                        Text = txt,
                        Color = isLocked ? $"1 1 1 {UITransparency}" : "1 1 1 0.38"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, $"{parent}.Get");

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"kit.desc {kit.Name} discord",
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

                x++;
                pos += 0.175f + 0.005f;
            }

            DestroyKits(player, 2);
            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_Pagination2(BasePlayer player, int page)
        {
            var ui = new CuiElementContainer
            {
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"kit.page {page - 1} discord",
                            Color = "0 0 0 0"
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 35,
                            Text = "<",
                            Color = "1.00 1.00 1.00 0.38"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.0004323007 0.4611111",
                            AnchorMax = "0.05251792 0.5537022"
                        }
                    },
                    UIMain, $"{UIMain}.Left2"
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"kit.page {page + 1} discord",
                            Color = "0 0 0 0"
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 35,
                            Text = ">",
                            Color = "1.00 1.00 1.00 0.38"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.9474843 0.4611111",
                            AnchorMax = "0.99957 0.5537022"
                        }
                    },
                    UIMain, $"{UIMain}.Right2"
                }
            };

            CuiHelper.DestroyUi(player, $"{UIMain}.Left2");
            CuiHelper.DestroyUi(player, $"{UIMain}.Right2");
            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_KitList3(BasePlayer player, int page)
        {
            var ui = new CuiElementContainer();
            var x = 0;
            var pos = 0.5f - (_config.KitConfig.KitsPremium.Skip(5 * page).Take(5).Count() * 0.175f +
                              (_config.KitConfig.KitsPremium.Skip(5 * page).Take(5).Count() - 1) * 0.005f) / 2;
            foreach (var kit in _config.KitConfig.KitsPremium.Skip(5 * page).Take(5))
            {
                var isLocked = !permission.UserHasPermission(player.UserIDString, kit.Permission) ||
                               IsCooldown(player, kit.Name);
                var parent = $"{UIMain}.Kits3.{x}";
                if (!kitCooldowns.KitCooldown.ContainsKey(player.userID))
                    kitCooldowns.KitCooldown.Add(player.userID, new List<KitCooldown>());
                var cooldown = kitCooldowns.KitCooldown[player.userID]
                    .Find(d => d.KitName.ToLower() == kit.Name.ToLower());
                var txt = lang.GetMessage("GIVE", this, player.UserIDString);
                if (isLocked) txt = lang.GetMessage("NOPERM", this, player.UserIDString);
                if (cooldown != null)
                {
                    txt = $"{ConvertTime(cooldown.Cooldown)}";
                }

                ui.Add(new CuiElement
                {
                    Name = parent,
                    Parent = UIMain,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage("UI.KitsSystem.Kit3", 0),
                            Color = isLocked ? $"1 1 1 {UITransparency}" : "1 1 1 1"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{pos} 0.1666667",
                            AnchorMax = $"{pos + 0.15545151f} 0.2888514"
                        }
                    }
                });

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 25,
                        Color = isLocked ? $"1 1 1 {UITransparency}" : "1 1 1 0.42",
                        Text = $"{kit.DisplayName.ToUpper()}\n" +
                               $"<size=13><color=#FFFFFF6B>{lang.GetMessage("DETAILS", this, player.UserIDString)}</color></size>"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, parent);

                ui.Add(new CuiElement
                {
                    Name = $"{parent}.Get",
                    Parent = parent,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage("UI.KitsSystem.Get", 0),
                            Color = isLocked ? $"1 1 1 {UITransparency}" : "1 1 1 1"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 -0.5228875",
                            AnchorMax = "1 -0.07"
                        }
                    }
                });

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"kit.give {kit.Name} premium",
                        Color = "0 0 0 0"
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 15,
                        Text = txt,
                        Color = isLocked ? $"1 1 1 {UITransparency}" : "1 1 1 0.38"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, $"{parent}.Get");

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"kit.desc {kit.Name} premium",
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

                x++;
                pos += 0.175f + 0.005f;
            }

            DestroyKits(player, 3);
            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_Pagination3(BasePlayer player, int page)
        {
            var ui = new CuiElementContainer
            {
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"kit.page {page - 1} premium",
                            Color = "0 0 0 0"
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 35,
                            Text = "<",
                            Color = "1.00 1.00 1.00 0.38"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.0004323007 0.1652778",
                            AnchorMax = "0.05251792 0.2578689"
                        }
                    },
                    UIMain, $"{UIMain}.Left3"
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"kit.page {page + 1} premium",
                            Color = "0 0 0 0"
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 35,
                            Text = ">",
                            Color = "1.00 1.00 1.00 0.38"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.9474843 0.1652778",
                            AnchorMax = "0.99957 0.2578689"
                        }
                    },
                    UIMain, $"{UIMain}.Right3"
                }
            };

            CuiHelper.DestroyUi(player, $"{UIMain}.Left3");
            CuiHelper.DestroyUi(player, $"{UIMain}.Right3");
            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_ViewDesc(BasePlayer player, string kitname, string type)
        {
            var ui = new CuiElementContainer();
            var kits = new List<Kit>();
            switch (type.ToLower())
            {
                case "standart":
                    kits = _config.KitConfig.KitsStandart;
                    break;
                case "discord":
                    kits = _config.KitConfig.KitsDiscord;
                    break;
                case "premium":
                    kits = _config.KitConfig.KitsPremium;
                    break;
            }

            var parent = $"{UIMain}.View";
            var kit = kits.Find(x => string.Equals(x.Name, kitname, StringComparison.CurrentCultureIgnoreCase));
            var list = new List<KitItem>();
            list.AddRange(kit.Belt);
            list.AddRange(kit.Wear);
            list.AddRange(kit.Main);

            ui.Add(new CuiButton
            {
                Button =
                {
                    Color = "0.13 0.13 0.15 0.5",
                    Close = $"{UIMain}.View",
                    Material = "assets/content/ui/uibackgroundblur.mat"
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
            }, UIMain, $"{UIMain}.View");

            var pos = 0.5f - ((list.Count > 12 ? 12 : list.Count) * 0.07f +
                              ((list.Count > 12 ? 12 : list.Count) - 1) * 0.005f) / 2;

            int t = 0, y = 0;
            foreach (var item in list.Take(84))
            {
                if (ItemManager.FindItemDefinition(item.ShortName) == null) continue;
                if (t == 12)
                {
                    pos = 0.62f - ((list.Count > 12 ? 12 : list.Count) * 0.09f +
                                   ((list.Count > 12 ? 12 : list.Count) - 1) * 0.005f) / 2;
                    t = 0;
                    y++;
                }

                ui.Add(new CuiElement
                {
                    Name = $"{parent}.Item.{t}",
                    Parent = parent,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0.56 0.55 0.54 0.15"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{pos} {0.8138797 - (y * 0.127f)}",
                            AnchorMax = $"{pos + 0.07f} {0.9323964 - (y * 0.127f)}"
                        }
                    }
                });

                ui.Add(new CuiElement
                {
                    Parent = $"{parent}.Item.{t}",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage(item.ShortName, item.SkinID)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.05 0.05",
                            AnchorMax = "0.95 0.95"
                        }
                    }
                });

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 11,
                        Text = $"x{item.Amount}",
                        Color = "1.00 1.00 1.00 0.38"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 0.2"
                    }
                }, $"{parent}.Item.{t}");


                pos += 0.07f + 0.005f;
                t++;
            }

            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_View(BasePlayer player, string kitname)
        {
            var ui = new CuiElementContainer
            {
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Color = "0.13 0.13 0.15 0.5",
                            Close = $"{UIMain}.View",
                            Material = "assets/content/ui/ui.background.tile.psd"
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
                    },
                    "Overlay", $"{UIMain}.View"
                },
                new CuiElement
                {
                    Parent = $"{UIMain}.View",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage(kitname, 0)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.1666666 0.1444445",
                            AnchorMax = "0.8333334 0.8555555"
                        }
                    }
                }
            };

            CuiHelper.AddUi(player, ui);
        }

        #endregion

        #region [Hooks]

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            LoadData();
            ServerMgr.Instance.StartCoroutine(LoadImages());
        }

        // ReSharper disable once UnusedMember.Local
        private void Unload()
        {
            SaveData();
            SaveConfig();
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void OnNewSave(string filename)
        {
            if (_config.KitConfig.AutoWipe != true) return;
            LoadData();
            kitCooldowns.KitCooldown.Clear();
            SaveData();
        }

        // ReSharper disable once UnusedMember.Local
        private void OnPlayerRespawned(BasePlayer player)
        {
            foreach (var kit in _config.KitConfig.AutoKits.Where(kit =>
                         permission.UserHasPermission(player.UserIDString, kit.Permission)))
            {
                player.inventory.Strip();
                GiveKit(player, kit.Name, "autokits");
            }
        }

        #endregion

        #region [Commands]

        [ConsoleCommand("Kit")]
        // ReSharper disable once UnusedMember.Local
        private void OpenKits(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            UpdateGroups(args.Player(), "discord");
            UI_Main(args.Player());
        }

        [ConsoleCommand("kit.desc")]
        // ReSharper disable once UnusedMember.Local
        private void ViewDesc(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            DrawUI_ViewDesc(args.Player(), args.Args[0], args.Args[1]);
        }

        [ConsoleCommand("kit.wipe")]
        // ReSharper disable once UnusedMember.Local
        private void Wipe(ConsoleSystem.Arg args)
        {
            if (args.Player() != null) return;
            kitCooldowns.KitCooldown.Clear();
        }

        [ConsoleCommand("kit.wipeignore")]
        // ReSharper disable once UnusedMember.Local
        private void WipeIgnore(ConsoleSystem.Arg args)
        {
            if (args.Player() != null) return;
            Ignored.IgnoredKit.Clear();
        }

        [ChatCommand("Kit")]
        // ReSharper disable once UnusedMember.Local
        private void OpenKitsCh(BasePlayer player)
        {
            if (player == null) return;
            UpdateGroups(player, "discord");
            UI_Main(player);
        }

        [ConsoleCommand("kit.page")]
        // ReSharper disable once UnusedMember.Local
        private void Pagination(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            var page = args.Args[0].ToInt();
            List<Kit> list;
            switch (args.Args[1].ToLower())
            {
                case "standart":
                    list = _config.KitConfig.KitsStandart;
                    if (!list.Skip(5 * page).Take(5).Any() & page > 0) return;
                    DrawUI_KitList1(player, page);
                    break;
                case "discord":
                    list = _config.KitConfig.KitsDiscord;
                    if (!list.Skip(5 * page).Take(5).Any() & page > 0) return;
                    DrawUI_KitList2(player, page);
                    break;
                case "premium":
                    list = _config.KitConfig.KitsPremium;
                    if (!list.Skip(5 * page).Take(5).Any() & page > 0) return;
                    DrawUI_KitList3(player, page);
                    break;
            }
        }

        [ChatCommand("Kit.add")]
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void AddKits(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;

            if (args.Length < 2)
            {
                SendReply(player, lang.GetMessage("ENTERTITLE", this, player.UserIDString));
                return;
            }

            var kitname = args[0].ToLower();
            List<Kit> list;
            switch (args[1].ToLower())
            {
                case "standart":
                    list = _config.KitConfig.KitsStandart;
                    break;
                case "discord":
                    list = _config.KitConfig.KitsDiscord;
                    break;
                case "premium":
                    list = _config.KitConfig.KitsPremium;
                    break;
                case "autokits":
                    list = _config.KitConfig.KitsPremium;
                    break;
                default:
                    list = _config.KitConfig.KitsStandart;
                    player.ChatMessage("Данной категории нет в списке [standart, discord, premium, autokits]");
                    return;
            }

            if (list.Exists(f => f.Name.ToLower() == kitname))
            {
                SendReply(player, $"Kit {kitname.ToUpper()} already created");
                return;
            }


            AddKit(player, kitname, args[1]);
        }

        [ConsoleCommand("kit.give")]
        // ReSharper disable once UnusedMember.Local
        private void GiveKits(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            if (args.Args.Length < 1)
            {
                SendReply(args.Player(), lang.GetMessage("ENTERTITLE", this, args.Player().UserIDString));
                return;
            }

            GiveKit(args.Player(), args.Args[0], args.Args[1]);
        }

        [ChatCommand("kit.remove")]
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void RemoveKits(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            if (!player.IsAdmin) return;
            if (args.Length < 1)
            {
                SendReply(player, lang.GetMessage("ENTERTITLE", this, player.UserIDString));
                return;
            }

            RemoveKit(player, args[0], args[1]);
        }

        [ConsoleCommand("kit.close")]
        // ReSharper disable once UnusedMember.Local
        private void CloseKits(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            CuiHelper.DestroyUi(args.Player(), UIMain);
        }

        #endregion

        #region [DataBase]

        public class IgnoredKits
        {
            public Dictionary<ulong, List<string>> IgnoredKit = new Dictionary<ulong, List<string>>();
        }

        public class KitCooldowns
        {
            public readonly Dictionary<ulong, List<KitCooldown>> KitCooldown =
                new Dictionary<ulong, List<KitCooldown>>();
        }

        #endregion

        #region [Helpers]

        private static void DestroyKits(BasePlayer player, int num)
        {
            for (var i = 0; i <= 5; i++) CuiHelper.DestroyUi(player, $"{UIMain}.Kits{num}.{i}");
        }

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        private static int CurrentTime() => (int)DateTime.UtcNow.Subtract(Epoch).TotalSeconds;

        private bool IsCooldown(BasePlayer player, string kitname)
        {
            if (!kitCooldowns.KitCooldown.ContainsKey(player.userID))
                kitCooldowns.KitCooldown.Add(player.userID, new List<KitCooldown>());

            var playercooldown = kitCooldowns.KitCooldown[player.userID];
            if (!playercooldown.Exists(a => a.KitName == kitname.ToLower())) return false;
            {
                var cooldown = playercooldown.Find(r => r.KitName == kitname);
                if (0 <= (cooldown.Cooldown - CurrentTime()))
                {
                    return true;
                }

                playercooldown.Remove(cooldown);
            }

            SaveData();
            return false;
        }

        private IEnumerator LoadImages()
        {
            ImageLibrary.Call("AddImage", "https://i.imgur.com/04ITjCS.png", "UI.KitsSystem.Get");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/ZJqyGAt.png", "UI.KitsSystem.Kit1");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/vedZx1g.png", "UI.KitsSystem.Kit2");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/ozrZjCI.png", "UI.KitsSystem.Kit3");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/oxcUCwQ.png", "UI.KitsSystem.Discord");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/efUXlUK.png", "UI.KitsSystem.Premium");
            foreach (var kit in _config.KitConfig.KitsStandart)
                ImageLibrary.Call("AddImage", kit.Image, kit.Name);
            foreach (var kit in _config.KitConfig.KitsDiscord)
                ImageLibrary.Call("AddImage", kit.Image, kit.Name);
            foreach (var kit in _config.KitConfig.KitsPremium)
                ImageLibrary.Call("AddImage", kit.Image, kit.Name);
            foreach (var kit in _config.KitConfig.KitsStandart)
                permission.RegisterPermission(kit.Permission, this);
            foreach (var kit in _config.KitConfig.KitsDiscord)
                permission.RegisterPermission(kit.Permission, this);
            foreach (var kit in _config.KitConfig.KitsPremium)
                permission.RegisterPermission(kit.Permission, this);
            foreach (var kit in _config.KitConfig.AutoKits)
                permission.RegisterPermission(kit.Permission, this);
            yield return 0;
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("KitsSystem/IgnoredKits", Ignored);
            Interface.Oxide.DataFileSystem.WriteObject("KitsSystem/KitCooldowns", Ignored);
        }

        private void LoadData()
        {
            try
            {
                kitCooldowns = Interface.Oxide.DataFileSystem.ReadObject<KitCooldowns>("KitsSystem/KitCooldowns");
                Ignored = Interface.Oxide.DataFileSystem.ReadObject<IgnoredKits>("KitsSystem/IgnoredKits");
            }
            catch (Exception)
            {
                Ignored = new IgnoredKits();
                kitCooldowns = new KitCooldowns();
            }
        }

        private static string ConvertTime(int seconds)
        {
            var time = TimeSpan.FromSeconds(seconds - CurrentTime());
            return $"{time.Days}D {time.Hours}H {time.Minutes}M {time.Seconds}S";
        }

        private static void GiveItem(PlayerInventory inv, Item item, ItemContainer cont = null)
        {
            if (item == null) return;
            // ReSharper disable once UnusedVariable
            var a = item.MoveToContainer(cont) || item.MoveToContainer(inv.containerBelt) ||
                    item.MoveToContainer(inv.containerWear) || item.MoveToContainer(inv.containerMain);
        }

        private void GiveItems(BasePlayer player, Kit kit)
        {
            foreach (var kitem in kit.Main)
            {
                GiveItem(player.inventory,
                    BuildItem(kitem.ShortName, kitem.Amount, kitem.SkinID, kitem.Condition, kitem.Weapon,
                        kitem.Contents), player.inventory.containerMain);
            }


            foreach (var kitem in kit.Wear)
            {
                GiveItem(player.inventory,
                    BuildItem(kitem.ShortName, kitem.Amount, kitem.SkinID, kitem.Condition, kitem.Weapon,
                        kitem.Contents), player.inventory.containerWear);
            }


            foreach (var kitem in kit.Belt)
            {
                GiveItem(player.inventory,
                    BuildItem(kitem.ShortName, kitem.Amount, kitem.SkinID, kitem.Condition, kitem.Weapon,
                        kitem.Contents), player.inventory.containerBelt);
            }

            CuiHelper.DestroyUi(player, UIMain);
        }

        private void AddKit(BasePlayer player, string name, string type)
        {
            var kit = new Kit
            {
                Name = name,
                DisplayName = name,
                Permission = $"{Name}.{name}",
                Main = CloneContainerMain(player),
                Belt = CloneContainerBelt(player),
                Wear = CloneContainerWear(player)
            };

            switch (type.ToLower())
            {
                case "standart":
                    _config.KitConfig.KitsStandart.Add(kit);
                    break;
                case "discord":
                    _config.KitConfig.KitsDiscord.Add(kit);
                    break;
                case "premium":
                    _config.KitConfig.KitsPremium.Add(kit);
                    break;
                case "autokits":
                    _config.KitConfig.AutoKits.Add(kit);
                    break;
            }

            permission.RegisterPermission($"{Name}.{name}", this);
            SendReply(player, $"Kit {name.ToUpper()} successfully created");
        }

        private void GiveKit(BasePlayer player, string kitname, string type)
        {
            List<Kit> list;
            switch (type.ToLower())
            {
                case "standart":
                    list = _config.KitConfig.KitsStandart;
                    break;
                case "discord":
                    list = _config.KitConfig.KitsDiscord;
                    break;
                case "premium":
                    list = _config.KitConfig.KitsPremium;
                    break;
                case "autokits":
                    list = _config.KitConfig.AutoKits;
                    break;
                default:
                    list = _config.KitConfig.KitsStandart;
                    break;
            }

            var kit = list.Find(s => s.Name == kitname);
            if (kit == null) return;


            if (!permission.UserHasPermission(player.UserIDString, $"{Name}.{kitname}"))
            {
                SendReply(player, lang.GetMessage("NOACCESS", this, player.UserIDString));
                DrawUI_View(player, kit.Name);
                return;
            }

            if (!kitCooldowns.KitCooldown.ContainsKey(player.userID))
                kitCooldowns.KitCooldown.Add(player.userID, new List<KitCooldown>());

            var playercooldown = kitCooldowns.KitCooldown[player.userID];
            if (IsCooldown(player, kit.Name))
            {
                var cooldown = playercooldown.Find(g => g.KitName == kitname);
                if (0 < (cooldown.Cooldown - CurrentTime()))
                {
                    var time = TimeSpan.FromSeconds(cooldown.Cooldown - CurrentTime());
                    SendReply(player,
                        $"{lang.GetMessage("CD", this, player.UserIDString)}{time.Days}:{time.Hours}:{time.Minutes}:{time.Seconds}");
                    return;
                }
            }

            var capacity = 38 - player.inventory.AllItems().Length;
            var kititemcount = kit.Belt.Count + kit.Wear.Count + kit.Main.Count;
            if (kititemcount > capacity)
            {
                player.ChatMessage(lang.GetMessage("NOSPACE", this, player.UserIDString));
                return;
            }

            GiveItems(player, kit);
            playercooldown.Add(new KitCooldown
            {
                KitName = kitname,
                Cooldown = CurrentTime() + kit.CoolDown
            });
        }

        private void RemoveKit(BasePlayer player, string kitname, string type)
        {
            List<Kit> list;
            switch (type.ToLower())
            {
                case "standart":
                    list = _config.KitConfig.KitsStandart;
                    break;
                case "discord":
                    list = _config.KitConfig.KitsDiscord;
                    break;
                case "premium":
                    list = _config.KitConfig.KitsPremium;
                    break;
                case "autokits":
                    list = _config.KitConfig.AutoKits;
                    break;
                default:
                    list = _config.KitConfig.KitsStandart;
                    break;
            }

            if (!list.Exists(x => x.Name == kitname))
            {
                SendReply(player, lang.GetMessage("NOTFOUND", this, player.UserIDString));
                return;
            }

            var kit = list.Find(x => x.Name == kitname);
            list.Remove(kit);
        }

        // ReSharper disable once UnusedParameter.Local
        private static KitItem ItemToKit(Item item, string container)
        {
            var kitem = new KitItem
            {
                Amount = item.amount,
                SkinID = item.skin,
                ShortName = item.info.shortname,
                Condition = item.condition,
                Weapon = null,
                Contents = null
            };
            if (item.info.category == ItemCategory.Weapon)
            {
                var weapon = item.GetHeldEntity() as BaseProjectile;
                if (weapon != null)
                {
                    kitem.Weapon = new Weapon
                    {
                        AmmoType = weapon.primaryMagazine.ammoType.shortname,
                        AmmoAmount = weapon.primaryMagazine.contents
                    };
                }
            }

            if (item.contents == null) return kitem;
            kitem.Contents = new List<ItemContent>();
            foreach (var cont in item.contents.itemList)
            {
                kitem.Contents.Add(new ItemContent
                {
                    Amount = cont.amount,
                    Condition = cont.condition,
                    ShortName = cont.info.shortname
                });
            }

            return kitem;
        }

        private Item BuildItem(string shortName, int amount, ulong skinID, float condition, Weapon weapon,
            List<ItemContent> content)
        {
            var item = ItemManager.CreateByName(shortName, amount > 1 ? amount : 1, skinID);
            item.condition = condition;
            if (weapon != null)
            {
                ((BaseProjectile)item.GetHeldEntity()).primaryMagazine.contents = weapon.AmmoAmount;
                ((BaseProjectile)item.GetHeldEntity()).primaryMagazine.ammoType =
                    ItemManager.FindItemDefinition(weapon.AmmoType);
            }

            if (content == null) return item;
            foreach (var cont in content)
            {
                var newCont = ItemManager.CreateByName(cont.ShortName, cont.Amount);
                newCont.condition = cont.Condition;
                newCont.MoveToContainer(item.contents);
            }

            return item;
        }

        private static List<KitItem> CloneContainerMain(BasePlayer player)
        {
            var list = player.inventory.containerMain.itemList;

            return list.Select(item => ItemToKit(item, "Main")).ToList();
        }

        private static List<KitItem> CloneContainerWear(BasePlayer player)
        {
            var list = player.inventory.containerWear.itemList;

            return list.Select(item => ItemToKit(item, "Wear")).ToList();
        }

        private static List<KitItem> CloneContainerBelt(BasePlayer player)
        {
            var list = player.inventory.containerBelt.itemList;

            return list.Select(item => ItemToKit(item, "Belt")).ToList();
        }

        private void UpdateGroups(BasePlayer player, string type)
        {
            var list = new List<string>();
            switch (type.ToLower())
            {
                case "discord":
                    list.AddRange(from kit in _config.KitConfig.KitsDiscord
                        where permission.UserHasPermission(player.UserIDString, kit.Permission)
                        select kit.Permission);
                    break;
            }

            var num = 0;
            foreach (var perm in list.Take(list.Count - 1))
            {
                if (num == 0)
                {
                    num++;
                    continue;
                }

                if (!Ignored.IgnoredKit.ContainsKey(player.userID))
                    Ignored.IgnoredKit.Add(player.userID, new List<string>());
                if (!Ignored.IgnoredKit[player.userID].Contains(perm)) Ignored.IgnoredKit[player.userID].Add(perm);
            }
        }

        private string GetImage(string image, ulong skin)
        {
            return (string)ImageLibrary.Call("GetImage", image, skin);
        }

        #endregion
    }
}