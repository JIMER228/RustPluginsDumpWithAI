using System.Linq;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;
using System.Runtime.Remoting.Messaging;
using System.Collections.Generic;
using Oxide.Core.Configuration;
using Oxide.Core;
using System.Runtime.InteropServices;
using System.Runtime;
using CompanionServer.Handlers;
using ConVar;
using static Oxide.Plugins.Quests2;
using Oxide.Game.Rust.Libraries;

namespace Oxide.Plugins
{
    [Info("Quests", "VSP", "0.0.0")]
    class Quests2 : RustPlugin
    {
        StoredQuests QuestData;
        string QuestDataPath = "Quests/QuestData";
        StoredPlayerProgress PlayerData;
        string PlayerDataPath = "Quests/PlayerData";

        /*
        [ChatCommand("wtf")]
        private void MyCommand(BasePlayer player, string command, string[] args)
        {
            player.ChatMessage("wtf");
        }
        */
        public Quests2()
        {
            LoadQuestsData();
            LoadPlayerData();
        }

        void LoadQuestsData()
        {
            Puts("1");
            QuestData = Interface.Oxide.DataFileSystem.ReadObject<StoredQuests>(QuestDataPath);
            Puts("2");

            if (QuestData.Quests == null) { QuestData.Quests = new Dictionary<string, Quest>(); }
            Puts("3");
            var newQuest = new Quest(
                   QuestType.Kill,
                   "TestQuest",
                   "QuestForTest. Kill 2 boards",
                   ("boar", 2),
                   ("Scrap", 50),
                   QuestRequirementType.None
                   );
            Puts("4");
            if (!QuestData.Quests.ContainsKey("[TestQuest]"))
                QuestData.Quests.Add("[TestQuest]", newQuest);
            Puts("5");
            SaveQuestsData();
        }
        void LoadPlayerData()
        {
            PlayerData = Interface.Oxide.DataFileSystem.ReadObject<StoredPlayerProgress>(PlayerDataPath);
        }
        void SaveQuestsData()
        { Interface.Oxide.DataFileSystem.WriteObject(QuestDataPath, QuestData); }
        void SavePlayerData()
        { Interface.Oxide.DataFileSystem.WriteObject(PlayerDataPath, PlayerData); }




        //PATCHS ===============================
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (info == null) return;
            if (entity == null) return;
            if (info.InitiatorPlayer == null) return;
            info.InitiatorPlayer.ChatMessage("Убит: " + entity.ShortPrefabName);
            ulong playerId = info.InitiatorPlayer.userID;
            Quest quest = FindActiveQuestByPlayerId(playerId);
            if (quest == null) return;
            if (quest.QuestType == QuestType.Kill)
            {
                info.InitiatorPlayer.ChatMessage(quest.QuestTargetPrefab.Item1 + " && " + entity.ShortPrefabName);
                info.InitiatorPlayer.ChatMessage(entity.ShortPrefabName.Contains(quest.QuestTargetPrefab.Item1).ToString());
                if (entity.ShortPrefabName.Contains(quest.QuestTargetPrefab.Item1))
                {
                    info.InitiatorPlayer.ChatMessage("Quest complited: "+CheckQuestComplite(playerId).ToString());
                    if (CheckQuestComplite(playerId))
                        QuestComplited(playerId);
                    else
                        IncQuestProgress(playerId);
                }
            }
            //info.InitiatorPlayer.ChatMessage("Счет: " + PlayerData.Progress[playerId].QuestProgressCount + " из " + FindActiveQuestByPlayerId(playerId).QuestTargetPrefab.Item2);
            
            return;
        }

        object OnPlayerSpawn(BasePlayer player)
        {
            if (player == null)
                return null;
            if (!PlayerData.Progress.ContainsKey(player.userID))
                PlayerData.Progress.Add(player.userID, new PlayerProgress(null, null, 0));
            SavePlayerData();
            return null;

        }
        object OnGrowableGather(GrowableEntity plant, BasePlayer player)
        {
            if (plant == null) return null;
            if (player == null) return null;
            ulong playerId = player.userID;
            Quest quest = FindActiveQuestByPlayerId(playerId);
            if (quest == null) return null;
            if (quest.QuestType == QuestType.Gather)
            {
                player.ChatMessage(quest.QuestTargetPrefab.Item1 + "&&" + plant.ShortPrefabName);
                if (plant.ShortPrefabName.Contains(quest.QuestTargetPrefab.Item1))
                {
                    if (CheckQuestComplite(playerId))
                        QuestComplited(playerId);
                    else
                        IncQuestProgress(playerId);
                }
            }
            return null;
        }
        void OnLootItem(PlayerLoot playerLoot, Item item)
        {
            Puts("OnLootItem works!");
        }
        void OnItemDropped(Item item, BaseEntity entity)
        {
            BasePlayer.FindByID(76561198271235631).ChatMessage(item.info.name);
        }

        [ChatCommand("addme")]
        private void Addme(BasePlayer player, string command, string[] args)
        {
            PlayerData.Progress.Add(player.userID, new PlayerProgress(new List<string>(), "[TestQuest]", 0));
            player.ChatMessage("wtf");
        }
        [ChatCommand("clearquest")]
        private void ClearQuest(BasePlayer player, string command, string[] args)
        {
            if (player == null)
                return;
            Quest quest = FindActiveQuestByPlayerId(player.userID);
            ClearActiveQuest(player.userID);
            player.ChatMessage("Квест " + quest.Name + "Очищен");
        }
        [ChatCommand("addquest")]
        private void AddQuest(BasePlayer player, string command, string[] args)
        {
            if (player == null)
                return;
            if (args.Length == 0)
            {
                player.ChatMessage("Укажите ид квеста");
            }
            Quest quest = FindQuestById(args[0]);
            if (quest == null)
            {
                player.ChatMessage("Квест " + args[0] + " не найден");
                return;
            }
            PlayerProgress progress = GetPlayerProgress(player.userID);
            progress.QuestProcessId = FindQuestIdByQuest(quest);
            progress.QuestProgressCount = 0;
            player.ChatMessage("Квест " + args[0] + " Добавлен");
        }
        [ChatCommand("qinfo")]
        private void QuestInfo(BasePlayer player, string command, string[] args)
        {
            if (player == null)
                return;
            Quest quest = FindActiveQuestByPlayerId(player.userID);
            PlayerProgress progress = GetPlayerProgress(player.userID);
            player.ChatMessage(quest.Name +"\n"+ quest.Description +"\n"+"Цель: "+quest.QuestTargetPrefab.Item1 +" " +progress.QuestProgressCount +" из "+ quest.QuestTargetPrefab.Item2 +"\n"+quest.QuestRewardPrefab+"\n"+quest.QuestRequirement);
        }
        







        //ADITIONS ===============================================
        Quest FindQuestById(string questId)
        {
            Quest quest;
            if (QuestData.Quests.TryGetValue(questId, out quest))
                return quest;
            return null;
        }
        Quest FindActiveQuestByPlayerId(ulong playerId)
        {
            Quest quest;
            if (QuestData.Quests.TryGetValue(PlayerData.Progress[playerId].QuestProcessId, out quest))
                return quest;
            return null;
        }
        string FindQuestIdByQuest(Quest quest)
        {
            return QuestData.Quests.FirstOrDefault(x => x.Value == quest).Key;
        }
        PlayerProgress GetPlayerProgress(ulong playerId)
        {
            PlayerProgress progress;
            if (PlayerData.Progress.TryGetValue(playerId, out progress))
                return progress;
            return null;
        }
        void IncQuestProgress(ulong playerId)
        {
            Puts("inc");
            PlayerProgress progress;
            if (PlayerData.Progress.TryGetValue(playerId, out progress))
                progress.QuestProgressCount++;
        }
        int GetQuestProgressCount(ulong playerId)
        {
            PlayerProgress progress;
            if (!PlayerData.Progress.TryGetValue(playerId, out progress))
            {
                return -1;
            }
            return FindQuestById(progress.QuestProcessId).QuestTargetPrefab.Item2;
        }
        bool CheckQuestComplite(ulong playerId)
        {
            PlayerProgress progress;
            if (!PlayerData.Progress.TryGetValue(playerId, out progress))
                return false;
            if (GetQuestProgressCount(playerId) <= progress.QuestProgressCount)
                return true;
            return false;
        }
        void QuestComplited(ulong playerId)
        {
            PlayerProgress progress;
            if (!PlayerData.Progress.TryGetValue(playerId, out progress))
            {
                Puts("QuestComplited error " + playerId + progress.QuestProcessId);
                return;
            }
            Quest quest = FindActiveQuestByPlayerId(playerId);
            if (quest == null)
            {
                Puts("Quest not found " + playerId);
            }
            Puts("1");
            Puts(progress.QuestProcessId);
            Puts(progress.QuestProgressCount.ToString());
            Puts("Кол-во квестов " + progress.QuestComplited.ToString());
            Puts(progress.QuestComplited.ToString());
            progress.QuestComplited.Add(progress.QuestProcessId);
            Puts("3");
            ClearActiveQuest(playerId);
            Puts("4");
            Puts(playerId.ToString());
            RewardGive(playerId, quest.QuestRewardPrefab.Item1,quest.QuestRewardPrefab.Item2);
            Puts("5");
        }
        void RewardGive(ulong playerId, string prefabName, int count)
        {
            BasePlayer player = BasePlayer.FindByID(playerId);
            Puts(ItemManager.CreateByName(prefabName).info.name + " " + prefabName);
            player.GiveItem(ItemManager.CreateByName(prefabName,count));
            player.ChatMessage("Квест выполнен. Вы получили " + prefabName + count);
        }
        void ClearActiveQuest(ulong playerId)
        {
            PlayerProgress progress;
            if (!PlayerData.Progress.TryGetValue(playerId, out progress))
            {
                Puts("QuestComplited error " + playerId + progress.QuestProcessId);
                return;
            }
            progress.QuestProcessId = null;
            progress.QuestProgressCount = 0;
        }
        
        class StoredQuests
        {
            public Dictionary<string,Quest> Quests;
            public StoredQuests()
            {
                Quests = new Dictionary<string,Quest>();
            }
        }
        class Quest
        {
            public QuestType QuestType { get; private set; }
            public string Name { get; private set; }
            public string Description { get; private set; }
            public (string, int) QuestTargetPrefab { get; private set; }
            public (string, int) QuestRewardPrefab { get; private set; }
            public QuestRequirementType QuestRequirement { get; private set; }
            public Quest(QuestType questType, string name, string description, (string, int) questTargetPrefab, (string, int) questRewardPrefab, QuestRequirementType questRequirement)
            {
                QuestType = questType;
                Name = name;
                Description = description;
                QuestTargetPrefab = questTargetPrefab;
                QuestRewardPrefab = questRewardPrefab;
                QuestRequirement = questRequirement;
            }
        }
        class StoredPlayerProgress
        {
            public Dictionary<ulong, PlayerProgress> Progress;
            public StoredPlayerProgress() 
            {
                Progress = new Dictionary<ulong, PlayerProgress>();
            }
        }
        class PlayerProgress
        {
            public List<string> QuestComplited { get; set; }
            public string QuestProcessId { get; set; }
            public int QuestProgressCount { get; set; }

            public PlayerProgress(List<string> questComplited, string questProcessId, int questProgressCount)
            {
                QuestComplited = questComplited;
                QuestProcessId = questProcessId;
                QuestProgressCount = questProgressCount;
            }
            public PlayerProgress()
            {
                QuestComplited = new List<string>();
                QuestProcessId = null;
                QuestProgressCount = 0;
            }
        }

        enum QuestType
        {
            Gather,
            Kill,
            Loot,
        }
        enum QuestRequirementType
        {
            None,
            QuestFinished,
            QuestNotFinished,
            HasPermision,
        }
    }
}