using System.Collections.Generic;

namespace Oxide.Plugins
{
    public partial class GatherRewards
    {
        private class PluginConfig
        {
            #region Properties and Indexers

            public Dictionary<string, float> Rewards { get; set; }
            public PluginSettings Settings { get; set; }

            #endregion
        }
    }
}