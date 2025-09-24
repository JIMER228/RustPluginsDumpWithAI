using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Shortmenu", "Greyghost", "1.0.2")]
    [Description("Interface for displaying game statistics")]
    public class Shortmenu : RustPlugin
    {
        #region [Reference]

#pragma warning disable CS0649
        [PluginReference] private Plugin ImageLibrary;
#pragma warning restore CS0649
        private StoredData _dataBase = new StoredData();

        #endregion

        #region [Configuraton] / [Конфигурация]

        private ConfigData _config;

        public class ConfigData
        {
            [JsonProperty(PropertyName = "ShortMenu - Настройка")]
            public MenuCFG MenuConfig = new MenuCFG();

            public class MenuCFG
            {
                [JsonProperty(PropertyName = "Черный список IP (192.168.0.1)")]
                public List<string> BlockedIP;
            }
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                MenuConfig = new ConfigData.MenuCFG
                {
                    BlockedIP = new List<string>
                    {
                        "192.168.0.1"
                    }
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<ConfigData>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintError("Файл конфигурации поврежден (или не существует), создан новый!");
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        #endregion

        #region [Vars]

        private const string UIMain = "Shortmenu.Main";
        private const string UIMenu = "Shortmenu.Main.Menu";

        private readonly WaitForSeconds _waitForSeconds = new WaitForSeconds(0.1f);

        private readonly Dictionary<string, string> _images = new Dictionary<string, string>
        {
            ["UI.Menu.Background1"] = "https://gspics.org/images/2022/07/01/0139bj.png",
            ["UI.Menu.Background2"] = "https://gspics.org/images/2022/07/01/0134ME.png",
        };

        #endregion

        #region [UI]

        private void OpenUI_Main(BasePlayer player)
        {
            var ui = new CuiElementContainer
            {
                {
                    new CuiPanel
                    {
                        CursorEnabled = true,
                        Image =
                        {
                            Color = "0.13 0.13 0.15 0.00"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.1557292 0.1870372",
                            AnchorMax = "0.8067709 0.8379631"
                        }
                    },
                    "Overlay", UIMain
                },
                new CuiElement
                {
                    Name = UIMenu,
                    Parent = UIMain,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = lang.GetLanguage(player.UserIDString) == "ru"
                                ? GetImage("UI.Menu.Background1")
                                : GetImage("UI.Menu.Background2")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    }
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Color = "0 0 0 0",
                            Close = UIMain,
                        },
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        },
                        Text =
                        {
                            Text = ""
                        }
                    },
                    UIMenu
                }
            };

            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.AddUi(player, ui);
            if (!_dataBase.Players.Contains(player.userID))
            {
                _dataBase.Players.Add(player.userID);
            }
        }

        #endregion

        #region [Hooks]

        // ReSharper disable once UnusedMember.Local
        private void OnServerSave()
        {
            SaveData();
        }

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            LoadData();
            ServerMgr.Instance.StartCoroutine(LoadImages());
            cmd.AddChatCommand("menu", this, "OpenStatistic");
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void OnPlayerConnected(BasePlayer player, string reason)
        {
            if (_dataBase.Players.Contains(player.userID)) return;
            if (_config.MenuConfig.BlockedIP.Any(ip => player.Connection.ipaddress.Contains(ip))) return;
            NextFrame(() => OpenUI_Main(player));
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void OnNewSave(string filename)
        {
            _dataBase.Players.Clear();
            SaveData();
        }

        // ReSharper disable once UnusedMember.Local
        private void Unload()
        {
            SaveData();
        }

        #endregion

        #region [Commands]

        // ReSharper disable once UnusedMember.Local
        private void OpenStatistic(BasePlayer player)
        {
            OpenUI_Main(player);
        }

        [ConsoleCommand("CloseShortmenu")]
        // ReSharper disable once UnusedMember.Local
        private void CloseStatistic(ConsoleSystem.Arg args)
        {
            CuiHelper.DestroyUi(args.Player(), UIMain);
        }

        [ChatCommand("addblockip")]
        // ReSharper disable once UnusedMember.Local
        private void AddBlock(BasePlayer player, string Command, string[] args)
        {
            _config.MenuConfig.BlockedIP.Add(args[0]);
            // SaveData();
        }

        #endregion

        #region [DataBase]

        private class StoredData
        {
            public List<ulong> Players = new List<ulong>();
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _dataBase);

        private void LoadData()
        {
            try
            {
                _dataBase = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch (Exception)
            {
                _dataBase = new StoredData();
            }
        }

        #endregion

        #region [Helpers]

        private string GetImage(string image)
        {
            return (string) ImageLibrary.Call("GetImage", image);
        }

        private IEnumerator LoadImages()
        {
            PrintWarning("Load images...");
            foreach (var image in _images)
            {
                ImageLibrary.Call("AddImage", image.Value, image.Key);
                yield return _waitForSeconds;
            }

            PrintWarning("Images loaded");
            yield return 0;
        }

        #endregion
    }
}