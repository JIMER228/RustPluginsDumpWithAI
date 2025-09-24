using System;
using UnityEngine;
using System.Globalization;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("Menu panel server", "create by Kill me, edit by Dima1809", "2.0.0")]
    class Menu : RustPlugin
    {
        #region Configuration

        private string ComButton1 = "chat.say /kit"; 
        private string TextButton1 = "Киты"; 

        private string ComButton2 = "chat.say /remove"; 
        private string TextButton2 = "Ремув"; 

        private string ComButton3 = "chat.say /report"; 
        private string TextButton3 = "Репорт"; 

        private string ComButton4 = "chat.say /vk"; 
        private string TextButton4 = "ВК"; 

        private string ComButton5 = "chat.say /up"; 
        private string TextButton5 = "UP"; 

        private string ComButton6 = "chat.say /skin"; 
        private string TextButton6 = "Скины";
		
		private string NamePanel = "JustRust";
		
		private void LoadDefaultConfig()
        {
            GetConfig("Настройки меню", "Название сервера", ref NamePanel);
            GetConfig("Настройки меню", "Название кнопки 1", ref TextButton1);
			GetConfig("Настройки меню", "Команда кнопки 1", ref ComButton1);
            GetConfig("Настройки меню", "Название кнопки 2", ref TextButton2);
			GetConfig("Настройки меню", "Команда кнопки 2", ref ComButton2);
            GetConfig("Настройки меню", "Название кнопки 3", ref TextButton3);
			GetConfig("Настройки меню", "Команда кнопки 3", ref ComButton3);
            GetConfig("Настройки меню", "Название кнопки 4", ref TextButton4);
			GetConfig("Настройки меню", "Команда кнопки 4", ref ComButton4);
			GetConfig("Настройки меню", "Название кнопки 5", ref TextButton5);
			GetConfig("Настройки меню", "Команда кнопки 5", ref ComButton5);
            GetConfig("Настройки меню", "Название кнопки 6", ref TextButton6);
			GetConfig("Настройки меню", "Команда кнопки 6", ref ComButton6);
            SaveConfig();
        }

        // ReSharper disable once UnusedMember.Local
        void OnServerInitialized()
        {
            LoadDefaultConfig();
        }


        #endregion

        #region Variables
        
        [PluginReference] private Plugin ImageLibrary;

        private string Layer = "UI_MenuHandler";

        #endregion

        #region GUI

        [ChatCommand("menu")]
        private void cmdMenu(BasePlayer player, string command, string[] args)
        {
            var container = new CuiElementContainer();
            var Panel = container.Add(new CuiPanel
            {
                Image = {Color = HexToCuiColor("#FFFFFF00")},
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                CursorEnabled = true,
            }, "Overlay", Layer);
			
			container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent { Color = "0 0 0 0.5", FadeIn = 1f, Sprite = "assets/content/ui/ui.background.tiletex.psd", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    new CuiRectTransformComponent{ AnchorMin = "-0.003906235 0", AnchorMax = "0.9992187 1.005556" }
                }
            });

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent {FadeIn = 0.25f, Color = HexToCuiColor("#7A7A7AFF")},
                    new CuiRectTransformComponent {AnchorMin = "0.275 0.6986135", AnchorMax = "0.7070313 0.7500031"}
                }
            });
			
			container.Add(new CuiElement
            {
                Parent = Layer,
                Components = {
                    new CuiTextComponent() { Color = HexToCuiColor("#ff8b00"), FadeIn = 1f, Text = NamePanel, FontSize = 25, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf"  },
                    new CuiRectTransformComponent { AnchorMin = "0.2773436 0.6583332", AnchorMax = "0.715625 0.7972222" },
                }
            });

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent {FadeIn = 0.25f, Color = HexToCuiColor("#4D4D4DD4")},
                    new CuiRectTransformComponent {AnchorMin = "0.3898438 0.6041667", AnchorMax = "0.7070313 0.6944445"}
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent {FadeIn = 0.25f, Color = HexToCuiColor("#4D4D4DD4")},
                    new CuiRectTransformComponent {AnchorMin = "0.275 0.5041667", AnchorMax = "0.3875 0.6944444"}
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent {FadeIn = 0.25f, Color = HexToCuiColor("#4D4D4DD4")},
                    new CuiRectTransformComponent {AnchorMin = "0.2757812 0.4236111", AnchorMax = "0.3875 0.4986112"}
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent {FadeIn = 0.25f, Color = HexToCuiColor("#4D4D4DD4")},
                    new CuiRectTransformComponent {AnchorMin = "0.3898438 0.4236111", AnchorMax = "0.7070313 0.5944431"}
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".AvatarHandler",
                Components =
                {
                    new CuiImageComponent { Color = HexToCuiColor("#FFFFFF00") },
                    new CuiRectTransformComponent {AnchorMin = "0.2757812 0.5041667", AnchorMax = "0.3882812 0.6944444", OffsetMax = "0 0"}
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = Layer + ".AvatarHandler",
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", player.UserIDString) },
                    new CuiRectTransformComponent { AnchorMin = "0.01657543 0.01657447", AnchorMax = "0.9834245 0.9834255", OffsetMax = "0 0" }
                }
            });
			
		    container.Add(new CuiElement 
            { 
                Parent = Layer, 
                Name = Layer + ".NameHandler", 
                Components = 
                { 
                    new CuiImageComponent { Color = HexToCuiColor("#4D4D4DD4") }, 
                    new CuiRectTransformComponent {AnchorMin = "0.3898438 0.6041667", AnchorMax = "0.7070313 0.6944445", OffsetMax = "0 0"} 
                } 
            });
			
			container.Add(new CuiElement 
            { 
               Parent = Layer + ".NameHandler", 
               Components = 
			   {
                  new CuiTextComponent { Text = "<color=#FFFFFFB3><b>" + player.displayName.ToUpper() + "</b></color>", FontSize = 26, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }, 
                  new CuiRectTransformComponent { AnchorMin = "0 0.33333", AnchorMax = "1 1.012", OffsetMax = "0 0"} 
                } 
            }); 
			
			container.Add(new CuiElement 
            { 
               Parent = Layer + ".NameHandler", 
               Components = 
                { 
                 new CuiTextComponent { Text = $"Онлайн: <color=#FFFFFFB3>{BasePlayer.activePlayerList.Count} / {ConVar.Server.maxplayers}</color>", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleRight }, 
                 new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0.98 0.45", OffsetMax = "0 0"} 
                } 
            });

          container.Add(new CuiElement 
            { 
             Parent = Layer + ".NameHandler", 
             Components = 
              { 
                new CuiTextComponent { Text = $"<color=#FFFFFFB3>{TOD_Sky.Instance.Cycle.DateTime.ToString("HH:mm")}</color>", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft }, 
                new CuiRectTransformComponent { AnchorMin = "0.01 0", AnchorMax = "0.98 0.45", OffsetMax = "0 0"} 
              } 
            });
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Close = Layer, Color = HexToCuiColor("#FFFFFF00"), FadeIn = 0.1f},
                Text = { Text = "" }
            }, Layer);
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.3914062 0.4263889", AnchorMax = "0.4679688 0.5652777" },
                Button = { Command = ComButton1, Close = Layer, Color = HexToCuiColor("#878585D4")},
                Text = { Text = TextButton1, FontSize = 20, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" },
            }, Layer);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.4703113 0.4263889", AnchorMax = "0.5468739 0.5652777" },
                Button = { Command = ComButton2, Close = Layer, Color = HexToCuiColor("#878585D4")},
                Text = { Text = TextButton2, FontSize = 20, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" },
            }, Layer);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5492163 0.4263889", AnchorMax = "0.6257789 0.5652777" },
                Button = { Command = ComButton3, Close = Layer, Color = HexToCuiColor("#878585D4")},
                Text = { Text = TextButton3, FontSize = 20, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" },
            }, Layer);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.6281214 0.5236111", AnchorMax = "0.7062463 0.5652777" },
                Button = { Command = ComButton4, Close = Layer, Color = HexToCuiColor("#878585D4")},
                Text = { Text = TextButton4, FontSize = 20, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" },
            }, Layer);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.6281214 0.4749999", AnchorMax = "0.7062463 0.5166659" },
                Button = { Command = ComButton5, Close = Layer, Color = HexToCuiColor("#878585D4")},
                Text = { Text = TextButton5, FontSize = 20, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" },
            }, Layer);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.6281214 0.4263892", AnchorMax = "0.7062463 0.4680549" },
                Button = { Command = ComButton6, Close = Layer, Color = HexToCuiColor("#878585D4")},
                Text = { Text = TextButton6, FontSize = 20, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" },
            }, Layer);
		
            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Avatar


        #endregion

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