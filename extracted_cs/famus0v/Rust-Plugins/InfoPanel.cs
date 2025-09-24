using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("InfoPanel", "Luminos", "1.0.0")]
    [Description("Server Info in game HUD")]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class InfoPanel : RustPlugin
    {
        #region [Vars]

#pragma warning disable CS0649
        [PluginReference] private Plugin ImageLibrary;
#pragma warning restore CS0649

        private const string UIMain = "InfoPanel.Layer.Main";
        private const string UIInfo = "InfoPanel.Layer.Main.Info.BG";
        private const string UIEvents = "InfoPanel.Layer.Main.Events.BG";

        private TOD_Sky Sky;
        private static InfoPanel _;
        private static readonly WaitForSeconds WaitForSeconds = new WaitForSeconds(0.1f);

        private readonly Dictionary<string, string> Images = new Dictionary<string, string>
        {
            ["InfoPanel.IconBG"] = "https://i.imgur.com/LKD8GmH.png",
            ["InfoPanel.EventsBG"] = "https://i.imgur.com/yC44saU.png",
            ["InfoPanel.InfoBG"] = "https://i.imgur.com/QAxIus9.png",
            ["InfoPanel.ButtonBG"] = "https://i.imgur.com/FQvPC3z.png",
            ["InfoPanel.EventICBG"] = "https://i.imgur.com/QR3Chbr.png",
            ["InfoPanel.EventIC.Helicopter"] = "https://i.imgur.com/HtAifod.png",
            ["InfoPanel.EventIC.BradleyAPC"] = "https://i.imgur.com/j5cMDpt.png",
            ["InfoPanel.EventIC.CargoPlane"] = "https://i.imgur.com/CL41EJS.png",
            ["InfoPanel.EventIC.CargoShip"] = "https://i.imgur.com/xU8IUWO.png",
            ["InfoPanel.InfoBG.OnlineIC"] = "https://i.imgur.com/lWoUIw2.png"
        };

        private readonly Dictionary<ulong, PlayerPanel> ActiveComponents = new Dictionary<ulong, PlayerPanel>();

        public class Button
        {
            public string Name;
            public string Command;
            public string Icon;
            public int IconTranspent;
        }

        private readonly List<string> ActiveEvents = new List<string>();

        #endregion

        #region [Configuration]

        private static ConfigData _config;

        public class ConfigData
        {
            [JsonProperty(PropertyName = "InfoPanel [Configuration]")]
            public InfoPanelConfig InfoPanelCfg = new InfoPanelConfig();

            public class InfoPanelConfig
            {
                [JsonProperty(PropertyName = "ServerName")]
                public string Servername;

                [JsonProperty(PropertyName = "ServerLogo (URL)")]
                public string ServerLogo;

                [JsonProperty(PropertyName = "ServerLogo Color (HEX)")]
                public string ServerLogoColor;

                [JsonProperty(PropertyName = "UI Event Background Color (HEX)")]
                public string UIEventBGColor;

                [JsonProperty(PropertyName = "UI Logo Background Color (HEX)")]
                public string UIIconBGColor;

                [JsonProperty(PropertyName = "UI Info Background Color (HEX)")]
                public string UIInfoColor;

                [JsonProperty(PropertyName = "UI Event Icon Background Color (HEX)")]
                public string UIEventBGIconColor;

                [JsonProperty(PropertyName = "UI Event Icons Color (HEX)")]
                public string UIEventIconColor;

                [JsonProperty(PropertyName = "UI Button Background Color (HEX)")]
                public string UIButtonBGColor;

                [JsonProperty(PropertyName = "UI Icon PlayerCount Color (HEX)")]
                public string UIIconPlayerColor;

                [JsonProperty(PropertyName = "UI Text PlayerCount Color (HEX)")]
                public string UICountPlayerColor;

                [JsonProperty(PropertyName = "UI Time Color (HEX)")]
                public string UITimeColor;

                [JsonProperty(PropertyName = "Buttons")]
                public List<Button> Buttons;
            }
        }

        private static ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                InfoPanelCfg = new ConfigData.InfoPanelConfig
                {
                    Servername = ConVar.Server.hostname,
                    ServerLogo = "https://i.imgur.com/tLX5e5s.png",
                    Buttons = new List<Button>
                    {
                        new Button
                        {
                            Name = "Test1",
                            Command = "chat.say Test",
                            Icon = "https://i.imgur.com/lxlYHdk.png"
                        },
                        new Button
                        {
                            Name = "Test2",
                            Command = "chat.say Test",
                            Icon = "https://i.imgur.com/lxlYHdk.png"
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
            PrintError("The config file is corrupted (or does not exist), a new one was created!");
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        #endregion

        #region [MonoBehaviours]

        public class PlayerPanel : MonoBehaviour
        {
            private BasePlayer player;
            public bool IsOpen;

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                DrawUI_Main();
                Open();
            }

            private void Timer()
            {
                DrawUI_TimeClock();
            }

            public void Open()
            {
                if (IsOpen) return;
                IsOpen = true;
                DrawUI_Background();
                DrawUI_Events();
                DrawUI_Menu();
                DrawUI_Online();
                DrawUI_TimeClock();
                DrawUI_Logo();
                InvokeRepeating(nameof(Timer), 1, 1);
            }

            public void Close()
            {
                IsOpen = false;
                CancelInvoke(nameof(Timer));
                CuiHelper.DestroyUi(player, UIInfo);
                CuiHelper.DestroyUi(player, UIEvents);

                var y = 0;
                foreach (var button in _config.InfoPanelCfg.Buttons)
                {
                    CuiHelper.DestroyUi(player, $"{UIEvents}.{button.Name}.BG.{y}");
                    y++;
                }
            }

            private void DrawUI_Main()
            {
                var ui = new CuiElementContainer
                {
                    new CuiElement
                    {
                        Name = UIMain,
                        Parent = "Hud",
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = "0 0 0 0"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.006217115 0.8780348",
                                AnchorMax = "0.1549654 0.9879107"
                            }
                        }
                    }
                };

                CuiHelper.DestroyUi(player, UIMain);
                CuiHelper.AddUi(player, ui);
                DrawUI_Logo();
            }

            private void DrawUI_Logo()
            {
                var ui = new CuiElementContainer
                {
                    new CuiElement
                    {
                        Name = $"{UIMain}.Icon.BG",
                        Parent = UIMain,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Color = HexToRustFormat(_config.InfoPanelCfg.UIIconBGColor),
                                Png = _.GetImage("InfoPanel.IconBG"),
                                FadeIn = 0.2f
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "-0.0008843194 0.4951484",
                                AnchorMax = "0.2091798 1.000862"
                            }
                        }
                    },
                    new CuiElement
                    {
                        Parent = $"{UIMain}.Icon.BG",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Color = HexToRustFormat(_config.InfoPanelCfg.ServerLogoColor),
                                Png = _.GetImage("InfoPanel.Logo"),
                                FadeIn = 0.2f
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.1 0.1",
                                AnchorMax = "0.9 0.9"
                            }
                        }
                    }
                };

                ui.Add(new CuiButton
                {
                    Button =
                    {
                        Color = "0 0 0 0",
                        Command = "infopanel"
                    },
                    Text =
                    {
                        Text = " "
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.1 0.1",
                        AnchorMax = "0.9 0.9"
                    }
                }, $"{UIMain}.Icon.BG");

                CuiHelper.DestroyUi(player, $"{UIMain}.Icon.BG");
                CuiHelper.AddUi(player, ui);
            }

            private void DrawUI_Background()
            {
                var ui = new CuiElementContainer
                {
                    new CuiElement
                    {
                        Name = $"{UIMain}.Info.BG",
                        Parent = UIMain,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Color = HexToRustFormat(_config.InfoPanelCfg.UIInfoColor),
                                Png = _.GetImage("InfoPanel.InfoBG"),
                                FadeIn = 0.2f
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.1 -0.01048407",
                                AnchorMax = "1.0005 0.748"
                            }
                        }
                    },
                    new CuiElement
                    {
                        Name = $"{UIMain}.Events.BG",
                        Parent = UIMain,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Color = HexToRustFormat(_config.InfoPanelCfg.UIEventBGColor),
                                Png = _.GetImage("InfoPanel.EventsBG"),
                                FadeIn = 0.2f
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "-0.0008933351 0.4952035",
                                AnchorMax = "1.00045 1.000892"
                            }
                        }
                    },
                    new CuiElement
                    {
                        Name = $"{UIInfo}.ServerName",
                        Parent = UIInfo,
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Align = TextAnchor.MiddleCenter,
                                FontSize = 12,
                                Text = _config.InfoPanelCfg.Servername,
                                FadeIn = 0.2f
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.13 0",
                                AnchorMax = "1 0.3403818"
                            }
                        }
                    },
                    new CuiElement
                    {
                        Parent = UIInfo,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Color = HexToRustFormat(_config.InfoPanelCfg.UIIconPlayerColor),
                                Png = _.GetImage("InfoPanel.InfoBG.OnlineIC"),
                                FadeIn = 0.2f
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.157709 0.3645711",
                                AnchorMax = "0.2210962 0.5459934"
                            }
                        }
                    }
                };

                CuiHelper.AddUi(player, ui);
            }

            public void DrawUI_Events()
            {
                var ui = new CuiElementContainer();

                var x = 0;
                foreach (var Event in _.ActiveEvents)
                {
                    ui.Add(new CuiElement
                    {
                        Name = $"{UIEvents}.{Event}.BG",
                        Parent = UIEvents,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Color = HexToRustFormat(_config.InfoPanelCfg.UIEventBGIconColor),
                                Png = _.GetImage("InfoPanel.EventICBG"),
                                FadeIn = 0.2f
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{0.2675062 + (x * 0.18)} 0.1833343",
                                AnchorMax = $"{0.3999104 + (x * 0.18)} 0.8166674"
                            }
                        }
                    });

                    ui.Add(new CuiElement
                    {
                        Parent = $"{UIEvents}.{Event}.BG",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Color = HexToRustFormat(_config.InfoPanelCfg.UIEventIconColor),
                                Png = _.GetImage($"InfoPanel.EventIC.{Event}"),
                                FadeIn = 0.2f
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.15 0.2",
                                AnchorMax = "0.8 0.75"
                            }
                        }
                    });

                    x++;
                }

                CuiHelper.AddUi(player, ui);
            }

            private void DrawUI_TimeClock()
            {
                var ui = new CuiElementContainer
                {
                    new CuiElement
                    {
                        Name = $"{UIInfo}.TimeClock",
                        Parent = UIInfo,
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Align = TextAnchor.MiddleRight,
                                Color = HexToRustFormat(_config.InfoPanelCfg.UITimeColor),
                                FontSize = 16,
                                Text = $"{_.Sky.Cycle.DateTime:HH:mm}"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.5 0.3",
                                AnchorMax = "0.95 0.6"
                            }
                        }
                    }
                };

                CuiHelper.DestroyUi(player, $"{UIInfo}.TimeClock");
                CuiHelper.AddUi(player, ui);
            }

            private void DrawUI_Online()
            {
                var ui = new CuiElementContainer
                {
                    new CuiElement
                    {
                        Name = $"{UIInfo}.OnlineCount",
                        Parent = UIInfo,
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Align = TextAnchor.MiddleLeft,
                                Color = HexToRustFormat(_config.InfoPanelCfg.UICountPlayerColor),
                                FontSize = 10,
                                Text =
                                    $"{BasePlayer.activePlayerList.Count} <size=9>(+{ServerMgr.Instance.connectionQueue.Joining})</size>",
                                FadeIn = 0.2f
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.25 0.3",
                                AnchorMax = "0.4755404 0.6"
                            }
                        }
                    }
                };

                CuiHelper.DestroyUi(player, $"{UIInfo}.OnlineCount");
                CuiHelper.AddUi(player, ui);
            }

            private void DrawUI_Menu()
            {
                var ui = new CuiElementContainer();

                var y = 0;
                foreach (var button in _config.InfoPanelCfg.Buttons)
                {
                    var butname = $"{UIEvents}.{button.Name}.BG.{y}";
                    ui.Add(new CuiElement
                    {
                        Name = butname,
                        Parent = UIMain,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Color = HexToRustFormat(_config.InfoPanelCfg.UIButtonBGColor),
                                Png = _.GetImage("InfoPanel.ButtonBG"),
                                FadeIn = 0.2f + (y * 0.2f)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0.02303147 {0.05839038 - (y * 0.44)}",
                                AnchorMax = $"0.173586 {0.4207886 - (y * 0.44)}"
                            }
                        }
                    });

                    ui.Add(new CuiElement
                    {
                        Parent = butname,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Color = $"1 1 1 {(float) button.IconTranspent / 100}",
                                Png = _.GetImage($"InfoPanel.Buttons.{button.Name}"),
                                FadeIn = 0.2f
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.2 0.2",
                                AnchorMax = "0.8 0.8"
                            }
                        }
                    });

                    ui.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = button.Command,
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
                    }, butname);

                    y++;
                }

                CuiHelper.AddUi(player, ui);
            }

            private void OnDestroy()
            {
                CuiHelper.DestroyUi(player, UIMain);
                CancelInvoke(nameof(Timer));
            }
        }

        #endregion

        #region [Hooks]

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            Sky = TOD_Sky.Instance;
            _ = this;
            ServerMgr.Instance.StartCoroutine(LoadImages());
            UpdateActiveEvents();
            ServerMgr.Instance.StartCoroutine(InitializedComponents());
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (ActiveComponents.ContainsKey(player.userID))
                UnityEngine.Object.Destroy(ActiveComponents[player.userID]);
        }

        // ReSharper disable once UnusedMember.Local
        private void OnPlayerConnected(BasePlayer player)
        {
            if (ActiveComponents.ContainsKey(player.userID)) return;
            var component = player.gameObject.AddComponent<PlayerPanel>();
            ActiveComponents.Add(player.userID, component);
        }

        // ReSharper disable once UnusedMember.Local
        private void Unload()
        {
            ServerMgr.Instance.StartCoroutine(UnloadComponents());
        }

        #endregion

        #region [Commands]

        [ConsoleCommand("infopanel")]
        // ReSharper disable once UnusedMember.Local
        private void LoadInfoPanel(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!ActiveComponents.ContainsKey(player.userID)) return;
            var component = ActiveComponents[player.userID];
            if (component.IsOpen)
                ActiveComponents[player.userID].Close();
            else
                ActiveComponents[player.userID].Open();
        }

        [ConsoleCommand("infopanel.close")]
        // ReSharper disable once UnusedMember.Local
        private void UnloadInfoPanel(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (ActiveComponents.ContainsKey(player.userID))
            {
                ActiveComponents[player.userID].Close();
            }
        }

        #endregion

        #region [Helpers]

        private IEnumerator LoadImages()
        {
            ImageLibrary.Call("AddImage", _config.InfoPanelCfg.ServerLogo, "InfoPanel.Logo");
            // ReSharper disable once UseDeconstruction
            foreach (var image in Images)
                ImageLibrary.Call("AddImage", image.Value, image.Key);
            foreach (var image in _config.InfoPanelCfg.Buttons)
                ImageLibrary.Call("AddImage", image.Icon, $"InfoPanel.Buttons.{image.Name}");
            yield return 0;
        }

        private IEnumerator InitializedComponents()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (!ActiveComponents.ContainsKey(player.userID))
                    ActiveComponents.Add(player.userID, player.gameObject.AddComponent<PlayerPanel>());
                yield return WaitForSeconds;
            }

            yield return 0;
        }

        private IEnumerator UnloadComponents()
        {
            foreach (var component in ActiveComponents)
                UnityEngine.Object.Destroy(component.Value);
            yield return 0;
        }

        private IEnumerator UpdateComponents()
        {
            foreach (var component in ActiveComponents)
                component.Value.DrawUI_Events();
            yield return 0;
        }

        private void UpdateActiveEvents()
        {
            if (UnityEngine.Object.FindObjectOfType(typeof(BaseHelicopter)) != null)
            {
                if (!ActiveEvents.Contains("Helicopter"))
                {
                    ActiveEvents.Add("Helicopter");
                }
            }
            else
            {
                if (ActiveEvents.Contains("Helicopter"))
                {
                    ActiveEvents.Remove("Helicopter");
                }
            }

            if (UnityEngine.Object.FindObjectOfType(typeof(BradleyAPC)) != null)
            {
                if (!ActiveEvents.Contains("BradleyAPC"))
                {
                    ActiveEvents.Add("BradleyAPC");
                }
            }
            else
            {
                if (ActiveEvents.Contains("BradleyAPC"))
                {
                    ActiveEvents.Remove("BradleyAPC");
                }
            }

            if (UnityEngine.Object.FindObjectOfType(typeof(CargoPlane)) != null)
            {
                if (!ActiveEvents.Contains("CargoPlane"))
                {
                    ActiveEvents.Add("CargoPlane");
                }
            }
            else
            {
                if (ActiveEvents.Contains("CargoPlane"))
                {
                    ActiveEvents.Remove("CargoPlane");
                }
            }

            if (UnityEngine.Object.FindObjectOfType(typeof(CargoShip)) != null)
            {
                if (!ActiveEvents.Contains("CargoShip"))
                {
                    ActiveEvents.Add("CargoShip");
                }
            }
            else
            {
                if (ActiveEvents.Contains("CargoShip"))
                {
                    ActiveEvents.Remove("CargoShip");
                }
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity is BaseHelicopter)
            {
                if (!ActiveEvents.Contains("Helicopter"))
                {
                    ActiveEvents.Add("Helicopter");
                    ServerMgr.Instance.StartCoroutine(UpdateComponents());
                }
            }

            if (entity is BradleyAPC)
            {
                if (!ActiveEvents.Contains("BradleyAPC"))
                {
                    ActiveEvents.Add("BradleyAPC");
                    ServerMgr.Instance.StartCoroutine(UpdateComponents());
                }
            }

            if (entity is CargoPlane)
            {
                if (!ActiveEvents.Contains("CargoPlane"))
                {
                    ActiveEvents.Add("CargoPlane");
                    ServerMgr.Instance.StartCoroutine(UpdateComponents());
                }
            }

            if (entity is CargoShip)
            {
                if (!ActiveEvents.Contains("CargoShip"))
                {
                    ActiveEvents.Add("CargoShip");
                    ServerMgr.Instance.StartCoroutine(UpdateComponents());
                }
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void OnEntityKill(BaseNetworkable entity)
        {
            if (entity is BaseHelicopter)
                if (ActiveEvents.Contains("Helicopter"))
                {
                    ActiveEvents.Remove("Helicopter");
                    ServerMgr.Instance.StartCoroutine(UpdateComponents());
                }

            if (entity is BradleyAPC)
                if (ActiveEvents.Contains("BradleyAPC"))
                {
                    ActiveEvents.Remove("BradleyAPC");
                    ServerMgr.Instance.StartCoroutine(UpdateComponents());
                }

            if (entity is CargoPlane)
                if (ActiveEvents.Contains("CargoPlane"))
                {
                    ActiveEvents.Remove("CargoPlane");
                    ServerMgr.Instance.StartCoroutine(UpdateComponents());
                }

            if (entity is CargoShip)
                if (ActiveEvents.Contains("CargoShip"))
                {
                    ActiveEvents.Remove("CargoShip");
                    ServerMgr.Instance.StartCoroutine(UpdateComponents());
                }
        }

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
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);
            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }

        private string GetImage(string name)
        {
            return (string) ImageLibrary?.Call("GetImage", name);
        }

        #endregion
    }
}