// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Core.Plugins;
using System.Text;

namespace Oxide.Plugins
{
    [Info("CustomMenu", "Vlad-00003", "1.1.0")]
    [Description("This plugin allows you to create custom GUI menu with any commands and sub menus")]
    /*
     * Author info:
     *   E-mail: Vlad-00003@mail.ru
     *   Vk: vk.com/vlad_00003
     */
    class CustomMenu : RustPlugin
    {
        #region Vars
        [PluginReference]
        Plugin Friends, NTeleportation;
        private PluginConfig config;
        private string PanelName = "CustomMenuGUI";
        private string Footername = "CustomMenuGUIFoot";
        #endregion

        #region Config
        private class BuildinCommands
        {
            [JsonProperty("CustomMenuTP")]
            public GuiButton FriendTP;
            [JsonProperty("CustomMenuTrade")]
            public GuiButton FriendTrade;
            [JsonProperty("CustomMenuHome")]
            public GuiButton HomeTP;
        }
        private class GuiButton
        {
            [JsonProperty("Команда")]
            public string Command;
            [JsonProperty("Цвет кнопки")]
            public string Color;
            [JsonProperty("Текст на кнопке")]
            public string Text;
            [JsonProperty("Размер текста")]
            public int TextSize;
            [JsonProperty("Цвет текста")]
            public string TextColor;
            [JsonProperty("Шрифт текста")]
            public string TextFont;
        }
        private class GuiButtonSub : GuiButton
        {
            [JsonProperty("Минимальный отступ")]
            public string Amin;
            [JsonProperty("Максимальный отступ")]
            public string Amax;
        }
        private class AutoButtons
        {
            [JsonProperty("Левая граница")]
            public float Left;
            [JsonProperty("Правая граница")]
            public float Right;
        }
        private class GuiPanel
        {
            [JsonProperty("Минимальный отступ")]
            public string Amin;
            [JsonProperty("Максимальный отступ")]
            public string Amax;
            [JsonProperty("Фоновый цвет")]
            public string Color;
        }
        private class PluginConfig
        {
            [JsonProperty("Настройки основной панели")]
            public GuiPanel Main;
            [JsonProperty("Настройки нижней панели")]
            public GuiPanel Footer;
            [JsonProperty("Края кнопок")]
            public AutoButtons Auto;
            [JsonProperty("Кнопка выхода")]
            public GuiButtonSub Exit;
            [JsonProperty("Кнопка возврата в меню")]
            public GuiButtonSub Menu;
            [JsonProperty("Кнопки основной панели")]
            public List<GuiButton> Buttons;
            [JsonProperty("Настройки кнопок для встроенных функций")]
            public BuildinCommands Commands;
            [JsonProperty("Дополнительные меню. Команда + кнопки")]
            public Dictionary<string, List<GuiButton>> Custom;
            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    Main = new GuiPanel()
                    {
                        Amin = "0.344 0.2",
                        Amax = "0.64 0.7",
                        Color = "0.1 0.1 0.1 0.8"
                    },
                    Footer = new GuiPanel()
                    {
                        Amin = "0.344 0.11",
                        Amax = "0.64 0.2",
                        Color = "0.1 0.1 0.1 0.8"
                    },
                    Auto = new AutoButtons()
                    {
                        Left = 0.15f,
                        Right = 0.85f
                    },
                    Exit = new GuiButtonSub()
                    {
                        Command = null,
                        Amin = "0.45 0.2",
                        Amax = "0.85 0.8",
                        Color = "0.50 0.25 0.00 1.00",
                        Text = "Закрыть меню",
                        TextSize = 15,
                        TextColor = "1.00 1.00 1.00 1.00",
                        TextFont = "robotocondensed-regular.ttf"
                    },
                    Menu = new GuiButtonSub()
                    {
                        Command = null,
                        Amin = "0.15 0.2",
                        Amax = "0.40 0.8",
                        Color = "1.00 0.00 1.00 0.2",
                        Text = "На главную",
                        TextSize = 15,
                        TextColor = "1.00 1.00 1.00 1.00",
                        TextFont = "robotocondensed-regular.ttf"
                    },
                    Buttons = new List<GuiButton>()
                   {
                       new GuiButton()
                       {
                           Command = "chat.say /tpa",
                           Color = "0.50 1.00 0.51 1.00",
                           Text = "Принять телепорт",
                           TextSize = 15,
                           TextColor = "0.00 0.00 0.00 1.00",
                           TextFont = "RobotoCondensed-Bold.ttf"
                       },
                       new GuiButton()
                       {
                           Command = "chat.say /tpc",
                           Color = "1.00 0.36 0.25 1.00",
                           Text = "Отменить телепорт",
                           TextSize = 15,
                           TextColor = "0.00 0.00 0.00 1.00",
                           TextFont = "RobotoCondensed-Bold.ttf"
                       },
                       new GuiButton()
                       {
                           Command = "chat.say /kit",
                           Color = "0.50 0.00 0.50 0.67",
                           Text = "Открыть киты",
                           TextSize = 15,
                           TextColor = "1.00 1.00 1.00 1.00",
                           TextFont = "RobotoCondensed-Bold.ttf"
                       },
                       new GuiButton()
                       {
                           Command = "chat.say /trade accept",
                           Color = "0.00 0.85 0.78 1.00",
                           Text = "Приянть обмен",
                           TextSize = 15,
                           TextColor = "0.00 0.00 0.00 1.00",
                           TextFont = "RobotoCondensed-Bold.ttf"
                       },
                        new GuiButton()
                       {
                           Command = "CustomMenuTrade",
                           Color = "0.50 0.50 1.00 1.00",
                           Text = "Меню обмена с друзьями",
                           TextSize = 15,
                           TextColor = "0.00 0.00 0.00 1.00",
                           TextFont = "RobotoCondensed-Bold.ttf"
                       },
                       new GuiButton()
                       {
                           Command = "CustomMenuHome",
                           Color = "0.00 0.50 0.75 1.00",
                           Text = "Меню домов",
                           TextSize = 15,
                           TextColor = "0.00 0.00 0.00 1.00",
                           TextFont = "RobotoCondensed-Bold.ttf"
                       },
                       new GuiButton()
                       {
                           Command = "CustomMenuTP",
                           Color = "0.00 0.00 0.63 1.00",
                           Text = "Телепорты к друзьям",
                           TextSize = 15,
                           TextColor = "1.00 1.00 1.00 1.00",
                           TextFont = "RobotoCondensed-Bold.ttf"
                       },
                       new GuiButton()
                       {
                           Command = "CustomMenuBgrade",
                           Color = "0.00 0.50 0.00 0.47",
                           Text = "Улучшение построек",
                           TextSize = 15,
                           TextColor = "1.00 1.00 1.00 1.00",
                           TextFont = "RobotoCondensed-Bold.ttf"
                       },
                       new GuiButton()
                       {
                           Command = "chat.say /remove",
                           Color = "1.00 0.39 0.00 1.00",
                           Text = "Remove",
                           TextSize = 15,
                           TextColor = "0.00 0.00 0.00 1.00",
                           TextFont = "RobotoCondensed-Bold.ttf"
                       }
                   },
                    Commands = new BuildinCommands()
                    {
                        FriendTP = new GuiButton()
                        {
                            Command = "chat.say /tpr {0}",
                            Color = "0.67 0.85 0.48 1.00",
                            Text = "Телепорт к игроку {0}",
                            TextSize = 13,
                            TextColor = "0.00 0.00 0.00 1.00",
                            TextFont = "RobotoCondensed-Bold.ttf"
                        },
                        FriendTrade = new GuiButton()
                        {
                            Command = "trade {0}",
                            Color = "0.67 0.85 0.48 1.00",
                            Text = "Обмен с игроком {0}",
                            TextSize = 13,
                            TextColor = "0.00 0.00 0.00 1.00",
                            TextFont = "RobotoCondensed-Bold.ttf"
                        },
                        HomeTP = new GuiButton()
                        {
                            Command = "chat.say /home {0}",
                            Color = "0.83 0.56 0.00 1.00",
                            Text = "Телепорт в дом {0}",
                            TextSize = 13,
                            TextColor = "0.00 0.00 0.00 1.00",
                            TextFont = "RobotoCondensed-Bold.ttf"
                        }
                    },
                    Custom = new Dictionary<string, List<GuiButton>>()
                    {
                        ["CustomMenuBgrade"] = new List<GuiButton>()
                       {
                           new GuiButton()
                           {
                               Command = "chat.say /bgrade 0",
                               Color = "0.00 1.00 0.00 1.00",
                               Text = "Отключить авто-улучшение",
                               TextSize = 15,
                               TextColor = "0.00 0.00 0.00 1.00",
                               TextFont = "RobotoCondensed-Bold.ttf"
                           },
                           new GuiButton()
                           {
                               Command = "chat.say /bgrade 1",
                               Color = "0.75 0.25 0.00 1.00",
                               Text = "Авто улучшение до дерева",
                               TextSize = 15,
                               TextColor = "0.00 0.00 0.00 1.00",
                               TextFont = "RobotoCondensed-Bold.ttf"
                           },
                           new GuiButton()
                           {
                               Command = "chat.say /bgrade 2",
                               Color = "0.55 0.55 0.48 1.00",
                               Text = "Авто улучшение до камня",
                               TextSize = 15,
                               TextColor = "0.00 0.00 0.00 1.00",
                               TextFont = "RobotoCondensed-Bold.ttf"
                           },
                           new GuiButton()
                           {
                               Command = "chat.say /bgrade 3",
                               Color = "0.33 0.38 0.40 1.00",
                               Text = "Авто улучшение до метала",
                               TextSize = 15,
                               TextColor = "1.00 1.00 1.00 1.00",
                               TextFont = "RobotoCondensed-Bold.ttf"
                           },
                           new GuiButton()
                           {
                               Command = "chat.say /bgrade 4",
                               Color = "0.25 0.00 0.00 1.00",
                               Text = "Авто улучшение до бронированного",
                               TextSize = 15,
                               TextColor = "1.00 1.00 1.00 1.00",
                               TextFont = "RobotoCondensed-Bold.ttf"
                           }
                       }
                    }
                };
            }
        }
        #endregion

        #region Config initialization
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Благодарим за приобритение плагина на сайте RustPlugin.ru. Если вы приобрели этот плагин на другом ресурсе знайте - это лишает вас гарантированных обновлений!");
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
            foreach (var key in config.Custom.Keys)
            {
                cmd.AddConsoleCommand(key, this, "CustomCommands");
            }
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
        #endregion

        #region Commands
        private void CustomCommands(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
            {
                arg.ReplyWith("This command can be only executed by the player");
                return;
            }
            var split = arg.cmd.FullName.Split('.');
            var parent = split.Length >= 2 ? split[0].Trim() == "global" ? "" : split[0].Trim() : "";
            parent = parent == "" ? "" : parent + ".";
            var name = split.Length >= 2 ? string.Join(".", split.Skip(1).ToArray()) : split[0].Trim();
            var CmdName = parent + name;
            List<GuiButton> btns;
            if (!config.Custom.TryGetValue(CmdName, out btns))
            {
                PrintWarning($"Назначенная команда {CmdName} на нейдена в файле конфигурации!\nСообщите об этом разработчику - https://vk.com/vlad_00003");
            }
            CreateGui(player, btns);
        }
        [ChatCommand("cm")]
        private void OpenMenu(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelName);
            CreateGui(player, config.Buttons);
        }
        [ConsoleCommand("cm")]
        private void OpenMenuConsole(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
            {
                arg.ReplyWith("This command can be only executed by the player");
                return;
            }
            OpenMenu(player);
        }
        #endregion

        #region Oxide hooks
        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, PanelName);
                CuiHelper.DestroyUi(player, Footername);
            }
        }
        #endregion

        #region GUI
        private void CreateGui(BasePlayer player, List<GuiButton> buttons)
        {
            CuiHelper.DestroyUi(player, PanelName);
            CuiHelper.DestroyUi(player, Footername);
            var foot = new CuiElementContainer()
            {
                {
                    new CuiPanel
                    {
                        Image =
                        {
                            Color = config.Footer.Color
                        },
                        RectTransform =
                        {
                            AnchorMin = config.Footer.Amin,
                            AnchorMax = config.Footer.Amax
                        },
                        CursorEnabled = true
                    },
                    new CuiElement().Parent = "Overlay", Footername
                }
            };
            foot.Add(new CuiButton
            {
                Button =
                {
                    Command = "CMUI_COMMAND_HANDLER ",
                    Color = config.Exit.Color
                },
                RectTransform =
                {
                    AnchorMin = config.Exit.Amin,
                    AnchorMax = config.Exit.Amax
                },
                Text =
                {
                    Text = config.Exit.Text,
                    FontSize = config.Exit.TextSize,
                    Align = TextAnchor.MiddleCenter,
                    Color = config.Exit.TextColor,
                    Font = config.Exit.TextFont
                }
            }, Footername);
            foot.Add(new CuiButton
            {
                Button =
                {
                    Command = "cm",
                    Color = config.Menu.Color
                },
                RectTransform =
                {
                    AnchorMin = config.Menu.Amin,
                    AnchorMax = config.Menu.Amax
                },
                Text =
                {
                    Text = config.Menu.Text,
                    FontSize = config.Menu.TextSize,
                    Align = TextAnchor.MiddleCenter,
                    Color = config.Menu.TextColor,
                    Font = config.Menu.TextFont
                }
            }, Footername);
            var main = new CuiElementContainer()
            {
                {
                    new CuiPanel
                    {
                        Image =
                        {
                            Color = config.Main.Color
                        },
                        RectTransform =
                        {
                            AnchorMin = config.Main.Amin,
                            AnchorMax = config.Main.Amax
                        },
                        CursorEnabled = true
                    },
                    new CuiElement().Parent = "Overlay", PanelName
                }
            };
            var poses = GetPositions(buttons.Count, config.Auto.Left, config.Auto.Right);
            int i = 0;
            foreach (var btn in buttons)
            {
                main.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"CMUI_COMMAND_HANDLER {btn.Command}",
                        Color = btn.Color
                    },
                    RectTransform =
                    {
                        AnchorMin = poses[i].Amin,//btn.Amin,
                        AnchorMax = poses[i].Amax//btn.Amax
                    },
                    Text =
                    {
                        Text = btn.Text,
                        FontSize = btn.TextSize,
                        Align = TextAnchor.MiddleCenter,
                        Color = btn.TextColor,
                        Font = btn.TextFont
                    }
                }, PanelName);
                i++;
            }
            CuiHelper.AddUi(player, main);
            CuiHelper.AddUi(player, foot);
        }
        [ConsoleCommand("CMUI_COMMAND_HANDLER")]
        private void CustomMenuUiCommand(ConsoleSystem.Arg arg)
        {
            var cmd = "";
            var player = arg.Player();
            if (player == null)
            {
                arg.ReplyWith("This command can be executed only be the client!");
                return;
            }

            CuiHelper.DestroyUi(player, PanelName);
            CuiHelper.DestroyUi(player, Footername);
            var array = arg.Args.ToArray();
            if (array.Length > 0 && array[0] == "chat.say")
            {
                cmd = $"chat.say \"{string.Join(" ", array.Skip(1).ToArray())}\"";
            }
            else
            {
                cmd = string.Join(" ", arg.Args);
            }
            //cmd = string.Join(" ", arg.Args.ToArray());
            //Puts($"Cmd is {cmd}");
            player.Command(cmd);
        }
        #endregion

        #region GUI Positions
        class Position
        {
            public string Amin;
            public string Amax;
            public override string ToString()
            {
                return $"Amin: {Amin}\nAmax: {Amax}";
            }
        }
        private Position[] GetPositions(int num, float left, float right)
        {
            Position[] result = new Position[num];
            float perc = 1f / (num + 1);
            float size = perc / 3;
            for (int i = 1; i <= num; i++)
            {
                float mul = i * perc;
                result[i - 1] = new Position();
                result[i - 1].Amin = $"{left} {Math.Round(mul - size, 4)}";
                result[i - 1].Amax = $"{right} {Math.Round(mul + size, 4)}";
            }
            return result;
        }
        #endregion

        #region Build-in sub menus
        [ConsoleCommand("CustomMenuTP")]
        private void FriendsTP(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
            {
                arg.ReplyWith("This command can be only executed by the player");
                return;
            }
            List<GuiButton> buttons = new List<GuiButton>();
            var btn = config.Commands.FriendTP;
            var list = Friends?.CallHook("GetFriends", player.userID);
            if (list != null)
            {
                foreach (var id in (ulong[])list)
                {
                    var friend = GetPlayerByID(id);
                    if (friend == null)
                        continue;
                    buttons.Add(new GuiButton()
                    {
                        Color = btn.Color,
                        Command = string.Format(btn.Command, friend.userID),
                        Text = string.Format(btn.Text, friend.displayName),
                        TextColor = btn.TextColor,
                        TextFont = btn.TextFont,
                        TextSize = btn.TextSize
                    });
                }
            }
            CreateGui(player, buttons);
        }
        [ConsoleCommand("CustomMenuTrade")]
        private void FriendsTrade(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
            {
                arg.ReplyWith("This command can be only executed by the player");
                return;
            }
            List<GuiButton> buttons = new List<GuiButton>();
            var btn = config.Commands.FriendTrade;
            var list = Friends?.CallHook("GetFriends", player.userID);
            if (list != null)
            {
                foreach (var id in (ulong[])list)
                {
                    var friend = GetPlayerByID(id);
                    if (friend == null)
                        continue;
                    string command = string.Format(btn.Command, "\"" + friend.displayName + "\"");
                    //PrintWarning(command);
                    buttons.Add(new GuiButton()
                    {
                        Color = btn.Color,
                        Command = command,
                        Text = string.Format(btn.Text, friend.displayName),
                        TextColor = btn.TextColor,
                        TextFont = btn.TextFont,
                        TextSize = btn.TextSize
                    });
                }
            }
            CreateGui(player, buttons);
        }
        [ConsoleCommand("CustomMenuHome")]
        private void testcommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
            {
                arg.ReplyWith("This command can be only executed by the player");
                return;
            }
            List<GuiButton> buttons = new List<GuiButton>();
            var btn = config.Commands.HomeTP;
            var list = NTeleportation?.CallHook("GetHomes", player.userID);
            if(list != null)
            {
                foreach(var home in (Dictionary<string, Vector3>)list)
                {
                    buttons.Add(new GuiButton()
                    {
                        Color = btn.Color,
                        Command = string.Format(btn.Command, home.Key),
                        Text = string.Format(btn.Text, home.Key),
                        TextColor = btn.TextColor,
                        TextFont = btn.TextFont,
                        TextSize = btn.TextSize
                    });
                }
            }
            CreateGui(player, buttons);
        }
        #endregion

        #region Helpers
        private BasePlayer GetPlayerByID(ulong id)
        {
            return BasePlayer.activePlayerList.FirstOrDefault(x => x.userID == id);
        }
        public static string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_' || c == ' ')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
        #endregion
    }
}