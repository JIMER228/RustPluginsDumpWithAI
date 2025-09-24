using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ZealQuests", "Kira", "1.0.0")]
    [Description("Система квестов")]
    public class ZealQuests : RustPlugin
    {
        #region [Vars] / [Переменные]

        private const string PlayerPrefab = "assets/prefabs/player/player.prefab";
        private static ZealQuests _;
        private StoredData _database = new StoredData();

        #endregion

        #region [MonoBehaviours]

        public class QuestNpc : MonoBehaviour
        {
            public BasePlayer npc;
            public BasePlayer player;
            public NpcTrigger trigger;
            private StoredData.NpcConfig _npcConfig;
            public Vector3 position;
            public Vector3 rotation;

            private void Awake()
            {
                name = "Гильдейский чувак";
                _npcConfig = new StoredData.NpcConfig();
            }

            private void Start()
            {
                position = player.transform.position;
                rotation = player.eyes.BodyRay().GetPoint(1000);
                Invoke(nameof(SpawnNpc), 0.1f);
                Invoke(nameof(Delete), 60f);
            }

            #region [NPC]

            public void SpawnNpc()
            {
                npc = GameManager.server.CreateEntity(PlayerPrefab).ToPlayer();
                npc.Spawn();
                npc.SendNetworkUpdate();
                InitializeNpc();
            }

            public void InitializeNpc()
            {
                npc.displayName = name;
                npc.MovePosition(position);
                LookTowards(rotation);
                npc.SendNetworkUpdate();

                InitializedTrigger();

                _npcConfig.Position = position;
                _npcConfig.DisplayName = name;
                _._database.NpcConfigs.Add(npc.userID, _npcConfig);
                _.SaveData();
            }

            public void LookTowards(Vector3 pos)
            {
                if (pos != npc.transform.position)
                    SetViewAngle(Quaternion.LookRotation(pos - npc.transform.position));

                //player.transform.LookAt(pos - player.transform.position);
                npc.eyes.position.Set(pos.x, pos.y, pos.z);
            }

            public void SetViewAngle(Quaternion viewAngles)
            {
                if (viewAngles.eulerAngles == default(Vector3)) return;
                npc.viewAngles = viewAngles.eulerAngles;
                npc.SendNetworkUpdate();
            }

            private void Delete()
            {
                Destroy(this);
            }

            #endregion [NPC]

            #region [Trigger]

            public void InitializedTrigger()
            {
                trigger = gameObject.AddComponent<NpcTrigger>();
                trigger.name = name;
                trigger.tag = "(TRIGGER)";
            }

            public class NpcTrigger : MonoBehaviour
            {
                public QuestNpc questNpc;
                public SphereCollider trigger;
                public Vector3 position;
                public float radius;

                private void Awake()
                {
                    questNpc = GetComponent<QuestNpc>();
                    gameObject.layer = (int) Layer.Reserved1;
                    position = questNpc.position;
                    radius = 3f;
                }

                private void Start()
                {
                    Initialized();
                }

                private void Initialized()
                {
                    trigger = gameObject.AddComponent<SphereCollider>();
                    trigger.radius = radius;
                    trigger.transform.position = position;
                    trigger.isTrigger = true;
                }

                private void OnTriggerEnter(Collider other)
                {
                    var ent = other.ToBaseEntity();
                    if (ent == null) return;
                    if (!ent.IsValid()) return;
                    if (!(ent is BasePlayer)) return;
                    var player = ent.ToPlayer();
                    player.ChatMessage($"{questNpc.name} : Однако здравствуйте !");
                }

                private void OnDestroy()
                {
                    Destroy(trigger);
                }
            }

            #endregion

            private void OnDestroy()
            {
                if (npc != null) npc.Kill();
                if (trigger != null) Destroy(trigger);
                _.PrintToChat("Destroy");
            }
        }

        #endregion

        #region [Hooks] / [Крюки]

        private void OnServerInitialized()
        {
            _ = this;
            LoadData();
        }

        private void Unload()
        {
            SaveData();
        }

        #endregion

        #region [Commands] / [Команды]

        [ConsoleCommand("npc")]
        private void Npc(ConsoleSystem.Arg args)
        {
            var component = new GameObject().AddComponent<QuestNpc>();
            component.player = args.Player();
        }

        #endregion

        #region [Helpers] / [Вспомокательный код]

        private void EquipNpc(BasePlayer npc)
        {
            var inventory = npc.inventory;
            foreach (var wearItem in _database.NpcConfigs[npc.userID].Wear.WearItems)
                inventory.containerWear.AddItem(ItemManager.FindItemDefinition(wearItem.Key), 1, wearItem.Value);
            inventory.SendSnapshot();
        }

        #endregion

        #region [DataBase] / [База данных]

        public class StoredData
        {
            [JsonProperty(PropertyName = "Настройка ботов")]
            public Dictionary<ulong, NpcConfig> NpcConfigs = new Dictionary<ulong, NpcConfig>();

            public class NpcConfig : BaseQuestNpc
            {
                [JsonProperty(PropertyName = "Имя бота (Видно игрокам)")]
                public string DisplayName;

                [JsonProperty(PropertyName = "Позиция бота")]
                public Vector3 Position;

                [JsonProperty(PropertyName = "Позиция головы бота")]
                public Vector3 LookRotation;
            }

            public class BaseQuestNpc
            {
                [JsonProperty(PropertyName = "Одежда бота (Максимум 7 слотов)")]
                public readonly Wear Wear = new Wear();

                [JsonProperty(PropertyName = "Диалоги бота")]
                public Conversation Conversation = new Conversation();
            }

            public class Wear
            {
                [JsonProperty(PropertyName = "Список одежды")]
                public readonly Dictionary<string, ulong> WearItems = new Dictionary<string, ulong>();
            }

            public class Conversation
            {
                [JsonProperty(PropertyName = "Приветствие бота (Тип приветствия : Сообщение)")]
                public Dictionary<MessageType, string> Greeting = new Dictionary<MessageType, string>();

                public enum MessageType
                {
                    Hello,
                    Bye,
                    BadKarma,
                    GoodKarma
                }
            }
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _database);

        private void LoadData()
        {
            try
            {
                _database = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch (Exception)
            {
                _database = new StoredData();
            }
        }

        #endregion
    }
}