using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

using Newtonsoft.Json;
using System.Linq;

using Oxide.Core;
using Oxide.Plugins;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;

using UnityEngine;

namespace Oxide.Plugins 
{
    [Info("PrivelegeInfo", "Lolikon", "1.0.0")]
    [Description("Добавляет возможность просмотра дней до окончания привилегии.")]
        class PrivelegeInfo : RustPlugin
        {

            #region ConfigInformation
            private string PanelName = "InfoPanel";
            private string AnchorMin = "0.39 0.53";
            private string Anchormax = "0.652 0.622";
            private string Text = "  ДОСТУПНАЯ ВАМ ПРИВИЛЕГИЯ - VIP \n\n Доступные команды: \n  /kit - дополнительные наборы китов \n /rec - карманный переработчик\n /skin - скин на оружие \n /chat - персональный цвет, префикс в чате";
            private string ErrorText = "ЛИБО У ВАС НЕТУ ПРИВИЛЕГИИ, ЛИБО ОНА ЗАКОНЧИЛАСЬ.";
            private string PermissionName = "privelegeinfo.use";
            #endregion

            #region InitializationConfig
            protected override void LoadDefaultConfig()
            {
                PrintWarning("Создаю файл конфига...");
            }
            protected override void LoadConfig()
            {
                base.LoadConfig();
                GetConfig("ANCHOR MIN :", ref AnchorMin);
                GetConfig("ANCHOR MAX :", ref Anchormax);
                GetConfig("ТЕКСТ :", ref Text );
                GetConfig("ТЕКСТ У ИГРОКА БЕЗ ПРИВИЛЕГИИ :", ref ErrorText);
                GetConfig("НАЗВАНИЕ ПРИВИЛЕГИИ ДЛЯ ОТОБРАЖЕНИЯ :", ref PermissionName);
                SaveConfig();
            }

            private void GetConfig<T>(string Key, ref T var)
            {
                if (Config[Key] != null)
                {
                    var = (T)Convert.ChangeType(Config[Key], typeof(T));
                }
                Config[Key] = var;
            }
            #endregion


            #region DataFile

            #endregion

            #region dead_player
            object OnPlayerDie(BasePlayer player, HitInfo info)
            {
                CuiHelper.DestroyUi(player, PanelName);
                return null;
            }
            #endregion

            #region PermissionDone

            void Loaded () {
                permission.RegisterPermission (PermissionName, this);
            }

            #endregion

            #region TimeCounter
                private void TimeCounter(BasePlayer player){
                    if (permission.UserHasPermission (player.UserIDString, PermissionName)) {
                    }
                }

            #endregion


            #region UICreating
            private const string Layer = "UI_VIP";
            [ChatCommand("vip")]        
            private void DrawInterface(BasePlayer player)
            {
                CuiHelper.DestroyUi(player, Layer);
                CuiElementContainer elements = new CuiElementContainer();

                elements.Add(new CuiPanel
                { 
                    CursorEnabled = true, 
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"},
                    Image         = {FadeIn = 0f, Color     = "0 0 0 0.4"}
                }, "Overlay", Layer);

                if (permission.UserHasPermission (player.UserIDString, PermissionName)) {
                    elements.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = AnchorMin, AnchorMax = Anchormax },
                        Text = { Text = Text, FontSize = 19, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FadeIn = 1f }
                    }, Layer);
                }
                else{

                    elements.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = AnchorMin, AnchorMax = Anchormax },
                        Text = { Text = ErrorText, FontSize = 19, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FadeIn = 1f }
                    }, Layer);

                }    

                elements.Add(new CuiButton  
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"},
                    Button        = {Color     = "0 0 0 0", Close = Layer},
                    Text          = {Text      = ""}
                }, Layer);

                CuiHelper.AddUi(player, elements); 
            }
            #endregion
        }
}