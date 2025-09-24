using System.Collections.Generic;
using ConVar;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using UnityEngine.UI;

namespace Oxide.Plugins
{
    [Info("ZealReconnect", "Kira", "1.0.0")]
    public class ZealReconnect : RustPlugin
    {
        [PluginReference] private Plugin ImageLibrary;

        private const string UIMain = "ZealReconnect.UI.Main";

        #region [Lang]
 
        protected override void LoadDefaultMessages()
        {
            var ru = new Dictionary<string, string> 
            {
                ["IP_MESSAGE"] = "<b>Вы подключились через резервный IP</b>\n".ToUpper() +
                                 "<size=10>Для стабильного подключения переподключитесь по коннекту\n\n" +
                                 "Нажми для получения более подробной информации</size>"
            };

            var en = new Dictionary<string, string>
            {
                ["IP_MESSAGE"] = "<b>You connected through the reserve IP</b>\n".ToUpper() +
                                 "<size=10>For a stable connection, reconnect via this address\n\n" +
                                 "Click for more information</size>"
            };
            lang.RegisterMessages(ru, this, "ru");
            lang.RegisterMessages(en, this);
        }

        #endregion

        private void DrawUI_Main(BasePlayer player)
        {
            var ui = new CuiElementContainer();

            ui.Add(new CuiElement
            {
                Name = UIMain,
                Parent = "Overlay",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string)ImageLibrary.Call("GetImage", $"{Name}.Background.1"),
                        Color = "1 1 1 0.95"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "-0.001 0.89",
                        AnchorMax = "-0.001 0.89",
                        OffsetMin = "0 0",
                        OffsetMax = "349 70"
                    }
                }
            });

            ui.Add(new CuiLabel
            {
                Text =
                {
                    Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 0.5",
                    FontSize = 12,
                    Text = "<b>You connected through the reserve IP</b>\n".ToUpper() +
                           "<size=10>For a stable connection, reconnect via this address\n\n" +
                           "Click for more information</size>",
                    Font = "robotocondensed-regular.ttf"
                },
                RectTransform =
                {
                    AnchorMin = "0.03 0",
                    AnchorMax = "1 1"
                }
            }, UIMain);

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Input",
                Parent = UIMain,
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        ReadOnly = true, LineType = InputField.LineType.SingleLine,
                        Text = "client.connect phoenixrust.ru:28015"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "0 0",
                        OffsetMax = "350 65"
                    }
                }
            });

            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.AddUi(player, ui);
        }

        private void OnServerInitialized()
        {
            LoadDefaultMessages();
            ImageLibrary.Call("AddImage", "https://i.imgur.com/jpC8NFI.png", $"{Name}.Background.1");
        }

        [ConsoleCommand("asd")]
        private void Rec(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            DrawUI_Main(player);
        }
    }
}