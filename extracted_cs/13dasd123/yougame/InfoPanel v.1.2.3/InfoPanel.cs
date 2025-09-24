// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using UnityEngine;
using Oxide.Core.Plugins;
using UnityEngine.UI;

namespace Oxide.Plugins
{
    [Info("InfoPanel", "A0001", "1.2.3")]
    class InfoPanel : RustPlugin
    {
        private int RefreshRate = 5;
        private int FontSize = 16;
        private string FontName = "robotocondensed-regular.ttf";
        private string ImageURL = "i.imgur.com/z0A6CMC.png";
        private string ShadowSize = "0.2 0.2";
        private string PanelAnchorMin = "0 0";
        private string PanelAnchorMax = "1 0.025";


        #region Настройка вертолета
        
        private string ImageHeliON = "https://i.imgur.com/0E33yAX.png";
        private string ImageHeliOFF = "http://i.imgur.com/hTTyTTx.png";
        private string AnchorMinHeli = "0.895 -0.2";
        private string AnchorMaxHeli = "0.925 1.05";
        private bool heli = true;
        
        #endregion
        
        #region Настройка Самолета
        
        private string ImageAirON = "https://i.imgur.com/0Dxdgx7.png";
        private string ImageAirOFF = "https://i.imgur.com/p4cefAP.png";
        private string AnchorMinAir = "0.93 -0.05";
        private string AnchorMaxAir = "0.95 0.85";
        private bool Air = true;
        
        #endregion
        
        #region Настройка Чинука
        
        private string ImageCH47ON = "https://i.imgur.com/ZQ3TEUd.png";
        private string ImageCH47OFF = "https://i.imgur.com/Qf9yQ5q.png";
        private string AnchorMinCH47 = "0.85 -0.5";
        private string AnchorMaxCH47 = "0.89 1.5";
        private bool CH47 = true;
        
        #endregion

        #region Настройка сообщение

        private int MessageTIME = 10;
        private string AnchorMinMessage = "0.0544375 0.05";
        private string AnchorMaxMessage = "0.9084583 1";
        
        #endregion
        
        
        List<string> messageList = new List<string>
        {
            "...",
            "Создатель плагина: <color=#82B57A>A1M41K</color>",
            "Плагин скачен на DarkPlugins.ru",
            "..."
        };
		
            List<BasePlayer> InfoPlus = new List<BasePlayer>{};
        
        void OnPlayerInit(BasePlayer player)
        {
            InfoPlus.Add(player);
        }
        void OnPlayerDisconnected(BasePlayer player)
        {
            InfoPlus.Remove(player);
        }

        void LoadData()
        {
            
        }
		
		[ChatCommand("infopanel")]
		private void Command(BasePlayer player, string command, string[] args)
		{
			if (args.Length != 1)
            {
                PrintToChat(player, Messages["Error"]);
                return;
            }
			switch (args[0])
            {
                case "off":
					if (!InfoPlus.Contains(player))
					{
						PrintToChat(player, Messages["Panel_Check_off"]);
						break;
					}
					InfoPlus.Remove(player);
                    CuiHelper.DestroyUi(player, "InfoPanel");
                    CuiHelper.DestroyUi(player, "Clock");
                    CuiHelper.DestroyUi(player, "Message");
					PrintToChat(player, Messages["Panel_off"]);
                    break;
				case "on":
					if (InfoPlus.Contains(player))
					{
						PrintToChat(player, Messages["Panel_Check_on"]);
						break;
					}
					InfoPlus.Add(player);
				    CuiHelper.DestroyUi(player, "InfoPanel");
				    CuiHelper.DestroyUi(player, "Clock");
				    CuiHelper.DestroyUi(player, "Message");
					PrintToChat(player, Messages["Panel_on"]);
                    break;
				default:
                    PrintToChat(player, Messages["Error"]);
                    break;
			}
		}

		void Unload(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, "InfoPanel");
		}

        [PluginReference] 
        private Plugin ImageLibrary;
        
        void OnServerInitialized()
        {
            foreach (BasePlayer current in BasePlayer.activePlayerList)
            {
                InfoPlus.Add(current);
            }

            if (!plugins.Exists("ImageLibrary"))
            {
                PrintError("Плагин ImageLibrary не загружен");
            }
             
            ImageLibrary.Call("AddImage", $"{ImageHeliOFF}", "NoHeli"); // верт не вызван
            ImageLibrary.Call("AddImage", $"{ImageHeliON}", "HeliCalled"); // верт вызван
            ImageLibrary.Call("AddImage", $"{ImageAirOFF}", "NoAir");// аир не вызван
            ImageLibrary.Call("AddImage", $"{ImageAirON}", "AirCalled");// аир вызван
            ImageLibrary.Call("AddImage", $"{ImageCH47OFF}", "NoCH47");// аир не вызван
            ImageLibrary.Call("AddImage", $"{ImageCH47ON}", "CH47Called");// аир вызван            

            lang.RegisterMessages(Messages, this, "en");
            Messages = lang.GetMessages("en", this);
            
            LoadConfig();
            if (!Interface.Oxide.DataFileSystem.ExistsDatafile("InfoPanel/Message"))
                Interface.Oxide.DataFileSystem.WriteObject("InfoPanel/Message", messageList);

            timer.Once(MessageTIME, () => messageList = Interface.Oxide.DataFileSystem.ReadObject<List<string>>("InfoPanel/Message"));
            timer.Every(RefreshRate, () =>
            {
				foreach (BasePlayer player in BasePlayer.activePlayerList)
				{
					if (!InfoPlus.Contains(player))
					return;
				}
                foreach (var check in BasePlayer.activePlayerList)
                    DrawGUI(check);
            });
            timer.Every(1, () =>
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    if (!InfoPlus.Contains(player))
                        return;
                }
                foreach (var ClockCheck in BasePlayer.activePlayerList)
                    DrawGUICLOCK(ClockCheck);
            });
            timer.Every(MessageTIME, () =>
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    if (!InfoPlus.Contains(player))
                        return;
                }
                foreach (var MessageCHECK in BasePlayer.activePlayerList)
                    DrawGUIMessage(MessageCHECK);
            });
            
            
        }
        bool IsHelI()
        {
            foreach(var check in BaseNetworkable.serverEntities)
                if (check is BaseHelicopter)
                    return true;
            return false;
        }  
        
        bool IsAir()
        {
            foreach (var check in BaseNetworkable.serverEntities)
                if (check is CargoPlane)
                    return true;
            return false;
        }
        
        bool IsCH47()
        {
            foreach(var check in BaseNetworkable.serverEntities)
                if (check is CH47Helicopter)
                    return true;
            return false;
        }



        void DrawGUICLOCK(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "Clock");
            string ClockFormat = "HH:mm";
            var clocktop = new CuiElementContainer();
            var ClockRead = clocktop.Add(new CuiPanel
            {
                Image = { Color = $"0 0 0 0" },
                RectTransform = { AnchorMin = PanelAnchorMin, AnchorMax = PanelAnchorMax },
                CursorEnabled = false,
            }, "Hud", "Clock");

            #region Часы
            clocktop.Add(new CuiElement
            {
                Parent = "Clock",
                Components = {
                    new CuiTextComponent() { Text = "ВРЕМЯ: " + "<color=#82B57A>" + TOD_Sky.Instance.Cycle.DateTime.ToString(ClockFormat) + "</color>", Align = TextAnchor.MiddleLeft, Font = FontName, FontSize = FontSize, Color = "1 1 1 1"},
                    new CuiRectTransformComponent { AnchorMin = "0.04454582 0.02", AnchorMax = "0.13796749 1" },
                    new CuiOutlineComponent() { Color = "0 0 0 1", Distance = ShadowSize }
                }
            });
            #endregion

            CuiHelper.AddUi(player, clocktop);
        }
        
        void DrawGUIMessage(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "Message");
            var MessageTOP = new CuiElementContainer();
            var Message = MessageTOP.Add(new CuiPanel
            {
                Image = { Color = $"0 0 0 0" },
                RectTransform = { AnchorMin = PanelAnchorMin, AnchorMax = PanelAnchorMax },
                CursorEnabled = false,
            }, "Hud", "Message");
            #region Сообщения

            string message = messageList.GetRandom();
            MessageTOP.Add(new CuiElement
            {
                Parent = "Message",
                Components = {
                    new CuiTextComponent() {Text = message, Align = TextAnchor.MiddleCenter, Font = FontName, FontSize = FontSize },
                    new CuiRectTransformComponent { AnchorMin = AnchorMinMessage, AnchorMax = AnchorMaxMessage },
                    new CuiOutlineComponent() { Color = "0 0 0 0.5", Distance = ShadowSize }
                }
            });
            #endregion

            CuiHelper.AddUi(player, MessageTOP);
        }
        
        void DrawGUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "InfoPanel");
            var TOP = new CuiElementContainer();
            var Rating = TOP.Add(new CuiPanel
            {
                Image = { Color = $"0 0 0 0" },
                RectTransform = { AnchorMin = PanelAnchorMin, AnchorMax = PanelAnchorMax },
                CursorEnabled = false,
            }, "Hud", "InfoPanel");
            
            #region Online
            TOP.Add(new CuiElement
            {
                Parent = "InfoPanel",
                Components = {
                    new CuiTextComponent() { Text = "ОНЛАЙН: " + "<color=#82B57A>" + BasePlayer.activePlayerList.Count + "/" + "</color>" + "<color=#82B57A>" + ConVar.Server.maxplayers + "</color>", FontSize = FontSize, Align = TextAnchor.MiddleLeft, Font = FontName },
                    new CuiRectTransformComponent { AnchorMin = "0.1248124 0", AnchorMax = "0.2476 1" },
                    new CuiOutlineComponent() { Color = "0 0 0 1", Distance = ShadowSize }
                }
            });
            TOP.Add(new CuiElement
            {
                Parent = "InfoPanel",
                Components = {
                    new CuiTextComponent() { Text = "СПЯЩИХ: " + "<color=#82B57A>" + BasePlayer.sleepingPlayerList.Count + "</color>", FontSize = FontSize, Align = TextAnchor.MiddleLeft, Font = FontName },
                    new CuiRectTransformComponent { AnchorMin = "0.2148124 0", AnchorMax = "0.3366 1" },
                    new CuiOutlineComponent() { Color = "0 0 0 1", Distance = ShadowSize }
                }
            });
            #endregion

            #region PanelAIR

            if (heli)
            {
                TOP.Add(new CuiElement // Helicopter Вертолет
                {
                    Parent = "InfoPanel",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = IsHelI()
                                ? ImageLibrary.Call<string>("GetImage", "HeliCalled")
                                : ImageLibrary.Call<string>("GetImage", "NoHeli")
                        },
                        new CuiRectTransformComponent {AnchorMin = AnchorMinHeli, AnchorMax = AnchorMaxHeli},
                    }
                });
            }

            if (Air)
            {
                TOP.Add(new CuiElement // AirDrop Самолет
                {
                    Parent = "InfoPanel",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = IsAir()
                                ? ImageLibrary.Call<string>("GetImage", "AirCalled")
                                : ImageLibrary.Call<string>("GetImage", "NoAir")
                        },
                        new CuiRectTransformComponent {AnchorMin = AnchorMinAir, AnchorMax = AnchorMaxAir},
                    }
                });
            }

            if (CH47)
            {
                TOP.Add(new CuiElement // CH47 Вертолет
                {
                    Parent = "InfoPanel",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = IsCH47()
                                ? ImageLibrary.Call<string>("GetImage", "CH47Called")
                                : ImageLibrary.Call<string>("GetImage", "NoCH47")
                        },
                        new CuiRectTransformComponent {AnchorMin = AnchorMinCH47, AnchorMax = AnchorMaxCH47},
                    }
                });
            }
            #endregion

            CuiHelper.AddUi(player, TOP);
        }
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Создание нового файла конфигурации... | Благодарим за установку данного плагина");
            LoadConfig();
        }
        
        private new void LoadConfig()
        {
            GetConfig("Настройка основной панели","Положение панели min", ref PanelAnchorMin);
            GetConfig("Настройка основной панели","Положение панели max", ref PanelAnchorMax);
            GetConfig("Настройка основной панели","Размер обводки", ref ShadowSize);
            GetConfig("Настройка основной панели","Частота обновления", ref RefreshRate);
            GetConfig("Настройка основной панели","Шрифт текста", ref FontName);
            GetConfig("Настройка основной панели","Размер текста", ref FontSize);
            GetConfig("Настройка вертолета", "Положение панели min", ref AnchorMinHeli);
            GetConfig("Настройка вертолета", "Положение панели max", ref AnchorMaxHeli);
            GetConfig("Настройка вертолета", "Картинка вертолета ВКЛ", ref ImageHeliON);
            GetConfig("Настройка вертолета", "Картинка вертолета ВЫКЛ", ref ImageHeliOFF);
            GetConfig("Настройка вертолета", "ВКЛЮЧИТЬ ВЕРТОЛЕТ?", ref heli);
            GetConfig("Настройка Самолета", "ВКЛЮЧИТЬ Самолет?", ref Air);
            GetConfig("Настройка Самолета", "Картинка самолета вкл", ref ImageAirON);
            GetConfig("Настройка Самолета", "Картинка самолета выкл", ref ImageAirOFF);
            GetConfig("Настройка Самолета", "Положение панели самолета MIN", ref AnchorMinAir);
            GetConfig("Настройка Самолета", "Положение панели самолета MAX", ref AnchorMaxAir);
            GetConfig("Настройка CH47", "ВКЛЮЧИТЬ CH47?", ref CH47);
            GetConfig("Настройка CH47", "Картинка CH47 ВКЛ", ref ImageCH47ON);
            GetConfig("Настройка CH47", "Картинка CH47 ВЫКЛ", ref ImageCH47OFF);
            GetConfig("Настройка CH47", "Положение панели CH47 MIN", ref AnchorMinCH47);
            GetConfig("Настройка CH47", "Положение панели CH47 MAX", ref AnchorMaxCH47);
            
            GetConfig("Настройка Сообщений", "Частота обновление Сообщений", ref MessageTIME);
            GetConfig("Настройка Сообщений", "Положение сообщений min", ref AnchorMinMessage);
            GetConfig("Настройка Сообщений", "Положение сообщений max", ref AnchorMaxMessage);
        }
        private void GetConfig<T>(string menu, string Key, ref T var)
        {
            if (Config[menu, Key] != null)
            {
                var = (T)Convert.ChangeType(Config[menu, Key], typeof(T));
            }

            Config[menu, Key] = var;
        }
        #region LOCALIZATION
		
        private string GetLangValue(string key, string userId) => lang.GetMessage(key, this, userId);
		
        Dictionary<string, string> Messages = new Dictionary<string, string>()
        {
            { "Error", "<size=20>Вы ввели не правильно команду:</size>\nИспользуйте: <color=#0081de>/infopanel on|off</color>"},
            { "Panel_on", "<size=20>Вы успешно включили панель!</size>\nОна появиться через несколько секунд" },
            { "Panel_off", "<size=20>Вы успешно скрыли панель</size>"},
            { "Panel_Check_on", "<size=20>У вас уже включена панель.</size>\nИспользуйте <color=#0081de>' /infopanel off '</color> чтоб скрыть ее"},
            { "Panel_Check_off", "<size=20>У вас уже скрыта панель.</size>\nИспользуйте <color=#0081de>' /infopanel on '</color> чтоб включить ее"},
        };

        #endregion
    }
}