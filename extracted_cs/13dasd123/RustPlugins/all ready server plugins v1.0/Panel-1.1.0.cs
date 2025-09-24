using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;


namespace Oxide.Plugins
{
    [Info("Panel", "Ryamkk", "1.1.0")]
      //  Слив плагинов server-rust by Apolo YouGame
    class Panel : RustPlugin
    {
		private PluginConfig config;
        private Dictionary<ulong, bool> Paneldata = new Dictionary<ulong, bool>();
		private static string Layer = "PanelGUI";
		
		#region Plugin Reference
		[PluginReference] Plugin ImageLibrary;
		#endregion
		
		#region Initialization
        private void Init() => config = Config.ReadObject<PluginConfig>();
        protected override void LoadDefaultConfig() => Config.WriteObject(PanelConfig(), true);
        
        void OnServerInitialized()
        {
			if (!plugins.Exists("ImageLibrary"))
            {
                PrintError("Плагин ImageLibrary не загружен, плагин выгружен!");
                Unload();
            }
			
			ImageLibrary.Call("AddImage", config.LogoURL,  "LogoURL");
			
            foreach (var player in BasePlayer.activePlayerList) 
			    OnPlayerInit(player);
        }
		
		void OnPlayerInit(BasePlayer player)
        {
            if (player.IsReceivingSnapshot())
            {
                NextTick(() =>
                {
                    OnPlayerInit(player);
                    return;
                });
            }
            
            DrawGUI(player);
        }
		
		void Load()
		{
			foreach (BasePlayer player in BasePlayer.activePlayerList) CuiHelper.DestroyUi(player, Layer);
		}
		
		void Unload()
        {
			foreach (BasePlayer player in BasePlayer.activePlayerList) CuiHelper.DestroyUi(player, Layer);
        }
		
        #endregion
		
        #region Configuration
        private class PluginConfig
        {	
		    [JsonProperty("Ссылка на логотип")]
		    public string LogoURL;
			
			[JsonProperty("Названия сервера")]
		    public string ServerName;
			
			[JsonProperty("Цвет названия сервера")]
		    public string ServerNameColor;
			
			[JsonProperty("Названия шрифта")]
		    public string NameFont;
			
			[JsonProperty("Размер названия сервера (Дефолтный размер: 16)")]
		    public int ServerNameSize;
			
			[JsonProperty("Размер кнопки (Дефолтный размер: 12)")]
		    public int ButtonSize;
			
            [JsonProperty("Вкладки меню")]
            public List<AddtionalPanel> panellist = new List<AddtionalPanel>();
        }

        private class AddtionalPanel
        {
            [JsonProperty("Минимальный отступ кнопки")]
            public string PanelButtonAnchorMin;
			
            [JsonProperty("Максимальный отступ кнопки")]
            public string PanelButtonAnchorMax;
			
			[JsonProperty("Минимальный отступ картинки")]
            public string PanelImageAnchorMin;
			
            [JsonProperty("Максимальный отступ картинки")]
            public string PanelImageAnchorMax;
			
            [JsonProperty("Картинка")]
            public string PanelImageUrl;
			
            [JsonProperty("Команда")]
            public string PanelButtonCmd;
			
			[JsonProperty("Названия кнопки")]
            public string PanelButtonName;
			
			[JsonProperty("Цвет кнопки и картинки")]
            public string PanelColor;
			
			[JsonProperty("Размер кнопки (Дефолтный размер: 12)")]
		    public int ButtonSize;
			
			[JsonProperty("Названия шрифта")]
		    public string NameFont;
        }
		
		private PluginConfig PanelConfig()
        {
            return new PluginConfig
            {
				LogoURL = "https://i.imgur.com/dFziN7i.png",
				ServerName = "MAGIC RUST • MAX3 • X5",
				ServerNameColor = "#FFFFFF5A",
				NameFont = "robotocondensed-bold.ttf",
				ButtonSize = 12,
				ServerNameSize = 16,
				
				
				
                panellist = new List<AddtionalPanel>
                {
                   new AddtionalPanel
                   {
                        PanelImageAnchorMin = "-0.1374998 -1.080003",
                        PanelImageAnchorMax = "-0.01249984 -0.0800035",
						PanelImageUrl = "https://i.imgur.com/9FJ1Cqu.png",
						
						NameFont = "robotocondensed-bold.ttf",
						
                        PanelButtonAnchorMin = "0.005895811 -1.06",
						PanelButtonAnchorMax = "0.7558958 -0.0600118",
						PanelButtonName = "НАСТРОЙКА ИГРОВОГО ЧАТА",
                        PanelButtonCmd = "chat.say /chat",
						ButtonSize = 12,
						PanelColor = "#FFFFFF5A",
                   },
                   new AddtionalPanel
                   {
                        PanelImageAnchorMin = "-0.1374999 -2.099999",
                        PanelImageAnchorMax = "-0.01249996 -1.099998",
						PanelImageUrl = "https://i.imgur.com/OPmWEo7.png",
						 
						NameFont = "robotocondensed-bold.ttf",
						 
                        PanelButtonAnchorMin = "0.007499999 -2.099999",
						PanelButtonAnchorMax = "0.7574999 -1.099998",
						PanelButtonName = "БЛОКИРОВКА ПРЕДМЕТОВ",
						PanelButtonCmd = "chat.say /block",
						ButtonSize = 12,
						PanelColor = "#FFFFFF5A",
                   },
				   new AddtionalPanel
                   {
                        PanelImageAnchorMin = "-0.1374999 -3.099771",
                        PanelImageAnchorMax = "-0.01249985 -2.099771",
						PanelImageUrl = "https://i.imgur.com/uHGxrOc.png",
						 
						NameFont = "robotocondensed-bold.ttf",
						 
                        PanelButtonAnchorMin = "0.004999999 -3.099998",
						PanelButtonAnchorMax = "0.7549999 -2.099998",
						PanelButtonName = "НАСТРОЙКИ ПОПАДАНИЙ",
						PanelButtonCmd = "chat.say /marker",
						ButtonSize = 12,
						PanelColor = "#FFFFFF5A",
                   },
				   new AddtionalPanel
                   {
                        PanelImageAnchorMin = "-0.1374999 -4.079997",
                        PanelImageAnchorMax = "-0.01249996 -3.079997",
						PanelImageUrl = "https://i.imgur.com/kNEaL90.png",
						 
						NameFont = "robotocondensed-bold.ttf",
						 
                        PanelButtonAnchorMin = "0 -4.079997",
						PanelButtonAnchorMax = "0.7499999 -3.079997",
						PanelButtonName = "РАСПИСАНИЯ ВАЙПОВ",
						PanelButtonCmd = "chat.say /wipe",
						ButtonSize = 12,
						PanelColor = "#FFFFFF5A",
                   },
				   new AddtionalPanel
                   {
                        PanelImageAnchorMin = "-0.1374999 -5.059996",
                        PanelImageAnchorMax = "-0.01249996 -4.059996",
						PanelImageUrl = "https://i.imgur.com/Vz4GEfE.png",
						 
						NameFont = "robotocondensed-bold.ttf",
						 
                        PanelButtonAnchorMin = "0 -5.099996",
						PanelButtonAnchorMax = "0.7499999 -4.099996",
						PanelButtonName = "ДОСТУПНЫЕ НАБОРЫ",
						PanelButtonCmd = "chat.say /kit",
						ButtonSize = 12,
						PanelColor = "#FFFFFF5A",
                   },
				   new AddtionalPanel
                   {
                        PanelImageAnchorMin = "-0.1374999 -6.099995",
                        PanelImageAnchorMax = "-0.01249996 -5.099996",
						PanelImageUrl = "https://i.imgur.com/NYZPHuE.png",
						 
						NameFont = "robotocondensed-bold.ttf",
						 
                        PanelButtonAnchorMin = "0 -6.099995",
						PanelButtonAnchorMax = "0.7499999 -5.099996",
						PanelButtonName = "СТАТИСТИКА",
						PanelButtonCmd = "chat.say /stat",
						ButtonSize = 12,
						PanelColor = "#FFFFFF5A",
                   },
				   new AddtionalPanel
                   {
                        PanelImageAnchorMin = "-0.1374999 -7.099994",
                        PanelImageAnchorMax = "-0.01249996 -6.099995",
						PanelImageUrl = "https://i.imgur.com/hXtyw8a.png",
						 
						NameFont = "robotocondensed-bold.ttf",
						 
                        PanelButtonAnchorMin = "0 -7.099994",
						PanelButtonAnchorMax = "0.7499999 -6.099995",
						PanelButtonName = "ПРАВИЛА",
						PanelButtonCmd = "chat.say /rules",
						ButtonSize = 12,
						PanelColor = "#FFFFFF5A",
                   },
				   new AddtionalPanel
                   {
                        PanelImageAnchorMin = "-0.1374999 -8.099996",
                        PanelImageAnchorMax = "-0.01249996 -7.099997",
						PanelImageUrl = "https://i.imgur.com/A1bpS78.png",
						 
						NameFont = "robotocondensed-bold.ttf",
						 
                        PanelButtonAnchorMin = "0 -8.099996",
						PanelButtonAnchorMax = "0.7499999 -7.099997",
						PanelButtonName = "КОРЗИНА",
						PanelButtonCmd = "chat.say /store",
						ButtonSize = 12,
						PanelColor = "#FFFFFF5A",
                   }
                }
            };
        }
        #endregion

        #region Commands
		[ConsoleCommand("ui.panel")]
        private void cmdMPanel(ConsoleSystem.Arg arg)
        {
			var player = arg.Player();
            if (!Paneldata.ContainsKey(player.userID)) Paneldata.Add(player.userID, false);
            else
            {
                if (Paneldata[player.userID])
                {
                    Paneldata[player.userID] = false;
                    DrawGUI(player);
                }
                else
                {
                    Paneldata[player.userID] = true;
                    DrawGUI(player);
                }
            }
            return;
        }
		#endregion
		
		#region GUI Interface
        private void DrawGUI(BasePlayer player)
        {
            if (player == null) return;

            if (!Paneldata.ContainsKey(player.userID)) Paneldata.Add(player.userID, false);

			CuiElementContainer container = new CuiElementContainer();
			
			#region Создаём элемент родитель интерфейса панели.
            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.03121747 0.9491682", AnchorMax = "0.239334 0.9953789" },
                CursorEnabled = false,
            }, "Overlay", Layer);
			#endregion
			
			#region Создаём логотоип и названия сервера.
			container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", $"LogoURL") },
                    new CuiRectTransformComponent { AnchorMin = "-0.1384371 -0.01343705", AnchorMax = "-0.01593705 0.9975058" }
                }
            });
			
			container.Add(new CuiElement
            {
                Parent = Layer,
                Components = {
                    new CuiTextComponent() { Text = config.ServerName, FontSize = config.ServerNameSize, Align = TextAnchor.UpperLeft, Font = config.NameFont, Color = HexToCuiColor(config.ServerNameColor) },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                }
            });
			#endregion
			
			#region Запоминаем информацию #1
            if(config.panellist.Count() < 1)
            {
                CuiHelper.DestroyUi(player, Layer);
                CuiHelper.AddUi(player, container);
                return;
            }
			#endregion
			
			#region Создаём переключатель.
			string textswitsh = Paneldata[player.userID] ? "НАЖМИТЕ ЧТОБЫ ОТКРЫТЬ МЕНЮ":"НАЖМИТЕ ЧТОБЫ ЗАКРЫТЬ МЕНЮ";
			container.Add(new CuiElement
			{
				Parent = Layer,
		        Name = Layer,
				Components = {
					new CuiTextComponent() { Text = textswitsh, FontSize = config.ButtonSize, Align = TextAnchor.LowerLeft, Color = HexToCuiColor(config.ServerNameColor), Font = config.NameFont },
                    new CuiRectTransformComponent { AnchorMin = "0 0.1600001", AnchorMax = "1 1.16", OffsetMin = "0 0" },
				}
			});
			
			container.Add(new CuiButton
            {
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Color = "0 0 0 0", Command = "ui.panel" },
                Text = { Text = "" }
            }, Layer);
			#endregion
			
			#region Запоминаем информацию #2
            if (Paneldata[player.userID])
            {
                CuiHelper.DestroyUi(player, Layer);
                CuiHelper.AddUi(player, container);
                return;
            }
			#endregion

			#region Создаём кнопки для меню.
            int i = 0;
            foreach(var panel in config.panellist)
            {
				CuiHelper.DestroyUi(player, Layer);
				container.Add(new CuiElement
				{
					Parent = Layer,
                    Name = Layer,
					Components = {
						new CuiRawImageComponent { FadeIn = 3.0f, Url = panel.PanelImageUrl, Color = HexToCuiColor(panel.PanelColor) },
                        new CuiRectTransformComponent { AnchorMin = panel.PanelImageAnchorMin, AnchorMax = panel.PanelImageAnchorMax }
					}
				});
				
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = panel.PanelButtonAnchorMin, AnchorMax = panel.PanelButtonAnchorMax },
                    Button = { FadeIn = 3.0f, Color = "0 0 0 0", Command = panel.PanelButtonCmd },
                    Text = { Text = panel.PanelButtonName, FontSize = panel.ButtonSize, Align = TextAnchor.MiddleLeft, Color = HexToCuiColor(panel.PanelColor), Font = panel.NameFont }
                }, Layer);
                i++;
            }
			#endregion

            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
        }
        #endregion
		
		#region Utilits and Others
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
		#endregion
    }
}
