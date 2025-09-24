using System.Threading;
namespace Rust.Plugins 
{
    [Info("HealthBar", "kurushimu", "0.1.1")]
    class HealthBar : RustPlugin
    {
        [ChatCommand("testgui")]
        void cmdChatTestGui(BasePlayer player)
        {;
            string json = CUI_MAIN
            .Replace("{progress}", "0.5")
            .Replace("{text}", "50")

            CuiHelper.AddUi(player, json);
            Timer.Once (10f, TimerCallback)
        }
        string CUI_MAIN = @"
        [;
  {
    ""name"": ""hpbar_background"",
    ""parent"": ""Overlay"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""color"": ""0.5254902 0.5254902 0.5254902 0.2980392""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.5 0"",
        ""anchormax"": ""0.5 0"",
        ""offsetmin"": ""-106 86"",
        ""offsetmax"": ""86 112""
      }
    ]
  },
  {
    ""name"": ""hpbar_img"",
    ""parent"": ""hpbar_background"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""sprite"": ""assets/icons/health.png"",
        ""color"": ""1 1 1 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""0 1"",
        ""offsetmin"": ""4 4"",
        ""offsetmax"": ""22 -4""
      }
    ]
  },
  {
    ""name"": ""hpbar_wrapper"",
    ""parent"": ""hpbar_background"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""color"": ""1 0.8321342 0.8321342 0.5411765""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""25 3"",
        ""offsetmax"": ""-3 -3""
      }
    ]
  },
  {
    ""name"": ""hpbar_progress"",
    ""parent"": ""hpbar_wrapper"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""sprite"": ""assets/content/ui/ui.background.tile.psd"",
        ""color"": ""0.141806 0.7097909 0.04659747 0.5450981"",
        ""imagetype"": ""Tiled""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""{progress} 1"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""hpbar_text"",
    ""parent"": ""hpbar_progress"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""{text}"",
        ""fontSize"": 15,
        ""font"": ""robotocondensed-bold.ttf"",
        ""align"": ""MiddleLeft""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""2 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  }
]
        ";
    }
}