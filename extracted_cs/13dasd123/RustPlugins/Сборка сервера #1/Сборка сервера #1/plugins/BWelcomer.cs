using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BWelcomer","https://discord.gg/9vyTXsJyKR","1.0")]
    public class BWelcomer : RustPlugin
    {

        [PluginReference] private Plugin ImageLibrary;


        #region data

        Dictionary<ulong,bool> _player = new Dictionary<ulong, bool>();
        

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("BWelcomer",_player);
        }

        void LoadData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("BWelcomer"))
            {
                _player = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, bool>>("BWelcomer");
            }
        }
        
        void Unload()
        {
            SaveData();
        }

        #endregion

        void OnServerInitialized()
        {
            ImageLibrary.Call("AddImage",
                _Img,
                "btn");
            LoadData();
            SaveData();
            
        }
        /*void OnPlayerConnected(BasePlayer player)
        {
            PrintWarning($"{player.userID} connected!!!");
            
        }*/
        
        /*void OnPlayerRespawned(BasePlayer player)
        {
            
        }*/
        private List<ulong> queue = new List<ulong>();
        void OnPlayerConnected(BasePlayer player)
        {
            if (!_player.ContainsKey(player.userID))
            {
                _player.Add(player.userID,false);
                queue.Add(player.userID);
            }
        }
        
        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (!_player.ContainsKey(player.userID))
            {
                _player.Add(player.userID,false);
            }
            if (_player[player.userID] == false && queue.Contains(player.userID))
            {
                OpenUI(player);
                queue.Remove(player.userID);
                _player[player.userID] = true;
            }
            SaveData();
        }

        #region UI

        private string _layer = "WelcomeUI";
        
        string _materialGrey = "assets/icons/greyout.mat";
        private string _materialBlur = "assets/content/ui/uibackgroundblur.mat";

        private string _Img =
            "https://gspics.org/images/2022/06/13/0nec8x.png";
        void OpenUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, _layer);
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                Image = {Color = "0 0 0 0.95",Material = _materialBlur},
                RectTransform = {AnchorMin = "0 0",AnchorMax = "1 1"},
                CursorEnabled = true
            }, "Overlay", _layer);

            container.Add(new CuiPanel
            {
                Image = {Color = "0 0 0 0.8"},
                RectTransform = {AnchorMin = "0 0.3",AnchorMax = "1 0.7"}
            }, _layer, "TextPanel");
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMax = "0.6 0.98",AnchorMin = "0.4 0"},
                Text = {Text = "<color=#B1D6F1>NUKA RUST</color>",FontSize = 22,FadeIn = 0.3f,Align = TextAnchor.UpperCenter}
            }, "TextPanel", "Title");
            
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMax = "1 0.8",AnchorMin = "0 0.2"},
                Text =
                {
                    Text =
                        "                            Изменить текст велкомера можете в <color=#EBD4AE>BWelcomer.cs</color>!\n                            По всем техничеcким вопросам и пополнению баланса обращайтесь к Vi#2180\n                            Розыгрыши призов во Вконтакте: <color=#EBD4AE>ваш ВК</color>\n                            Бесплатный ежедневный кейс: <color=#EBD4AE>ваш сайт</color>",
                    FontSize = 18,
                    FadeIn = 0.3f,
                    Align = TextAnchor.
UpperLeft
                }
            }, "TextPanel", "Text");
            
            container.Add(new CuiPanel
            {
                Image = {Color = "0 0 0 0"}
                ,
                RectTransform = {AnchorMin = "0.2 0.05",AnchorMax = "0.4 0.2"}
            }, "TextPanel", "Close");
            container.Add(new CuiElement
            {
                Parent = "Close",
                Name = "Img",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage","btn")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",AnchorMax = "1 1"
                    }
                    
                }
            });

            container.Add(new CuiButton
            {
                Button = {Close = _layer,Color = "0 0 0 0"},
                Text = {Text = "ЗАКРЫТЬ",Align = TextAnchor.MiddleCenter,FontSize = 18},
                RectTransform = { AnchorMin = "0 0",AnchorMax = "1 1"}
            }, "Close","Btn");

            container.Add(new CuiPanel
            {
                Image = {Color = "0 0 0 0"}
                ,
                RectTransform = {AnchorMin = "0.6 0.05",AnchorMax = "0.8 0.2"}
            }, "TextPanel", "Menu");
            container.Add(new CuiElement
            {
                Parent = "Menu",
                Name = "Img",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = (string) ImageLibrary.Call("GetImage","btn")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",AnchorMax = "1 1"
                    }
                    
                }
            });
            
            container.Add(new CuiButton
            {
                Button = {Close = _layer,Command = "chat.say /menu",Color = "0 0 0 0"},
                Text = {Text = "В МЕНЮ",Align = TextAnchor.MiddleCenter,FontSize = 18},
                RectTransform = { AnchorMin = "0 0",AnchorMax = "1 1"}
            }, "Menu","Button");
            

            CuiHelper.AddUi(player, container);

        }

        #endregion
        
        #region commands

        /*[ChatCommand("welc")]
        void OPWelc(BasePlayer player, string command, string[] args)
        {
            OpenUI(player);
        }*/

        #endregion
    }
}