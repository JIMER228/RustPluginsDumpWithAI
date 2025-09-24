 //Thank you for purchasing my plugin!
//It is forbidden to distribute this plugin for free and for a fee.
//It is allowed to modify the plugin.
//Discord of BUDAPESHTER#7439
using System;
using System.Collections.Generic;
using System.Linq;
using ConVar;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SvPanUi", "BUDAPESHTER#7439", "2.2.5")]
    [Description("A great plugin for displaying server events and online players!")]
    class SvPanUi : RustPlugin
    {
        private PluginConfig _config;
        public string Layer = "LayerUi";
        public bool IsAir, IsHeli, IsTank, IsCargo, IsCh;
        public List<string> xClose = new List<string>();

        [PluginReference] 
        private Plugin ImageLibrary, Economics, ServerRewards;
        #region Hooks
        void OnServerInitialized()
        {
            if (!ImageLibrary)
            {
                PrintError("The plugin is not installed on the server [ImageLibrary]");
                Interface.Oxide.UnloadPlugin(Title);
                return;
            }
            if (!Economics) PrintError("The plugin is not installed on the server [Economics]");         
            if (!ServerRewards) PrintError("The plugin is not installed on the server [ServerRewards]");            
            EventCheck();
            if (_config.Logo.LogoStat)
            {
                ImageLibrary.Call("AddImage", _config.Image.Logo, "logo");
            }
            ImageLibrary.Call("AddImage", _config.Image.Bred, "bred");
            ImageLibrary.Call("AddImage", _config.Image.Air, "air");
            ImageLibrary.Call("AddImage", _config.Image.Cargo, "cargo");
            ImageLibrary.Call("AddImage", _config.Image.Heli, "heli");
            ImageLibrary.Call("AddImage", _config.Image.Ch, "ch");
            timer.Once(5, () =>
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    if (player.IsNpc || player.IsSleeping() || player.IsReceivingSnapshot) continue;

                    if (!xClose.Contains($"{player.userID}")) CreatePanel(player);                                         
                }
                if (_config.Time.TimeUi)
                {
                    timer.Every(_config.Time.Timer, () =>
                    {
                        foreach (var players in BasePlayer.activePlayerList) if (!xClose.Contains($"{players.userID}")) EventInit(players, "time");
                    });
                }
                if (_config.Rewards.RewardStats)
                {
                    timer.Every(_config.Rewards.TimeRew, () =>
                    {
                        foreach (var players in BasePlayer.activePlayerList) if (!xClose.Contains($"{players.userID}")) EventInit(players, "sreward");
                    });
                }
            });       
        }       
        object OnPlayerSleep(BasePlayer player)
        {
            foreach (var players in BasePlayer.activePlayerList)
            {
                timer.Once(5, () =>
                {
                    if (!xClose.Contains($"{players.userID}")) EventInit(players, "online");
                });
            }
            return null;
        }
       void OnPlayerSleepEnded(BasePlayer player)
        {
            foreach (var players in BasePlayer.activePlayerList)
            {
                timer.Once(5, () =>
                {
                    if (!xClose.Contains($"{players.userID}")) EventInit(players, "online");
                });
            }
        }
        void Loaded()
        {
            Puts("Loaded SvPanUi!");
        }
        void Unload()
        {
            SaveCom();
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
            }
        }
        private string GetImg(string name)
        {
            return (string)ImageLibrary?.Call("GetImage", name) ?? "";
        }
        #region Config
        private class PluginConfig
        {
            [JsonProperty("MainPanel Settings")]
            public MainSettings MainPanel;
            [JsonProperty("NameServer Settings")]
            public TitleSettings Text;
            [JsonProperty("Economics Settings")]
            public EconomicsSettings Economics;
            [JsonProperty("ServerRewards Settings")]
            public RewardsSettings Rewards;
            [JsonProperty("Time Settings")]
            public TimeSettings Time;
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
            [JsonProperty("Logo Icon Settings")]
            public LogoSettings Logo;

            #region MainSettings
            internal class MainSettings
            {
                [JsonProperty("AnchorMin")] public string AnchorMin;
                [JsonProperty("AnchorMax")] public string AnchorMax;
                [JsonProperty("OffsetMin")] public string OffsetMin;
                [JsonProperty("OffsetMax")] public string OffsetMax;
                [JsonProperty("Overlay or Hud")] public string UiOver;
                [JsonProperty("Chat Tag")] public string Tag;
            }
            #endregion
            #region EconomicsSettings
            internal class EconomicsSettings
            {
                [JsonProperty("On/Off (true/false)")] public bool EconStats;
                [JsonProperty("Parent OffsetMin")] public string OffsetMin;
                [JsonProperty("Parent OffsetMax")] public string OffsetMax;
                [JsonProperty("Parent Color")] public string ParenColor;
                [JsonProperty("$ OffsetMin")] public string OffsetMinSign;
                [JsonProperty("$ OffsetMax")] public string OffsetMaxSign;
                [JsonProperty("$ font size")] public int SizeFontSign;
                [JsonProperty("$ text color")] public string ColorSign;
                [JsonProperty("$ text")] public string TextSign;
                [JsonProperty("Balance text color")] public string ColorBal;
                [JsonProperty("Balance text font size")] public int SizeFontBal;
                [JsonProperty("Balance text OffsetMin")] public string OffsetMinBal;
                [JsonProperty("Balance text OffsetMax")] public string OffsetMaxBal;
            }
            #endregion
            #region RewardsSettings
            internal class RewardsSettings
            {
                [JsonProperty("On/Off (true/false)")] public bool RewardStats;
                [JsonProperty("Parent OffsetMin")] public string OffsetMin;
                [JsonProperty("Parent OffsetMax")] public string OffsetMax;
                [JsonProperty("Parent Color")] public string ParenColor;
                [JsonProperty("RP OffsetMin")] public string OffsetMinSign;
                [JsonProperty("RP OffsetMax")] public string OffsetMaxSign;
                [JsonProperty("RP font size")] public int SizeFontSign;
                [JsonProperty("RP text color")] public string ColorSign;
                [JsonProperty("RP text")] public string TextSign;
                [JsonProperty("Points color")] public string ColorRew;
                [JsonProperty("Points font size")] public int SizeFontRew;
                [JsonProperty("Points OffsetMin")] public string OffsetMinRew;
                [JsonProperty("Points OffsetMax")] public string OffsetMaxRew;
                [JsonProperty("Timer")] public int TimeRew;
            }
            #endregion
            #region TimeSettings
            internal class TimeSettings
            {
                [JsonProperty("AnchorMin")] public string AnchorMin;
                [JsonProperty("AnchorMax")] public string AnchorMax;
                [JsonProperty("OffsetMin")] public string OffsetMin;
                [JsonProperty("OffsetMax")] public string OffsetMax;
                [JsonProperty("Font size")] public int SizeFont;
                [JsonProperty("Font")] public string Font;
                [JsonProperty("Align")] public TextAnchor Align;
                [JsonProperty("Timer")] public int Timer;
                [JsonProperty("Time true/false")] public bool TimeUi;
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
            }
            #endregion
            #region ImageSettings
            internal class ImageSettings //images
            {
                [JsonProperty("Color of inactive images")] public string Color;
                [JsonProperty("Color of active images")] public string ColorAk;
                [JsonProperty("Picture Bradley")] public string Bred;
                [JsonProperty("Picture Air")] public string Air;
                [JsonProperty("Picture Cargo")] public string Cargo;
                [JsonProperty("Picture Heli")] public string Heli;
                [JsonProperty("Picture Ch")] public string Ch;
                [JsonProperty("Picture Logo")] public string Logo;

            }
            #endregion
            #region OnlinePanelSettings
            internal class OnlineSettings //onlinepanel
            {
                [JsonProperty("TextOnline")] public string OnlineTxt;
                [JsonProperty("ColorText")] public string ColorText;
                [JsonProperty("Align")] public TextAnchor Align;
                [JsonProperty("Font size")] public int SizeFont;
                [JsonProperty("Font")] public string Font;
                [JsonProperty("AnchorMin")] public string AnchorMin;
                [JsonProperty("AnchorMax")] public string AnchorMax;
                [JsonProperty("OffsetMin")] public string OffsetMin;
                [JsonProperty("OffsetMax")] public string OffsetMax;

            }
            #endregion
            #region Icon settings
            internal class LogoSettings
            {
                [JsonProperty("On/Off Logo(true/false)")] public bool LogoStat;
                [JsonProperty("AnchorMin")] public string AnchorMin;
                [JsonProperty("AnchorMax")] public string AnchorMax;
                [JsonProperty("OffsetMin")] public string OffsetMin;
                [JsonProperty("OffsetMax")] public string OffsetMax;
            }
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
                    UiOver = "Hud",
                    Tag = "SvPanUi",

                },
                Economics = new PluginConfig.EconomicsSettings()
                {
                    EconStats = true,
                    OffsetMin = "270 -25",
                    OffsetMax = "370 -5",
                    ParenColor = "0.11 0.38 0.16 0.7",
                    OffsetMinSign = "0 0",
                    OffsetMaxSign = "30 20",
                    SizeFontSign = 14,
                    ColorSign = "#fff",
                    TextSign = "$",
                    OffsetMinBal = "30 0",
                    OffsetMaxBal = "90 20",
                    ColorBal = "#fff",
                    SizeFontBal = 14         
                },
                Rewards = new PluginConfig.RewardsSettings()
                {
                    RewardStats = true,
                    OffsetMin = "270 -60",
                    OffsetMax = "370 -40",
                    ParenColor = "0.11 0.38 0.16 0.7",
                    OffsetMinSign = "0 0",
                    OffsetMaxSign = "30 20",
                    SizeFontSign = 14,
                    ColorSign = "#fff",
                    TextSign = "RP",
                    OffsetMinRew = "30 0",
                    OffsetMaxRew = "90 20",
                    ColorRew = "#fff",
                    SizeFontRew = 14,
                    TimeRew = 30
                },
                Time = new PluginConfig.TimeSettings()
                {
                    AnchorMin = "0 0",
                    AnchorMax = "0 0",
                    OffsetMin = "5 -78",
                    OffsetMax = "59 -60",
                    Font = "robotocondensed-bold.ttf",
                    SizeFont = 14,
                    Align = TextAnchor.MiddleCenter,
                    Timer = 10,
                    TimeUi = true
                },
                Image = new PluginConfig.ImageSettings()
                {
                    Color = "0 0 0 0.90",
                    ColorAk = "255 106 0 0.90",
                    Bred = "https://i.ibb.co/pLg31pD/22.png",
                    Air = "https://i.ibb.co/gdqxJp8/AirSv.png",
                    Cargo = "https://i.ibb.co/8sxWfrJ/4.png",
                    Heli = "https://i.ibb.co/cXNqzNd/1.png",
                    Ch = "https://i.ibb.co/wdLvftb/ChSv.png",
                    Logo = "https://i.ibb.co/WPpDvJk/image.png"
                },

                Text = new PluginConfig.TitleSettings()
                {
                    TitleText = "SERVERNAME",
                    Color = "1 1 1 0.74",
                    SizeFont = 16,
                    AnchorMin = "0 0",
                    AnchorMax = "0 0",
                    OffsetMin = "65 -24",
                    OffsetMax = "255 -2",
                    Font = "robotocondensed-bold.ttf",
                    Align = TextAnchor.MiddleCenter
                },
                OnlinePanel = new PluginConfig.OnlineSettings()
                {
                    OnlineTxt = "<size=12><color=#fff>ONLINE</color>: <color=#fff>{0}</color> | <color=#fff>OFFLINE</color>: <color=#fff>{1}</color> | <color=#fff>JOIN</color>: <color=#fff>{2}</color></size>",
                    SizeFont = 12,
                    ColorText = "#ffffffe9",
                    Font = "robotocondensed-bold.ttf",
                    Align = TextAnchor.MiddleCenter,
                    AnchorMin = "0 0",
                    AnchorMax = "0 0",
                    OffsetMin = "65 -60",
                    OffsetMax = "255 -45"
                },
                Logo = new PluginConfig.LogoSettings()
                {
                    AnchorMin = "0 0",
                    AnchorMax = "0 0",
                    OffsetMin = "3 -56",
                    OffsetMax = "60 -8",
                    LogoStat = true
                },
                Bradley = new PluginConfig.BradleySettings()
                {
                    AnchorMin = "0 0",
                    AnchorMax = "0 0",
                    OffsetMin = "65 -45",
                    OffsetMax = "95 -20"
                },
                Air = new PluginConfig.AirSettings()
                {
                    AnchorMin = "0 0",
                    AnchorMax = "0 0",
                    OffsetMin = "105 -45",
                    OffsetMax = "135 -20"
                },
                Cargo = new PluginConfig.CargoSettings()
                {
                    AnchorMin = "0 0",
                    AnchorMax = "0 0",
                    OffsetMin = "145 -45",
                    OffsetMax = "175 -20"
                },
                Heli = new PluginConfig.HeliSettings()
                {
                    AnchorMin = "0 0",
                    AnchorMax = "0 0",
                    OffsetMin = "185 -45",
                    OffsetMax = "215 -20"
                },
                Ch = new PluginConfig.ChSettings()
                {
                    AnchorMin = "0 0",
                    AnchorMax = "0 0",
                    OffsetMin = "225 -45",
                    OffsetMax = "255 -20"
                }

            };
        }
        protected override void LoadConfig() //load config
        {
            base.LoadConfig();
            _config = Config.ReadObject<PluginConfig>();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        #endregion
        #region Data
        private void Init()
        {
            LoadData();
        }
        private void OnServerSave()
        {
            int randomSave = UnityEngine.Random.Range(5, 20);
            timer.Once(randomSave, () =>
            {
                SaveCom();
            });            
        }
        private void SaveCom()
        {
            Interface.Oxide.DataFileSystem.WriteObject($"{Name}/PlayerList", xClose);
        }
        private void LoadData()
        {
            try
            {
                xClose = Interface.Oxide.DataFileSystem.ReadObject<List<string>>($"{Name}/PlayerList");
            }
            catch (Exception e)
            {
                PrintError(e.ToString());
            }
            if (xClose == null) xClose = new List<string>();
        }
        #endregion
        private void OnPlayerConnected(BasePlayer player)
        {
            if (!xClose.Contains($"{player.userID}")) CreatePanel(player);
            
            foreach (var players in BasePlayer.activePlayerList)
            {
                timer.Once(3, () =>
                {
                    if (!xClose.Contains($"{players.userID}")) EventInit(players, "online");
                });
            }
        }
        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            foreach (var players in BasePlayer.activePlayerList)
            {
                timer.Once(3, () =>
                {
                    if (!xClose.Contains($"{players.userID}")) EventInit(players, "online");
                });
            }
        }
        #endregion
        #region Economics Hook
        private void OnEconomicsBalanceUpdated(string playerId, double amount)
        {
            BasePlayer player = BasePlayer.FindAwakeOrSleeping(playerId);
            if (player == null || !player.IsConnected) return;
            if (!xClose.Contains($"{player.userID}")) EventInit(player, "balance");
        }
        #endregion
        #region Commands
        [ChatCommand("xclose")]
        void ClosePanel(BasePlayer player)
        {
            if (!xClose.Contains($"{player.userID}"))
            {
                xClose.Add($"{player.userID}");
                CuiHelper.DestroyUi(player, Layer);
                SendChat(player, String.Format(lang.GetMessage("ClosePanel", this), _config.MainPanel.Tag));
            }
            else 
            {
                xClose.Remove($"{player.userID}");
                CreatePanel(player);
                SendChat(player, String.Format(lang.GetMessage("OpenPanel", this), _config.MainPanel.Tag));
            }           
        }
        #endregion
        #region UI
        void CreatePanel(BasePlayer player)
        {
            if (player == null) return;
            var SleepersCount = BasePlayer.sleepingPlayerList.Count.ToString();
            var JoiningCount = ServerMgr.Instance.connectionQueue.Joining.ToString();
            CuiHelper.DestroyUi(player, Layer);
            var cont = new CuiElementContainer //parent panel
            {
                {
                    new CuiPanel {
                        RectTransform = 
                        {
                            AnchorMin = _config.MainPanel.AnchorMin, 
                            AnchorMax = _config.MainPanel.AnchorMax
                        },
                        Image = 
                        {
                            Color = "0 0 0 0"
                        }
                    },
                    _config.MainPanel.UiOver, Layer
                }
            };
            //add logo
            if (_config.Logo.LogoStat)
            {
                cont.Add(new CuiElement
                {
                    Parent = Layer,
                    Name = "Logo",
                    Components =
                    {
                        new CuiRawImageComponent {  Png = GetImg("logo") },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = _config.Logo.AnchorMin,
                            AnchorMax = _config.Logo.AnchorMax,
                            OffsetMin = _config.Logo.OffsetMin,
                            OffsetMax = _config.Logo.OffsetMax
                        }
                    }
                });
            }
            
            //add time
            if (_config.Time.TimeUi)
            {
                cont.Add(new CuiLabel
                {
                    RectTransform =
                    {
                    AnchorMin = _config.Time.AnchorMin,
                    AnchorMax = _config.Time.AnchorMax,
                    OffsetMin = _config.Time.OffsetMin,
                    OffsetMax = _config.Time.OffsetMax
                    },
                    Text = { Text = TOD_Sky.Instance.Cycle.DateTime.ToString("HH:mm"),
                    FontSize = _config.Time.SizeFont,
                    Font = _config.Time.Font,
                    Align = _config.Time.Align
                    }
                }, Layer, Layer + ".Time");
            }
            //add name
            cont.Add(new CuiElement
            {
                Parent = Layer,
                Name = ".Name",
                Components = {
                     new CuiTextComponent()
                     {
                        Color = _config.Text.Color,
                        Text = _config.Text.TitleText,
                        FontSize = _config.Text.SizeFont,
                        Align = _config.Text.Align,
                        Font = _config.Text.Font

                     },
                      new CuiOutlineComponent 
                      { 
                          Color = "0.3 0.3 0.3 0.62", 
                          Distance = "0.5 0.5" 
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
            //add brad
            cont.Add(new CuiPanel()
            {
                RectTransform =
                            {
                            AnchorMin = _config.Bradley.AnchorMin,
                            AnchorMax = _config.Bradley.AnchorMax,
                            OffsetMin = _config.Bradley.OffsetMin,
                            OffsetMax = _config.Bradley.OffsetMax
                            },

                Image =
                            {
                                Color = _config.Image.Color, Png = GetImg("bred")
                            },
            }, Layer, Layer + ".Brad");
            //add Air
            cont.Add(new CuiPanel()
            {
                RectTransform =
                            {
                            AnchorMin = _config.Air.AnchorMin,
                            AnchorMax = _config.Air.AnchorMax,
                            OffsetMin = _config.Air.OffsetMin,
                            OffsetMax = _config.Air.OffsetMax
                            },

                Image =
                            {
                                Color = _config.Image.Color, Png = GetImg("air")
                            },
            }, Layer, Layer + ".Air");
            //add Cargo
            cont.Add(new CuiPanel()
            {
                RectTransform =
                            {
                            AnchorMin = _config.Cargo.AnchorMin,
                            AnchorMax = _config.Cargo.AnchorMax,
                            OffsetMin = _config.Cargo.OffsetMin,
                            OffsetMax = _config.Cargo.OffsetMax
                            },

                Image =
                            {
                                Color = _config.Image.Color, Png = GetImg("cargo")
                            },
            }, Layer, Layer + ".Cargo");
            //add Heli
            cont.Add(new CuiPanel()
            {
                RectTransform =
                            {
                            AnchorMin = _config.Heli.AnchorMin,
                            AnchorMax = _config.Heli.AnchorMax,
                            OffsetMin = _config.Heli.OffsetMin,
                            OffsetMax = _config.Heli.OffsetMax
                            },

                Image =
                            {
                                Color = _config.Image.Color, Png = GetImg("heli")
                            },
            }, Layer, Layer + ".Heli");
            //add Ch
            cont.Add(new CuiPanel()
            {
                RectTransform =
                            {
                            AnchorMin = _config.Ch.AnchorMin,
                            AnchorMax = _config.Ch.AnchorMax,
                            OffsetMin = _config.Ch.OffsetMin,
                            OffsetMax = _config.Ch.OffsetMax
                            },

                Image =
                            {
                                Color = _config.Image.Color, Png = GetImg("ch")
                            },
            }, Layer, Layer + ".Ch");
            //add econom
            if(_config.Economics.EconStats && Economics)
            {
               
                cont.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    RectTransform = 
                    {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = _config.Economics.OffsetMin,
                            OffsetMax = _config.Economics.OffsetMax
                    },
                    Image = 
                    { 
                        Color = _config.Economics.ParenColor
                    }
                }, Layer, Layer + ".Econom");

                cont.Add(new CuiElement
                {
                    Parent = Layer + ".Econom",
                    Name = ".TextEcom",
                    Components = {
                     new CuiTextComponent()
                     {
                        Color = _config.Economics.ColorSign,
                        Text = _config.Economics.TextSign,
                        FontSize = _config.Economics.SizeFontSign,
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf"

                     },
                      new CuiOutlineComponent
                      {
                          Color = "0.3 0.3 0.3 0.62",
                          Distance = "0.5 0.5"
                      },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = _config.Economics.OffsetMinSign,
                            OffsetMax = _config.Economics.OffsetMaxSign
                        }
                }
                });
                cont.Add(new CuiElement
                {
                    Parent = Layer + ".Econom",
                    Name = ".TextBal",
                    Components = {
                     new CuiTextComponent()
                     {
                        Color = _config.Economics.ColorBal,
                        Text = FormattedMoney(player),
                        FontSize = _config.Economics.SizeFontBal,
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf"

                     },
                      new CuiOutlineComponent
                      {
                          Color = "0.3 0.3 0.3 0.62",
                          Distance = "0.5 0.5"
                      },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = _config.Economics.OffsetMinBal,
                            OffsetMax = _config.Economics.OffsetMaxBal
                        }
                }
                });
            }
            //serverreward
            if (_config.Rewards.RewardStats && ServerRewards)
            {
                cont.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    RectTransform =
                    {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = _config.Rewards.OffsetMin,
                            OffsetMax = _config.Rewards.OffsetMax
                    },
                    Image =
                    {
                        Color = _config.Rewards.ParenColor
                    }
                }, Layer, Layer + ".Rp");
                cont.Add(new CuiElement
                {
                    Parent = Layer + ".Rp",
                    Name = ".TextRpSign",
                    Components = {
                     new CuiTextComponent()
                     {
                        Color = _config.Rewards.ColorSign,
                        Text = _config.Rewards.TextSign,
                        FontSize = _config.Rewards.SizeFontSign,
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf"

                     },
                      new CuiOutlineComponent
                      {
                          Color = "0.3 0.3 0.3 0.62",
                          Distance = "0.5 0.5"
                      },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = _config.Rewards.OffsetMinSign,
                            OffsetMax = _config.Rewards.OffsetMaxSign
                        }
                }
                });
                cont.Add(new CuiElement
                {
                    Parent = Layer + ".Rp",
                    Name = ".TextRp",
                    Components = {
                     new CuiTextComponent()
                     {
                        Color = _config.Rewards.ColorRew,
                        Text = $"{GetPoint(player)}",
                        FontSize = _config.Rewards.SizeFontRew,
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf"

                     },
                      new CuiOutlineComponent
                      {
                          Color = "0.3 0.3 0.3 0.62",
                          Distance = "0.5 0.5"
                      },
                      new CuiRectTransformComponent
                      {
                          AnchorMin = "0 0",
                          AnchorMax = "0 0",
                          OffsetMin = _config.Rewards.OffsetMinRew,
                          OffsetMax = _config.Rewards.OffsetMaxRew
                      }
                }
                });
            }
            //add online 
            cont.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".Online",
                Components =
                {

                     new CuiTextComponent { 
                         Color = _config.OnlinePanel.ColorText, FontSize = _config.OnlinePanel.SizeFont, Align = _config.OnlinePanel.Align,
                         Text = string.Format(_config.OnlinePanel.OnlineTxt, BasePlayer.activePlayerList.Count, BasePlayer.sleepingPlayerList.Count,ServerMgr.Instance.connectionQueue.Joining + ServerMgr.Instance.connectionQueue.Queued)
                        },
                    new CuiOutlineComponent { Color = "0 0 0 1", Distance = "0.5 0.5" },
                    new CuiRectTransformComponent {AnchorMin = $"{_config.OnlinePanel.AnchorMin}", AnchorMax = $"{_config.OnlinePanel.AnchorMax}", OffsetMin = $"{_config.OnlinePanel.OffsetMin}", OffsetMax = $"{_config.OnlinePanel.OffsetMax}" }
                }
            });
            
            CuiHelper.AddUi(player, cont);
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
                    if (!xClose.Contains($"{basePlayer.userID}")) EventInit(basePlayer, "heli");
            }
            else if (entity is BradleyAPC)
            {
                IsTank = true;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    if (!xClose.Contains($"{basePlayer.userID}")) EventInit(basePlayer, "bradley");
            }
            else if (entity is CargoPlane)
            {
                IsAir = true;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    if (!xClose.Contains($"{basePlayer.userID}")) EventInit(basePlayer, "air");
            }
            else if (entity is CargoShip)
            {
                IsCargo = true;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    if (!xClose.Contains($"{basePlayer.userID}")) EventInit(basePlayer, "cargo");
            }
            else if (entity is CH47Helicopter)
            {
                IsCh = true;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    if (!xClose.Contains($"{basePlayer.userID}")) EventInit(basePlayer, "ch");
            }
        }
        void OnEntityKill(BaseNetworkable entity)
        {
            if (entity is CargoPlane)
            {
                IsAir = false;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    if (!xClose.Contains($"{basePlayer.userID}")) EventInit(basePlayer, "air");
            }
            else if (entity is CargoShip)
            {
                IsCargo = false;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    if (!xClose.Contains($"{basePlayer.userID}")) EventInit(basePlayer, "cargo");
            }
            else if (entity is BaseHelicopter)
            {
                IsHeli = false;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    if (!xClose.Contains($"{basePlayer.userID}")) EventInit(basePlayer, "heli");
            }
            else if (entity is BradleyAPC)
            {
                IsTank = false;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    if (!xClose.Contains($"{basePlayer.userID}")) EventInit(basePlayer, "bradley");
            }
            else if (entity is CH47Helicopter)
            {
                IsCh = false;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    if (!xClose.Contains($"{basePlayer.userID}")) EventInit(basePlayer, "ch");
            }
        }
        private void EventCheck()
        {
            foreach (var entity in BaseNetworkable.serverEntities.Where(p =>
                p is CargoPlane || p is BradleyAPC || p is BaseHelicopter || p is CargoShip ||
                p is CH47Helicopter))
            {
                if (entity is CargoPlane)
                    IsAir = true;
                if (entity is BradleyAPC)
                    IsTank = true;
                if (entity is BaseHelicopter)
                    IsHeli = true;
                if (entity is CargoShip)
                    IsCargo = true;
                if (entity is CH47Helicopter)
                    IsCh = true;
            }
        }
        #endregion   
        #region Init event
        private void EventInit(BasePlayer player, string type)
        {
            var SleepersCount = BasePlayer.sleepingPlayerList.Count.ToString();
            var JoiningCount = ServerMgr.Instance.connectionQueue.Joining.ToString();
            var cont = new CuiElementContainer();
            switch (type)
            {
                    case "bradley":
                    CuiHelper.DestroyUi(player, Layer + ".Brad");
                    if (IsTank)
                    {
                        //add brad
                        cont.Add(new CuiPanel()
                        {
                            RectTransform =
                            {
                            AnchorMin = _config.Bradley.AnchorMin,
                            AnchorMax = _config.Bradley.AnchorMax,
                            OffsetMin = _config.Bradley.OffsetMin,
                            OffsetMax = _config.Bradley.OffsetMax
                            },

                            Image =
                            {
                                Color = _config.Image.ColorAk, Png = GetImg("bred")
                            },
                        }, Layer, Layer + ".Brad");
                    }
                    else
                    {
                        CuiHelper.DestroyUi(player, Layer + ".Brad");
                        //add brad
                        cont.Add(new CuiPanel()
                        {
                            RectTransform =
                            {
                            AnchorMin = _config.Bradley.AnchorMin,
                            AnchorMax = _config.Bradley.AnchorMax,
                            OffsetMin = _config.Bradley.OffsetMin,
                            OffsetMax = _config.Bradley.OffsetMax
                            },

                            Image =
                            {
                                Color = _config.Image.Color, Png = GetImg("bred")
                            },
                        }, Layer, Layer + ".Brad");
                    }
                    CuiHelper.AddUi(player, cont);
                    break;
                case "air":
                    CuiHelper.DestroyUi(player, Layer + ".Air");
                    if (IsAir)
                    {
                        //add Air
                        cont.Add(new CuiPanel()
                        {
                            RectTransform =
                            {
                            AnchorMin = _config.Air.AnchorMin,
                            AnchorMax = _config.Air.AnchorMax,
                            OffsetMin = _config.Air.OffsetMin,
                            OffsetMax = _config.Air.OffsetMax
                            },

                            Image =
                            {
                                Color = _config.Image.ColorAk, Png = GetImg("air")
                            },
                        }, Layer, Layer + ".Air");
                    }
                    else
                    {
                        CuiHelper.DestroyUi(player, Layer + ".Air");
                        //add Air
                        cont.Add(new CuiPanel()
                        {
                            RectTransform =
                            {
                            AnchorMin = _config.Air.AnchorMin,
                            AnchorMax = _config.Air.AnchorMax,
                            OffsetMin = _config.Air.OffsetMin,
                            OffsetMax = _config.Air.OffsetMax
                            },

                            Image =
                            {
                                Color = _config.Image.Color, Png = GetImg("air")
                            },
                        }, Layer, Layer + ".Air");
                    }
                    CuiHelper.AddUi(player, cont);
                    break;
                case "cargo":
                    CuiHelper.DestroyUi(player, Layer + ".Cargo");
                    if (IsCargo)
                    {
                        //add Cargo
                        cont.Add(new CuiPanel()
                        {
                            RectTransform =
                            {
                            AnchorMin = _config.Cargo.AnchorMin,
                            AnchorMax = _config.Cargo.AnchorMax,
                            OffsetMin = _config.Cargo.OffsetMin,
                            OffsetMax = _config.Cargo.OffsetMax
                            },

                            Image =
                            {
                                Color = _config.Image.ColorAk, Png = GetImg("cargo")
                            },
                        }, Layer, Layer + ".Cargo");
                    }
                    else
                    {
                        CuiHelper.DestroyUi(player, Layer + ".Cargo");
                        //add Cargo
                        cont.Add(new CuiPanel()
                        {
                            RectTransform =
                            {
                            AnchorMin = _config.Cargo.AnchorMin,
                            AnchorMax = _config.Cargo.AnchorMax,
                            OffsetMin = _config.Cargo.OffsetMin,
                            OffsetMax = _config.Cargo.OffsetMax
                            },

                            Image =
                            {
                                Color = _config.Image.Color, Png = GetImg("cargo")
                            },
                        }, Layer, Layer + ".Cargo");
                    }
                    CuiHelper.AddUi(player, cont);
                    break;
                case "heli":
                    CuiHelper.DestroyUi(player, Layer + ".Heli");
                    if (IsHeli)
                    {
                        //add Heli
                        cont.Add(new CuiPanel()
                        {
                            RectTransform =
                            {
                            AnchorMin = _config.Heli.AnchorMin,
                            AnchorMax = _config.Heli.AnchorMax,
                            OffsetMin = _config.Heli.OffsetMin,
                            OffsetMax = _config.Heli.OffsetMax
                            },

                            Image =
                            {
                                Color = _config.Image.ColorAk, Png = GetImg("heli")
                            },
                        }, Layer, Layer + ".Heli");
                    }
                    else
                    {
                        CuiHelper.DestroyUi(player, Layer + ".Heli");
                        //add Heli
                        cont.Add(new CuiPanel()
                        {
                            RectTransform =
                            {
                            AnchorMin = _config.Heli.AnchorMin,
                            AnchorMax = _config.Heli.AnchorMax,
                            OffsetMin = _config.Heli.OffsetMin,
                            OffsetMax = _config.Heli.OffsetMax
                            },

                            Image =
                            {
                                Color = _config.Image.Color, Png = GetImg("heli")
                            },
                        }, Layer, Layer + ".Heli");
                    }
                    CuiHelper.AddUi(player, cont);
                    break;
                case "ch":
                    CuiHelper.DestroyUi(player, Layer + ".Ch");
                    if (IsCh)
                    {
                        //add Ch
                        cont.Add(new CuiPanel()
                        {
                            RectTransform =
                            {
                            AnchorMin = _config.Ch.AnchorMin,
                            AnchorMax = _config.Ch.AnchorMax,
                            OffsetMin = _config.Ch.OffsetMin,
                            OffsetMax = _config.Ch.OffsetMax
                            },

                            Image =
                            {
                                Color = _config.Image.ColorAk, Png = GetImg("ch")
                            },
                        }, Layer, Layer + ".Ch");
                    }
                    else
                    {
                        CuiHelper.DestroyUi(player, Layer + ".Ch");
                        //add Ch
                        cont.Add(new CuiPanel()
                        {
                            RectTransform =
                            {
                            AnchorMin = _config.Ch.AnchorMin,
                            AnchorMax = _config.Ch.AnchorMax,
                            OffsetMin = _config.Ch.OffsetMin,
                            OffsetMax = _config.Ch.OffsetMax
                            },

                            Image =
                            {
                                Color = _config.Image.Color, Png = GetImg("ch")
                            },
                        }, Layer, Layer + ".Ch");
                    }
                    CuiHelper.AddUi(player, cont);
                    break;
                case "online":
                    CuiHelper.DestroyUi(player, Layer + ".Online");
                    //add online 
                    cont.Add(new CuiElement
                    {
                        Parent = Layer,
                        Name = Layer + ".Online",
                        Components =
                    {
                    new CuiTextComponent { Color = _config.OnlinePanel.ColorText, FontSize = _config.OnlinePanel.SizeFont, Align = _config.OnlinePanel.Align,

                            Text = string.Format(_config.OnlinePanel.OnlineTxt, BasePlayer.activePlayerList.Count, BasePlayer.sleepingPlayerList.Count,ServerMgr.Instance.connectionQueue.Joining + ServerMgr.Instance.connectionQueue.Queued)
                        }, 
                    new CuiOutlineComponent { Color = "0 0 0 1", Distance = "0.5 0.5" },
                    new CuiRectTransformComponent {AnchorMin = $"{_config.OnlinePanel.AnchorMin}", AnchorMax = $"{_config.OnlinePanel.AnchorMax}", OffsetMin = $"{_config.OnlinePanel.OffsetMin}", OffsetMax = $"{_config.OnlinePanel.OffsetMax}" }
                    }
                    });

                    CuiHelper.AddUi(player, cont);
                    break;
                case "time":
                    CuiHelper.DestroyUi(player, Layer + ".Time");
                    if (_config.Time.TimeUi)
                    {
                    //add time
                    cont.Add(new CuiLabel
                    {
                    RectTransform =
                    {
                    AnchorMin = _config.Time.AnchorMin,
                    AnchorMax = _config.Time.AnchorMax,
                    OffsetMin = _config.Time.OffsetMin,
                    OffsetMax = _config.Time.OffsetMax
                    },
                    Text = { Text = TOD_Sky.Instance.Cycle.DateTime.ToString("HH:mm"),
                    FontSize = _config.Time.SizeFont,
                    Font = _config.Time.Font,
                    Align = _config.Time.Align
                    }
                        }, Layer, Layer + ".Time");
                    }                   
                    CuiHelper.AddUi(player, cont);
                    break;
                case "balance":
                    CuiHelper.DestroyUi(player, Layer + ".Econom");
                    //add econom
                    if (_config.Economics.EconStats && Economics)
                    {
                        cont.Add(new CuiPanel
                        {
                            CursorEnabled = false,
                            RectTransform =
                    {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = _config.Economics.OffsetMin,
                            OffsetMax = _config.Economics.OffsetMax
                    },
                            Image =
                    {
                        Color = _config.Economics.ParenColor
                    }
                        }, Layer, Layer + ".Econom");

                    cont.Add(new CuiElement
                    {
                        Parent = Layer + ".Econom",
                        Name = ".TextEcom",
                        Components = {
                     new CuiTextComponent()
                     {
                        Color = _config.Economics.ColorSign,
                        Text = _config.Economics.TextSign,
                        FontSize = _config.Economics.SizeFontSign,
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf"

                     },
                    new CuiOutlineComponent
                    {
                        Color = "0.3 0.3 0.3 0.62",
                        Distance = "0.5 0.5"
                    },
                    new CuiRectTransformComponent
                        {
                        AnchorMin = "0 0",
                        AnchorMax = "0 0",
                        OffsetMin = _config.Economics.OffsetMinSign,
                        OffsetMax = _config.Economics.OffsetMaxSign
                        }
                    }
                        });

                    cont.Add(new CuiElement
                    {
                          Parent = Layer + ".Econom",
                          Name = ".TextBal",
                          Components = {
                     new CuiTextComponent()
                     {
                        Color = _config.Economics.ColorBal,
                        Text = FormattedMoney(player),
                        FontSize = _config.Economics.SizeFontBal,
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf"

                     },
                      new CuiOutlineComponent
                      {
                          Color = "0.3 0.3 0.3 0.62",
                          Distance = "0.5 0.5"
                      },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = _config.Economics.OffsetMinBal,
                            OffsetMax = _config.Economics.OffsetMaxBal
                        }
                    }
                        });
                    }
                    CuiHelper.AddUi(player, cont);
                    break;
                case "sreward":
                    CuiHelper.DestroyUi(player, Layer + ".Rp");
                    //serverreward
                    if (_config.Rewards.RewardStats && ServerRewards)
                    {

                        cont.Add(new CuiPanel
                        {
                            CursorEnabled = false,
                            RectTransform =
                    {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = _config.Rewards.OffsetMin,
                            OffsetMax = _config.Rewards.OffsetMax
                    },
                            Image =
                    {
                        Color = _config.Rewards.ParenColor
                    }
                        }, Layer, Layer + ".Rp");

                        cont.Add(new CuiElement
                        {
                            Parent = Layer + ".Rp",
                            Name = ".TextRpSign",
                            Components = {
                     new CuiTextComponent()
                     {
                        Color = _config.Rewards.ColorSign,
                        Text = _config.Rewards.TextSign,
                        FontSize = _config.Rewards.SizeFontSign,
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf"

                     },
                      new CuiOutlineComponent
                      {
                          Color = "0.3 0.3 0.3 0.62",
                          Distance = "0.5 0.5"
                      },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = _config.Rewards.OffsetMinSign,
                            OffsetMax = _config.Rewards.OffsetMaxSign
                        }
                    }
                        });

                        cont.Add(new CuiElement
                        {
                            Parent = Layer + ".Rp",
                            Name = ".TextRp",
                            Components = {
                     new CuiTextComponent()
                     {
                        Color = _config.Rewards.ColorRew,
                        Text = $"{GetPoint(player)}",
                        FontSize = _config.Rewards.SizeFontRew,
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf"

                     },
                      new CuiOutlineComponent
                      {
                          Color = "0.3 0.3 0.3 0.62",
                          Distance = "0.5 0.5"
                      },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = _config.Rewards.OffsetMinRew,
                            OffsetMax = _config.Rewards.OffsetMaxRew
                        }
                    }
                        });
                    }
                    CuiHelper.AddUi(player, cont);
                    break;
            }
        }
        #endregion
        #region Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["OpenPanel"] = "<size=14><color=#ffd500>{0}</color>PANEL IS OPEN</size>",
                ["ClosePanel"] = "<size=14><color=#ffd500>{0}</color>PANEL IS CLOSED</size>"
            }, this);
        }
        #endregion
        #region Help
        public void SendChat(BasePlayer player, string Message, Chat.ChatChannel channel = Chat.ChatChannel.Global) //отправка смс в чат с выбором канала
        {
            player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        private string FormattedMoney(BasePlayer player)
        {
            string balls = string.Format("{0:C}", (double)Economics?.Call("Balance", player.UserIDString));
            balls = balls.Substring(1);
            balls = balls.Remove(balls.Length - 3);
            return balls;
        }
        int GetPoint(BasePlayer player)
        {
            object points = ServerRewards.Call("CheckPoints", player.userID);
            if (points is int)
            {
                return (int)points;
            }
            return 0;
        }
        #endregion
    }
}
