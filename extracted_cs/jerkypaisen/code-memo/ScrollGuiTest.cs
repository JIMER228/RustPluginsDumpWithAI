using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("ScrollGuiTest", "jerky", "0.0.1")]
    class ScrollGuiTest : CovalencePlugin
    {
        #region json
        private static string TEMPLATE = @"
 [
    {
        ""name"": ""UI"",
        ""parent"":""Overlay"",
        ""destroyUi"": ""UI"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.RawImage"",
                ""color"": ""0.03 0.03 0.03 1.0"",
                ""sprite"": ""Assets/Content/UI/UI.Background.Tile.psd""
            },
            {
                ""type"":""RectTransform"",
                ""anchormin"": ""0 0"",
                ""anchormax"": ""1 1""
            },
            {
                ""type"":""NeedsCursor""
            }
        ]
    },
    {
        ""name"": ""Example"",
        ""parent"":""UI"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.RawImage"",
                ""color"": ""0.1 0.1 0.1 1.0"",
                ""sprite"": ""Assets/Content/UI/UI.Background.Tile.psd""
            },
            {
                ""type"":""RectTransform"",
                ""anchormin"": ""0.3 0"",
                ""anchormax"": ""0.7 1""
            },
            {
                ""type"":""NeedsCursor""
            }
        ]
    },
    {
        ""parent"": ""Example"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.Text"",
                ""text"":""Scroll View"",
                ""fontSize"":24,
                ""align"": ""MiddleCenter"",
            },
            {
                ""type"":""RectTransform"",
                ""anchormin"": ""0 1"",
                ""anchormax"": ""1 1"",
                ""offsetmin"": ""0 -50""
            }
        ]
    },
    {
        ""name"": ""Scroller"",
        ""parent"":""Example"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.RawImage"",
                ""sprite"": ""assets/content/effects/crossbreed/fx gradient skewed.png"",
                ""color"": ""0.05 0.05 0.05 0.5"",
            },
            {
                ""type"":""UnityEngine.UI.ScrollView"",
                ""contentTransform"": {
                    ""anchormin"": ""0 1"",
                    ""anchormax"": ""1 1"",
                    ""offsetmin"": ""0 -1500"",
                    ""offsetmax"": ""0 0""
                },
                ""vertical"": true,
                ""horizontal"": false,
                ""movementType"": ""Elastic"",
                ""elasticity"": 0.25,
                ""inertia"": true,
                ""decelerationRate"": 0.3,
                ""scrollSensitivity"": 24.0,
                ""maskSoftness"": ""0 100"",
                ""verticalScrollbar"": {
                    ""size"": 20,
                    ""autoHide"": true
                },
                ""horizontalScrollbar"": {
                    ""size"": 20,
                    ""autoHide"": true
                }
            },
            {
                ""type"":""RectTransform"",
                ""anchormin"": ""0 0"",
                ""anchormax"": ""1 1"",
                ""offsetmax"": ""0 -50""
            },
            {
                ""type"":""NeedsCursor""
            }
        ]
    },
    {
        ""name"": ""child1"",
        ""parent"":""Scroller"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.RawImage"",
                ""color"": ""0.2 0.2 0.2 1.0"",
                ""sprite"": ""Assets/Content/UI/UI.Background.Tile.psd"",
            },
            {
                ""type"":""RectTransform"",
                ""anchormin"": ""0.03 0.97"",
                ""anchormax"": ""0.97 0.97"",
                ""offsetmin"": ""0 -90"",
                ""offsetmax"": ""0 0""
            },
            {
                ""type"": ""UnityEngine.UI.Outline"",
                ""distance"": ""2 2"",
                ""color"": ""0 0 0 0.06""
            },
            {
                ""type"": ""UnityEngine.UI.Outline"",
                ""distance"": ""4 4"",
                ""color"": ""0 0 0 0.025""
            }
        ]
    },
    {
        ""parent"": ""child1"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.Text"",
                ""text"":""child1 Text"",
                ""color"": ""0.6 0.6 0.6 1.0"",
                ""fontSize"":26,
                ""align"": ""MiddleCenter""
            }
        ]
    },
    {
        ""name"": ""child2"",
        ""parent"":""Scroller"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.RawImage"",
                ""color"": ""0.2 0.2 0.2 1.0"",
                ""sprite"": ""Assets/Content/UI/UI.Background.Tile.psd"",
            },
            {
                ""type"":""RectTransform"",
                ""anchormin"": ""0.03 0.97"",
                ""anchormax"": ""0.97 0.97"",
                ""offsetmin"": ""0 -190"",
                ""offsetmax"": ""0 -100""
            },
            {
                ""type"": ""UnityEngine.UI.Outline"",
                ""distance"": ""2 2"",
                ""color"": ""0 0 0 0.06""
            },
            {
                ""type"": ""UnityEngine.UI.Outline"",
                ""distance"": ""4 4"",
                ""color"": ""0 0 0 0.025""
            }
        ]
    },
    {
        ""parent"": ""child2"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.Text"",
                ""text"":""child2 Text"",
                ""color"": ""0.6 0.6 0.6 1.0"",
                ""fontSize"":26,
                ""align"": ""MiddleCenter""
            }
        ]
    },
    {
        ""name"": ""child3"",
        ""parent"":""Scroller"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.RawImage"",
                ""color"": ""0.2 0.2 0.2 1.0"",
                ""sprite"": ""Assets/Content/UI/UI.Background.Tile.psd"",
            },
            {
                ""type"":""RectTransform"",
                ""anchormin"": ""0.03 0.97"",
                ""anchormax"": ""0.97 0.97"",
                ""offsetmin"": ""0 -290"",
                ""offsetmax"": ""0 -200""
            },
            {
                ""type"": ""UnityEngine.UI.Outline"",
                ""distance"": ""2 2"",
                ""color"": ""0 0 0 0.06""
            },
            {
                ""type"": ""UnityEngine.UI.Outline"",
                ""distance"": ""4 4"",
                ""color"": ""0 0 0 0.025""
            }
        ]
    },
    {
        ""parent"": ""child3"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.Text"",
                ""text"":""child3 Text"",
                ""color"": ""0.6 0.6 0.6 1.0"",
                ""fontSize"":26,
                ""align"": ""MiddleCenter""
            }
        ]
    },
    {
        ""name"": ""child4"",
        ""parent"":""Scroller"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.RawImage"",
                ""color"": ""0.2 0.2 0.2 1.0"",
                ""sprite"": ""Assets/Content/UI/UI.Background.Tile.psd"",
            },
            {
                ""type"":""RectTransform"",
                ""anchormin"": ""0.03 0.97"",
                ""anchormax"": ""0.97 0.97"",
                ""offsetmin"": ""0 -390"",
                ""offsetmax"": ""0 -300""
            },
            {
                ""type"": ""UnityEngine.UI.Outline"",
                ""distance"": ""2 2"",
                ""color"": ""0 0 0 0.06""
            },
            {
                ""type"": ""UnityEngine.UI.Outline"",
                ""distance"": ""4 4"",
                ""color"": ""0 0 0 0.025""
            }
        ]
    },
    {
        ""parent"": ""child4"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.Text"",
                ""text"":""child4 Text"",
                ""color"": ""0.6 0.6 0.6 1.0"",
                ""fontSize"":26,
                ""align"": ""MiddleCenter""
            }
        ]
    },
    {
        ""name"": ""child5"",
        ""parent"":""Scroller"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.RawImage"",
                ""color"": ""0.2 0.2 0.2 1.0"",
                ""sprite"": ""Assets/Content/UI/UI.Background.Tile.psd"",
            },
            {
                ""type"":""RectTransform"",
                ""anchormin"": ""0.03 0.97"",
                ""anchormax"": ""0.97 0.97"",
                ""offsetmin"": ""0 -490"",
                ""offsetmax"": ""0 -400""
            },
            {
                ""type"": ""UnityEngine.UI.Outline"",
                ""distance"": ""2 2"",
                ""color"": ""0 0 0 0.06""
            },
            {
                ""type"": ""UnityEngine.UI.Outline"",
                ""distance"": ""4 4"",
                ""color"": ""0 0 0 0.025""
            }
        ]
    },
    {
        ""parent"": ""child5"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.Text"",
                ""text"":""child5 Text"",
                ""color"": ""0.6 0.6 0.6 1.0"",
                ""fontSize"":26,
                ""align"": ""MiddleCenter""
            }
        ]
    },
    {
        ""name"": ""child6"",
        ""parent"":""Scroller"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.RawImage"",
                ""color"": ""0.2 0.2 0.2 1.0"",
                ""sprite"": ""Assets/Content/UI/UI.Background.Tile.psd"",
            },
            {
                ""type"":""RectTransform"",
                ""anchormin"": ""0.03 0.97"",
                ""anchormax"": ""0.97 0.97"",
                ""offsetmin"": ""0 -590"",
                ""offsetmax"": ""0 -500""
            },
            {
                ""type"": ""UnityEngine.UI.Outline"",
                ""distance"": ""2 2"",
                ""color"": ""0 0 0 0.06""
            },
            {
                ""type"": ""UnityEngine.UI.Outline"",
                ""distance"": ""4 4"",
                ""color"": ""0 0 0 0.025""
            }
        ]
    },
    {
        ""parent"": ""child6"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.Text"",
                ""text"":""child6 Text"",
                ""color"": ""0.6 0.6 0.6 1.0"",
                ""fontSize"":26,
                ""align"": ""MiddleCenter""
            }
        ]
    },
    {
        ""name"": ""child7"",
        ""parent"":""Scroller"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.RawImage"",
                ""color"": ""0.2 0.2 0.2 1.0"",
                ""sprite"": ""Assets/Content/UI/UI.Background.Tile.psd"",
            },
            {
                ""type"":""RectTransform"",
                ""anchormin"": ""0.03 0.97"",
                ""anchormax"": ""0.97 0.97"",
                ""offsetmin"": ""0 -690"",
                ""offsetmax"": ""0 -600""
            },
            {
                ""type"": ""UnityEngine.UI.Outline"",
                ""distance"": ""2 2"",
                ""color"": ""0 0 0 0.06""
            },
            {
                ""type"": ""UnityEngine.UI.Outline"",
                ""distance"": ""4 4"",
                ""color"": ""0 0 0 0.025""
            }
        ]
    },
    {
        ""parent"": ""child7"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.Text"",
                ""text"":""child7 Text"",
                ""color"": ""0.6 0.6 0.6 1.0"",
                ""fontSize"":26,
                ""align"": ""MiddleCenter""
            }
        ]
    },
    {
        ""name"": ""child8"",
        ""parent"":""Scroller"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.RawImage"",
                ""color"": ""0.2 0.2 0.2 1.0"",
                ""sprite"": ""Assets/Content/UI/UI.Background.Tile.psd"",
            },
            {
                ""type"":""RectTransform"",
                ""anchormin"": ""0.03 0.97"",
                ""anchormax"": ""0.97 0.97"",
                ""offsetmin"": ""0 -790"",
                ""offsetmax"": ""0 -700""
            },
            {
                ""type"": ""UnityEngine.UI.Outline"",
                ""distance"": ""2 2"",
                ""color"": ""0 0 0 0.06""
            },
            {
                ""type"": ""UnityEngine.UI.Outline"",
                ""distance"": ""4 4"",
                ""color"": ""0 0 0 0.025""
            }
        ]
    },
    {
        ""parent"": ""child8"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.Text"",
                ""text"":""child8 Text"",
                ""color"": ""0.6 0.6 0.6 1.0"",
                ""fontSize"":26,
                ""align"": ""MiddleCenter""
            }
        ]
    },
    {
        ""name"": ""child9"",
        ""parent"":""Scroller"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.RawImage"",
                ""color"": ""0.2 0.2 0.2 1.0"",
                ""sprite"": ""Assets/Content/UI/UI.Background.Tile.psd"",
            },
            {
                ""type"":""RectTransform"",
                ""anchormin"": ""0.03 0.97"",
                ""anchormax"": ""0.97 0.97"",
                ""offsetmin"": ""0 -890"",
                ""offsetmax"": ""0 -800""
            },
            {
                ""type"": ""UnityEngine.UI.Outline"",
                ""distance"": ""2 2"",
                ""color"": ""0 0 0 0.06""
            },
            {
                ""type"": ""UnityEngine.UI.Outline"",
                ""distance"": ""4 4"",
                ""color"": ""0 0 0 0.025""
            }
        ]
    },
    {
        ""parent"": ""child9"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.Text"",
                ""text"":""child9 Text"",
                ""color"": ""0.6 0.6 0.6 1.0"",
                ""fontSize"":26,
                ""align"": ""MiddleCenter""
            }
        ]
    },
    {
        ""name"": ""child10"",
        ""parent"":""Scroller"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.RawImage"",
                ""color"": ""0.2 0.2 0.2 1.0"",
                ""sprite"": ""Assets/Content/UI/UI.Background.Tile.psd"",
            },
            {
                ""type"":""RectTransform"",
                ""anchormin"": ""0.03 0.97"",
                ""anchormax"": ""0.97 0.97"",
                ""offsetmin"": ""0 -990"",
                ""offsetmax"": ""0 -900""
            },
            {
                ""type"": ""UnityEngine.UI.Outline"",
                ""distance"": ""2 2"",
                ""color"": ""0 0 0 0.06""
            },
            {
                ""type"": ""UnityEngine.UI.Outline"",
                ""distance"": ""4 4"",
                ""color"": ""0 0 0 0.025""
            }
        ]
    },
    {
        ""parent"": ""child10"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.Text"",
                ""text"":""child10 Text"",
                ""color"": ""0.6 0.6 0.6 1.0"",
                ""fontSize"":26,
                ""align"": ""MiddleCenter""
            }
        ]
    },
    {
        ""name"": ""child11"",
        ""parent"":""Scroller"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.RawImage"",
                ""color"": ""0.2 0.2 0.2 1.0"",
                ""sprite"": ""Assets/Content/UI/UI.Background.Tile.psd"",
            },
            {
                ""type"":""RectTransform"",
                ""anchormin"": ""0.03 0.97"",
                ""anchormax"": ""0.97 0.97"",
                ""offsetmin"": ""0 -1090"",
                ""offsetmax"": ""0 -1000""
            },
            {
                ""type"": ""UnityEngine.UI.Outline"",
                ""distance"": ""2 2"",
                ""color"": ""0 0 0 0.06""
            },
            {
                ""type"": ""UnityEngine.UI.Outline"",
                ""distance"": ""4 4"",
                ""color"": ""0 0 0 0.025""
            }
        ]
    },
    {
        ""parent"": ""child11"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.Text"",
                ""text"":""child11 Text"",
                ""color"": ""0.6 0.6 0.6 1.0"",
                ""fontSize"":26,
                ""align"": ""MiddleCenter""
            }
        ]
    },
    {
        ""name"": ""child12"",
        ""parent"":""Scroller"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.RawImage"",
                ""color"": ""0.2 0.2 0.2 1.0"",
                ""sprite"": ""Assets/Content/UI/UI.Background.Tile.psd"",
            },
            {
                ""type"":""RectTransform"",
                ""anchormin"": ""0.03 0.97"",
                ""anchormax"": ""0.97 0.97"",
                ""offsetmin"": ""0 -1190"",
                ""offsetmax"": ""0 -1100""
            },
            {
                ""type"": ""UnityEngine.UI.Outline"",
                ""distance"": ""2 2"",
                ""color"": ""0 0 0 0.06""
            },
            {
                ""type"": ""UnityEngine.UI.Outline"",
                ""distance"": ""4 4"",
                ""color"": ""0 0 0 0.025""
            }
        ]
    },
    {
        ""parent"": ""child12"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.Text"",
                ""text"":""<color=#cccccc>child12</color> Text"",
                ""color"": ""0.6 0.6 0.6 1.0"",
                ""fontSize"":26,
                ""align"": ""MiddleCenter""
            }
        ]
    },
    {
        ""name"": ""child13"",
        ""parent"":""Scroller"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.RawImage"",
                ""color"": ""0.2 0.2 0.2 1.0"",
                ""sprite"": ""Assets/Content/UI/UI.Background.Tile.psd"",
            },
            {
                ""type"":""RectTransform"",
                ""anchormin"": ""0.03 0.97"",
                ""anchormax"": ""0.97 0.97"",
                ""offsetmin"": ""0 -1290"",
                ""offsetmax"": ""0 -1200""
            },
            {
                ""type"": ""UnityEngine.UI.Outline"",
                ""distance"": ""2 2"",
                ""color"": ""0 0 0 0.06""
            },
            {
                ""type"": ""UnityEngine.UI.Outline"",
                ""distance"": ""4 4"",
                ""color"": ""0 0 0 0.025""
            }
        ]
    },
    {
        ""parent"": ""child13"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.Text"",
                ""text"":""child13 Text"",
                ""color"": ""0.6 0.6 0.6 1.0"",
                ""fontSize"":26,
                ""align"": ""MiddleCenter""
            }
        ]
    },
    {
        ""name"": ""child14"",
        ""parent"":""Scroller"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.RawImage"",
                ""color"": ""0.2 0.2 0.2 1.0"",
                ""sprite"": ""Assets/Content/UI/UI.Background.Tile.psd"",
            },
            {
                ""type"":""RectTransform"",
                ""anchormin"": ""0.03 0.97"",
                ""anchormax"": ""0.97 0.97"",
                ""offsetmin"": ""0 -1390"",
                ""offsetmax"": ""0 -1300""
            },
            {
                ""type"": ""UnityEngine.UI.Outline"",
                ""distance"": ""2 2"",
                ""color"": ""0 0 0 0.06""
            },
            {
                ""type"": ""UnityEngine.UI.Outline"",
                ""distance"": ""4 4"",
                ""color"": ""0 0 0 0.025""
            }
        ]
    },
    {
        ""parent"": ""child14"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.Text"",
                ""text"":""child14 Text"",
                ""color"": ""0.6 0.6 0.6 1.0"",
                ""fontSize"":26,
                ""align"": ""MiddleCenter""
            }
        ]
    },
    {
        ""name"": ""Button88"",
        ""parent"": ""UI"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.Button"",
                ""close"":""UI"",
                ""color"": ""0.9 0.0 0.0 0.8"",
            },
            {
                ""type"":""RectTransform"",
                ""anchormin"": ""0.02 0.05"",
                ""anchormax"": ""0.2 0.12""
            }
        ]
    },
    {
        ""parent"": ""Button88"",
        ""components"":
        [
            {
                ""type"":""UnityEngine.UI.Text"",
                ""text"":""close"",
                ""fontSize"":16,
                ""align"": ""MiddleCenter""
            }
        ]
    }
]
        ";
        #endregion

        [Command("guitest")]
        private void CommandGuiTest(IPlayer player, string command, string[] args)
        {
            var bPlayer        = (BasePlayer)player.Object;
            var filledTemplate = TEMPLATE;

            CuiHelper.DestroyUi(bPlayer, "UI");
            CuiHelper.AddUi(bPlayer, filledTemplate);
        }

        [Command("guitest.close")]
        private void CommandGuiTestClose(IPlayer player, string command, string[] args)
        {
            player.Reply("Closing menu!");
            CuiHelper.DestroyUi((BasePlayer)player.Object, "UI");
        }
    }
}
