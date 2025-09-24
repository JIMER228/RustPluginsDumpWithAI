using System.Collections.Generic;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Oxide.Plugins
{
    [Info("ServerNameGui", "MJSU", "0.0.1")]
    [Description("Displays the server name in a gui on the players screen")]
    class ServerNameGui : RustPlugin
    {
        #region Class Fields
        private PluginConfig _pluginConfig; //Plugin Config
        private StoredData _storedData;
        private const string GuiContainerName = "ServerNameGui_Container";
        //private CuiElementContainer _container;
        private uint _iconId;
        private readonly List<int> _avaliableScreenSizes = new List<int> { 2560, 1920, 1600, 1366 };
        #endregion

        #region Setup & Loading
        private void Loaded()
        {
            _pluginConfig = ConfigOrDefault(Config.ReadObject<PluginConfig>());
            if (_pluginConfig.DisplayName == null) PrintError("Loading config file failed. Using default config");
            else Config.WriteObject(_pluginConfig, true);

            _storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("ServerNameGui");

            //LoadImage();
            LoadUiForConnectedPlayers();
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Creates the UI to be sent to all players
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        private CuiElementContainer CreateServerGui(int size)
        {
            CuiElementContainer container;
            switch (size)
            {
                case 2560:
                    container = UICreator.CreateElementContainer(GuiContainerName, "1 0.95 0.875 0.025", ".0095 .1", ".1214 .135", 0f, 0f); //2560
                    break;

                case 1600:
                case 1366:
                    container = UICreator.CreateElementContainer(GuiContainerName, "1 0.95 0.875 0.025", ".0125 .1", ".162 .135", 0f, 0f); //1366
                    break;

                case 1920:
                    container = UICreator.CreateElementContainer(GuiContainerName, "1 0.95 0.875 0.025", ".0125 .1", ".1616 .135", 0f, 0f); //1920
                    break;

                default:
                    container = UICreator.CreateElementContainer(GuiContainerName, "1 0.95 0.875 0.025", ".0125 .1", ".1616 .135", 0f, 0f); //1920
                    break;
            }
            
            //if (_iconId != 0) UICreator.LoadImage(ref _container, GuiContainerName, $"{_iconId}", ".0075 .10", ".125 .8"); //1080
            //UICreator.CreateLabel(ref container, GuiContainerName, ".8 .8 .8 .8", _pluginConfig.DisplayName, 16, ".155 0", "1 1"); //Image
            UICreator.CreateLabel(ref container, GuiContainerName, ".8 .8 .8 .8", _pluginConfig.DisplayName, 16, "0 0", "1 1"); //No Image

            return container;
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Loops over the players and adds the UI to their screen
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        private void LoadUiForConnectedPlayers()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                LoadGuiForPlayer(player);
            }
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// load the default config for this plugin
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Loading Default Config");
            Config.WriteObject(ConfigOrDefault(null), true);
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Uses the values passed in from config. If any values are null it updates them with default values
        /// </summary>
        /// <param name="config">Config that has been loaded or null</param>
        /// <returns>Config using values passed in from config default values</returns>
        /// ////////////////////////////////////////////////////////////////////////
        private PluginConfig ConfigOrDefault(PluginConfig config)
        {
            return new PluginConfig
            {
                DisplayName = config?.DisplayName ?? $"{ConVar.Server.hostname}",
            };
        }

        private void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, GuiContainerName);
            }
            //FileStorage.server.RemoveEntityNum(_iconId, _iconId);
        }
        #endregion

        #region Chat Command
        [ChatCommand("sname")]
        private void ScreenSizeChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;

            if (args.Length != 1)
            {
                
            }

            int size;
            if (!int.TryParse(args[0], out size)) return;

            if (!_avaliableScreenSizes.Contains(size)) return;

            _storedData.ScreenSize[player.userID] = size;

            LoadGuiForPlayer(player);

            Interface.Oxide.DataFileSystem.WriteObject("ServerNameGui", _storedData);
        }

        #endregion

        #region Oxide Hooks
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// When player connects add the GUI to their screen
        /// </summary>
        /// <param name="player"></param>
        /// ////////////////////////////////////////////////////////////////////////
        void OnPlayerInit(BasePlayer player)
        {
            if (!_storedData.ScreenSize.ContainsKey(player.userID)) _storedData.ScreenSize[player.userID] = 1080;
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            LoadGuiForPlayer(player);
        }

        private void LoadGuiForPlayer(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, GuiContainerName);
            CuiHelper.AddUi(player, CreateServerGui(_storedData.ScreenSize[player.userID]));
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// When plays disconnects remove the GUI from their screen
        /// </summary>
        /// <param name="player"></param>
        /// ////////////////////////////////////////////////////////////////////////
        private void OnPlayerDisconnected(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, GuiContainerName);
        }
        #endregion

        //private void LoadImage()
        //{
        //    using (var www = new WWW("http://www.newdesignfile.com/postpic/2010/03/home-icon-white_338306.png"))
        //    {
        //        while (!www.isDone) { }

        //        if (string.IsNullOrEmpty(www.error))
        //        {
        //            var stream = new MemoryStream();
        //            stream.Write(www.bytes, 0, www.bytes.Length);
        //            _iconId = FileStorage.server.Store(stream, FileStorage.Type.png, uint.MaxValue-1001);
        //        }
        //        else
        //        {
        //            Debug.Log("Error downloading img");
        //            ConsoleSystem.Run.Server.Normal("oxide.unload ServerNameGui");
        //        }
        //    }
        //}

        #region Classes
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Plugin Config class
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        class PluginConfig
        {
            public string DisplayName { get; set; }
        }
        #endregion

        #region UICreator Class
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once ClassNeverInstantiated.Local
        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// @credit to k1lly0u - code from ServerRewards
        /// UI class for SurveyInfo
        /// </summary>
        /// //////////////////////////////////////////////////////////////////////////////////////
        private class UICreator
        {
            public static CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, float fadeIn, float fadeOut)
            {
                var newElement = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color , FadeIn = fadeIn},
                            RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                            CursorEnabled = false,
                            FadeOut = fadeOut
                        },
                        new CuiElement().Parent = "Overlay",
                        panelName
                    }
                };
                return newElement;
            }
            public static void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel, CuiHelper.GetGuid());

            }
            public static void LoadImage(ref CuiElementContainer container, string panel, string png, string aMin, string aMax)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent {Png = png },
                        new CuiRectTransformComponent {AnchorMin = aMin, AnchorMax = aMax }
                    }
                });
            }
        }

        class StoredData
        {
            public Hash<ulong, int> ScreenSize = new Hash<ulong, int>();
        }
        #endregion
    }
}
