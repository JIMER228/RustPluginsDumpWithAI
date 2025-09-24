using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Experience", "__red", "1.11.773")]
    public class Experience : RustPlugin
    {
        #region Objects
        private static Experience  Instance;
        private ExperienceSettings Settings;
        #endregion

        #region Fields
        private List<ExpPlayer> CurrentPlayers = new List<ExpPlayer>();
        private List<ItemQueue> ForgottenItems = new List<ItemQueue>();
        #endregion

        #region Custom
        public class TimerBase
        {
            public Timer Object { get; set; }
            public int Remaining { get; set; }
            public bool IsEnabled { get; set; }

            public TimerBase()
            {
                Remaining = 0;
                IsEnabled = false;
            }

            public void Instantiate(PluginTimers @object, int secs, Action callBackAction, Action onTick = null)
            {
                Object = @object.Repeat(1, secs, () =>
                {
                    secs--;
                    Remaining = secs;

                    if (onTick != null) onTick();

                    if (secs == 0)
                    {
                        IsEnabled = false;
                        callBackAction();
                    }
                    else IsEnabled = true;
                });
            }

            public void Destroy()
            {
                if (Object != null)
                {
                    Object.Destroy();
                    Object = null;
                }

                IsEnabled = false;
            }
        }
        public class ItemQueue
        {
            public string Name;
            public int Count;
            public ulong OwnerId;

            public ItemQueue() { }
            public ItemQueue(Item item, ulong owner)
            {
                Name = item.info.shortname;
                Count = item.amount;
                OwnerId = owner;
            }
            public ItemQueue(string name, int count, ulong owner)
            {
                Name = name;
                Count = count;
                OwnerId = owner;
            }
        }
        public class ExpPlayer : ICloneable
        {
            public ulong Id;
            public string Name;
            public int Level;
            public float Needed;
            public float Current;
            public TimerBase Counter;

            public ExpPlayer()
            {
                Id = 0;
            }
            public ExpPlayer(BasePlayer player, int level, float needed, float current)
            {
                Id = player.userID;
                Name = player.displayName;
                Level = level;
                Needed = needed;
                Current = current;

                Counter = new TimerBase();
            }
            public void Init(PluginTimers timerInstance, int secs, Action callback)
            {
                if (Counter == null) Counter = new TimerBase();

                Counter.Instantiate(timerInstance, secs, callback);
            }
            public void Destroy()
            {
                if (Counter != null)
                {
                    Counter.Destroy();
                    Counter = null;
                }
            }
            public void Update(ExpPlayer data)
            {
                Id = data.Id;
                Name = data.Name;
                Level = data.Level;
                Needed = data.Needed;
                Current = data.Current;
            }

            public void Iter()
            {
                if (Instance.Settings.MaxLevel > Level)
                {
                    Level++;
                }
            }
            public void GiveExp(float count)
            {
                Current += count;
            }
            public void GiveLevel(int level)
            {
                Level += level;
            }

            public object Clone()
            {
                return MemberwiseClone();
            }
        }
        #endregion

        #region Config
        public class ExperienceSettings
        {
            [JsonProperty("Максимальный уровень системы опыта:")]
            public int MaxLevel;

            [JsonProperty("Мультипликатор формулы подсчета игрового опыта:")]
            public int FormulaMultiplier;

            [JsonProperty("Временные циклы выдачи награды за игровое время:")]
            public float RewardTimeCicles;

            [JsonProperty("Количество игрового опыта выдаваемое за проведенное время:")]
            public float RewardXP;

            [JsonProperty("Множитель награды за проведенное время (скалирование от уровня):")]
            public float RewardMultiplier;

            [JsonProperty("Количество игрового опыта за убийство животных:")]
            public float ExpForKillAnimal;

            [JsonProperty("Включить награду за игровое время?")]
            public bool EnableAutoReward;

            [JsonProperty("Количество игрового опыта за убийство других игроков:")]
            public float ExpForKillPlayer;

            [JsonProperty("Количество игрового опыта за добычу ресурсов:")]
            public float ExpForGatherResources;

            [JsonProperty("Цвет прогресс бара:")]
            public string ProgressBarColor;

            [JsonProperty("Цвет заднего фона:")]
            public string BackgroundColor;

            [JsonProperty("Размер шрифта:")]
            public int FontSize;

            [JsonProperty("Стиль материала для прогресс бара:")]
            public string ProgressBarMaterial;

            [JsonProperty("Стиль материала для заднего фона:")]
            public string BackgroundMaterial;

            [JsonProperty("Стиль строки информации об уровне:")]
            public string ProgressBarInfo;

            [JsonProperty("Левый якорь UI:")]
            public string AnchorMin;

            [JsonProperty("Правый якорь UI:")]
            public string AnchorMax;

            [JsonProperty("Награды согласно уровню (0 - это награда за время):")]
            public Dictionary<int, string> Rewards;

            public static ExperienceSettings Prototype()
            {
                return new ExperienceSettings()
                {
                    MaxLevel = 30,
                    FormulaMultiplier = 2,
                    RewardTimeCicles = 30,
                    RewardXP = 0.5f,
                    ExpForKillPlayer = 0.5f,
                    ExpForKillAnimal = 0.1f,
                    ExpForGatherResources = 0.005f,
                    RewardMultiplier = 1.2f,
                    EnableAutoReward = true,

                    //UI
                    BackgroundColor = "#181818BE",
                    ProgressBarColor = "#235A6AFF",
                    AnchorMin = "0.343 0.115",
                    AnchorMax = "0.641 0.140",
                    BackgroundMaterial = "assets/content/ui/uibackgroundblur.mat",
                    ProgressBarMaterial = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                    ProgressBarInfo = "Уровень: {0} ({1} из {2} EXP)",
                    FontSize = 12,

                    Rewards = new Dictionary<int, string>()
                    {
                        [0] = "ammo.rifle|3|chat.say 123",
                        [1] = "ammo.pistol|1",
                        [2] = "ammo.pistol|2",
                        [3] = "ammo.pistol|3",
                        [4] = "ammo.pistol|4",
                    },
                };
            }
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();

            Settings = Config.ReadObject<ExperienceSettings>();
        }
        protected override void LoadDefaultConfig() => Settings = ExperienceSettings.Prototype();
        protected override void SaveConfig() => Config.WriteObject(Settings);
        #endregion

        #region Oxide
        private void OnServerInitialized()
        {
            Instance = this;
            BasePlayer.activePlayerList.ForEach(OnPlayerInit);

            var forgotten = Interface.Oxide.DataFileSystem.ReadObject<List<ItemQueue>>($"{Author}\\{Title}\\ItemsQueue");
            if(forgotten != null || forgotten.Count > 0)
            {
                ForgottenItems = forgotten;
            }
        }
        private void Unload()
        {
            foreach(var player in BasePlayer.activePlayerList)
            {
                Save(player, false);
                DestroyUI(player);
            }

            CurrentPlayers.Clear();

            Interface.Oxide.DataFileSystem.WriteObject($"{Author}\\{Title}\\ItemsQueue", ForgottenItems);
        }
        private void OnPlayerInit(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() =>
                {
                    OnPlayerInit(player);

                    return;
                });
            }
            else
            {

                var loaded = Load(player.userID);
                if (BasePlayer.FindByID(loaded.Id) == null) return;

                ReinstallTimer(loaded.Id);
                DrawUI(loaded);
            }
        }
        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            DestroyUI(player);
            Save(player);
        }
        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (info == null || entity == null) return;
            if (info.InitiatorPlayer == null) return;
            if (info.InitiatorPlayer is NPCPlayer) return;
            if (info.InitiatorPlayer is NPCPlayerApex) return;
            if (info.InitiatorPlayer.GetEntity().name.Contains("scientist")) return;
            if (info.InitiatorPlayer.GetEntity().IsNpc) return;

            float gived = 0f;
            ExpPlayer exp = Reinterpret<ExpPlayer>(info.InitiatorPlayer.userID);
            if (exp == null) return;

            if (entity.GetEntity() == null) return;
            if (entity.GetEntity().ToPlayer() != null)
            {
                gived = Settings.ExpForKillPlayer;
                DestroyUI(entity.GetEntity().ToPlayer());
            }
            else if(entity.GetEntity().name.Contains("agents/"))
            {
                gived = Settings.ExpForKillAnimal;
            }
            if (gived != 0f)
            {
                exp = (ExpPlayer)exp.Clone();
                exp.GiveExp(gived);

                Update(exp);
            }
        }
        private object OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (player == null) return null;

            ExpPlayer exp = Reinterpret<ExpPlayer>(player.userID);
            if (exp == null) return null;

            exp = (ExpPlayer)exp.Clone();
            exp.GiveExp(Settings.ExpForGatherResources);

            Update(exp);

            return null;
        }
        private void OnPlayerRespawned(BasePlayer player)
        {
            var exp = Reinterpret<ExpPlayer>(player.userID);
            if (exp == null) return;
            else
            {
                DrawUI(exp);
            }
        }
        #endregion

        #region Interface
        private const string Hud = "Experience_UI";
        private const string Hud_Text = Hud + ".Text";

        private void DrawUI(ExpPlayer player)
        {
            var @base = BasePlayer.FindByID(player.Id);
            if(@base == null)
            {
                Save(player);

                return;
            }

            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiButton()
            {
                RectTransform = { AnchorMin = Settings.AnchorMin, AnchorMax = Settings.AnchorMax, OffsetMax = "0 0" },
                Button = { Material = Settings.BackgroundMaterial, Color = Color(Settings.BackgroundColor, 0.9f) },
                Text = { Text = "" }
            }, "Overlay", Hud);

            string max = $"0.{(int)((decimal)player.Current * 100M / 0.998M)} 0.95";

            container.Add(new CuiButton()
            {
                RectTransform = { AnchorMin = "0.002 0.05", AnchorMax = max, OffsetMax = "0 0" },
                Button = { Material = Settings.ProgressBarMaterial, Color = Color(Settings.ProgressBarColor, 0.9f) },
                Text = { Text = "" }
            }, Hud);

            container.Add(new CuiButton()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = "" }
            }, Hud);

            container.Add(new CuiLabel()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Text = { Color = "1 1 1 1", Text = string.Format(Settings.ProgressBarInfo, player.Level, player.Current, player.Needed), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = Settings.FontSize },
            }, Hud);

            CuiHelper.AddUi(@base, container);
        }
        private void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Hud);
            //CuiHelper.DestroyUi(player, Hud_Text);
        }
        private void RefreshUI(ExpPlayer player)
        {
            var @base = BasePlayer.FindByID(player.Id);
            if(@base == null)
            {
                Save(player);

                return;
            }

            DestroyUI(@base);
            DrawUI(player);
        }
        private string Color(string hexColor, float alpha)
        {
            if (hexColor.StartsWith("#"))
            {
                hexColor = hexColor.TrimStart('#');
            }

            int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
            int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
            int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
            return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
        }
        #endregion

        #region Commands
        [ChatCommand("exp.queue")]
        void ChatExpQueue(BasePlayer player)
        {
            if (player == null) return;
            else
            {
                if(ForgottenItems.Any((x) => x.OwnerId == player.userID))
                {
                    List<ItemQueue> herItems = new List<ItemQueue>();

                    foreach(var forgot in ForgottenItems.Where((x) => x.OwnerId == player.userID))
                    {
                        herItems.Add(forgot);
                    }

                    if(herItems.Count <= 0)
                    {
                        SendReply(player, $"Вы не забывали свои награды");

                        return;
                    }

                    if (player.inventory.containerMain.availableSlots.Count > herItems.Count)
                    {

                        SendReply(player, $"Не удалось получить награду.\nТребуется слотов в инвентаре: {herItems.Count}");

                        return;
                    }
                    else
                    {
                        foreach(var item in herItems)
                        {
                            ForgottenItems.Remove(item);

                            var giveable = ItemManager.CreateByPartialName(item.Name, item.Count);
                            if(giveable != null)
                            {
                                giveable.MoveToContainer(player.inventory.containerMain);
                            }
                        }

                        SendReply(player, "Вы успешно забрали свои забытые награды");
                    }
                }
            }
        }

        [ChatCommand("exp.give")]
        void ChatExpGive(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            if (!player.IsAdmin) return;
            if(args.Length < 2)
            {
                SendReply(player, "Некорректное использование команды. Используйте /exp.give playername 2,32");

                return;
            }

            var target = BasePlayer.Find(args[0]);
            if(target == null)
            {
                SendReply(player, $"Игрок: {args[0]} не найден");

                return;
            }

            float giveable = 0f;
            if(!float.TryParse(args[1], out giveable))
            {
                SendReply(player, $"Неверный формат опыта для выдачи. Пример: 2,15");

                return;
            }

            var exp = Reinterpret<ExpPlayer>(target.userID);
            if(exp == null)
            {
                SendReply(player, $"Компонент опыта для игрока: {target.displayName} не найден. Вероятно это системная ошибка");

                return;
            }

            SendReply(player, giveable.ToString());
            exp.GiveExp(giveable);

            SendReply(player, $"Опыт игрока: {exp.Name} изменился на {exp.Current}");
            Update((ExpPlayer)exp.Clone());
        }

        [ChatCommand("level.give")]
        private void ChatLevelGive(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            if (!player.IsAdmin) return;
            if(args.Length < 2)
            {
                SendReply(player, "Некорректное использование команды. Используйте /level.give playername 3");

                return;
            }

            var target = BasePlayer.Find(args[0]);
            if (target == null)
            {
                SendReply(player, $"Игрок: {args[0]} не найден");

                return;
            }

            int giveable = 0;
            if (!int.TryParse(args[1], out giveable))
            {
                SendReply(player, $"Неверный формат выдачи уровня. Должно быть целое число.");

                return;
            }

            var exp = Reinterpret<ExpPlayer>(target.userID);
            if (exp == null)
            {
                SendReply(player, $"Компонент опыта для игрока: {target.displayName} не найден. Вероятно это системная ошибка");

                return;
            }

            exp.GiveLevel(giveable);
            exp.Current = 0f;
            exp.Needed = GetNeeded(exp.Level);

            Update((ExpPlayer)exp.Clone());

            SendReply(player, $"Уровень игрока: {exp.Name} изменился на {exp.Level}");
        }
        #endregion

        #region Core
        private void Update(ExpPlayer player)
        {
            var @base = BasePlayer.FindByID(player.Id);
            if (@base == null)
            {
                Save(player, true);

                return;
            }
            else
            {
                if (player.Current >= player.Needed)
                {
                    player.Iter();
                    player.Current = 0f;
                    player.Needed = GetNeeded(player.Level);

                    SendReply(@base, $"Вы получили новый уровень!");

                    RewardPlayer(player.Id, "lUp");
                }

                RefreshUI(player);
                Reinterpret<ExpPlayer>(player.Id).Update(player);
            }
        }
        private int GetRewardSeconds(int level)
        {
            return (int)Math.Floor(Settings.RewardTimeCicles * level);
        }
        private void RewardPlayer(ulong id, string from)
        {
            if (!Settings.EnableAutoReward) return;

            var player = BasePlayer.FindByID(id);
            if (player == null) return;

            var exp = Reinterpret<ExpPlayer>(id);
            if (exp == null) return;

            string[] data = null;
            if(from == "auto")
            {
                if(Settings.Rewards.ContainsKey(0))
                {
                    data = Settings.Rewards[0].Split('|');

                    exp.GiveExp(Settings.RewardXP);
                    Update((ExpPlayer)exp.Clone());
                }
            }
            else if(from == "lUp")
            {
                if (!Settings.Rewards.ContainsKey(exp.Level)) return;
                else
                {
                    data = Settings.Rewards[exp.Level].Split('|');
                }
            }

            if (data == null) return;
            if (data.Length == 3)
            {
                player.SendConsoleCommand(data[2]);

                return;
            }

            int count = 0;
            if (Int32.TryParse(data[1], out count))
            {
                var reward = ItemManager.CreateByPartialName(data[0], (int)(count * Settings.RewardMultiplier));
                if (reward == null) return;

                if (player.inventory.containerMain.availableSlots.Count > count)
                {

                    ForgottenItems.Add(new ItemQueue(reward, player.userID));

                    SendReply(player, $"Не удалось получить награду.\nИспользуйте команду: /exp.queue для получения.");
                    return;
                }
                else
                {
                    reward.MoveToContainer(player.inventory.containerMain);

                    SendReply(player, $"Вы получили награду: {reward.info.displayName.english} ({reward.amount} шт.)");
                }
            }
        }
        private void ReinstallTimer(ulong id)
        {
            var exp = Reinterpret<ExpPlayer>(id);
            if (exp == null) return;
            else
            {
                exp.Init(timer, GetRewardSeconds(exp.Level), () =>
                {
                    RewardPlayer(exp.Id, "auto");
                    ReinstallTimer(exp.Id);
                });
            }
        }
        private T Reinterpret<T>(ulong id)
        {
            if (!CurrentPlayers.Any((x) => x.Id == id)) return (T)(object)null;
            else
            {
                var input = CurrentPlayers.First((x) => x.Id == id);
                if (input == null) return (T)(object)null;
                else
                {
                    if (typeof(T) == typeof(ExpPlayer))
                    {
                        return (T)(object)input;
                    }
                    else if (typeof(T) == typeof(BasePlayer))
                    {
                        var player = BasePlayer.FindByID(input.Id);
                        if (player == null) return (T)(object)null;
                        else
                        {
                            return (T)(object)player;
                        }
                    }
                    else if(typeof(T) == typeof(TimerBase))
                    {
                        return (T)(object)input.Counter;
                    }
                    else return (T)(object)null;
                }
            }
        }
        private void Save<T>(T self, bool remove = true)
        {
            ExpPlayer saveable = null;

            if(typeof(T) != typeof(ExpPlayer))
            {
                if(typeof(T) == typeof(BasePlayer))
                {
                    saveable = Reinterpret<ExpPlayer>(((BasePlayer)(object)self).userID);
                }
                else if(typeof(T) == typeof(ulong))
                {
                    saveable = Reinterpret<ExpPlayer>(((ulong)(object)self));
                }
                else if(typeof(T) == typeof(string))
                {
                    if(((string)(object)self).IsSteamId())
                    {
                        saveable = Reinterpret<ExpPlayer>(ulong.Parse((string)(object)self));
                    }
                }
            }

            if (saveable == null) return;
            else
            {
                saveable.Destroy(); //Destroy don't saveable components (timer, etc..)

                Interface.Oxide.DataFileSystem.WriteObject($"{Author}\\{Title}\\{saveable.Id.ToString()}", saveable);

                if (remove) CurrentPlayers.Remove(saveable);
            }
        }
        private ExpPlayer Load(ulong id)
        {
            ExpPlayer loadeable = Interface.Oxide.DataFileSystem.ReadObject<ExpPlayer>($"{Author}\\{Title}\\{id.ToString()}");
            if(loadeable == null || loadeable.Id == 0)
            {
                loadeable = null;
                loadeable = new ExpPlayer(BasePlayer.FindByID(id), 1, GetNeeded(1), 0f);
            }

            if (loadeable == null || loadeable.Id == 0) return null;
            if (Reinterpret<ExpPlayer>(id) != null)
            {
                Save(id);
                Load(id);
            }

            CurrentPlayers.Add(loadeable);
            return CurrentPlayers[CurrentPlayers.IndexOf(loadeable)];
        }
        private float GetNeeded(int level)
        {
            return level * Settings.FormulaMultiplier; 
        }
        #endregion
    }
}
