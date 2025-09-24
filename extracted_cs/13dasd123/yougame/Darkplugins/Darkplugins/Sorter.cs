using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("Sorter", "https://discord.gg/dNGbxafuJn", "1.0.8")]
    public class Sorter : RustPlugin
    {
        #region Commands

        private List<ulong> _butons = new List<ulong>();

        [ConsoleCommand("sorter")]
        void cmdToggle(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null) return;
            var player = arg.Player();
            var userId = player.userID;

            if (_butons.Contains(userId))
            {
                _butons.Remove(userId);
                DestroyUI(player);
                drawui(player);
            }
            else
            {
                _butons.Add(userId);
                DestroyUI(player);
                drawui(player);
            }
        }

        [ConsoleCommand("sort")]
        void cmdSort(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null) return;
            var player = arg.Player();
            var category = arg.GetInt(0);
            if (InDuel(player))
            {
                DestroyUI(player);
                return;
            }

            var lootContainer = player.inventory.loot?.containers?.Count > 0 ? player.inventory.loot?.containers[0] : null;
            if (lootContainer == null) return;
            var playerContainer = player.inventory.containerMain;

            if (lootContainer == null || playerContainer == null)
            {
                DestroyUI(player);
                return;
            }

            var inputContainer = _butons.Contains(player.userID) ? playerContainer : lootContainer;
            var outputContainer = inputContainer == lootContainer ? playerContainer : lootContainer;

            GetItemsByCategory(inputContainer, category).ForEach(item => item.MoveToContainer(outputContainer));
        }
        
        #endregion

        #region Config

        private Configuration _config;
        private NamesBTN _names = new NamesBTN();

        private class Configuration
        {
            [JsonProperty(PropertyName = "Настройка названий кнопок", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<NamesBTN> Names = new List<NamesBTN>{new NamesBTN()};

            [JsonProperty(PropertyName = "Заголовок")]
            public string Sorter = "Сортировка";
            
            [JsonProperty(PropertyName = "Цвет заголовка")]
            public string ColorSorter = "#C77335FF";

            [JsonProperty(PropertyName = "Цвет Текста")]
            public string ColorText = "#3A5137FF";
            
            [JsonProperty(PropertyName = "Цвет Кнопки")]
            public string ColorPanel = "#87B13BFF";

            [JsonProperty(PropertyName = "Цвет Активной Кнопки")]
            public string ActiveColorPanel = "#4896CDFF";
        }

        private class NamesBTN
        {
            [JsonProperty(PropertyName = "Take")] public string Take = "Взять";
            [JsonProperty(PropertyName = "Take Size")] public int TakeSize = 10;
            [JsonProperty(PropertyName = "Lay")] public string Lay = "Положить";
            [JsonProperty(PropertyName = "Lay Size")] public int LaySize = 10;
            [JsonProperty(PropertyName = "All")] public string All = "Всё";
            [JsonProperty(PropertyName = "All Size")] public int AllSize = 10;
            [JsonProperty(PropertyName = "Food")] public string Food = "Еда";
            [JsonProperty(PropertyName = "Food Size")] public int FoodSize = 10;
            [JsonProperty(PropertyName = "Weapon")] public string Weapon = "Оружие";
            [JsonProperty(PropertyName = "Weapon Size")] public int WeaponSize = 10;
            [JsonProperty(PropertyName = "Resources")] public string Resources = "Ресурсы";
            [JsonProperty(PropertyName = "Resources Size")] public int ResourcesSize = 10;
            [JsonProperty(PropertyName = "Medicals")] public string Medicals = "Медикаменты";
            [JsonProperty(PropertyName = "Medicals Size")] public int MedicalsSize = 9;
            [JsonProperty(PropertyName = "Attire")] public string Attire = "Одежда";
            [JsonProperty(PropertyName = "Attire Size")] public int AttireSize = 10;
            [JsonProperty(PropertyName = "Components")] public string Components = "Компоненты";
            [JsonProperty(PropertyName = "Components Size")] public int ComponentsSize = 10;
            [JsonProperty(PropertyName = "Tool")] public string Tool = "Инструменты";
            [JsonProperty(PropertyName = "Tool Size")] public int ToolSize = 9;
            [JsonProperty(PropertyName = "Ammunition")] public string Ammunition = "Боеприпасы";
            [JsonProperty(PropertyName = "Ammunition Size")] public int AmmunitionSize = 10;
            [JsonProperty(PropertyName = "Construction")] public string Construction = "Конструкции";
            [JsonProperty(PropertyName = "Construction Size")] public int ConstructionSize = 9;
            [JsonProperty(PropertyName = "Traps")] public string Traps = "Ловушки";
            [JsonProperty(PropertyName = "Traps Size")] public int TrapsSize = 10;
            [JsonProperty(PropertyName = "Items")] public string Items = "Предметы";
            [JsonProperty(PropertyName = "Items Size")] public int ItemsSize = 10;
            [JsonProperty(PropertyName = "Misc")] public string Misc = "Остальное";
            [JsonProperty(PropertyName = "Misc Size")] public int MiscSize = 10;
            
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
            }
            catch (Exception e)
            {
                Puts(e.ToString());
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = new Configuration();
            PrintWarning("Создание нового файла конфигурации...");
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        #endregion
        
        #region functional

        #region Color

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

            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        #endregion

        #region Looting

        void OnLootEntity(BasePlayer player, BaseEntity entry)
        {
            if (player.IsAdmin || permission.UserHasPermission(player.UserIDString, "sorter.use"))
            {
                if (entry is StorageContainer)
                {
                    StorageContainer box = entry as StorageContainer;
                    if (!(box.panelName == "largewoodbox" 
                          || box.panelName == "smallwoodbox"                           
                          || box.panelName == "toolcupboard"
                          || box.panelName == "smallstash"
                          || box.name.Contains("hopperoutput")
                          || box.prefabID == 349880778))
                        return;
                }
                drawui(player);
            }
        }

        void OnPlayerLootEnd(PlayerLoot inventory)
        {
            var player = inventory.GetComponent<BasePlayer>();
            if (player != null)
            {

                DestroyUI(player);
                return;
            }

            if (player.IsAdmin || permission.UserHasPermission(player.UserIDString, "sorter.use"))
                DestroyUI(player);
        }
        
        [PluginReference]
        Plugin Duel;

        bool InDuel(BasePlayer player) => Duel?.Call<bool>("IsPlayerOnActiveDuel", player) ?? false;

        #endregion

        #region Sorting

        List<Item> GetItemsByCategory(ItemContainer container, int category)
        {
            List<ItemCategory> categories = new List<ItemCategory>();
            switch (category)
            {
                case 0:
                    for (int i = 0; i < 15; i++)
                        categories.Add((ItemCategory)i);
                    break;
                case 1:
                    categories.Add(ItemCategory.Food);
                    break;
                case 2:
                    categories.Add(ItemCategory.Weapon);
                    break;
                case 3:
                    categories.Add(ItemCategory.Resources);
                    break;
                case 4:
                    categories.Add(ItemCategory.Medical);
                    break;
                case 5:
                    categories.Add(ItemCategory.Attire);
                    break;
                case 6:
                    categories.Add(ItemCategory.Component);
                    break;
                case 7:
                    categories.Add(ItemCategory.Tool);
                    break;
                case 8:
                    categories.Add(ItemCategory.Ammunition);
                    break;
                case 9:
                    categories.Add(ItemCategory.Construction);
                    break;
                case 10:
                    categories.Add(ItemCategory.Traps);
                    break;
                case 11:
                    categories.Add(ItemCategory.Items);
                    break;
                case 12:
                    categories.Add(ItemCategory.Misc);
                    categories.Add(ItemCategory.Common);
                    categories.Add(ItemCategory.Search);
                    break;
            }
            return container.itemList.Where(item => item != null && categories.Contains(item.info.category)).ToList();
        }
        bool HasAccess(BasePlayer player) => player.IsAdmin;

        #endregion

        #region Hooks

        void OnServerInitialized()
        {
            LoadConfig();
            permission.RegisterPermission("sorter.use", this);
        }

        #endregion

        #endregion

        #region UI

        void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "SorterUI");
        }
        
        void drawui(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "SorterUI");
            var colorpanel = HexToRustFormat(_config.ColorPanel);
            var colortext = HexToRustFormat(_config.ColorText);
            var color = _butons.Contains(player.userID) ? HexToRustFormat(_config.ActiveColorPanel) : colorpanel;
            var color1 = !_butons.Contains(player.userID) ? HexToRustFormat(_config.ActiveColorPanel) : colorpanel;
            var sortergui = new CuiElementContainer();
            var sorterui = sortergui.Add(new CuiPanel
            {
                Image =
                {
                    Color = HexToRustFormat("#4444444A")
                },
                RectTransform =
                {
                    AnchorMin = "0.6463541 0.01388888",
                    AnchorMax = "0.8338541 0.1435185",
                    OffsetMax = "0 0"
                }
            }, "Overlay", "SorterUI");
            sortergui.Add(new CuiElement
            {
                Parent = "SorterUI",
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = _config.Sorter,
                        FontSize = 10,
                        Font = "robotocondensed-bold.ttf",
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat(_config.ColorSorter)
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "-0.005555496 0.7142858",
                        AnchorMax = "0.2777783 1.007143",
                        OffsetMax = "0 0"
                    }
                }
            });
            sortergui.Add(new CuiButton
            {
                Button =
                {
                    Command = "sorter",
                    Color = color
                },
                Text =
                {
                    Text = _names.Lay,
                    Color = colortext,
                    Align = TextAnchor.MiddleCenter,
                    FontSize = _names.LaySize,
                    Font = "robotocondensed-bold.ttf"
                },
                RectTransform =
                {
                    AnchorMin = "0.01925996 0.514763",
                    AnchorMax = "0.2414822 0.7147636",
                    OffsetMax = "0 0"
                }
                
            }, "SorterUI");
            sortergui.Add(new CuiButton
            {
                Button =
                {
                    Command = "sorter",
                    Color = color1
                },
                Text =
                {
                    Text = _names.Take,
                    Color = colortext,
                    Align = TextAnchor.MiddleCenter,
                    FontSize = _names.TakeSize,
                    Font = "robotocondensed-bold.ttf"
                },
                RectTransform =
                {
                    AnchorMin = "0.01925984 0.2790487",
                    AnchorMax = "0.2414821 0.4790483",
                    OffsetMax = "0 0"
                }

            }, "SorterUI");
            
            sortergui.Add(new CuiButton
            {
                Button =
                {
                    Command = "sort 0",
                    Color = colorpanel
                },
                Text =
                {
                    Text = _names.All,
                    Color = colortext,
                    Align = TextAnchor.MiddleCenter,
                    FontSize = _names.AllSize,
                    Font = "robotocondensed-bold.ttf"
                },
                RectTransform =
                {
                    AnchorMin = "0.01925984 0.04333441",
                    AnchorMax = "0.2414821 0.2433344",
                    OffsetMax = "0 0"
                }

            }, "SorterUI");

            sortergui.Add(new CuiButton
            {
                Button =
                {
                    Command = "sort 1",
                    Color = colorpanel
                },
                Text =
                {
                    Text = _names.Food,
                    Color = colortext,
                    Align = TextAnchor.MiddleCenter,
                    FontSize = _names.FoodSize,
                    Font = "robotocondensed-bold.ttf"
                },
                RectTransform =
                {
                    AnchorMin = "0.2638893 0.7500001",
                    AnchorMax = "0.4861118 0.9500002",
                    OffsetMax = "0 0"
                }

            }, "SorterUI");
            
            sortergui.Add(new CuiButton
            {
                Button =
                {
                    Command = "sort 2",
                    Color = colorpanel
                },
                Text =
                {
                    Text = _names.Weapon,
                    Color = colortext,
                    Align = TextAnchor.MiddleCenter,
                    FontSize = _names.WeaponSize,
                    Font = "robotocondensed-bold.ttf"
                },
                RectTransform =
                {
                    AnchorMin = "0.2638893 0.5142863",
                    AnchorMax = "0.4861118 0.7142864",
                    OffsetMax = "0 0"
                }

            }, "SorterUI");
            
            sortergui.Add(new CuiButton
            {
                Button =
                {
                    Command = "sort 3",
                    Color = colorpanel
                },
                Text =
                {
                    Text = _names.Resources,
                    Color = colortext,
                    Align = TextAnchor.MiddleCenter,
                    FontSize = _names.ResourcesSize,
                    Font = "robotocondensed-bold.ttf"
                },
                RectTransform =
                {
                    AnchorMin = "0.2638893 0.2785716",
                    AnchorMax = "0.4861118 0.4785709",
                    OffsetMax = "0 0"
                }

            }, "SorterUI");
            
            sortergui.Add(new CuiButton
            {
                Button =
                {
                    Command = "sort 4",
                    Color = colorpanel
                },
                Text =
                {
                    Text = _names.Medicals,
                    Color = colortext,
                    Align = TextAnchor.MiddleCenter,
                    FontSize = _names.MedicalsSize,
                    Font = "robotocondensed-bold.ttf"
                },
                RectTransform =
                {
                    AnchorMin = "0.2638893 0.04285727",
                    AnchorMax = "0.4861118 0.2428569",
                    OffsetMax = "0 0"
                }

            }, "SorterUI");
            
            sortergui.Add(new CuiButton
            {
                Button =
                {
                    Command = "sort 5",
                    Color = colorpanel
                },
                Text =
                {
                    Text = _names.Attire,
                    Color = colortext,
                    Align = TextAnchor.MiddleCenter,
                    FontSize = _names.AttireSize,
                    Font = "robotocondensed-bold.ttf"
                },
                RectTransform =
                {
                    AnchorMin = "0.5083342 0.7500001",
                    AnchorMax = "0.7305591 0.9500002",
                    OffsetMax = "0 0"
                }

            }, "SorterUI");
            
            sortergui.Add(new CuiButton
            {
                Button =
                {
                    Command = "sort 6",
                    Color = colorpanel
                },
                Text =
                {
                    Text = _names.Components,
                    Color = colortext,
                    Align = TextAnchor.MiddleCenter,
                    FontSize = _names.ComponentsSize,
                    Font = "robotocondensed-bold.ttf"
                },
                RectTransform =
                {
                    AnchorMin = "0.5083342 0.5142863",
                    AnchorMax = "0.7305591 0.7142864",
                    OffsetMax = "0 0"
                }

            }, "SorterUI");
            
            sortergui.Add(new CuiButton
            {
                Button =
                {
                    Command = "sort 7",
                    Color = colorpanel
                },
                Text =
                {
                    Text = _names.Tool,
                    Color = colortext,
                    Align = TextAnchor.MiddleCenter,
                    FontSize = _names.ToolSize,
                    Font = "robotocondensed-bold.ttf"
                },
                RectTransform =
                {
                    AnchorMin = "0.5083342 0.2785716",
                    AnchorMax = "0.7305591 0.4785709"
                }

            }, "SorterUI");
            
            sortergui.Add(new CuiButton
            {
                Button =
                {
                    Command = "sort 8",
                    Color = colorpanel
                },
                Text =
                {
                    Text = _names.Ammunition,
                    Color = colortext,
                    Align = TextAnchor.MiddleCenter,
                    FontSize = _names.AmmunitionSize,
                    Font = "robotocondensed-bold.ttf"
                },
                RectTransform =
                {
                    AnchorMin = "0.5083343 0.04285727",
                    AnchorMax = "0.7305592 0.2428569",
                    OffsetMax = "0 0"
                }

            }, "SorterUI");
            
            sortergui.Add(new CuiButton
            {
                Button =
                {
                    Command = "sort 9",
                    Color = colorpanel
                },
                Text =
                {
                    Text = _names.Construction,
                    Color = colortext,
                    Align = TextAnchor.MiddleCenter,
                    FontSize = _names.ConstructionSize,
                    Font = "robotocondensed-bold.ttf"
                },
                RectTransform =
                {
                    AnchorMin = "0.749999 0.7500001",
                    AnchorMax = "0.9722239 0.9500002",
                    OffsetMax = "0 0"
                }

            }, "SorterUI");
            
            sortergui.Add(new CuiButton
            {
                Button =
                {
                    Command = "sort 10",
                    Color = colorpanel
                },
                Text =
                {
                    Text = _names.Traps,
                    Color = colortext,
                    Align = TextAnchor.MiddleCenter,
                    FontSize = _names.TrapsSize,
                    Font = "robotocondensed-bold.ttf"
                },
                RectTransform =
                {
                    AnchorMin = "0.749999 0.5142863",
                    AnchorMax = "0.9722239 0.7142864",
                    OffsetMax = "0 0"
                }

            }, "SorterUI");
            
            sortergui.Add(new CuiButton
            {
                Button =
                {
                    Command = "sort 11",
                    Color = colorpanel
                },
                Text =
                {
                    Text = _names.Items,
                    Color = colortext,
                    Align = TextAnchor.MiddleCenter,
                    FontSize = _names.ItemsSize,
                    Font = "robotocondensed-bold.ttf"
                },
                RectTransform =
                {
                    AnchorMin = "0.749999 0.2785716",
                    AnchorMax = "0.9722239 0.4785709",
                    OffsetMax = "0 0"
                }

            }, "SorterUI");
            
            sortergui.Add(new CuiButton
            {
                Button =
                {
                    Command = "sort 12",
                    Color = colorpanel
                },
                Text =
                {
                    Text = _names.Misc,
                    Color = colortext,
                    Align = TextAnchor.MiddleCenter,
                    FontSize = _names.MiscSize,
                    Font = "robotocondensed-bold.ttf"
                },
                RectTransform =
                {
                    AnchorMin = "0.749999 0.04285727",
                    AnchorMax = "0.9722239 0.2428569",
                    OffsetMax = "0 0"
                }

            }, "SorterUI");
            CuiHelper.AddUi(player, sortergui);
        }

        #endregion
    }
}