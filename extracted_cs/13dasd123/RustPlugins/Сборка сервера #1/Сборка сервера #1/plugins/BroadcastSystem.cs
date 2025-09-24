using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BroadcastSystem","https://discord.gg/9vyTXsJyKR","1.0")]
    public class BroadcastSystem : RustPlugin
    {
        #region var

        [PluginReference] private Plugin ImageLibrary;
        
        Dictionary<string,string> _replaceKeys = new Dictionary<string, string>
        {
            
            ["online"] = "GetOnline",
            ["sleppers"] = "Sleepers",
            ["connecting"] = "GetConnectingPlayers",
            ["queue"] = "Getqueue",
            ["all"] = "GetAllPlayers"
        };
        
        Notification Bradly = new Notification
        {
            Duration = 10f,
            IsDefault = true,
            Sound = "assets/bundled/prefabs/fx/notice/stack.world.fx.prefab",
            Title = "ОБНАРЖЕН БМП",
            Text = "'БРЕДЛИ' ПЕРЕВОЗИТ ЦЕННОЕ ПЕХОТНОЕ СНАРЯЖЕНИЕ!"
        };
        
        Notification Heli = new Notification
        {
            Duration = 10f,
            IsDefault = true,
            Sound = "assets/bundled/prefabs/fx/notice/stack.world.fx.prefab",
            Title = "ОБНАРУЖЕН ВЕРТОЛЕТ",
            Text = "ПОСТАРАЙТЕСЬ СБИТЬ ВЕРТОЛЕТ, ЧТОБЫ ПОЛУЧИТЬ ПЕРЕВОЗИМОЕ СНАРЯЖЕНИЕ!"
        };
        Notification Cargo = new Notification
        {
            Duration = 10f,
            IsDefault = true,
            Sound = "assets/bundled/prefabs/fx/notice/stack.world.fx.prefab",
            Title = "ОБНАРУЖЕН ФРЕЙТЕР",
            Text = "ПОСТАРАЙТЕСЬ ПЕРЕХВАТИТЬ КОРАБЛЬ С ЦЕННЫМ ГРУЗОМ!"
        };
        
        Notification Ch47 = new Notification
        {
            Duration = 10f,
            IsDefault = true,
            Sound = "assets/bundled/prefabs/fx/notice/stack.world.fx.prefab",
            Title = "ОБНАРУЖЕН СБРОС ГРУЗА!",
            Text = "ГРУЗОВОЙ ВЕРТОЛЕТ СКОРО СБРОСИТ ЗАЩИЩЕННЫЙ КОНТЕЙНЕР!"
        };

        
        #endregion

        #region handler

        int GetOnline()
        {
            return BasePlayer.activePlayerList.Count;
        }

        int Sleepers()
        {
            return BasePlayer.sleepingPlayerList.Count;
        }
        
        int GetConnectingPlayers()
        {
            return ServerMgr.Instance.connectionQueue.joining.Count;
        }
        int Getqueue()
        {
            return ServerMgr.Instance.connectionQueue.queue.Count;
        }
        int GetAllPlayers()
        {
            return BasePlayer.activePlayerList.Count + BasePlayer.sleepingPlayerList.Count + ServerMgr.Instance.connectionQueue.joining.Count + ServerMgr.Instance.connectionQueue.queue.Count;
        }

        string TextConstuctior(string text,List<string> keys = null)
        {
            if (keys == null )
            {
                return text;
            }
            //PrintWarning("start replace");
            foreach (var key in keys)
            {
                //PrintWarning($"{Call(_replaceKeys[key])}");
                if (_replaceKeys != null) text =text.Replace(key, Call(_replaceKeys[key]).ToString());
            }

            return text;

        }

        #endregion

        #region config

        static Configuration config = null;

        class Configuration
        {
            [JsonProperty("Заполнение капельки")] 
            public Dictionary<double, string> ImageFill;
            [JsonProperty("Частота стандартных уведомлений")]
            public float defaultRate;
            [JsonProperty("Стандартные уведомления")] 
            public List<Notification> Notifications;
            [JsonProperty("фоны")]
            public Dictionary<string, string> Backgrounds = new Dictionary<string, string>
            {
                ["default"] = "https://i.imgur.com/7lOYsov.png",
                ["custom"] = "https://i.imgur.com/egfVi5e.png",
            };
            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    ImageFill = new Dictionary<double, string>
                    {
                        [0] = "https://xackuscr.ru/rust/xxxxacku/newred/0.png",
                        [0.1] = "https://xackuscr.ru/rust/xxxxacku/newred/2.png",
                        [0.2] = "https://xackuscr.ru/rust/xxxxacku/newred/4.png",
                        [0.3] = "https://xackuscr.ru/rust/xxxxacku/newred/6.png",
                        [0.4] = "https://xackuscr.ru/rust/xxxxacku/newred/8.png",
                        [0.5] = "https://xackuscr.ru/rust/xxxxacku/newred/10.png",
                        [0.6] = "https://xackuscr.ru/rust/xxxxacku/newred/12.png",
                        [0.7] = "https://xackuscr.ru/rust/xxxxacku/newred/14.png",
                        [0.8] = "https://xackuscr.ru/rust/xxxxacku/newred/16.png",
                        [0.9] = "https://xackuscr.ru/rust/xxxxacku/newred/18.png",
                        [1.0] = "https://xackuscr.ru/rust/xxxxacku/newred/20.png"
                    },
                    defaultRate = 300f,
                    Notifications = new List<Notification>
                    {
                        new Notification
                        {
                            Title = "ДОБРО ПОЖАЛОВАТЬ НА NUKA RUST",
                            Text = "ИГРОКОВ ОНЛАЙН: online",
                            Keys = new List<string>{"online"},
                            Color = "1 1 1 0.9",
                            Duration = 3f,
                        },
                        new Notification
                        {
                            Title = "ДОБРО ПОЖАЛОВАТЬ НА NUKA RUST",
                            Text = "ВСЕГО ИГРОКОВ: all",
                            Keys = new List<string>{"all"},
                            Color = "1 1 1 0.9",
                            Duration = 3f,
                        },
                        new Notification
                        {
                            Title = "ДОБРО ПОЖАЛОВАТЬ НА NUKA RUST",
                            Text = "ПОДКЛЮЧАЕТСЯ: connecting В очереди: queue",
                            Keys = new List<string>{"connecting","queue"},
                            Color = "1 1 1 0.9",
                            Duration = 3f,
                        },
                    }
                };
            }
        }
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) PrintWarning("NULL");
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintError($"Не удалось найти конфигурацию 'oxide/config/{Name}', Создание конфига!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }
        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region enum

        
        #endregion

        #region classes

        class Notification
        {
            [JsonProperty("Стандартное")]
            public bool IsDefault;
            [JsonProperty("Заголовок")]
            public string Title;
            [JsonProperty("Текст")]
            public string Text;
            [JsonProperty("Используемые ключи")] 
            public List<string> Keys = new List<string>();
            [JsonProperty("Цвет")]
            public string Color;
            [JsonProperty("Картинка(название)")]
            public string Image;
            [JsonProperty("Длительность")]
            public float Duration;
            [JsonProperty("Звук")]
            public string Sound;
        }

        #endregion

        #region hooks
        
        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity is BradleyAPC)
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    DrawSingleNotification(player,Bradly);
                }
                return;
            }
            
            if (entity is CH47Helicopter)
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    DrawSingleNotification(player,Ch47);
                }
                return;
            }
            
            if (entity is CargoShip)
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    DrawSingleNotification(player,Cargo);
                }
                return;
            }
            
            if (entity is BaseHelicopter)
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    DrawSingleNotification(player,Heli);
                }
                return;
            }
        }

        void OnServerInitialized()
        {
            foreach (var var in config.Backgrounds)
            {
                ImageLibrary.Call("AddImage", var.Value, var.Key);
            }
            
            foreach (var var in config.ImageFill)
            {
                PrintWarning($"Add image {var.Key} -{var.Value}");
                ImageLibrary.Call("AddImage", var.Value, var.Key.ToString());
            }
            
            
            
            foreach (var player in BasePlayer.activePlayerList)
            {
                OnPlayerConnected(player);
            }
        }

        void OnPlayerRespawned(BasePlayer player)
        {
            InitLayer(player);
            return;
        }
        
        
        void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            InitLayer(player);
            return;
        }
        void OnPlayerConnected(BasePlayer player)
        {
            
            InitLayer(player);
        }
        
        

        #endregion

        #region UI

        private string _Logo = "LogoUI";
        private string _Notice = "NotificationUI";
        
        
        List<ulong> InitPlayers = new List<ulong>();

        void InitLayer(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, _Logo);

            

            var container = new CuiElementContainer();
            
                container.Add(new CuiPanel
                {
                    Image = {Color = "0 0 0 0"},
                    RectTransform = {AnchorMin = "0 1",AnchorMax = "0 1",OffsetMin = "0 -64", OffsetMax = "154 -10"}
                }, "Hud", _Logo);
            
                container.Add(new CuiPanel
                {
                    Image = {Color = "0 0 0 0"},
                    RectTransform = {AnchorMin = "0 1",AnchorMax = "0 1",OffsetMin = "0 -114", OffsetMax = "154 -60"}
                }, "Overlay", _Notice);

            CuiHelper.AddUi(player, container);
            DrawLogo(player);
            
            if (!InitPlayers.Contains(player.userID))
            {
                Notification notification = config.Notifications.GetRandom();
                DrawNotification(player,notification);
                InitPlayers.Add(player.userID);
            }
            
        }

        void DrawLogo(BasePlayer player)
        {
            
            
                double players = (double)BasePlayer.activePlayerList.Count / ConVar.Server.maxplayers;
            double round = Math.Round(players, 1);
            var container = new CuiElementContainer();
           
            container.Add(new CuiPanel
            {
                Image = {Color = "0 0 0 0"},
                RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 0",OffsetMax = "54 50"}
            }, _Logo, "Logo");
            
            container.Add(new CuiElement
            {
                Parent = "Logo",
                Components =
                {
                    new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage",round.ToString())},
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",AnchorMax = "1 1"
                    }
                }
            });
            container.Add(new CuiButton
            {
                Button = {Color = "0 0 0 0",Command = "bopen"},
                Text = {Text = ""},
                RectTransform = {AnchorMin = "0 0",AnchorMax = "1 1"}
            }, "Logo");
            CuiHelper.AddUi(player, container);
            //timer.Once(60f, ()=>DrawLogo(player));
        }

        void DrawNotification(BasePlayer player,Notification notification)
        {
            
            CuiHelper.DestroyUi(player, "Notification");
            CuiHelper.DestroyUi(player, _Notice);
            var container = new CuiElementContainer();
            
            container.Add(new CuiPanel
            {
                Image = {Color = "0 0 0 0"},
                RectTransform = {AnchorMin = "0 1",AnchorMax = "0 1",OffsetMin = "0 -114", OffsetMax = "154 -60"}
            }, "Overlay", _Notice);

            container.Add(new CuiPanel
            {
                Image = {Color = "0 0 0 0"},
                RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 0",OffsetMax = "330 50"}
            }, _Notice, "Notification");
            container.Add(new CuiElement
            {
                Parent = "Notification",
                Components =
                {
                    new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage",notification.IsDefault ? "default" : "custom")},
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",AnchorMax = "1 1"
                    }
                }
            });
            container.Add(new CuiLabel
            {
                Text = {Text = notification.Title,Align = TextAnchor.UpperCenter,FontSize = 16,Font = "robotocondensed-bold.ttf"},
                RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = " 30 35",OffsetMax = "294 50"}
            }, "Notification","Notification"+".Title");

            string text = "";
            if (notification.Keys == null || notification.Keys == new List<string>())
            {
                text = notification.Text;
            }
            else
            {
                text = TextConstuctior(notification.Text,notification.Keys);
            }
            
            
            container.Add(new CuiLabel
            {
                Text = {Text = text,Align = TextAnchor.MiddleLeft,FontSize = 15,Font = "robotocondensed-regular.ttf"},
                RectTransform = {AnchorMin = "0.1 0",AnchorMax = "0.1 0",OffsetMin = " 0 25",OffsetMax = "294 35"}
            }, "Notification","Notification"+".Text");
            if (notification.Sound != null && notification.Sound != "")
            {
                Effect x = new Effect(notification.Sound, player, 0, new Vector3(), new Vector3());
                EffectNetwork.Send(x, player.Connection);
            }

            CuiHelper.AddUi(player, container);

            timer.Once(notification.Duration, () => CuiHelper.DestroyUi(player, "Notification"));
            timer.Once(config.defaultRate, ()=>InitLayer(player));
        }
        void DrawSingleNotification(BasePlayer player,Notification notification)
        {
            
            CuiHelper.DestroyUi(player, "Notification");
            CuiHelper.DestroyUi(player, _Notice);
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = {Color = "0 0 0 0"},
                RectTransform = {AnchorMin = "0 1",AnchorMax = "0 1",OffsetMin = "0 -114", OffsetMax = "154 -60"}
            }, "Overlay", _Notice);

            container.Add(new CuiPanel
            {
                Image = {Color = "0 0 0 0"},
                RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 0",OffsetMax = "330 50"}
            }, _Notice, "Notification");
            container.Add(new CuiElement
            {
                Parent = "Notification",
                Components =
                {
                    new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage",notification.IsDefault ? "default" : "custom")},
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",AnchorMax = "1 1"
                    }
                }
            });
            container.Add(new CuiLabel
            {
                Text = {Text = notification.Title,Align = TextAnchor.UpperCenter,FontSize = 16,Font = "robotocondensed-bold.ttf"},
                RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = " 30 35",OffsetMax = "294 50"}
            }, "Notification","Notification"+".Title");

            string text = "";
            if (notification.Keys == null || notification.Keys == new List<string>())
            {
                text = notification.Text;
            }
            else
            {
                text = TextConstuctior(notification.Text,notification.Keys);
            }
            
            
            container.Add(new CuiLabel
            {
                Text = {Text = text,Align = TextAnchor.MiddleLeft,FontSize = 15,Font = "robotocondensed-regular.ttf"},
                RectTransform = {AnchorMin = "0.1 0",AnchorMax = "0.1 0",OffsetMin = " 0 25",OffsetMax = "294 35"}
            }, "Notification","Notification"+".Text");
            if (notification.Sound != null && notification.Sound != "")
            {
                Effect x = new Effect(notification.Sound, player, 0, new Vector3(), new Vector3());
                EffectNetwork.Send(x, player.Connection);
            }

            CuiHelper.AddUi(player, container);

            timer.Once(notification.Duration, () => CuiHelper.DestroyUi(player, "Notification"));
            
        }

        void SendCustonNotification(BasePlayer player, string title, string text, string sound, float duration,List<string> keys = null)
        {
            Notification notification = new Notification
            {
                IsDefault = false,
                Title = title,
                Text = text,
                Sound = sound,
                Duration = duration,
                Keys = keys
            };
            DrawSingleNotification(player,notification);
        }

        

        #endregion

        [ConsoleCommand("bopen")]
        void OpenMenuFromBroadcast(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
            {
                 return;
            }

            
            
            arg.Player().SendConsoleCommand("chat.say /menu");
            InitLayer(arg.Player());
        }
    }
}