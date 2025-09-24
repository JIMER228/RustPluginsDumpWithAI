// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("MiningFerma","Sempai#3239","3.0.0")]
    class MiningFerma : RustPlugin
    {
        #region зависимости
        [PluginReference] Plugin IQEconomic, ImageLibrary;
        public void apisetbalance(ulong userID, int Balance) => IQEconomic?.CallHook("API_SET_BALANCE", userID, Balance);
        public bool apiindata(ulong userID) => (bool)IQEconomic?.CallHook("API_IS_USER", userID);
        public Item apiitem(int Amount) => (Item)IQEconomic?.CallHook("API_GET_ITEM", Amount);
        public bool apiisremoved(ulong userID, int Amount) => (bool)IQEconomic?.CallHook("API_IS_REMOVED_BALANCE", userID, Amount);
        public void apiremovebalance(ulong userID, int Balance) => IQEconomic?.CallHook("API_REMOVE_BALANCE", userID, Balance);
        Timer setzero;
        private List<string> ListSpawnBatarey = new List<string>() // Выпадение золота!
        {
            {"codelockedhackablecrate"},
            {"crate_basic"},
            {"crate_elite"},
            {"crate_normal"},
            {"supply_drop"},
            {"loot-barrel-1"},
            {"loot-barrel-2"},
            {"bradley_crate"},
        };
        private List<LootContainer> handledContainers = new List<LootContainer>();
        #endregion зависимости      

        #region Configuration

        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Настройка Фермы")]
            public FermSet Fermsettings = new FermSet();
            [JsonProperty("Поддержка плагина IQEconomic")]
            public bool Economic;
            [JsonProperty("Шанс на дроп компонентов в ящике")]
            public int chanse;
            [JsonProperty("Частота обновления")]
            public float time;

            internal class FermSet
            {
                [JsonProperty("Shortname коина")]
                public string Shortname;
                [JsonProperty("Название коина")]
                public string Name;
                [JsonProperty("SkinID коина")]
                public ulong SkinID;
                [JsonProperty("Падающее кол-во")]
                public int ammount;
            }
            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    Economic = false,
                    chanse = 15,
                    time = 15f,
                    Fermsettings = new FermSet
                    {
                        Shortname = "glue",
                        Name = "Золото",
                        SkinID = 2554884075,
                        ammount = 1
                    }
                };
            }
        }
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
                PrintWarning("Ошибка #1" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }
        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        Timer Timer = null;
        void MiningStart()
        {
            float time = config.time;
            Timer = timer.Every(time, () => changepower());
        }
        #endregion

        #region data
        void OnServerInitialized()
        {
			PrintWarning("\n-----------------------------\n" +
            "     Author - Sempai#3239\n" +
            "     VK - https://vk.com/rustnastroika/n" +
            "     Discord - https://discord.gg/5DPTsRmd3G/n" +
            "-----------------------------");
            CreateDataBase();
            foreach (var check in BasePlayer.activePlayerList)
                OnPlayerConnected(check);

            ConsoleSystem.Run(ConsoleSystem.Option.Unrestricted, "cmdminingstart");
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (!DB.ContainsKey(player.userID))
                DB.Add(player.userID, new bool());
        }

        void Unload() => SaveDataBase();
        void GetNewSave()
        { 
           PrintWarning("Обнаружен вайп. Очищаем данные с data/MiningBox");
           DB.Clear();
           SaveDataBase();
        }
        #endregion data

        #region Дата
        Dictionary<ulong, bool> DB = new Dictionary<ulong, bool>();

        void CreateDataBase()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("MiningFerma/PlayerList"))
                DB = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, bool>>("MiningFerma/PlayerList");
        }

        void SaveDataBase() => Interface.Oxide.DataFileSystem.WriteObject($"MiningFerma/PlayerList", DB);
        #endregion

        #region gets
        public ElectricBattery GetsMyBattary(ulong userid)
        {
            foreach (var battary in UnityEngine.Object.FindObjectsOfType<ElectricBattery>().ToList().Where(x => x.OwnerID == userid))                            
                return battary;            
            return null;
        }
        public Mailbox GetMailBox(ulong userid)
        {
                foreach (var mail in UnityEngine.Object.FindObjectsOfType<Mailbox>().ToList().Where(x => x.skinID == 2107419166 && x.OwnerID == userid))
                    return mail;
            return null;
        }
        public RFBroadcaster Getbroadcas(ulong userid)
        {
                foreach (var broadcaster in UnityEngine.Object.FindObjectsOfType<RFBroadcaster>().ToList().Where(x => x.OwnerID == userid))
                    return broadcaster;
            return null;
        }
        public FlasherLight GetFlasser(ulong userid)
        {
            foreach (var flasser in UnityEngine.Object.FindObjectsOfType<FlasherLight>().ToList().Where(x => x.OwnerID == userid))
                return flasser;
            return null;
        }

        #endregion gets 

        #region methods
        void spawnmail(BasePlayer player)
        {
            RFBroadcaster broadcaster = Getbroadcas(player.userID);           
            Splitter splitter = IOEntity.FindObjectOfType<Splitter>();
            if (splitter == null) return;
            if (!broadcaster.IsConnectedTo(splitter, 0, 1))return;
            ElectricBattery batareya = GetsMyBattary(player.userID);
            if (batareya == null) return;
            if (!batareya.IsConnectedTo(splitter, 0, 1)) return;
            FlasherLight flas = GetFlasser(player.userID);
            if (flas == null) return;
            if (!flas.IsConnectedTo(splitter, 0, 1)) return;
            var bpos = broadcaster.transform.position;
            CreateMailBox(player.userID, bpos);
            broadcaster.Kill();
            DB[player.userID] = true;
            changepower();
            MiningStart();
            SaveDataBase();
        }
        void changepower()
        {   
            foreach (var check in DB)
            {
                if (check.Value == false) return;
                ElectricBattery battarey = GetsMyBattary(check.Key);
                FlasherLight flas = GetFlasser(check.Key);
                if (battarey == null || flas == null)
                {
                    DB[check.Key] = false;
                    SaveDataBase();
                    return;
                }             
                Drop(check.Key);
            }
        }
        void Drop(ulong userID)
        {
            var sett = config.Fermsettings;
            Mailbox mail = GetMailBox(userID);
            if (mail == null) return;
            if (mail.skinID != 2107419166) return;
            var itemContainer = mail.inventory;
            Item item = ItemManager.CreateByItemID(1414245162, sett.ammount, sett.SkinID);
            item.name = sett.Name;
            item.MoveToContainer(itemContainer);
        }
        #endregion methods

        #region commands
        [ChatCommand("admingive")]
        void admingive(BasePlayer player)
        {      
            if (player.net.connection.authLevel == 2)
            {
                CreateBatar(player);
                Createbroadcaster(player);
                Createflasher(player);
            }
            
        }    
        [ChatCommand("coingive")]
        void asdasd(BasePlayer player)
        {
            if (player.net.connection.authLevel == 2)
            {
                var item = ItemManager.CreateByName("glue", 1000, 2554884075);
                player.GiveItem(item);
            }
            else { SendReply(player, "У вас нет доступа к этой команде!"); }
        }
        [ConsoleCommand("cmdminingstart")]
        void minins(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            MiningStart();
        }

        #endregion commands

        #region hooks
        object OnOutputUpdate(IOEntity entity)
        {
            var ownerID = entity.GetComponent<BaseEntity>().OwnerID;
            if (ownerID == 0) return null;
            var i = 364; if (i == 364) { };
            BasePlayer player = BasePlayer.FindByID(ownerID);
            if (player == null) return null;
            RFBroadcaster broadcas = Getbroadcas(player.userID);
            if (broadcas == null) return null;
            if (broadcas.frequency != 2020) return null;
            if (!broadcas.IsPowered()) return null;
            if (DB[player.userID] == true)
            {
                SendReply(player, "У вас уже создана ферма!");
                return null;
            }
            spawnmail(player);
            return null;
        }
        void OnLootEntity(BasePlayer player, BaseEntity entity, Item item)
        {
            if (!(entity is LootContainer)) return;
            var container = (LootContainer)entity;
            if (handledContainers.Contains(container) || container.ShortPrefabName == "stocking_large_deployed" ||
               container.ShortPrefabName == "stocking_small_deployed") return;
            handledContainers.Add(container);
            List<int> ItemsList = new List<int>();
            if (ListSpawnBatarey.Contains(container.ShortPrefabName))
            {
                var random = UnityEngine.Random.Range(1 , 3);
                var droprandom = UnityEngine.Random.Range(0f, 100f);
                if (droprandom < config.chanse)
                {
                    var itemContainer = container.inventory;
                    foreach (var i1 in itemContainer.itemList)
                    {
                        ItemsList.Add(i1.info.itemid);
                    }
                    if (random == 1 && !ItemsList.Contains(-692338819))
                    {
                        if (container.inventory.itemList.Count == container.inventory.capacity)
                            container.inventory.capacity++;
                        item = ItemManager.CreateByName("electric.battery.rechargable.small", 1, 0);
                        item.name = "Блок питания для фермы";
                        item.MoveToContainer(itemContainer);
                    }
                    if (random == 2 && !ItemsList.Contains(-939424778))
                    {
                        if (container.inventory.itemList.Count == container.inventory.capacity)
                            container.inventory.capacity++;
                        item = ItemManager.CreateByName("electric.flasherlight", 1, 0);
                        item.name = "Индикатор для фермы";
                        item.MoveToContainer(itemContainer);
                    }
                    if (random == 3 && !ItemsList.Contains(-1044468317))
                    {
                        if (container.inventory.itemList.Count == container.inventory.capacity)
                            container.inventory.capacity++;
                        item = ItemManager.CreateByName("electric.rf.broadcaster", 1, 0);
                        item.name = "Видеокарта для фермы";
                        item.MoveToContainer(itemContainer);
                    }
                }
            }
        }
        void OnEntitySpawned(IOEntity entity)
        {
            var ownerID = entity.GetComponent<BaseEntity>().OwnerID;
            if (ownerID == 0) return;
            BasePlayer player = BasePlayer.FindByID(ownerID);
            if (player == null) return;
            ElectricBattery battery = IOEntity.FindObjectOfType<ElectricBattery>();  
            if (entity == battery)
                SendReply(player, "<color=red>Внимание!</color> Если вы подберёте компонент фермы после её спавна она будет удалена!\nБудте внимательны!");    
            return;
        }

        object CanMoveItem(Item item, PlayerInventory playerinventory)
        {
            var sett = config.Fermsettings;
            var player = playerinventory.GetComponent<BasePlayer>();
            var amount = item.amount;
            if (item.info.itemid == 1414245162 && item.skin != 0)
            {
                if (config.Economic)
                {
                    if (IQEconomic)
                    {
                        if (apiindata(player.userID))
                        {
                            apisetbalance(player.userID, amount);
                            item.Remove();
                            return null;
                        }
                    }
                }
                var name = ItemManager.CreateByName(sett.Shortname, amount, sett.SkinID);
                if (!playerinventory.GiveItem(name))
                {
                    name.Drop(playerinventory.containerMain.dropPosition, playerinventory.containerMain.dropVelocity);
                }
                item.Remove();
            }
            return null;
        }
        private void OnItemAddedToContainer(ItemContainer container, Item item, BasePlayer player)
        {
            var sett = config.Fermsettings;
            var iname = item.info.shortname.ToLower();
            if (iname == sett.Shortname)
            {
                item.name = sett.Name;
                item.skin = sett.SkinID;
            }

        }
        #endregion hooks

        #region craeator
        void CreateBatar(BasePlayer player)
        {
            Item batarey = ItemManager.CreateByName("electric.battery.rechargable.small", 1, 0);
            bool bat = batarey.CanStack(batarey);
            bat = false;
            player.GiveItem(batarey);
        }
        void Createbroadcaster(BasePlayer player)
        {
            Item broadcaster = ItemManager.CreateByItemID(-1044468317, 1, 0);//2107606507
            player.GiveItem(broadcaster);
        }
        void Createflasher(BasePlayer player)
        {
            Item flasher = ItemManager.CreateByItemID(-939424778, 1, 0);
            bool fla = flasher.CanStack(flasher);
            fla = false;
            player.GiveItem(flasher);
        }
        private void CreateBroadcaster(ulong userID, Vector3 pos)
        {
            RFBroadcaster broadcaster = GameManager.server.CreateEntity("assets/prefabs/deployable/playerioents/gates/rfbroadcaster/rfbroadcaster.prefab", pos, Quaternion.identity) as RFBroadcaster;
            if (broadcaster == null) return;
            broadcaster.OwnerID = userID;
            broadcaster.Spawn();
            broadcaster.SendNetworkUpdate();
        }
        private void CreateMailBox(ulong userID, Vector3 pos)
        {
            Mailbox mail = GameManager.server.CreateEntity("assets/prefabs/deployable/mailbox/mailbox.deployed.prefab", pos, Quaternion.identity) as Mailbox;
            if (mail == null) return;
            mail.OwnerID = userID;
            mail.Spawn();
            mail.skinID = 2107419166;
            mail.SendNetworkUpdate();
        }
        #endregion creator

    }
}
