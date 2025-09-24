// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("Bee", "https://discord.gg/dNGbxafuJn", "0.1.2")]
    [Description("Пчелки")]
    class Bee : RustPlugin
    {
        [PluginReference] Plugin ImageLibrary, RustStore;
        #region Data
        private StoredData DataBase = new StoredData();
        public class StoredData
        {
            public Dictionary<ulong, BeeBase> BeeInfo = new Dictionary<ulong, BeeBase>();
        }
        public class ApiaryBase
        {
            public int count;
            public int honey;
            public int Hours;
            public DateTime Date;
            public int HoneyInDay;
        }
        public class BeeBase
        {
            public int honey;
            public float Rub;
            public Dictionary<int, ApiaryBase> apiarys;

        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, DataBase);

        private void LoadData()
        {
            try
            {
                DataBase = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch (Exception e)
            {
                DataBase = new StoredData();
            }
        }
        #endregion

        #region Config
        private static ConfigFile config;

        public class ConfigFile
        {
            [JsonProperty(PropertyName = "Поддержка Магазина Moscow OVH")]
            public bool OvhStore { get; set; } = false;
            [JsonProperty(PropertyName = "Поддержка Магазина GameStore")]
            public bool GameStore { get; set; } = false;
            [JsonProperty(PropertyName = "GameStore Id Магазина")]
            public string GSId { get; set; } = "";
            [JsonProperty(PropertyName = "GameStore Api Ключь")]
            public string GSApi { get; set; } = "";
            [JsonProperty(PropertyName = "ShortName Предмета пасики")]
            public string ApiaryShortName { get; set; }
            [JsonProperty(PropertyName = "SkinId Предмета пасики")]
            public ulong ApiarySkin { get; set; }
            [JsonProperty(PropertyName = "ShortName Предмета пчелы")]
            public string BeeShortName { get; set; }
            [JsonProperty(PropertyName = "SkinId Предмета пчелы")]
            public ulong BeeSkin { get; set; }
            [JsonProperty(PropertyName = "ID Npc (HumanNPC)")]
            public ulong NPCID { get; set; }
            [JsonProperty(PropertyName = "Максимальное количество пасек")]
            public Dictionary<string, int> MaxApiarys { get; set; }
            [JsonProperty(PropertyName = "Максимальное выроботка меда за сутки")]
            public Dictionary<string, int> MaxHoney { get; set; }
            [JsonProperty(PropertyName = "Через сколько часов начислять 1 ед меда с 1 улья")]
            public int ApiaryTime { get; set; }
            [JsonProperty(PropertyName = "Сколько получит рублей за 1 ед меда")]
            public float CourseRub { get; set; }

        }

        protected override void SaveConfig() => Config.WriteObject(config);

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<ConfigFile>();
                if (config == null)
                    Regenerate();
            }
            catch
            {
                Regenerate();
            }
        }

        private void Regenerate()
        {
            LoadDefaultConfig();
        }

        private ConfigFile GetDefaultSettings()
        {
            return new ConfigFile
            {
                GameStore = false,
                OvhStore = false,
                GSApi = "",
                GSId = "",
                ApiaryShortName = "xmas.decoration.star",
                ApiarySkin = 1917469429,
                BeeShortName = "xmas.decoration.tinsel",
                BeeSkin = 1917472170,
                NPCID = 232996371,
                MaxApiarys = new Dictionary<string, int>()
                {
                    { "level1", 10 },
                    { "level2", 8 },
                    { "level3", 5 },
                    { "level4", 3 },
                    { "level5", 1 },

                },
                MaxHoney = new Dictionary<string, int>()
                {
                    { "level1", 5 },
                    { "level2", 7 },
                    { "level3", 10 },
                    { "level4", 14 },
                    { "level5", 17 },

                },
                ApiaryTime = 2,
                CourseRub = 0.4f,

            };
        }
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Attempting to create default config...");
            Config.Clear();
            Config.WriteObject(GetDefaultSettings(), true);
            Config.Save();
        }
        #endregion

        #region Hoocks
        void Init()
        {
            LoadConfig();
            LoadData();
        }
        private void OnServerInitialized()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CheckDb(player.userID);
            }
            foreach (var apiary in DataBase.BeeInfo)
            {
                if (apiary.Value.apiarys.Count != 0)
                {
                    foreach (var Find in apiary.Value.apiarys)
                    {
                        Find.Value.Hours = config.ApiaryTime;
                        Find.Value.Date = DateTime.Now;
                    }
                }
            }
            ImageLibrary?.Call("AddImage", "https://i.imgur.com/5YGZbuQ.png", "paseka");
            ImageLibrary?.Call("AddImage", "https://i.imgur.com/4mlFBv4.png", "beelogo");

        }
        void OnPlayerInit(BasePlayer player)
        {
            CheckDb(player.userID);
        }
        void GetRandom()
        {
            int Get = 0;
            if (Get == {DarkPluginsID})
                {
                PrintError("Error Check");
            }
        
        }
        Item OnItemSplit(Item Thisitem, int amount)
        {
            if (Thisitem.skin == config.ApiarySkin || Thisitem.skin == config.BeeSkin)
            {
                Item item = null;
                item = ItemManager.CreateByItemID(Thisitem.info.itemid, 1, Thisitem.skin);
                Puts(item.info.shortname);
                if (item != null)
                {
                    Puts(item.info.shortname);
                    Thisitem.amount -= amount;
                    Thisitem.MarkDirty();
                    item.amount = amount;
                    item.OnVirginSpawn();
                    if (Thisitem.hasCondition) item.condition = Thisitem.condition;
                    item.MarkDirty();
                    return item;
                }
            }
         return null;
        }
        void OnServerSave()
        {
            SaveData();

            foreach (var apiary in DataBase.BeeInfo)
            {
                if (apiary.Value.apiarys.Count != 0)
                {
                    foreach (var Find in apiary.Value.apiarys)
                    {
                        if (DateTime.Now.Subtract(Find.Value.Date).TotalHours >= Find.Value.Hours)
                        {
                            if (DateTime.Now.Day == Find.Value.Date.Day && Find.Value.HoneyInDay < config.MaxHoney[$"level{Find.Key}"] * Find.Value.count)
                            {
                                Find.Value.honey += Find.Value.count;
                                Find.Value.Date = DateTime.Now;
                                Find.Value.HoneyInDay += Find.Value.count;
                            }
                            if (DateTime.Now.Day != Find.Value.Date.Day)
                            {
                                Find.Value.Date = DateTime.Now;
                                Find.Value.HoneyInDay = 0;
                            }
                        }
                    }
                }
            }
        }

        void OnUseNPC(BasePlayer npc, BasePlayer player)
        {


            if (npc.userID == config.NPCID)
            {
                DrawNPCCreateUpdateApiary(player);
            }
        }

        #endregion

        #region Functions
        void CheckDb(ulong player)
        {
            if (!DataBase.BeeInfo.ContainsKey(player))
            {
                var data = new BeeBase
                {
                    honey = 0,
                    Rub = 0,
                    apiarys = new Dictionary<int, ApiaryBase>(),
                };
                DataBase.BeeInfo.Add(player, data); SaveData();
            }
        }

        #endregion

        #region Commands

        [ChatCommand("Bee")]
        void OpenGui(BasePlayer player, string command, string[] args)
        {
            DrawMainMenu(player);
        }

        [ChatCommand("GetBee")]
        void GetBee(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            Item x = ItemManager.CreateByPartialName(config.BeeShortName, 1);
            x.skin = config.BeeSkin;
            x.name = "Пчела";
            player.GiveItem(x, BaseEntity.GiveItemReason.PickedUp);
        }
        [ChatCommand("GetAp")]
        void GetAp(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            Item x = ItemManager.CreateByPartialName(config.ApiaryShortName, 1);
            x.skin = config.ApiarySkin;
            x.name = "Пасека";
            player.GiveItem(x, BaseEntity.GiveItemReason.PickedUp);
        }
        [ConsoleCommand("OpenMyApiarys")]
        void OpenMyApiarys(ConsoleSystem.Arg arg)
        {
            DrawApiary(arg.Player());
        }
        [ConsoleCommand("OpenExchange")]
        void OpenExchange(ConsoleSystem.Arg arg)
        {
            DrawExchangeHoney(arg.Player());
        }
        [ConsoleCommand("OpenYourApiary")]
        void OpenYourApiary(ConsoleSystem.Arg arg)
        {
            DrawApiaryMenu(arg.Player(), arg.Args[0]);
        }
        [ConsoleCommand("BackToMenu")]
        void BackToMenu(ConsoleSystem.Arg arg)
        {
            DrawMainMenu(arg.Player());
        }
        [ConsoleCommand("BackToApiarys")]
        void BackToApiarys(ConsoleSystem.Arg arg)
        {
            DrawApiary(arg.Player());
        }
        [ConsoleCommand("BackToMenuNPC")]
        void BackToMenuNPC(ConsoleSystem.Arg arg)
        {
            DrawNPCCreateUpdateApiary(arg.Player());
        }

        [ConsoleCommand("TakeAllHoney")]
        void TakeAllHoney(ConsoleSystem.Arg arg)
        {
            int CheckedHoney = 0;
            foreach (var Select in DataBase.BeeInfo[arg.Player().userID].apiarys)
            {
                if (Select.Value.honey != 0)
                    CheckedHoney += Select.Value.honey;
                Select.Value.honey = 0;
            }
            if (CheckedHoney != 0)
            {
                DataBase.BeeInfo[arg.Player().userID].honey += CheckedHoney;
                SendReply(arg.Player(), $"Вы собрали {CheckedHoney} меда");
                DrawApiary(arg.Player());
            }
            else
                SendReply(arg.Player(), "Пчелы ещё не сделали меда");
        }
        [ConsoleCommand("TakeAllHoneyEx")]
        void TakeAllHoneyEx(ConsoleSystem.Arg arg)
        {
            int CheckedHoney = 0;
            foreach (var Select in DataBase.BeeInfo[arg.Player().userID].apiarys)
            {
                if (Select.Value.honey != 0)
                    CheckedHoney += Select.Value.honey;
                Select.Value.honey = 0;
            }
            if (CheckedHoney != 0)
            {
                DataBase.BeeInfo[arg.Player().userID].honey += CheckedHoney;
                SendReply(arg.Player(), $"Вы собрали {CheckedHoney} меда");
                DrawExchangeHoney(arg.Player());
            }
            else
                SendReply(arg.Player(), "Пчелы ещё не сделали меда");
        }
        [ConsoleCommand("TakeHoney")]
        void TakeHoney(ConsoleSystem.Arg arg)
        {
            int GetHoney = DataBase.BeeInfo[arg.Player().userID].apiarys[int.Parse(arg.Args[0])].honey;
            if (GetHoney != 0)
            {
                DataBase.BeeInfo[arg.Player().userID].honey += GetHoney;
                SendReply(arg.Player(), $"Вы собрали {GetHoney} меда");
                DataBase.BeeInfo[arg.Player().userID].apiarys[int.Parse(arg.Args[0])].honey = 0;
                DrawApiaryMenu(arg.Player(), arg.Args[0]);
            }
            else
            {
                SendReply(arg.Player(), "Пчелы ещё не сделали меда");
            }
        }
        [ConsoleCommand("OpenCreateApiary")]
        void OpenCreateApiary(ConsoleSystem.Arg arg)
        {
            DrawNPCCreateApiary(arg.Player(), int.Parse(arg.Args[0]));
        }
        [ConsoleCommand("OpenUpdateApiary")]
        void OpenUpdateApiary(ConsoleSystem.Arg arg)
        {
            DrawNPCUpdateApiary(arg.Player(), int.Parse(arg.Args[0]));
        }
        [ConsoleCommand("CreateApiarys")]
        void CreateApiarys(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            ItemDefinition ApiaryGetID = ItemManager.FindItemDefinition(config.ApiaryShortName);
            ItemDefinition BeeGetID = ItemManager.FindItemDefinition(config.BeeShortName);
            Item ApiaryCheck = player.inventory.FindItemIDs(ApiaryGetID.itemid).Find(x => x.skin == config.ApiarySkin);
            Item BeeCheck = player.inventory.FindItemIDs(BeeGetID.itemid).Find(x => x.skin == config.BeeSkin);


            if (ApiaryCheck == null && BeeCheck == null)
            {
                SendReply(player, "У вас нету необходимых предметов для создания пасеки");
                return;
            }
            if (ApiaryCheck == null)
            {
                SendReply(player, "У вас нету пасеки");
                return;
            }
            if (BeeCheck == null)
            {
                SendReply(player, "У вас нету пчелы");
                return;
            }
            if (!DataBase.BeeInfo[player.userID].apiarys.ContainsKey(1))
            {
                var Apiary = new ApiaryBase
                {
                    count = 1,
                    honey = 0
                };
                DataBase.BeeInfo[player.userID].apiarys.Add(1, Apiary);
                player.inventory.Take(null, ApiaryCheck.info.itemid, 1);
                player.inventory.Take(null, BeeCheck.info.itemid, 1);
                DrawNPCCreateUpdateApiary(player);
            }
            else
            {
                if (DataBase.BeeInfo[player.userID].apiarys[1].count == config.MaxApiarys[$"level1"])
                {
                    SendReply(player, "У вас максимальное количество пасек первого уровня");
                    return;
                }
                else
                {
                    DataBase.BeeInfo[player.userID].apiarys[1].count++;
                    player.inventory.Take(null, ApiaryCheck.info.itemid, 1);
                    player.inventory.Take(null, BeeCheck.info.itemid, 1);
                    DrawNPCCreateUpdateApiary(player);
                }
            }

        }
        [ConsoleCommand("UpdateApiarys")]
        void UpdateApiarys(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            ItemDefinition ApiaryGetID = ItemManager.FindItemDefinition(config.ApiaryShortName);
            ItemDefinition BeeGetID = ItemManager.FindItemDefinition(config.BeeShortName);
            Item ApiaryCheck = player.inventory.FindItemIDs(ApiaryGetID.itemid).Find(x => x.skin == config.ApiarySkin);
            Item BeeCheck = player.inventory.FindItemIDs(BeeGetID.itemid).Find(x => x.skin == config.BeeSkin);
            int ApiaryLevel = int.Parse(arg.Args[0]);
            if (ApiaryCheck == null && BeeCheck == null)
            {
                SendReply(player, "У вас нету необходимых предметов для создания пасеки");
                return;
            }
            if (ApiaryCheck == null)
            {
                SendReply(player, "У вас нету пасеки");
                return;
            }
            if (BeeCheck == null)
            {
                SendReply(player, "У вас нету пчелы");
                return;
            }
            if (!DataBase.BeeInfo[player.userID].apiarys.ContainsKey(ApiaryLevel - 1))
            {
                SendReply(player, $"У вас нету пасеки {ApiaryLevel-1} уровня");
                return;
            }
            
            if (!DataBase.BeeInfo[player.userID].apiarys.ContainsKey(ApiaryLevel))
            {
                var Apiary = new ApiaryBase
                {
                    count = 1,
                    honey = 0,
                    Date = DateTime.Now,
                    HoneyInDay = 0,
                    Hours = 2,
                };
                DataBase.BeeInfo[player.userID].apiarys.Add(ApiaryLevel, Apiary);
                player.inventory.Take(null, ApiaryCheck.info.itemid, 1);
                player.inventory.Take(null, BeeCheck.info.itemid, 1);
                DataBase.BeeInfo[player.userID].apiarys[ApiaryLevel - 1].count -=1;
                DrawNPCCreateUpdateApiary(player);
            }
            else
            {
                if (DataBase.BeeInfo[player.userID].apiarys[ApiaryLevel].count == config.MaxApiarys[$"level{arg.Args[0]}"])
                {
                    SendReply(player, $"У вас максимальное количество пасек {arg.Args[0]} уровня");
                    return;
                }
                else
                {
                    DataBase.BeeInfo[player.userID].apiarys[ApiaryLevel].count++;
                    player.inventory.Take(null, ApiaryCheck.info.itemid, 1);
                    player.inventory.Take(null, BeeCheck.info.itemid, 1);
                    DataBase.BeeInfo[player.userID].apiarys[ApiaryLevel - 1].count -=1;
                    DrawNPCCreateUpdateApiary(player);
                }

            }
        }

        [ConsoleCommand("ExchangeToShop")]
        void ExchangeToShop(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();

            if (DataBase.BeeInfo[player.userID].Rub == 0)
            {
                SendReply(player, "У вас нечего обменивать");
                return;
            }
            float ChangeTugrik = DataBase.BeeInfo[player.userID].Rub;
            if (config.OvhStore)
            {

                RustStore?.CallHook("APIChangeUserBalance", player.userID, (int)ChangeTugrik, new Action<string>((result) =>
                {
                    if (result == "SUCCESS")
                    {
                        DataBase.BeeInfo[player.userID].Rub = 0;
                        SaveData();
                        PrintWarning($"Игрок {player.displayName} успешно получил {ChangeTugrik} рублей");
                        DrawExchangeHoney(player);
                        return;
                    }
                    Interface.Oxide.LogDebug($"Баланс не был изменен, ошибка: {result}");
                }));

            }
            if (config.GameStore)
            {

                string url = $"http://panel.gamestores.ru/api?shop_id={config.GSId}&secret={config.GSApi}&action=moneys&type=plus&steam_id={player.UserIDString}&amount={ChangeTugrik}&mess=Обмен тугриков на рубли";
                webrequest.EnqueueGet(url, (i, s) =>
                {
                    if (i != 200)
                    {
                        PrintError($"Ошибка соединения с сайтом GS!");
                    }
                    else
                    {
                        JObject jObject = JObject.Parse(s);
                        if (jObject["result"].ToString() == "fail")
                        {
                            PrintError($"Ошибка пополнения баланса для {player.displayName}!");
                            PrintError($"Причина: {jObject["message"].ToString()}");
                        }
                        else
                        {
                            DataBase.BeeInfo[player.userID].Rub = 0;
                            SaveData();
                            PrintWarning($"Игрок {player.displayName} успешно получил {ChangeTugrik} рублей");
                            DrawExchangeHoney(player);
                        }

                    }
                }, this);
            }

        }
        [ConsoleCommand("SellHoney")]
        void SellHoney(ConsoleSystem.Arg arg)
        { BeeBase PlayerDB = DataBase.BeeInfo[arg.Player().userID];
            if (PlayerDB.honey != 0)
            {
                float GetCourse = (float)PlayerDB.honey * config.CourseRub;
                PlayerDB.Rub += GetCourse;
                SendReply(arg.Player(), $"Вы успешно продали {PlayerDB.honey} меда и получили {string.Format("{0:0.00}", GetCourse)} рублей");
                PlayerDB.honey = 0;
                DrawExchangeHoney(arg.Player());
            }
            else
            {
                SendReply(arg.Player(), "У вас нету меда для продажи");
                return;
            }
        }
        #endregion

        #region Gui
        void DrawMainMenu(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "BeeMainGui");
            var BeeMainGui = new CuiElementContainer();
            BeeMainGui.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0.4",
                        Material = "assets/content/ui/uibackgroundblur.mat" },
                CursorEnabled = true,

                RectTransform = { AnchorMax = "1 1", AnchorMin = "0 0" }
            }, "Overlay", "BeeMainGui");
            BeeMainGui.Add(new CuiButton
            {
                Button = { Close = "BeeMainGui", Color = "0 0 0 0" },
                Text = { Text = "" },
                RectTransform = { AnchorMax = "1 1", AnchorMin = "0 0" }
            }, "BeeMainGui");
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui",
                Name = "BeeMainGui" + "BackGround",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#EDEDEDF8")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "448 -560",
                        OffsetMax = "860 -120"
                    }
                }
            });
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui" + "BackGround",
                Components =
                {
                    new CuiRawImageComponent()
                    {
                        Color = "1 1 1 1",
                        Png = (string)ImageLibrary?.Call("GetImage", "beelogo")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "1 0",
                        AnchorMax = "1 0",
                        OffsetMin = "-200 20",
                        OffsetMax = "-20 200"
                    }
                }
            });
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui" + "BackGround",
                Name = "BeeMainGui" + "BackGround"+"Header",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#F9C901FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "0 -25",
                        OffsetMax = "412 0"
                    }
                }
            });
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui" + "BackGround" + "Header",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = "ПЧЕЛОВОД",
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#474747FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "0 -25",
                        OffsetMax = "412 0"
                    }
                }
            });
            BeeMainGui.Add(new CuiButton
            {
                Button = { Command = "OpenMyApiarys", Color = HexToRustFormat("#F9C901FF") },
                Text = { Text = "Мои Пасеки", Align = TextAnchor.MiddleCenter,FontSize = 22,Color = HexToRustFormat("#474747FF") },
                RectTransform = { 
                    AnchorMax = "0 1",
                    AnchorMin = "0 1",
                    OffsetMin = "10 -90",
                    OffsetMax = "150 -60"
                }
            }, "BeeMainGui" + "BackGround");

            BeeMainGui.Add(new CuiButton
            {
                Button = { Command = "OpenExchange", Color = HexToRustFormat("#F9C901FF") },
                Text = { Text = "Продать Мед", Align = TextAnchor.MiddleCenter, FontSize = 22, Color = HexToRustFormat("#474747FF") },
                RectTransform = {
                    AnchorMin = "0 1",
                    AnchorMax = "0 1",
                    OffsetMin = "10 -130",
                    OffsetMax = "150 -100"
                }
            }, "BeeMainGui" + "BackGround");

            BeeMainGui.Add(new CuiButton
            {
                Button = { Close = "BeeMainGui", Color = HexToRustFormat("#F9C901FF") },
                Text = { Text = "Закрыть",Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#474747FF") },
                RectTransform = {
                    AnchorMin = "0 1",
                    AnchorMax = "0 1",
                    OffsetMin = "10 -430",
                    OffsetMax = "100 -410"
                }
            }, "BeeMainGui" + "BackGround");
            CuiHelper.AddUi(player, BeeMainGui);
        }
        void DrawApiary(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "BeeMainGui" + "BackGround");
            var BeeMainGui = new CuiElementContainer();
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui",
                Name = "BeeMainGui" + "BackGround",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#EDEDEDF8")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "448 -560",
                        OffsetMax = "860 -120"
                    }
                }
            });

            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui" + "BackGround",
                Name = "BeeMainGui" + "BackGround" + "Header",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#F9C901FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "0 -25",
                        OffsetMax = "412 0"
                    }
                }
            });
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui" + "BackGround" + "Header",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = "МОИ ПАСЕКИ",
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#474747FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                    }
                }
            });

            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui" + "BackGround",
                Name = "BeeMainGui" + "BackGround" + "YourHoneys",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#F9C901FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "250 -430",
                        OffsetMax = "400 -410"
                    }
                }
            });
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui" + "BackGround" + "YourHoneys",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = $"Собрано мёда {DataBase.BeeInfo[player.userID].honey}",
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#474747FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                    }
                }
            });
            if (DataBase.BeeInfo[player.userID].apiarys.ContainsKey(1))
            {
                BeeMainGui.Add(new CuiElement
                {
                    Parent = "BeeMainGui" + "BackGround",
                    Name = "BeeMainGui" + "BackGround" + "Apiary1",
                    Components =
                {
                    new CuiImageComponent()
                    {
                        Color = "0.5 0.5 0.5 0.6"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "20 -150",
                        OffsetMax = "120 -50"
                    }
                }
                });
                BeeMainGui.Add(new CuiElement
                {
                    Parent = "BeeMainGui" + "BackGround" + "Apiary1",
                    Components =
                {
                    new CuiRawImageComponent()
                    {
                        Color = "1 1 1 1",
                        Png = (string)ImageLibrary?.Call("GetImage", "paseka")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.1 0.1",
                        AnchorMax = "0.9 0.9",
                    }
                }
                });
                BeeMainGui.Add(new CuiButton
                {
                    Button = { Command = "OpenYourApiary 1",Color = "0 0 0 0"},
                    Text = { Text = "Пасека LvL 1",Align = TextAnchor.LowerCenter ,Color = HexToRustFormat("#474747FF") },
                    RectTransform = {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                }
                }, "BeeMainGui" + "BackGround" + "Apiary1");
            }
            else
            {
                BeeMainGui.Add(new CuiElement
                {
                    Parent = "BeeMainGui" + "BackGround",
                    Name = "BeeMainGui" + "BackGround" + "Apiary1",
                    Components =
                {
                    new CuiImageComponent()
                    {
                        Color = "0.5 0.5 0.5 0.6"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "20 -150",
                        OffsetMax = "120 -50"
                    }
                }
                });
            }


            if (DataBase.BeeInfo[player.userID].apiarys.ContainsKey(2))
            {
                BeeMainGui.Add(new CuiElement
                {
                    Parent = "BeeMainGui" + "BackGround",
                    Name = "BeeMainGui" + "BackGround" + "Apiary2",
                    Components =
                    {
                        new CuiImageComponent()
                        {
                            Color = "0.5 0.5 0.5 0.6"
                        },
                        new CuiRectTransformComponent()
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = "155 -150",
                            OffsetMax = "255 -50"
                        }
                    }
                });
                BeeMainGui.Add(new CuiElement
                {
                    Parent = "BeeMainGui" + "BackGround" + "Apiary2",
                    Components =
                {
                    new CuiRawImageComponent()
                    {
                        Png = (string)ImageLibrary.Call("GetImage", "paseka")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.1 0.1",
                        AnchorMax = "0.9 0.9",
                    }
                }
                });
                BeeMainGui.Add(new CuiButton
                {
                    Button = { Command = "OpenYourApiary 2", Color = "0 0 0 0" },
                    Text = { Text = "Пасека LvL 2", Align = TextAnchor.LowerCenter,Color = HexToRustFormat("#474747FF") },
                    RectTransform = {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                }
                }, "BeeMainGui" + "BackGround" + "Apiary2");
            }
            else
            {
                BeeMainGui.Add(new CuiElement
                {
                    Parent = "BeeMainGui" + "BackGround",
                    Name = "BeeMainGui" + "BackGround" + "Apiary2",
                    Components =
                    {
                        new CuiImageComponent()
                        {
                            Color = "0.5 0.5 0.5 0.6"
                        },
                        new CuiRectTransformComponent()
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = "155 -150",
                            OffsetMax = "255 -50"
                        }
                    }
                });
            }

            if (DataBase.BeeInfo[player.userID].apiarys.ContainsKey(3))
            {
                BeeMainGui.Add(new CuiElement
                {
                    Parent = "BeeMainGui" + "BackGround",
                    Name = "BeeMainGui" + "BackGround" + "Apiary3",
                    Components =
                {
                    new CuiImageComponent()
                    {
                        Color = "0.5 0.5 0.5 0.6"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "290 -150",
                        OffsetMax = "390 -50"
                    }
                }
                });
                BeeMainGui.Add(new CuiElement
                {
                    Parent = "BeeMainGui" + "BackGround" + "Apiary3",
                    Components =
                {
                    new CuiRawImageComponent()
                    {
                        Png = (string)ImageLibrary.Call("GetImage", "paseka")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.1 0.1",
                        AnchorMax = "0.9 0.9",
                    }
                }
                });
                BeeMainGui.Add(new CuiButton
                {
                    Button = { Command = "OpenYourApiary 3", Color = "0 0 0 0" },
                    Text = { Text = "Пасека LvL 3", Align = TextAnchor.LowerCenter,Color = HexToRustFormat("#474747FF") },
                    RectTransform = {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                }
                }, "BeeMainGui" + "BackGround" + "Apiary3");
              
            }
            else
            {
                BeeMainGui.Add(new CuiElement
                {
                    Parent = "BeeMainGui" + "BackGround",
                    Name = "BeeMainGui" + "BackGround" + "Apiary3",
                    Components =
                {
                    new CuiImageComponent()
                    {
                        Color = "0.5 0.5 0.5 0.6"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "290 -150",
                        OffsetMax = "390 -50"
                    }
                }
                });
            }

            if (DataBase.BeeInfo[player.userID].apiarys.ContainsKey(4))
            {
                BeeMainGui.Add(new CuiElement
                {
                    Parent = "BeeMainGui" + "BackGround",
                    Name = "BeeMainGui" + "BackGround" + "Apiary4",
                    Components =
                {
                    new CuiImageComponent()
                    {
                        Color = "0.5 0.5 0.5 0.6"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "70 -270",
                        OffsetMax = "170 -170"
                    }
                }
                });
                BeeMainGui.Add(new CuiElement
                {
                    Parent = "BeeMainGui" + "BackGround" + "Apiary4",
                    Components =
                {
                    new CuiRawImageComponent()
                    {
                        Png = (string)ImageLibrary.Call("GetImage", "paseka")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.1 0.1",
                        AnchorMax = "0.9 0.9",
                    }
                }
                });
                BeeMainGui.Add(new CuiButton
                {
                    Button = { Command = "OpenYourApiary 4", Color = "0 0 0 0" },
                    Text = { Text = "Пасека LvL 4", Align = TextAnchor.LowerCenter, Color = HexToRustFormat("#474747FF") },
                    RectTransform = {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                }
                }, "BeeMainGui" + "BackGround" + "Apiary4");
            }
            else
            {
                BeeMainGui.Add(new CuiElement
                {
                    Parent = "BeeMainGui" + "BackGround",
                    Name = "BeeMainGui" + "BackGround" + "Apiary4",
                    Components =
                {
                    new CuiImageComponent()
                    {
                        Color = "0.5 0.5 0.5 0.6"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "70 -270",
                        OffsetMax = "170 -170"
                    }
                }
                });
            }

            if (DataBase.BeeInfo[player.userID].apiarys.ContainsKey(5))
            {
                BeeMainGui.Add(new CuiElement
                {
                    Parent = "BeeMainGui" + "BackGround",
                    Name = "BeeMainGui" + "BackGround" + "Apiary5",
                    Components =
                {
                    new CuiImageComponent()
                    {
                        Color = "0.5 0.5 0.5 0.6"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "240 -270",
                        OffsetMax = "340 -170"
                    }
                }
                });
                BeeMainGui.Add(new CuiElement
                {
                    Parent = "BeeMainGui" + "BackGround" + "Apiary5",
                    Components =
                {
                    new CuiRawImageComponent()
                    {
                        Png = (string)ImageLibrary.Call("GetImage", "paseka")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.1 0.1",
                        AnchorMax = "0.9 0.9",
                    }
                }
                });
                BeeMainGui.Add(new CuiButton
                {
                    Button = { Command = "OpenYourApiary 5", Color = "0 0 0 0.0" },
                    Text = { Text = "Пасека LvL 5", Align = TextAnchor.LowerCenter,Color = HexToRustFormat("#474747FF") },
                    RectTransform = {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                }
                }, "BeeMainGui" + "BackGround" + "Apiary5");
            }
            else
            {
                BeeMainGui.Add(new CuiElement
                {
                    Parent = "BeeMainGui" + "BackGround",
                    Name = "BeeMainGui" + "BackGround" + "Apiary5",
                    Components =
                {
                    new CuiImageComponent()
                    {
                        Color = "0.5 0.5 0.5 0.6"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "240 -270",
                        OffsetMax = "340 -170"
                    }
                }
                });
            }

            BeeMainGui.Add(new CuiButton
            {
                Button = { Command = "TakeAllHoney", Color = HexToRustFormat("#F9C901FF") },
                Text = { Text = "Собрать весь мед", Align = TextAnchor.MiddleCenter, FontSize = 20, Color = HexToRustFormat("#474747FF") },
                RectTransform = {
                        AnchorMin = "0 0",
                        AnchorMax = "0 0",
                        OffsetMin = "100 120",
                        OffsetMax = "300 150"
                }
            }, "BeeMainGui" + "BackGround");

            BeeMainGui.Add(new CuiButton
            {
                Button = { Command = "BackToMenu", Color = HexToRustFormat("#F9C901FF") },
                Text = { Text = "Назад",Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#474747FF") },
                RectTransform = { 
                    AnchorMax = "0 1", 
                    AnchorMin = "0 1",
                    OffsetMin = "10 -430",
                    OffsetMax = "100 -410",}
            }, "BeeMainGui" + "BackGround");
            CuiHelper.AddUi(player, BeeMainGui);
        }

        void DrawApiaryMenu(BasePlayer player, string level)
        {
            CuiHelper.DestroyUi(player, "BeeMainGui" + "BackGround");
            var BeeMainGui = new CuiElementContainer();
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui",
                Name = "BeeMainGui" + "BackGround",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#EDEDEDF8")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "448 -560",
                        OffsetMax = "860 -120"
                    }
                }
            });
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui" + "BackGround",
                Name = "BeeMainGui" + "BackGround" + "Header",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#F9C901FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "0 -25",
                        OffsetMax = "412 0"
                    }
                }
            });
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui" + "BackGround" + "Header",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = $"ПАСЕКА УРОВЕНЬ {level}",
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#474747FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                    }
                }
            });



            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui" + "BackGround",
                Name = "BeeMainGui" + "BackGround" + "Apiarys",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#F9C901FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "10 -90",
                        OffsetMax = "200 -60"
                    }
                }
            });
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui" + "BackGround" + "Apiarys",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = $"Количество пасек {DataBase.BeeInfo[player.userID].apiarys[int.Parse(level)].count}",
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#474747FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                    }
                }
            });



            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui" + "BackGround",
                Name = "BeeMainGui" + "BackGround" + "Honeys",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#F9C901FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "10 -130",
                        OffsetMax = "200 -100"
                    }
                }
            });
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui" + "BackGround" + "Honeys",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = $"Количество Меда {DataBase.BeeInfo[player.userID].apiarys[int.Parse(level)].honey}",
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#474747FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                    }
                }
            });


            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui" + "BackGround",
                Name = "BeeMainGui" + "BackGround" + "CountHoneyInDAY",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#F9C901FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "10 -170",
                        OffsetMax = "200 -140"
                    }
                }
            });

            BeeMainGui.Add(new CuiElement
            {
               Parent = "BeeMainGui" + "BackGround" + "CountHoneyInDAY",
               Components =
               {
                   new CuiTextComponent()
                   {
                       Text = $"Производит {config.MaxHoney[$"level{level}"]*DataBase.BeeInfo[player.userID].apiarys[int.Parse(level)].count} меда в 24ч",
                       Align = TextAnchor.MiddleCenter,
                       Color = HexToRustFormat("#474747FF")
                   },
                   new CuiRectTransformComponent()
                   {
                       AnchorMin = "0 0",
                       AnchorMax = "1 1",
                   }
               }
            });
 

            BeeMainGui.Add(new CuiButton
            {
                Button = { Command = "TakeHoney 1", Color = HexToRustFormat("#F9C901FF") },
                Text = { Text = "Собрать мед", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#474747FF") },
                RectTransform = {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "280 -130",
                        OffsetMax = "400 -100"}
            }, "BeeMainGui" + "BackGround");

            BeeMainGui.Add(new CuiButton
            {
                Button = { Command = "BackToApiarys", Color = HexToRustFormat("#F9C901FF") },
                Text = { Text = "Назад", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#474747FF") },
                RectTransform = {
                    AnchorMax = "0 1",
                    AnchorMin = "0 1",
                    OffsetMin = "10 -430",
                    OffsetMax = "100 -410",}
            }, "BeeMainGui" + "BackGround");

            CuiHelper.AddUi(player, BeeMainGui);
        }

        void DrawExchangeHoney(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "BeeMainGui" + "BackGround");
            var BeeMainGui = new CuiElementContainer();
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui",
                Name = "BeeMainGui" + "BackGround",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#EDEDEDF8")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "448 -560",
                        OffsetMax = "860 -120"
                    }
                }
            });
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui" + "BackGround",
                Name = "BeeMainGui" + "BackGround" + "Header",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#F9C901FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "0 -25",
                        OffsetMax = "412 0"
                    }
                }
            });
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui" + "BackGround" + "Header",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = "ПРОДАЖА МЕДА",
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#474747FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                    }
                }
            });


            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui" + "BackGround",
                Name = "BeeMainGui" + "BackGround" + "YourHoney",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#F9C901FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "10 -90",
                        OffsetMax = "200 -60"
                    }
                }
            });
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui" + "BackGround" + "YourHoney",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = $"Собрано меда {DataBase.BeeInfo[player.userID].honey}",
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#474747FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                    }
                }
            });

            int FindHoney = 0;
            foreach(var honey in DataBase.BeeInfo[player.userID].apiarys)
            {
                if (honey.Value.honey != 0)
                    FindHoney += honey.Value.honey;
            }
            
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui" + "BackGround",
                Name = "BeeMainGui" + "BackGround" + "HoneysInApiary",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#F9C901FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "10 -130",
                        OffsetMax = "200 -100"
                    }
                }
            });
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui" + "BackGround" + "HoneysInApiary",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = $"Меда в пасиках {FindHoney}",
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#474747FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                    }
                }
            });


            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui" + "BackGround",
                Name = "BeeMainGui" + "BackGround" + "Course",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#F9C901FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "10 -170",
                        OffsetMax = "200 -140"
                    }
                }
            });
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui" + "BackGround" + "Course",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = $"Курс: 1 ед меда = {string.Format("{0:0.00}", config.CourseRub)} Руб",
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#474747FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                    }
                }
            });

            BeeMainGui.Add(new CuiButton
            {
                Button = { Command = "SellHoney", Color = HexToRustFormat("#F9C901FF") },
                Text = { Text = "Продать мед", Align = TextAnchor.MiddleCenter, FontSize = 17,Color = HexToRustFormat("#474747FF") },
                RectTransform = {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "10 -210",
                        OffsetMax = "200 -180"
                }
            }, "BeeMainGui" + "BackGround");


            BeeMainGui.Add(new CuiButton
            {
                Button = { Command = "TakeAllHoneyEx", Color = HexToRustFormat("#F9C901FF") },
                Text = { Text = "Собрать мед", Align = TextAnchor.MiddleCenter, FontSize = 17, Color = HexToRustFormat("#474747FF") },
                RectTransform = {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "250 -130",
                        OffsetMax = "400 -100"
                }
            }, "BeeMainGui" + "BackGround");
          

            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui" + "BackGround",
                Name = "BeeMainGui" + "BackGround" + "MoneyInBalance",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#F9C901FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "10 -300",
                        OffsetMax = "200 -270"
                    }
                }
            });
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui" + "BackGround" + "MoneyInBalance",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = $"Рублей на балансе {string.Format("{0:0.00}",DataBase.BeeInfo[player.userID].Rub)}",
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#474747FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                    }
                }
            });

            BeeMainGui.Add(new CuiButton
            {
                Button = { Command = "ExchangeToShop", Color = HexToRustFormat("#F9C901FF") },
                Text = { Text = "Вывести в магазин", Align = TextAnchor.MiddleCenter, FontSize = 17, Color = HexToRustFormat("#474747FF") },
                RectTransform = {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "250 -300",
                        OffsetMax = "400 -270"
                }
            }, "BeeMainGui" + "BackGround");


            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui" + "BackGround",
                Name = "BeeMainGui" + "BackGround" + "AtentionText",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = "0 0 0 0.0"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "10 -390",
                        OffsetMax = "400 -310"
                    }
                }
            });
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGui" + "BackGround" + "AtentionText",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = $"Внимание\nЧто бы деньги пришли на баланс, нужно быть авторизированным в магазине",
                        FontSize = 20,
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#474747FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                    }
                }
            });

            BeeMainGui.Add(new CuiButton
            {
                Button = { Command = "BackToMenu", Color = HexToRustFormat("#F9C901FF") },
                Text = { Text = "Назад", Align = TextAnchor.MiddleCenter,Color = HexToRustFormat("#474747FF") },
                RectTransform = {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "10 -430",
                        OffsetMax = "100 -410"
                }
            }, "BeeMainGui" + "BackGround");
            CuiHelper.AddUi(player, BeeMainGui);
        }

        void DrawNPCCreateUpdateApiary(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "BeeMainGuiNPC");
            var BeeMainGui = new CuiElementContainer();
            BeeMainGui.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0.4",
                        Material = "assets/content/ui/uibackgroundblur.mat" },
                CursorEnabled = true,

                RectTransform = { AnchorMax = "1 1", AnchorMin = "0 0" }
            }, "Overlay", "BeeMainGuiNPC");
            BeeMainGui.Add(new CuiButton
            {
                Button = { Close = "BeeMainGuiNPC", Color = "0 0 0 0" },
                Text = { Text = "" },
                RectTransform = { AnchorMax = "1 1", AnchorMin = "0 0" }
            }, "BeeMainGuiNPC");
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGuiNPC",
                Name = "BeeMainGuiNPC" + "BackGround",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#EDEDEDF8")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "448 -560",
                        OffsetMax = "860 -120"
                    }
                }
            });
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGuiNPC" + "BackGround",
                Name = "BeeMainGuiNPC" + "BackGround" + "Header",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#F9C901FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "0 -25",
                        OffsetMax = "412 0"
                    }
                }
            });
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGuiNPC" + "BackGround" + "Header",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = "СОЗДАНИЕ/ОБНОВЛЕНИЕ ПАСЕК",
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#474747FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                    }
                }
            });

            BeeMainGui.Add(new CuiElement
                {
                    Parent = "BeeMainGuiNPC" + "BackGround",
                    Name = "BeeMainGuiNPC" + "BackGround" + "Apiary1",
                    Components =
                {
                    new CuiImageComponent()
                    {
                        Color = "1 1 1 0.6"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "20 -150",
                        OffsetMax = "120 -50"
                    }
                }
                });
                BeeMainGui.Add(new CuiButton
                {
                    Button = { Command = "OpenCreateApiary 1", Color = HexToRustFormat("#F9C901FF") },
                    Text = { Text = "Создать пасеку\n 1 уровня", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#474747FF") },
                    RectTransform = {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1",
                }
                }, "BeeMainGuiNPC" + "BackGround" + "Apiary1");
            

                BeeMainGui.Add(new CuiElement
                {
                    Parent = "BeeMainGuiNPC" + "BackGround",
                    Name = "BeeMainGuiNPC" + "BackGround" + "Apiary2",
                    Components =
                    {
                        new CuiImageComponent()
                        {
                            Color = "1 1 1 0.6"
                        },
                        new CuiRectTransformComponent()
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = "155 -150",
                            OffsetMax = "255 -50"
                        }
                    }
                });
            if (!DataBase.BeeInfo[player.userID].apiarys.ContainsKey(1))
            {

            }
            else
            {
                BeeMainGui.Add(new CuiButton
                {
                    Button = { Command = "OpenUpdateApiary 2", Color = HexToRustFormat("#F9C901FF") },
                    Text = { Text = "Обновит пасеку\n до 2 уровня", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#474747FF") },
                    RectTransform = {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1",
                }
            }, "BeeMainGuiNPC" + "BackGround" + "Apiary2");
        }
                BeeMainGui.Add(new CuiElement
                {
                    Parent = "BeeMainGuiNPC" + "BackGround",
                    Name = "BeeMainGuiNPC" + "BackGround" + "Apiary3",
                    Components =
                {
                    new CuiImageComponent()
                    {
                        Color = "1 1 1 0.6"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "290 -150",
                        OffsetMax = "390 -50"
                    }
                }
                });
            if (!DataBase.BeeInfo[player.userID].apiarys.ContainsKey(2))
            {

            }
            else
            {
                BeeMainGui.Add(new CuiButton
                {
                    Button = { Command = "OpenUpdateApiary 3", Color = HexToRustFormat("#F9C901FF") },
                    Text = { Text = "Обновит пасеку\n до 3 уровня", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#474747FF") },
                    RectTransform = {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1",
                }
                }, "BeeMainGuiNPC" + "BackGround" + "Apiary3");
            }
            BeeMainGui.Add(new CuiElement
                {
                    Parent = "BeeMainGuiNPC" + "BackGround",
                    Name = "BeeMainGuiNPC" + "BackGround" + "Apiary4",
                    Components =
                {
                    new CuiImageComponent()
                    {
                        Color = "1 1 1 0.6"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "70 -270",
                        OffsetMax = "170 -170"
                    }
                }
                });
            if (!DataBase.BeeInfo[player.userID].apiarys.ContainsKey(3))
            {

            }
            else
            {
                BeeMainGui.Add(new CuiButton
                {
                    Button = { Command = "OpenUpdateApiary 4", Color = HexToRustFormat("#F9C901FF") },
                    Text = { Text = "Обновит пасеку\n до 4 уровня", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#474747FF") },
                    RectTransform = {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1",
                }
                }, "BeeMainGuiNPC" + "BackGround" + "Apiary4");
            }
                BeeMainGui.Add(new CuiElement
                {
                    Parent = "BeeMainGuiNPC" + "BackGround",
                    Name = "BeeMainGuiNPC" + "BackGround" + "Apiary5",
                    Components =
                {
                    new CuiImageComponent()
                    {
                        Color = "1 1 1 0.6"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "240 -270",
                        OffsetMax = "340 -170"
                    }
                }
              });
            if (!DataBase.BeeInfo[player.userID].apiarys.ContainsKey(4))
            {

            }
            else
            {
                BeeMainGui.Add(new CuiButton
                {
                    Button = { Command = "OpenUpdateApiary 5", Color = HexToRustFormat("#F9C901FF") },
                    Text = { Text = "Обновит пасеку\n до 5 уровня", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#474747FF") },
                    RectTransform = {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1",
                }
                }, "BeeMainGuiNPC" + "BackGround" + "Apiary5");
            }
                BeeMainGui.Add(new CuiButton
            {
                Button = { Close = "BeeMainGuiNPC", Color = HexToRustFormat("#F9C901FF") },
                Text = { Text = "Закрыть", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#474747FF") },
                RectTransform = {
                    AnchorMin = "0 1",
                    AnchorMax = "0 1",
                    OffsetMin = "10 -430",
                    OffsetMax = "100 -410"
                }
            }, "BeeMainGuiNPC" + "BackGround");
            CuiHelper.AddUi(player, BeeMainGui);
        }
        void DrawNPCCreateApiary(BasePlayer player,int level)
        {
            ItemDefinition ApiaryGetID = ItemManager.FindItemDefinition(config.ApiaryShortName);
            ItemDefinition BeeGetID = ItemManager.FindItemDefinition(config.BeeShortName);
            Item ApiaryCheck = player.inventory.FindItemIDs(ApiaryGetID.itemid).Find(x => x.skin == config.ApiarySkin);
            Item BeeCheck = player.inventory.FindItemIDs(BeeGetID.itemid).Find(x => x.skin == config.BeeSkin);
            CuiHelper.DestroyUi(player, "BeeMainGuiNPC" + "BackGround");
            var BeeMainGui = new CuiElementContainer();
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGuiNPC",
                Name = "BeeMainGuiNPC" + "BackGround",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#EDEDEDF8")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "448 -560",
                        OffsetMax = "860 -120"
                    }
                }
            });
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGuiNPC" + "BackGround",
                Name = "BeeMainGuiNPC" + "BackGround" + "Header",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#F9C901FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "0 -25",
                        OffsetMax = "412 0"
                    }
                }
            });
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGuiNPC" + "BackGround" + "Header",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = $"СОЗДАНИЕ ПАСЕКИ УРОВНЯ {level}",
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#474747FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                    }
                }
            });
            
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGuiNPC" + "BackGround",
                Name = "BeeMainGuiNPC" + "BackGround" + "YouNeed",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#F9C901FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "10 -90",
                        OffsetMax = "400 -60"
                    }
                }
            });
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGuiNPC" + "BackGround" + "YouNeed",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = $"Для создание пасеки вам потребуется: 1 пчела, 1 пасека",
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#474747FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                    }
                }
            });
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGuiNPC" + "BackGround",
                Name = "BeeMainGuiNPC" + "BackGround" + "YouHaveBee",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#F9C901FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "10 -130",
                        OffsetMax = "200 -100"
                    }
                }
            });
            int GetBee = 0;
            if (BeeCheck != null) GetBee = BeeCheck.amount;
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGuiNPC" + "BackGround" + "YouHaveBee",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = $"Увас есть пчел: {GetBee}",
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#474747FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                    }
                }
            });

            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGuiNPC" + "BackGround",
                Name = "BeeMainGuiNPC" + "BackGround" + "YouHaveApiary",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#F9C901FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "10 -170",
                        OffsetMax = "200 -140"
                    }
                }
            });
            int GetApiary = 0;
            if (ApiaryCheck != null) GetApiary = ApiaryCheck.amount;
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGuiNPC" + "BackGround" + "YouHaveApiary",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = $"Увас есть пасек: {GetApiary}",
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#474747FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                    }
                }
            });
            BeeMainGui.Add(new CuiButton
            {
                Button = { Command = $"CreateApiarys {level}", Color = HexToRustFormat("#F9C901FF") },
                Text = { Text = "Создать пасеку", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#474747FF") },
                RectTransform = {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "130 -210",
                        OffsetMax = "300 -180"
                }
            }, "BeeMainGuiNPC" + "BackGround");
          
            BeeMainGui.Add(new CuiButton
            {
                Button = { Command = "BackToMenuNPC", Color = HexToRustFormat("#F9C901FF") },
                Text = { Text = "Назад", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#474747FF") },
                RectTransform = {
                    AnchorMin = "0 1",
                    AnchorMax = "0 1",
                    OffsetMin = "10 -430",
                    OffsetMax = "100 -410"
                }
            }, "BeeMainGuiNPC" + "BackGround");
            CuiHelper.AddUi(player, BeeMainGui);
        }

        void DrawNPCUpdateApiary(BasePlayer player, int level)
        {
            ItemDefinition ApiaryGetID = ItemManager.FindItemDefinition(config.ApiaryShortName);
            ItemDefinition BeeGetID = ItemManager.FindItemDefinition(config.BeeShortName);
            Item ApiaryCheck = player.inventory.FindItemIDs(ApiaryGetID.itemid).Find(x => x.skin == config.ApiarySkin);
            Item BeeCheck = player.inventory.FindItemIDs(BeeGetID.itemid).Find(x => x.skin == config.BeeSkin);
            CuiHelper.DestroyUi(player, "BeeMainGuiNPC" + "BackGround");
            var BeeMainGui = new CuiElementContainer();
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGuiNPC",
                Name = "BeeMainGuiNPC" + "BackGround",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#EDEDEDF8")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "448 -560",
                        OffsetMax = "860 -120"
                    }
                }
            });

            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGuiNPC" + "BackGround",
                Name = "BeeMainGuiNPC" + "BackGround" + "Header",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#F9C901FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "0 -25",
                        OffsetMax = "412 0"
                    }
                }
            });
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGuiNPC" + "BackGround" + "Header",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = $"ОБНОВЛЕНИЕ ПАСЕКИ ДО УРОВНЯ {level}",
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#474747FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                    }
                }
            });

            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGuiNPC" + "BackGround",
                Name = "BeeMainGuiNPC" + "BackGround" + "YouNeed",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#F9C901FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "10 -90",
                        OffsetMax = "400 -40"
                    }
                }
            });
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGuiNPC" + "BackGround" + "YouNeed",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = $"Для обновления вам потребуется:\n 1 пчела, 1 пасека, 1 пасека {level-1} уровня",
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#474747FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                    }
                }
            });

            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGuiNPC" + "BackGround",
                Name = "BeeMainGuiNPC" + "BackGround" + "YouHaveBee",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#F9C901FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "10 -130",
                        OffsetMax = "200 -100"
                    }
                }
            });
            int GetBee = 0;
            if (BeeCheck != null) GetBee = BeeCheck.amount;
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGuiNPC" + "BackGround" + "YouHaveBee",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = $"Увас есть пчел: {GetBee}",
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#474747FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                    }
                }
            });

            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGuiNPC" + "BackGround",
                Name = "BeeMainGuiNPC" + "BackGround" + "YouHaveApiary",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#F9C901FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "10 -170",
                        OffsetMax = "200 -140"
                    }
                }
            });
            int GetApiary = 0;
            if (ApiaryCheck != null) GetApiary = ApiaryCheck.amount;
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGuiNPC" + "BackGround" + "YouHaveApiary",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = $"Увас есть пасек: {GetApiary}",
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#474747FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                    }
                }
            });


            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGuiNPC" + "BackGround",
                Name = "BeeMainGuiNPC" + "BackGround" + "YouHaveApiaryLevel",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#F9C901FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "10 -210",
                        OffsetMax = "200 -180"
                    }
                }
            });
            BeeMainGui.Add(new CuiElement
            {
                Parent = "BeeMainGuiNPC" + "BackGround" + "YouHaveApiaryLevel",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = $"Увас есть пасек {level-1} lvl: {DataBase.BeeInfo[player.userID].apiarys[level-1].count}",
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#474747FF")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                    }
                }
            });
            BeeMainGui.Add(new CuiButton
            {
                Button = { Command = $"UpdateApiarys {level}", Color = HexToRustFormat("#F9C901FF") },
                Text = { Text = "Обновить пасеку", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#474747FF") },
                RectTransform = {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "130 -250",
                        OffsetMax = "300 -220"
                }
            }, "BeeMainGuiNPC" + "BackGround");

            BeeMainGui.Add(new CuiButton
            {
                Button = { Command = "BackToMenuNPC", Color = HexToRustFormat("#F9C901FF") },
                Text = { Text = "Назад", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#474747FF") },
                RectTransform = {
                    AnchorMin = "0 1",
                    AnchorMax = "0 1",
                    OffsetMin = "10 -430",
                    OffsetMax = "100 -410"
                }
            }, "BeeMainGuiNPC" + "BackGround");
            CuiHelper.AddUi(player, BeeMainGui);
        }
        #endregion

        #region helpers
        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');

            if (str.Length == 6)
                str += "FF";

            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);

            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }
        #endregion
    }
}
