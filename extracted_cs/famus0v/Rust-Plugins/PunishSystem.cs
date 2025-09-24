using System;
using System.Collections.Generic;
using System.Linq;
using ConVar;
using Facepunch.Extend;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("PunishSystem", "Kira", "1.0.9")]
    public class PunishSystem : RustPlugin
    {
        #region [Vars]

        [PluginReference] private Plugin Payback;
        private const string UIMain = "UI.PunishSystem";
        private const string AdminPerm = "punishsystem.admin";
        private const string ModeratorPerms = "punishsystem.moderator";
        private const string Blur = "assets/content/ui/uibackgroundblur.mat";
        private StoredData _dataBase = new StoredData();
        private Timer _timer;

        #endregion

        #region [Classes]

        private class Buffer
        {
            public string Name;
            public string Description;
            public int Time;
        }

        #endregion

        #region [Configuraton] / [Конфигурация]

        private ConfigData _config;

        public class ConfigData
        {
            [JsonProperty(PropertyName = "PunishSystem - Config")]
            public PunishSystemCfg PunishSystemSettings = new PunishSystemCfg();

            public class PunishSystemCfg
            {
                [JsonProperty(PropertyName = "Список наказаний")]
                public List<string> PunishList;
            }
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                PunishSystemSettings = new ConfigData.PunishSystemCfg
                {
                    PunishList = new List<string>
                    {
                        "egg",
                        "rocketman",
                        "shark"
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
                ["PLAYERS"] = "СПИСОК ИГРОКОВ",
                ["NICK"] = "ЗАПРЕЩЁННЫЙ НИК",
                ["NOPERM"] = "Нет доступа к команде",
                ["NOTFOUND"] = "Игрок [{0}] не найден",
                ["MESSAGE"] = "Вы нарушили правила сервера: {0}\n" +
                              "Что бы продолжить играть на нашем сервере вы обязаны отыграть в таком виде время наказания [{1}]. " +
                              "Время афк не учитывается. Для проверки оставшегося времени наказания напишите /jail в чат. "
            };

            var en = new Dictionary<string, string>
            {
                ["NICK"] = "FORBIDDEN NICKNAME",
                ["NOPERM"] = "No access to the command",
                ["NOTFOUND"] = "Player [{0}] not found",
                ["MESSAGE"] = "You violated the rules of the server: {0}\n" +
                              "To continue playing on our server, you must play the punishment time in this form [{1}]. " +
                              "afc time is not taken into account. To check the remaining time of punishment, write /jail to the chat. "
            };
            lang.RegisterMessages(ru, this, "ru");
            lang.RegisterMessages(en, this);
        }

        #endregion

        #region [UI]

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
                            Color = "0.00 0.00 0.00 0.5",
                            Material = Blur
                        },
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    },
                    "Overlay", UIMain
                }
            };

            ui.Add(new CuiLabel
            {
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.7",
                    Text = lang.GetMessage("PLAYERS", this, player.UserIDString),
                    FontSize = 20
                },
                RectTransform =
                {
                    AnchorMin = "0 0.9",
                    AnchorMax = "1 1"
                }
            }, UIMain);

            ui.Add(new CuiButton
            {
                Button =
                {
                    Close = UIMain,
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
            }, UIMain);

            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.AddUi(player, ui);
            DrawUI_Players(player, BasePlayer.activePlayerList, 0);
            DrawUI_Find(player);
            DrawUI_Pagination(player, 0);
        }

        private void DrawUI_Players(BasePlayer player, IEnumerable<BasePlayer> players, int page)
        {
            var ui = new CuiElementContainer
            {
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Close = UIMain,
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
                    },
                    UIMain, $"{UIMain}.Players.Bg"
                }
            };

            int x = 0, y = 0, num = 0;
            foreach (var obj in players.Skip(page * 144).Take(144))
            {
                if (x == 9)
                {
                    x = 0;
                    y++;
                }

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Color = "0 0 0 0.7",
                        Command = $"punish.info {obj.userID}",
                        FadeIn = 0.01f * num
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1",
                        FontSize = 14,
                        Text = obj.displayName
                    },
                    RectTransform =
                    {
                        AnchorMin = $"{0.02864584 + (x * 0.105)} {0.850926 - (y * 0.05)}",
                        AnchorMax = $"{0.128125 + (x * 0.105)} {0.892592 - (y * 0.05)}"
                    }
                }, $"{UIMain}.Players.Bg");
                x++;
                num++;
            }

            CuiHelper.DestroyUi(player, $"{UIMain}.Players.Bg");
            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_Find(BasePlayer player)
        {
            var ui = new CuiElementContainer
            {
                new CuiElement
                {
                    Name = $"{UIMain}.Find.Bg",
                    Parent = UIMain,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0 0 0 0.7"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.4054688 0.01203599",
                            AnchorMax = "0.5945312 0.05277722"
                        }
                    }
                },
                new CuiElement
                {
                    Name = $"{UIMain}.Find",
                    Parent = $"{UIMain}.Find.Bg",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1",
                            Command = "punish.find",
                            FontSize = 14
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    }
                }
            };

            CuiHelper.DestroyUi(player, $"{UIMain}.Find.Bg");
            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_Pagination(BasePlayer player, int page)
        {
            var ui = new CuiElementContainer
            {
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Color = "0 0 0 0.5",
                            Command = $"punish.playerspage {page - 1}",
                            FadeIn = 0.01f
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1",
                            FontSize = 20,
                            Text = "<"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.3822916 0.01203599",
                            AnchorMax = "0.4045 0.05277722"
                        }
                    },
                    UIMain, $"{UIMain}.Players.Left"
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Color = "0 0 0 0.5",
                            Command = $"punish.playerspage {page + 1}",
                            FadeIn = 0.01f
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1",
                            FontSize = 14,
                            Text = ">"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.5955 0.01203599",
                            AnchorMax = "0.6171844 0.05277722"
                        }
                    },
                    UIMain, $"{UIMain}.Players.Right"
                }
            };

            CuiHelper.DestroyUi(player, $"{UIMain}.Players.Left");
            CuiHelper.DestroyUi(player, $"{UIMain}.Players.Right");
            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_PunishList(BasePlayer player, BasePlayer target)
        {
            var ui = new CuiElementContainer
            {
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Color = "0 0 0 0.95",
                            Close = $"{UIMain}.PunishList",
                            Material = Blur
                        },
                        Text = { Text = " " },
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    },
                    UIMain, $"{UIMain}.PunishList"
                },
                new CuiElement
                {
                    Name = $"{UIMain}.BG.1",
                    Parent = $"{UIMain}.PunishList",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0 0 0 0.7"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.4013021 0.655551",
                            AnchorMax = "0.5986979 0.7314757"
                        }
                    }
                }
            };

            ui.Add(new CuiLabel
            {
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.05",
                    Text = "PUNISH NAME",
                    FontSize = 15
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, $"{UIMain}.BG.1");

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.BG.2",
                Parent = $"{UIMain}.PunishList",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0.7"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.4013021 0.5601835",
                        AnchorMax = "0.5986979 0.6361082"
                    }
                }
            });
            ui.Add(new CuiLabel
            {
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.05",
                    Text = "TIME (S/M/H/D)",
                    FontSize = 15
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, $"{UIMain}.BG.2");

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.BG.3",
                Parent = $"{UIMain}.PunishList",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0.7"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.4013021 0.4648148",
                        AnchorMax = "0.5986979 0.5407406"
                    }
                }
            });

            ui.Add(new CuiLabel
            {
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.05",
                    Text = "DESCRIPTION",
                    FontSize = 15
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, $"{UIMain}.BG.3");

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.PunishName",
                Parent = $"{UIMain}.PunishList",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1",
                        Command = $"punish.send {target.userID} name",
                        FontSize = 14
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.4013021 0.655551",
                        AnchorMax = "0.5986979 0.7314757"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Time",
                Parent = $"{UIMain}.PunishList",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1",
                        Command = $"punish.send {target.userID} time",
                        FontSize = 14
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.4013021 0.5601835",
                        AnchorMax = "0.5986979 0.6361082"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Description",
                Parent = UIMain,
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1",
                        Command = $"punish.send {target.userID} description",
                        FontSize = 14
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.4013021 0.4648148",
                        AnchorMax = "0.5986979 0.5407406"
                    }
                }
            });

            ui.Add(new CuiButton
            {
                Button =
                {
                    Color = "0.24 0.72 0.27 0.72",
                    Command = $"punish.send {target.userID} completed",
                    FadeIn = 0.01f,
                    Close = UIMain
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.7",
                    FontSize = 20,
                    Text = "НАКАЗАТЬ"
                },
                RectTransform =
                {
                    AnchorMin = "0.4013021 0.4064815",
                    AnchorMax = "0.5986979 0.4500009"
                }
            }, $"{UIMain}.PunishList", $"{UIMain}.Send.Completed");

            ui.Add(new CuiButton
            {
                Button =
                {
                    Color = "0.72 0.24 0.24 0.72",
                    Command = $"punish.clear {target.userID} ",
                    FadeIn = 0.01f,
                    Close = UIMain
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.7",
                    FontSize = 20,
                    Text = "ОЧИСТИТЬ"
                },
                RectTransform =
                {
                    AnchorMin = "0.4013021 0.3546295",
                    AnchorMax = "0.5986979 0.3981489"
                }
            }, $"{UIMain}.PunishList", $"{UIMain}.Send.Clear");

            int y = 0, x = 0;
            foreach (var punish in _config.PunishSystemSettings.PunishList)
            {
                if (!permission.UserHasPermission(player.UserIDString, AdminPerm) & punish != "egg") continue;
                if (y == 14)
                {
                    y = 0;
                    x++;
                }

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Color = "0 0 0 0.72",
                        Command = $"punish.send {target.userID} name {punish.ToLower()}",
                        FadeIn = 0.01f
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 0.7",
                        FontSize = 20,
                        Text = punish.ToUpper()
                    },
                    RectTransform =
                    {
                        AnchorMin = $"{0.2494792 - (x * 0.147f)} {0.6935183 - (y * 0.045f)}",
                        AnchorMax = $"{0.3921876 - (x * 0.147f)} {0.7314813 - (y * 0.045f)}"
                    }
                }, $"{UIMain}.PunishList");
                y++;
            }

            CuiHelper.AddUi(player, ui);
        }

        private void DrawUI_Description(BasePlayer player, string txt)
        {
            var ui = new CuiElementContainer
            {
                {
                    new CuiLabel
                    {
                        Text =
                        {
                            Align = TextAnchor.UpperCenter,
                            Color = "1.00 0.18 0.00 1.00",
                            FontSize = 40,
                            Text = txt
                        },
                        RectTransform =
                        {
                            AnchorMin = "0 0.7",
                            AnchorMax = "1 0.9"
                        }
                    },
                    "Overlay", "Nick"
                }
            };

            CuiHelper.DestroyUi(player, "Nick");
            CuiHelper.AddUi(player, ui);
        }

        #endregion

        #region [Hooks]

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            LoadData();
            if (!permission.PermissionExists(ModeratorPerms)) permission.RegisterPermission(ModeratorPerms, this);
            if (!permission.PermissionExists(AdminPerm)) permission.RegisterPermission(AdminPerm, this);
            _timer = timer.Every(1f, () =>
            {
                foreach (var obj in _dataBase.Cooldowns.ToList())
                {
                    var player = BasePlayer.FindByID(obj.Key);
                    if (player == null) continue;
                    if (!_dataBase.Afk.ContainsKey(player.userID))
                        _dataBase.Afk.Add(player.userID, player.eyes.GetLookRotation().eulerAngles);
                    if (player.eyes.GetLookRotation().eulerAngles == _dataBase.Afk[player.userID]) return;
                    _dataBase.Afk[obj.Key] = player.eyes.GetLookRotation().eulerAngles;
                    _dataBase.Cooldowns[obj.Key]--;
                    IsCooldown(player);
                }
            });
        }

        // ReSharper disable once UnusedMember.Local
        private void Unload()
        {
            SaveData();
            _timer.Destroy();
        }

        // ReSharper disable once UnusedMember.Local
        private void OnPlayerSleepEnded(BasePlayer player)
        {
            CheckName(player);
        }

        // ReSharper disable once UnusedMember.Local 
        // ReSharper disable once UnusedParameter.Local
        private object OnItemPickup(Item item, BasePlayer player)
        {
            if (_dataBase.Cooldowns.ContainsKey(player.userID)) return false;
            return null;
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private object CanLootEntity(BasePlayer player, StorageContainer container)
        {
            if (_dataBase.Cooldowns.ContainsKey(player.userID)) return false;
            return null;
        }

        // ReSharper disable once UnusedMember.Local
        private object OnPlayerRespawn(BasePlayer player)
        {
            NextFrame(() =>
            {
                if (!_dataBase.Cooldowns.ContainsKey(player.userID)) return;
                player.inventory.containerBelt.Clear();
                player.inventory.containerMain.Clear();
                player.inventory.containerWear.Clear();
                var item = ItemManager.CreateByName("attire.egg.suit");
                item.MoveToContainer(player.inventory.containerWear);
                var pistol = ItemManager.CreateByName("pistol.water");
                pistol.contents.AddItem(ItemManager.FindItemDefinition("water"), 250);
                pistol.MoveToContainer(player.inventory.containerBelt);
                player.SetHealth(1);
            });

            return null;
        }

        // ReSharper disable once UnusedMember.Local  
        // ReSharper disable once UnusedParameter.Local
        private object OnPlayerVoice(BasePlayer player, Byte[] data)
        {
            if (_dataBase.Cooldowns.ContainsKey(player.userID)) return false;
            return null;
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private object OnPlayerCommand(BasePlayer player, string command, string[] args)
        {
            if (!_dataBase.Cooldowns.ContainsKey(player.userID)) return null;
            if (command != "punish" & command != "jail") return false;
            return null;
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private object OnPlayerChat(BasePlayer player, string message, Chat.ChatChannel channel)
        {
            if (_dataBase.Cooldowns.ContainsKey(player.userID)) return false;
            return null;
        }

        #endregion

        #region [Commands]

        [ChatCommand("punish")]
        // ReSharper disable once UnusedMember.Local
        private void PunishOpen(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, ModeratorPerms))
            {
                player.ChatMessage(lang.GetMessage("NOPERM", this, player.UserIDString));
                return;
            }

            DrawUI_Main(player);
        }

        [ConsoleCommand("punish.playerspage")]
        // ReSharper disable once UnusedMember.Local
        private void PlayersPagination(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!permission.UserHasPermission(player.UserIDString, ModeratorPerms))
            {
                player.ChatMessage(lang.GetMessage("NOPERM", this, player.UserIDString));
                return;
            }

            var page = args.Args[0].ToInt();
            if (page <= 0) return;
            if (BasePlayer.activePlayerList.Count / 144 < page) return;
            DrawUI_Players(player, BasePlayer.activePlayerList, page);
            DrawUI_Pagination(player, page);
        }

        [ConsoleCommand("punish.find")]
        // ReSharper disable once UnusedMember.Local
        private void PlayersFind(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!permission.UserHasPermission(player.UserIDString, ModeratorPerms))
            {
                player.ChatMessage(lang.GetMessage("NOPERM", this, player.UserIDString));
                return;
            }

            var find = GetFindPlayers(args.Args[0]);
            DrawUI_Players(player, find, 0);
            DrawUI_Find(player);
            DrawUI_Pagination(player, 0);
        }

        [ChatCommand("jail")]
        // ReSharper disable once UnusedMember.Local
        private void Jail(BasePlayer player)
        {
            if (!_dataBase.Cooldowns.ContainsKey(player.userID)) return;
            var time = TimeSpan.FromSeconds(_dataBase.Cooldowns[player.userID]);
            player.ChatMessage($"{time.Days} D {time.Hours} {time.Minutes} M {time.Seconds} S");
        }

        [ConsoleCommand("punish.clear")]
        // ReSharper disable once UnusedMember.Local
        private void PunishClear(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!permission.UserHasPermission(player.UserIDString, ModeratorPerms))
            {
                player.ChatMessage(lang.GetMessage("NOPERM", this, player.UserIDString));
                return;
            }

            var find = BasePlayer.Find(args.Args[0]);
            if (find == null) return;
            Payback.Call("CardClear", find);
            if (_dataBase.Nicknames.ContainsKey(find.userID)) _dataBase.Nicknames.Remove(find.userID);
            if (_dataBase.Cooldowns.ContainsKey(find.userID)) _dataBase.Cooldowns[find.userID] = 0;
            IsCooldown(find);
        }

        [ConsoleCommand("punish.info")]
        // ReSharper disable once UnusedMember.Local
        private void PunishInfo(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            if (!permission.UserHasPermission(args.Player().UserIDString, ModeratorPerms))
            {
                args.Player().ChatMessage(lang.GetMessage("NOPERM", this, args.Player().UserIDString));
                return;
            }

            var target = BasePlayer.Find(args.Args[0]);
            if (target == null) return;
            DrawUI_PunishList(args.Player(), target);
        }

        [ConsoleCommand("punish.send")]
        // ReSharper disable once UnusedMember.Local
        private void Send(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            if (!permission.UserHasPermission(args.Player().UserIDString, ModeratorPerms))
            {
                args.Player().ChatMessage(lang.GetMessage("NOPERM", this, args.Player().UserIDString));
                return;
            }

            var target = BasePlayer.Find(args.Args[0]);
            if (target == null) return;
            if (args.Args[1] == "name")
            {
                if ((args.Args[2] != "egg" & args.Args[2] != "nick"))
                {
                    if (!permission.UserHasPermission(target.UserIDString, ModeratorPerms))
                    {
                        target.ChatMessage(lang.GetMessage("NOPERM", this, target.UserIDString));
                        return;
                    }

                    Payback.Call("GenericChatCommand", target, args.Args[2].ToLower(), args.Args);
                    return;
                }
            }

            var send = new Buffer();
            if (_dataBase.Buffers.ContainsKey(target.userID)) send = _dataBase.Buffers[target.userID];
            else _dataBase.Buffers.Add(target.userID, send);
            switch (args.Args[1].ToLower())
            {
                case "name":
                    send.Name = args.Args[2].ToLower();
                    _dataBase.Buffers[target.userID] = send;
                    SaveData();
                    return;
                case "time":
                    var txt = args.Args[2].ToLower();
                    int time;
                    if (txt.Contains("d"))
                    {
                        var replace = txt.Replace("d", "");
                        time = replace.ToInt() * 86400;
                        send.Time = time;
                    }

                    if (txt.Contains("h"))
                    {
                        var replace = txt.Replace("h", "");
                        time = replace.ToInt() * 3600;
                        send.Time = time;
                    }

                    if (txt.Contains("m"))
                    {
                        var replace = txt.Replace("m", "");
                        time = replace.ToInt() * 60;
                        send.Time = time;
                    }

                    if (txt.Contains("s"))
                    {
                        var replace = txt.Replace("s", "");
                        send.Time = replace.ToInt();
                    }

                    _dataBase.Buffers[target.userID] = send;
                    SaveData();
                    return;
                case "description":
                    send.Description = args.Args[2];
                    _dataBase.Buffers[target.userID] = send;
                    SaveData();
                    break;
                case "completed":
                    if (send.Name == null | send.Description == null | send.Time == 0) return;

                    switch (send.Name.ToLower())
                    {
                        case "egg":
                            PunishEgg(target, send.Time);
                            CuiHelper.DestroyUi(args.Player(), UIMain);
                            SaveData();
                            return;
                        case "nick":
                            if (_dataBase.Nicknames.ContainsKey(target.userID)) return;
                            _dataBase.Nicknames.Add(target.userID, target.displayName);
                            PunishEgg(target, 3596400);
                            CuiHelper.DestroyUi(args.Player(), UIMain);
                            SaveData();
                            return;
                        default:
                            PrintWarning("Хуево всё, звоните Кире [-Kira#1920]");
                            break;
                    }

                    return;
            }
        }

        #endregion

        #region [Helpers]

        private void CheckName(BasePlayer player)
        {
            if (!_dataBase.Nicknames.ContainsKey(player.userID)) return;
            if (player.displayName == _dataBase.Nicknames[player.userID])
            {
                DrawUI_Description(player, lang.GetMessage("NICK", this, player.UserIDString));
                player.inventory.containerBelt.Clear();
                player.inventory.containerMain.Clear();
                player.inventory.containerWear.Clear();
                var egg = ItemManager.CreateByName("attire.egg.suit");
                egg.MoveToContainer(player.inventory.containerWear);
                var pistol = ItemManager.CreateByName("pistol.water");
                pistol.contents.AddItem(ItemManager.FindItemDefinition("water"), 250);
                pistol.MoveToContainer(player.inventory.containerBelt);
                player.SetHealth(1);
                player.inventory.containerWear.SetLocked(true);
                player.inventory.containerMain.SetLocked(true);
                player.inventory.containerBelt.SetLocked(true);
            }
            else
            {
                _dataBase.Nicknames.Remove(player.userID);
                _dataBase.Cooldowns[player.userID] = 0;
                IsCooldown(player);
            }
        }

        private void IsCooldown(BasePlayer player)
        {
            if (!_dataBase.Cooldowns.ContainsKey(player.userID)) return;
            var cooldowngive = _dataBase.Cooldowns[player.userID];
            if (0 <= cooldowngive) return;
            CuiHelper.DestroyUi(player, "Nick");
            _dataBase.Cooldowns.Remove(player.userID);
            _dataBase.Buffers.Remove(player.userID);
            var egg = player.inventory.FindItemID("attire.egg.suit");
            egg?.Remove();
            var pistol = player.inventory.FindItemID("pistol.water");
            pistol?.Remove();
            player.SetHealth(100);
            player.inventory.containerWear.SetLocked(false);
            player.inventory.containerMain.SetLocked(false);
            player.inventory.containerBelt.SetLocked(false);
        }

        private void PunishEgg(BasePlayer player, int time)
        {
            player.inventory.containerBelt.Clear();
            player.inventory.containerMain.Clear();
            player.inventory.containerWear.Clear();
            var egg = ItemManager.CreateByName("attire.egg.suit");
            egg.MoveToContainer(player.inventory.containerWear);
            var pistol = ItemManager.CreateByName("pistol.water");
            pistol.contents.AddItem(ItemManager.FindItemDefinition("water"), 250);
            pistol.MoveToContainer(player.inventory.containerBelt);
            player.SetHealth(1);
            _dataBase.Cooldowns.Add(player.userID, time);
            player.inventory.containerWear.SetLocked(true);
            player.inventory.containerMain.SetLocked(true);
            player.inventory.containerBelt.SetLocked(true);
            player.Hurt(1000);
            var span = TimeSpan.FromSeconds(_dataBase.Buffers[player.userID].Time);
            DrawUI_Description(player,
                lang.GetMessage("MESSAGE", this, player.UserIDString)
                    .Replace("{0}", _dataBase.Buffers[player.userID].Description).Replace("{1}",
                        $"{span.Days} D {span.Hours} H {span.Minutes} M {span.Seconds} S"));
        }

        private static IEnumerable<BasePlayer> GetFindPlayers(string name)
        {
            return !name.IsNumeric()
                ? BasePlayer.activePlayerList.ToList().Where(p => p.displayName.ToLower().Contains(name.ToLower()))
                : BasePlayer.activePlayerList.ToList()
                    .Where(p => p.userID.ToString().ToLower().Contains(name.ToLower()));
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

        #region [DataBase]

        private class StoredData
        {
            public readonly Dictionary<ulong, int> Cooldowns = new Dictionary<ulong, int>();
            public readonly Dictionary<ulong, Vector3> Afk = new Dictionary<ulong, Vector3>();
            public readonly Dictionary<ulong, string> Nicknames = new Dictionary<ulong, string>();
            public readonly Dictionary<ulong, Buffer> Buffers = new Dictionary<ulong, Buffer>();
        }

        #endregion
    }
}