using Oxide.Core;
using Oxide.Game.Rust.Cui;
using System.Diagnostics;
using System.Text;

namespace Oxide.Plugins
{
    public class GuiService
    {
        private LeaderboardService _leaderboardService;

        public GuiService(LeaderboardService leaderboardService)
        {
            _leaderboardService = leaderboardService;
        }
        public void ActivateGui(BasePlayer player)
        {
            var watch = new Stopwatch();
            watch.Start();
            Interface.Oxide.LogDebug($"START Enabling lb ui for {player.displayName}");
            string readyGui = PrepareUi(player);

            CuiHelper.AddUi(player, readyGui);
            watch.Stop();
            Interface.Oxide.LogDebug($"END Enabling lb ui for {player.displayName} {watch.ElapsedMilliseconds}ms");
        }

        private string PrepareUi(BasePlayer player)
        {
            StringBuilder stringBuilder = new StringBuilder(STATSGUI);
            stringBuilder.Replace("%lb_close_command%", "lb_close " + player.userID);

            PopulateLeaderboard("Kills", stringBuilder);
            PopulateLeaderboard("Deaths", stringBuilder);
            PopulateLeaderboard("Damage", stringBuilder);
            PopulateLeaderboard("ScrapWon", stringBuilder);
            PopulateLeaderboard("OresFarmed", stringBuilder);
            PopulateLeaderboard("ItemsCrafted", stringBuilder);

            return stringBuilder.ToString();

        }

        private StringBuilder PopulateLeaderboard(string lb, StringBuilder stringBuilder)
        {
            var leaderboard = _leaderboardService.GetLeaderboards().Find(x => x.LeaderboardName == lb);
            int i = 1;
            foreach (var place in leaderboard.GetLeaderboard())
            {
                stringBuilder.Replace($"%{lb}{i}Name%", place.Key + ":");
                stringBuilder.Replace($"%{lb}{i}%", place.Value.ToString());
                i++;
            }
            return stringBuilder;
        }

        public void DestroyGui(BasePlayer player)
        {
            Interface.Oxide.LogDebug($"Destroying lb ui for {player.displayName}");

            CuiHelper.DestroyUi(player, "lb");
        }
        public static readonly string STATSGUI = @"[
  {
    ""name"": ""lb"",
    ""parent"": ""Hud"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.1614583 0.1759259"",
        ""anchormax"": ""0.8385417 0.824074"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_frame"",
    ""parent"": ""lb"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 0.3411765""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_close_frame"",
    ""parent"": ""lb_frame"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.9046153 0.9271429"",
        ""anchormax"": ""1.00125 1.002381"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_close_text"",
    ""parent"": ""lb_close_frame"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""Close"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_close_button"",
    ""parent"": ""lb_close_text"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Button"",
        ""command"": ""%lb_close_command%"",
        ""sprite"": """",
        ""material"": """",
        ""color"": ""0 0 0 0""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_body"",
    ""parent"": ""lb_frame"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 0.9200001"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_row1"",
    ""parent"": ""lb_body"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01153846 0.511646"",
        ""anchormax"": ""0.9884616 0.9767081"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_kills"",
    ""parent"": ""lb_row1"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""0.3333333 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_kills_title"",
    ""parent"": ""lb_kills"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.4901961 0 0.6039216 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.8361157"",
        ""anchormax"": ""0.988189 0.9833055"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_kills_title_text"",
    ""parent"": ""lb_kills_title"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""Kills"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_kills_1"",
    ""parent"": ""lb_kills"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.6722315"",
        ""anchormax"": ""0.988189 0.8194213"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_kills_1_text"",
    ""parent"": ""lb_kills_1"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%Kills1Name% %Kills1%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_kills_2"",
    ""parent"": ""lb_kills"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.5083473"",
        ""anchormax"": ""0.988189 0.655537"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_kills_2_text"",
    ""parent"": ""lb_kills_2"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%Kills2Name% %Kills2%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_kills_3"",
    ""parent"": ""lb_kills"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.3444631"",
        ""anchormax"": ""0.988189 0.4916528"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_kills_3_text"",
    ""parent"": ""lb_kills_3"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%Kills3Name% %Kills3%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_kills_4"",
    ""parent"": ""lb_kills"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.1805788"",
        ""anchormax"": ""0.988189 0.3277686"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_kills_4_text"",
    ""parent"": ""lb_kills_4"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%Kills4Name% %Kills4%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_kills_5"",
    ""parent"": ""lb_kills"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.01669461"",
        ""anchormax"": ""0.988189 0.1638843"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_kills_5_text"",
    ""parent"": ""lb_kills_5"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%Kills5Name% %Kills5%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_deaths"",
    ""parent"": ""lb_row1"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.3333333 0"",
        ""anchormax"": ""0.6666667 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_deaths_title"",
    ""parent"": ""lb_deaths"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.4901961 0 0.6039216 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.8361157"",
        ""anchormax"": ""0.988189 0.9833055"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_deaths_title_text"",
    ""parent"": ""lb_deaths_title"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""Deaths"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_deaths_1"",
    ""parent"": ""lb_deaths"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.6722315"",
        ""anchormax"": ""0.988189 0.8194213"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_deaths_1_text"",
    ""parent"": ""lb_deaths_1"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%Deaths1Name% %Deaths1%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_deaths_2"",
    ""parent"": ""lb_deaths"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.5083473"",
        ""anchormax"": ""0.988189 0.655537"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_deaths_2_text"",
    ""parent"": ""lb_deaths_2"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%Deaths2Name% %Deaths2%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_deaths_3"",
    ""parent"": ""lb_deaths"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.3444631"",
        ""anchormax"": ""0.988189 0.4916528"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_deaths_3_text"",
    ""parent"": ""lb_deaths_3"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%Deaths3Name% %Deaths3%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_deaths_4"",
    ""parent"": ""lb_deaths"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.1805788"",
        ""anchormax"": ""0.988189 0.3277686"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_deaths_4_text"",
    ""parent"": ""lb_deaths_4"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%Deaths4Name% %Deaths4%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_deaths_5"",
    ""parent"": ""lb_deaths"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.01669461"",
        ""anchormax"": ""0.988189 0.1638843"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_deaths_5_text"",
    ""parent"": ""lb_deaths_5"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%Deaths5Name% %Deaths5%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_damage"",
    ""parent"": ""lb_row1"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.6666667 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_damage_title"",
    ""parent"": ""lb_damage"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.4901961 0 0.6039216 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.8361157"",
        ""anchormax"": ""0.988189 0.9833055"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_damage_title_text"",
    ""parent"": ""lb_damage_title"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""Damage"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_damage_1"",
    ""parent"": ""lb_damage"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.6722315"",
        ""anchormax"": ""0.988189 0.8194213"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_damage_1_text"",
    ""parent"": ""lb_damage_1"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%Damage1Name% %Damage1%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_damage_2"",
    ""parent"": ""lb_damage"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.5083473"",
        ""anchormax"": ""0.988189 0.655537"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_damage_2_text"",
    ""parent"": ""lb_damage_2"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%Damage2Name% %Damage2%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_damage_3"",
    ""parent"": ""lb_damage"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.3444631"",
        ""anchormax"": ""0.988189 0.4916528"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_damage_3_text"",
    ""parent"": ""lb_damage_3"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%Damage3Name% %Damage3%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_damage_4"",
    ""parent"": ""lb_damage"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.1805788"",
        ""anchormax"": ""0.988189 0.3277686"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_damage_4_text"",
    ""parent"": ""lb_damage_4"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%Damage4Name% %Damage4%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_damage_5"",
    ""parent"": ""lb_damage"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.01669461"",
        ""anchormax"": ""0.988189 0.1638843"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_damage_5_text"",
    ""parent"": ""lb_damage_5"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%Damage5Name% %Damage5%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_row2"",
    ""parent"": ""lb_body"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01153846 0.02329195"",
        ""anchormax"": ""0.9884616 0.488354"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_scrapWon"",
    ""parent"": ""lb_row2"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.3333333 0"",
        ""anchormax"": ""0.6666667 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_scrapWon_title"",
    ""parent"": ""lb_scrapWon"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.4901961 0 0.6039216 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.8361157"",
        ""anchormax"": ""0.988189 0.9833055"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_scrapWon_title_text"",
    ""parent"": ""lb_scrapWon_title"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""ScrapWon"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_scrapWon_1"",
    ""parent"": ""lb_scrapWon"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.6722315"",
        ""anchormax"": ""0.988189 0.8194213"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_scrapWon_1_text"",
    ""parent"": ""lb_scrapWon_1"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%ScrapWon1Name% %ScrapWon1%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_scrapWon_2"",
    ""parent"": ""lb_scrapWon"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.5083473"",
        ""anchormax"": ""0.988189 0.655537"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_scrapWon_2_text"",
    ""parent"": ""lb_scrapWon_2"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%ScrapWon2Name% %ScrapWon2%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_scrapWon_3"",
    ""parent"": ""lb_scrapWon"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.3444631"",
        ""anchormax"": ""0.988189 0.4916528"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_scrapWon_3_text"",
    ""parent"": ""lb_scrapWon_3"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%ScrapWon3Name% %ScrapWon3%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_scrapWon_4"",
    ""parent"": ""lb_scrapWon"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.1805788"",
        ""anchormax"": ""0.988189 0.3277686"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_scrapWon_4_text"",
    ""parent"": ""lb_scrapWon_4"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%ScrapWon4Name% %ScrapWon4%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_scrapWon_5"",
    ""parent"": ""lb_scrapWon"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.01669461"",
        ""anchormax"": ""0.988189 0.1638843"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_scrapWon_5_text"",
    ""parent"": ""lb_scrapWon_5"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%ScrapWon5Name% %ScrapWon5%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_oresFarmed"",
    ""parent"": ""lb_row2"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.6666667 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_oresFarmed_title"",
    ""parent"": ""lb_oresFarmed"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.4901961 0 0.6039216 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.8361157"",
        ""anchormax"": ""0.988189 0.9833055"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_oresFarmed_title_text"",
    ""parent"": ""lb_oresFarmed_title"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""OresFarmed"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_oresFarmed_1"",
    ""parent"": ""lb_oresFarmed"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.6722315"",
        ""anchormax"": ""0.988189 0.8194213"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_oresFarmed_1_text"",
    ""parent"": ""lb_oresFarmed_1"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%OresFarmed1Name% %OresFarmed1%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_oresFarmed_2"",
    ""parent"": ""lb_oresFarmed"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.5083473"",
        ""anchormax"": ""0.988189 0.655537"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_oresFarmed_2_text"",
    ""parent"": ""lb_oresFarmed_2"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%OresFarmed2Name% %OresFarmed2%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_oresFarmed_3"",
    ""parent"": ""lb_oresFarmed"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.3444631"",
        ""anchormax"": ""0.988189 0.4916528"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_oresFarmed_3_text"",
    ""parent"": ""lb_oresFarmed_3"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%OresFarmed3Name% %OresFarmed3%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_oresFarmed_4"",
    ""parent"": ""lb_oresFarmed"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.1805788"",
        ""anchormax"": ""0.988189 0.3277686"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_oresFarmed_4_text"",
    ""parent"": ""lb_oresFarmed_4"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%OresFarmed4Name% %OresFarmed4%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_oresFarmed_5"",
    ""parent"": ""lb_oresFarmed"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.01669461"",
        ""anchormax"": ""0.988189 0.1638843"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_oresFarmed_5_text"",
    ""parent"": ""lb_oresFarmed_5"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%OresFarmed5Name% %OresFarmed5%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_itemsCrafted"",
    ""parent"": ""lb_row2"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""0.3333333 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_itemsCrafted_title"",
    ""parent"": ""lb_itemsCrafted"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.4901961 0 0.6039216 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.8361157"",
        ""anchormax"": ""0.988189 0.9833055"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_itemsCrafted_title_text"",
    ""parent"": ""lb_itemsCrafted_title"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""ItemsCrafted"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_itemsCrafted_1"",
    ""parent"": ""lb_itemsCrafted"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.6722315"",
        ""anchormax"": ""0.988189 0.8194213"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_itemsCrafted_1_text"",
    ""parent"": ""lb_itemsCrafted_1"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%ItemsCrafted1Name% %ItemsCrafted1%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_itemsCrafted_2"",
    ""parent"": ""lb_itemsCrafted"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.5083473"",
        ""anchormax"": ""0.988189 0.655537"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_itemsCrafted_2_text"",
    ""parent"": ""lb_itemsCrafted_2"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%ItemsCrafted2Name% %ItemsCrafted2%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_itemsCrafted_3"",
    ""parent"": ""lb_itemsCrafted"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.3444631"",
        ""anchormax"": ""0.988189 0.4916528"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_itemsCrafted_3_text"",
    ""parent"": ""lb_itemsCrafted_3"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%ItemsCrafted3Name% %ItemsCrafted3%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_itemsCrafted_4"",
    ""parent"": ""lb_itemsCrafted"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.1805788"",
        ""anchormax"": ""0.988189 0.3277686"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_itemsCrafted_4_text"",
    ""parent"": ""lb_itemsCrafted_4"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%ItemsCrafted4Name% %ItemsCrafted4%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_itemsCrafted_5"",
    ""parent"": ""lb_itemsCrafted"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01181102 0.01669461"",
        ""anchormax"": ""0.988189 0.1638843"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""lb_itemsCrafted_5_text"",
    ""parent"": ""lb_itemsCrafted_5"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%ItemsCrafted5Name% %ItemsCrafted5%"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""CuiElement"",
    ""parent"": ""Overlay"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.05208334 0.09259259"",
        ""anchormax"": ""0.1041667 0.1851852"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  }
]";
    }
}
