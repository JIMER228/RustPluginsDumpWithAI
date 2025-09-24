//Requires: ImageLibrary
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Class Selector", "sami37", "1.0.1")]
    [Description("Display UI to allow all player to select a class.")]
    public class ClassSelector : RustPlugin
    {
        [PluginReference] ImageLibrary ImageLibrary;
        public Dictionary<ulong, string> ClassSelect = new Dictionary<ulong, string>();
        private const string OverlayMain = "ClassSelectorMainUI";
        private string dataDirectory = $"file://{Interface.Oxide.DataDirectory}{Path.DirectorySeparatorChar}Arena{Path.DirectorySeparatorChar}Images{Path.DirectorySeparatorChar}";
        private const float EDITOR_ELEMENT_HEIGHT = 0.3f;
        private const float EDITOR_ELEMENT_WIDTH = 0.3f;

        public class PlayerClass
        {
            public string ClassName { get; set; }
            public string PanelName { get; set; }
            public DescriptionProperty Description { get; set; }
            public string RoleName { get; set; }
            public UI4 Position { get; set; }
            public ImageProperty Image { get; set; }

            public class DescriptionProperty
            {
                public string Description;
                public UI4 Position;
            }

            public class ImageProperty
            {
                public UI4 ImagePosition;
                public string URL;
            }

        }

        #region Image Management
        private void LoadImages()
        {
            Puts("[Warning] Icon images have not been found. Uploading images to file storage");

            Dictionary<string, string> newLoadOrder = new Dictionary<string, string>();

            foreach (var infoClass in config.ClassListInfo)
            {
                if(!newLoadOrder.ContainsKey(infoClass.PanelName))
                    newLoadOrder.Add(infoClass.PanelName, infoClass.Image.URL);
            }

            ImageLibrary.ImportImageList(Title, newLoadOrder, 0, true);
        }

        public string GetImage(string name) => ImageLibrary.GetImage(name);
        #endregion

        #region Config
        private Configuration config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "Class list")]
            public List<PlayerClass> ClassListInfo = new List<PlayerClass>();


            [JsonProperty(PropertyName = "Civilian Class Name")]
            public string Civilian = "Civilian";
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    LoadDefaultConfig();
                    SaveConfig();
                }
            }
            catch
            {
                PrintWarning("Creating new config file.");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration
            {
                ClassListInfo = new List<PlayerClass>
                {
                    new PlayerClass
                    {
                        ClassName = "Hunter",
                        PanelName = "Hunter",
                        Description = new PlayerClass.DescriptionProperty
                        {
                            Description = "Hunter Description",
                            Position = new UI4(0f, 0f, 1f, 0.5f)
                        },
                        Image = new PlayerClass.ImageProperty
                        {
                            ImagePosition = new UI4(0.3f, 0.5f, 0.7f, 0.9f),
                            URL = "https://alcedochassepeche.com/63892-large_default/fusil-groose.jpg"
                        },
                        RoleName = "hunter",
                        Position = new UI4(
                            0,
                            0,
                            0.25f,
                            0.5f
                        )
                    },
                    new PlayerClass
                    {
                        ClassName = "Doctor",
                        PanelName = "Doctor",
                        Description = new PlayerClass.DescriptionProperty
                        {
                            Description = "Healer Description",
                            Position = new UI4(0f, 0f, 1f, 0.5f)
                        },
                        Image = new PlayerClass.ImageProperty
                        {
                            URL = "https://oldschool.runescape.wiki/images/6/67/Healer_icon_detail.png?4568a",
                            ImagePosition = new UI4(0.3f, 0.5f, 0.7f, 0.9f),
                        },
                        RoleName = "healer",
                        Position = new UI4(
                            0.25f,
                            0,
                            0.5f,
                            0.5f
                        )
                    },
                    new PlayerClass
                    {
                        ClassName = "Mechanic",
                        PanelName = "Mechanic",
                        Description = new PlayerClass.DescriptionProperty
                        {
                            Description = "Mechanic Description",
                            Position = new UI4(0f, 0f, 1f, 0.5f)
                        },
                        Image = new PlayerClass.ImageProperty
                        {
                            URL = "https://cdn2.clc2l.fr/c/thumbnail75webp/t/s/y/system-mechanic-R21mI9.png",
                            ImagePosition = new UI4(0.3f, 0.5f, 0.7f, 0.9f),
                        },
                        RoleName = "mechanic",
                        Position = new UI4(
                            0.5f,
                            0,
                            0.75f,
                            0.5f
                        )
                    },
                    new PlayerClass
                    {
                        ClassName = "Fisherman",
                        PanelName = "Fisherman",
                        Description = new PlayerClass.DescriptionProperty
                        {
                            Description = "Fisherman Description",
                            Position = new UI4(0f, 0f, 1f, 0.5f)
                        },
                        Image = new PlayerClass.ImageProperty
                        {
                            URL = "https://lezebre.lu/images/detailed/81/46222-Sticker-Pecheur-Canne-a-peche.png",
                            ImagePosition = new UI4(0.3f, 0.5f, 0.7f, 0.9f),
                        },
                        RoleName = "fisherman",
                        Position = new UI4(
                            0.75f,
                            0,
                            1f,
                            0.5f
                        )
                    },
                    new PlayerClass
                    {
                        ClassName = "HandyMan",
                        PanelName = "HandyMan",
                        Description = new PlayerClass.DescriptionProperty
                        {
                            Description = "HandyMan Description",
                            Position = new UI4(0f, 0f, 1f, 0.5f)
                        },
                        Image = new PlayerClass.ImageProperty
                        {
                            URL =
                                "https://www.pngimages.pics/images/quotes/english/general/handyman-png-image-clipart-52650-150918.png",
                            ImagePosition = new UI4(0.3f, 0.5f, 0.7f, 0.9f),
                        },
                        RoleName = "handyman",
                        Position = new UI4(
                            0,
                            0.5f,
                            0.25f,
                            1f
                        )
                    },
                    new PlayerClass
                    {
                        ClassName = "Civilian",
                        PanelName = "Civilian",
                        Description = new PlayerClass.DescriptionProperty
                        {
                            Description = "Civilian Description",
                            Position = new UI4(0f, 0f, 1f, 0.5f)
                        },
                        Image = new PlayerClass.ImageProperty
                        {
                            URL = "https://pic.onlinewebfonts.com/svg/img_178843.png",
                            ImagePosition = new UI4(0.3f, 0.5f, 0.7f, 0.9f),
                        },
                        RoleName = "civilian",
                        Position = new UI4(
                            0.25f,
                            0.5f,
                            0.5f,
                            1f
                        )
                    },
                    new PlayerClass
                    {
                        ClassName = "Shop Keeper",
                        PanelName = "Shop Keeper",
                        Description = new PlayerClass.DescriptionProperty
                        {
                            Description = "Shop Keeper Description",
                            Position = new UI4(0f, 0f, 1f, 0.5f)
                        },
                        Image = new PlayerClass.ImageProperty
                        {
                            URL = "https://icon-library.com/images/worker-icon-png/worker-icon-png-25.jpg",
                            ImagePosition = new UI4(0.3f, 0.5f, 0.7f, 0.9f),
                        },
                        RoleName = "keeper",
                        Position = new UI4(
                            0.5f,
                            0.5f,
                            0.75f,
                            1f
                        )
                    },
                    new PlayerClass
                    {
                        ClassName = "Cafe Owner",
                        PanelName = "Cafe Owner",
                        Description = new PlayerClass.DescriptionProperty
                        {
                            Description = "Cafe Owner Description",
                            Position = new UI4(0f, 0f, 1f, 0.5f)
                        },
                        Image = new PlayerClass.ImageProperty
                        {
                            URL =
                                "https://icon-library.com/images/restaurant-icon-transparent/restaurant-icon-transparent-23.jpg",
                            ImagePosition = new UI4(0.3f, 0.5f, 0.7f, 0.9f),
                        },
                        RoleName = "cafeowner",
                        Position = new UI4(
                            0.75f,
                            0.5f,
                            1f,
                            1f
                        )
                    }
                }
            };
        }

        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        private void Loaded()
        {
            ClassSelect = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, string>>(Name);
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, OverlayMain);
            }
        }

        private void ValidateImages()
        {
            Puts("[Warning] Validating imagery");
            LoadImages();
        }

        void OnServerInitialized()
        {
            ValidateImages();
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.Once(2, () => OnPlayerConnected(player));
                return;
            }

            if (ClassSelect == null || !ClassSelect.ContainsKey(player.userID))
            {
                OpenUi(player);
            }
            else if(ClassSelect != null && ClassSelect.ContainsKey(player.userID) && ClassSelect[player.userID] == config.Civilian)
                OpenUi(player);
        }

        private float GetVerticalPos(int i, float start = 0.1f) => start + i * (EDITOR_ELEMENT_HEIGHT + 0.005f);

        private float GetHorizontalPos(int i, float start = 0.1f) => start + i * (EDITOR_ELEMENT_WIDTH + 0.005f);

        [ChatCommand("test")]
        void test(BasePlayer player, string cmd, string[] args)
        {
            OpenUi(player);
        }

        void OpenUi(BasePlayer player)
        {
            if (ClassSelect == null) ClassSelect = new Dictionary<ulong, string>();

            var elementsContainer = new CuiElementContainer();

            elementsContainer.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = new UI4(0, 0, 1, 1).GetMin(),
                    AnchorMax = new UI4(0, 0, 1, 1).GetMax()
                },
                CursorEnabled = true
            }, new CuiElement().Parent, OverlayMain);

            foreach (var info in config.ClassListInfo)
            {

                elementsContainer.Add(new CuiPanel
                {
                    Image =
                    {
                        Color = "0 0 0 0"
                    },
                    RectTransform =
                    {
                        AnchorMin = info.Position.GetMin(),
                        AnchorMax = info.Position.GetMax()
                    }
                }, OverlayMain, info.PanelName);
                elementsContainer.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = info.PanelName,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = GetImage(info.PanelName),
                            Color = "0 0 0 1"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = info.Image.ImagePosition.GetMin(),
                            AnchorMax = info.Image.ImagePosition.GetMax()
                        }
                    }
                });
                elementsContainer.Add(new CuiButton
                {
                    Text =
                    {
                        Text = info.Description.Description,
                        Align = TextAnchor.MiddleCenter
                    },
                    RectTransform =
                    {
                        AnchorMin = info.Description.Position.GetMin(),
                        AnchorMax = info.Description.Position.GetMax()
                    },
                    Button =
                    {
                        Command = $""
                    }
                }, info.PanelName);
                elementsContainer.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = info.ClassName,
                        Align = TextAnchor.UpperCenter
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }, info.PanelName);
            }

            CuiHelper.AddUi(player, elementsContainer);
        }

        public class UI4
        {
            public float xMin, yMin, xMax, yMax;

            public UI4(float xMin, float yMin, float xMax, float yMax)
            {
                this.xMin = xMin;
                this.yMin = yMin;
                this.xMax = xMax;
                this.yMax = yMax;
            }

            public string GetMin() => $"{xMin} {yMin}";

            public string GetMax() => $"{xMax} {yMax}";

            private static UI4 _full;

            public static UI4 Full
            {
                get
                {
                    if (_full == null)
                        _full = new UI4(0, 0, 1, 1);
                    return _full;
                }
            }
        }
    }
}
