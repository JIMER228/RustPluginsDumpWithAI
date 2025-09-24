// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using Oxide.Core;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using WishInfrastructure;

namespace Oxide.Plugins
{
    public class GuiService
    {
        public static readonly string STATSGUI = @"[
  {
    ""name"": ""stats"",
    ""parent"": ""Hud"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.1614583 0.3055556"",
        ""anchormax"": ""0.8385417 0.6944444"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""stats_frame"",
    ""parent"": ""stats"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 0.345098""
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
    ""name"": ""stats_close_frame"",
    ""parent"": ""stats_frame"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.9292307 0.8880954"",
        ""anchormax"": ""1.00125 1.002381"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""stats_close_text"",
    ""parent"": ""stats_close_frame"",
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
    ""name"": ""stats_close_button"",
    ""parent"": ""stats_close_text"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Button"",
        ""command"": ""%stats_close_command%"",
        ""sprite"": """",
        ""material"": """",
        ""color"": ""0 0 0 0""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""-0.007632971 0.04255176"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""stats_frame_2"",
    ""parent"": ""stats_frame"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""-0.001250005 0.00238095"",
        ""anchormax"": ""0.9999999 0.919048"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""stats_combat"",
    ""parent"": ""stats_frame_2"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.6 0.6 0.6 0.3999993""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.01152406 0.03896103"",
        ""anchormax"": ""0.329492 0.9610389"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""stats_combat_title"",
    ""parent"": ""stats_combat"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.5411765 0.1372549 0.1372549 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.03624282 0.8403756"",
        ""anchormax"": ""0.9637572 0.9577465"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""stats_combat_textstats_combat_text"",
    ""parent"": ""stats_combat_title"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""Combat"",
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
    ""name"": ""stats_kills"",
    ""parent"": ""stats_combat"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.03624282 0.6807512"",
        ""anchormax"": ""0.9637572 0.798122"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""kills_bg"",
    ""parent"": ""stats_kills"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""0.6 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""kills_text"",
    ""parent"": ""kills_bg"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""Total Kills"",
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
    ""name"": ""kills_amount_bg"",
    ""parent"": ""stats_kills"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.2470588 0.454902 0.06666667 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.6 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""kills_amount"",
    ""parent"": ""kills_amount_bg"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%Kills%"",
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
    ""name"": ""stats_hits"",
    ""parent"": ""stats_combat"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.03624282 0.5211267"",
        ""anchormax"": ""0.9637572 0.6384977"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""hits_bg"",
    ""parent"": ""stats_hits"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""0.6 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""hits_text"",
    ""parent"": ""hits_bg"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""Total Hits"",
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
    ""name"": ""hits_amount_bg"",
    ""parent"": ""stats_hits"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.2470588 0.454902 0.06666667 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.6 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""hits_amount"",
    ""parent"": ""hits_amount_bg"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%Hits%"",
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
    ""name"": ""stats_deaths"",
    ""parent"": ""stats_combat"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.03624282 0.3615023"",
        ""anchormax"": ""0.9637572 0.4788733"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""deaths_bg"",
    ""parent"": ""stats_deaths"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""0.6 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""deaths_text"",
    ""parent"": ""deaths_bg"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""Total Deaths"",
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
    ""name"": ""deaths_amount_bg"",
    ""parent"": ""stats_deaths"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.5411765 0.1333333 0.1333333 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.6 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""deaths_amount"",
    ""parent"": ""deaths_amount_bg"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%Deaths%"",
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
    ""name"": ""stats_damage"",
    ""parent"": ""stats_combat"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.03624282 0.201878"",
        ""anchormax"": ""0.9637572 0.3192489"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""damage_bg"",
    ""parent"": ""stats_damage"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""0.6 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""damage_text"",
    ""parent"": ""damage_bg"",
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
    ""name"": ""damage_amount_bg"",
    ""parent"": ""stats_damage"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.2470588 0.454902 0.06666667 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.6 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""damage_amount"",
    ""parent"": ""damage_amount_bg"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%Damage%"",
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
    ""name"": ""stats_headshots"",
    ""parent"": ""stats_combat"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.03624282 0.04225355"",
        ""anchormax"": ""0.9637572 0.1596245"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""headshots_bg"",
    ""parent"": ""stats_headshots"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""0.6 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""headshots_text"",
    ""parent"": ""headshots_bg"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""Headshots"",
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
    ""name"": ""headshots_amount_bg"",
    ""parent"": ""stats_headshots"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.2470588 0.454902 0.06666667 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.6 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""headshots_amount"",
    ""parent"": ""headshots_amount_bg"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%Headshots%"",
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
    ""name"": ""stats_farm"",
    ""parent"": ""stats_frame_2"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.6 0.6 0.6 0.3960784""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.341016 0.03896103"",
        ""anchormax"": ""0.6589839 0.9610389"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""farm_title"",
    ""parent"": ""stats_farm"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.2470588 0.454902 0.06666667 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.03624282 0.8403756"",
        ""anchormax"": ""0.9637572 0.9577465"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""farm_text"",
    ""parent"": ""farm_title"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""Farm"",
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
    ""name"": ""stats_ItemsCrafted"",
    ""parent"": ""stats_farm"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.03624282 0.6807512"",
        ""anchormax"": ""0.9637572 0.798122"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""ItemsCrafted_bg"",
    ""parent"": ""stats_ItemsCrafted"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""0.6 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""ItemsCrafted_text"",
    ""parent"": ""ItemsCrafted_bg"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""Items Crafted"",
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
    ""name"": ""ItemsCrafted_amount_bg"",
    ""parent"": ""stats_ItemsCrafted"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.2470588 0.454902 0.06666667 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.6 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""ItemsCrafted_amount"",
    ""parent"": ""ItemsCrafted_amount_bg"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%ItemsCrafted%"",
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
    ""name"": ""stats_TreesFarmed"",
    ""parent"": ""stats_farm"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.03624282 0.5211267"",
        ""anchormax"": ""0.9637572 0.6384977"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""TreesFarmed_bg"",
    ""parent"": ""stats_TreesFarmed"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""0.6 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""TreesFarmed_text"",
    ""parent"": ""TreesFarmed_bg"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""Trees Farmed"",
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
    ""name"": ""TreesFarmed_amount_bg"",
    ""parent"": ""stats_TreesFarmed"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.2470588 0.454902 0.06666667 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.6 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""TreesFarmed_amount"",
    ""parent"": ""TreesFarmed_amount_bg"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%TreesFarmed%"",
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
    ""name"": ""stats_OresFarmed"",
    ""parent"": ""stats_farm"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.03624282 0.3615023"",
        ""anchormax"": ""0.9637572 0.4788733"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""OresFarmed_bg"",
    ""parent"": ""stats_OresFarmed"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""0.6 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""OresFarmed_text"",
    ""parent"": ""OresFarmed_bg"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""Ores Farmed"",
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
    ""name"": ""OresFarmed_amount_bg"",
    ""parent"": ""stats_OresFarmed"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.2470588 0.454902 0.06666667 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.6 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""OresFarmed_amount"",
    ""parent"": ""OresFarmed_amount_bg"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%OresFarmed%"",
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
    ""name"": ""stats_NpcKills"",
    ""parent"": ""stats_farm"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.03624282 0.201878"",
        ""anchormax"": ""0.9637572 0.3192489"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""NpcKills_bg"",
    ""parent"": ""stats_NpcKills"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""0.6 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""NpcKills_text"",
    ""parent"": ""NpcKills_bg"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""Npc Kills"",
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
    ""name"": ""NpcKills_amount_bg"",
    ""parent"": ""stats_NpcKills"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.2470588 0.454902 0.06666667 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.6 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""NpcKills_amount"",
    ""parent"": ""NpcKills_amount_bg"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%NpcKills%"",
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
    ""name"": ""stats_AnimalKills"",
    ""parent"": ""stats_farm"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.03624282 0.04225355"",
        ""anchormax"": ""0.9637572 0.1596245"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""AnimalKills_bg"",
    ""parent"": ""stats_AnimalKills"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""0.6 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""AnimalKills_text"",
    ""parent"": ""AnimalKills_bg"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""Animal Kills"",
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
    ""name"": ""AnimalKills_amount_bg"",
    ""parent"": ""stats_AnimalKills"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.2470588 0.454902 0.06666667 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.6 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""AnimalKills_amount"",
    ""parent"": ""AnimalKills_amount_bg"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%AnimalKills%"",
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
    ""name"": ""stats_other"",
    ""parent"": ""stats_frame_2"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.6 0.6 0.6 0.3921569""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.670508 0.03896103"",
        ""anchormax"": ""0.9884759 0.9610389"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""other_title"",
    ""parent"": ""stats_other"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.5215687 0 1 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.03624283 0.8403756"",
        ""anchormax"": ""0.9637572 0.9577465"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""other_title_text"",
    ""parent"": ""other_title"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""Other"",
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
    ""name"": ""stats_Shots"",
    ""parent"": ""stats_other"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.03624283 0.6807512"",
        ""anchormax"": ""0.9637572 0.798122"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""Shots_bg"",
    ""parent"": ""stats_Shots"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""0.6 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""Shots_text"",
    ""parent"": ""Shots_bg"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""Shots"",
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
    ""name"": ""Shots_amount_bg"",
    ""parent"": ""stats_Shots"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.5411765 0.1372549 0.1372549 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.6 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""Shots_amount"",
    ""parent"": ""Shots_amount_bg"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%Shots%"",
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
    ""name"": ""stats_ScrapWon"",
    ""parent"": ""stats_other"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.03624283 0.5211267"",
        ""anchormax"": ""0.9637572 0.6384977"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""ScrapWon_bg"",
    ""parent"": ""stats_ScrapWon"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""0.6 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""ScrapWon_text"",
    ""parent"": ""ScrapWon_bg"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""Scrap Won"",
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
    ""name"": ""ScrapWon_amount_bg"",
    ""parent"": ""stats_ScrapWon"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.5215687 0 1 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.6 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""ScrapWon_amount"",
    ""parent"": ""ScrapWon_amount_bg"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%ScrapWon%"",
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
    ""name"": ""stats_ScrapLost"",
    ""parent"": ""stats_other"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.03624283 0.3615023"",
        ""anchormax"": ""0.9637572 0.4788733"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""ScrapLost_bg"",
    ""parent"": ""stats_ScrapLost"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0 0 0 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0 0"",
        ""anchormax"": ""0.6 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""ScrapLost_text"",
    ""parent"": ""ScrapLost_bg"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""Scrap Lost"",
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
    ""name"": ""ScrapLost_amount_bg"",
    ""parent"": ""stats_ScrapLost"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.5411765 0.1372549 0.1372549 1""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.6 0"",
        ""anchormax"": ""1 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""ScrapLost_amount"",
    ""parent"": ""ScrapLost_amount_bg"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%ScrapLost%"",
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
    ""name"": ""other_p1"",
    ""parent"": ""stats_other"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.03624283 0.201878"",
        ""anchormax"": ""0.9637572 0.3192489"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""other_p2"",
    ""parent"": ""stats_other"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.03624283 0.04225355"",
        ""anchormax"": ""0.9637572 0.1596245"",
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
        private readonly DatabaseClient _database;

        public GuiService(DatabaseClient database)
        {
            _database = database;
        }

        public void ActivateGui(BasePlayer player)
        {

            Interface.Oxide.LogDebug($"Enabling statisctics ui for {player.displayName}");
            string readyGui = PrepareUi(player);
            CuiHelper.AddUi(player, readyGui);
        }

        private string PrepareUi(BasePlayer player)
        {
            string gui = STATSGUI.Replace("%stats_close_command%", "stats_close " + player.userID);

            gui = ReplaceIntPlaceholder(player, gui, "Kills");
            gui = ReplaceIntPlaceholder(player, gui, "Deaths");
            gui = ReplaceIntPlaceholder(player, gui, "Hits");
            gui = ReplaceIntPlaceholder(player, gui, "Damage");
            gui = ReplaceIntPlaceholder(player, gui, "Headshots");

            gui = ReplaceIntPlaceholder(player, gui, "ItemsCrafted");
            gui = ReplaceIntPlaceholder(player, gui, "TreesFarmed");
            gui = ReplaceIntPlaceholder(player, gui, "OresFarmed");
            gui = ReplaceIntPlaceholder(player, gui, "NpcKills");
            gui = ReplaceIntPlaceholder(player, gui, "AnimalKills");

            gui = ReplaceIntPlaceholder(player, gui, "ScrapLost");
            gui = ReplaceIntPlaceholder(player, gui, "ScrapWon");
            gui = ReplaceIntPlaceholder(player, gui, "Shots");


            return gui;

        }

        private string ReplaceIntPlaceholder(BasePlayer player, string gui, string statistic)
        {
            return gui.Replace($"%{statistic}%", _database.GetPlayerDataRaw<int>(player.userID.ToString(), statistic).ToString());
        }

        public void DestroyGui(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "stats_frame");
        }
    }
}
