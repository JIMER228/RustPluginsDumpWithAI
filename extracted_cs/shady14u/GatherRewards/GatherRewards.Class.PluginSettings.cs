#region Using Statements

using System.Collections.Generic;

#endregion

namespace Oxide.Plugins
{
    //Define:FileOrder=7
    public partial class GatherRewards
    {
        private class PluginSettings
        {
            public int UINotifyMessageType = 1;
            public bool AddMissingRewards { get; set; }

            public bool AwardOnlyOnFullHarvest { get; set; }
            public float ChainsawModifier { get; set; } = .25f;
            public string ChatEditCommand { get; set; }
            public string ConsoleEditCommand { get; set; }
            public string EditPermission { get; set; }

            public Dictionary<string, object> GroupModifiers { get; set; } = new Dictionary<string, object>
            {
                {"gatherrewards.vip1", 4.0},
                {"gatherrewards.vip2", 3.0},
                {"gatherrewards.vip3", 2.0}
            };

            public float JackHammerModifier { get; set; } = .25f;
            public string PluginPrefix { get; set; }
            public bool ShowMessagesOnGather { get; set; }
            public bool ShowMessagesOnKill { get; set; }
            public bool UseEconomics { get; set; }
            public bool UseServerRewards { get; set; }
            public bool UseUINotify { get; set; }
            public string Version { get; set; }
        }
    }
}