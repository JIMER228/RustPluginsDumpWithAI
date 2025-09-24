using Oxide.Core;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WishInfrastructure;

namespace Oxide.Plugins
{
    public class ClanGuiService
    {
        static List<BasePlayer> _players = new List<BasePlayer>();
        private readonly ConfigSetup _configSetup;
        private readonly DatabaseClient _databaseClient;

        public ClanGuiService(ConfigSetup configSetup, DatabaseClient databaseClient)
        {
            _configSetup = configSetup;
            _databaseClient = databaseClient;
        }

        internal void UpdateClanUI()
        {

            //foreach (var player in _players)
            //{
            //    if (IsOnline(player))
            //    {
            //        CuiHelper.DestroyUi(player, "mainclan");
            //    }
            //}
            string generatedGui = GenerateClanGui();

            var activePlayers = BasePlayer.activePlayerList;

            foreach (var player in activePlayers)
            {
                CuiHelper.DestroyUi(player, "mainclan");

                CuiHelper.AddUi(player, generatedGui);
                _players.Add(player);
            };
        }

        private string GenerateClanGui()
        {
            var clans = _configSetup.ConfigFile.lb_clans;
            int i = 1;
            StringBuilder stringBuilder = new StringBuilder(CLANGUI);
            foreach (var clan in clans)
            {
                var clanId = RelationshipManager.ServerInstance.teams.FirstOrDefault(x => x.Value.teamName == clan.Id).Value?.teamID;
                if (clanId == null)
                {
                    Interface.Oxide.LogDebug($"Could not find clan {clanId}");
                    continue;
                }
                Interface.Oxide.LogDebug($"Found clan {clanId}");

                stringBuilder = stringBuilder.Replace($"%team_{i}%", clan.Name + ":");
                stringBuilder = stringBuilder.Replace($"%team_{i}kills%", _databaseClient.GetClanDataRaw<int>(clanId.ToString(), "Kills").ToString());

                i++;
            }
            return stringBuilder.ToString();
        }

        private static bool IsOnline(BasePlayer player)
        {
            return BasePlayer.activePlayerList.Any(x => x.userID == player.userID);
        }
        public static readonly string CLANGUI = @"[
  {
    ""name"": ""mainclan"",
    ""parent"": ""Hud"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.000520848 0.8703703"",
        ""anchormax"": ""0.23125 1"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""main_2"",
    ""parent"": ""mainclan"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.002257336 0.5035715"",
        ""anchormax"": ""0.9977427 0.9928572"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""team_1"",
    ""parent"": ""main_2"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.03921569 0.03921569 0.03921569 0.7137255""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.004535148 0.02919707"",
        ""anchormax"": ""0.4977324 0.9708029"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""team_1_text"",
    ""parent"": ""team_1"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%team_1% %team_1kills%"",
        ""fontSize"": 12,
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
    ""name"": ""team_2"",
    ""parent"": ""main_2"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.03921569 0.03921569 0.03921569 0.7137255""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.5022676 0.02919707"",
        ""anchormax"": ""0.9954649 0.9708029"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""team_2_text"",
    ""parent"": ""team_2"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%team_2% %team_2kills%"",
        ""fontSize"": 12,
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
    ""name"": ""main_1"",
    ""parent"": ""mainclan"",
    ""components"": [
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.002257336 0.007142901"",
        ""anchormax"": ""0.9977427 0.4964286"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""team_3"",
    ""parent"": ""main_1"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.03921569 0.03921569 0.03921569 0.7137255""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.004535148 0.02919707"",
        ""anchormax"": ""0.4977324 0.9708029"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""team_3_text"",
    ""parent"": ""team_3"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%team_3% %team_3kills%"",
        ""fontSize"": 12,
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
    ""name"": ""team_4"",
    ""parent"": ""main_1"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""material"": """",
        ""color"": ""0.03921569 0.03921569 0.03921569 0.7137255""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.5022676 0.02919707"",
        ""anchormax"": ""0.9954649 0.9708029"",
        ""offsetmin"": ""0 0"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""team_4_text"",
    ""parent"": ""team_4"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""%team_4% %team_4kills%"",
        ""fontSize"": 12,
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
  }
]";
    }
}