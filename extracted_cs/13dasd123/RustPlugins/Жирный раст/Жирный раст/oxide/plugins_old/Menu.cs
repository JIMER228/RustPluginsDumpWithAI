using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Menu", "Mercury", "0.0.2")]
    [Description("Не гэй,а пионер Mercury")]
    class Menu : RustPlugin
    {
        #region Reference
        [PluginReference] Plugin ImageLibrary;
        private string GetImage(string fileName, ulong skin = 0)
        {
            var imageId = (string)plugins.Find("ImageLibrary").CallHook("GetImage", fileName, skin);
            if (!string.IsNullOrEmpty(imageId))
                return imageId;
            return string.Empty;
        }
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);

        #endregion

        #region Vars
        public List<BasePlayer> IsOpenMenu = new List<BasePlayer>();
        public List<string> ActiveEvent = new List<string>();
        #endregion

        #region Configuration

        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Настройки плагина")]
            public SettingsPlugin SettingPlugin = new SettingsPlugin();

            internal class SettingsPlugin
            {
                [JsonProperty("Название сервера в UI")]
                public string ServerName;
                [JsonProperty("Дополнительная строка в UI")]
                public List<string> Description = new List<string>();
                [JsonProperty("Настройка меню")]
                public List<MenuSettings> menuSettings = new List<MenuSettings>();
                [JsonProperty("Настройка иконки в панели")]
                public string PNG;
                [JsonProperty("Ссылка на линию в панели")]
                public string PNGLine;
                [JsonProperty("Интервал обновления дополнительной строки в UI")]
                public int IntervalUpdateDescription;
                [JsonProperty("Интервал обновления информационной панели")]
                public int IntervalUpdateInfoPanel;
                [JsonProperty("Интервал обновления ивентов")]
                public int IntervalUpdateEvents;
                [JsonProperty("Ключ от ipgeolocation.io (Для получения времени игрока, сервис бесплатный)")]
                public string IpGeoAPIKey = "";

                internal class MenuSettings
                {
                    [JsonProperty("Название кнопки")]
                    public string Name;
                    [JsonProperty("Описание кнопки")]
                    public string Description;
                    [JsonProperty("Команда кнопки")]
                    public string Command;
                }
            }

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    SettingPlugin = new SettingsPlugin
                    {
                        IpGeoAPIKey = "",
                        ServerName = "<size=23><b>TRASH <color=#D04425>RUST</color> X20</b> [RPG]:</size>",
                        IntervalUpdateDescription = 5,
                        IntervalUpdateInfoPanel = 5,
                        IntervalUpdateEvents = 5,
                        PNG = "https://i.imgur.com/PHTlu4K.png",
                        PNGLine = "https://i.imgur.com/Ed24JDb.png",
                        Description = new List<string>
                        {
							"<size=9>ДОБРО ПОЖАЛОВАТЬ НА СЕРВЕР ▲ <color=#FFFFFF>TRASH</color> <color=#E74425>RUST</color> [RPG]</size>",
							"<size=9>НАЖМИТЕ СЮДА ЧТОБЫ ОТКРЫТЬ МЕНЮ НАШЕГО СЕРВЕРА</size>",
							"<size=9>ВАЙП КАРТЫ СЕРВЕРА ПРОИЗВОДИЛСЯ ▲ <color=#E74425>2.05.2020г.</color></size>",
							"<size=9>СЛЕДУЮЩИЙ ВАЙП КАРТЫ ОЖИДАЕТСЯ ▲ <color=#E74425>7.05.2020г.</color></size>",
							"<size=9>ПРОСМОТРЕТЬ КАЛЕНДАРЬ ВАЙПОВ ▲ <color=#E74425>/WIPE</color></size>",
							"<size=9>КОЛИЧЕСТВО ДРУЗЕЙ ▲ <color=#E74425>МАКСИМУМ 4 ИГРОКА!</color></size>",
							"<size=9>КОЛИЧЕСТВО СОКЛАНА ▲ <color=#E74425>МАКСИМУМ 4 ИГРОКА!</color></size>",
							"<size=9>ГРУППА ВКОНТАКТЕ ▲ <color=#25AAFF>HTTP://VK.COM/TRASHRUST</color></size>",
							"<size=9>МАГАЗИН СЕРВЕРА ▲ <color=#25AAFF>HTTP://WWW.TRASHRUST.RU/</color></size>",
							"<size=9>РЕСТАРТ СЕРВЕРА ПРОИЗВОДИТСЯ В ▲ <color=#E74425>07:00</color> по МСК</size>",
							"<size=9>НА СЕРВЕРЕ УСТАНОВЛЕНА RPG СИСТЕМА ▲ <color=#E74425>/RPG</color></size>",
							"<size=9>ПОЖАЛОВАТЬСЯ НА ИГРОКА КОМАНДА ▲ <color=#E74425>/REPORT</color></size>",
							"<size=9>КРАФТ СИСТЕМА СЕРВЕРА ▲ <color=#E74425>/CRAFT</color></size>"
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
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning("Ошибка #1" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region UI

        #region Parent
        public static string INTERFACE_PARENT = "INTERFACE_MENU_PARENT";
        public static string INTERFACE_PARENT_PANEL = "INTERFACE_MENU_PARENT_PANEL";
        public static string INTERFACE_PARENT_DROP_MENU = "INTERFACE_MENU_DROP_LIST_PARENT";
        #endregion

        void OnPlayerConnected(BasePlayer player)
        {
            UI_Interface(player);
            Broadcasters(player);
            EventRefresh(player);
            UI_Panel_Interface(player);
        }

        void UI_Interface(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, INTERFACE_PARENT);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "15 -60", OffsetMax = "400 -5" },
                Image = { Color = "0 0 0 0" }
            }, "Overlay", INTERFACE_PARENT);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.496969", AnchorMax = "0.6987013 1" },
                Text = { Text = config.SettingPlugin.ServerName, Font = "robotocondensed-regular.ttf", Align = TextAnchor.UpperLeft }
            },  INTERFACE_PARENT,"LABEL_NAME");

            container.Add(new CuiElement
            {
                Parent = INTERFACE_PARENT,
                Name = "LINE_PANEL",
                Components = {
                        new CuiImageComponent {
                            Png = GetImage("PNG_LINE"),
                        },
                        new CuiRectTransformComponent { AnchorMin = "-0.009 0.25", AnchorMax = "0.7 1.0" }
                    },
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = $"chat.say /menu", Color = "0 0 0 0" },
                Text = { Text = "", Color = "0 0 0 0", Align = TextAnchor.MiddleCenter }
            }, INTERFACE_PARENT);

            CuiHelper.AddUi(player, container);
        }

        void UI_Panel_Interface(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, INTERFACE_PARENT_PANEL);
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-200 -50", OffsetMax = "-3 -3" },
                Image = { Color = "0 0 0 0" }
            }, "Hud", INTERFACE_PARENT_PANEL);

            container.Add(new CuiElement
            {
                Parent = INTERFACE_PARENT_PANEL,
                Name = "Joined",
                Components = {
                        new CuiImageComponent {
                            Png = GetImage("PNG_PANEL"),
                        },
                        new CuiRectTransformComponent { AnchorMin = "0.7700684 0.07777759", AnchorMax = "0.9768711 0.922222" }
                    },
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1.1" },
                Text = { Text = $"<size=9>ОЧЕРЕДЬ</size>\n<size=16>{SingletonComponent<ServerMgr>.Instance.connectionQueue.Joining}</size>", Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter }
            }, "Joined");

            container.Add(new CuiElement
            {
                Parent = INTERFACE_PARENT_PANEL,
                Name = "Sleepers",
                Components = {
                        new CuiImageComponent {
                            Png = GetImage("PNG_PANEL"),
                        },
                        new CuiRectTransformComponent { AnchorMin = "0.5390563 0.07777759", AnchorMax = "0.745859 0.922222" }
                    },
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1.1" },
                Text = { Text = $"<size=9>СПЯЩИЕ</size>\n<size=16>{BasePlayer.sleepingPlayerList.Count.ToString()}</size>", Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter }
            }, "Sleepers");

            container.Add(new CuiElement
            {
                Parent = INTERFACE_PARENT_PANEL,
                Name = "Online",
                Components = {
                        new CuiImageComponent {
                            Png = GetImage("PNG_PANEL"),
                        },
                        new CuiRectTransformComponent { AnchorMin = "0.3074842 0.07777759", AnchorMax = "0.514289 0.922222" }
                    },
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1.1" },
                Text = { Text = $"<size=9>ОНЛАЙН</size>\n<size=16>{BasePlayer.activePlayerList.Count.ToString()}</size>", Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter }
            }, "Online");

            CuiHelper.AddUi(player, container);
        }

        void DropMenuList(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, INTERFACE_PARENT_DROP_MENU);

            container.Add(new CuiPanel
            {
                FadeOut = 0.1f,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = $"0 {-6.5 * config.SettingPlugin.menuSettings.Count}", OffsetMax = "250 0" },
                Image = { FadeIn = 0.1f, Color = "0 0 0 0" }
            },  INTERFACE_PARENT, INTERFACE_PARENT_DROP_MENU);

            for (int i = 0; i < config.SettingPlugin.menuSettings.Count; i++)
            {
                var cfg = config.SettingPlugin.menuSettings[i];

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0 {0.7 - (i * 0.5)}", AnchorMax = $"1 {1 - (i * 0.5)}" },
                    Button = { Command = $"{cfg.Command.Replace("%STEAMID%", player.UserIDString)}", Color = "0 0 0 0" },
                    Text = { Text = cfg.Name, Color = HexToRustFormat("#FFFFFFFF"), Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf" }
                }, INTERFACE_PARENT_DROP_MENU, $"BTN_{i}");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0 0", OffsetMin = "0 -15", OffsetMax = "240 5" },
                    Text = { Text = cfg.Description, Font = "robotocondensed-regular.ttf", Align = TextAnchor.UpperLeft }
                }, $"BTN_{i}");
            }

            CuiHelper.AddUi(player, container);
        }

        void EventRefresh(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, "ICO_PANEL");
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5708813 0.4633332", AnchorMax = "1.1 1.0" },
                Image = { Color = "0 0 0 0" }
            }, INTERFACE_PARENT, "ICO_PANEL");

            int Count = 0;
            foreach (var stateIcon in stateIcons)
            {
                if (stateIcon.Count == 0) continue;

                container.Add(new CuiElement
                {
                    Parent = "ICO_PANEL",
                    Name = $"Element_{Count}",
                    Components = {
                        new CuiImageComponent {
                            Png = GetImage(stateIcon.Name)
                        },
                         new CuiRectTransformComponent { AnchorMin = $"{0 + (Count * 0.18)} 0.24", AnchorMax = $"{0.15 + (Count * 0.18)} 0.84" }

                    },
                });
                Count++;
            }
      
            CuiHelper.AddUi(player, container);
        }
 

        void Broadcasters(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, "Broadcast_Label");
            int RandomIndex = UnityEngine.Random.Range(0, config.SettingPlugin.Description.Count);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.1454542", AnchorMax = "1 0.4727274" },
                Text = { Text = config.SettingPlugin.Description[RandomIndex].Replace("%ONLINE%", BasePlayer.activePlayerList.Count.ToString()), Font = "robotocondensed-regular.ttf", Align = TextAnchor.UpperLeft }
            }, INTERFACE_PARENT, "Broadcast_Label");

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Hooks
        private void OnServerInitialized()
        {
            LoadImage();
            timer.Every(config.SettingPlugin.IntervalUpdateEvents, () =>
            {
                foreach(var player in BasePlayer.activePlayerList)
				    EventRefresh(player);
            });

            timer.Every(config.SettingPlugin.IntervalUpdateDescription, () => 
            {
                foreach(var player in BasePlayer.activePlayerList)
				    Broadcasters(player);
            });
            timer.Every(config.SettingPlugin.IntervalUpdateInfoPanel, () =>
            {
				foreach(var player in BasePlayer.activePlayerList)
				    UI_Panel_Interface(player);
            });
            
			foreach(var player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);
        } 

        void OnEntitySpawned(BaseNetworkable entity)
        {
            foreach (var stateIcon in stateIcons)
            {
                if (!stateIcon.Test(entity)) continue;

                stateIcon.Count++;
            }
        }

        void OnEntityKill(BaseNetworkable entity)
        {
            foreach (var stateIcon in stateIcons)
            {
                if (!stateIcon.Test(entity)) continue;

                if (stateIcon.Count > 0) stateIcon.Count--;
            }
        }

        #endregion

        #region Metods
        private class StateIcon
        {
            public string Url;
            public string Name;
            public Type Type;
            public int Count;
            public Func<BaseNetworkable, bool> Test;

            public StateIcon()
            {
                Count = 0;
            }
        }
        List<StateIcon> stateIcons = new List<StateIcon> {
            new StateIcon {
                Name = "ZenPanelIconCargoPlane",
                Url = "https://i.imgur.com/CL41EJS.png",
                Type = typeof(CargoPlane),
                Test = e => e is CargoPlane
            },
            new StateIcon {
                Name = "ZenPanelIconPatrolHelicopter",
                Url = "https://i.imgur.com/HtAifod.png",
                Type = typeof(BaseHelicopter),
                Test = e => e is BaseHelicopter
            },
            new StateIcon {
                Name = "ZenPanelIconTank",
                Url = "https://i.imgur.com/j5cMDpt.png",
                Type = typeof(BradleyAPC),
                Test = e => e is BradleyAPC
            },
            new StateIcon {
                Name = "ZenPanelIconCH47",
                Url = "https://i.imgur.com/5VE9BAg.png",
                Type = typeof(CH47Helicopter),
                Test = e => e is CH47Helicopter
            },
            new StateIcon {
                Name = "ZenPanelIconCargoShip",
                Url = "https://i.imgur.com/xU8IUWO.png",
                Type = typeof(CargoShip),
                Test = e => e is CargoShip
            }
        };
        #endregion

        #region Commands

        [ConsoleCommand("menu")]
        void ConsoleCommandMenu(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            switch(arg.Args[0])
            {
                case "open":
                    {
                        if(IsOpenMenu.Contains(player))
                        {
                            CuiHelper.DestroyUi(player, INTERFACE_PARENT_DROP_MENU);
                            IsOpenMenu.Remove(player);
                        }
                        else
                        {
                            DropMenuList(player);
                            IsOpenMenu.Add(player);
                        }
                        break;
                    }
            }
        }

        #endregion

        #region HelpMetods
        private static string HexToRustFormat(string hex)
        {
            UnityEngine.Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        void LoadImage()
        {
            foreach (var stateIcon in stateIcons) 
                 AddImage(stateIcon.Url, stateIcon.Name);

            AddImage(config.SettingPlugin.PNG, "PNG_PANEL");
            AddImage(config.SettingPlugin.PNGLine, "PNG_LINE");
        }

        #endregion
    }
}
