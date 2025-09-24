// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Oxide.Core.Plugins;
using System.Text;
using System;
using Oxide.Game.Rust.Cui;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using ConVar;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using Oxide.Core;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("IQEconomic", "https://discord.gg/dNGbxafuJn", "1.2.6")]
    [Description("Экономика на ваш сервер")]
    class IQEconomic : RustPlugin
    {
        public void GameStoresBalanceSet(ulong userID, int Balance)
        {
            var GameStores = config.ReferenceSettings.GameStoresSettings;
            if (String.IsNullOrEmpty(GameStores.GameStoresAPIStore) || String.IsNullOrEmpty(GameStores.GameStoresIDStore))
            {
                PrintWarning("Магазин GameStores не настроен! Невозможно выдать баланс пользователю");
                return;
            }
            webrequest.Enqueue($"https://gamestores.ru/api?shop_id={GameStores.GameStoresIDStore}&secret={GameStores.GameStoresAPIStore}&action=moneys&type=plus&steam_id={userID}&amount={Balance}&mess={GameStores.GameStoresMessage}", null, (i, s) =>
            {
                BasePlayer player = BasePlayer.FindByID(userID);
                if (i != 200) { }
                if (s.Contains("success"))
                {
                    Puts($"Пользователю {userID} успешно зачислен баланс - {Balance}");
                    if (player == null) return;
                    SendChat(GetLang("CHAT_STORE_SUCCESS", player.UserIDString), player);
                    return;
                }
                if (s.Contains("fail"))
                {
                    Puts($"Пользователь {userID} не авторизован в магазине");
                    if (player == null) return;
                    SendChat(GetLang("CHAT_NO_AUTH_STORE", player.UserIDString), player);
                }
            }, this);
        }
        void API_TRANSFERS(ulong userID, ulong trasferUserID, int Balance) => TransferPlayer(userID, trasferUserID, Balance);
		   		 		  						  	   		  		 			   		 		   		 		  	 	 
        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        bool API_MONEY_TYPE()
        {
            if (!config.UseUI)
                return true;
            else return false;
        }
        public int GetBalance(ulong userID)
        {
            if (config.UseUI)
            {
                if (IsData(userID))
                    return DataEconomics[userID].Balance;
                else return 0;
            }
            else
            {
                BasePlayer player = BasePlayer.FindByID(userID);
                if (player == null)
                {
                    PrintWarning(GetLang("BALANCE_CUSTOM_MONEY_NOT_PLAYER"));
                    return 0;
                }
                var PMoney = player.inventory.GetAmount(ItemManager.FindItemDefinition(config.CustomMoneySetting.Shortname).itemid);
                return PMoney;
            }
        }
        Item API_GET_ITEM(int Amount) => CreateCustomMoney(Amount);

        void OnCollectiblePickup(CollectibleEntity collectible, BasePlayer player)
        {
            foreach (ItemAmount item in collectible.itemList)
            {
                if (item == null || player == null) return;
                Configuration.GeneralSettings.GatherSettingReward General = config.GeneralSetting.CollectableReward;
                if (!General.UseReward) return;
                Dictionary<String, Configuration.GeneralSettings.AdvancedSetting> PickupGeneral = General.GatherSetting;
                if (!String.IsNullOrWhiteSpace(General.PermissionReward) &&
                    !permission.UserHasPermission(player.UserIDString, General.PermissionReward)) return;

                if (!PickupGeneral.ContainsKey(item.itemDef.shortname)) return;
                Configuration.GeneralSettings.AdvancedSetting Gather = PickupGeneral[item.itemDef.shortname];
                if (!IsRare(Gather.Rare)) return;

                SetBalance(player.userID, Gather.Money);
            }
        }
        
                [JsonProperty("Система экономики")] public Dictionary<UInt64, InformationData> DataEconomics = new Dictionary<UInt64, InformationData>();
		   		 		  						  	   		  		 			   		 		   		 		  	 	 
        private void ClearDataFile()
        {
            Configuration.GeneralSettings.DataFileSettings.DetalisWipeData DataFileSetting = new Configuration.GeneralSettings.DataFileSettings.DetalisWipeData();

            switch (DataFileSetting.TypeClear)
            {
                case TypeData.None:
                    break;
                case TypeData.AllWipe:
                    {
                        DataEconomics.Clear();
                        break;
                    }
                case TypeData.AllWipeOnlyBalance:
                    {
                        foreach (KeyValuePair<UInt64, InformationData> Data in DataEconomics.Where(x => x.Value.Balance < DataFileSetting.AmountBalance))
                            NextTick(() => { DataEconomics.Remove(Data.Key); });
                        break;
                    }
                case TypeData.TimeClear:
                    {
                        foreach (KeyValuePair<UInt64, InformationData> Data in DataEconomics.Where(x => (CurrentTime - x.Value.DateTime) > (DataFileSetting.DayPlayer * 86400)))
                            NextTick(() => { DataEconomics.Remove(Data.Key); });
                        break;
                    }
                case TypeData.TimeClearOnlyBalance:
                    {
                        foreach (KeyValuePair<UInt64, InformationData> Data in DataEconomics.Where(x => (CurrentTime - x.Value.DateTime) > (DataFileSetting.DayPlayer * 86400) && x.Value.Balance < DataFileSetting.AmountBalance))
                            NextTick(() => { DataEconomics.Remove(Data.Key); });
                        break;
                    }
                default:
                    break;
            }
            WriteData();
        }

        private BasePlayer FindPlayer(string nameOrId)
        {
            foreach (var check in BasePlayer.activePlayerList)
            {
                if (check.displayName.ToLower().Contains(nameOrId.ToLower()) || check.userID.ToString() == nameOrId)
                    return check;
            }

            return null;
        }
        void API_SET_BALANCE(ulong userID, int Balance, ItemContainer itemContainer = null) => SetBalance(userID, Balance, itemContainer);
        public void MoscovOVHBalanceSet(ulong userID, int Balance)
        {
            if (!RustStore)
            {
                PrintWarning("У вас не установлен магазин MoscovOVH");
                return;
            }
            plugins.Find("RustStore").CallHook("APIChangeUserBalance", userID, Balance, new Action<string>((result) =>
            {
                BasePlayer player = BasePlayer.FindByID(userID);
                if (result == "SUCCESS")
                {
                    Puts($"Пользователю {userID} успешно зачислен баланс - {Balance}");
                    if (player == null) return;
                    SendChat(GetLang("CHAT_STORE_SUCCESS", player.UserIDString), player);
                    return;
                }
                Puts($"Пользователь {userID} не авторизован в магазине");
                if (player == null) return;
                SendChat(GetLang("CHAT_NO_AUTH_STORE", player.UserIDString), player);
            }));
        }
        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UI_MY_BALANCE"] = "<b><size=12>Balance : {0}</size></b>",
                ["UI_MY_BALANCE_TRANSFER"] = "<b><size=9>Balance : {0}</size></b>",
		   		 		  						  	   		  		 			   		 		   		 		  	 	 
                ["CHAT_MY_BALANCE"] = "Your Balance : <color=yellow>{0}</color>",
                ["BALANCE_CUSTOM_MONEY_NOT_PLAYER"] = "Player not found",
                ["BALANCE_CUSTOM_MONEY_INVENTORY_FULL"] = "Your inventory is full, coins fell to the floor",

                ["BALANCE_SET"] = "You have successfully received : {0} money",
                ["BALANCE_TRANSFER_NO_BALANCE"] = "You do not have so many coins to transfer",
                ["BALANCE_TRANSFER_PLAYER"] = "You have successfully submitted {0} {1} money",
                ["BALANCE_TRANSFER_TRANSFERPLAYER"] = "You have successfully received {0} money from {1}",
                ["BALANCE_CUSTOM_MONEY_NO_COUNT_TAKE"] = "The player does not have as many coins available",
                ["BALANCE_TRANSFER_TRANSFERPLAYER_DIE"] = "The player is dead, you can’t give him coins",
		   		 		  						  	   		  		 			   		 		   		 		  	 	 
                ["TRANSFER_COMMAND_NO_ARGS"] = "Invalid Command\nEnter the correct transfer command transfer [Nick] [Amount Money]",

                ["UI_CHANGER_TRANSFER"] = "<size=14>EXCHANGE</size>",
                ["UI_CHANGER_TRANSFER_ALL"] = "<size=14>EXCHANGE ALL</size>",
                ["UI_CHANGER_TRANSFER_TITLE"] = "Currency exchange system",

                ["CHAT_MY_BALANCE"] = "Ваш баланс на данный момент : {0}",

                ["CHAT_NO_AUTH_STORE"] = "You no auth stores",
                ["CHAT_STORE_SUCCESS"] = "You succes transfers",
                ["CHAT_LIMITED_PLAYER"] = "You have raised the withdrawal limit, expect the limit to be reset",
                ["UI_LIMITED_PLAYER"] = "Your available limit : {0}",

            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UI_MY_BALANCE"] = "<b><size=12>Ваш баланс : {0}</size></b>",
                ["UI_MY_BALANCE_TRANSFER"] = "<b><size=9>Ваш баланс : {0}</size></b>",

                ["CHAT_MY_BALANCE"] = "Ваш баланс на данный момент : <color=yellow>{0}</color>",

                ["BALANCE_SET"] = "Вы успешно получили : {0} монет",
                ["BALANCE_CUSTOM_MONEY_NOT_PLAYER"] = "Такого игрока нет",
                ["BALANCE_CUSTOM_MONEY_INVENTORY_FULL"] = "Ваш инвентарь полон, монеты выпали на пол",
                ["BALANCE_CUSTOM_MONEY_NO_COUNT_TAKE"] = "У игрока нет столько монет в наличии",

                ["BALANCE_TRANSFER_NO_BALANCE"] = "У вас нет столько монет для передачи",
                ["BALANCE_TRANSFER_PLAYER"] = "Вы успешно передали {0} {1} монет(ы)",
                ["BALANCE_TRANSFER_TRANSFERPLAYER"] = "Вы успешно получили {0} монет(ы) от {1}",
                ["BALANCE_TRANSFER_TRANSFERPLAYER_DIE"] = "Игрок мертв,вы не можете передать ему монеты",

                ["TRANSFER_COMMAND_NO_ARGS"] = "Неверная команда\nВведите корректную команду transfer [Ник] [Количество монет]",

                ["UI_CHANGER_TRANSFER"] = "<size=14>ОБМЕНЯТЬ</size>",
                ["UI_CHANGER_TRANSFER_ALL"] = "<size=14>ОБМЕНЯТЬ ВСЕ</size>",
                ["UI_CHANGER_TRANSFER_TITLE"] = "Система обмена валюты",
		   		 		  						  	   		  		 			   		 		   		 		  	 	 
                ["CHAT_MY_BALANCE"] = "Ваш баланс на данный момент : {0}",
                ["CHAT_NO_AUTH_STORE"] = "Вы не аваторизованы в магазине",
                ["CHAT_STORE_SUCCESS"] = "Вы успешно обменяли валюту",
                ["CHAT_LIMITED_PLAYER"] = "Вы привысили лимит на вывод средств, ожидайте обнуление лимита",
                ["UI_LIMITED_PLAYER"] = "Ваш доступный лимит : {0}",
            }, this, "ru");
            PrintWarning("Языковой файл загружен успешно");
        }
        void WriteData() {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQEconomic/DataEconomics", DataEconomics);
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQEconomic/ActualityLimitTime", ActualityLimitTime);
        }

        public bool IsTransfer(int Balance)
        {
            var TransferCurse = config.TransferSettings;

            if (Balance <= 0) 
                return false;
            if (Balance >= TransferCurse.MoneyCount) 
                return true;
            else
                return false;
        }

        public void AddAllImage()
        {
            var Images = config.TransferSettings;
            if (string.IsNullOrEmpty(Images.URLMoney) || string.IsNullOrEmpty(Images.URLStores)) return;
            if (!ImageLibrary)
            {
                PrintError("Не установлен плагин ImageLibrary!");
                return;
            }

            AddImage(Images.URLMoney, "URLMoney"); 
            AddImage(Images.URLStores, "URLStores");
        }
        
        
        [ChatCommand("balance")]
        void ChatCommandBalance(BasePlayer player)
        {
            SendChat(GetLang("CHAT_MY_BALANCE", player.UserIDString, GetBalance(player.userID)), player);
        }

        
        public void TrackerTime()
        {
            Configuration.GeneralSettings.TimeSettingsReward TimeSetting = config.GeneralSetting.TimeReward;

            IEnumerable<BasePlayer> FiltredList = !String.IsNullOrWhiteSpace(TimeSetting.PermissionReward) ? BasePlayer.activePlayerList.Where(x => permission.UserHasPermission(x.UserIDString, TimeSetting.PermissionReward)) : BasePlayer.activePlayerList;
            foreach (BasePlayer player in FiltredList)
                if (DataEconomics[player.userID].Time <= CurrentTime)
                {
                    Int32 SetTime = Convert.ToInt32(config.GeneralSetting.TimeReward.OnlineTime + CurrentTime);
                    SetBalance(player.userID, config.GeneralSetting.TimeReward.OnlineTimeReward);
                    DataEconomics[player.userID].Time = SetTime;
                }
        }
        void OnEntityTakeDamage(BaseCombatEntity victim, HitInfo info)
        {
            if (victim.GetComponent<BaseHelicopter>() != null && info?.Initiator?.ToPlayer() != null)
            {
                var heli = victim.GetComponent<BaseHelicopter>();
                var player = info.Initiator.ToPlayer();
                NextTick(() =>
                {
                    if (heli == null) return;
                    if (!HeliAttackers.ContainsKey(heli.net.ID))
                        HeliAttackers.Add(heli.net.ID, new Dictionary<ulong, int>());
                    if (!HeliAttackers[heli.net.ID].ContainsKey(player.userID))
                        HeliAttackers[heli.net.ID].Add(player.userID, 0);
                    HeliAttackers[heli.net.ID][player.userID]++;
                });
            }
        }

                private Item OnItemSplit(Item item, int amount)
        {
            if (plugins.Find("Stacks") || plugins.Find("CustomSkinsStacksFix") || plugins.Find("SkinBox")) return null; 
            var CustomMoney = config.CustomMoneySetting;
            if (CustomMoney.SkinID == 0) return null;
            if (item.skin == CustomMoney.SkinID)
            {
                Item x = ItemManager.CreateByPartialName(CustomMoney.Shortname, amount);
                x.name = CustomMoney.DisplayName;
                x.skin = CustomMoney.SkinID;
                x.amount = amount;
                item.amount -= amount;
                return x;
            }
            return null;
        }
        public void RemoveBalance(ulong userID, int RemoveBalance)
        {
            if (!config.UseUI)
            {
                BasePlayer player = BasePlayer.FindByID(userID);
                if (player == null)
                {
                    PrintWarning(GetLang("BALANCE_CUSTOM_MONEY_NOT_PLAYER"));
                    return;
                }
                if(!IsRemoveBalance(userID,RemoveBalance))
                {
                    PrintWarning(GetLang("BALANCE_CUSTOM_MONEY_NO_COUNT_TAKE"));
                    return;
                }
                player.inventory.Take(null, ItemManager.FindItemDefinition(config.CustomMoneySetting.Shortname).itemid, RemoveBalance);
            }
            else
            {
                if (IsData(userID))
                {
                    DataEconomics[userID].Balance -= RemoveBalance;
                    BasePlayer player = BasePlayer.FindByID(userID);
                    if (player == null) return;
                    if (config.ShowUI)
                        Interface_Balance(player);
                }
                else RegisteredDataUser(userID);
            }
        }
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        
                bool API_IS_USER(ulong userID) => IsData(userID);

        
        
        private static StringBuilder sb = new StringBuilder();
        private void Init() => ReadData();

        private ulong GetLastAttacker(uint id)
        {
            int hits = 0;
            ulong majorityPlayer = 0U;
            if (HeliAttackers.ContainsKey(id))
            {
                foreach (var score in HeliAttackers[id])
                {
                    if (score.Value > hits)
                        majorityPlayer = score.Key;
                }
            }
            return majorityPlayer;
        }
        
                static Double CurrentTime => Facepunch.Math.Epoch.Current;
        
                private static Configuration config = new Configuration();
        void RegisteredDataUser(UInt64 player)
        {
            if (!DataEconomics.ContainsKey(player))
                DataEconomics.Add(player, new InformationData { Balance = config.StartedBalance, Time = 0, LimitBalance = config.TransferSettings.LimitBalance, DateTime = CurrentTime });
            else DataEconomics[player].DateTime = CurrentTime;
        }
        /// <summary>
        /// Обновление 1.2.х
        /// - Исправлено зачисление валюты за поднятие ресурсов после обновления игры
        /// </summary>

                [PluginReference] Plugin IQChat, Friends, Clans, Battles, Duel, RustStore, ImageLibrary, IQSphereEvent;

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning("Ошибка #49" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию! #33");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }
        public void LimitedUpdate(UInt64 userID, Int32 Amount)
        {
            if (IsLimited(userID, Amount)) return;
            DataEconomics[userID].LimitBalance -= Amount;
        }

        public void ConnectedPlayer(BasePlayer player)
        {
            RegisteredDataUser(player.userID);

            if (config.UseUI)
                if (config.UseUIMoney)
                {
                    if (config.ShowUI)
                        Interface_Balance(player);
                }
                else SendChat(GetLang("CHAT_MY_BALANCE", player.UserIDString, GetBalance(player.userID)), player);

            DataEconomics[player.userID].Time = Convert.ToInt32(config.GeneralSetting.TimeReward.OnlineTime + CurrentTime);
        }

        public void Interface_Changer(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, UI_CHANGER_PARENT);

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1607843 0.1647059 0.1294118 0.6117647", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-199.937 81.1", OffsetMax = "180.454 133.498" }
            }, "Overlay", UI_CHANGER_PARENT);

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1607843 0.1647059 0.1294118 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "13.656 -26.2", OffsetMax = "66.238 26.199" }
            }, UI_CHANGER_PARENT, "PanelBackgroundMoney");

            container.Add(new CuiElement
            {
                Name = "MoneyItem",
                Parent = "PanelBackgroundMoney", 
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("URLStores") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-15.747 -40.3", OffsetMax = "16.253 -8.3" }
                }
            });
		   		 		  						  	   		  		 			   		 		   		 		  	 	 
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1607843 0.1647059 0.1294118 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-130.174 -26.2", OffsetMax = "-77.592 26.199" }
            }, UI_CHANGER_PARENT, "PanelBackgroundCoin");

            container.Add(new CuiElement
            {
                Name = "CoinsItem",
                Parent = "PanelBackgroundCoin",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("URLMoney") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-15.217 -40.3", OffsetMax = "16.783 -8.3" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = HexToRustFormat("#B03825"), Close = UI_CHANGER_PARENT },
                Text = { Text = "<b>✖</b>", Font = "robotocondensed-regular.ttf", FontSize = 30, Align = TextAnchor.MiddleCenter, Color = "0.7843137 0.627451 0.5921569 1" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0 0", OffsetMax = "60.019 52.398" }
            }, UI_CHANGER_PARENT, "CloseBut");

            container.Add(new CuiButton
            {
                Button = { Color = HexToRustFormat("#89B63B"), Command = "transfer" },
                Text = { Text = GetLang("UI_CHANGER_TRANSFER", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0.8431373 0.9215686 0.6588235 1" },
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-123.951 0", OffsetMax = "-0.001 24.934" }
            }, UI_CHANGER_PARENT, "TransferBut");
		   		 		  						  	   		  		 			   		 		   		 		  	 	 
            container.Add(new CuiButton
            {
                Button = { Color = HexToRustFormat("#89B63B"), Command = "transfer.all" },
                Text = { Text = GetLang("UI_CHANGER_TRANSFER_ALL", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0.8431373 0.9215686 0.6588235 1" },
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-123.957 -24.934", OffsetMax = "0.003 0" }
            }, UI_CHANGER_PARENT, "TransferButAll");

            container.Add(new CuiElement
            {
                Name = "BalanceCount",
                Parent = UI_CHANGER_PARENT,
                Components = {
                    new CuiTextComponent { Text = GetLang("UI_MY_BALANCE_TRANSFER",player.UserIDString, GetBalance(player.userID)), Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-130.179 0", OffsetMax = "66.241 13.579" }
                }
            });
		   		 		  						  	   		  		 			   		 		   		 		  	 	 
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "1 1 1 1", Sprite = "assets/icons/chevron_right.png" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-47.969 -13.801", OffsetMax = "-15.969 18.199" }
            }, UI_CHANGER_PARENT, "PanelNextVisual");

            container.Add(new CuiElement
            {
                Name = "LabelAmountCoin",
                Parent = UI_CHANGER_PARENT,
                Components = {
                    new CuiTextComponent { Text = $"X{config.TransferSettings.MoneyCount}", Font = "robotocondensed-regular.ttf", FontSize = 15, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-75.169 -10.817", OffsetMax = "-47.969 15.215" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "LabelAmountBalance",
                Parent = UI_CHANGER_PARENT,
                Components = {
                    new CuiTextComponent { Text = $"X{config.TransferSettings.StoresMoneyCount}", Font = "robotocondensed-regular.ttf", FontSize = 15, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-15.969 -10.817", OffsetMax = "11.23 15.215" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "IQEconomicLabel",
                Parent = UI_CHANGER_PARENT,
                Components = {
                    new CuiTextComponent { Text = GetLang("UI_CHANGER_TRANSFER_TITLE", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.UpperCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-190.196 41.482", OffsetMax = "190.194 68.72" }
                }
            });

            if (config.TransferSettings.UseLimits)
                container.Add(new CuiElement
                {
                    Name = "IQEconomicLabelAccesLimit",
                    Parent = UI_CHANGER_PARENT,
                    Components = {
                    new CuiTextComponent { Text = GetLang("UI_LIMITED_PLAYER", player.UserIDString, GetLimit(player.userID)), Font = "robotocondensed-regular.ttf", FontSize = 13, Align = TextAnchor.UpperCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-190.197 26.199", OffsetMax = "190.193 46.643" }
                }
                });

            CuiHelper.AddUi(player, container);
        }
        void OnPlayerConnected(BasePlayer player) => ConnectedPlayer(player);
        void OnNewSave(string filename) => ClearDataFile();
        bool API_IS_REMOVED_BALANCE(ulong userID, int Amount) => IsRemoveBalance(userID, Amount);
		   		 		  						  	   		  		 			   		 		   		 		  	 	 
        [ConsoleCommand("iq.eco")] //iq.eco give.store 76561198807822175 300
        void IQEconomicCommandsAdmin(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null && !arg.Player().IsAdmin)
                return;

            switch(arg.Args[0])
            {
                case "give":
                    {
                        if(arg.Args[1] == null || arg.Args[1].Length == 0)
                        {
                            PrintWarning("Введите ник или ID. Синтаксис : iq.eco give SteamID/Name Amount");
                            return;
                        }
                        String NamaOrID = arg.Args[1];
                        if(String.IsNullOrWhiteSpace(NamaOrID))
                        {
                            PrintWarning("Введите ник или ID");
                            return;
                        }
                        IPlayer iPlayer = covalence.Players.FindPlayer(NamaOrID);
                        if(iPlayer == null)
                        {
                            Puts($"Такого игрока не существует");
                            return;
                        }
                        UInt64 userID = UInt64.Parse(iPlayer.Id);
                        Int32 Balance;
                        if (!Int32.TryParse(arg.Args[2], out Balance))
                        {
                            PrintWarning("Второй аргумент не является числом!");
                            return;
                        }
                        SetBalance(userID, Balance);
                        Puts($"Игроку {userID} успешно зачислено {Balance} монет");
                        break;
                    }
                case "remove":
                    {
                        String NamaOrID = arg.Args[1];
                        if (String.IsNullOrWhiteSpace(NamaOrID))
                        {
                            PrintWarning("Введите ник или ID");
                            return;
                        }
                        IPlayer iPlayer = covalence.Players.FindPlayer(NamaOrID);
                        if (iPlayer == null)
                        {
                            Puts($"Такого игрока не существует");
                            return;
                        }
                        UInt64 userID = UInt64.Parse(iPlayer.Id);
                        Int32 Balance;
                        if (!Int32.TryParse(arg.Args[2], out Balance))
                        {
                            PrintWarning("Второй аргумент не является числом!");
                            return;
                        }
                        RemoveBalance(userID, Balance);
                        Puts($"Игроку {userID} успешно снято {Balance} монет");
                        break;
                    }
                case "give.store":
                    {
                        Configuration.ReferenceSetting Reference = config.ReferenceSettings;
                        String NamaOrID = arg.Args[1];
                        if (String.IsNullOrWhiteSpace(NamaOrID))
                        {
                            PrintWarning("Введите ник или ID");
                            return;
                        }
                        IPlayer iPlayer = covalence.Players.FindPlayer(NamaOrID);
                        if (iPlayer == null)
                        {
                            Puts($"Такого игрока не существует");
                            return;
                        }
                        UInt64 userID = UInt64.Parse(iPlayer.Id);
                        Int32 Balance;
                        if (!Int32.TryParse(arg.Args[2], out Balance))
                        {
                            PrintWarning("Второй аргумент не является числом!");
                            return;
                        }

                        if (Reference.GameStoreshUse)
                            GameStoresBalanceSet(userID, Balance);

                        if (Reference.MoscovOvhUse)
                            MoscovOVHBalanceSet(userID, Balance);

                        Puts($"Игроку {userID} успешно зачислено {Balance} на магазин сервера");
                        break;
                    }
            }
        }
        string API_GET_STORES_IL() { return "URLStores"; }
        
                private static string HexToRustFormat(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        
        
        
        private TierIQSphereEvent GetTierIQSphereEvent(UInt64 OwnerID)
        {
            switch (OwnerID)
            {
                case 11111: return TierIQSphereEvent.Under;
                case 22222: return TierIQSphereEvent.Around;
                case 33333: return TierIQSphereEvent.Tier1;
                case 44444: return TierIQSphereEvent.Tier2;
                case 55555: return TierIQSphereEvent.Tier3;
            }
            return TierIQSphereEvent.None;
        }
        int API_GET_BALANCE(ulong userID) => GetBalance(userID);
        public bool IsDuel(ulong userID)
        {
            if (Battles)
                return (bool)Battles?.Call("IsPlayerOnBattle", userID);
            else if (Duel) return (bool)Duel?.Call("IsPlayerOnActiveDuel", BasePlayer.FindByID(userID));
            else return false;
        }
		   		 		  						  	   		  		 			   		 		   		 		  	 	 
        [ChatCommand("transfer")]
        void ChatCommandTransfer(BasePlayer player, String cmd, String[] arg)
        {
            if (player == null) return;
            if (!config.TransferSettings.TransferStoreUse && arg.Length == 0 || arg == null)
            {
                SendChat(GetLang("TRANSFER_COMMAND_NO_ARGS", player.UserIDString),player);
                return;
            }
            else if(config.TransferSettings.TransferStoreUse && arg.Length == 0 || arg == null)
            {
                Interface_Changer(player);
                return;
            }
            BasePlayer transferPlayer = FindPlayer(arg[0]);
            if (transferPlayer == null)
            {
                SendChat(GetLang("BALANCE_CUSTOM_MONEY_NOT_PLAYER", player.UserIDString), player);
                return;
            }
            if(transferPlayer.IsDead())
            {
                SendChat(GetLang("BALANCE_TRANSFER_TRANSFERPLAYER_DIE", player.UserIDString), player);
                return;
            }

            Regex regex = new Regex("^[0-9]+$");
            if (!regex.IsMatch(arg[1])) return;

            Int32 Amount;
            if (!Int32.TryParse(arg[1], out Amount)) return;
            if(Amount <= 0) return; 
		   		 		  						  	   		  		 			   		 		   		 		  	 	 
            TransferPlayer(player.userID, transferPlayer.userID, Amount);
        }
        public string GetLang(string LangKey, string userID = null, params object[] args)
        {
            sb.Clear();
            if (args != null)
            {
                sb.AppendFormat(lang.GetMessage(LangKey, this, userID), args);
                return sb.ToString();
            }
            return lang.GetMessage(LangKey, this, userID);
        }
        private string GetImage(string fileName, ulong skin = 0)
        {
            var imageId = (string)plugins.Find("ImageLibrary").CallHook("GetImage", fileName, skin);
            if (!string.IsNullOrEmpty(imageId))
                return imageId;
            return string.Empty;
        }
        
        
        
        public static string UI_BALANCE_PARENT = "BALANCE_PLAYER_PARENT";
        string API_GET_MONEY_IL() { return "URLMoney"; }
		   		 		  						  	   		  		 			   		 		   		 		  	 	 
        [ConsoleCommand("transfer.all")]
        void TransferAll(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null) return;

            Int32 Balance = config.TransferSettings.UseLimits ? GetBalance(player.userID) > GetLimit(player.userID) ? GetLimit(player.userID) : GetBalance(player.userID) : GetBalance(player.userID);
            if (!IsTransfer(Balance)) return;
            var Reference = config.ReferenceSettings;

            var Transfer = config.TransferSettings;
		   		 		  						  	   		  		 			   		 		   		 		  	 	 
            if (Reference.GameStoreshUse)
                GameStoresBalanceSet(player.userID, Convert.ToInt32((Balance / Transfer.MoneyCount) * Transfer.StoresMoneyCount), Balance);

            if (Reference.MoscovOvhUse)
                MoscovOVHBalanceSet(player.userID, Convert.ToInt32((Balance / Transfer.MoneyCount) * Transfer.StoresMoneyCount), Balance);

            if (config.TransferSettings.UseLimits)
                LimitedUpdate(player.userID, Balance);
        }
        public void MoscovOVHBalanceSet(ulong userID, int Balance, int MoneyTake)
        {
            if (!RustStore)
            {
                PrintWarning("У вас не установлен магазин MoscovOVH");
                return;
            }
            plugins.Find("RustStore").CallHook("APIChangeUserBalance", userID, Balance, new Action<string>((result) =>
            {
                BasePlayer player = BasePlayer.FindByID(userID);
                if (result == "SUCCESS")
                {
                    Puts($"Пользователю {userID} успешно зачислен баланс - {Balance}");
                    RemoveBalance(userID, MoneyTake);
                    if (player == null) return;
                    SendChat(GetLang("CHAT_STORE_SUCCESS", player.UserIDString), player);
                    Interface_Changer(player);
                    return;
                }
                Puts($"Пользователь {userID} не авторизован в магазине");
                if (player == null) return;
                SendChat(GetLang("CHAT_NO_AUTH_STORE", player.UserIDString), player);
            }));
        }
        Item CreateCustomMoney(int Amount)
        {
            var CustomMoney = config.CustomMoneySetting;
            Item Money = ItemManager.CreateByName(CustomMoney.Shortname, Amount, CustomMoney.SkinID);
            Money.name = CustomMoney.DisplayName;
            return Money;
        }
        private class Configuration
        {
            internal class TransferSetting
            {
                [JsonProperty("Включить обмен валюты на баланс в магазине(GameStores/MoscovOVH)")]
                public Boolean TransferStoreUse;
                [JsonProperty("URL вашей монеты")]
                public String URLMoney;
                [JsonProperty("URL валюты для магазина")]
                public String URLStores;
                [JsonProperty("Сколько монет требуется для обмена")]
                public Int32 MoneyCount;
                [JsonProperty("Сколько баланса получит игрок после обмена")]
                public Int32 StoresMoneyCount;
                [JsonProperty("Включить лимит обмена средств")]
                public Boolean UseLimits = false;
                [JsonProperty("Лимит средств(баланса на магазин)")]
                public Int32 LimitBalance = 100;
                [JsonProperty("Время лимита(в секундах) (Пример : Установлен лимит 100 баланса, раз в 3600 секунд)")]
                public Int32 LimitTime = 3600;
            }
                
            internal class Interface
            {
                [JsonProperty("Цвет основной панели")]
                public String ColorBackground = "#FFFFFF05";
                [JsonProperty("Цвет дополнительной панели #1")]
                public String ColorMoreOne = "#b1b1b1";
                [JsonProperty("Цвет дополнительной панели #2")]
                public String ColorMoreTwo = "#CCD045FF";
                [JsonProperty("Цвет текста")]
                public String ColorLabel = "#FEFFDDFF";
                [JsonProperty("AnchorMin")]
                public String AnchorMin = "1 0";
                [JsonProperty("AnchorMax")]
                public String AnchorMax = "1 0";
                [JsonProperty("OffsetMin")]
                public String OffsetMin = "-390 15";
                [JsonProperty("OffsetMax")]
                public String OffsetMax = "-212 42";
            }

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    StartedBalance = 0,
                    UseUI = true,
                    ShowUI = true,
                    UseAlert = true,
                    UseUIMoney = true,
                    TransferSettings = new TransferSetting
                    {
                        TransferStoreUse = true,
                        MoneyCount = 3,
                        StoresMoneyCount = 1,
                        URLMoney = "https://i.imgur.com/iGwnOyG.png",
                        URLStores = "https://i.imgur.com/fPPzFaE.png",
                        UseLimits = false,
                        LimitBalance = 100,
                        LimitTime = 3600,
                    },
                    GeneralSetting = new GeneralSettings
                    {
                        DataFileSetting = new GeneralSettings.DataFileSettings
                        {
                          SaveTime = 360,
                          DetalisWipe = new GeneralSettings.DataFileSettings.DetalisWipeData
                          {
                              AmountBalance = 0,
                              DayPlayer = 0,
                              TypeClear = TypeData.None,
                          }
                        },
                        AnimalReward = new GeneralSettings.GeneralSettingReward
                        {
                            PermissionReward = "",
                            UseReward = true,
                            AdvancedReward = new GeneralSettings.AdvancedSetting
                            {
                                Rare = 53,
                                Money = 2,
                            },
                        },
                        BarrelReward = new GeneralSettings.GeneralSettingReward
                        {
                            PermissionReward = "",
                            UseReward = true,
                            AdvancedReward = new GeneralSettings.AdvancedSetting
                            {
                                Rare = 23,
                                Money = 5,
                            },
                        },
                        BradleyReward = new GeneralSettings.GeneralSettingReward
                        {
                            PermissionReward = "",
                            UseReward = true,
                            AdvancedReward = new GeneralSettings.AdvancedSetting
                            {
                                Rare = 44,
                                Money = 100,
                            },
                        },
                        HelicopterReward = new GeneralSettings.GeneralSettingReward
                        {
                            PermissionReward = "",
                            UseReward = true,
                            AdvancedReward = new GeneralSettings.AdvancedSetting
                            {
                                Rare = 80,
                                Money = 100,
                            },
                        },
                        KilledReward = new GeneralSettings.GeneralSettingReward
                        {
                            PermissionReward = "",
                            UseReward = true,
                            AdvancedReward = new GeneralSettings.AdvancedSetting
                            {
                                Rare = 35,
                                Money = 10,
                            },
                        },
                        NPCReward = new GeneralSettings.GeneralSettingReward
                        {
                            PermissionReward = "",
                            UseReward = true,
                            AdvancedReward = new GeneralSettings.AdvancedSetting
                            {
                                Rare = 10,
                                Money = 15,
                            },
                        },
                        TimeReward = new GeneralSettings.TimeSettingsReward
                        {
                            UseReward = true,
                            PermissionReward = "",
                            OnlineTime = 100,
                            OnlineTimeReward = 10,                                             
                        },
                        GatherReward = new GeneralSettings.GatherSettingReward
                        {
                            UseReward = true,
                            PermissionReward = "",
                            GatherSetting = new Dictionary<String, GeneralSettings.AdvancedSetting>
                            {
                                ["sulfur.ore"] = new GeneralSettings.AdvancedSetting
                                {
                                    Rare = 10,
                                    Money = 10,
                                },
                                ["stones"] = new GeneralSettings.AdvancedSetting
                                {
                                    Rare = 20,
                                    Money = 1,
                                }
                            },
                        },
                        CollectableReward = new GeneralSettings.GatherSettingReward
                        {
                            UseReward = true,
                            PermissionReward = "",
                            GatherSetting = new Dictionary<String, GeneralSettings.AdvancedSetting>
                            {
                                ["sulfur.ore"] = new GeneralSettings.AdvancedSetting
                                {
                                    Rare = 10,
                                    Money = 10,
                                },
                                ["stones"] = new GeneralSettings.AdvancedSetting
                                {
                                    Rare = 20,
                                    Money = 1,
                                }
                            },
                        },
                        IQSphereNpcReward = new Dictionary<TierIQSphereEvent, GeneralSettings.GeneralSettingReward>
                        {
                            [TierIQSphereEvent.Under] = new GeneralSettings.GeneralSettingReward
                            {
                                PermissionReward = "",
                                UseReward = true,
                                AdvancedReward = new GeneralSettings.AdvancedSetting
                                {
                                    Rare = 50,
                                    Money = 5,
                                },
                            },
                            [TierIQSphereEvent.Around] = new GeneralSettings.GeneralSettingReward
                            {
                                PermissionReward = "",
                                UseReward = true,
                                AdvancedReward = new GeneralSettings.AdvancedSetting
                                {
                                    Rare = 30,
                                    Money = 5,
                                },
                            },
                            [TierIQSphereEvent.Tier1] = new GeneralSettings.GeneralSettingReward
                            {
                                PermissionReward = "",
                                UseReward = true,
                                AdvancedReward = new GeneralSettings.AdvancedSetting
                                {
                                    Rare = 30,
                                    Money = 10,
                                },
                            },
                            [TierIQSphereEvent.Tier2] = new GeneralSettings.GeneralSettingReward
                            {
                                PermissionReward = "",
                                UseReward = true,
                                AdvancedReward = new GeneralSettings.AdvancedSetting
                                {
                                    Rare = 45,
                                    Money = 12,
                                },
                            },
                            [TierIQSphereEvent.Tier3] = new GeneralSettings.GeneralSettingReward
                            {
                                PermissionReward = "",
                                UseReward = true,
                                AdvancedReward = new GeneralSettings.AdvancedSetting
                                {
                                    Rare = 70,
                                    Money = 15,
                                },
                            },
                        }
                    },
                    CustomMoneySetting = new CustomMoney
                    {
                        DisplayName = "Монета удачи",
                        Shortname = "bleach",
                        SkinID = 1337228,
                    },
                    ReferenceSettings = new ReferenceSetting
                    {
                        FriendsBlockUse = true,
                        ClansBlockUse = true,
                        DuelBlockUse = true,
                        MoscovOvhUse = true,
                        GameStoreshUse = false,
                        GameStoresSettings = new ReferenceSetting.GameStores
                        {
                            GameStoresAPIStore = "",
                            GameStoresIDStore = "",
                            GameStoresMessage = "Успешный обмен"
                        },
                        ChatSettings = new ReferenceSetting.ChatSetting
                        {
                            CustomAvatar = "",
                            CustomPrefix = "",
                            UIAlertUse = true,
                        }
                    }

                };
            }
            [JsonProperty("Использовать UI интерфейс для отображения баланса(true - да(Если вы не поставили , чтобы монетки были на руках))/false - информация будет в чате по команде и при входе)")]
            public Boolean UseUIMoney;
            [JsonProperty("Настройки интерфейса в плагине (Не трогайте, если не понимаете как с этим работать)")]
            public Interface InterfaceSetting = new Interface();
            [JsonProperty("Настройки обмена валют на баланс в магазине")]
            public TransferSetting TransferSettings = new TransferSetting();
            [JsonProperty("Монетки будут и гроков на руках - false / Иначе будет чат или интерфейсе - true")]
            public Boolean UseUI;
            [JsonProperty("Настройки валюты(Если вид экономики - false)")]
            public CustomMoney CustomMoneySetting = new CustomMoney();
            internal class CustomMoney
            {
                [JsonProperty("SkinID монетки")]
                public UInt64 SkinID;
                [JsonProperty("Shortname монетки")]
                public String Shortname;
                [JsonProperty("Название валюты")]
                public String DisplayName;
            }
            [JsonProperty("Отображение интерфейса с балансом(true - отображает/false - скрывает)")]
            public Boolean ShowUI;
            [JsonProperty("Стартовый баланс для всех игроков")]
            public Int32 StartedBalance = 0;
            internal class GeneralSettings
            {
                [JsonProperty("Настройка дата-файла")]
                public DataFileSettings DataFileSetting = new DataFileSettings();
                internal class DataFileSettings
                {
                    [JsonProperty("Интервал сохранения дата файла (Установите 0, если не требуется сохранять файл по таймеру)")]
                    public Int32 SaveTime = 360;
                    [JsonProperty("Настройка очистки дата-файла")]
                    public DetalisWipeData DetalisWipe = new DetalisWipeData();
                    internal class DetalisWipeData
                    {
                        [JsonProperty("Тип очистки дата-файла : 0 - Не очищать совсем, 1 - Очищать всю дату при вайпе сервера, 2 - Очищать дата файл игроков с балансом <N(настраивается в этой категории) при вайпе сервера, 3 - Очищать игроков, которые не заходили >N(настраивается в этой категории) дней, 4 - Очищать игроков, которые не заходили >N(настраивается в этой категории) дней и их баланс <N(настраивается в этой категории)")]
                        public TypeData TypeClear;
                        [JsonProperty("<N-количество баланса для очистки даты. Для типов : 2 и 4")]
                        public Int32 AmountBalance = 0;
                        [JsonProperty(">N-дней оффлайн для очистки даты. Для типов : 3 и 4")]
                        public Int32 DayPlayer = 0;
                    }

                }
                [JsonProperty("Настройка получение валюты за убийство игроков")]
                public GeneralSettingReward KilledReward;
                [JsonProperty("Настройка получение валюты за убийство животных")]
                public GeneralSettingReward AnimalReward;
                [JsonProperty("Настройка получение валюты за убийство NPC")]
                public GeneralSettingReward NPCReward;
                [JsonProperty("Настройка получение валюты за уничтожение танка")]
                public GeneralSettingReward BradleyReward;
                [JsonProperty("Настройка получение валюты за уничтожение вертолета")]
                public GeneralSettingReward HelicopterReward;
                [JsonProperty("Настройка получение валюты за уничтожение бочек")]
                public GeneralSettingReward BarrelReward;

                [JsonProperty("Настройка получение валюты за добычу ресурсов")]
                public GatherSettingReward GatherReward;
                [JsonProperty("Настройка получение валюты за поднятие ресурсов с земли (грибы, ягоды, дерево и т.д)")]
                public GatherSettingReward CollectableReward;

                [JsonProperty("Настройка получение валюты за проведенное время на сервере")]
                public TimeSettingsReward TimeReward;

                [JsonProperty("IQSphereEvent : Настройка получение валюты за убийство NPC (Under - под сферой, Around - вокруг сферы, Tier1 - 1 этаж сферы, Tier2 - 2 этаж сферы, Tier3 - 3 этаж сферы)")]
                public Dictionary<TierIQSphereEvent, GeneralSettingReward> IQSphereNpcReward;
		   		 		  						  	   		  		 			   		 		   		 		  	 	 
                internal class TimeSettingsReward
                {
                    [JsonProperty("Права для использования данной возможности (Если вам требуется сделать ее доступной всем по стандарту - оставьте поле пустым)")]
                    public String PermissionReward = "";
                    [JsonProperty("Использовать эту возможность получения валюты")]
                    public Boolean UseReward = true;
                    [JsonProperty("Сколько нужно провести времени,чтобы выдали награду")]
                    public Int32 OnlineTime;
                    [JsonProperty("Сколько начислять валюты за проведенное время на сервере")]
                    public Int32 OnlineTimeReward;
                }
                internal class GatherSettingReward
                {
                    [JsonProperty("Права для использования данной возможности (Если вам требуется сделать ее доступной всем по стандарту - оставьте поле пустым)")]
                    public String PermissionReward = "";
                    [JsonProperty("Использовать эту возможность получения валюты")]
                    public Boolean UseReward = true;
                    [JsonProperty("Сколько начислять валюты за ресурсы ( [за какой ресурс давать] = { остальная настройка }")]
                    public Dictionary<String, AdvancedSetting> GatherSetting = new Dictionary<String, AdvancedSetting>();
                }
                internal class GeneralSettingReward
                {
                    [JsonProperty("Права для использования данной возможности (Если вам требуется сделать ее доступной всем по стандарту - оставьте поле пустым)")]
                    public String PermissionReward = "";
                    [JsonProperty("Использовать эту возможность получения валюты")]
                    public Boolean UseReward = true;
                    [JsonProperty("Настройка получения валюты")]
                    public AdvancedSetting AdvancedReward = new AdvancedSetting();
                }
                internal class AdvancedSetting
                {
                    [JsonProperty("Шанс получить валюту")]
                    public Int32 Rare;
                    [JsonProperty("Сколько выдавать валюты")]
                    public Int32 Money;
                }
            }
            [JsonProperty("Основные настройки")]
            public GeneralSettings GeneralSetting = new GeneralSettings();
            [JsonProperty("Настройки совместной работы с другими плагинами")]
            public ReferenceSetting ReferenceSettings = new ReferenceSetting();
            internal class ReferenceSetting
            {
                [JsonProperty("Friends : Запретить получение монет за убийство друзей")]
                public Boolean FriendsBlockUse;
                [JsonProperty("Clans : Запретить получение монет за убийство сокланов")]
                public Boolean ClansBlockUse;
                [JsonProperty("Duel/Battles : Запретить получение монет за убийство на дуэлях")]
                public Boolean DuelBlockUse;
                [JsonProperty("MoscovOVH : Включить использование магазина(Должен быть включен обмен валют)")]
                public Boolean MoscovOvhUse;
                [JsonProperty("GameStores : Включить использование магазина(Должен быть включен обмен валют)")]
                public Boolean GameStoreshUse;
                [JsonProperty("GameStores : Настройки магазина GameStores")]
                public GameStores GameStoresSettings = new GameStores();
                [JsonProperty("IQChat : Настройки чата")]
                public ChatSetting ChatSettings = new ChatSetting();
                internal class ChatSetting
                {
                    [JsonProperty("IQChat : Кастомный префикс в чате")]
                    public String CustomPrefix;
                    [JsonProperty("IQChat : Кастомный аватар в чате(Если требуется)")]
                    public String CustomAvatar;
                    [JsonProperty("IQChat : Использовать UI уведомления")]
                    public Boolean UIAlertUse;
                }
                internal class GameStores
                {
                    [JsonProperty("API Магазина(GameStores)")]
                    public String GameStoresAPIStore;
                    [JsonProperty("ID Магазина(GameStores)")]
                    public String GameStoresIDStore;
                    [JsonProperty("Сообщение в магазин при выдаче баланса(GameStores)")]
                    public String GameStoresMessage;
                }
            }
            [JsonProperty("Включить уведомления игроку о получении валюты за добычу (true - да/false - нет)")]
            public Boolean UseAlert = true;
        }
        
        public bool IsRare(int Rare)
        {
            if (Rare >= UnityEngine.Random.Range(0, 100))
                return true;
            else return false;
        }
        public Int32 GetLimit(UInt64 userID) => DataEconomics[userID].LimitBalance;
        public void Interface_Balance(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, UI_BALANCE_PARENT);

            var Balance = GetBalance(player.userID);
            Configuration.Interface Interface = config.InterfaceSetting;

            container.Add(new CuiPanel 
            {
                RectTransform = { AnchorMin = Interface.AnchorMin, AnchorMax = Interface.AnchorMax, OffsetMin = Interface.OffsetMin, OffsetMax = Interface.OffsetMax },
                Image = { FadeIn = 0.15f, Color = HexToRustFormat(Interface.ColorBackground), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            }, "Hud", UI_BALANCE_PARENT);
		   		 		  						  	   		  		 			   		 		   		 		  	 	 
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.01932367 0.04938272", AnchorMax = $"0.15415828 0.9382828" }, 
                Image = { FadeIn = 0.15f, Color = HexToRustFormat(Interface.ColorMoreOne), Sprite = "assets/icons/connection.png" }
            }, UI_BALANCE_PARENT);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.1835208 0.1358024", AnchorMax = $"0.9625472 0.8518519" },
                Image = { FadeIn = 0.15f, Color = HexToRustFormat(Interface.ColorMoreTwo), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            }, UI_BALANCE_PARENT);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.22 0", AnchorMax = "1 1" },
                Text = { Text = GetLang("UI_MY_BALANCE", player.UserIDString, Balance), Color = HexToRustFormat(Interface.ColorLabel), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft, FadeIn = 0.3f }
            }, UI_BALANCE_PARENT); 

            CuiHelper.AddUi(player, container); 
        }
        object CanCombineDroppedItem(DroppedItem item, DroppedItem targetItem)
        {
            if (item.GetItem().skin != targetItem.GetItem().skin) return false;

            return null;
        }
        public void TransferPlayer(ulong userID, ulong transferUserID, int Balance )
        {
            BasePlayer player = BasePlayer.FindByID(userID);
            BasePlayer transferPlayer = BasePlayer.FindByID(transferUserID);
            if (player == null) return;
            if (transferPlayer == null) return;

            if (!IsRemoveBalance(player.userID, Balance))
            {
                SendChat(GetLang("BALANCE_TRANSFER_NO_BALANCE", player.UserIDString), player);
                return;
            }

            RemoveBalance(player.userID, Balance);
            SetBalance(transferPlayer.userID, Balance);
            SendChat(GetLang("BALANCE_TRANSFER_PLAYER", player.UserIDString, transferPlayer.displayName, Balance), player);
            SendChat(GetLang("BALANCE_TRANSFER_TRANSFERPLAYER", transferPlayer.UserIDString, Balance, player.displayName), transferPlayer);
        }

        private void ResetLimit()
        {
            if (!config.TransferSettings.UseLimits) return;
            timer.Once(300f, () => ResetLimit());

            if (ActualityLimitTime > CurrentTime) return;

            ActualityLimitTime = Convert.ToInt32(CurrentTime + config.TransferSettings.LimitTime);

            foreach (var Value in DataEconomics.Values)
                NextTick(() => { Value.LimitBalance = config.TransferSettings.LimitBalance; });
            Puts("Лимит баланса был сброшен за пройденное время");
        }
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return;
            BasePlayer player = info.InitiatorPlayer;
            if (info.InitiatorPlayer != null)
                player = info.InitiatorPlayer;
            else if (entity.GetComponent<BaseHelicopter>() != null)
                player = BasePlayer.FindByID(GetLastAttacker(entity.net.ID));
            if (player == null) return;

            var General = config.GeneralSetting;
            var ReferenceGeneral = config.ReferenceSettings;

            if ((bool)(entity as NPCPlayer) || entity.IsNpc || (bool)(entity as BaseNpc))
            {
                if (IQSphereEvent)
                {
                    TierIQSphereEvent TierNPC = GetTierIQSphereEvent(entity.OwnerID);
                    if (General.IQSphereNpcReward.ContainsKey(TierNPC))
                    {
                        var NPCReward = General.IQSphereNpcReward[TierNPC];
                        if (!String.IsNullOrWhiteSpace(General.IQSphereNpcReward[TierNPC].PermissionReward) && !permission.UserHasPermission(player.UserIDString, General.IQSphereNpcReward[TierNPC].PermissionReward)) return;
                        if (!IsRare(NPCReward.AdvancedReward.Rare)) return;
                        SetBalance(player.userID, NPCReward.AdvancedReward.Money);
                        return;
                    }
                }
                if (General.NPCReward.UseReward)
                {
                    var Setting = General.NPCReward.AdvancedReward;
                    if (!String.IsNullOrWhiteSpace(General.NPCReward.PermissionReward) && !permission.UserHasPermission(player.UserIDString, General.NPCReward.PermissionReward)) return;
                    if (!IsRare(Setting.Rare)) return;
                    SetBalance(player.userID, Setting.Money);
                    return;
                }
            }
            if ((bool)(entity as BasePlayer))
            {
                if ((bool)(entity as NPCPlayer) || entity.IsNpc || (bool)(entity as BaseNpc)) return;

                BasePlayer targetPlayer = entity.ToPlayer();
                if (targetPlayer == null) return;
                if (targetPlayer.userID != player.userID)
                {
                    if (General.KilledReward.UseReward)
                    {
                        if (!String.IsNullOrWhiteSpace(General.KilledReward.PermissionReward) && !permission.UserHasPermission(player.UserIDString, General.KilledReward.PermissionReward)) return;

                        if (ReferenceGeneral.FriendsBlockUse)
                            if (IsFriends(player.userID, targetPlayer.userID)) return;
                        if (ReferenceGeneral.ClansBlockUse)
                            if (IsClans(player.UserIDString, targetPlayer.UserIDString))
                                return;
                        if (ReferenceGeneral.DuelBlockUse)
                            if (IsDuel(player.userID)) return;

                        var Setting = General.KilledReward.AdvancedReward;
                        if (!IsRare(Setting.Rare)) return;
                        SetBalance(player.userID, Setting.Money);
                        return;
                    }
                }
            }
            if ((bool)(entity as BaseAnimalNPC))
                if (General.AnimalReward.UseReward)
                {
                    if (!String.IsNullOrWhiteSpace(General.AnimalReward.PermissionReward) && !permission.UserHasPermission(player.UserIDString, General.AnimalReward.PermissionReward)) return;

                    var Setting = General.AnimalReward.AdvancedReward;
                    if (!IsRare(Setting.Rare)) return;
                    SetBalance(player.userID, Setting.Money);
                    return;
                }
            if ((bool)(entity as BaseHelicopter))
                if (General.HelicopterReward.UseReward)
                {
                    if (!String.IsNullOrWhiteSpace(General.HelicopterReward.PermissionReward) && !permission.UserHasPermission(player.UserIDString, General.HelicopterReward.PermissionReward)) return;

                    var Setting = General.HelicopterReward.AdvancedReward;
                    if (!IsRare(Setting.Rare)) return;
                    SetBalance(player.userID, Setting.Money);
                    return;
                }
            if ((bool)(entity as BradleyAPC))
                if (General.BradleyReward.UseReward)
                {
                    if (!String.IsNullOrWhiteSpace(General.BradleyReward.PermissionReward) && !permission.UserHasPermission(player.UserIDString, General.BradleyReward.PermissionReward)) return;

                    var Setting = General.BradleyReward.AdvancedReward;
                    if (!IsRare(Setting.Rare)) return;
                    SetBalance(player.userID, Setting.Money);
                    return;
                }
            if(entity.PrefabName.Contains("barrel") && !entity.PrefabName.Contains("hobobarrel"))  
                if(General.BarrelReward.UseReward)
                {
                    if (!String.IsNullOrWhiteSpace(General.BarrelReward.PermissionReward) && !permission.UserHasPermission(player.UserIDString, General.BarrelReward.PermissionReward)) return;

                    var Setting = General.BarrelReward.AdvancedReward;
                    if (!IsRare(Setting.Rare)) return;
                    SetBalance(player.userID, Setting.Money);
                    return;
                }
        }

        public bool IsRemoveBalance(ulong userID,int Amount)
        {
            if (GetBalance(userID) >= Amount)
                return true;
            else return false;
        }
        object CanStackItem(Item item, Item targetItem)
        {
            if (item.skin != targetItem.skin) return false;

            return null;
        }
        public static string UI_CHANGER_PARENT = "CHANGER_PLAYER_PARENT";
        
        
                void Unload()
        {
            foreach(BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, UI_BALANCE_PARENT);
                CuiHelper.DestroyUi(player, UI_CHANGER_PARENT);
            }
            WriteData();
        }
        
        [ConsoleCommand("transfer")]
        void ConsoleCommandTransfer(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null) return;

            Int32 Balance = GetBalance(player.userID);
            var Reference = config.ReferenceSettings;

            if (IsLimited(player.userID, Balance))
            {
                SendChat(GetLang("CHAT_LIMITED_PLAYER", player.UserIDString), player);
                return;
            }

            if (!IsTransfer(Balance)) return;
            var Transfer = config.TransferSettings;

            if (Reference.GameStoreshUse)
                GameStoresBalanceSet(player.userID, Transfer.StoresMoneyCount, Transfer.MoneyCount);
		   		 		  						  	   		  		 			   		 		   		 		  	 	 
            if(Reference.MoscovOvhUse)
                MoscovOVHBalanceSet(player.userID, Transfer.StoresMoneyCount, Transfer.MoneyCount);

            if (config.TransferSettings.UseLimits)
                LimitedUpdate(player.userID, Balance);
        }
        public bool IsData(ulong userID)
        {
            if (DataEconomics.ContainsKey(userID))
                return true;
            else return false;
        }
        void API_REMOVE_BALANCE(ulong userID, int Balance) => RemoveBalance(userID, Balance);
        [JsonProperty("Время действующего времени на лимит выводов")] public Int32 ActualityLimitTime;
        protected override void SaveConfig() => Config.WriteObject(config);
        public class InformationData
        {
            [JsonProperty("Баланс игрока")]
            public Int32 Balance;
            [JsonProperty("Лимит вывода баланса игрока")]
            public Int32 LimitBalance;
            [JsonProperty("Счетчик времени")]
            public Int32 Time;
            [JsonProperty("Последний вход игрока на сервер")]
            public Double DateTime;
        }
        public void GameStoresBalanceSet(ulong userID, int Balance,int MoneyTake)
        {
            var GameStores = config.ReferenceSettings.GameStoresSettings;
            if (String.IsNullOrEmpty(GameStores.GameStoresAPIStore) || String.IsNullOrEmpty(GameStores.GameStoresIDStore))
            {
                PrintWarning("Магазин GameStores не настроен! Невозможно выдать баланс пользователю");
                return;
            }
            webrequest.Enqueue($"https://gamestores.ru/api?shop_id={GameStores.GameStoresIDStore}&secret={GameStores.GameStoresAPIStore}&action=moneys&type=plus&steam_id={userID}&amount={Balance}&mess={GameStores.GameStoresMessage}", null, (i, s) =>
            {
                BasePlayer player = BasePlayer.FindByID(userID);
                if (i != 200) { }
                if (s.Contains("success"))
                {
                    Puts($"Пользователю {userID} успешно зачислен баланс - {Balance}");
                    RemoveBalance(userID, MoneyTake);
                    if (player == null) return;
                    SendChat(GetLang("CHAT_STORE_SUCCESS", player.UserIDString), player);

                    Interface_Changer(player);
                    return;
                }
                if (s.Contains("fail"))
                {
                    Puts($"Пользователь {userID} не авторизован в магазине");
                    if (player == null) return;
                    SendChat(GetLang("CHAT_NO_AUTH_STORE", player.UserIDString),player);
                }
            }, this);

        }
        public void SetBalance(ulong userID, int SetBalance, ItemContainer cont = null)
        {
            if(!config.UseUI)
            {
                BasePlayer player = BasePlayer.FindByID(userID);
                if (player == null)
                {
                    PrintWarning(GetLang("BALANCE_CUSTOM_MONEY_NOT_PLAYER"));
                    return;
                }
                Item Money = CreateCustomMoney(SetBalance);
                ItemContainer itemContainer = cont == null ? player.inventory.containerMain : cont;
		   		 		  						  	   		  		 			   		 		   		 		  	 	 
                if (player.inventory.containerMain.itemList.Count == 24)
                {
                    Item FindMoney = player.inventory.containerMain.FindItemsByItemName(Money.info.shortname);
                    if (FindMoney != null && FindMoney.skin == Money.skin)
                    {
                        FindMoney.amount += SetBalance;
                        FindMoney.MarkDirty();

                        if (config.UseAlert)
                            SendChat(GetLang("BALANCE_SET", player.UserIDString, SetBalance), player);
                        return;
                    }
                    SendChat(GetLang("BALANCE_CUSTOM_MONEY_INVENTORY_FULL", player.UserIDString), player);
                    Money.Drop(player.transform.position, Vector3.zero);
                    return;
                }
                Money.MoveToContainer(itemContainer);
                if (config.UseAlert)
                    SendChat(GetLang("BALANCE_SET", player.UserIDString, SetBalance), player);
            }
            else
            {
                if (IsData(userID))
                {
                    DataEconomics[userID].Balance += SetBalance;
                    BasePlayer player = BasePlayer.FindByID(userID);
                    if (player == null) return;
                    if (config.UseAlert)
                        SendChat(GetLang("BALANCE_SET", player.UserIDString, SetBalance), player);
                    if (config.ShowUI)
                        Interface_Balance(player);
                }
                else RegisteredDataUser(userID);
            }
            Interface.Oxide.CallHook("SET_BALANCE_USER", userID, SetBalance);
        }
        enum TypeData
        {
            None,
            AllWipe,
            AllWipeOnlyBalance,
            TimeClear,
            TimeClearOnlyBalance,
        }
        void ReadData() { 
            DataEconomics = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<UInt64, InformationData>>("IQEconomic/DataEconomics");
            ActualityLimitTime = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Int32>("IQEconomic/ActualityLimitTime");
        }
        void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (dispenser == null || player == null || item == null) return;
            var General = config.GeneralSetting.GatherReward;
            if (!General.UseReward) return;
            var GatherGeneral = General.GatherSetting;
            if (!String.IsNullOrWhiteSpace(General.PermissionReward) && !permission.UserHasPermission(player.UserIDString, General.PermissionReward)) return;

            if (!GatherGeneral.ContainsKey(item.info.shortname)) return;
            var Gather = GatherGeneral[item.info.shortname];
            if (!IsRare(Gather.Rare)) return;

            SetBalance(player.userID, Gather.Money);
        }
        private void OnServerShutdown() => Unload();

        public bool IsFriends(ulong userID, ulong targetID)
        {
            if (Friends)
                return (bool)Friends?.Call("HasFriend", userID, targetID);
            else return false;
        }
        public enum TierIQSphereEvent
        {
            Under,
            Around,
            Tier1,
            Tier2,
            Tier3,
            None
        }
        public bool IsClans(string userID, string targetID)
        {
            if (Clans)
            {
                if(Clans.Author.Contains("dcode"))
                {
                    String TagUserID = (String)Clans?.Call("GetClanOf", userID);
                    String TagTargetID = (String)Clans?.Call("GetClanOf", targetID);
                    return (bool)(TagUserID == TagTargetID);
                }
                else return (bool)Clans?.Call("IsClanMember", userID, targetID);
            }
            else return false;
        }
        private void OnServerInitialized()
        {
            permission.RegisterPermission(config.GeneralSetting.TimeReward.PermissionReward, this);
            permission.RegisterPermission(config.GeneralSetting.NPCReward.PermissionReward, this);
            permission.RegisterPermission(config.GeneralSetting.KilledReward.PermissionReward, this);
            permission.RegisterPermission(config.GeneralSetting.HelicopterReward.PermissionReward, this);
            permission.RegisterPermission(config.GeneralSetting.GatherReward.PermissionReward, this);
            permission.RegisterPermission(config.GeneralSetting.CollectableReward.PermissionReward, this);
            permission.RegisterPermission(config.GeneralSetting.BradleyReward.PermissionReward, this);
            permission.RegisterPermission(config.GeneralSetting.BarrelReward.PermissionReward, this);
            permission.RegisterPermission(config.GeneralSetting.AnimalReward.PermissionReward, this);

            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);

            AddAllImage();

            if (config.GeneralSetting.TimeReward.UseReward)
                timer.Every(120f, () => TrackerTime());

            if (config.GeneralSetting.DataFileSetting.SaveTime > 0)
                timer.Every(config.GeneralSetting.DataFileSetting.SaveTime, () => WriteData());

            ResetLimit();
        }

        private Dictionary<uint, Dictionary<ulong, int>> HeliAttackers = new Dictionary<uint, Dictionary<ulong, int>>();
        public Boolean IsLimited(UInt64 userID, Int32 Amount)
        {
            if (config.TransferSettings.UseLimits)
                return GetLimit(userID) < Amount;
            else return false;
        }
        public void SendChat(string Message, BasePlayer player, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            var Chat = config.ReferenceSettings.ChatSettings;
            if (IQChat)
                if (Chat.UIAlertUse)
                    IQChat?.Call("API_ALERT_PLAYER_UI", player, Message);
                else IQChat?.Call("API_ALERT_PLAYER", player, Message, Chat.CustomPrefix, Chat.CustomAvatar);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
            }
}
