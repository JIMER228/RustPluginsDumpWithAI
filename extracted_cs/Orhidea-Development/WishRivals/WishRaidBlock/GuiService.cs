using Oxide.Core;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    public class GuiService
    {
        public static readonly string RAIDBLOCKENABLEDGUI = @"[
  {
    ""name"": ""RaidLockBackground"",
    ""parent"": ""Overlay"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Image"",
        ""sprite"": """",
        ""material"": """",
        ""color"": ""1 0 0 0.5756225""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.0140625 0.4407408"",
        ""anchormax"": ""0.1479166 0.5083333"",
        ""offsetmax"": ""0 0""
      }
    ]
  },
  {
    ""name"": ""RaidlockText"",
    ""parent"": ""RaidLockBackground"",
    ""components"": [
      {
        ""type"": ""UnityEngine.UI.Text"",
        ""text"": ""Raiding is disabled"",
        ""align"": ""MiddleCenter""
      },
      {
        ""type"": ""RectTransform"",
        ""anchormin"": ""0.003890991 -5.960464E-08"",
        ""anchormax"": ""1.003891 1.041097"",
        ""offsetmax"": ""0 0""
      }
    ]
  }
]";
        static List<BasePlayer> _players = new List<BasePlayer>();
        
        public void ActivateGui()
        {
            Interface.Oxide.LogDebug($"Active player with ui enabled {_players.Count}");

            _players = _players.Where(player => IsOnline(player)).ToList();
            Interface.Oxide.LogDebug($"Active player with ui enabled {_players.Count}");

            var activePlayers = BasePlayer.activePlayerList;

            foreach (var player in activePlayers)
            {
                if (_players.Any(x => x.userID == player.userID))
                {
                    continue;
                }

                CuiHelper.AddUi(player, RAIDBLOCKENABLEDGUI);
                _players.Add(player);
            };
        }

        private static bool IsOnline(BasePlayer player)
        {
            return BasePlayer.activePlayerList.Any(x => x.userID == player.userID);
        }

        public void DestroyGui()
        {
            foreach (var player in _players)
            {
                if (IsOnline(player))
                {
                    CuiHelper.DestroyUi(player, "RaidLockBackground");
                }
            };
            _players = new List<BasePlayer>();
        }
    }
}
