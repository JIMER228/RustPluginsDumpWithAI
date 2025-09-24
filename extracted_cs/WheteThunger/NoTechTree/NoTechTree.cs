using System.Collections.Generic;
using static TechTreeData;

namespace Oxide.Plugins
{
    [Info("No Tech Tree", "WhiteThunder", "0.1.0")]
    [Description("Disallows players from opening the tech tree.")]
    internal class NoTechTree : CovalencePlugin
    {
        // Disallow opening the tech tree.
        private bool? CanLootEntity(BasePlayer player, Workbench workbench)
        {
            player.ChatMessage(lang.GetMessage("TechTreeDisabled", this, player.UserIDString));
            return false;
        }

        // Disallow unlocking nodes in case cheaters send direct message to the server.
        private bool? CanUnlockTechTreeNode(BasePlayer player, NodeInstance node, TechTreeData techTree)
        {
            return false;
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TechTreeDisabled"] = "The tech tree is disabled on this server.",
            }, this, "en");
        }
    }
}
