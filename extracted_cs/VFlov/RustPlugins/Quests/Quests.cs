using System.Linq;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;
using System.Runtime.Remoting.Messaging;
using System.Collections.Generic;
using Oxide.Core.Configuration;
using Oxide.Core;
using System.Runtime.InteropServices;
using System.Runtime;

namespace Oxide.Plugins
{
    [Info("Quests", "VSP", "0.0.0")]
    public class Quests : RustPlugin
    {
        DynamicConfigFile QuestData;
        DynamicConfigFile PlayerData;

        /*
        [ChatCommand("wtf")]
        private void MyCommand(BasePlayer player, string command, string[] args)
        {
            player.ChatMessage("wtf");
        }
        */
        public Quests()
        {
            LoadQuestsData();
        }

        void LoadQuestsData()
        {
            QuestData = Interface.Oxide.DataFileSystem.GetDatafile("Quests/Quests");
            QuestData.WriteObject<Quest>(new Quest(
                   "[TestQuest]",
                   QuestType.Kill,
                   "TestQuest",
                   "QuestForTest. Kill 2 boards",
                   ("boar", 2),
                   ("Scrap", 50),
                   QuestRequirementType.None
                   ));
        }
        void LoadPlayerData()
        {
            PlayerData = Interface.Oxide.DataFileSystem.GetDatafile("Quests/PlayerData");
        }
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (info == null) return;
            if (entity == null) return;
            if (info.InitiatorPlayer == null) return;

        }
        object OnPlayerSpawn(BasePlayer player)
        {
            if (player.userID)
        }
    }
}