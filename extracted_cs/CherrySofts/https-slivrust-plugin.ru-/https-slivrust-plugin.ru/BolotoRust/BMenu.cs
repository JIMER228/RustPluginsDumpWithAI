using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("BMenu", "King", "1.0.0")]
	public class BMenu : RustPlugin
	{
        #region [Vars]
        [PluginReference] Plugin ImageLibrary;
        public string Layer = "BMenu.Layer";
        private readonly List<BasePlayer> MenuUsers = new List<BasePlayer>();
        private readonly List<BasePlayer> MenuUsers2 = new List<BasePlayer>();
        #endregion

        #region [Oxide]
        private void OnServerInitialized()
        {
            if (!ImageLibrary)
            {
                NextTick(() =>
                {
                    PrintError($"ERROR! Plugin ImageLibrary not found!");
                    Interface.Oxide.UnloadPlugin(Name);
                });
                return;
            }
            
            AddCovalenceCommand("menuOpen", nameof(CmdMenu));
            AddCovalenceCommand("menu", nameof(CmdMenu2));
            foreach (var image in config._Images)
                AddImage(image.Key, image.Value);

			foreach (var entity in BaseNetworkable.serverEntities)
				OnEntitySpawned(entity as BaseEntity);
                
            foreach (var player in BasePlayer.activePlayerList)
            {
                InitializeUI(player);
                MenuUsers.Add(player);
            }

			timer.Every(2, () =>
			{
				foreach (var player in BasePlayer.activePlayerList)
				{
					if (player.IsNpc || player.IsSleeping() || player.IsReceivingSnapshot) continue;

                    if (MenuUsers.Contains(player))
					    SupportUI(player, "time");
				}
			});
        }
        private void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
            }
        }
        #endregion

        #region [Rust-Api]
		private void OnPlayerConnected(BasePlayer player)
		{
			if (player.IsReceivingSnapshot || player.IsSleeping())
			{
				timer.In(1, () => OnPlayerConnected(player));
				return;
			}
 
            MenuUsers.Add(player);
			InitializeUI(player);
			UpdateOnline();
		}
		private void OnPlayerDisconnected(BasePlayer player)
		{
            MenuUsers.Remove(player);
			timer.In(1f, UpdateOnline);
		}
		private void OnEntitySpawned(BaseEntity entity)
		{
			EntityHandle(entity, true);
		}
		private void OnEntityKill(BaseEntity entity)
		{
			EntityHandle(entity, false);
		}
        #endregion

        #region [ImageLibrary]
        private bool HasImage(string imageName, ulong imageId = 0) => (bool)ImageLibrary.Call("HasImage", imageName, imageId);
        private bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        private string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary?.Call("GetImage", shortname, skin);
        #endregion

        #region [Ui]
        private void InitializeUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
			var container = new CuiElementContainer
			{
				{
					new CuiPanel {RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1"}, Image = {Color = "0 0 0 0"}},
					"Overlay", Layer
				}
			};
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "4 -32.5", OffsetMax = "36.5 0" },
                Image = { Material = "assets/icons/greyout.mat", Color = "0.8 0.8 0.8 0.125" },
            }, Layer, Layer + "store");
            container.Add(new CuiElement
            {
                Parent = Layer + "store",
                Components =
                {
                    new CuiRawImageComponent { Png = GetImage("store"), Color = "1 1 1 0.9" },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 6", OffsetMax = "-5 -5"},
                    new CuiOutlineComponent{Color = "0 0 0 1", Distance = "0.4 0.4"}
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Color = "0 0 0 0", Command = "chat.say /store" },
                Text = { Text = "" }
            }, Layer + "store");
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "4 -52", OffsetMax = "36.5 -38" },
                Image = { Material = "assets/icons/greyout.mat", Color = "0.8 0.8 0.8 0.125" },
            }, Layer, Layer + "/menu");
            container.Add(new CuiElement
            {
                Parent = Layer + "/menu",
                Components =
                {
                        new CuiTextComponent { Text = $"/MENU", Color = "1 1 1 0.9", Align = TextAnchor.MiddleCenter, FontSize = 11, Font = "robotocondensed-bold.ttf" },
                        new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = $"0.95 1" },
                        new CuiOutlineComponent{Color = "0 0 0 1", Distance = "0.4 0.4"}
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Color = "0 0 0 0", Command = "chat.say /menu" },
                Text = { Text = "" }
            }, Layer + "/menu");
            CuiHelper.AddUi(player, container);
            SupportUI(player, "all");
        }

        private void SupportUI(BasePlayer player, string Type)
        {
            var container = new CuiElementContainer();
            switch(Type)
            {
                case "all":
                    DestroyUI(player);
                    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "42 -15", OffsetMax = "168.5 0" },
                        Image = { Material = "assets/icons/greyout.mat", Color = "0.8 0.8 0.8 0.125" },
                    }, Layer, Layer + "nameServer");
                    container.Add(new CuiElement
                    {
                        Parent = Layer + "nameServer",
                        Components =
                        {
                            new CuiTextComponent { Text = config.ServerName, Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-bold.ttf" },
                            new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                            new CuiOutlineComponent{Color = "0 0 0 1", Distance = "0.4 0.4"}
                        }
                    });
                    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "174 -15", OffsetMax = "189 0" },
                        Image = { Material = "assets/icons/greyout.mat", Color = "0.8 0.8 0.8 0.125" },
                    }, Layer, Layer + "CloseMenu");
                    container.Add(new CuiElement
                    {
                        Parent = Layer + "CloseMenu",
                        Components =
                        {
                            new CuiTextComponent { Text = $"<", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-bold.ttf" },
                            new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = $"0.9 1" },
                            new CuiOutlineComponent{Color = "0 0 0 1", Distance = "0.4 0.4"}
                        }
                    });
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Button = { Color = "0 0 0 0", Command = "chat.say /menuOpen" },
                        Text = { Text = "" }
                    }, Layer + "CloseMenu");
                    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "42 -32.5", OffsetMax = "104 -18.5" },
                        Image = { Material = "assets/icons/greyout.mat", Color = "0.8 0.8 0.8 0.125" },
                    }, Layer, Layer + "playerServer");
                    container.Add(new CuiElement
                    {
                        Parent = Layer + "playerServer",
                        Components =
                        {
                            new CuiRawImageComponent { Png = GetImage("online"), Color = "1 1 1 0.9" },
                            new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "0.2 1", OffsetMin = "1 1.25", OffsetMax = "1 -1.25"},
                            new CuiOutlineComponent{Color = "0 0 0 1", Distance = "0.4 0.4"}
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Name = Layer + "playerServerText",
                        Parent = Layer + "playerServer",
                        Components =
                        {
                            new CuiTextComponent { Text = $"{GetOnline()}/{ConVar.Server.maxplayers}", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-bold.ttf" },
                            new CuiRectTransformComponent { AnchorMin = $"0.22 0", AnchorMax = $"1 0.9" },
                            new CuiOutlineComponent{Color = "0 0 0 1", Distance = "0.4 0.4"}
                        }
                    });
                    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "109 -32.5", OffsetMax = "168.5 -18.5" },
                        Image = { Material = "assets/icons/greyout.mat", Color = "0.8 0.8 0.8 0.125" },
                    }, Layer, Layer + "timeServer");
                    container.Add(new CuiElement
                    {
                        Parent = Layer + "timeServer",
                        Components =
                        {
                            new CuiRawImageComponent { Png = GetImage("time"), Color = "1 1 1 0.9" },
                            new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "0.2 1", OffsetMin = "2.4 1.25", OffsetMax = "2.1 -1.25"},
                            new CuiOutlineComponent{Color = "0 0 0 1", Distance = "0.4 0.4"}
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Name = Layer + "timeServerText",
                        Parent = Layer + "timeServer",
                        Components =
                        {
                            new CuiTextComponent { Text = TOD_Sky.Instance.Cycle.DateTime.ToString("HH:mm"), Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-bold.ttf" },
                            new CuiRectTransformComponent { AnchorMin = $"0.25 0", AnchorMax = $"1 0.9" },
                            new CuiOutlineComponent{Color = "0 0 0 1", Distance = "0.4 0.4"}
                        }
                    });
                    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "42 -52", OffsetMax = "56 -38" },
                        Image = { Material = "assets/icons/greyout.mat", Color = "0.8 0.8 0.8 0.125" },
                    }, Layer, Layer + "patrolHeliCopter");
                    container.Add(new CuiElement
                    {
                        Name = Layer + "patrolHeliCopterImage",
                        Parent = Layer + "patrolHeliCopter",
                        Components =
                        {
                            new CuiRawImageComponent { Png = GetImage("heli"), Color = Events["heli"] },
                            new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "1.7 1.7", OffsetMax = "-1.7 -1.7"},
                            new CuiOutlineComponent{Color = "0 0 0 1", Distance = "0.4 0.4"}
                        }
                    });
                    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "61 -52", OffsetMax = "75 -38" },
                        Image = { Material = "assets/icons/greyout.mat", Color = "0.8 0.8 0.8 0.125" },
                    }, Layer, Layer + "CargoShip");
                    container.Add(new CuiElement
                    {
                        Name = Layer + "CargoShipImage",
                        Parent = Layer + "CargoShip",
                        Components =
                        {
                            new CuiRawImageComponent { Png = GetImage("cargo"), Color = Events["cargo"] },
                            new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "1.7 1.7", OffsetMax = "-1.7 -1.7"},
                            new CuiOutlineComponent{Color = "0 0 0 1", Distance = "0.4 0.4"}
                        }
                    });
                    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "80 -52", OffsetMax = "95 -38" },
                        Image = { Material = "assets/icons/greyout.mat", Color = "0.8 0.8 0.8 0.125" },
                    }, Layer, Layer + "Bradley");
                    container.Add(new CuiElement
                    {
                        Parent = Layer + "Bradley",
                        Components =
                        {
                            new CuiRawImageComponent { Png = GetImage("bradley"), Color = Events["bradley"] },
                            new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "2.5 2.5", OffsetMax = "-2.5 -2.5"},
                            new CuiOutlineComponent{Color = "0 0 0 1", Distance = "0.4 0.4"}
                        }
                    });
                    CuiHelper.AddUi(player, container);
                break;
                case "openMenu":
                    DestroyUI(player);

                    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "42 -15", OffsetMax = "57 0" },
                        Image = { Material = "assets/icons/greyout.mat", Color = "0.8 0.8 0.8 0.125" },
                    }, Layer, Layer + "openMenu");
                    container.Add(new CuiElement
                    {
                        Parent = Layer + "openMenu",
                        Components =
                        {
                            new CuiTextComponent { Text = $">", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-bold.ttf" },
                            new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                            new CuiOutlineComponent{Color = "0 0 0 1", Distance = "0.4 0.4"}
                        }
                    });
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Button = { Color = "0 0 0 0", Command = "chat.say /menuOpen" },
                        Text = { Text = "" }
                    }, Layer + "openMenu");

                    CuiHelper.AddUi(player, container);
                break;
                case "time":
                    CuiHelper.DestroyUi(player, Layer + "timeServerText");
                    container.Add(new CuiElement
                    {
                        Name = Layer + "timeServerText",
                        Parent = Layer + "timeServer",
                        Components =
                        {
                            new CuiTextComponent { Text = TOD_Sky.Instance.Cycle.DateTime.ToString("HH:mm"), Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-bold.ttf" },
                            new CuiRectTransformComponent { AnchorMin = $"0.25 0", AnchorMax = $"1 0.9" },
                            new CuiOutlineComponent{Color = "0 0 0 1", Distance = "0.4 0.4"}
                        }
                    });
                    CuiHelper.AddUi(player, container);
                break;
                case "online":
                    CuiHelper.DestroyUi(player, Layer + "playerServerText");
                    container.Add(new CuiElement
                    {
                        Name = Layer + "playerServerText",
                        Parent = Layer + "playerServer",
                        Components =
                        {
                            new CuiTextComponent { Text = $"{GetOnline()}/{ConVar.Server.maxplayers}", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-bold.ttf" },
                            new CuiRectTransformComponent { AnchorMin = $"0.22 0", AnchorMax = $"1 0.9" },
                            new CuiOutlineComponent{Color = "0 0 0 1", Distance = "0.4 0.4"}
                        }
                    });
                    CuiHelper.AddUi(player, container);
                break;
                case "heli":
                    CuiHelper.DestroyUi(player, Layer + "patrolHeliCopterImage");
                    container.Add(new CuiElement
                    {
                        Name = Layer + "patrolHeliCopterImage",
                        Parent = Layer + "patrolHeliCopter",
                        Components =
                        {
                            new CuiRawImageComponent { Png = GetImage("heli"), Color = Events["heli"] },
                            new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "1.7 1.7", OffsetMax = "-1.7 -1.7"},
                            new CuiOutlineComponent{Color = "0 0 0 1", Distance = "0.4 0.4"}
                        }
                    });
                    CuiHelper.AddUi(player, container);
                break;
                case "cargo":
                    CuiHelper.DestroyUi(player, Layer + "CargoShipImage");
                    player.ChatMessage("хуй");
                    container.Add(new CuiElement
                    {
                        Name = Layer + "CargoShipImage",
                        Parent = Layer + "CargoShip",
                        Components =
                        {
                            new CuiRawImageComponent { Png = GetImage("cargo"), Color = Events["cargo"] },
                            new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "1.7 1.7", OffsetMax = "-1.7 -1.7"},
                            new CuiOutlineComponent{Color = "0 0 0 1", Distance = "0.4 0.4"}
                        }
                    });
                    CuiHelper.AddUi(player, container);
                break;
            }
        }

        private void ButtonsUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer + ".Menu.Opened");
			var container = new CuiElementContainer();
			var ySwitch = -57;

			container.Add(new CuiPanel
			{
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
				Image = { Color = "0 0 0 0" }
			}, Layer, Layer + ".Menu.Opened");

            foreach (var button in config._SettingsMenu)
			{
				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"4 {ySwitch -15}",
						OffsetMax = $"{92 + 5} {ySwitch}"
					},
					Image = { Material = "assets/icons/greyout.mat", Color = "0.8 0.8 0.8 0.125" }
				}, Layer + ".Menu.Opened", Layer + $".Menu.Opened.{button.Key}");
                container.Add(new CuiElement
                {
                    Parent = Layer + $".Menu.Opened.{button.Key}",
                    Components =
                    {
                        new CuiTextComponent { Text = button.Key, Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 11, Font = "robotocondensed-bold.ttf" },
                        new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                        new CuiOutlineComponent{Color = "0 0 0 1", Distance = "0.4 0.4"}
                    }
                });
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Color = "0 0 0 0", Command = $"playerSendly {button.Value}" },
                    Text = { Text = "" }
                }, Layer + $".Menu.Opened.{button.Key}");
                ySwitch -= 15 + 3;
			}

			CuiHelper.AddUi(player, container);
        }
        #endregion

        #region [Func]
        private void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer + "nameServer");
            CuiHelper.DestroyUi(player, Layer + "CloseMenu");
            CuiHelper.DestroyUi(player, Layer + "playerServer");
            CuiHelper.DestroyUi(player, Layer + "timeServer");
            CuiHelper.DestroyUi(player, Layer + "patrolHeliCopter");
            CuiHelper.DestroyUi(player, Layer + "CargoShip");
            CuiHelper.DestroyUi(player, Layer + "Bradley");
            CuiHelper.DestroyUi(player, Layer + "openMenu");
        }
        private void CmdMenu(IPlayer user, string cmd, string[] args)
		{
			var player = user?.Object as BasePlayer;
			if (player == null) return;

			if (MenuUsers.Contains(player))
			{
                DestroyUI(player);

                SupportUI(player, "openMenu");
				MenuUsers.Remove(player);
			}
			else
			{
                DestroyUI(player);

                SupportUI(player, "all");
				MenuUsers.Add(player);
			}
		}
		private void CmdMenu2(IPlayer user, string cmd, string[] args)
		{
			var player = user?.Object as BasePlayer;
			if (player == null) return;

			if (MenuUsers2.Contains(player))
			{
				CuiHelper.DestroyUi(player, Layer + ".Menu.Opened");
				MenuUsers2.Remove(player);
			}
			else
			{
				ButtonsUI(player);
				MenuUsers2.Add(player);
			}
		}
		[ConsoleCommand("playerSendly")]
		private void SendlyPlayer(ConsoleSystem.Arg args)
		{
			if (args.Player() != null)
			{
				var player = args.Player();
				var convertcmd =
					$"{args.Args[0]}  \" {string.Join(" ", args.Args.ToList().GetRange(1, args.Args.Length - 1))}\" 0";
				player.SendConsoleCommand(convertcmd);
			}
		}
		private void UpdateOnline()
		{
			foreach (var player in BasePlayer.activePlayerList)
			{
                if (MenuUsers.Contains(player))
				    SupportUI(player, "online");
			}
		}
        private int GetOnline()
		{
			return BasePlayer.activePlayerList.Count;
		}
        private readonly Dictionary<string, string> Events = new Dictionary<string, string>
        {
            ["cargo"] = "1 1 1 0.9",
            ["bradley"] = "1 1 1 0.9",
            ["heli"] = "1 1 1 0.9"
        };
        private void EntityHandle(BaseEntity entity, bool spawn)
		{
            if (entity == null) return;
            if (entity is BradleyAPC)
            {
                Events["bradley"] = spawn ? "1.00 0.84 0.52 1" : "1 1 1 0.9";
                foreach (var player in BasePlayer.activePlayerList)
                {
                    if (MenuUsers.Contains(player))
                        SupportUI(player, "bradley");
                }
            }
            if (entity is CargoShip)
            {
                Events["cargo"] = spawn ? "0.45 0.65 0.86 1" : "1 1 1 0.9";
                foreach (var player in BasePlayer.activePlayerList)
                {
                    if (MenuUsers.Contains(player))
                        SupportUI(player, "cargo");
                }
            }
            if (entity is BaseHelicopter)
            {
                Events["heli"] = spawn ? "0.97 0.62 0.62 1" : "1 1 1 0.9";
                foreach (var player in BasePlayer.activePlayerList)
                {
                    if (MenuUsers.Contains(player))
                        SupportUI(player, "heli");
                }
            }
		}
        #endregion

        #region [Config]
        private PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            config = PluginConfig.DefaultConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();

            if (config.PluginVersion < Version)
                UpdateConfigValues();

            Config.WriteObject(config, true);
        }

        private void UpdateConfigValues()
        {
            PluginConfig baseConfig = PluginConfig.DefaultConfig();
            if (config.PluginVersion < Version)
            {
                config.PluginVersion = Version;
                if (Version == new VersionNumber(1, 0, 0))
                {
                    //
                }

                PrintWarning("Config checked completed!");
            }
            config.PluginVersion = Version;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        private class PluginConfig
        {
            [JsonProperty("Название сервера")]
            public string ServerName;

            [JsonProperty("Настройки /MENU")]
            public Dictionary<string, string> _SettingsMenu;

            [JsonProperty("Изображения")]
            public Dictionary<string, string> _Images;

            [JsonProperty("Config version")]
            public VersionNumber PluginVersion = new VersionNumber();

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    ServerName = "BOLOTO RUST MAX3",
                    _SettingsMenu = new Dictionary<string, string>()
                    {
                        ["ИНФОРМАЦИЯ"] = "chat.say /help",
                        ["ТОП"] = "chat.say /top",
                        ["ТЕЛЕПОРТ"] = "chat.say /tpmenu",
                        ["КРАФТ"] = "chat.say /craft",
                        ["СКИНЫ"] = "chat.say /skin",
                        ["БЛОКИРОВКА"] = "chat.say /block",
                        ["ПРИЦЕЛ"] = "chat.say /hair",
                        ["РЕПОРТ"] = "chat.say /report",
                        ["РУЛЕТКА"] = "chat.say /roll",
                    },
                    _Images = new Dictionary<string, string>()
                    {
                        ["https://i.imgur.com/r5WXR8C.png"] = "store",
                        ["https://imgur.com/T3IN2qz.png"] = "time",
                        ["https://i.imgur.com/dleUaul.png"] = "online",
                        ["https://i.imgur.com/8nx4pnW.png"] = "heli",
                        ["https://imgur.com/b39MBgv.png"] = "cargo",
                        ["https://imgur.com/bqB9Gkb.png"] = "bradley",
                    },
                    PluginVersion = new VersionNumber()
                };
            }
        }
        #endregion
    }
}