using CompanionServer.Handlers;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using Oxide.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Events;


namespace Oxide.Plugins
{
    [Info("Quests", "VSP", "0.0.0")]
    public class Quests : RustPlugin
    {
        List<Quest> QuestsList;
        string QuestDataPath = "Quests/QuestData";
        List<PlayerProgress> PlayersList;
        string PlayerDataPath = "Quests/PlayerData";

        public Quests()
        {
            LoadQuestsData();
            LoadPlayerData();
        }

        void LoadQuestsData()
        {
            QuestsList = Interface.Oxide.DataFileSystem.ReadObject<List<Quest>>(QuestDataPath);
            if (QuestsList == null) { QuestsList = new List<Quest>(); }
            var newQuest = new Quest(
                   "test",
                   "TestQuest",
                   "QuestForTest. Kill 2 boards",
                   QuestType.Kill,
                   ("boar", 2),
                   ("scrap", 50),
                   null
                   );
            var quest = QuestsList.Find(x => x.Id == newQuest.Id);
            if (quest == null)
                QuestsList.Add(newQuest);
            SaveQuestsData();
        }
        void LoadPlayerData()
        {
            PlayersList = Interface.Oxide.DataFileSystem.ReadObject<List<PlayerProgress>>(PlayerDataPath);
            if (PlayersList == null) { PlayersList = new List<PlayerProgress>(); }
        }
        void SaveQuestsData()
        { Interface.Oxide.DataFileSystem.WriteObject(QuestDataPath, QuestsList); }
        void SavePlayerData()
        { Interface.Oxide.DataFileSystem.WriteObject(PlayerDataPath, PlayersList); }
        //COMMANDS===============================================================================
        [ChatCommand("qHelp")]
        private void QuestHelp(BasePlayer player, string command, string[] args)
        {
            player.ChatMessage("qAdd\nqClear\nqInfo");
        }
        [ChatCommand("qAdd")]
        private void QuestAdd(BasePlayer player, string command, string[] args)
        {
            ulong id = player.userID;
            string questId = args[0];
            PlayerProgress progress = FindPlayerProgressById(id);
            Quest quest = FindQuestById(questId);
            if (quest == null)
            {
                player.ChatMessage($"Задание с именем {questId} не найдено");
                return;
            }
            progress.Progress = quest.Id;
            progress.Count = 0;
            player.ChatMessage($"Задание с именем {questId} успешно добавлено");

        }
        [ChatCommand("qClear")]
        private void ClearQuest(BasePlayer player, string command, string[] args)
        {
            ulong id = player.userID;
            PlayerProgress progress = FindPlayerProgressById(id);
            if (progress.Progress == null)
            {
                player.ChatMessage("У вас нет активных заданий");
                return;
            }
            progress.Progress = null;
            progress.Count = 0;
        }
        [ChatCommand("qinfo")]
        private void QuestInfo(BasePlayer player, string command, string[] args)
        {
            ulong id = player.userID;
            PlayerProgress progress = FindPlayerProgressById(id);
            if (progress.Progress == null)
            {
                player.ChatMessage("У вас нет активных заданий");
                return;
            }
            player.ChatMessage(progress.Progress + " " + progress.Count);
        }
        [ChatCommand("qSave")]
        private void QuestSave(BasePlayer player, string command, string[] args)
        {
            SaveQuestsData();
            SavePlayerData();
        }
        [ChatCommand("qLoad")]
        private void QuestLoad(BasePlayer player, string command, string[] args)
        {
            LoadQuestsData();   
            LoadPlayerData();
        }
        [ChatCommand("qShow")]
        private void QuestShow(BasePlayer player, string command, string[] args)
        {
            CuiHelper.AddUi(player, CUI);
        }
        [ChatCommand("qHide")]
        private void QuestHide(BasePlayer player, string command, string[] args)
        {
            CuiHelper.DestroyUi(player, CUI);
        }

        //PATCHS============================================================================================
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (NullCheck(info,entity, info.InitiatorPlayer))
                return;
            Puts("1");
            ulong id = info.InitiatorPlayer.userID;
            PlayerProgress progress = FindPlayerProgressById(id);
            if (progress == null)
                return;
            Puts("2");
            Quest quest = FindQuestById(id);
            Puts("2");
            Puts(quest.Id);
            if (quest == null)
                return;
            Puts("3");
            if (quest.Type == QuestType.Kill && quest.Target.Item1 == entity.ShortPrefabName)
            {
                Puts(quest.Target.Item2 + " " + progress.Count);
                if (quest.Target.Item2 <= progress.Count)
                    QuestComplite(id);
                else
                {
                    Puts("5");
                    progress.Count++;
                    BasePlayer.FindByID(id).ChatMessage($"Вы убили {entity.ShortPrefabName}. Счет: {progress.Count}");
                    Puts("5");
                }
                Puts("6");
            }

        }
        void OnPlayerConnected(BasePlayer player)
        {
            PlayerProgress progress = FindPlayerProgressById(player.userID);
            if (progress == null)
                FirstJoid(player.userID);
        }
        /*
        object OnPlayerSpawn(BasePlayer player)
        {
            PlayerProgress progress = FindPlayerProgressById(player.userID);
            if (progress == null)
                FirstJoid(player.userID);
            return null;
        }
        */
        object OnGrowableGather(GrowableEntity plant, BasePlayer player)
        {
            if (NullCheck(player, plant))
                return null;
            ulong id = player.userID;
            PlayerProgress progress = FindPlayerProgressById(id);
            if (progress == null)
                return null;
            Quest quest = FindQuestById(id);
            if (quest == null)
                return null;
            player.ChatMessage(quest.Target.Item1 + " " + plant.ShortPrefabName);
            if (quest.Type == QuestType.Gather && quest.Target.Item1 == plant.ShortPrefabName)
            {
                if (quest.Target.Item2 >= progress.Count)
                    QuestComplite(id);
                else
                    progress.Count++;
            }
            return null;
        }
        void OnLootItem(PlayerLoot playerLoot, Item item)
        {
            Puts("OnLootItem works!");
        }
        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (NullCheck(entity, info, info.InitiatorPlayer))
                return null;
            BasePlayer.FindByID(info.InitiatorPlayer.userID).ChatMessage(entity.ShortPrefabName);
            return null;
        }
        void OnItemDropped(Item item, BaseEntity entity)
        {
            Puts(item.info.shortname);  
        }
        //ADITIONS=======================================================================
        PlayerProgress FindPlayerProgressById(ulong id)
        {
            return PlayersList.Find(x => x.Id == id);
        }
        Quest FindQuestById(ulong id)
        {
            return QuestsList.Find(x => x.Id == FindPlayerProgressById(id).Progress);
        }
        Quest FindQuestById(string id)
        {
            return QuestsList.Find(x => x.Id == id);
        }
        void FirstJoid(ulong id)
        {
            PlayersList.Add(new PlayerProgress(id, new List<string>(), "", 0));
        }
        bool NullCheck(params object[] parameters)
        {
            foreach (var param in parameters)
            {
                if (param == null)
                    return true;
            }
            return false;
        }
        void QuestComplite(ulong id)
        {
            PlayerProgress progress = FindPlayerProgressById(id);
            Quest quest = FindQuestById(id);
            progress.Count = 0;
            progress.Progress = null;
            progress.Complited.Add(quest.Id);
            GiveReward(id, quest.Reward.Item1, quest.Reward.Item2);
        }
        void GiveReward(ulong id, string prefab, int count)
        {
            BasePlayer player = BasePlayer.FindByID(id);
            player.GiveItem(ItemManager.CreateByName(prefab, count));
            player.ChatMessage("Квест выполнен. Вы получили " + prefab + " " + count);
        }


        public class PlayerProgress
        {
            public PlayerProgress(ulong id, List<string> complited, string progress, int count)
            {
                Id = id;
                Complited = complited;
                Progress = progress;
                Count = count;
            }

            public ulong Id { get; set; }
            public List<string> Complited { get; set; }
            public string Progress { get; set;}
            public int Count { get; set; }
        }
        public class Quest
        {
            public Quest(string id, string name, string description, QuestType type, (string, int) target, (string, int) reward, string condition)
            {
                Id = id;
                Name = name;
                Description = description;
                Type = type;
                Target = target;
                Reward = reward;
                Condition = condition;
            }

            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public QuestType Type { get; set; }
            public (string, int) Target { get; set; }
            public (string,int ) Reward { get; set; }
            public string Condition { get; set; }

        }
        public enum QuestType
        {
            Kill,
            Loot,
            Gather
        }
        public string CUI = "[{\"name\":\"QBackGround\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"material\":\"\",\"color\":\"0.254902 0.2509804 0.2156863 0.4666667\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0.5\",\"anchormax\":\"0.5 0.5\",\"offsetmin\":\"-400 -225\",\"offsetmax\":\"400 225\"}]},{\"name\":\"Title\",\"parent\":\"QBackGround\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Quests\",\"fontSize\":28,\"font\":\"Rust\",\"color\":\"0.8980392 0.854902 0.8235294 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.02187494 0.8911111\",\"anchormax\":\"0.15 0.9733334\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestBoard\",\"parent\":\"QBackGround\",\"components\":[{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0.5\",\"anchormax\":\"0.5 0.5\",\"offsetmin\":\"-370 50\",\"offsetmax\":\"370 150\"}]},{\"name\":\"Quest1\",\"parent\":\"QuestBoard\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"material\":\"\",\"color\":\"0.254902 0.2509804 0.2156863 0.5490196\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.01081081 0.08\",\"anchormax\":\"0.1978378 0.92\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestButton\",\"parent\":\"Quest1\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"qadd a\",\"sprite\":\"assets/icons/check.png\",\"material\":\"assets/icons/iconmaterial.mat\",\"color\":\"0.4313726 0.5254902 0.254902 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.75 0\",\"anchormax\":\"1 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestText\",\"parent\":\"Quest1\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.3\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TargetText\",\"parent\":\"Quest1\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0.75 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Quest2\",\"parent\":\"QuestBoard\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"material\":\"\",\"color\":\"0.254902 0.2509804 0.2156863 0.5490196\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.2086487 0.08\",\"anchormax\":\"0.3956757 0.92\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestButton\",\"parent\":\"Quest2\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"qadd b\",\"sprite\":\"assets/icons/check.png\",\"material\":\"assets/icons/iconmaterial.mat\",\"color\":\"0.4313726 0.5254902 0.254902 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.75 0\",\"anchormax\":\"1 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestText\",\"parent\":\"Quest2\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.3\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TargetText\",\"parent\":\"Quest2\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0.75 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Quest3\",\"parent\":\"QuestBoard\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"material\":\"\",\"color\":\"0.254902 0.2509804 0.2156863 0.5490196\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.4064865 0.08\",\"anchormax\":\"0.5935135 0.92\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestButton\",\"parent\":\"Quest3\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"qadd c\",\"sprite\":\"assets/icons/check.png\",\"material\":\"assets/icons/iconmaterial.mat\",\"color\":\"0.4313726 0.5254902 0.254902 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.75 0\",\"anchormax\":\"1 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestText\",\"parent\":\"Quest3\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.3\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TargetText\",\"parent\":\"Quest3\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0.75 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Quest4\",\"parent\":\"QuestBoard\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"material\":\"\",\"color\":\"0.254902 0.2509804 0.2156863 0.5490196\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.6043243 0.08\",\"anchormax\":\"0.7913514 0.92\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestButton\",\"parent\":\"Quest4\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"qadd d\",\"sprite\":\"assets/icons/check.png\",\"material\":\"assets/icons/iconmaterial.mat\",\"color\":\"0.4313726 0.5254902 0.254902 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.75 0\",\"anchormax\":\"1 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestText\",\"parent\":\"Quest4\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.3\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TargetText\",\"parent\":\"Quest4\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0.75 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Quest5\",\"parent\":\"QuestBoard\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"material\":\"\",\"color\":\"0.254902 0.2509804 0.2156863 0.5490196\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.8021622 0.08\",\"anchormax\":\"0.9891892 0.92\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestButton\",\"parent\":\"Quest5\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"qadd e\",\"sprite\":\"assets/icons/check.png\",\"material\":\"assets/icons/iconmaterial.mat\",\"color\":\"0.4313726 0.5254902 0.254902 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.75 0\",\"anchormax\":\"1 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestText\",\"parent\":\"Quest5\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.3\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TargetText\",\"parent\":\"Quest5\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0.75 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Title2\",\"parent\":\"QBackGround\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Основные\",\"fontSize\":18,\"font\":\"Rust\",\"color\":\"0.8980392 0.854902 0.8235294 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.04749992 0.8277777\",\"anchormax\":\"0.265 0.8755557\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Title3\",\"parent\":\"QBackGround\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Ежедневные\",\"fontSize\":18,\"font\":\"Rust\",\"color\":\"0.8980392 0.854902 0.8235294 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.04687493 0.5566669\",\"anchormax\":\"0.3325 0.6077781\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestBoard2\",\"parent\":\"QBackGround\",\"components\":[{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0.5\",\"anchormax\":\"0.5 0.5\",\"offsetmin\":\"-370 -70\",\"offsetmax\":\"370 30\"}]},{\"name\":\"Quest6\",\"parent\":\"QuestBoard2\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"material\":\"\",\"color\":\"0.254902 0.2509804 0.2156863 0.5450981\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.01081081 0.08\",\"anchormax\":\"0.1978378 0.92\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestButton\",\"parent\":\"Quest6\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"qadd f\",\"sprite\":\"assets/icons/check.png\",\"material\":\"assets/icons/iconmaterial.mat\",\"color\":\"0.4313726 0.5254902 0.254902 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.75 0\",\"anchormax\":\"1 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestText\",\"parent\":\"Quest6\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.3\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TargetText\",\"parent\":\"Quest6\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0.75 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Quest7\",\"parent\":\"QuestBoard2\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"material\":\"\",\"color\":\"0.254902 0.2509804 0.2156863 0.5490196\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.2086487 0.08\",\"anchormax\":\"0.3956757 0.92\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestButton\",\"parent\":\"Quest7\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"qadd g\",\"sprite\":\"assets/icons/check.png\",\"material\":\"assets/icons/iconmaterial.mat\",\"color\":\"0.4313726 0.5254902 0.254902 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.75 0\",\"anchormax\":\"1 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestText\",\"parent\":\"Quest7\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.3\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TargetText\",\"parent\":\"Quest7\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0.75 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Quest8\",\"parent\":\"QuestBoard2\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"material\":\"\",\"color\":\"0.254902 0.2509804 0.2156863 0.5490196\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.4064865 0.08\",\"anchormax\":\"0.5935135 0.92\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestButton\",\"parent\":\"Quest8\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"qadd h\",\"sprite\":\"assets/icons/check.png\",\"material\":\"assets/icons/iconmaterial.mat\",\"color\":\"0.4313726 0.5254902 0.254902 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.75 0\",\"anchormax\":\"1 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestText\",\"parent\":\"Quest8\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.3\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TargetText\",\"parent\":\"Quest8\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0.75 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Quest9\",\"parent\":\"QuestBoard2\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"material\":\"\",\"color\":\"0.254902 0.2509804 0.2156863 0.5490196\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.6043243 0.08\",\"anchormax\":\"0.7913514 0.92\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestButton\",\"parent\":\"Quest9\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"qadd i\",\"sprite\":\"assets/icons/check.png\",\"material\":\"assets/icons/iconmaterial.mat\",\"color\":\"0.4313726 0.5254902 0.254902 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.75 0\",\"anchormax\":\"1 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestText\",\"parent\":\"Quest9\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.3\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TargetText\",\"parent\":\"Quest9\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0.75 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Quest10\",\"parent\":\"QuestBoard2\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"material\":\"\",\"color\":\"0.254902 0.2509804 0.2156863 0.5490196\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.8021622 0.08\",\"anchormax\":\"0.9891892 0.92\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestButton\",\"parent\":\"Quest10\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"qadd j\",\"sprite\":\"assets/icons/check.png\",\"material\":\"assets/icons/iconmaterial.mat\",\"color\":\"0.4313726 0.5254902 0.254902 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.75 0\",\"anchormax\":\"1 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestText\",\"parent\":\"Quest10\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.3\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TargetText\",\"parent\":\"Quest10\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0.75 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestBoard3\",\"parent\":\"QBackGround\",\"components\":[{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0.5\",\"anchormax\":\"0.5 0.5\",\"offsetmin\":\"-370 -200\",\"offsetmax\":\"370 -100\"}]},{\"name\":\"Quest11\",\"parent\":\"QuestBoard3\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"material\":\"\",\"color\":\"0.254902 0.2509804 0.2156863 0.5490196\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.01081081 0.08\",\"anchormax\":\"0.1978378 0.92\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestButton\",\"parent\":\"Quest11\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"qadd k\",\"sprite\":\"assets/icons/check.png\",\"material\":\"assets/icons/iconmaterial.mat\",\"color\":\"0.4313726 0.5254902 0.254902 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.75 0\",\"anchormax\":\"1 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestText\",\"parent\":\"Quest11\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.3\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TargetText\",\"parent\":\"Quest11\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0.75 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Quest12\",\"parent\":\"QuestBoard3\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"material\":\"\",\"color\":\"0.254902 0.2509804 0.2156863 0.5490196\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.2086487 0.08\",\"anchormax\":\"0.3956757 0.92\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestButton\",\"parent\":\"Quest12\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"qadd l\",\"sprite\":\"assets/icons/check.png\",\"material\":\"assets/icons/iconmaterial.mat\",\"color\":\"0.4313726 0.5254902 0.254902 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.75 0\",\"anchormax\":\"1 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestText\",\"parent\":\"Quest12\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.3\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TargetText\",\"parent\":\"Quest12\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0.75 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Quest13\",\"parent\":\"QuestBoard3\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"material\":\"\",\"color\":\"0.254902 0.2509804 0.2156863 0.5490196\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.4064865 0.08\",\"anchormax\":\"0.5935135 0.92\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestButton\",\"parent\":\"Quest13\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"qadd m\",\"sprite\":\"assets/icons/check.png\",\"material\":\"assets/icons/iconmaterial.mat\",\"color\":\"0.4313726 0.5254902 0.254902 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.75 0\",\"anchormax\":\"1 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestText\",\"parent\":\"Quest13\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.3\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TargetText\",\"parent\":\"Quest13\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0.75 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Quest14\",\"parent\":\"QuestBoard3\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"material\":\"\",\"color\":\"0.254902 0.2509804 0.2156863 0.5490196\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.6043243 0.08\",\"anchormax\":\"0.7913514 0.92\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestButton\",\"parent\":\"Quest14\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"qadd n\",\"sprite\":\"assets/icons/check.png\",\"material\":\"assets/icons/iconmaterial.mat\",\"color\":\"0.4313726 0.5254902 0.254902 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.75 0\",\"anchormax\":\"1 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestText\",\"parent\":\"Quest14\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.3\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TargetText\",\"parent\":\"Quest14\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0.75 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Quest15\",\"parent\":\"QuestBoard3\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"material\":\"\",\"color\":\"0.254902 0.2509804 0.2156863 0.5490196\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.8021622 0.08\",\"anchormax\":\"0.9891892 0.92\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestButton\",\"parent\":\"Quest15\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"qadd o\",\"sprite\":\"assets/icons/check.png\",\"material\":\"assets/icons/iconmaterial.mat\",\"color\":\"0.4313726 0.5254902 0.254902 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.75 0\",\"anchormax\":\"1 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"QuestText\",\"parent\":\"Quest15\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.3\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TargetText\",\"parent\":\"Quest15\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"text\",\"fontSize\":12},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0.75 0.3\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Title4\",\"parent\":\"QBackGround\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Дополнительные\",\"fontSize\":18,\"font\":\"Rust\",\"color\":\"0.8980392 0.854902 0.8235294 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.04874992 0.267778\",\"anchormax\":\"0.334375 0.3188893\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]}]";
    }
}
