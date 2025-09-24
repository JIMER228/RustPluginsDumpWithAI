// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿//Thank you for purchasing my plugin!
//It is forbidden to distribute this plugin for free and for a fee.
//It is allowed to modify the plugin.
//Discord of BUDAPESHTER#7439
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConVar;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SvUi", "TMK-BUD.RU", "1.3.3")]
    [Description("A great plugin for displaying server events and online players!")]
    class SvUi : RustPlugin
    {
        private PluginConfig _config;
        public string Layer = "UiPanel";
        public string Layer2 = "UiPanel2";
        [PluginReference] Plugin ImageLibrary;
        void OnServerInitialized()
        {
            Check();
            BasePlayer.activePlayerList.ToList().ForEach(DrawUI);
            foreach (var player in BasePlayer.activePlayerList) DrawInterface(player);
            if (!ImageLibrary)
            {
                PrintError("The plugin is not installed on the server [ImageLibrary]");
                return;
            }
                ImageLibrary.Call("AddImage", _config.Image.Bred, "bred");
                ImageLibrary.Call("AddImage", _config.Image.Air, "air");
                ImageLibrary.Call("AddImage", _config.Image.Cargo, "cargo");
                ImageLibrary.Call("AddImage", _config.Image.Heli, "heli");
                ImageLibrary.Call("AddImage", _config.Image.Ch, "ch");

        }
        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            foreach (var players in BasePlayer.activePlayerList)
            {
                timer.Once(1, () =>
                {
                    DrawInterface(players);
                });
            }
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            foreach (var players in BasePlayer.activePlayerList)
            {
                timer.Once(1, () =>
                {
                    DrawInterface(players);
                });
            }
            DrawUI(player);

        }
        private void Check()
        {
            foreach (var entity in BaseNetworkable.serverEntities.Where(p =>
                p is CargoPlane || p is BradleyAPC || p is BaseHelicopter || p is BaseHelicopter || p is CargoShip ||
                p is CH47Helicopter))
            {
                if (entity is CargoPlane)
                    IsAir = true;
                if (entity is BradleyAPC)
                    isTank = true;
                if (entity is BaseHelicopter)
                    IsHeli = true;
                if (entity is CargoShip)
                    IsCargo = true;
                if (entity is CH47Helicopter)
                    IsCh = true;
            }
        }
        void Loaded() 
        { 
            Puts("Loaded SvUi!"); 
        }
        private string GetImg(string name) 
        {
            return (string)ImageLibrary?.Call("GetImage", name) ?? "";
        }
        void Unload() 
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
                CuiHelper.DestroyUi(player, Layer2);

            }
        }
        #region Config
        private class PluginConfig
        {
            [JsonProperty("MainPanel Settings")]
            public MainSettings MainPanel;
            [JsonProperty("NameServer Settings")]
            public TitleSettings Text;
            [JsonProperty("Image Settings")]
            public ImageSettings Image;
            [JsonProperty("OnlinePanel Settings")]
            public OnlineSettings OnlinePanel;
            [JsonProperty("Bradley Icon Settings")]
            public BradleySettings Bradley;
            [JsonProperty("Air Icon Settings")]
            public AirSettings Air;
            [JsonProperty("Cargo Icon Settings")]
            public CargoSettings Cargo;
            [JsonProperty("Heli Icon Settings")]
            public HeliSettings Heli;
            [JsonProperty("Ch Icon Settings")]
            public ChSettings Ch;
            #region MainSettings
            internal class MainSettings
            {
                [JsonProperty("AnchorMin")] public string AnchorMin;
                [JsonProperty("AnchorMax")] public string AnchorMax;
                [JsonProperty("OffsetMin")] public string OffsetMin;
                [JsonProperty("OffsetMax")] public string OffsetMax;
            }
            #endregion
            #region TitleSettings
            internal class TitleSettings
            {
                [JsonProperty("AnchorMin")] public string AnchorMin;
                [JsonProperty("AnchorMax")] public string AnchorMax;
                [JsonProperty("OffsetMin")] public string OffsetMin;
                [JsonProperty("OffsetMax")] public string OffsetMax;
                [JsonProperty("Align")] public TextAnchor Align;
                [JsonProperty("Text color")] public string Color;
                [JsonProperty("NameServer")] public string TitleText;
                [JsonProperty("Font size")] public int SizeFont;
                [JsonProperty("Font")] public string Font;
                [JsonProperty("Overlay or Hud")] public string UiOver;
                [JsonProperty("Time true/false")] public bool TimeUi;
            }
            #endregion
            #region ImageSettings
            internal class ImageSettings //images
            {
                [JsonProperty("Color of inactive images")] public string Color;
                [JsonProperty("Color of active images")] public string ColorAk;
                [JsonProperty("Panel Sprite")] public string SpritePanel;
                [JsonProperty("Panel background color")] public string ColorPanel;
                [JsonProperty("AnchorMin Panel")] public string AnchorMin;
                [JsonProperty("AnchorMax Panel")] public string AnchorMax;
                [JsonProperty("OffsetMin Panel")] public string OffsetMin;
                [JsonProperty("OffsetMax Panel")] public string OffsetMax;
                [JsonProperty("Picture Bradley")] public string Bred;
                [JsonProperty("Picture Air")] public string Air;
                [JsonProperty("Picture Cargo")] public string Cargo;
                [JsonProperty("Picture Heli")] public string Heli;
                [JsonProperty("Picture Ch")] public string Ch;

            }
            #endregion
            #region OnlinePanelSettings
            internal class OnlineSettings //onlinepanel
            {
                [JsonProperty("TextOnline")] public string OnlineTxt;
                [JsonProperty("TextOffline")] public string OfflineTxt;
                [JsonProperty("TextJoin")] public string JoinText;
                [JsonProperty("ColorText")] public string ColorText;
                [JsonProperty("Align")] public TextAnchor Align;
                [JsonProperty("Text Separator")] public string SepText;
                [JsonProperty("Font size")] public int SizeFont;
                [JsonProperty("Font")] public string Font;
                [JsonProperty("AnchorMin")] public string AnchorMin;
                [JsonProperty("AnchorMax")] public string AnchorMax;
                [JsonProperty("OffsetMin")] public string OffsetMin;
                [JsonProperty("OffsetMax")] public string OffsetMax;

            }
            #endregion
            #region Icon settings
            internal class BradleySettings 
            {
                [JsonProperty("AnchorMin")] public string AnchorMin;
                [JsonProperty("AnchorMax")] public string AnchorMax;
                [JsonProperty("OffsetMin")] public string OffsetMin;
                [JsonProperty("OffsetMax")] public string OffsetMax;
            }
            internal class AirSettings
            {
                [JsonProperty("AnchorMin")] public string AnchorMin;
                [JsonProperty("AnchorMax")] public string AnchorMax;
                [JsonProperty("OffsetMin")] public string OffsetMin;
                [JsonProperty("OffsetMax")] public string OffsetMax;
            }
            internal class CargoSettings
            {
                [JsonProperty("AnchorMin")] public string AnchorMin;
                [JsonProperty("AnchorMax")] public string AnchorMax;
                [JsonProperty("OffsetMin")] public string OffsetMin;
                [JsonProperty("OffsetMax")] public string OffsetMax;
            }
            internal class HeliSettings
            {
                [JsonProperty("AnchorMin")] public string AnchorMin;
                [JsonProperty("AnchorMax")] public string AnchorMax;
                [JsonProperty("OffsetMin")] public string OffsetMin;
                [JsonProperty("OffsetMax")] public string OffsetMax;
            }
            internal class ChSettings
            {
                [JsonProperty("AnchorMin")] public string AnchorMin;
                [JsonProperty("AnchorMax")] public string AnchorMax;
                [JsonProperty("OffsetMin")] public string OffsetMin;
                [JsonProperty("OffsetMax")] public string OffsetMax;
            }
            #endregion
        }
        protected override void LoadDefaultConfig()
        {
            _config = new PluginConfig
            {
                MainPanel = new PluginConfig.MainSettings()
                {
                    AnchorMin = "0 1",
                    AnchorMax = "0 1",
                    OffsetMin = "0 0",
                    OffsetMax = "0 0",
                },
                Image = new PluginConfig.ImageSettings()
                {
                    Color = "0 0 0 0.90",
                    ColorAk = "255 106 0 0.90",
                    ColorPanel = "0 0 0 0.73",
                    SpritePanel = "assets/content/ui/ui.background.transparent.linearltr.tga",
                    Bred = "https://i.ibb.co/pLg31pD/2.png",
                    Air = "https://i.yapx.ru/O47QR.png",
                    Cargo = "https://i.ibb.co/8sxWfrJ/4.png",
                    Heli = "https://i.ibb.co/cXNqzNd/1.png",
                    Ch = "https://cdn.discordapp.com/attachments/676533751190126631/690265099625431227/5.png",
                    AnchorMin = "0 0",
                    AnchorMax = "0 0",
                    OffsetMin = "0 -33",
                    OffsetMax = "320 0"

                },

                Text = new PluginConfig.TitleSettings()
                {
                    TitleText = "SERVERNAME",
                    Color = "1 1 1 0.74",
                    SizeFont = 14,
                    AnchorMin = "0 0",
                    AnchorMax = "0 0",
                    Font = "robotocondensed-bold.ttf",
                    OffsetMin = "190 -25",
                    OffsetMax = "310 -2",
                    Align = TextAnchor.MiddleLeft,
                    UiOver="Hud",
                    TimeUi = false
                },
                OnlinePanel = new PluginConfig.OnlineSettings()
                {
                    OnlineTxt = "ONLINE",
                    OfflineTxt = "OFFLINE",
                    SizeFont = 10,
                    JoinText = "JOIN",
                    ColorText = "#ffe500",
                    Font = "robotocondensed-bold.ttf",
                    SepText = "|",
                    Align = TextAnchor.MiddleLeft,
                    AnchorMin = "0 0",
                    AnchorMax = "0 0",
                    OffsetMin = "0 -55",
                    OffsetMax = "275 0"
                },
                Bradley = new PluginConfig.BradleySettings()
                {
                    AnchorMin = "0 0",
                    AnchorMax = "0 0",
                    OffsetMin = "20 -25",
                    OffsetMax = "45 -5"
                },
                Air = new PluginConfig.AirSettings()
                {
                    AnchorMin = "0 0",
                    AnchorMax = "0 0",
                    OffsetMin = "55 -27",
                    OffsetMax = "80 -5"
                },
                Cargo = new PluginConfig.CargoSettings()
                {
                    AnchorMin = "0 0",
                    AnchorMax = "0 0",
                    OffsetMin = "90 -25",
                    OffsetMax = "115 -5"
                },
                Heli = new PluginConfig.HeliSettings()
                {
                    AnchorMin = "0 0",
                    AnchorMax = "0 0",
                    OffsetMin = "125 -25",
                    OffsetMax = "150 -5"
                },
                Ch = new PluginConfig.ChSettings()
                {
                    AnchorMin = "0 0",
                    AnchorMax = "0 0",
                    OffsetMin = "160 -25",
                    OffsetMax = "185 -5"
                }

            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<PluginConfig>();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        #endregion
        #region CUI
        private CuiPanel _mainPanel = new CuiPanel() 
        {
            RectTransform =
            {
                  AnchorMin = "0 1", 
                  AnchorMax = "0 1",
                  OffsetMin = "0 0",
                  OffsetMax = "0 0"
            }
        };        

        private void DrawInterface(BasePlayer player)
        {
            if (player == null) return;
            CuiHelper.DestroyUi(player, Layer2);
            var container = new CuiElementContainer();
            var SleepersCount = BasePlayer.sleepingPlayerList.Count.ToString();
            var JoiningCount = ServerMgr.Instance.connectionQueue.Joining.ToString();
            container.Add(_mainPanel, _config.Text.UiOver, Layer2);
            //online 
            container.Add(new CuiElement
            {
                Parent = Layer2,
                Components =
                {
                    new CuiTextComponent { Text = $"{_config.OnlinePanel.OnlineTxt}: <color={_config.OnlinePanel.ColorText}>{BasePlayer.activePlayerList.Count}</color> {_config.OnlinePanel.SepText} {_config.OnlinePanel.OfflineTxt}: <color={_config.OnlinePanel.ColorText}>{SleepersCount}</color> {_config.OnlinePanel.SepText} {_config.OnlinePanel.JoinText}: <color={_config.OnlinePanel.ColorText}>{JoiningCount}</color>", Align = TextAnchor.MiddleCenter, FontSize = _config.OnlinePanel.SizeFont, Font = _config.OnlinePanel.Font},
                    new CuiOutlineComponent { Color = "0 0 0 1", Distance = "0.5 0.5" },
                    new CuiRectTransformComponent {AnchorMin = $"{_config.OnlinePanel.AnchorMin}", AnchorMax = $"{_config.OnlinePanel.AnchorMax}", OffsetMin = $"{_config.OnlinePanel.OffsetMin}", OffsetMax = $"{_config.OnlinePanel.OffsetMax}" }
                }
            });

            CuiHelper.AddUi(player, container);
        }
          
            void DrawUI(BasePlayer player)
            {
            if (player == null) return;
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();
            container.Add(_mainPanel, _config.Text.UiOver, Layer); 

            container.Add(new CuiElement 
            {
                Parent = Layer,
                Components = {
                     new CuiTextComponent()
                    {
                        Color = _config.Text.Color,
                        Text = _config.Text.TitleText,
                        FontSize = _config.Text.SizeFont,
                        Align = _config.Text.Align,
                        Font = _config.Text.Font

                    },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = _config.Text.AnchorMin,
                            AnchorMax = _config.Text.AnchorMax,
                            OffsetMin = _config.Text.OffsetMin,
                            OffsetMax = _config.Text.OffsetMax
                        }
                }
            });
            container.Add(new CuiPanel() //Bradley icon
            {
                RectTransform =
                            {
                            AnchorMin = $"{_config.Bradley.AnchorMin}",
                            AnchorMax = $"{_config.Bradley.AnchorMax}",
                            OffsetMin = $"{_config.Bradley.OffsetMin}",
                            OffsetMax = $"{_config.Bradley.OffsetMax}"
                            },

                Image =
                            {
                                Color = _config.Image.Color, Png = GetImg("bred")
                            },
            }, Layer);
            container.Add(new CuiPanel()//Air icon
            {
                RectTransform =
                            {
                            AnchorMin = $"{_config.Air.AnchorMin}",
                            AnchorMax = $"{_config.Air.AnchorMax}",
                            OffsetMin = $"{_config.Air.OffsetMin}",
                            OffsetMax = $"{_config.Air.OffsetMax}"
                            },
                Image =
                            {
                                Color = _config.Image.Color, Png = GetImg("air")
                            },
            }, Layer);
            container.Add(new CuiPanel()//Cargo icon
            {
                RectTransform =
                            {
                            AnchorMin = $"{_config.Cargo.AnchorMin}",
                            AnchorMax = $"{_config.Cargo.AnchorMax}",
                            OffsetMin = $"{_config.Cargo.OffsetMin}",
                            OffsetMax = $"{_config.Cargo.OffsetMax}"
                            },
                Image =
                            {
                                Color = _config.Image.Color, Png = GetImg("cargo")
                            },
            }, Layer);
            container.Add(new CuiPanel()//Heli icon
            {
                RectTransform =
                            {
                            AnchorMin = $"{_config.Heli.AnchorMin}",
                            AnchorMax = $"{_config.Heli.AnchorMax}",
                            OffsetMin = $"{_config.Heli.OffsetMin}",
                            OffsetMax = $"{_config.Heli.OffsetMax}"
                            },
                Image =
                            {
                                Color = _config.Image.Color, Png = GetImg("heli")
                            },
            }, Layer);
            container.Add(new CuiPanel()//Ch icon
            {
                RectTransform =
                            {
                            AnchorMin = $"{_config.Ch.AnchorMin}",
                            AnchorMax = $"{_config.Ch.AnchorMax}",
                            OffsetMin = $"{_config.Ch.OffsetMin}",
                            OffsetMax = $"{_config.Ch.OffsetMax}"
                            },
                Image =
                            {
                                Color = _config.Image.Color, Png = GetImg("ch")
                            },
            }, Layer);
            container.Add(new CuiElement()
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = _config.Image.ColorPanel,
                        Sprite = _config.Image.SpritePanel
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = $"{_config.Image.AnchorMin}", AnchorMax = $"{_config.Image.AnchorMax}", OffsetMin = $"{_config.Image.OffsetMin}", OffsetMax = $"{_config.Image.OffsetMax}"
                    }
                }
            });
           
            CuiHelper.AddUi(player, container);
            EventInit(player, "air");
            EventInit(player, "ch");
            EventInit(player, "heli");
            EventInit(player, "cargo");
            EventInit(player, "bradley");

            }       
        #endregion
        #region Check Event

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity is BaseHelicopter)
            {
                IsHeli = true;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    EventInit(basePlayer, "heli");
            }
            else if (entity is BradleyAPC)
            {
                isTank = true;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    EventInit(basePlayer, "bradley");
            }
            else if (entity is CargoPlane)
            {
                IsAir = true;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    EventInit(basePlayer, "air");
            }
            else if (entity is CargoShip)
            {
                IsCargo = true;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    EventInit(basePlayer, "cargo");
            }
            else if (entity is CH47Helicopter)
            {
                IsCh = true;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    EventInit(basePlayer, "ch");
            }
        }
        void OnEntityKill(BaseNetworkable entity) 
        {
            if (entity is CargoPlane)
            {
                IsAir = false;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    EventInit(basePlayer, "air");
            }
            else if (entity is CargoShip)
            {
                IsCargo = false;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    EventInit(basePlayer, "cargo");
            }
            else if (entity is BaseHelicopter)
            {
                IsHeli = false;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    EventInit(basePlayer, "heli");
            }
            else if (entity is BradleyAPC)
            {
                isTank = false;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    EventInit(basePlayer, "bradley");
            }
            else if (entity is CH47Helicopter)
            {
                IsCh = false;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    EventInit(basePlayer, "ch");
            }
        }

        #endregion
        #region Init event
        private bool IsAir, IsHeli, isTank, IsCargo, IsCh;
        private void EventInit(BasePlayer player, string type)
        {
            var cont = new CuiElementContainer();
            switch (type)
            {
                case "bradley":
                    CuiHelper.DestroyUi(player, Layer + "bred");
                    if (isTank)
                    {
                        cont.Add(new CuiButton()
                        {
                            RectTransform =
                            {
                               AnchorMin = "0 0", 
                               AnchorMax = "0 0",
                               OffsetMin = "0 0",
                               OffsetMax = "0 0"
                            },
                            Button =
                            {
                                Command = "", Color = "1 1 1 0.15",
                            },
                            Text =
                            {
                                Text = $"",
                            }
                        }, Layer, Layer + "bred");

                        cont.Add(new CuiPanel()
                        {
                            RectTransform =
                            {
                            AnchorMin = $"{_config.Bradley.AnchorMin}",
                            AnchorMax = $"{_config.Bradley.AnchorMax}",
                            OffsetMin = $"{_config.Bradley.OffsetMin}",
                            OffsetMax = $"{_config.Bradley.OffsetMax}"
                            },

                            Image =
                            {
                                Color = _config.Image.ColorAk, Png = GetImg("bred")
                            },
                        }, Layer + "bred");
                    }
                    else
                    {
                        CuiHelper.DestroyUi(player, Layer + "bred");                           
                    }
                    CuiHelper.AddUi(player, cont);
                    break;
                case "air":
                    CuiHelper.DestroyUi(player, Layer + "air");
                    if (IsAir)
                    {
                        cont.Add(new CuiButton()
                        {
                            RectTransform =
                            {
                               AnchorMin = "0 0", 
                               AnchorMax = "0 0",
                               OffsetMin = "0 0",
                               OffsetMax = "0 0"
                            },
                            Button =
                            {
                                Command = "", Color = "",
                            },
                            Text =
                            {
                                Text = $"",
                            }
                        }, Layer, Layer + "air");

                        cont.Add(new CuiPanel()
                        {
                            RectTransform =
                            {
                            AnchorMin = $"{_config.Air.AnchorMin}",
                            AnchorMax = $"{_config.Air.AnchorMax}",
                            OffsetMin = $"{_config.Air.OffsetMin}",
                            OffsetMax = $"{_config.Air.OffsetMax}"
                            },
                            Image =
                            {
                                Color = _config.Image.ColorAk, Png = GetImg("air")
                            },
                        }, Layer + "air");
                    }
                    else
                    {
                        CuiHelper.DestroyUi(player, Layer + "air");
                    }
                    CuiHelper.AddUi(player, cont);
                    break;
               case "cargo":
                    CuiHelper.DestroyUi(player, Layer + "cargo");
                    if (IsCargo)
                    {
                        cont.Add(new CuiButton()
                        {
                            RectTransform =
                            {
                               AnchorMin = "0 0", 
                               AnchorMax = "0 0",
                               OffsetMin = "0 0",
                               OffsetMax = "0 0"
                            },
                            Button =
                            {
                                Command = "", Color = "",
                            },
                            Text =
                            {
                                Text = $"",
                            }
                        }, Layer, Layer + "cargo");

                        cont.Add(new CuiPanel()
                        {
                            RectTransform =
                            {
                            AnchorMin = $"{_config.Cargo.AnchorMin}",
                            AnchorMax = $"{_config.Cargo.AnchorMax}",
                            OffsetMin = $"{_config.Cargo.OffsetMin}",
                            OffsetMax = $"{_config.Cargo.OffsetMax}"
                            },
                            Image =
                            {
                                Color = _config.Image.ColorAk, Png = GetImg("cargo")
                            },
                        }, Layer + "cargo");
                    }
                    else
                    {
                        CuiHelper.DestroyUi(player, Layer + "cargo");
                    }
                    CuiHelper.AddUi(player, cont);
                    break;
                case "heli":

                    if (IsHeli)
                    {
                        cont.Add(new CuiButton()
                        {
                            RectTransform =
                            {
                               AnchorMin = "0 0", 
                               AnchorMax = "0 0",
                               OffsetMin = "0 0",
                               OffsetMax = "0 0"
                            },
                            Button =
                            {
                                Command = "", Color = "",
                            },
                            Text =
                            {
                                Text = $"",
                            }
                        }, Layer, Layer + "heli");

                        cont.Add(new CuiPanel()
                        {
                            RectTransform =
                            {
                            AnchorMin = $"{_config.Heli.AnchorMin}",
                            AnchorMax = $"{_config.Heli.AnchorMax}",
                            OffsetMin = $"{_config.Heli.OffsetMin}",
                            OffsetMax = $"{_config.Heli.OffsetMax}"
                            },
                            Image =
                            {
                                Color = _config.Image.ColorAk, Png = GetImg("heli")
                            },
                        }, Layer + "heli");
                    }
                    else
                    {
                        CuiHelper.DestroyUi(player, Layer + "heli");
                    }
                    CuiHelper.AddUi(player, cont);
                    break;
                case "ch":
                    CuiHelper.DestroyUi(player, Layer + "ch");
                    if (IsCh)
                    {
                        cont.Add(new CuiButton()
                        {
                            RectTransform =
                            {
                               AnchorMin = "0 0", 
                               AnchorMax = "0 0",
                               OffsetMin = "0 0",
                               OffsetMax = "0 0"
                            },
                            Button =
                            {
                                Command = "", Color = "",
                            },
                            Text =
                            {
                                Text = $"",
                            }
                        }, Layer, Layer + "ch");

                        cont.Add(new CuiPanel()
                        {
                            RectTransform =
                            {
                            AnchorMin = $"{_config.Ch.AnchorMin}",
                            AnchorMax = $"{_config.Ch.AnchorMax}",
                            OffsetMin = $"{_config.Ch.OffsetMin}",
                            OffsetMax = $"{_config.Ch.OffsetMax}"
                            },
                            Image =
                            {
                                Color = _config.Image.ColorAk, Png = GetImg("ch")
                            },
                        }, Layer + "ch");
                    }
                    else
                    {
                        CuiHelper.DestroyUi(player, Layer + "ch");
                    }
                    CuiHelper.AddUi(player, cont);
                    break;
            }
            #endregion
            
        }
    }
}
 