using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("RustMenu", "VooDoo", "1.0.0")]
    [Description("Menu for RUST")]
    public class RustMenu : RustPlugin
    {
        #region API
        [PluginReference] private Plugin ImageLibrary;

        public static RustMenu Instance;

        private Dictionary<string, MenuItem> _menuItems = new Dictionary<string, MenuItem>();
        private Dictionary<string, Dictionary<string, string>> _translations = new Dictionary<string, Dictionary<string, string>>();

        public class MenuItem
        {
            public string menuName { get; private set; }
            private Plugin pluginOwner;
            private string pluginMethod;
            public Dictionary<string, string> menuTranslations;
            public Dictionary<string, MenuItem> subMenuItems;
            
            public void CallRender(Dictionary<string, object> objects)
            {
                if(pluginOwner != null)
                {
                    if (!string.IsNullOrEmpty(pluginMethod))
                    {
                        pluginOwner.Call(pluginMethod, objects);
                    }
                }
            }

            public bool TryGetSubMenu(string name, out MenuItem subMenuItem)
            {
                MenuItem menuItem;
                if(subMenuItems != null && subMenuItems.TryGetValue(name, out menuItem))
                {
                    subMenuItem = menuItem;
                    return true;
                }
                subMenuItem = null;
                return false;
            }

            public static void RegisterMenu(string menuItemName, string pluginName, string pluginMethod, Dictionary<string, string> menuTranslations)
            {
                Instance._menuItems[menuItemName] = new MenuItem()
                {
                    menuName = menuItemName,
                    pluginOwner = Instance.Manager.GetPlugin(pluginName),
                    pluginMethod = pluginMethod,
                    menuTranslations = menuTranslations,
                    subMenuItems = new Dictionary<string, MenuItem>()
                };
            }

            public void RegisterSubMenu(string menuItemName, string pluginName, string pluginMethod, Dictionary<string,string> menuTranslations)
            {
                subMenuItems[menuItemName] = new MenuItem()
                {
                    menuName = menuItemName,
                    pluginOwner = Instance.Manager.GetPlugin(pluginName),
                    pluginMethod = pluginMethod,
                    menuTranslations = menuTranslations,
                    subMenuItems = null
                };
            }

            public string GetTranslated(string langCode = "en")
            {
                string msg = string.Empty;
                if (menuTranslations.TryGetValue(langCode, out msg))
                    return msg;
                return menuTranslations["en"];
            }
        }

        private void API_RegisterMenu(string menuItemName, string pluginName, string pluginMethod, Dictionary<string, string> menuTranslations)
        {
            var plugin = Manager.GetPlugin(pluginName);
            if(plugin != null)
            {
                MenuItem.RegisterMenu(menuItemName, pluginName, pluginMethod, menuTranslations);
            }
        }

        private void API_RegisterSubMenu(string menuItemName, string pluginName, string pluginMethod, Dictionary<string, string> menuTranslations, string parentName)
        {
            var plugin = Manager.GetPlugin(pluginName);
            var menuItem = _menuItems.ContainsKey(parentName) ? _menuItems[parentName] : null;
            if (plugin != null && menuItem != null)
            {
                menuItem.RegisterSubMenu(menuItemName, pluginName, pluginMethod, menuTranslations);
            }
        }

        private bool API_MenuExist(string menuItemName, string subMenuItemName)
        {
            MenuItem menuItem;
            if(_menuItems.TryGetValue(menuItemName, out menuItem))
            {
                MenuItem subMenuItem;
                if(menuItem.subMenuItems.TryGetValue(subMenuItemName, out subMenuItem))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region DefaultUI
        private CuiElementContainer CreateBackground(bool withSubMenu)
        {
            CuiElementContainer Container = new CuiElementContainer();

            Container.Add(new CuiElement
            {
                Name = "RustMenu",
                Parent = "Overlay",
                Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0 0 0 0",
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1",
                        },
                    }
            });

            if (withSubMenu)
            {
                Container.Add(new CuiElement
                {
                    Name = "RustMenu" + ".SubMenu",
                    Parent = "RustMenu",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#D84525F0"),
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 1",
                            OffsetMin = "350 0",
                            OffsetMax = "600 0"
                        },
                    }
                });
            }

            Container.Add(new CuiElement
            {
                Name = "RustMenu" + ".Content",
                Parent = "RustMenu",
                Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#3A3A3AFA"),
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1",
                            OffsetMin = $"{(withSubMenu ? 600 : 350)} 0",
                            OffsetMax = "-65 0"
                        },
                    }
            });

            Container.Add(new CuiElement
            {
                Name = "RustMenu" + "BackgroundRadial",
                Parent = "RustMenu",
                Components =
                    {
                        new CuiRawImageComponent
                        {
                            Color = HexToRustFormat("#000000FF"),
                            Sprite = "assets/content/ui/ui.background.transparent.radial.psd",
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1",
                        },
                        new CuiNeedsCursorComponent() {}
                    }
            });

            Container.Add(new CuiElement
            {
                Name = "RustMenu" + ".BackgroundColor",
                Parent = "RustMenu",
                Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#000000BA"),
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1",
                        },
                    }
            });

            Container.Add(new CuiElement
            {
                Name = "RustMenu" + ".Background",
                Parent = "RustMenu",
                Components =
                    {
                        new CuiRawImageComponent
                        {
                            Color = HexToRustFormat("#FFFFFF80"),
                            Sprite = "assets/content/textures/generic/background/background.bmp"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1",
                        },
                    }
            });
            return Container;
        }

        private void CreateAnchors(CuiElementContainer Container, bool withSubMenu)
        {
            Container.Add(new CuiElement
            {
                Name = "RustMenu" + ".Content" + ".Left",
                Parent = "RustMenu",
                Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0 0 0 0",
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 1",
                            OffsetMin = $"0 0",
                            OffsetMax = "350 0"
                        },
                    }
            });

            Container.Add(new CuiElement
            {
                Name = "RustMenu" + ".Content" + ".Left" + ".Logo",
                Parent = "RustMenu" + ".Content" + ".Left",
                Components =
                    {
                        new CuiRawImageComponent
                        {
                            Color = "0.90 0.90 0.90 1",
                            Sprite = "assets/content/ui/menuui/rustlogo-blurred.png",
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "1 1",
                            OffsetMin = "55 -145",
                            OffsetMax = "-30 -80"
                        },
                    }
            });

            Container.Add(new CuiElement
            {
                Name = "RustMenu" + ".Content" + ".Left" + ".Logo",
                Parent = "RustMenu" + ".Content" + ".Left",
                Components =
                    {
                        new CuiButtonComponent
                        {
                            Color = "0 0 0 0",
                            Command = "rustmenu.show MENUMAIN SUBMENUINFO"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "1 1",
                            OffsetMin = "55 -145",
                            OffsetMax = "-30 -80"
                        },
                    }
            });

            if (withSubMenu)
            {
                Container.Add(new CuiElement
                {
                    Name = "RustMenu" + ".Content" + ".Middle",
                    Parent = "RustMenu",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0 0 0 0",
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 1",
                            OffsetMin = $"350 0",
                            OffsetMax = $"600 0"
                        },
                    }
                });
            }

            Container.Add(new CuiElement
            {
                Name = "RustMenu" + ".Content" + ".Right",
                Parent = "RustMenu",
                Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0 0 0 0",
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1",
                            OffsetMin = $"{(withSubMenu ? 600 : 350)} 0",
                            OffsetMax = "-65 0"
                        },
                    }
            });
        }

        private void CreateMenuItems(BasePlayer player, CuiElementContainer Container, string selectedName)
        {
            Container.Add(new CuiButton
            {
                RectTransform =
                    {
                        AnchorMin = $"0.075 {0.92}",
                        AnchorMax = $"1 {0.99}",
                        OffsetMax = "0 0"
                    },
                Button =
                    {
                        Color = "0 0 0 0",
                        Command = $"rustmenu.show {selectedName} SERVICENAME",
                    },
                Text =
                    {
                        Text = _menuItems[selectedName].GetTranslated(lang.GetLanguage(player.UserIDString)),
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 28,
                        Color = "0.91 0.86 0.82 1"
                    },
            }, "RustMenu" + ".Content" + ".Middle");

            for (int i = 0; i < _menuItems.Count; i++)
            {
                Container.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = $"0.23 {0.68 - i * 0.07}",
                        AnchorMax = $"0.9 {0.75 - i * 0.07}"
                    },
                    Button =
                    {
                        Color = "0 0 0 0",
                        Command = $"rustmenu.show {_menuItems.ElementAt(i).Value.menuName} {(_menuItems.ElementAt(i).Value.subMenuItems != null && _menuItems.ElementAt(i).Value.subMenuItems.Count > 0 ? _menuItems.ElementAt(i).Value.subMenuItems.ElementAt(0).Key : "SERVICENAME")}",
                    },
                    Text =
                    {
                        Text = _menuItems.ElementAt(i).Value.GetTranslated(lang.GetLanguage(player.UserIDString)),
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 28,
                        Color = (selectedName == _menuItems.ElementAt(i).Value.menuName ? "0.83 0.79 0.76 1" : "0.43 0.41 0.38 1")
                    },
                }, "RustMenu" + ".Content" + ".Left");
            }        
        }

        private void CreateSubMenuItems(BasePlayer player, CuiElementContainer Container, string menuItemName, string selectedName)
        {
            var subMenuItems = _menuItems[menuItemName].subMenuItems;
            double yMarginMin = 0.63;
            double yMarginMax = 0.70;
            if (subMenuItems.Count > 7)
            {
                yMarginMin = 0.78;
                yMarginMax = 0.85;
            }
            for (int i = 0; i < subMenuItems.Count; i++)
            {
                Container.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = $"0.075 {yMarginMin - i * 0.07}",
                        AnchorMax = $"1 {yMarginMax - i * 0.07}",
                        OffsetMax = "0 0"
                    },
                    Button =
                    {
                        Color = (selectedName == subMenuItems.ElementAt(i).Value.menuName ? "0 0 0 0.7" : "0 0 0 0"),
                        Command = $"rustmenu.show {menuItemName} {subMenuItems.ElementAt(i).Value.menuName}",
                    },
                    Text =
                    {
                        Text = subMenuItems.ElementAt(i).Value.GetTranslated(lang.GetLanguage(player.UserIDString)) + "    ",
                        Align = TextAnchor.MiddleRight,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 20,
                        Color = "0.91 0.86 0.82 1"
                    },
                }, "RustMenu" + ".Content" + ".Middle");
            }
        }

        private void CreateRightTabPanel(BasePlayer player, CuiElementContainer Container, string menuItemName, string subMenuItemName)
        {
            Container.Add(new CuiElement
            {
                Parent = "RustMenu" + ".Content" + ".Right",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "1 1 1 0.6",
                        Sprite = "assets/icons/close.png"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "1 1",
                        AnchorMax = "1 1",
                        OffsetMin = "15 -50",
                        OffsetMax = "50 -15"
                    },
                }
            });
            Container.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = "1 1",
                    AnchorMax = "1 1",
                    OffsetMin = "10 -55",
                    OffsetMax = "55 -10"
                },
                Button =
                {
                    Color = "0 0 0 0",
                    Command = "rustmenu.close",
                },
                Text =
                {
                    Text = "",
                },
            }, "RustMenu" + ".Content" + ".Right");
            Container.Add(new CuiElement
            {
                Parent = "RustMenu" + ".Content" + ".Right",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Color = "1 1 1 1",
                        Png = (string)ImageLibrary.Call("GetImage", "ruLang")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "1 1",
                        AnchorMax = "1 1",
                        OffsetMin = "10 -105",
                        OffsetMax = "55 -65"
                    },
                }
            });
            Container.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = "1 1",
                    AnchorMax = "1 1",
                    OffsetMin = "10 -110",
                    OffsetMax = "55 -60"
                },
                Button =
                {
                    Color = "0 0 0 0",
                    Command = $"rustmenu.changelang ru {menuItemName} {subMenuItemName}",
                },
                Text =
                {
                    Text = "",
                },
            }, "RustMenu" + ".Content" + ".Right");
            Container.Add(new CuiElement
            {
                Parent = "RustMenu" + ".Content" + ".Right",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Color = "1 1 1 1",
                        Png = (string)ImageLibrary.Call("GetImage", "enLang")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "1 1",
                        AnchorMax = "1 1",
                        OffsetMin = "10 -155",
                        OffsetMax = "55 -115"
                    },
                }
            });
            Container.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = "1 1",
                    AnchorMax = "1 1",
                    OffsetMin = "10 -160",
                    OffsetMax = "55 -110"
                },
                Button =
                {
                    Color = "0 0 0 0",
                    Command = $"rustmenu.changelang en {menuItemName} {subMenuItemName}",
                },
                Text =
                {
                    Text = "",
                },
            }, "RustMenu" + ".Content" + ".Right");
        }

        #endregion

        #region U'mod hooks
        private void OnServerInitialized()
        {
            Instance = this;
 
            API_RegisterMenu("MENUMAIN", this.Name, "", new Dictionary<string, string>()
            {
                ["ru"] = "ГЛАВНАЯ",
                ["en"] = "MAIN"
            });

            API_RegisterSubMenu("SUBMENUINFO", this.Name, "RenderServerInfo", new Dictionary<string, string>()
            {
                ["ru"] = "О СЕРВЕРЕ",
                ["en"] = "ABOUT SERVER",
            }, "MENUMAIN");

            API_RegisterSubMenu("SUBMENURULES", this.Name, "RenderRules", new Dictionary<string, string>()
            {
                ["ru"] = "ПРАВИЛА",
                ["en"] = "RULES"
            }, "MENUMAIN");

            API_RegisterSubMenu("SUBMENUCOMMANDS", this.Name, "RenderCommands", new Dictionary<string, string>()
            {
                ["ru"] = "КОМАНДЫ И БИНДЫ",
                ["en"] = "COMMANDS AND BINDS"
            }, "MENUMAIN");

            ImageLibrary.Call("AddImage", "http://i.imgur.com/ziFZoP6.png", "enLang", 0UL, null);
            ImageLibrary.Call("AddImage", "http://i.imgur.com/TnS324W.png", "ruLang", 0UL, null);

            cmd.AddChatCommand("info", this, (p, cmd, args) => rust.RunClientCommand(p, "rustmenu.open MENUMAIN SUBMENUINFO"));
            cmd.AddChatCommand("rules", this, (p, cmd, args) => rust.RunClientCommand(p, "rustmenu.open MENUMAIN SUBMENURULES"));
            cmd.AddChatCommand("commands", this, (p, cmd, args) => rust.RunClientCommand(p, "rustmenu.open MENUMAIN SUBMENUCOMMANDS"));
            cmd.AddChatCommand("report", this, (p, cmd, args) => rust.RunClientCommand(p, "rustmenu.open MENUREPORTS SERVICENAME"));

            cmd.AddChatCommand("stats", this, (p, cmd, args) => rust.RunClientCommand(p, "rustmenu.open MENUSTATS SUBMENUPLAYERKILLS"));

            cmd.AddChatCommand("block", this, (p, cmd, args) => rust.RunClientCommand(p, "rustmenu.open MENUWIPEBLOCK SUBMENUWEAPON"));
            cmd.AddChatCommand("wipeblock", this, (p, cmd, args) => rust.RunClientCommand(p, "rustmenu.open MENUWIPEBLOCK SUBMENUWEAPON"));

            cmd.AddChatCommand("kit", this, (p, cmd, args) => rust.RunClientCommand(p, "rustmenu.open MENUSTORES MENUKITS"));
            cmd.AddChatCommand("kits", this, (p, cmd, args) => rust.RunClientCommand(p, "rustmenu.open MENUSTORES MENUKITS"));

            cmd.AddChatCommand("case", this, (p, cmd, args) => rust.RunClientCommand(p, "rustmenu.open MENUSTORES SUBMENUCASES"));

            cmd.AddChatCommand("store", this, (p, cmd, args) => rust.RunClientCommand(p, "rustmenu.open MENUSTORES SUBMENUSTORE"));

            cmd.AddChatCommand("map", this, (p, cmd, args) => rust.RunClientCommand(p, "rustmenu.open MENUMAP SERVICENAME"));

        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["INFO"] = $"<color=#fff9f9AA>Welcome to <color=#FFAA00AA>[RUSTLAND]</color>\n\nIn this menu you can read rules, information, commands, binds and other\nYou should read the rules\n\nIP Address: <color=#FFAA00AA>{ConVar.Server.ip}:{ConVar.Server.port}</color>\nSite: <color=#FFAA00AA>http://rustland.store</color>\nGroup VK: <color=#FFAA00AA>http://vk.com/rustland</color></color>",
                ["RULESTITLE"] = "Rules | Page №{%}",
                ["RULES_0"] = "<b>Game process:</b>\n1. The administration will be present on the server and control the gameplay." +
                                "\n2. Players are prohibited from joining teams of more than two people." +
                                "\n3. Players are forbidden to build houses in textures, as well as build up game locations (except for quarries) and build buildings that have no purpose, these buildings will be deleted." +
                                "\n4. Players are prohibited from using bugs, macros, cheats and various flaws in the game and the server." +
                                "\n5. It is forbidden to use nicknames that offend other players." +
                                "\n6. It is forbidden to put the nickname and avatar of another player in order to deceive." +
                                "\n7. The administration is not responsible for exchanges on the server." +
                                "\n8. If you notice players playing in 3 or more, please make a screen (better video) and provide us." +
                                "\n9. It is forbidden to hide loot under water.\n\n" +
                                "<b>Chat:</b>\nFlood, caps, bad words, insulting players, racism, advertising of third-party resources, selling for real money are prohibited.\n\n" +
                                "<b>Additional Information:</b>\n1. Administrators at any time have the right to check your PC for cheats. Refusal to check is punishable by a ban." +
                                "\n2. Items bought in the store cannot be returned or exchanged, after the wipe they are not returned, also if you accidentally lost them." +
                                "\n3. You can be blocked on the game server (ban), without explanation." +
                                "\n4. The game server can be stopped, rebooted or removed at any time." +
                                "\n5. By playing on our servers, you automatically agree with the rules of the server, also with changes in the rules." +
                                "\n6. Refusal to check - a permanent ban." +
                                "\n<b>More information in the group https://vk.com/@rustland-pravila</b>",
                ["COMMANDS"] = "<color=#90BD47>Command</color>",
                ["COMMANDS_1"] = "<color=#90BD47>" +
                                "/sethome <Name>" +
                                "\n/removehome <Name>" +
                                "\n/home <Name>" +
                                "\n/tpr <Nickname/SteamID>" +
                                "\n/tpa" +
                                "\n/tpc" +
                                "\n/ff" +
                                "\n/trade" +
                                "\n/trade accept" +
                                "\n/stats" +
                                "\n/help" +
                                "\n/case" +
                                "\n/kit" +
                                "\n/pm <Nickname/SteamID>" +
                                "\n/up <1-4>" +
                                "\n/remove" +
                                "\n/craft" +
                                "\n/report" +
                                "\n/vk" +
                                "\n/ignore add <Nickname>" +
                                "\n/ignore remove <Nickname>" +
                                "</color><color=#90BD47>" +
                                "\nbind k kill" +
                                "\nbind z 'chat.say /remove'" +
                                "\nbind x 'chat.say /up'" +
                                "\nbind c 'chat.say /kit'" +
                                "\nbind o custommenu" +
                                "\nbind o menu" +
                                "\nPress MMB, with hammer" +
                                "\nPress MMB, with building plan</color>",
                ["DESCRIPTION"] = "<color=#fff9f9AA>Description</color>",
                ["DESCRIPTION_1"] = "<color=#fff9f9AA>" +
                                "Set point of home" +
                                "\nRemove point of home" +
                                "\nTeleport to home" +
                                "\nTeleport request" +
                                "\nAccept teleport request" +
                                "\nCancel teleport" +
                                "\nOn/Off friendly fire" +
                                "\nSend trade request" +
                                "\nAccept trade request" +
                                "\nOpen stats" +
                                "\nOpen information" +
                                "\nOpen cases" +
                                "\nOpen kits" +
                                "\nSend PM" +
                                "\nAuto upgrade buildings" +
                                "\nEnable remove" +
                                "\nOpen custom craft menu" +
                                "\nOpen report menu" +
                                "\nOpen VK menu" +
                                "\nAdd player in blacklist" +
                                "\nRemove player from blacklist" +
                                "\nSuicide" +
                                "\nOn/Off Remove" +
                                "\nOn/Off Auto upgrade" +
                                "\nOpen kits menu" +
                                "\nOpen this menu" +
                                "\nOpen fast menu" +
                                "\nOn/Off Remove" +
                                "\nOn/Off Auto upgrade</color>",
            }, this, "en");
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["INFO"] = $"<color=#fff9f9AA>Добро пожаловать на сервер <color=#FFAA00AA>[RUSTLAND]</color>\n\nОзнакомиться с сервером, узнать команды и особенности вы сможете в данном меню\nОбязательно прочитайте правила сервера\n\nIP Адрес: <color=#FFAA00AA>{ConVar.Server.ip}:{ConVar.Server.port}</color>\nНаш сайт: <color=#FFAA00AA>http://rustland.store</color>\nНаша группа ВК: <color=#FFAA00AA>http://vk.com/rustland</color></color>",
                ["RULESTITLE"] = "Правила | Страница №{%}",
                ["RULES_0"] = "<b>Игровой процесс:</b>\n1. На сервере будет присутствовать администрация и контролировать игровой процесс." +
                                "\n2. Игрокам запрещено объединяться в команды состоящие более чем из двух человек." +
                                "\n3. Игрокам запрещено строить дома в текстурах, а также застраивать игровые локации (кроме карьеров) и строить постройки, не имеющие назначения, данные постройки будут удалены." +
                                "\n4. Игрокам запрещено использование багов, макросов, читов и различных недоработок игры и сервера." +
                                "\n5. Запрещено использование ников оскорбляющих других игроков." +
                                "\n6. Запрещено ставить ник и аватар чужого игрока в целях обмана." +
                                "\n8. Администрация не несет ответственности за обмены на сервере." +
                                "\n8. Если вы заметили игроков играющих в 3 - ём и больше, будьте добры сделать скриншот (лучше видео) и предоставить нам. Нет скриншота - нет доказательств, на слово никому не верим." +
                                "\n9. Запрещено прятать лут под водой.\n\n" +
                                "<b>Чат:</b>\nЗапрещен флуд, капс, маты (в том числе завуалированные), оскорбление игроков, расизм, реклама сторонних ресурсов, продажа игровых вещей за реальные деньги\n\n" +
                                "<b>Дополнительная информация:</b>\n1. Администраторы в любой момент имеют право проверить Ваш ПК на наличие читов. Отказ от проверки карается баном." +
                                "\n2. Вещи, купленные в магазине, не подлежат возврату или обмену, после вайпа не возвращаются, так же, если вы их случайно потеряли." +
                                "\n3. Вы можете быть заблокированы на игровом сервере (бан), без объяснения причин." +
                                "\n4. Игровой сервер может быть остановлен, перезагружен или удален в любое время." +
                                "\n5. Играя на наших серверах, Вы автоматически соглашаетесь с правилами сервера, также с изменениями в правилах." +
                                "\n6. Отказ от проверки - перманентный бан." +
                                "\n<b>Более подробная информация в группе https://vk.com/@rustland-pravila</b>",
                ["COMMANDS"] = "<color=#90BD47>Команда</color>",
                ["COMMANDS_1"] = "<color=#90BD47>" +
                                "/sethome <Название>" +
                                "\n/removehome <Название>" +
                                "\n/home <Название>" +
                                "\n/tpr <Ник/SteamID>" +
                                "\n/tpa" +
                                "\n/tpc" +
                                "\n/ff" +
                                "\n/trade" +
                                "\n/trade accept" +
                                "\n/stats" +
                                "\n/help" +
                                "\n/case" +
                                "\n/kit" +
                                "\n/pm <Ник/SteamID>" +
                                "\n/up <1-4>" +
                                "\n/remove" +
                                "\n/craft" +
                                "\n/report" +
                                "\n/vk" +
                                "\n/ignore add <Ник>" +
                                "\n/ignore remove <Ник>" +
                                "</color><color=#90BD47>" +
                                "\nbind k kill" +
                                "\nbind z 'chat.say /remove'" +
                                "\nbind x 'chat.say /up'" +
                                "\nbind c 'chat.say /kit'" +
                                "\nbind o custommenu" +
                                "\nbind o menu" +
                                "\nНажать СКМ, с киянкой в руках" +
                                "\nНажать СКМ, с планом постройки</color>",
                ["DESCRIPTION"] = "<color=#fff9f9AA>Описание</color>",
                ["DESCRIPTION_1"] = "<color=#fff9f9AA>" +
                                "Поставить точку дома" +
                                "\nУдалить точку дома" +
                                "\nТелепортироваться домой" +
                                "\nЗапрос на телепортацию" +
                                "\nПринять запрос на телепортацию" +
                                "\nОтмена телепортации" +
                                "\nВключить/выключить огонь по друзьям" +
                                "\nОтправить запрос на обмен" +
                                "\nПринять запрос на обмен" +
                                "\nОткрыть статистику" +
                                "\nОткрыть информацию" +
                                "\nОткрыть кейсы" +
                                "\nОткрыть меню наборов" +
                                "\nОтправить личное сообщение" +
                                "\nАвтоулучшение построек" +
                                "\nВключить режим удаления своих построек" +
                                "\nОткрыть дополнительное меню крафта" +
                                "\nОткрыть меню репортов на игроков" +
                                "\nОткрыть меню привязки аккаунта ВК" +
                                "\nДобавить игрока в черный список" +
                                "\nУдалить игрока из черного списка" +
                                "\nСамоубийство" +
                                "\nВключить/Выключить удаление" +
                                "\nПереключить режим автоулучшения" +
                                "\nОткрыть меню наборов" +
                                "\nОткрыть это меню" +
                                "\nОткрыть меню быстрого доступа" +
                                "\nВключить/Выключить удаление" +
                                "\nПереключить режим автоулучшения</color>",
            }, this, "ru");
        }
        #endregion

        #region Menu Items
        private void RenderServerInfo(Dictionary<string, object> args)
        {
            BasePlayer player = (BasePlayer)args["player"];
            CuiElementContainer Container = (CuiElementContainer)args["container"];
            string menuName = (string)args["menuName"];
            string subMenuName = (string)args["subMenuName"];

            Container.Add(new CuiElement
            {
                Name = "RustMenu" + ".Content" + ".Right" + ".Logo",
                Parent = "RustMenu" + ".Content" + ".Right",
                Components =
                    {
                        new CuiRawImageComponent
                        {
                            FadeIn = 1f,
                            Color = "1 1 1 0.6",
                            Sprite = "assets/content/ui/menuui/rustlogo-blurred.png",
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-200 100",
                            OffsetMax = "200 200"
                        },
                    }
            });

            Container.Add(new CuiElement
            {
                Name = "RustMenu" + ".Content" + ".Right" + ".Text",
                Parent = "RustMenu" + ".Content" + ".Right",
                Components =
                        {
                            new CuiTextComponent
                            {
                                FadeIn = 1f,
                                Text = lang.GetMessage("INFO", this, player.UserIDString),
                                Align = TextAnchor.UpperCenter,
                                FontSize = 24,
                                Font = "robotocondensed-regular.ttf",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.5 0.5",
                                AnchorMax = "0.5 0.5",
                                OffsetMin = "-250 -200",
                                OffsetMax = "250 75"
                            },
                            new CuiOutlineComponent
                            {
                                 Color = "0 0 0 0.2", 
                                Distance = "-0.1 0.1"
                            }
                        }
            });
        }

        private void RenderRules(Dictionary<string, object> args)
        {
            BasePlayer player = (BasePlayer)args["player"];
            CuiElementContainer Container = (CuiElementContainer)args["container"];
            string menuName = (string)args["menuName"];
            string subMenuName = (string)args["subMenuName"];
            int Page = 0;
            if(args.ContainsKey("arg_1"))
                Page = int.Parse((string)args["arg_1"]);

            Container.Add(new CuiElement
            {
                Name = "RustMenu" + ".Content" + ".Right" + ".Title",
                Parent = "RustMenu" + ".Content" + ".Right",
                Components =
                        {
                            new CuiTextComponent
                            {
                                FadeIn = 1f,
                                Text = lang.GetMessage("RULESTITLE", this, player.UserIDString).Replace("{%}", $"{Page + 1}"),
                                Align = TextAnchor.MiddleCenter,
                                FontSize = 24,
                                Font = "robotocondensed-bold.ttf",
                                Color = "1 1 1 0.8"
                            },
                            new CuiRectTransformComponent
                            {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-200 225",
                            OffsetMax = "200 275"
                            }
                        }
            });

            var messages = lang.GetMessages("ru", this).Where(x => x.Key.StartsWith("RULES_")).ToList();
            string ContentText = string.Empty;

            Container.Add(new CuiElement
            {
                Name = "RustMenu" + ".Content" + ".Right" + ".Text",
                Parent = "RustMenu" + ".Content" + ".Right",
                Components =
                        {
                            new CuiTextComponent
                            {
                                FadeIn = 1f,
                                Text = lang.GetMessage($"RULES_{Page}", this, player.UserIDString),
                                Align = TextAnchor.UpperLeft,
                                FontSize = 14,
                                Font = "robotocondensed-regular.ttf",
                                Color = "1 1 1 0.6"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.5 0.5",
                                AnchorMax = "0.5 0.5",
                                OffsetMin = "-275 -250",
                                OffsetMax = "275 220"
                            }
                        }
            });

            if (Page > 0)
            {
                Container.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-275 -300",
                        OffsetMax = "-225 -250"
                    },
                    Button =
                    {
                        FadeIn = 1f,
                        Color = "1 1 1 0",
                        Command = $"rustmenu.show {menuName} {subMenuName} {Page-1}",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"
                    },
                    Text =
                    {
                        Text = "<",
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 24
                    },
                }, "RustMenu" + ".Content" + ".Right");
            }

            if (Page < messages.Count() - 1)
            {
                Container.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "225 -300",
                        OffsetMax = "275 -250"
                    },
                    Button =
                    {
                        FadeIn = 1f,
                        Color = "1 1 1 0",
                        Command = $"rustmenu.show {menuName} {subMenuName} {Page+1}",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"
                    },
                    Text =
                        {
                        Text = ">",
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 24
                    },
                }, "RustMenu" + ".Content" + ".Right");
            }
        }

        private void RenderCommands(Dictionary<string, object> args)
        {
            BasePlayer player = (BasePlayer)args["player"];
            CuiElementContainer Container = (CuiElementContainer)args["container"];
            string menuName = (string)args["menuName"];
            string subMenuName = (string)args["subMenuName"];

            Container.Add(new CuiElement
            {
                Name = "RustMenu" + ".Content" + ".Right" + ".Text",
                Parent = "RustMenu" + ".Content" + ".Right",
                Components =
                        {
                            new CuiTextComponent
                            {
                                FadeIn = 1f,
                                Text = lang.GetMessage("COMMANDS", this, player.UserIDString),
                                Align = TextAnchor.MiddleLeft,
                                FontSize = 24,
                                Font = "robotocondensed-bold.ttf",
                                Color = "1 1 1 0.6"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.5 0.5",
                                AnchorMax = "0.5 0.5",
                                OffsetMin = "-275 225",
                                OffsetMax = "-100 275"
                            }
                        }
            });

            Container.Add(new CuiElement
            {
                Name = "RustMenu" + ".Content" + ".Right" + ".Text",
                Parent = "RustMenu" + ".Content" + ".Right",
                Components =
                        {
                            new CuiTextComponent
                            {
                                FadeIn = 1f,
                                Text = lang.GetMessage("DESCRIPTION", this, player.UserIDString),
                                Align = TextAnchor.MiddleRight,
                                FontSize = 24,
                                Font = "robotocondensed-bold.ttf",
                                Color = "1 1 1 0.6"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.5 0.5",
                                AnchorMax = "0.5 0.5",
                                OffsetMin = "100 225",
                                OffsetMax = "275 275"
                            }
                        }
            });

            Container.Add(new CuiElement
            {
                Name = "RustMenu" + ".Content" + ".Right" + ".Text",
                Parent = "RustMenu" + ".Content" + ".Right",
                Components =
                        {
                            new CuiTextComponent
                            {
                                FadeIn = 1f,
                                Text = lang.GetMessage("COMMANDS_1", this, player.UserIDString),
                                Align = TextAnchor.UpperLeft,
                                FontSize = 14,
                                Font = "robotocondensed-regular.ttf",
                                Color = "1 1 1 0.6"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.5 0.5",
                                AnchorMax = "0.5 0.5",
                                OffsetMin = "-275 -250",
                                OffsetMax = "275 220"
                            }
                        }
            });

            Container.Add(new CuiElement
            {
                Name = "RustMenu" + ".Content" + ".Right" + ".Text",
                Parent = "RustMenu" + ".Content" + ".Right",
                Components =
                        {
                            new CuiTextComponent
                            {
                                FadeIn = 1f,
                                Text = lang.GetMessage("DESCRIPTION_1", this, player.UserIDString),
                                Align = TextAnchor.UpperRight,
                                FontSize = 14,
                                Font = "robotocondensed-regular.ttf",
                                Color = "1 1 1 0.6"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.5 0.5",
                                AnchorMax = "0.5 0.5",
                                OffsetMin = "-275 -250",
                                OffsetMax = "275 220"
                            }
                        }
            });
        }
        #endregion
        
        #region Commands Handle
        [ConsoleCommand("rustmenu.open")]
        private void RustMenuOpen(ConsoleSystem.Arg arg)
        {
            if (arg.HasArgs(1))
            {
                Dictionary<string, object> Objects = new Dictionary<string, object>();
                CuiElementContainer Container = new CuiElementContainer();

                string menuName = arg.Args[0];
                string subMenuName = "SERVICENAME";
                if (arg.HasArgs(2))
                    subMenuName = arg.Args[1];

                Objects.Add("player", arg.Player());
                Objects.Add("container", Container);
                Objects.Add("menuName", menuName);

                if(arg.HasArgs(2))
                    Objects.Add("subMenuName", subMenuName);

                for(int i = (arg.HasArgs(2) ? 2 : 1); i < arg.Args.Count(); i++)
                    Objects.Add($"arg_{i - 1}", arg.Args[i]);

                MenuItem menuItem;
                if (_menuItems.TryGetValue(menuName, out menuItem))
                {
                    CuiHelper.DestroyUi(arg.Player(), "RustMenu");
                    Container.AddRange(CreateBackground(true));
                    CreateAnchors(Container, true);
                    CreateMenuItems(arg.Player(), Container, menuName);

                    if (menuItem.menuName != "MENUMAP")
                        rust.RunClientCommand(arg.Player(), "map.close");
                    else
                    {
                        Objects.Add("arg_mapName", "open");
                    }

                    MenuItem subMenuItem;
                    if (arg.HasArgs(2) && menuItem.TryGetSubMenu(subMenuName, out subMenuItem))
                    {
                        CreateSubMenuItems(arg.Player(), Container, menuName, subMenuName);
                        CreateRightTabPanel(arg.Player(), Container, menuName, subMenuName);
                        subMenuItem.CallRender(Objects);
                    }
                    else
                    {
                        CreateRightTabPanel(arg.Player(), Container, menuName, subMenuName);
                        menuItem.CallRender(Objects);
                    }
                }

                CuiHelper.AddUi(arg.Player(), Container);
            }
            else
            {
                rust.RunClientCommand(arg.Player(), $"rustmenu.open {_menuItems.ElementAt(0).Value.menuName} {_menuItems.ElementAt(0).Value.subMenuItems.ElementAt(0).Value.menuName}");
            }
        }

        [ConsoleCommand("rustmenu.show")]
        private void RustMenuShow(ConsoleSystem.Arg arg)
        {
            if (arg.HasArgs(1))
            {
                Dictionary<string, object> Objects = new Dictionary<string, object>();
                CuiElementContainer Container = new CuiElementContainer();

                string menuName = arg.Args[0];
                string subMenuName = "SERVICENAME";
                if (arg.HasArgs(2))
                    subMenuName = arg.Args[1];

                Objects.Add("player", arg.Player());
                Objects.Add("container", Container);
                Objects.Add("menuName", menuName);

                if (arg.HasArgs(2))
                    Objects.Add("subMenuName", subMenuName);

                for (int i = (arg.HasArgs(2) ? 2 : 1); i < arg.Args.Count(); i++)
                    Objects.Add($"arg_{i - 1}", arg.Args[i]);

                MenuItem menuItem;
                if (_menuItems.TryGetValue(menuName, out menuItem))
                {
                    CuiHelper.DestroyUi(arg.Player(), "RustMenu" + ".Content" + ".Left");
                    CuiHelper.DestroyUi(arg.Player(), "RustMenu" + ".Content" + ".Middle");
                    CuiHelper.DestroyUi(arg.Player(), "RustMenu" + ".Content" + ".Right");
                    CreateAnchors(Container, true);
                    CreateMenuItems(arg.Player(), Container, menuName);

                    if (menuItem.menuName != "MENUMAP")
                        rust.RunClientCommand(arg.Player(), "map.close");
                    else
                    {
                        Objects.Add("arg_mapName", "show");
                    }

                    MenuItem subMenuItem;
                    if (arg.HasArgs(2) && menuItem.TryGetSubMenu(subMenuName, out subMenuItem))
                    {
                        CreateSubMenuItems(arg.Player(), Container, menuName, subMenuName);
                        CreateRightTabPanel(arg.Player(), Container, menuName, subMenuName);
                        subMenuItem.CallRender(Objects);
                    }
                    else
                    {
                        CreateRightTabPanel(arg.Player(), Container, menuName, subMenuName);
                        menuItem.CallRender(Objects);
                    }
                }

                CuiHelper.AddUi(arg.Player(), Container);
            }
        }

        [ConsoleCommand("rustmenu.close")]
        private void RustMenuClose(ConsoleSystem.Arg arg)
        {
            CuiHelper.DestroyUi(arg.Player(), this.Name);

            rust.RunClientCommand(arg.Player(), "map.close");
        }

        [ConsoleCommand("rustmenu.changelang")]
        private void RustMenuChangeLang(ConsoleSystem.Arg arg)
        {
            if (arg.HasArgs(3))
            {
                string langCode = arg.Args[0];
                string menuItem = arg.Args[1];
                string submenuItem = arg.Args[2];

                rust.RunClientCommand(arg.Player(), $"lang {langCode}");
                rust.RunClientCommand(arg.Player(), $"rustmenu.show {menuItem} {submenuItem}");
            }        
        }

        [ChatCommand("rustmenu")]
        private void RustMenuChatOpen(BasePlayer player, string cmd, string[] args)
        {
            rust.RunClientCommand(player, $"rustmenu.open {_menuItems.ElementAt(0).Value.menuName} {_menuItems.ElementAt(0).Value.subMenuItems.ElementAt(0).Value.menuName}");
        }
        #endregion

        #region Utils
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
    }
}
