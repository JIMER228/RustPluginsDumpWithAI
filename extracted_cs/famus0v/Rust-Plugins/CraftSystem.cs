using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("CraftSystem", "Kira", "1.0.6")] 
    [Description("Craft system for Moon Rust")]
    public class CraftSystem : RustPlugin
    {
        #region [Vars] / [Переменные]

        [PluginReference] private Plugin ImageLibrary;
        private const string UIParent = "UI.CraftSystem";
        private static WaitForSeconds wait = new WaitForSeconds(0.1f);

        #endregion

        #region [Dictionary] / [Словари]

        public class CraftItem
        {
            public string Shortname;
            public string DisplayName;
            public string DisplayNameEng;
            public ulong SkinID;
            public string Image;
            public int Count;
            public List<RequiredItem> RequiredItems = new List<RequiredItem>();

            public class RequiredItem
            {
                public string DisplayName;
                public string Shortname;
                public ulong SkinID;
                public int Count;
            }
        }

        private Dictionary<string, string> Images = new Dictionary<string, string>
        {
            ["UI.CraftSystem.Background"] = "https://i.imgur.com/nGJ20Sw.jpg",
            ["UI.CraftSystem.Button.Active"] = "https://i.imgur.com/Otf0eAB.png",
            ["UI.CraftSystem.Button"] = "https://i.imgur.com/WvazCxc.png",
            ["UI.CraftSystem.Description"] = "https://i.imgur.com/3dqlJES.png",
            ["UI.CraftSystem.Category"] = "https://i.imgur.com/8Xbfwkp.png",
            ["UI.CraftSystem.Craft"] = "https://i.imgur.com/XAKtH3m.png",
            ["UI.CraftSystem.RequiredItem"] = "https://i.imgur.com/wikH8kL.png",
            ["UI.CraftSystem.Category"] = "https://i.imgur.com/8Xbfwkp.png"
        };

        #endregion

        #region [Configuraton] / [Конфигурация]

        private ConfigData _config;

        public class ConfigData
        {
            [JsonProperty(PropertyName = "CraftSystem - Config")]
            public CraftCFG CraftSettings = new CraftCFG();

            public class CraftCFG
            {
                [JsonProperty(PropertyName = "Список крафтов")]
                public List<CraftItem> CraftItems;
            }
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                CraftSettings = new ConfigData.CraftCFG
                {
                    CraftItems = new List<CraftItem>
                    {
                        new CraftItem
                        {
                            Shortname = "metal.facemask",
                            DisplayName = "Улучшенный шлем1",
                            Count = 1,
                            Image = "https://rustlabs.com/img/items180/smg.mp5.png",
                            RequiredItems = new List<CraftItem.RequiredItem>
                            {
                                new CraftItem.RequiredItem
                                {
                                    DisplayName = "Assault Rifle",
                                    Shortname = "rifle.ak",
                                    Count = 1000
                                },
                                new CraftItem.RequiredItem
                                {
                                    DisplayName = "Rock",
                                    Shortname = "rock",
                                    Count = 1000
                                },
                                new CraftItem.RequiredItem
                                {
                                    DisplayName = "Metal Ore",
                                    Shortname = "metal.ore",
                                    Count = 1000
                                },
                                new CraftItem.RequiredItem
                                {
                                    DisplayName = "Meteor Dust",
                                    Shortname = "ducttape",
                                    Count = 1000,
                                    SkinID = 2814895972
                                }
                            }
                        },
                        new CraftItem
                        {
                            Shortname = "metal.plate.torso",
                            DisplayName = "Улучшенный нагрудник",
                            Count = 1,
                            Image = "https://rustlabs.com/img/items180/smg.mp5.png",
                            RequiredItems = new List<CraftItem.RequiredItem>
                            {
                                new CraftItem.RequiredItem
                                {
                                    Shortname = "wood",
                                    Count = 1000
                                },
                                new CraftItem.RequiredItem
                                {
                                    Shortname = "stones",
                                    Count = 1000
                                },
                                new CraftItem.RequiredItem
                                {
                                    Shortname = "metal.ore",
                                    Count = 1000
                                },
                                new CraftItem.RequiredItem
                                {
                                    Shortname = "sulfur.ore",
                                    Count = 1000
                                }
                            }
                        },
                        new CraftItem
                        {
                            Shortname = "hoodie",
                            DisplayName = "Улучшенное худи",
                            Count = 1,
                            Image = "https://rustlabs.com/img/items180/smg.mp5.png",
                            RequiredItems = new List<CraftItem.RequiredItem>
                            {
                                new CraftItem.RequiredItem
                                {
                                    Shortname = "wood",
                                    Count = 1000
                                },
                                new CraftItem.RequiredItem
                                {
                                    Shortname = "stones",
                                    Count = 1000
                                },
                                new CraftItem.RequiredItem
                                {
                                    Shortname = "metal.ore",
                                    Count = 1000
                                },
                                new CraftItem.RequiredItem
                                {
                                    Shortname = "sulfur.ore",
                                    Count = 1000
                                }
                            }
                        },
                        new CraftItem
                        {
                            Shortname = "roadsign.kilt",
                            DisplayName = "Улучшенный килт",
                            Count = 1,
                            Image = "https://rustlabs.com/img/items180/smg.mp5.png",
                            RequiredItems = new List<CraftItem.RequiredItem>
                            {
                                new CraftItem.RequiredItem
                                {
                                    Shortname = "wood",
                                    Count = 1000
                                },
                                new CraftItem.RequiredItem
                                {
                                    Shortname = "stones",
                                    Count = 1000
                                },
                                new CraftItem.RequiredItem
                                {
                                    Shortname = "metal.ore",
                                    Count = 1000
                                },
                                new CraftItem.RequiredItem
                                {
                                    Shortname = "sulfur.ore",
                                    Count = 1000
                                }
                            }
                        },
                        new CraftItem
                        {
                            Shortname = "pants",
                            DisplayName = "Улучшенные штаны",
                            Count = 1,
                            Image = "https://rustlabs.com/img/items180/smg.mp5.png",
                            RequiredItems = new List<CraftItem.RequiredItem>
                            {
                                new CraftItem.RequiredItem
                                {
                                    Shortname = "wood",
                                    Count = 1000
                                },
                                new CraftItem.RequiredItem
                                {
                                    Shortname = "stones",
                                    Count = 1000
                                },
                                new CraftItem.RequiredItem
                                {
                                    Shortname = "metal.ore",
                                    Count = 1000
                                },
                                new CraftItem.RequiredItem
                                {
                                    Shortname = "sulfur.ore",
                                    Count = 1000
                                }
                            }
                        },
                        new CraftItem
                        {
                            Shortname = "shoes.boots",
                            DisplayName = "Улучшенные ботинки",
                            Count = 1,
                            Image = "https://rustlabs.com/img/items180/smg.mp5.png",
                            RequiredItems = new List<CraftItem.RequiredItem>
                            {
                                new CraftItem.RequiredItem
                                {
                                    Shortname = "wood",
                                    Count = 1000
                                },
                                new CraftItem.RequiredItem
                                {
                                    Shortname = "stones",
                                    Count = 1000
                                },
                                new CraftItem.RequiredItem
                                {
                                    Shortname = "metal.ore",
                                    Count = 1000
                                },
                                new CraftItem.RequiredItem
                                {
                                    Shortname = "sulfur.ore",
                                    Count = 1000
                                }
                            }
                        },
                        new CraftItem
                        {
                            Shortname = "tactical.gloves",
                            DisplayName = "Улучшенные перчатки",
                            Count = 1,
                            Image = "https://rustlabs.com/img/items180/smg.mp5.png",
                            RequiredItems = new List<CraftItem.RequiredItem>
                            {
                                new CraftItem.RequiredItem
                                {
                                    Shortname = "wood",
                                    Count = 1000
                                },
                                new CraftItem.RequiredItem
                                {
                                    Shortname = "stones",
                                    Count = 1000
                                },
                                new CraftItem.RequiredItem
                                {
                                    Shortname = "metal.ore",
                                    Count = 1000
                                },
                                new CraftItem.RequiredItem
                                {
                                    Shortname = "sulfur.ore",
                                    Count = 1000
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

        #region [Lang]

        protected override void LoadDefaultMessages()
        {
            var ru = new Dictionary<string, string>
            {
                ["metal.facemask"] = "Mask",
                ["metal.plate.torso"] = "Mask",
                ["hoodie"] = "Mask",
                ["roadsign.kilt"] = "Mask",
                ["pants"] = "Mask",
                ["shoes.boots"] = "Mask",
                ["tactical.gloves"] = "Mask",
                ["CRAFT"] = "КРАФТ",
                ["CATEGORY1"] = "CATEGORY1",
                ["CATEGORY2"] = "CATEGORY2",
                ["CATEGORY3"] = "CATEGORY3",
                ["EXIT"] = "ВЫХОД"
            };

            var en = new Dictionary<string, string>
            {
                ["metal.facemask"] = "Mask",
                ["metal.plate.torso"] = "Mask",
                ["hoodie"] = "Mask",
                ["roadsign.kilt"] = "Mask",
                ["pants"] = "Mask",
                ["shoes.boots"] = "Mask",
                ["tactical.gloves"] = "Mask",
                ["CRAFT"] = "CRAFT",
                ["CATEGORY1"] = "CATEGORY1",
                ["CATEGORY2"] = "CATEGORY2",
                ["CATEGORY3"] = "CATEGORY3",
                ["EXIT"] = "EXIT"
            };
            lang.RegisterMessages(ru, this, "ru");
            lang.RegisterMessages(en, this);
        }

        #endregion

        #region [DrawUI] / [Отрисовка UI]

        private void DrawUI_Main(BasePlayer player)
        {
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
            }, "Overlay", UIParent);
            ui.Add(new CuiElement
            {
                Parent = UIParent,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage("UI.CraftSystem.Background")
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
                Name = $"{UIParent}.ServerName",
                Parent = UIParent,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0.15 0.24 0.38 0.36"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.04062501 0.8648148",
                        AnchorMax = "0.165625 0.9333333"
                    }
                }
            });

            ui.Add(new CuiLabel
            {
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "0.67 0.74 0.88 1.00",
                    Text = ConVar.Server.hostname.ToUpper(),
                    FontSize = 15
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, $"{UIParent}.ServerName");

            ui.Add(new CuiButton
            {
                Button =
                {
                    Close = UIParent,
                    Color = "0.84 0.47 0.47 0.24"
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    FontSize = 15,
                    Text = lang.GetMessage("EXIT", this, player.UserIDString)
                },
                RectTransform =
                {
                    AnchorMin = "0.8760417 0.8648148",
                    AnchorMax = "0.959375 0.9333336"
                }
            }, UIParent);

            CuiHelper.DestroyUi(player, UIParent);
            CuiHelper.AddUi(player, ui);
            DrawUI_Categories(player, "metal.facemask");
        }

        private void DrawUI_Categories(BasePlayer player, string Category)
        {
            var ui = new CuiElementContainer();

            var y = 0;
            foreach (var item in _config.CraftSettings.CraftItems)
            {
                CuiHelper.DestroyUi(player, $"{UIParent}.Button.{y}");
                ui.Add(new CuiElement
                {
                    Name = $"{UIParent}.Button.{y}",
                    Parent = UIParent,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage(item.Shortname == Category
                                ? "UI.CraftSystem.Button.Active"
                                : "UI.CraftSystem.Button")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0.040625 {0.6916667 - (y * 0.0868819166666667)}",
                            AnchorMax = $"0.2916667 {0.7703702 - (y * 0.0868819166666667)}"
                        }
                    }
                });

                var name = " ";
                if (lang.GetLanguage(player.UserIDString) == "ru") name = item.DisplayName;
                if (lang.GetLanguage(player.UserIDString) == "en") name = item.DisplayNameEng;
                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"craft.select {item.Shortname}",
                        Color = "0 0 0 0"
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 15,
                        Color = "0.67 0.74 0.88 1.00",
                        Text = name
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, $"{UIParent}.Button.{y}");
                y++;
            }

            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_SelectItem(BasePlayer player, CraftItem item)
        {
            var ui = new CuiElementContainer
            {
                new CuiElement
                {
                    Name = $"{UIParent}.Description",
                    Parent = UIParent,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage("UI.CraftSystem.Description")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.7083334 0.162037",
                            AnchorMax = "0.871875 0.7777778"
                        }
                    }
                },
                new CuiElement
                {
                    Name = $"{UIParent}.Item.Icon",
                    Parent = UIParent,
                    Components =
                    {
                        new CuiRawImageComponent
                        { 
                            Color = "1 1 1 1",
                            Png = (string) ImageLibrary.Call("GetImage", $"{item.Shortname}_{item.SkinID}")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.3666667 0.262963",
                            AnchorMax = "0.6333333 0.7370371"
                        }
                    }
                },
                {
                    new CuiLabel
                    {
                        Text =
                        {
                            Align = TextAnchor.UpperLeft,
                            FontSize = 15,
                            Color = "0.67 0.74 0.88 1.00",
                            Text = $"{lang.GetMessage(item.Shortname, this, player.UserIDString)}",
                            Font = "robotocondensed-regular.ttf"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.1 0.05",
                            AnchorMax = "0.9 0.95"
                        }
                    },
                    $"{UIParent}.Description", $"{UIParent}.Item.Description"
                },
                new CuiElement
                {
                    Name = $"{UIParent}.Item.Craft",
                    Parent = UIParent,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Color = "1 1 1 1",
                            Png = GetImage("UI.CraftSystem.Craft")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.3927083 0.03703703",
                            AnchorMax = "0.6072916 0.1296296"
                        }
                    }
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Command = $"craft.item {item.Shortname}",
                            Color = "0 0 0 0"
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 25,
                            Text = lang.GetMessage("CRAFT", this, player.UserIDString),
                            Color = "0.67 0.74 0.88 1.00"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 0.95"
                        }
                    },
                    $"{UIParent}.Item.Craft", $"{UIParent}.Item.Craft.Button"
                }
            };


            var ry = 0;
            foreach (var ritem in item.RequiredItems)
            {
                ui.Add(new CuiElement
                {
                    Name = $"{UIParent}.Item.Craft.{ry}",
                    Parent = UIParent,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Color = "1 1 1 1",
                            Png = GetImage("UI.CraftSystem.RequiredItem")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0.876041 {0.6287037 - ry * 0.1555545}",
                            AnchorMax = $"0.959375 {0.7777778 - ry * 0.1555545}"
                        }
                    }
                });

                var color = GetAvalibleItem(player, ritem.Shortname, ritem.Count, ritem.SkinID)
                    ? "0.67 0.74 0.88 1"
                    : "0.84 0.47 0.47 1";

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 11,
                        Color = color,
                        Text = $"{ritem.DisplayName} x{ritem.Count}",
                        Font = "robotocondensed-regular.ttf"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 0.2"
                    }
                }, $"{UIParent}.Item.Craft.{ry}");

                ui.Add(new CuiElement
                {
                    Parent = $"{UIParent}.Item.Craft.{ry}",
                    Components =
                    { 
                        new CuiRawImageComponent
                        {
                            Color = "1 1 1 1",
                            Png = (string) ImageLibrary.Call("GetImage", ritem.Shortname, ritem.SkinID)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.15 0.15",
                            AnchorMax = "0.85 0.85"
                        }
                    }
                });
                ry++;
            }

            CuiHelper.DestroyUi(player, $"{UIParent}.Item.Icon");
            CuiHelper.DestroyUi(player, $"{UIParent}.Description");
            CuiHelper.DestroyUi(player, $"{UIParent}.Item.Craft.Button");
            CuiHelper.DestroyUi(player, $"{UIParent}.Item.Craft");
            for (var i = 0; i <= 4; i++)
            {
                CuiHelper.DestroyUi(player, $"{UIParent}.Item.Craft.{i}");
            }

            CuiHelper.AddUi(player, ui);
            DrawUI_Categories(player, item.Shortname);
        }

        #endregion

        #region [Hooks] / [Крюки]

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            ServerMgr.Instance.StartCoroutine(LoadImages());
            var image = new KeyValuePair<string, ulong>("ducttape", 2814895972);
            var list = new List<KeyValuePair<string, ulong>> {image};
            ImageLibrary.Call("LoadImageList", Name, list);
        }

        #endregion

        #region [Commands] / [Команды]

        [ChatCommand("craft")]
        // ReSharper disable once UnusedMember.Local
        private void OpenCraft(BasePlayer player)
        {
            DrawUI_Main(player);
            DrawUI_SelectItem(player, _config.CraftSettings.CraftItems[0]);
        }

        [ConsoleCommand("craft.select")]
        // ReSharper disable once UnusedMember.Local
        private void SelectItemCMD(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (args.Args.Length < 1) return;
            var item = args.Args[0];
            var obj = _config.CraftSettings.CraftItems.Find(x => x.Shortname == item);
            DrawUI_SelectItem(player, obj);
        }

        [ConsoleCommand("craft.item")]
        // ReSharper disable once UnusedMember.Local
        private void CraftItemCMD(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            Craft(player, _config.CraftSettings.CraftItems.Find(x => x.Shortname == args.Args[0]));
        }

        [ConsoleCommand("craft.close")]
        // ReSharper disable once UnusedMember.Local
        private void CloseCraft(ConsoleSystem.Arg args)
        {
            CuiHelper.DestroyUi(args.Player(), UIParent);
        }

        #endregion

        #region [Helpers] / [Вспомогательный код]

        private static void Craft(BasePlayer player, CraftItem craftItem)
        {
            var list = (from ritem in craftItem.RequiredItems
                where GetAvalibleItem(player, ritem.Shortname, ritem.Count, ritem.SkinID)
                select player.inventory.FindItemID(craftItem.Shortname)).ToList();

            if (list.Count < 4) return;
            foreach (var critem in craftItem.RequiredItems)
                player.inventory.Take(null, ItemManager.FindItemDefinition(critem.Shortname).itemid, critem.Count);


            var item = ItemManager.CreateByName(craftItem.Shortname, craftItem.Count);
            item.name = craftItem.Shortname;
            item.skin = craftItem.SkinID;
            player.GiveItem(item);
            player.ChatMessage($"Crafted : {craftItem.DisplayName}");
            CuiHelper.DestroyUi(player, UIParent);
        }

        private static bool GetAvalibleItem(BasePlayer player, string shortname, int count, ulong skinid)
        {
            var item = player.inventory.FindItemID(shortname);
            if (item == null) return false;
            if (item.skin != skinid) return false;
            return item.amount >= count;
        }

        private IEnumerator LoadImages()
        {
            ImageLibrary.Call("AddImage", "https://i.imgur.com/fQgrkDH.png", "ducctape.2814895972");
            foreach (var image in Images)
            {
                ImageLibrary.Call("AddImage", image.Value, image.Key);
                yield return wait;
            }

            foreach (var item in _config.CraftSettings.CraftItems)
            {
                ImageLibrary.Call("AddImage", item.Image, $"{item.Shortname}_{item.SkinID}");
                yield return wait;
            } 

            yield return 0;
        }

        private string GetImage(string name)
        {
            return (string) ImageLibrary.Call("GetImage", name);
        }

        #endregion
    }
}