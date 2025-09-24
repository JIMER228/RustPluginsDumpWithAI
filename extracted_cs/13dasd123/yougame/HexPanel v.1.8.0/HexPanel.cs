using System;
using UnityEngine;
using System.Globalization;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("HexPanel", "poof.", "1.8.0")]
    class HexPanel : RustPlugin
    {
        #region Configuration

        private string ShadowSize = "0.2 0.2";
        public string Layer = "Panel";
        public string Layer2 = "Panel2";
		public string Layer3 = "Panel3";

        #region Configuration
        
        private static Configuration _config = new Configuration();

        public class Configuration
        {
			[JsonProperty(PropertyName = "Цвет обводки - формат RGBA / Outline color - format RGBA")]
            public string OutlineColor = "0 0 0 1";
			
			[JsonProperty(PropertyName = "Цвет текста - формат #FFFFFF00 (00 - прозрачность) / Text color - format #FFFFFF00 (00 - transparency)")]
            public string TextColor = "#EBEBEB9B";
			
			[JsonProperty(PropertyName = "Цвет панели (фон) - формат #FFFFFF00 (00 - прозрачность)  / Panel color - format #FFFFFF00 (00 - transparency)")]
            public string PanelColor = "#88888814";
			
			[JsonProperty(PropertyName = "Иконка Часов / Clock Icon")]
            public string ClockIcon = "https://i.imgur.com/CBKTj8B.png";
			
            [JsonProperty(PropertyName = "Иконка Онлайна / Online Icon")]
            public string OnlineIcon = "https://i.imgur.com/lWoUIw2.png";

            [JsonProperty(PropertyName = "Иконка Слипперов / Sleepers Icon")]
            public string SleepersIcon	= "https://i.imgur.com/prW2Gye.png";
            
            [JsonProperty(PropertyName = "Иконка Вертолета / Heli Icon")]
            public string HeliIcon = "https://i.imgur.com/HtAifod.png";
			
            [JsonProperty(PropertyName = "Иконка Корабля / Cargoship Icon")]
            public string CargoIcon = "https://i.imgur.com/xU8IUWO.png";
			
            [JsonProperty(PropertyName = "Иконка Чинука / CH-47 Icon")]
            public string Ch47Icon = "https://i.imgur.com/jRIF5y8.png";
			
            [JsonProperty(PropertyName = "Иконка Аирдропа / Airdrop Icon")]
            public string AirdropIcon = "https://i.imgur.com/CL41EJS.png";
			
			[JsonProperty(PropertyName = "Иконка Вертолета (вызванного) / Heli (called) Icon")]
            public string HeliCalledIcon = "https://i.imgur.com/hJWA60M.png";
			
            [JsonProperty(PropertyName = "Иконка Корабля (вызванного) / Cargoship (called) Icon")]
            public string CargoCalledIcon = "https://i.imgur.com/KDFgOzn.png";
			
            [JsonProperty(PropertyName = "Иконка Чинука (вызванного) / CH-47 (called) Icon")]
            public string Ch47CalledIcon = "https://i.imgur.com/fybhEua.png";
			
            [JsonProperty(PropertyName = "Иконка Аирдропа (вызванного) / Airdrop (called) Icon")]
            public string AirdropCalledIcon = "https://i.imgur.com/8QZswJf.png";

            [JsonProperty(PropertyName = "Положение АвтоСообщений - показывать сверху? [если false - то показываются снизу]")]
            public bool TopAndBottomSwitch = true;
			
            [JsonProperty(PropertyName = "Время автообновления АвтоСообщений (секунды)")]
            public int RefreshRate = 300;
            
            [JsonProperty(PropertyName = "Размер текста АвтоСообщений")]
            public int AutoMessagesTextSize = 15;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();
            }
            catch
            {
                Config.WriteObject(_config, false, $"{Interface.Oxide.ConfigDirectory}/{Name}.jsonError");
                PrintError("The configuration file contains an error and has been replaced with a default config.\n" +
                           "The error configuration file was saved in the .jsonError extension");
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig() => _config = new Configuration();

        protected override void SaveConfig() => Config.WriteObject(_config);
        
        #endregion

        void OnServerInitialized()
        {
			LoadConfig();
            timetimer();
			timetp();
            checks();          
            
            if (!Interface.Oxide.DataFileSystem.ExistsDatafile("HexPanel/Message"))
                Interface.Oxide.DataFileSystem.WriteObject("HexPanel/Message", messageList);

            timer.Once(5, () => messageList = Interface.Oxide.DataFileSystem.ReadObject<List<string>>("HexPanel/Message"));
            timer.Every(_config.RefreshRate, () =>
            {
                foreach (var check in BasePlayer.activePlayerList)
                {
					if (_config.TopAndBottomSwitch)
					{
						DrawGUI2(check);
					}
					if (!_config.TopAndBottomSwitch)
					{
						DrawGUI3(check);
					}
				}
            });

            ImageLibrary.Call("AddImage", _config.OnlineIcon, "online");
            ImageLibrary.Call("AddImage", _config.SleepersIcon, "sleepers");
            ImageLibrary.Call("AddImage", _config.ClockIcon, "clock");
            /* неперекрашенные иконки */
            ImageLibrary.Call("AddImage", _config.Ch47Icon, "chainook");
            ImageLibrary.Call("AddImage", _config.CargoIcon, "cargo");
            ImageLibrary.Call("AddImage", _config.AirdropIcon, "airdrop");
            ImageLibrary.Call("AddImage", _config.HeliIcon, "heli");
            /* иконки перекрашенные */
            ImageLibrary.Call("AddImage", _config.Ch47CalledIcon, "chainookcalled");
            ImageLibrary.Call("AddImage", _config.CargoCalledIcon, "cargocalled");
            ImageLibrary.Call("AddImage", _config.AirdropCalledIcon, "airdropcalled");
            ImageLibrary.Call("AddImage", _config.HeliCalledIcon, "helicalled");


            PrintWarning("Плагин разработан специально для форума Oxide-Russia.ru [Автор плагина: poof.]");
            PrintWarning("Группа разработчика: vk.com/plugdev");
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() =>
                {
                    OnPlayerInit(player);
                    return;
                });
            }
            DrawGUI(player);
        }

        #endregion

        #region MessageList
        
        List<string> messageList = new List<string>
        {
            "<color=#EBEBEB>Я могу использовать</color> <color=#dd0000>цвета</color>",
            "Мой разработчик - <color=#9200d4>poof.</color>",
            "Специально для <color=#ff8d00>Oxide-Russia.Ru</color>"
        };
        
        #endregion
        
        #region Variables

        [PluginReference] private Plugin ImageLibrary;

        private List<ulong> Exclude = new List<ulong>();

        #endregion

        private void DrawGUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);

            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                Image = { Color = HexToCuiColor("#FFFFFF00") },
                RectTransform = { AnchorMin = "0.3445313 0.1138889", AnchorMax = "0.6414062 0.1527778" },
                CursorEnabled = false,
            }, "Hud", Layer);

            #region panels

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent {Color = HexToCuiColor(_config.PanelColor)},
                    new CuiRectTransformComponent
                        {AnchorMin = "-0.002631575 -1.251698E-06", AnchorMax = "0.2184211 0.9999989"},
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent {Color = HexToCuiColor(_config.PanelColor)},
                    new CuiRectTransformComponent
                        {AnchorMin = "0.2263157 -1.251698E-06", AnchorMax = "0.4736842 0.999999"},
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent {Color = HexToCuiColor(_config.PanelColor)},
                    new CuiRectTransformComponent
                        {AnchorMin = "0.4815788 -1.251698E-06", AnchorMax = "0.6710526 0.999999"},
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent {Color = HexToCuiColor(_config.PanelColor)},
                    new CuiRectTransformComponent
                        {AnchorMin = "0.6789483 -1.341107E-06", AnchorMax = "0.7526335 0.9999989"},
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent {Color = HexToCuiColor(_config.PanelColor)},
                    new CuiRectTransformComponent
                        {AnchorMin = "0.7605273 -1.341107E-06", AnchorMax = "0.8342113 0.9999989"},
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent {Color = HexToCuiColor(_config.PanelColor)},
                    new CuiRectTransformComponent
                        {AnchorMin = "0.8421068 -1.341107E-06", AnchorMax = "0.9157909 0.9999989"},
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent {Color = HexToCuiColor(_config.PanelColor)},
                    new CuiRectTransformComponent
                        {AnchorMin = "0.9236855 -1.341107E-06", AnchorMax = "0.9921066 0.9999987"},
                }
            });

            #endregion
			
            #region texts

            container.Add(new CuiElement
            {
                Name = "Time",
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent()
                    {
                        Color = HexToCuiColor(_config.TextColor), Text = TOD_Sky.Instance.Cycle.DateTime.ToString("HH:mm"),
                        FadeIn = 1f, FontSize = 18,
                        Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.0605263 -1.594424E-06", AnchorMax = "0.2184211 0.9999998"
                    },
                    new CuiOutlineComponent()
                    {
                        Color = _config.OutlineColor, Distance = ShadowSize
                    }
                }
            });

            #endregion

            #region icons
			
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent()
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "clock"), Color = HexToCuiColor(_config.TextColor),
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.01578952 0.1428557",
                        AnchorMax = "0.0684211 0.8571426"
                    },
                    new CuiOutlineComponent()
                    {
                        Color = _config.OutlineColor, Distance = ShadowSize
                    }
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent()
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "online"), Color = HexToCuiColor(_config.TextColor),
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.2421052 0.1428557",
                        AnchorMax = "0.2947366 0.8571426"
                    },
                    new CuiOutlineComponent()
                    {
                        Color = _config.OutlineColor, Distance = ShadowSize
                    }
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent()
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "sleepers"), Color = HexToCuiColor(_config.TextColor),
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.5052641 0.1071413",
                        AnchorMax = "0.5657896 0.9285712"
                    },
                    new CuiOutlineComponent()
                    {
                        Color = _config.OutlineColor, Distance = ShadowSize
                    }
                }
            });
            container.Add(new CuiElement
            {
                Name = "HeliIcon",
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent()
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "heli"), Color = HexToCuiColor(_config.TextColor),
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.6868431 0.1785704",
                        AnchorMax = "0.7526316 0.8214285"
                    },
                    new CuiOutlineComponent()
                    {
                        Color = _config.OutlineColor, Distance = ShadowSize
                    }
                }
            });
            container.Add(new CuiElement
            {
                Name = "CargoIcon",
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent()
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "cargo"), Color = HexToCuiColor(_config.TextColor),
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.7684221 0.1785704",
                        AnchorMax = "0.831579 0.8214285"
                    },
                    new CuiOutlineComponent()
                    {
                        Color = _config.OutlineColor, Distance = ShadowSize
                    }
                }
            });
            container.Add(new CuiElement
            {
                Name = "ChinukIcon",
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent()
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "chainook"), Color = HexToCuiColor(_config.TextColor),
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.8500016 0.142856",
                        AnchorMax = "0.9131579 0.8928571"
                    },
                    new CuiOutlineComponent()
                    {
                        Color = _config.OutlineColor, Distance = ShadowSize
                    }
                }
            });
            container.Add(new CuiElement
            {
                Name = "AidropIcon",
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent()
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "airdrop"), Color = HexToCuiColor(_config.TextColor),
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.9289487 0.142856",
                        AnchorMax = "0.9842119 0.8571427"
                    },
                    new CuiOutlineComponent()
                    {
                        Color = _config.OutlineColor, Distance = ShadowSize
                    }
                }
            });

            CuiHelper.AddUi(player, container);

            #endregion
        }

		#region timetimer
		
        void timetimer()
        {
            Timer tim = timer.Repeat(1, 0, () =>
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    CuiHelper.DestroyUi(player, "Time");
                    var container = new CuiElementContainer();
                    container.Add(new CuiElement
                    {
                        Name = "Time",
                        Parent = Layer,
                        Components =
                                {
                                   new CuiTextComponent()
                                {
                                    Color = HexToCuiColor(_config.TextColor),
                                    Text = TOD_Sky.Instance.Cycle.DateTime.ToString("HH:mm"), FontSize = 18,
                                    Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf"
                                },
                                new CuiRectTransformComponent
                                {AnchorMin = "0.0605263 -1.594424E-06", AnchorMax = "0.2184211 0.9999998"},
                                new CuiOutlineComponent()
                                {
                                    Color = _config.OutlineColor, Distance = ShadowSize
                                }
                        }
                    });

                    if (Exclude.Contains(player.userID)) continue;

                    CuiHelper.AddUi(player, container);
                }
            });
        }
		
		#endregion
		
		#region OnlineSleepersTimer
		
        void timetp()
        {
            Timer tim = timer.Repeat(1, 0, () =>
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    CuiHelper.DestroyUi(player, "Sleepers");
                    CuiHelper.DestroyUi(player, "Online");
                    var container = new CuiElementContainer();
                    container.Add(new CuiElement
                    {
						Name = "Online",
                        Parent = Layer,
                        Components =
                     {
                       new CuiTextComponent()
                        {
                          Color = HexToCuiColor(_config.TextColor),
                          Text = BasePlayer.activePlayerList.Count + "/" + ConVar.Server.maxplayers, FontSize = 18,
                          Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf"
                        },
                        new CuiRectTransformComponent
                        {
                        AnchorMin = "0.3157894 -1.594424E-06", AnchorMax = "0.5105259 0.9999998"
                        },
                       new CuiOutlineComponent()
                       {
                        Color = _config.OutlineColor, Distance = ShadowSize
                       }
                     }
                    });
					
                    container.Add(new CuiElement
                    {
						Name = "Sleepers",
                        Parent = Layer,
                        Components =
                     {
                      new CuiTextComponent()
                      {
                        Color = HexToCuiColor(_config.TextColor),
                        Text = BasePlayer.sleepingPlayerList.Count.ToString(), FontSize = 18,
                        Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf"
                      },
                      new CuiRectTransformComponent
                      {
                        AnchorMin = "0.5605265 -1.594424E-06", AnchorMax = "0.6657894 0.9999998"
                      },
                      new CuiOutlineComponent()
                      {
                        Color = _config.OutlineColor, Distance = ShadowSize
                      }
                     }
                    });


                    if (Exclude.Contains(player.userID)) continue;

                    CuiHelper.AddUi(player, container);
                }
            });
        }
		
		#endregion
		
		#region Checks
		
        void checks()
        {
            Timer timers1 = timer.Repeat(5, 0, () =>
            {
                var ActiveHelicopters = UnityEngine.Object.FindObjectsOfType<BaseHelicopter>().ToList();
                ActiveHelicopters.RemoveAll(p => !p.IsValid() || !p.gameObject.activeInHierarchy);
                if (ActiveHelicopters.Count > 0)
                {
                    var container = new CuiElementContainer();
                    container.Add(new CuiElement
                    {
                        Name = "HeliIcon",
                        Parent = Layer,
                        Components =
                        {
                            new CuiRawImageComponent()
                            {
                                Png = (string) ImageLibrary.Call("GetImage", "helicalled"),
                            },
                            new CuiRectTransformComponent()
                            {
                                AnchorMin = "0.6868431 0.1785704",
                                AnchorMax = "0.7526316 0.8214285"
							},
							new CuiOutlineComponent()
							{
								Color = _config.OutlineColor, Distance = ShadowSize
							}
							
                        }
                    });
                    foreach (var players in BasePlayer.activePlayerList)
                    {
                        CuiHelper.DestroyUi(players, "HeliIcon");

                        if (Exclude.Contains(players.userID)) continue;

                        CuiHelper.AddUi(players, container);
                    }
                }
                else
                {
                    var container = new CuiElementContainer();
                    container.Add(new CuiElement
                    {
                        Name = "HeliIcon",
                        Parent = Layer,
                        Components =
                        {
                            new CuiRawImageComponent()
                            {
                                Png = (string) ImageLibrary.Call("GetImage", "heli"),
                                Color = HexToCuiColor(_config.TextColor),
                            },
                            new CuiRectTransformComponent()
                            {
                                AnchorMin = "0.6868431 0.1785704",
                                AnchorMax = "0.7526316 0.8214285"
							},
							new CuiOutlineComponent()
							{
								Color = _config.OutlineColor, Distance = ShadowSize
							}
                        }
                    });
                    foreach (var players in BasePlayer.activePlayerList)
                    {
                        CuiHelper.DestroyUi(players, "HeliIcon");

                        if (Exclude.Contains(players.userID)) continue;

                        CuiHelper.AddUi(players, container);
                    }
                }

                var ActiveChinuk = UnityEngine.Object.FindObjectsOfType<CH47Helicopter>().ToList();
                ActiveChinuk.RemoveAll(p => !p.IsValid() || !p.gameObject.activeInHierarchy);
                if (ActiveChinuk.Count > 0)
                {
                    var container = new CuiElementContainer();
                    container.Add(new CuiElement
                    {
                        Name = "ChinukIcon",
                        Parent = Layer,
                        Components =
                        {
                            new CuiRawImageComponent()
                            {
                                Png = (string) ImageLibrary.Call("GetImage", "chainookcalled"),
                            },
                            new CuiRectTransformComponent()
                            {
                                AnchorMin = "0.8500016 0.142856",
                                AnchorMax = "0.9131579 0.8928571"
							},
							new CuiOutlineComponent()
							{
								Color = _config.OutlineColor, Distance = ShadowSize
							}
                        }
                    });
                    foreach (var players in BasePlayer.activePlayerList)
                    {
                        CuiHelper.DestroyUi(players, "ChinukIcon");

                        if (Exclude.Contains(players.userID)) continue;

                        CuiHelper.AddUi(players, container);
                    }

                    return;
                }
                else
                {
                    var container = new CuiElementContainer();
                    container.Add(new CuiElement
                    {
                        Name = "ChinukIcon",
                        Parent = Layer,
                        Components =
                        {
                            new CuiRawImageComponent()
                            {
                                Png = (string) ImageLibrary.Call("GetImage", "chainook"),
                                Color = HexToCuiColor(_config.TextColor),
                            },
                            new CuiRectTransformComponent()
                            {
                                AnchorMin = "0.8500016 0.142856",
                                AnchorMax = "0.9131579 0.8928571"
							},
							new CuiOutlineComponent()
							{
								Color = _config.OutlineColor, Distance = ShadowSize
							}
                        }
                    });
                    foreach (var players in BasePlayer.activePlayerList)
                    {
                        CuiHelper.DestroyUi(players, "ChinukIcon");

                        if (Exclude.Contains(players.userID)) continue;

                        CuiHelper.AddUi(players, container);
                    }
                }

                var AirdropCheck = UnityEngine.Object.FindObjectsOfType<CargoPlane>().ToList();
                AirdropCheck.RemoveAll(p => !p.IsValid() || !p.gameObject.activeInHierarchy);
                if (AirdropCheck.Count > 0)
                {
                    var container = new CuiElementContainer();
                    container.Add(new CuiElement
                    {
                        Name = "AirdropIcon",
                        Parent = Layer,
                        Components =
                        {
                            new CuiRawImageComponent()
                            {
                                Png = (string) ImageLibrary.Call("GetImage", "airdropcalled"),
                            },
                            new CuiRectTransformComponent()
                            {
								AnchorMin = "0.9289487 0.142856",
								AnchorMax = "0.9842119 0.8571427"
							},
							new CuiOutlineComponent()
							{
								Color = _config.OutlineColor, Distance = ShadowSize
							}
                        }
                    });
                    foreach (var players in BasePlayer.activePlayerList)
                    {
                        CuiHelper.DestroyUi(players, "AirdropIcon");

                        if (Exclude.Contains(players.userID)) continue;

                        CuiHelper.AddUi(players, container);
                    }

                    return;
                }
                else
                {
                    var container = new CuiElementContainer();
                    container.Add(new CuiElement
                    {
                        Name = "AirdropIcon",
                        Parent = Layer,
                        Components =
                        {
                            new CuiRawImageComponent()
                            {
                                Png = (string) ImageLibrary.Call("GetImage", "airdrop"),
                                Color = HexToCuiColor(_config.TextColor),
                            },
                            new CuiRectTransformComponent()
                            {
								AnchorMin = "0.9289487 0.142856",
								AnchorMax = "0.9842119 0.8571427"
							},
							new CuiOutlineComponent()
							{
								Color = _config.OutlineColor, Distance = ShadowSize
							}
                        }
                    });
                    foreach (var players in BasePlayer.activePlayerList)
                    {
                        CuiHelper.DestroyUi(players, "AirdropIcon");

                        if (Exclude.Contains(players.userID)) continue;

                        CuiHelper.AddUi(players, container);
                    }
                }

                var CargoShipCheck = UnityEngine.Object.FindObjectsOfType<CargoShip>().ToList();
                CargoShipCheck.RemoveAll(p => !p.IsValid() || !p.gameObject.activeInHierarchy);
                if (CargoShipCheck.Count > 0)
                {
                    var container = new CuiElementContainer();
                    container.Add(new CuiElement
                    {
                        Name = "CargoShipIcon",
                        Parent = Layer,
                        Components =
                        {
                            new CuiRawImageComponent()
                            {
                                Png = (string) ImageLibrary.Call("GetImage", "cargocalled"),
                            },
                            new CuiRectTransformComponent()
                            {
                                AnchorMin = "0.7684221 0.1785704",
                                AnchorMax = "0.831579 0.8214285"
							},
							new CuiOutlineComponent()
							{
								Color = _config.OutlineColor, Distance = ShadowSize
							}
                        }
                    });
                    foreach (var players in BasePlayer.activePlayerList)
                    {
                        CuiHelper.DestroyUi(players, "CargoShipIcon");

                        if (Exclude.Contains(players.userID)) continue;

                        CuiHelper.AddUi(players, container);
                    }

                    return;
                }
                else
                {
                    var container = new CuiElementContainer();
                    container.Add(new CuiElement
                    {
                        Name = "CargoShipIcon",
                        Parent = Layer,
                        Components =
                        {
                            new CuiRawImageComponent()
                            {
                                Png = (string) ImageLibrary.Call("GetImage", "cargo"),
                                Color = HexToCuiColor(_config.TextColor),
                            },
                            new CuiRectTransformComponent()
                            {
                                AnchorMin = "0.7684221 0.1785704",
                                AnchorMax = "0.831579 0.8214285"
							},
							new CuiOutlineComponent()
							{
								Color = _config.OutlineColor, Distance = ShadowSize
							}
                        }
                    });
                    foreach (var players in BasePlayer.activePlayerList)
                    {
                        CuiHelper.DestroyUi(players, "CargoShipIcon");

                        if (Exclude.Contains(players.userID)) continue;

                        CuiHelper.AddUi(players, container);
                    }
                }
            });
        }
		
		#endregion
		
        private void DrawGUI2(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer2);

            var Panel = new CuiElementContainer();

            Panel.Add(new CuiPanel
            {
                Image = { Color = HexToCuiColor("#FFFFFF00") },
                RectTransform = { AnchorMin = "0.3437501 0.1583335", AnchorMax = "0.6406249 0.2" },
                CursorEnabled = false,
            }, "Hud", Layer2);
			
            string message = messageList.GetRandom();
            Panel.Add(new CuiElement
            {
                Parent = Layer2,
                Components = {
                    new CuiTextComponent() {Text = message, Align = TextAnchor.MiddleCenter,  FontSize = _config.AutoMessagesTextSize },
                    new CuiRectTransformComponent { AnchorMin = "0.01842077 -0.06667048", AnchorMax = "0.9815794 0.7999995" },
                    new CuiOutlineComponent() { Color = _config.OutlineColor, Distance = ShadowSize }
                }
            });

            CuiHelper.AddUi(player, Panel);
        }
		
        private void DrawGUI3(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer3);

            var Panel4 = new CuiElementContainer();

            Panel4.Add(new CuiPanel
            {
                Image = { Color = HexToCuiColor("#FFFFFF00") },
                RectTransform = { AnchorMin = "0.3437501 -0.01666705", AnchorMax = "0.6406249 0.02499948" },
                CursorEnabled = false,
            }, "Hud", Layer3);

            string message = messageList.GetRandom();
            Panel4.Add(new CuiElement
            {
                Parent = Layer3,
                Components = {
                    new CuiTextComponent() {Text = message, Align = TextAnchor.MiddleCenter,  FontSize = _config.AutoMessagesTextSize },
                    new CuiRectTransformComponent { AnchorMin = "0.01842078 0.2083306", AnchorMax = "0.9815794 1.075001" },
                    new CuiOutlineComponent() { Color = _config.OutlineColor, Distance = ShadowSize }
                }
            });

            CuiHelper.AddUi(player, Panel4);
        }
		
		void Unload()
        {
            foreach(var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
                CuiHelper.DestroyUi(player, Layer2);
				CuiHelper.DestroyUi(player, Layer3);
            }
        }
		
        #region Helpers

        private static string HexToCuiColor(string hex)
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

        private void GetConfig<T>(string menu, string Key, ref T var)
        {
            if (Config[menu, Key] != null)
            {
                var = (T)Convert.ChangeType(Config[menu, Key], typeof(T));
            }

            Config[menu, Key] = var;
        }

        #endregion

    }
}