// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Random = Oxide.Core.Random;

namespace Oxide.Plugins
{
    [Info("MagicCoin", "CASHR", "2.0.2")]
    internal class MagicCoin : RustPlugin
    {
        #region Static

        private readonly string Layer = "UI_MAGICCOIN_MAINGUI";
        private PluginConfig _config;
        [PluginReference] private Plugin ImageLibrary;

        #endregion

        #region Config

        public class PluginConfig
        {
            [JsonProperty("Настройка монеты || Coin Settings")]
            public CoinSettings Coin;

            [JsonProperty("Настройка вывода в плагин экономики || Setting up output in the Economy plugin")]
            public Economics econom;

            [JsonProperty("Настройка вывода в Геймстор || Setting up output to Gamestore")]
            public GameStores GS;

            [JsonProperty("Количество монет, который игрок может обменять в день? || The number of coins a player can exchange per day?")]
            public int MaxOut;

            [JsonProperty("Настройка руды || Ore Settings")]
            public OreSettings Ore;

            [JsonProperty(
                "ИсполUse the item separation system inside the plugin?(Set false if there is a conflict with the plugin on the stacks)")]
            public bool Stack;

            [JsonProperty("Включить автоматическую очистку даты при вайпе? || Enable automatic date clearing when wipe?")]
            public bool Wipe;

            internal class OreSettings
            {
                [JsonProperty("Включить возможность получения монетки при полном фарме камня? || Enable the ability to get a coin when the stone is completely minced?")]
                public bool Bonus;

                [JsonProperty("Шанс получения монеты при добыче || Chance of getting a coin when mining")]
                public int Chance;

                [JsonProperty("Имя руды || Ore Name")] public string DisplayName;

                [JsonProperty("Включить возможность получения монетки при каждом ударе? || Enable the ability to get a coin on each hit")]
                public bool Gather;

                [JsonProperty("Максимальное количество руды, которое может получить игрок || The maxmimum amount of ore that the player can get")]
                public int MaxAmount;

                [JsonProperty("Минимальное количество руды, которое может получить игрок || The minimum amount of ore that the player can get")]
                public int MinAmount;

                [JsonProperty("ShortName руды || Ore shortname")]
                public string ShortName;

                [JsonProperty("SkinID руды || Ore skinID")]
                public ulong SkinID;
            }

            internal class CoinSettings
            {
                [JsonProperty("Количество монет, выдаваемых при переработке || The number of coins issued during recycling")]
                public int Amount;

                [JsonProperty("Шанс получения монеты при переработке || Chance of getting a coin during processing")]
                public int Chance;

                [JsonProperty("Имя монеты || Coin name")]
                public string DisplayName;

                [JsonProperty("Изображение монеты(Для интерфейса) || Image URL")]
                public string Image;

                [JsonProperty("ShortName монеты || Coin shortname")]
                public string ShortName;

                [JsonProperty("SkinID монеты || Coin skinID")]
                public ulong SkinID;

                [JsonProperty("Стоимость одной монеты || The cost of one coin")]
                public int summ;
            }

            internal class GameStores
            {
                [JsonProperty("API Ключ || Api key")] public string API;

                [JsonProperty("Использовать вывод в GameStores? || Use out in GameStores?")]
                public bool Out;

                [JsonProperty("ServerID")] public string ServerID;
                [JsonProperty("ShopID")] public string ShopID;
            }

            internal class Economics
            {
                [JsonProperty("Использовать плагин экономики? || Use plugin Economic?")]
                public bool economics;

                [JsonProperty("Хук отвечающий за пополнение баланса || The hook responsible for adding funds to the balance")]
                public string PluginHook;

                [JsonProperty("Полное имя плагина с Экономикой || Full name of the plugin with Economy")]
                public string PluginName;
            }
        }

        protected override void LoadDefaultConfig()
        {
            _config = new PluginConfig
            {
                econom = new PluginConfig.Economics
                {
                    economics = true,
                    PluginName = "Economics",
                    PluginHook = "Deposit"
                },
                GS = new PluginConfig.GameStores
                {
                    API = "",
                    ShopID = "",
                    ServerID = "",
                    Out = false
                },
                Coin = new PluginConfig.CoinSettings
                {
                    ShortName = "glue",
                    Amount = 1,
                    Chance = 10,
                    DisplayName = "[COIN]",
                    Image = "https://imgur.com/P7WrOsH.png",
                    SkinID = 1984567801,
                    summ = 5
                },
                Ore = new PluginConfig.OreSettings
                {
                    Bonus = true,
                    Gather = false,
                    DisplayName = "[GOLD ORE] Recycle it in the recycle bin",
                    Chance = 10,
                    MinAmount = 4,
                    MaxAmount = 10,
                    SkinID = 1984568680,
                    ShortName = "ducttape"
                },
                MaxOut = 10,
                Stack = true,
                Wipe = true
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<PluginConfig>();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        #endregion

        #region OxideHooks

        private void Init()
        {
            LoadConfig();
            if (!_config.Ore.Bonus)
                Unsubscribe("OnDispenserBonus");
            else
                Subscribe("OnDispenserBonus");

            if (!_config.Ore.Gather)
                Unsubscribe("OnDispenserGather");
            else
                Subscribe("OnDispenserGather");

            if (!_config.Stack)
            {
                Unsubscribe("OnItemSplit");
                Unsubscribe("CanStackItem");
                Unsubscribe("CanCombineDroppedItem");
                Unsubscribe("CanStack");
            }
            else
            {
                Subscribe("OnItemSplit");
                Subscribe("CanStackItem");
                Subscribe("CanCombineDroppedItem");
                Subscribe("CanStack");
            }

            if (!_config.Wipe)
                Unsubscribe("OnNewSave");
            else
                Subscribe("OnNewSave");
        }

        private void OnServerInitialized()
        {
            PrintError("|-----------------------------------|");
            PrintWarning("|          Author: CASHR     |");
            PrintWarning("|          VK: vk.com/cashr         |");
            PrintWarning("|          Discord: CASHR#6906      |");
            PrintWarning("|          Email: pipnik99@gmail.com      |");
            PrintError("|-----------------------------------|");
            LoadData();
            ImageLibrary?.Call("AddImage", _config.Coin.Image, _config.Coin.Image);
            foreach (var check in BasePlayer.activePlayerList) OnPlayerConnected(check);
        }

        private void Unload()
        {
            SaveData();
        }

        private void OnServerSave()
        {
            SaveData();
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player == null) return;
            CheckData(player.userID);
        }

        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            var player = entity.ToPlayer();
            if (player == null) return;
            if (dispenser.gatherType == ResourceDispenser.GatherType.Ore)
                if (Random.Range(0, 100) <= _config.Ore.Chance)
                {
                    var x = CreateItem(Random.Range(_config.Ore.MinAmount, _config.Ore.MaxAmount), "ore");
                    player.GiveItem(x);
                }
        }

        private void OnNewSave()
        {
            if (_config.Wipe) data = new Dictionary<ulong, PlayerData>();
        }

        private void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (player == null) return;
            if (dispenser.gatherType == ResourceDispenser.GatherType.Ore)
                if (Random.Range(0, 100) <= _config.Ore.Chance)
                {
                    var x = CreateItem(Random.Range(_config.Ore.MinAmount, _config.Ore.MaxAmount), "ore");
                    player.GiveItem(x);
                
                }
        }

        private object CanStackItem(Item item, Item item2)
        {
            return CanStack(item, item2);
        }

        private object CanCombineDroppedItem(DroppedItem item, DroppedItem item2)
        {
            return CanStack(item.item, item2.item);
        }

        private object OnItemSplit(Item item, int amount)
        {
            if (item.skin == _config.Coin.SkinID)
            {
                var x = CreateItem(amount, "coin");
                item.amount -= amount;
                item.MarkDirty();
                x.MarkDirty();
                return x;
            }

            if (item.skin == _config.Ore.SkinID)
            {
                var x = CreateItem(amount, "ore");
                item.amount -= amount;
                item.MarkDirty();
                x.MarkDirty();
                return x;
            }

            return null;
        }

        private object CanStack(Item item, Item item2)
        {
            if (item.info.displayName.english == item2.info.displayName.english)
            {
                if (item.skin == item2.skin) return null;
                return false;
            }

            return null;
        }

        #endregion

        #region Дата

        public Dictionary<ulong, PlayerData> data = new Dictionary<ulong, PlayerData>();

        public class PlayerData
        {
            public DateTime Date;
            public string Name;
            public int Out;
        }

        private void LoadData()
        {
            try
            {
                data = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, PlayerData>>("MagicCoin/Players");
            }
            catch
            {
                data = new Dictionary<ulong, PlayerData>();
            }
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("MagicCoin/Players", data);
        }

        #endregion

        #region Function

        [ChatCommand("coin")]
        private void cmdChatcoin(BasePlayer player, string command, string[] args)
        {
            ShowGUI(player);
        }

        [ChatCommand("givecoin")]
        private void cmdChatgivecoin(BasePlayer player)
        {
            if (player.IsAdmin)
            {
                var s = ItemManager.CreateByPartialName("ducttape", 100);
                s.name = _config.Ore.DisplayName;
                s.skin = _config.Ore.SkinID;
                var x = ItemManager.CreateByPartialName("glue", 100);
                x.name = _config.Coin.DisplayName;
                x.skin = _config.Coin.SkinID;
                player.GiveItem(s);
                player.GiveItem(x);
            }
        }

        [ConsoleCommand("coinswap")]
        private void cmdConsoleCoin(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (data[player.userID].Out >= _config.MaxOut)
            {
                SendReply(player, GetMsg("COIN.LIMIT", player.UserIDString));
                DestroyUI(player);
                return;
            }

            var item = FindItem(player, _config.Coin.SkinID, 1);
            if (item == null)
            {
                SendReply(player, GetMsg("COIN.NOTAMOUNT", player.UserIDString));
                DestroyUI(player);
                return;
            }

            var mess = "[MAGICCOIN] Coin swap";
            var amount = item.amount;
            var limit = _config.MaxOut - data[player.userID].Out;
            int summ;
            int take;
            if (amount > limit)
            {
                take = limit;
                summ = _config.Coin.summ * take;
                item.UseItem(take);
                if (_config.econom.economics) GiveBalance(player, take);

                if (_config.GS.Out)
                    ExecuteApiRequest(player, take, _config.Coin.summ, new Dictionary<string, string>
                    {
                        {"action", "moneys"},
                        {"type", "plus"},
                        {"steam_id", player.UserIDString},
                        {"amount", summ.ToString()},
                        {"mess", mess}
                    });
            }

            if (amount <= limit)
            {
                take = amount;
                summ = _config.Coin.summ * take;
                item.UseItem(take);
                if (_config.econom.economics) GiveBalance(player, take);

                if (_config.GS.Out)
                    ExecuteApiRequest(player, take, _config.Coin.summ, new Dictionary<string, string>
                    {
                        {"action", "moneys"},
                        {"type", "plus"},
                        {"steam_id", player.UserIDString},
                        {"amount", summ.ToString()},
                        {"mess", mess}
                    });
            }

            ShowGUI(player);
        }

        private void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
        }

        private void CheckData(ulong userID)
        {
            if (!data.ContainsKey(userID))
                CreateData(userID);
            if (data[userID].Date != DateTime.Now.Date)
            {
                data[userID].Out = 0;
                data[userID].Date = DateTime.Now.Date;
            }
        }

        private void CreateData(ulong userID)
        {
            if (data.ContainsKey(userID)) return;
            var player = BasePlayer.FindByID(userID);
            if (player == null) return;
            data.Add(userID, new PlayerData
            {
                Date = DateTime.Now.Date,
                Name = player.displayName,
                Out = 0
            });
        }

        private Item FindItem(BasePlayer player, ulong skinID, int amount)
        {
            var items = new List<Item>();
            items.AddRange(player.inventory.AllItems());
            for (var i = 0; i < items.Count; i++)
            {
                var findItem = items[i];
                if (findItem.skin == skinID && findItem.amount >= amount) return findItem;
            }

            return null;
        }

        private void GiveBalance(BasePlayer player, int take)
        {
            var plug = plugins.Find(_config.econom.PluginName);
            if (plug == null)
            {
                PrintError(
                    $"Plugin '{_config.econom.PluginName} ' NOTFOUND! ");
                var item = CreateItem(take, "coin");
                player.GiveItem(item);
                player.ChatMessage(GetMsg("COIN.ERROR", player.UserIDString));
                DestroyUI(player);
                return;
            }

            plug?.Call(_config.econom.PluginHook, player.userID, (double)(_config.Coin.summ * take));
            SendReply(player, GetMsg("COIN.SUCCEFULL", player.UserIDString, take, _config.Coin.summ * take));
            data[player.userID].Out += take;
            DestroyUI(player);
            LogToFile("Withdraw",
                GetMsg("COIN.LOG", player.UserIDString, player.displayName, player.userID, _config.Coin.summ * take), this);
        }

        private void ExecuteApiRequest(BasePlayer player, int take, float summ, Dictionary<string, string> args)
        {
            var item = CreateItem(take, "coin");
            var url =
                $"https://gamestores.ru/api?shop_id={_config.GS.ShopID}&secret={_config.GS.API}&server={_config.GS.ServerID}" +
                $"{string.Join("", args.Select(arg => $"&{arg.Key}={arg.Value}").ToArray())}";
            webrequest.Enqueue(url, null, (i, s) =>
            {
                if (i != 200)
                {
                    player.GiveItem(item);
                    LogToFile("Error", $"Код ошибки: {i}, подробности:\n{s}", this);
                    SendReply(player, GetMsg("COIN.ERROR", player.UserIDString));
                    DestroyUI(player);
                    return;
                }

                if (s.Contains("fail"))
                {
                    player.GiveItem(item);
                    SendReply(player, GetMsg("COIN.ERROR", player.UserIDString));
                    LogToFile("Error", $"Код ошибки: {i}, подробности:\n{s}", this);
                    DestroyUI(player);
                    return;
                }

                SendReply(player, GetMsg("COIN.SUCCEFULL", player.UserIDString, take, _config.Coin.summ * take));
                data[player.userID].Out += take;
                DestroyUI(player);
                LogToFile("Withdraw",
                    GetMsg("COIN.LOG", player.UserIDString, player.displayName, player.userID, _config.Coin.summ * take), this);
            }, this);
        }

        private Item CreateItem(int amount, string type)
        {
            Item item = null;
            switch (type)
            {
                case "ore":
                    item = ItemManager.CreateByName(_config.Ore.ShortName, amount, _config.Ore.SkinID);
                    item.name = _config.Ore.DisplayName;
                    break;
                case "coin":
                    item = ItemManager.CreateByName(_config.Coin.ShortName, amount, _config.Coin.SkinID);
                    item.name = _config.Coin.DisplayName;
                    break;
            }

            return item;
        }

        private object OnItemRecycle(Item item, Recycler recycler)
        {
            if (item.skin == _config.Ore.SkinID)
            {
                
                item.UseItem();
                if (Random.Range(1, 100) <= _config.Coin.Chance)
                {
                    var GatherCoin = CreateItem(_config.Coin.Amount, "coin");
                    recycler.MoveItemToOutput(GatherCoin);
                }
                else
                {
                    var s = ItemManager.CreateByName("stones", 100);
                    recycler.MoveItemToOutput(s);
                }

                return true;
            }

            return null;
        }

        #endregion

        #region Lang

        private string GetMsg(string langKey, string steamID)
        {
            return lang.GetMessage(langKey, this, steamID);
        }

        private string GetMsg(string langKey, string steamID, params object[] args)
        {
            return args.Length == 0 ? GetMsg(langKey, steamID) : string.Format(GetMsg(langKey, steamID), args);
        }

        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["COIN.SUCCEFULL"] = "You have successfully exchanged {0} coins {1} RUB.",
                ["COIN.LOG"] = "Player {0}/{1} led {2} rubles.",
                ["COIN.ERROR"] = "an error Occurred while the exchange will notify the administrator.",
                ["COIN.NOTAMOUNT"] = "We didn't find a coin in your inventory!",
                ["COIN.LIMIT"] = "You have reached your withdrawal limit for today!",
                ["COIN.UIBUTTON"] = "<size=14><b>EXCHANGE</b></size>\nyou can withdraw another {0} coins!",
                ["COIN.UITEXT"] = "<size=25>INFO</size>\nEvery day, you can find magic ore.You must take the ore to the processor and process it.\n With a chance of {0}%, you can get a Coin that you can exchange for a balance in the store\n<size=20><color=#ffd700>1 COIN</color> = <color=#00ff00>{1} RUB</color></size>\n The number of withdrawal attempts is limited: {2} per day. \n Have a nice game on our server!"
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["COIN.SUCCEFULL"] = "Вы успешно обменяли {0} монет на {1} Руб.",
                ["COIN.LOG"] = "Игрок {0}/{1} вывел {2} рублей.",
                ["COIN.ERROR"] = "Произошла ошибка при обмене, сообщите администратору.",
                ["COIN.NOTAMOUNT"] = "Мы не нашли в вашем инвентаре монету!",
                ["COIN.LIMIT"] = "На сегодня вы исчерпали свой лимит на вывод!",
                ["COIN.UIBUTTON"] = "<size=14><b>ОБМЕНЯТЬ</b></size>\nВы можете вывести еще {0} монет!",
                ["COIN.UITEXT"] = " <size=25>ИНФОРМАЦИЯ</size>\nКаждый день, вы можете найти магическую руду.\nВы должны отнести руду на переработчик и переработать ее.\nС шансом в {0}% вы можете получить Монету, которую можете обменять на баланс в магазине\n<size=20><color=#ffd700>1 МОНЕТА</color> = <color=#00ff00>{1} RUB</color></size>\n Количество попыток на вывод ограничено: {2} в день. \n Приятной игры на нашем сервере!"
            }, this, "ru");
        }

        #endregion

        #region Interface

        private void ShowGUI(BasePlayer player)
        {
            var container = new CuiElementContainer();


            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform =
                    {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-700 -400", OffsetMax = "700 400"},
                Image = {Color = "0 0 0 0.5", Material = "assets/content/ui/uibackgroundblur.mat"}
            }, "Overlay", Layer);
            var text = GetMsg("COIN.UITEXT", player.UserIDString, _config.Coin.Chance, _config.Coin.summ, _config.MaxOut);
            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Button = {Color = "0 0 0 0", Close = Layer},
                Text =
                {
                    Text = "", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 18
                }
            }, Layer);
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = $"{text.ToUpper()}", FontSize = 20, Font = "robotocondensed-bold.ttf",
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 0.45"},
                    new CuiOutlineComponent {Color = "0 0 0 1", Distance = "1.2 -1.2"}
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".IMAGE",
                Components =
                {
                    new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage", _config.Coin.Image)},
                    new CuiRectTransformComponent {AnchorMin = "0.43 0.46", AnchorMax = "0.57 0.66"}
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.43 0.38", AnchorMax = "0.57 0.45"},
                Button = {Color = "0.00 0.95 0.40 0.8", Command = "coinswap"},
                Text =
                {
                    Text = GetMsg("COIN.UIBUTTON", player.UserIDString, _config.MaxOut - data[player.userID].Out),
                    Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 13
                }
            }, Layer, Layer + ".OUT");
            Outline(ref container, Layer + ".OUT");
            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
        }

        private void Outline(ref CuiElementContainer container, string parent, string color = "1 1 1 1",
            string size = "2.5")
        {
            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 0", OffsetMax = $"0 {size}"},
                Image = {Color = color}
            }, parent);
            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = $"0 -{size}", OffsetMax = "0 0"},
                Image = {Color = color}
            }, parent);
            container.Add(new CuiPanel
            {
                RectTransform =
                    {AnchorMin = "0 0", AnchorMax = "0 1", OffsetMin = $"0 {size}", OffsetMax = $"{size} -{size}"},
                Image = {Color = color}
            }, parent);
            container.Add(new CuiPanel
            {
                RectTransform =
                    {AnchorMin = "1 0", AnchorMax = "1 1", OffsetMin = $"-{size} {size}", OffsetMax = $"0 -{size}"},
                Image = {Color = color}
            }, parent);
        }

        #endregion
    }
}