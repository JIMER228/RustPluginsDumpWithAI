using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Kits", "Luminos", "1.0.0")]
    [Description("")]
    public class Kits : RustPlugin
    {
        #region [Vars]

        [PluginReference] private Plugin ImageLibrary;
        private StoredData _dataBase = new StoredData();

        private const string UiMain = "Kits.UI.Main";
        private const string UiKitList = "Kits.UI.Main.Kit.List.BG";
        private const string UiKitView = "Kits.UI.Main.Kit.Info.BG";
        private readonly string _serverName = ConVar.Server.hostname.ToUpper();
        private const string Sharp = "assets/content/ui/ui.background.tile.psd";

        #endregion

        #region [UI]

        private void UI_Main(BasePlayer player)
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            var ui = new CuiElementContainer();

            ui.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0.00 0.00 0.00 0.95"
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, "Overlay", $"{UiMain}.Background");

            ui.Add(new CuiButton
            {
                Button =
                {
                    Color = "0 0 0 0",
                    Close = $"{UiMain}.Background"
                },
                Text = { Text = " "},
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            },$"{UiMain}.Background");

            ui.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image =
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = "0.06041668 0.1574074",
                    AnchorMax = "0.9401042 0.8444444"
                }
            }, $"{UiMain}.Background", UiMain);

            ui.Add(new CuiElement
            {
                Name = $"{UiMain}.Kit.List.BG",
                Parent = UiMain,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0.00 0.00 0.00 0.9"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "0.2220249 1"
                    }
                }
            });

            #region [Kit-List]

            ui.Add(new CuiLabel
            {
                Text =
                {
                    Align = TextAnchor.MiddleLeft,
                    Color = "0.85 0.42 0.07 1.00",
                    FadeIn = 0.4f,
                    FontSize = 25,
                    Text = "Kits"
                },
                RectTransform =
                {
                    AnchorMin = "0.05 0.880054",
                    AnchorMax = "1 1"
                }
            }, $"{UiMain}.Kit.List.BG");

            #endregion

            #region [ServerName]

            ui.Add(new CuiElement
            {
                Name = $"{UiMain}.ServerName.BG",
                Parent = UiMain,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0.00 0.00 0.00 0.9"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.2291301 0.8787066",
                        AnchorMax = "1 1"
                    }
                }
            });
            ui.Add(new CuiLabel
            {
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.5",
                    FadeIn = 0.4f,
                    FontSize = 25,
                    Text = _serverName
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, $"{UiMain}.ServerName.BG");

            #endregion

            ui.Add(new CuiElement
            {
                Name = $"{UiMain}.Kit.Info.BG",
                Parent = UiMain,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0.00 0.00 0.00 0.9"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.2291301 0",
                        AnchorMax = "1 0.8625336"
                    }
                }
            });
            DestroyMainUi(player);
            CuiHelper.AddUi(player, ui);
            UI_KitList(player);
        }

        private void UI_KitList(BasePlayer player)
        {
            var ui = new CuiElementContainer();

            var y = 0;
            foreach (var kit in _dataBase.Kits)
            {
                ui.Add(new CuiElement
                {
                    Name = $"{UiMain}.KitButtonBG.{kit.Name}",
                    Parent = UiKitList,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0.38 0.38 0.38 0.71"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.05} {0.8018911 - (y * 0.08)}",
                            AnchorMax = $"{0.95} {0.8625386 - (y * 0.08)}"
                        }
                    }
                });

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleLeft,
                        Color = "1 1 1 0.25",
                        FadeIn = 0.4f,
                        FontSize = 15,
                        Text = $"    {kit.DisplayName.ToUpper()}"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, $"{UiMain}.KitButtonBG.{kit.Name}");

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Color = "0 0 0 0",
                        Command = $"kit.view {kit.Name}"
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
                }, $"{UiMain}.KitButtonBG.{kit.Name}");

                y++;
            }

            CuiHelper.AddUi(player, ui);
        }

        private void UI_KitView(BasePlayer player, string kitname)
        {
            var kitobj = _dataBase.Kits.Find(x => x.Name == kitname);

            // ReSharper disable once UseObjectOrCollectionInitializer
            var ui = new CuiElementContainer();

            #region [Kit-Inventory]

            ui.Add(new CuiElement
            {
                Name = $"{UiMain}.Kit.Info.BG",
                Parent = UiMain,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0.00 0.00 0.00 0.9"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.2291301 0",
                        AnchorMax = "1 0.8625336"
                    }
                }
            });

            ui.Add(new CuiLabel
            {
                Text =
                {
                    Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 0.2",
                    FadeIn = 0.4f,
                    FontSize = 14,
                    Text = "INVENTORY"
                },
                RectTransform =
                {
                    AnchorMin = "0.01689652 0.9296877",
                    AnchorMax = "0.1397844 0.9765628"
                }
            }, $"{UiMain}.Kit.Info.BG");

            for (int i = 0, x = 0, y = 0; i <= 23; i++, x++)
            {
                if (x >= 8)
                {
                    x = 0;
                    y++;
                }

                ui.Add(new CuiElement
                {
                    Name = $"{UiMain}.Kit.Info.Item.BG.{i}.1",
                    Parent = $"{UiMain}.Kit.Info.BG",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0.38 0.38 0.38 0.6",
                            FadeIn = 0.1f * i
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.0168965 + (x * 0.06298012)} {0.8015624 - (y * 0.12596024)}",
                            AnchorMax = $"{0.0745002 + (x * 0.06298012)} {0.9187499 - (y * 0.12596024)}"
                        }
                    }
                });
            }

            ui.Add(new CuiLabel
            {
                Text =
                {
                    Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 0.2",
                    FadeIn = 0.4f,
                    FontSize = 14,
                    Text = "CLOTHES"
                },
                RectTransform =
                {
                    AnchorMin = "0.01689652 0.4750066",
                    AnchorMax = "0.1397844 0.5218822"
                }
            }, $"{UiMain}.Kit.Info.BG");

            for (int i = 0, x = 0; i <= 5; i++, x++)
            {
                ui.Add(new CuiElement
                {
                    Name = $"{UiMain}.Kit.Info.Item.BG.{i}.2",
                    Parent = $"{UiMain}.Kit.Info.BG",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0.38 0.38 0.38 0.6",
                            FadeIn = 0.1f * i
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.0168965 + (x * 0.06298012)} {0.3531289}",
                            AnchorMax = $"{0.0745002 + (x * 0.06298012)} {0.4703186}"
                        }
                    }
                });
            }

            ui.Add(new CuiLabel
            {
                Text =
                {
                    Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 0.2",
                    FadeIn = 0.4f,
                    FontSize = 14,
                    Text = "BELT"
                },
                RectTransform =
                {
                    AnchorMin = "0.01689652 0.2796934",
                    AnchorMax = "0.1397844 0.3265693"
                }
            }, $"{UiMain}.Kit.Info.BG");

            for (int i = 0, x = 0; i <= 5; i++, x++)
            {
                ui.Add(new CuiElement
                {
                    Name = $"{UiMain}.Kit.Info.Item.BG.{i}.3",
                    Parent = $"{UiMain}.Kit.Info.BG",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0.38 0.38 0.38 0.6",
                            FadeIn = 0.1f * i
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.0168965 + (x * 0.06298012)} {0.1500031}",
                            AnchorMax = $"{0.0745002 + (x * 0.06298012)} {0.2671928}"
                        }
                    }
                });
            }

            #endregion

            #region [Kit-Items]

            var nummain = 0;
            foreach (var item in kitobj.Main)
            {
                ui.Add(new CuiElement
                {
                    Name = $"{UiMain}.Kit.Info.Item.Icon.{nummain}.1",
                    Parent = $"{UiMain}.Kit.Info.Item.BG.{nummain}.1",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage(item.ShortName),
                            FadeIn = 0.1f * nummain
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.1 0.1",
                            AnchorMax = "0.9 0.9"
                        }
                    }
                });

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleRight,
                        Color = "1 1 1 1",
                        FadeIn = 0.4f,
                        FontSize = 11,
                        Text = $"x{item.Amount}"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "0.9 0.35"
                    }
                }, $"{UiMain}.Kit.Info.Item.BG.{nummain}.1");

                nummain++;
            }

            var numwear = 0;
            foreach (var item in kitobj.Wear)
            {
                ui.Add(new CuiElement
                {
                    Name = $"{UiMain}.Kit.Info.Item.Icon.{numwear}.2",
                    Parent = $"{UiMain}.Kit.Info.Item.BG.{numwear}.2",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage(item.ShortName),
                            FadeIn = 0.1f * numwear
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.1 0.1",
                            AnchorMax = "0.9 0.9"
                        }
                    }
                });

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleRight,
                        Color = "1 1 1 1",
                        FadeIn = 0.4f,
                        FontSize = 11,
                        Text = $"x{item.Amount}"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "0.9 0.35"
                    }
                }, $"{UiMain}.Kit.Info.Item.BG.{numwear}.2");

                numwear++;
            }

            var numbelt = 0;
            foreach (var item in kitobj.Belt)
            {
                ui.Add(new CuiElement
                {
                    Name = $"{UiMain}.Kit.Info.Item.Icon.{numbelt}.3",
                    Parent = $"{UiMain}.Kit.Info.Item.BG.{numbelt}.3",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage(item.ShortName),
                            FadeIn = 0.1f * numbelt
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.1 0.1",
                            AnchorMax = "0.9 0.9"
                        }
                    }
                });

                ui.Add(new CuiLabel
                {
                    Text =
                    {
                        Align = TextAnchor.MiddleRight,
                        Color = "1 1 1 1",
                        FadeIn = 0.4f,
                        FontSize = 11,
                        Text = $"x{item.Amount}"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "0.9 0.35"
                    }
                }, $"{UiMain}.Kit.Info.Item.BG.{numbelt}.3");

                numbelt++;
            }

            #endregion

            #region [Kit-Info]

            ui.Add(new CuiLabel
            {
                Text =
                {
                    Align = TextAnchor.UpperLeft,
                    Color = "1 1 1 1",
                    FadeIn = 0.4f,
                    FontSize = 20,
                    Text = "<size=14><color=#6e6e6e>CURRENT KIT</color></size>\n" +
                           $"<color=#b9590d>{kitname.ToUpper()}</color>\n\n" +
                           "<size=14><color=#6e6e6e>DESCRIPTION</color></size>\n" +
                           $"<color=#b9590d>{(kitobj.Description == "" ? "Empty" : kitobj.Description)}</color>"
                },
                RectTransform =
                {
                    AnchorMin = "0.6 0.6156251",
                    AnchorMax = "1 0.9765628"
                }
            }, UiKitView);

            ui.Add(new CuiLabel
            {
                Text =
                {
                    Align = TextAnchor.UpperLeft,
                    Color = "1 1 1 1",
                    FadeIn = 0.4f,
                    FontSize = 20,
                    Text = "<size=14><color=#6e6e6e>COOLDOWN</color></size>\n" +
                           $"<color=#b9590d>{ConvertTime(kitobj.CoolDown)}</color>"
                },
                RectTransform =
                {
                    AnchorMin = "0.6 0.1640625",
                    AnchorMax = "1 0.6156251"
                }
            }, UiKitView);

            ui.Add(new CuiButton
            {
                Button =
                {
                    Color = "0.73 0.35 0.05 1.00",
                    Command = $"kit.give {kitname}",
                    FadeIn = 0.4f,
                    Material = Sharp
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.5",
                    FontSize = 15,
                    Text = !IsCooldown(player, kitname) ? "REEDEM" : "LOCKED"
                },
                RectTransform =
                {
                    AnchorMin = "0.6 0.04687499",
                    AnchorMax = "0.9769585 0.1296875"
                }
            }, UiKitView);

            #endregion

            CuiHelper.DestroyUi(player, $"{UiMain}.Kit.Info.BG");
            CuiHelper.AddUi(player, ui);
        }

        #endregion

        #region [Hooks]

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            LoadData();
            // ServerMgr.Instance.StartCoroutine(LoadImages());
        }

        // ReSharper disable once UnusedMember.Local
        private void Unload()
        {
            SaveData();
        }

        #endregion

        #region [Commands]

        [ConsoleCommand("Kit")]
        // ReSharper disable once UnusedMember.Local
        private void OpenKits(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            UI_Main(args.Player());
        }
        
        [ChatCommand("Kit")]
        // ReSharper disable once UnusedMember.Local
        private void OpenKitsCH(BasePlayer player)
        {
            if (player == null) return;
            UI_Main(player);
        }

        [ConsoleCommand("kit.view")]
        // ReSharper disable once UnusedMember.Local
        private void ViewKit(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            if (args.Args.Length < 1) return;
            if (!_dataBase.Kits.Exists(x => x.Name == args.Args[0].ToLower()))
            {
                SendReply(args.Player(), "Kit not found");
                return;
            }

            UI_KitView(args.Player(), args.Args[0]);
        }

        [ChatCommand("Kit.add")]
        // ReSharper disable once UnusedMember.Local
        private void AddKits(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            
            if (args.Length < 1)
            {
                SendReply(player, "Enter the title");
                return;
            }

            var kitname = args[0].ToLower();
            if (_dataBase.Kits.Exists(x => x.Name.ToLower() == kitname))
            {
                SendReply(player, $"Kit {kitname.ToUpper()} already created");
                return;
            }

            AddKit(player, kitname);
        }

        [ConsoleCommand("kit.give")]
        // ReSharper disable once UnusedMember.Local
        private void GiveKits(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            if (args.Args.Length < 1)
            {
                SendReply(args.Player(), "Enter the title");
                return;
            }

            GiveKit(args.Player(), args.Args[0]);
        }

        [ChatCommand("kit.remove")]
        // ReSharper disable once UnusedMember.Local
        private void RemoveKits(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            if (!player.IsAdmin) return;
            if (args.Length < 1)
            {
                SendReply(player, "Enter the title");
                return;
            }

            RemoveKit(player, args[0]);
        }


        [ConsoleCommand("kit.close")]
        // ReSharper disable once UnusedMember.Local
        private void CloseKits(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            DestroyMainUi(args.Player());
        }

        #endregion

        #region [DataBase]

        public class StoredData
        {
            public readonly List<Kit> Kits = new List<Kit>();

            public readonly Dictionary<ulong, List<KitCooldown>> KitCooldowns =
                new Dictionary<ulong, List<KitCooldown>>();

            public class Kit
            {
                public string Name;
                public string DisplayName;
                public readonly string Description = "";
                public readonly int CoolDown = 86400;
                public int Uses = 0;
                public string Permission;
                public List<Item> Main = new List<Item>();
                public List<Item> Wear = new List<Item>();
                public List<Item> Belt = new List<Item>();
            }

            public class KitCooldown
            {
                public string KitName;
                public double Cooldown;
            }

            public class Item
            {
                public string ShortName;
                public int Amount;
            }
        }

        #endregion

        #region [Helpers]

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        private static double CurrentTime() => DateTime.UtcNow.Subtract(Epoch).TotalSeconds;

        private bool IsCooldown(BasePlayer player, string kitname)
        {
            if (!_dataBase.KitCooldowns.ContainsKey(player.userID))
                _dataBase.KitCooldowns.Add(player.userID, new List<StoredData.KitCooldown>());

            var playercooldown = _dataBase.KitCooldowns[player.userID];
            if (!playercooldown.Exists(x => x.KitName == kitname.ToLower())) return false;
            {
                var cooldown = playercooldown.Find(x => x.KitName == kitname);
                if (0 < (cooldown.Cooldown - CurrentTime()))
                {
                    return true;
                }
                else playercooldown.Remove(cooldown);
            }

            return false;
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

        private static string ConvertTime(int seconds)
        {
            var time = TimeSpan.FromSeconds(seconds);
            return $"{time.Days}D {time.Hours}H {time.Minutes}M {time.Seconds}S";
        }

        private void AddKit(BasePlayer player, string name)
        {
            var kit = new StoredData.Kit
            {
                Name = name,
                DisplayName = name,
                Permission = $"kits.{name}",
                Main = CloneContainerMain(player),
                Belt = CloneContainerBelt(player),
                Wear = CloneContainerWear(player)
            };
            _dataBase.Kits.Add(kit);
            permission.RegisterPermission($"kits.{name}", this);
            SaveData();
            SendReply(player, $"Kit {name.ToUpper()} successfully created");
        }

        private void GiveKit(BasePlayer player, string kitname)
        {
            var kit = _dataBase.Kits.Find(x => x.Name == kitname);
            if (kit == null)
            {
                SendReply(player, "Kit not found");
                return;
            }

            if (!permission.UserHasPermission(player.UserIDString, $"kits.{kitname}"))
            {
                SendReply(player, "You do not have access to this kit");
                return;
            }


            if (!_dataBase.KitCooldowns.ContainsKey(player.userID))
                _dataBase.KitCooldowns.Add(player.userID, new List<StoredData.KitCooldown>());

            var playercooldown = _dataBase.KitCooldowns[player.userID];
            if (playercooldown.Exists(x => x.KitName == kitname.ToLower()))
            {
                var cooldown = playercooldown.Find(x => x.KitName == kitname);
                if (0 < (cooldown.Cooldown - CurrentTime()))
                {
                    var time = TimeSpan.FromSeconds(cooldown.Cooldown - CurrentTime());
                    SendReply(player,
                        $"You will be able to take this Kit through {time.Days}:{time.Hours}:{time.Minutes}:{time.Seconds}");
                    return;
                }
                else playercooldown.Remove(cooldown);
            }

            foreach (var giveitem in kit.Main.Select(item => ItemManager.CreateByName(item.ShortName, item.Amount)))
            {
                GiveItem(player.inventory, giveitem);
            }

            foreach (var giveitem in kit.Wear.Select(item => ItemManager.CreateByName(item.ShortName, item.Amount)))
            {
                GiveItem(player.inventory, giveitem);
            }

            foreach (var giveitem in kit.Belt.Select(item => ItemManager.CreateByName(item.ShortName, item.Amount)))
            {
                GiveItem(player.inventory, giveitem);
            }

            playercooldown.Add(new StoredData.KitCooldown
            {
                KitName = kitname,
                Cooldown = CurrentTime() + kit.CoolDown
            });
            SendReply(player, $"You have been given kit {kitname.ToUpper()}");
            SaveData();
        }

        private void RemoveKit(BasePlayer player, string kitname)
        {
            if (!_dataBase.Kits.Exists(x => x.Name == kitname))
            {
                SendReply(player, "Kit not found");
                return;
            }

            var kit = _dataBase.Kits.Find(x => x.Name == kitname);
            _dataBase.Kits.Remove(kit);
            SendReply(player, $"Kit {kitname.ToUpper()} deleted");
        }

        private static List<StoredData.Item> CloneContainerMain(BasePlayer player)
        {
            return player.inventory.containerMain.itemList.Select(item => new StoredData.Item {ShortName = item.info.shortname, Amount = item.amount}).ToList();
        }

        private static void GiveItem(PlayerInventory inv, Item item)
        {
            if (item == null) return;
            var a = item.MoveToContainer(inv.containerBelt) ||
                    item.MoveToContainer(inv.containerWear) || item.MoveToContainer(inv.containerMain);
        }

        private static List<StoredData.Item> CloneContainerWear(BasePlayer player)
        {
            return player.inventory.containerWear.itemList.Select(item => new StoredData.Item {ShortName = item.info.shortname, Amount = item.amount}).ToList();
        }

        private static List<StoredData.Item> CloneContainerBelt(BasePlayer player)
        {
            return player.inventory.containerBelt.itemList.Select(item => new StoredData.Item {ShortName = item.info.shortname, Amount = item.amount}).ToList();
        }

        private string GetImage(string image)
        {
            return (string) ImageLibrary.Call("GetImage", image);
        }

        private static void DestroyMainUi(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, $"{UiMain}.Background");
        }

        #endregion
    }
}