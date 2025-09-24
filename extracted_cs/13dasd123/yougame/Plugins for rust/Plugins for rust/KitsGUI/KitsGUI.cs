// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Globalization;
using Oxide.Core;
using System.IO;
//https://vk.com/mr.gr1me
namespace Oxide.Plugins
{
    [Info("KitsGUI", "RustPlugin.ru", "1.0.3")]
    class KitsGUI : RustPlugin
    {
        private PluginConfig _config;
        private ImagesCache _imagesCache = new ImagesCache();
        private List<Kit> _kits;
        private Dictionary<ulong, Dictionary<string, KitData>> _kitsData;
        private Dictionary<BasePlayer, List<string>> _kitsGUI = new Dictionary<BasePlayer, List<string>>();

        #region Classes

        class PluginConfig
        {
            public Position Position { get; set; }
            public string MainBackgroundColor { get; set; }

            public string DefaultKitImage { get; set; }
            public float MarginTop { get; set; }
            public float MarginBottom { get; set; }
            public float MarginBetween { get; set; }
            public float KitWidth { get; set; }
            public string DisableMaskColor { get; set; }
            public string KitBackgroundColor { get; set; }
            public ImageConfig Image { get; set; }
            public LabelConfig Label { get; set; }
            public LabelConfig Amount { get; set; }
            public LabelConfig Time { get; set; }

            public static PluginConfig CreateDefault()
            {
                return new PluginConfig
                {
                    MainBackgroundColor = "#00000088",
                    Position = new Position
                    {
                        AnchorMin = "0 0.35",
                        AnchorMax = "1 0.65"
                    },

                    DefaultKitImage = "file://" + Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar + "KitsGUI" + Path.DirectorySeparatorChar + "rust.png",
                    KitWidth = 0.12f,
                    MarginTop = 0.04f,
                    MarginBottom = 0.03f,
                    DisableMaskColor = "#000000DD",
                    MarginBetween = 0.01f,
                    KitBackgroundColor = "#0E4F1BFF",

                    Image = new ImageConfig
                    {
                        Color = "#FFFFFFFF",
                        Position = new Position
                        {
                            AnchorMin = "0.05 0.15",
                            AnchorMax = "0.95 0.95"
                        },
                        Png = ""
                    },
                    Label = new LabelConfig
                    {
                        Position = new Position
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 0.15"
                        },
                        FontSize = 14,
                        ForegroundColor = "#FFFFFFFF",
                        BackgroundColor = "#00000000",
                        TextAnchor = TextAnchor.MiddleCenter
                    },
                    Amount = new LabelConfig
                    {
                        Position = new Position
                        {
                            AnchorMin = "0.05 0.85",
                            AnchorMax = "0.95 0.95"
                        },
                        FontSize = 14,
                        ForegroundColor = "#FFFFFFFF",
                        BackgroundColor = "#00000000",
                        TextAnchor = TextAnchor.MiddleCenter
                    },
                    Time = new LabelConfig
                    {
                        Position = new Position
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        },
                        FontSize = 14,
                        ForegroundColor = "#FFFFFFFF",
                        BackgroundColor = "#00000000",
                        TextAnchor = TextAnchor.MiddleCenter
                    }
                };
            }

            public class LabelConfig
            {
                public Position Position { get; set; }
                public string ForegroundColor { get; set; }
                public string BackgroundColor { get; set; }
                public int FontSize { get; set; }
                public TextAnchor TextAnchor { get; set; }
            }

            public class ImageConfig
            {
                public Position Position { get; set; }
                public string Color { get; set; }
                public string Png { get; set; }
            }
        }

        public class Kit
        {
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public int Amount { get; set; }
            public double Cooldown { get; set; }
            public bool Hide { get; set; }
            public string Group { get; set; }
            public List<KitItem> Items { get; set; }

            public string Png { get; set; }
        }

        public class KitItem
        {
            public string ShortName { get; set; }
            public int Amount { get; set; }
            public ulong Skin { get; set; }
        }

        public class KitData
        {
            public int Amount { get; set; }
            public double Cooldown { get; set; }
        }

        public class Position
        {
            public string AnchorMin { get; set; }
            public string AnchorMax { get; set; }
        }

        public class ImagesCache : MonoBehaviour
        {
            private Dictionary<string, string> _images = new Dictionary<string, string>();

            public void Add(string name, string url)
            {
                if (_images.ContainsKey(name))
                    return;

                using (var www = new WWW(url))
                {
                    if (www.error != null)
                    {
                        print(string.Format("Image loading fail! Error: {0}", www.error));
                    }
                    else
                    {
                        var stream = new MemoryStream();
                        stream.Write(www.bytes, 0, www.bytes.Length);

                        _images[name] = FileStorage.server.Store(stream, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString();
                    }
                }
            }

            public string Get(string name)
            {
                if (_images.ContainsKey(name))
                    return _images[name];

                return string.Empty;
            }
        }

        #endregion

        #region Oxide hooks

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            Config.WriteObject(PluginConfig.CreateDefault(), true);
            PrintWarning("Default configuration file created.");
        }

        private void SaveKits()
        {
            Interface.Oxide.DataFileSystem.WriteObject("KitsGUI", _kits);
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("KitsGUI_Data", _kitsData);
        }

        private void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Kit Was Removed"] = "<color=#008B8B>[Сервер]:</color> {kitname} был удалён",
                ["Kit Doesn't Exist"] = "<color=#008B8B>[Сервер]:</color> Этого комплекта не существует",
                ["Not Found Player"] = "<color=#008B8B>[Сервер]:</color> Этого комплекта не существует",
                ["To Many Player"] = "<color=#008B8B>[Сервер]:</color> Этого комплекта не существует",
                ["Group Denied"] = "<color=#008B8B>[Сервер]:</color> У вас нет полномочий использовать этот комплект",
                ["Limite Denied"] = "<color=#008B8B>[Сервер]:</color> Вы уже использовали этот комплект максимальное количество раз",
                ["Cooldown Denied"] = "<color=#008B8B>[Сервер]:</color> Вы сможете использовать этот комплект через {time}",
                ["Reset"] = "<color=#008B8B>[Сервер]:</color> Вы обнулили все данные о использовании комплектов игроков",
                ["Kit Already Exist"] = "<color=#008B8B>[Сервер]:</color> Этот набор уже существует",
                ["Kit Created"] = "<color=#008B8B>[Сервер]:</color> Вы создали новый набор - {name}",
                ["Kit Extradited"] = "<color=#008B8B>[Сервер]:</color> Вы получили комплект {kitname}",
                ["Kit Cloned"] = "<color=#008B8B>[Сервер]:</color> Предметы были скопированы из инвентаря",
                ["UI Amount"] = "Осталось: {amount}",
                ["Help"] = "/kit name|add|clone|remove|list|reset",
                ["Help Add"] = "/kit add <kitname>",
                ["Help Clone"] = "/kit clone <kitname>",
                ["Help Remove"] = "/kit remove <kitname>",
                ["Help Give"] = "/kit give <playerName|steamID> <kitname>"
            }, this);
        }

        private void Loaded()
        {
            _config = Config.ReadObject<PluginConfig>();
            _kits = Interface.Oxide.DataFileSystem.ReadObject<List<Kit>>("KitsGUI");
            _kitsData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Dictionary<string, KitData>>>("KitsGUI_Data");

            LoadMessages();

            foreach (var player in BasePlayer.activePlayerList)
            {
                player.Command("bind k \"kit ui\"");
            }
        }

        private void Unload()
        {
            SaveData();

            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyUI(player);
                player.Command("bind k \"\"");
            }
        }

        private void OnServerInitialized()
        {
            foreach (var kit in _kits)
            {
                _imagesCache.Add(kit.Name, kit.Png);
            }

            timer.Repeat(1, 0, RefreshCooldownKitsUI);
        }

        private void OnPlayerInit(BasePlayer player)
        {
			player.Command("bind k \"kit ui\"");
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            _kitsGUI.Remove(player);
            player.Command("bind k \"\"");
        }

        #endregion

        #region Commands

        [ConsoleCommand("kit")]
        private void CommandConsoleKit(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null)
                return;

            var player = arg.Player();

            if (!arg.HasArgs())
                return;

            var value = arg.Args[0].ToLower();

            if (value == "ui")
            {
                TriggerUI(player);
                return;
            }

            if (!_kitsGUI.ContainsKey(player))
                return;

            if (!_kitsGUI[player].Contains(value))
                return;

            GiveKit(player, value);

            var container = new CuiElementContainer();
            var kit = _kits.First(x => x.Name == value);
            var playerData = GetPlayerData(player.userID, value);

            if (kit.Amount > 0)
            {
                if (playerData.Amount >= kit.Amount)
                {
                    foreach (var kitname in _kitsGUI[player])
                    {
                        CuiHelper.DestroyUi(player, $"ui.KitsGUI.{kitname}.button");
                        CuiHelper.DestroyUi(player, $"ui.KitsGUI.{kitname}.amount");
                        CuiHelper.DestroyUi(player, $"ui.KitsGUI.{kitname}");
                    }

                    InitilizeKitsUI(ref container, player);
                    CuiHelper.AddUi(player, container);
                    return;
                }

                CuiHelper.DestroyUi(player, $"ui.KitsGUI.{value}.amount");
                InitilizeAmountLabelUI(ref container, value, lang.GetMessage("UI Amount", this).Replace("{amount}", (kit.Amount - playerData.Amount).ToString()));
            }

            if (kit.Cooldown > 0)
            {
                var currentTime = GetCurrentTime();
                if (playerData.Cooldown > currentTime)
                {
                    CuiHelper.DestroyUi(player, $"ui.KitsGUI.{value}.button");

                    InitilizeMaskUI(ref container, kit.Name);
                    InitilizeCooldownLabelUI(ref container, value, TimeSpan.FromSeconds(playerData.Cooldown - currentTime));
                }
            }

            CuiHelper.AddUi(player, container);

            return;
        }

        [ChatCommand("kit")]
        private void CommandChatKit(BasePlayer player, string command, string[] args)
        {
            if (player == null)
                return;

            if (args.Length == 0)
            {
                foreach (var kit in GetKitsForPlayer(player))
                    SendReply(player, $"{kit.Name} - {kit.DisplayName}");
                return;
            }

            if (!player.IsAdmin){
				GiveKit(player, args[0].ToLower());
				return;
			}

            switch (args[0].ToLower())
            {
                case "help":
                    SendReply(player, lang.GetMessage("Help", this));
                    return;
                case "add":
                    if (args.Length < 2)
                        SendReply(player, lang.GetMessage("Help Add", this));
                    else
                        KitCommandAdd(player, args[1].ToLower());
                    return;
                case "clone":
                    if (args.Length < 2)
                        SendReply(player, lang.GetMessage("Help Clone", this));
                    else
                        KitCommandClone(player, args[1].ToLower());
                    return;
                case "remove":
                    if (args.Length < 2)
                        SendReply(player, lang.GetMessage("Help Remove", this));
                    else
                        KitCommandRemove(player, args[1].ToLower());
                    return;
                case "list":
                    KitCommandList(player);
                    return;
                case "reset":
                    KitCommandReset(player);
                    return;
                case "give":
                    if (args.Length < 3)
                    {
                        SendReply(player, lang.GetMessage("Help Give", this));
                    }
                    else
                    {
                        var foundPlayer = FindPlayer(player, args[1].ToLower());
                        if (foundPlayer == null)
                            return;

                        KitCommandGive(player, foundPlayer, args[2].ToLower());
                    }
                    return;
                default:
                    GiveKit(player, args[0].ToLower());
                    return;
            }
        }

        #endregion

        #region Kits

        private bool GiveKit(BasePlayer player, string kitname)
        {
            if (string.IsNullOrEmpty(kitname))
                return false;

            if (!_kits.Exists(x => x.Name == kitname))
            {
                SendReply(player, lang.GetMessage("Kit Doesn't Exist", this));
                return false;
            }

            var kit = _kits.First(x => x.Name == kitname);

            if (!string.IsNullOrEmpty(kit.Group) && !permission.UserHasGroup(player.UserIDString, kit.Group))
            {
                SendReply(player, lang.GetMessage("Group Denied", this));
                return false;
            }

            var playerData = GetPlayerData(player.userID, kitname);

            if (kit.Amount > 0 && playerData.Amount >= kit.Amount)
            {
                SendReply(player, lang.GetMessage("Limite Denied", this));
                return false;
            }

            if (kit.Cooldown > 0)
            {
                var currentTime = GetCurrentTime();
                if (playerData.Cooldown > currentTime)
                {
                    SendReply(player, lang.GetMessage("Cooldown Denied", this).Replace("{time}", TimeExtensions.FormatTime(TimeSpan.FromSeconds(playerData.Cooldown - currentTime))));
                    return false;
                }
            }

            foreach (var item in kit.Items)
                player.GiveItem(ItemManager.CreateByName(item.ShortName, item.Amount, item.Skin));

            if (kit.Amount > 0)
                playerData.Amount += 1;

            if (kit.Cooldown > 0)
                playerData.Cooldown = GetCurrentTime() + kit.Cooldown;

            SendReply(player, lang.GetMessage("Kit Extradited", this).Replace("{kitname}", kit.DisplayName));
            return true;
        }

        private void KitCommandAdd(BasePlayer player, string kitname)
        {
            if (_kits.Exists(x => x.Name == kitname))
            {
                SendReply(player, lang.GetMessage("Kit Already Exist", this));
                return;
            }

            _kits.Add(new Kit
            {
                Name = kitname,
                DisplayName = kitname,
                Cooldown = 600,
                Hide = true,
                Group = "",
                Amount = 0,
                Png = _config.DefaultKitImage,
                Items = GetPlayerItems(player)
            });

            SendReply(player, lang.GetMessage("Kit Created", this).Replace("{name}", kitname));

            SaveKits();
        }

        private void KitCommandClone(BasePlayer player, string kitname)
        {
            if (!_kits.Exists(x => x.Name == kitname))
            {
                SendReply(player, lang.GetMessage("Kit Doesn't Exist", this));
                return;
            }

            _kits.First(x => x.Name == kitname).Items = GetPlayerItems(player);

            SendReply(player, lang.GetMessage("Kit Cloned", this).Replace("{name}", kitname));

            SaveKits();
        }

        private void KitCommandRemove(BasePlayer player, string kitname)
        {
            if (_kits.RemoveAll(x => x.Name == kitname) <= 0)
            {
                SendReply(player, lang.GetMessage("Kit Doesn't Exist", this));
                return;
            }

            SendReply(player, lang.GetMessage("Kit Was Removed", this).Replace("{kitname}", kitname));

            SaveKits();
        }

        private void KitCommandList(BasePlayer player)
        {
            foreach (var kit in _kits)
                SendReply(player, $"{kit.Name} - {kit.DisplayName}");
        }

        private void KitCommandReset(BasePlayer player)
        {
            _kitsData.Clear();

            SendReply(player, lang.GetMessage("Reset", this));
        }

        private void KitCommandGive(BasePlayer player, BasePlayer foundPlayer, string kitname)
        {
            if (!_kits.Exists(x => x.Name == kitname))
            {
                SendReply(player, lang.GetMessage("Kit Doesn't Exist", this));
                return;
            }
            
            foreach (var item in _kits.First(x => x.Name == kitname).Items)
                player.GiveItem(ItemManager.CreateByName(item.ShortName, item.Amount, item.Skin));
        }

        #endregion

        #region UI

        private void TriggerUI(BasePlayer player)
        {
            if (_kitsGUI.ContainsKey(player))
                DestroyUI(player);
            else
                InitilizeUI(player);
        }

        private void InitilizeUI(BasePlayer player)
        {
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                Image = { Color = HexToRustFormat(_config.MainBackgroundColor) },
                RectTransform = { AnchorMin = _config.Position.AnchorMin, AnchorMax = _config.Position.AnchorMax },
                CursorEnabled = true
            }, name: "ui.KitsGUI");

            InitilizeKitsUI(ref container, player);

            CuiHelper.AddUi(player, container);
        }

        private void DestroyUI(BasePlayer player)
        {
            if (!_kitsGUI.ContainsKey(player))
                return;

            foreach (var kitname in _kitsGUI[player])
            {
                CuiHelper.DestroyUi(player, $"ui.KitsGUI.{kitname}.time");
                CuiHelper.DestroyUi(player, $"ui.KitsGUI.{kitname}.mask");
                CuiHelper.DestroyUi(player, $"ui.KitsGUI.{kitname}.button");
                CuiHelper.DestroyUi(player, $"ui.KitsGUI.{kitname}.amount");
                CuiHelper.DestroyUi(player, $"ui.KitsGUI.{kitname}");
            }
            CuiHelper.DestroyUi(player, "ui.KitsGUI");

            _kitsGUI.Remove(player);
        }

        private void RefreshCooldownKitsUI()
        {
            var currentTime = GetCurrentTime();

            foreach (var playerGUIData in _kitsGUI)
            {
                var container = new CuiElementContainer();
                var playerKitsData = _kitsData[playerGUIData.Key.userID];

                foreach (var kitname in playerGUIData.Value)
                {
                    var playerKitData = playerKitsData[kitname];

                    if (playerKitData.Cooldown > 0)
                    {
                        CuiHelper.DestroyUi(playerGUIData.Key, $"ui.KitsGUI.{kitname}.time");

                        if (playerKitData.Cooldown < currentTime)
                        {
                            CuiHelper.DestroyUi(playerGUIData.Key, $"ui.KitsGUI.{kitname}.mask");

                            InitilizeButtonUI(ref container, kitname);
                        }
                        else
                        {
                            InitilizeCooldownLabelUI(ref container, kitname, TimeSpan.FromSeconds(playerKitData.Cooldown - currentTime));
                        }
                    }
                }

                CuiHelper.AddUi(playerGUIData.Key, container);
            }
        }

        private void InitilizeKitsUI(ref CuiElementContainer container, BasePlayer player)
        {
            _kitsGUI[player] = new List<string>();
            var currentTime = GetCurrentTime();
            var kits = GetKitsForPlayer(player).Take((int)(1.0f / (_config.KitWidth + _config.MarginBetween))).ToList();
            var pos = 0.5f - (kits.Count * _config.KitWidth + (kits.Count - 1) * _config.MarginBetween) / 2;

            foreach (var kit in kits)
            {
                _kitsGUI[player].Add(kit.Name);

                var playerData = GetPlayerData(player.userID, kit.Name);

                // Kit panel
                container.Add(new CuiPanel
                {
                    Image = { Color = HexToRustFormat(_config.KitBackgroundColor) },
                    RectTransform = { AnchorMin = $"{pos} {_config.MarginBottom}", AnchorMax = $"{pos + _config.KitWidth} {1.0f - _config.MarginTop}" }
                }, "ui.KitsGUI", $"ui.KitsGUI.{kit.Name}");

                pos += _config.KitWidth + _config.MarginBetween;

                InitilizeNameLabelUI(ref container, kit.Name, kit.DisplayName);

                InitilizeKitImageUI(ref container, kit.Name);

                if (kit.Amount > 0)
                {
                    InitilizeAmountLabelUI(ref container, kit.Name, lang.GetMessage("UI Amount", this).Replace("{amount}", (kit.Amount - playerData.Amount).ToString()));
                }

                if (kit.Cooldown > 0 && (playerData.Cooldown > currentTime))
                {
                    InitilizeMaskUI(ref container, kit.Name);
                    InitilizeCooldownLabelUI(ref container, kit.Name, TimeSpan.FromSeconds(playerData.Cooldown - currentTime));
                }
                else
                {
                    InitilizeButtonUI(ref container, kit.Name);
                }
            }
        }

        private void InitilizeKitImageUI(ref CuiElementContainer container, string kitname)
        {
            container.Add(new CuiElement
            {
                Parent = $"ui.KitsGUI.{kitname}",
                Components =
                {
                    new CuiRawImageComponent { Png = _imagesCache.Get(kitname), Sprite = "assets/content/textures/generic/fulltransparent.tga", Color = HexToRustFormat(_config.Image.Color) },
                    new CuiRectTransformComponent {AnchorMin = _config.Image.Position.AnchorMin, AnchorMax = _config.Image.Position.AnchorMax }
                }
            });
        }

        private void InitilizeNameLabelUI(ref CuiElementContainer container, string kitname, string text)
        {
            var name = container.Add(new CuiPanel
            {
                Image = { Color = HexToRustFormat(_config.Label.BackgroundColor) },
                RectTransform = { AnchorMin = _config.Label.Position.AnchorMin, AnchorMax = _config.Label.Position.AnchorMax }
            }, $"ui.KitsGUI.{kitname}");

            container.Add(new CuiLabel
            {
                Text = { Color = HexToRustFormat(_config.Label.ForegroundColor), FontSize = _config.Label.FontSize, Align = _config.Label.TextAnchor, Text = text }
            }, name);
        }

        private void InitilizeMaskUI(ref CuiElementContainer container, string kitname)
        {
            var name = container.Add(new CuiPanel
            {
                Image = { Color = HexToRustFormat(_config.DisableMaskColor) }
            }, $"ui.KitsGUI.{kitname}", $"ui.KitsGUI.{kitname}.mask");
        }

        private void InitilizeAmountLabelUI(ref CuiElementContainer container, string kitname, string text)
        {
            container.Add(new CuiPanel
            {
                Image = { Color = HexToRustFormat(_config.Amount.BackgroundColor) },
                RectTransform = { AnchorMin = _config.Amount.Position.AnchorMin, AnchorMax = _config.Amount.Position.AnchorMax }
            }, $"ui.KitsGUI.{kitname}", $"ui.KitsGUI.{kitname}.amount");

            container.Add(new CuiLabel
            {
                Text = { Color = HexToRustFormat(_config.Amount.ForegroundColor), FontSize = _config.Amount.FontSize, Align = _config.Amount.TextAnchor, Text = text }
            }, $"ui.KitsGUI.{kitname}.amount");
        }

        private void InitilizeButtonUI(ref CuiElementContainer container, string kitname)
        {
            container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0", Command = $"kit {kitname}" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = "" }
            }, $"ui.KitsGUI.{kitname}", $"ui.KitsGUI.{kitname}.button");
        }

        private void InitilizeCooldownLabelUI(ref CuiElementContainer container, string kitname, TimeSpan time)
        {
            container.Add(new CuiPanel
            {
                Image = { Color = HexToRustFormat(_config.Time.BackgroundColor) },
                RectTransform = { AnchorMin = _config.Time.Position.AnchorMin, AnchorMax = _config.Time.Position.AnchorMax }
            }, $"ui.KitsGUI.{kitname}", $"ui.KitsGUI.{kitname}.time");

            container.Add(new CuiLabel
            {
                Text = { Color = HexToRustFormat(_config.Time.ForegroundColor), FontSize = _config.Time.FontSize, Align = _config.Time.TextAnchor, Text = TimeExtensions.FormatShortTime(time) }
            }, $"ui.KitsGUI.{kitname}.time");
        }

        #endregion

        #region Helpers 

        private KitData GetPlayerData(ulong userID, string name)
        {
            if (!_kitsData.ContainsKey(userID))
                _kitsData[userID] = new Dictionary<string, KitData>();

            if (!_kitsData[userID].ContainsKey(name))
                _kitsData[userID][name] = new KitData();

            return _kitsData[userID][name];
        }

        private List<KitItem> GetPlayerItems(BasePlayer player)
        {
            return player.inventory.AllItems().Select(x => new KitItem
            {
                ShortName = x.info.shortname,
                Amount = x.amount,
                Skin = x.skin
            }).ToList();
        }

        private List<Kit> GetKitsForPlayer(BasePlayer player)
        {
            return _kits.Where(kit => kit.Hide == false && (string.IsNullOrEmpty(kit.Group) || permission.UserHasGroup(player.UserIDString, kit.Group)) && (kit.Amount == 0 || (kit.Amount > 0 && GetPlayerData(player.userID, kit.Name).Amount < kit.Amount))).ToList();
        }

        private BasePlayer FindPlayer(BasePlayer player, string nameOrID)
        {
            ulong id;
            if (ulong.TryParse(nameOrID, out id) && nameOrID.StartsWith("7656119") && nameOrID.Length == 17)
            {
                var findedPlayer = BasePlayer.FindByID(id);
                if (findedPlayer == null || !findedPlayer.IsConnected)
                {
                    SendReply(player, lang.GetMessage("Not Found Player", this));
                    return null;
                }

                return findedPlayer;
            }

            var foundPlayers = BasePlayer.activePlayerList.Where(x => x.displayName.ToLower().Contains(nameOrID.ToLower()));

            if (foundPlayers.Count() == 0)
            {
                SendReply(player, lang.GetMessage("Not Found Player", this));
                return null;
            }

            if (foundPlayers.Count() > 1)
            {
                SendReply(player, lang.GetMessage("To Many Player", this));
                return null;
            }

            return foundPlayers.First();
        }

        private double GetCurrentTime() => new TimeSpan(DateTime.UtcNow.Ticks).TotalSeconds;

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
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);

            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        private static class TimeExtensions
        {
            public static string FormatShortTime(TimeSpan time)
            {
                string result = string.Empty;
                if (time.Days != 0)
                    result += $"{time.Days} д. ";

                if (time.Hours != 0)
                    result += $"{time.Hours} ч. ";

                if (time.Minutes != 0)
                    result += $"{time.Minutes} м. ";

                if (time.Seconds != 0)
                    result += $"{time.Seconds} с. ";

                return result;
            }

            public static string FormatTime(TimeSpan time)
            {
                string result = string.Empty;
                if (time.Days != 0)
                    result += $"{Format(time.Days, "дней", "дня", "день")} ";

                if (time.Hours != 0)
                    result += $"{Format(time.Hours, "часов", "часа", "час")} ";

                if (time.Minutes != 0)
                    result += $"{Format(time.Minutes, "минут", "минуты", "минута")} ";

                if (time.Seconds != 0)
                    result += $"{Format(time.Seconds, "секунд", "секунды", "секунда")} ";

                return result;
            }

            private static string Format(int units, string form1, string form2, string form3)
            {
                var tmp = units % 10;

                if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                    return $"{units} {form1}";

                if (tmp >= 2 && tmp <= 4)
                    return $"{units} {form2}";

                return $"{units} {form3}";
            }
        }

        #endregion
    }
}