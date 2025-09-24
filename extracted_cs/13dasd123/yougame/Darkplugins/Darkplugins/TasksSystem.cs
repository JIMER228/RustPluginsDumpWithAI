using System.Linq;
using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("TasksSystem", "https://discord.gg/dNGbxafuJn", "1.1.0")]
    class TasksSystem : RustPlugin
    {
        #region Вар
        string Layer = "Tasks_UI";

        public Dictionary<ulong, DataBase> DB = new Dictionary<ulong, DataBase>();
        private readonly List<uint> crateInfo = new List<uint>();
        #endregion

        #region Класс
        public class TasksSettings
        {
            public string DisplayName;
            public string Description;
            public string Tasks;
            public int Amount;
        }

        public class DataBase
        {
            public bool Enabled = false;
            public bool TasksCompleted = false;
            public int Completed = 0;
            public Dictionary<string, TasksData> db = new Dictionary<string, TasksData>();
        }

        public class TasksData
        {
            public int Amount;
            public bool Enabled;
        }
        #endregion

        #region Конфиг
        public string Display = "VIP НА 30 ДНЕЙ";
        public string Command = "addgroup %STEAMID% vip 3d";
        List<TasksSettings> tasks = new List<TasksSettings>()
        {
            new TasksSettings
            {
                DisplayName = "Добыча дерева",
                Description = "Добыть 1000 дерева, чтобы завершить задание!",
                Tasks = "wood",
                Amount = 1000
            },
            new TasksSettings
            {
                DisplayName = "Какие то бочки",
                Description = "Сломайте 20 обычных бочек, чтобы завершить задание!",
                Tasks = "loot_barrel_2",
                Amount = 20
            },
            new TasksSettings
            {
                DisplayName = "Царь горы",
                Description = "Убейте 5 игроков, чтобы завершить задание!",
                Tasks = "player",
                Amount = 5
            },
            new TasksSettings
            {
                DisplayName = "Пиздюки",
                Description = "Убейте 10 нпс, чтобы завершить задание!",
                Tasks = "scientistjunkpile",
                Amount = 10
            },
            new TasksSettings
            {
                DisplayName = "Пушка",
                Description = "Скрафтите ак, чтобы завершить задание!",
                Tasks = "rifle.ak",
                Amount = 1
            },
            new TasksSettings
            {
                DisplayName = "Гандон",
                Description = "Залутайте чинук, чтобы завершить задание!",
                Tasks = "codelockedhackablecrate",
                Amount = 1
            },
        };
        #endregion

        #region Хуки
        void OnServerInitialized()
        {
			PrintWarning("\n-----------------------------\n" +
            "     Author - Sempai#3239\n" +
            "     VK - https://vk.com/rustnastroika/n" +
            "     Discord - https://discord.gg/5DPTsRmd3G/n" +
            "-----------------------------");
            foreach (var check in BasePlayer.activePlayerList)
                OnPlayerConnected(check);
        }

        void OnPlayerConnected(BasePlayer player) => CreateDataBase(player);

        void OnPlayerDisconnected(BasePlayer player) => SaveDataBase(player.userID);

        void Unload()
        {
            foreach (var check in DB)
                SaveDataBase(check.Key);

            foreach (var check in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(check, Layer);
        }

        object OnCollectiblePickup(Item item, BasePlayer player)
        {
            Progress(player, item.info.shortname, item.amount);
            return null;
        }

        object OnDispenserBonus(ResourceDispenser dispenser, BaseEntity entity, Item item) => OnDispenserGather(dispenser, entity, item);
        object OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity.ToPlayer();
            NextTick(() => {
                Progress(player, item.info.shortname, item.amount);
            });
            return null;
        }

        public ulong lastDamageName;
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is BradleyAPC && info.Initiator is BasePlayer)
                lastDamageName = info.Initiator.ToPlayer().userID;
            if (entity is BaseHelicopter && info.Initiator is BasePlayer)
                lastDamageName = info.Initiator.ToPlayer().userID;
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return;
            BasePlayer player = null;

            if (info.InitiatorPlayer != null) player = info.InitiatorPlayer;
            else if (entity is BradleyAPC || entity is BaseHelicopter) player = BasePlayer.FindByID(lastDamageName);

            if (player == null) return;
            if (entity.ToPlayer() != null && entity as BasePlayer == player) return;
            Progress(player, entity?.ShortPrefabName, 1);
        }

        void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            Progress(task.owner, item.info.shortname, item.amount);
        }

        void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (crateInfo.Contains(entity.net.ID))
                return;

            crateInfo.Add(entity.net.ID);

            if (entity.ShortPrefabName.Contains("crate_normal") || entity.ShortPrefabName.Contains("codelockedhackablecrate"))
                Progress(player, entity?.ShortPrefabName, 1);
        }
        #endregion

        #region Дата
        void CreateDataBase(BasePlayer player)
        {
            var Database = Interface.Oxide.DataFileSystem.ReadObject<DataBase>($"TasksSystem/{player.userID}");
            
            if (!DB.ContainsKey(player.userID))
                DB.Add(player.userID, new DataBase());
             
            DB[player.userID] = Database ?? new DataBase();
        }

        TasksData GetDataBase(ulong userID, string name)
        {
            if (!DB.ContainsKey(userID))
                DB[userID].db = new Dictionary<string, TasksData>();

            if (!DB[userID].db.ContainsKey(name))
                DB[userID].db[name] = new TasksData();

            return DB[userID].db[name];
        }

        void SaveDataBase(ulong userId) => Interface.Oxide.DataFileSystem.WriteObject($"TasksSystem/{userId}", DB[userId]);
        #endregion

        #region Прогресс
        void Progress(BasePlayer player, string name, int amount)
        {
            if (DB[player.userID].Enabled == true)
            {
                foreach (var check in tasks)
                {
                    var db = DB[player.userID];
                    if (check.Tasks == name)
                    {
                        var database = db.db.FirstOrDefault(z => z.Key == name);
                        if (database.Value.Enabled == false)
                        {
                            database.Value.Amount += amount;
                            player.SendConsoleCommand($"note.inv 605467368 +{amount} \"Задание\"");
                            if (database.Value.Amount >= check.Amount)
                            {
                                player.SendConsoleCommand($"note.inv 605467368 +1 \"Задание выполнено\"");
                                database.Value.Enabled = true;
                                database.Value.Amount = check.Amount;
                                db.Completed += 1;
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Команды
        [ChatCommand("tasks")]
        void ChatTasks(BasePlayer player) 
        {
            if (DB[player.userID].Enabled == false)
            {
                DB[player.userID].Enabled = true;
                foreach (var check in tasks)
                    GetDataBase(player.userID, check.Tasks);
            }
            TasksUI(player);
        }

        [ConsoleCommand("tasks")]
        void ConsoleTasks(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player != null && args.HasArgs(1))
            {
                if (args.Args[0] == "priz")
                {
                    if (DB[player.userID].Completed == 6 && DB[player.userID].TasksCompleted == false)
                    {
                        Server.Command(Command.Replace("%STEAMID%", player.UserIDString));
                        SendReply(player, $"Вы успешно выполнили все 6 заданий и за это вы получаете {Display}");
                        DB[player.userID].TasksCompleted = true;
                        TasksUI(player);
                    }
                }
            }
        }
        #endregion

        #region Интерфейс
        void TasksUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.9", Material = "assets/content/ui/uibackgroundblur.mat" }
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0.6", Close = Layer },
                Text = { Text = "" }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0.7", AnchorMax = "1 0.8", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = $"<size=20>ЗАДАНИЯ</size>\nЗдесь, вы можете выполнить задания и получить награду!", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 12 }
            }, Layer);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.362 0.287", AnchorMax = "0.642 0.342", OffsetMax = "0 0" },
                Image = { Color = "1 1 1 0.1" }
            }, Layer, "Background");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.03 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Text = { Text = $"<b><size=12>Привилегия</size></b>\nВыполните 6 заданий и получите награду!", Color = "1 1 1 0.5", Align = TextAnchor.MiddleLeft, FontSize = 10, Font = "robotocondensed-regular.ttf" }
            }, "Background");

            var text = DB[player.userID].TasksCompleted == false ?  $"{DB[player.userID].Completed} / {tasks.Count()}" : "Награда получена!";
            var pos = DB[player.userID].TasksCompleted == false ? "0.97 1" : "0.98 1";
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.6 0", AnchorMax = pos, OffsetMax = "0 0" },
                Text = { Text = text, Color = "1 1 1 0.5", Align = TextAnchor.MiddleRight, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, "Background");

            if (DB[player.userID].TasksCompleted == false)
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0.9785 0.1", AnchorMax = "0.988 0.9", OffsetMax = "0 0" },
                    Image = { Color = "1 1 1 0.2" }
                }, "Background", "Progress");

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = $"0.93 {(float)DB[player.userID].Completed / tasks.Count()}", OffsetMax = "0 0" },
                    Image = { Color = "0.40 0.64 0.46 1" }
                }, "Progress");
            }

            if (DB[player.userID].Completed == 6 && DB[player.userID].TasksCompleted == false)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.6 0", AnchorMax = $"1 1", OffsetMin = "2 2", OffsetMax = "-2 -2" },
                    Button = { Color = "0.40 0.64 0.46 1", Command = "tasks priz" },
                    Text = { Text = $"ЗАБРАТЬ", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-regular.ttf" }
                }, "Background");
            }

            if (DB[player.userID].Enabled)
            {
                float width = 0.29f, height = 0.06f, startxBox = 0.354f, startyBox = 0.72f - height, xmin = startxBox, ymin = startyBox;
                foreach (var check in tasks)
                {
                    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = xmin + " " + ymin, AnchorMax = (xmin + width) + " " + (ymin + height * 1), OffsetMin = "10 -2", OffsetMax = "-3 -6" },
                        Image = { Color = "1 1 1 0.1" }
                    }, Layer, "Tasks");

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.03 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        Text = { Text = $"<size=12><b>{check.DisplayName}</b></size>\n{check.Description}", Color = "1 1 1 0.5", Align = TextAnchor.MiddleLeft, FontSize = 10, Font = "robotocondensed-regular.ttf" }
                    }, "Tasks");

                    var db = DB[player.userID].db.FirstOrDefault(z => z.Key == check.Tasks);
                    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0.98 0.1", AnchorMax = "0.988 0.9", OffsetMax = "0 0" },
                        Image = { Color = "1 1 1 0.2" }
                    }, "Tasks", "ProgressBar");

                    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = $"0.93 {(float)db.Value.Amount / check.Amount}", OffsetMax = "0 0" },
                        Image = { Color = "0.40 0.64 0.46 1" }
                    }, "ProgressBar");

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.6 0.1", AnchorMax = "0.97 0.9", OffsetMax = "0 0" },
                        Text = { Text = $"{db.Value.Amount} / {check.Amount}", Color = "1 1 1 0.5", Align = TextAnchor.MiddleRight, FontSize = 12, Font = "robotocondensed-regular.ttf" }
                    }, "Tasks");

                    xmin += width;
                    if (xmin + width >= 0)
                    {
                        xmin = startxBox;
                        ymin -= height;
                    }
                }
            }

            CuiHelper.AddUi(player, container);
        }
        #endregion
    }
}