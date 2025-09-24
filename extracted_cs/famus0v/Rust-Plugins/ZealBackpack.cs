using System;
using System.Collections;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("ZealBackpack", "Kira", "1.0.0")]
    [Description("Backpack")]
    public class ZealBackpack : RustPlugin
    {
        #region [Vars] / [Переменные]

        [PluginReference] private Plugin ImageLibrary;

        public Dictionary<ulong, BackpackStorage> opensBackpack = new Dictionary<ulong, BackpackStorage>();
        public static ZealBackpack ins = null;
        public string Layer = "UI.Backpack";
        public string LayerBlur = "UI.Backpack.Blur";

        #endregion

        #region [Commands] / [Команды]

        [ConsoleCommand("backpack.give")]
        void chatCmdBackpackGive(ConsoleSystem.Arg args)
        {
            if (!args.IsAdmin) return;

            var player = BasePlayer.FindByID(Convert.ToUInt64(args.Args[0]));

            if (player == null)
            {
                PrintError("Игрок не был найден!");
                return;
            }

            Item item = ItemManager.CreateByPartialName("wood", 1);
            item.name = _config.displayName;  
            item.skin = 1;
 
            player.GiveItem(item);
            PrintWarning($"Выдали рюкзак игроку {player.displayName}");
        } 

        [ChatCommand("backpack.spawn")]
        void chatCmdBackpackSpawn(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;

            Item item = ItemManager.CreateByPartialName("femalepubichair.style01", 1);
            item.name = _config.displayName;
            item.skin = _config.skinIdBackpack;
            string trimmed = _config.displayName.Trim();
            var name = trimmed.Substring(0, trimmed.IndexOf('\n'));

            item.MoveToContainer(player.inventory.containerMain);
            player.SendConsoleCommand($"note.inv {item.info.itemid} 1 \"{name}\"");
            SendReply(player, "Рюкзак был успешно выдан!");
        }

        private void chatCmdBackpackOpen(BasePlayer player, string command, string[] args)
        {
            player.SendConsoleCommand(_config.consoleCommand);
        }

        private void consoleCmdBackpack(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection?.player as BasePlayer;

            if (player == null) return;

            if (player.inventory.loot?.entitySource != null)
            {
                BackpackStorage backpackStorage;
                if (opensBackpack.TryGetValue(player.userID, out backpackStorage) &&
                    backpackStorage.gameObject == player.inventory.loot.entitySource.gameObject) return;

                player.EndLooting();

                timer.Once(0.1f, () => BackpackOpen(player));
            }
            else BackpackOpen(player);
        }

        #endregion

        #region [Hooks] / [Крюки]

        private bool? CanWearItem(PlayerInventory inventory, Item item)
        {
            var player = inventory.gameObject.ToBaseEntity() as BasePlayer;

            if (player == null) return null;
            if (player.IsNpc) return null;

            if (item.skin == _config.skinIdBackpack)
                if (player.inventory.containerWear.itemList.Find(x => x.skin == _config.skinIdBackpack) != null)
                    return false;
 
            return null;
        }

        void OnServerInitialized()
        {
            LoadData();
            LoadConfig();

            CheckConfig();
            UpdateData();

            cmd.AddConsoleCommand(_config.consoleCommand, this, "consoleCmdBackpack");
            cmd.AddChatCommand(_config.chatCommandOpen, this, chatCmdBackpackOpen);

            ins = this;
        }

        void UpdateData()
        {
            SaveData();
            timer.Once(300f, () => UpdateData());
        }

        object CanAcceptItem(ItemContainer container, Item item)
        {
            if (item.IsLocked())
                return ItemContainer.CanAcceptResult.CannotAccept;

            return null;
        }

        void Unload()
        {
            foreach (var backpack in opensBackpack)
                backpack.Value.Close();

            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
                CuiHelper.DestroyUi(player, LayerBlur);
            }

            SaveData();
        }

        #endregion

        #region [Helpers] / [Вспомогательный код]

        public Item FindItem(BasePlayer player, int itemID, ulong skinID, int amount)
        {
            Item item = null;

            if (skinID == 0U)
            {
                if (player.inventory.FindItemID(itemID) != null && player.inventory.FindItemID(itemID).amount >= amount)
                    return player.inventory.FindItemID(itemID);
            }
            else
            {
                List<Item> items = new List<Item>();

                items.AddRange(player.inventory.FindItemIDs(itemID));

                foreach (var findItem in items)
                {
                    if (findItem.skin == skinID && findItem.amount >= amount)
                    {
                        return findItem;
                    }
                }
            }

            return item;
        }

        public bool HaveItem(BasePlayer player, int itemID, ulong skinID, int amount)
        {
            if (skinID == 0U)
            {
                if (player.inventory.FindItemID(itemID) != null &&
                    player.inventory.FindItemID(itemID).amount >= amount) return true;
                return false;
            }

            List<Item> items = new List<Item>();

            items.AddRange(player.inventory.FindItemIDs(itemID));

            foreach (var item in items)
            {
                if (item.skin == skinID && item.amount >= amount)
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerator DownloadImages()
        {
            ImageLibrary.Call("AddImage", $"http://api.hougan.space/rust/skin/getImage/{_config.skinIdBackpack}",
                "femalepubichair.style01", _config.skinIdBackpack);

            PrintError("AddImages");

            yield return 0;
        }

        List<SavedItem> SaveItems(List<Item> items) => items.Select(SaveItem).ToList();

        bool BackpackHide(uint itemID, ulong playerId)
        {
            BackpackStorage backpackStorage;
            if (!opensBackpack.TryGetValue(playerId, out backpackStorage)) return false;
            opensBackpack.Remove(playerId);
            if (backpackStorage == null) return false;
            var items = SaveItems(backpackStorage.GetItems);
            if (items.Count > 0) storedData.backpacks[itemID] = items;
            else storedData.backpacks.Remove(itemID);

            backpackStorage.Close();

            return true;
        }

        void BackpackOpen(BasePlayer player)
        {
            if (player.inventory.loot?.entitySource != null) return;

            Item backpack = null;

            foreach (var item in player.inventory.containerWear.itemList)
            {
                if (item.skin == _config.skinIdBackpack)
                {
                    backpack = item;
                    break;
                }
            }

            if (backpack == null)
            {
                SendReply(player, "Для того, чтобы воспользоваться рюкзаком, необходимо одеть его в слоты одежды!");
                return;
            }

            if (Interface.Oxide.CallHook("CanBackpackOpen", player) != null) return;

            timer.Once(0.1f, () =>
            {
                if (!player.IsOnGround())
                {
                    SendReply(player, "Сначала приземлитесь, а потом пробуйте открыть рюкзак!!");
                    return;
                }

                List<SavedItem> savedItems;
                List<Item> items = new List<Item>();
                if (storedData.backpacks.TryGetValue(backpack.uid, out savedItems))
                    items = RestoreItems(savedItems);
                BackpackStorage backpackStorage = BackpackStorage.Spawn(player);

                opensBackpack.Add(player.userID, backpackStorage);
                if (items.Count > 0)
                    backpackStorage.Push(items);
                backpackStorage.StartLoot();
            });
        }

        List<Item> RestoreItems(List<SavedItem> sItems)
        {
            return sItems.Select(sItem =>
            {
                if (sItem.weapon) return BuildWeapon(sItem);
                return BuildItem(sItem);
            }).Where(i => i != null).ToList();
        }

        Item BuildItem(SavedItem sItem)
        {
            if (sItem.amount < 1) sItem.amount = 1;
            Item item = null;
            item = ItemManager.CreateByItemID(sItem.itemid, sItem.amount, sItem.skinid);

            if (item.hasCondition)
            {
                item.condition = sItem.condition;
                item.maxCondition = sItem.maxcondition;
                item.busyTime = sItem.busyTime;
            }

            if (sItem.name != null)
            {
                item.name = sItem.name;
            }

            if (sItem.OnFire)
            {
                item.SetFlag(global::Item.Flag.OnFire, true);
            }

            FlameThrower flameThrower = item.GetHeldEntity()?.GetComponent<FlameThrower>();
            if (flameThrower)
                flameThrower.ammo = sItem.flamefuel;
            return item;
        }

        Item BuildWeapon(SavedItem sItem)
        {
            Item item = null;
            item = ItemManager.CreateByItemID(sItem.itemid, 1, sItem.skinid);

            if (item.hasCondition)
            {
                item.condition = sItem.condition;
                item.maxCondition = sItem.maxcondition;
            }

            var weapon = item.GetHeldEntity() as BaseProjectile;
            if (weapon != null)
            {
                var def = ItemManager.FindItemDefinition(sItem.ammotype);
                weapon.primaryMagazine.ammoType = def;
                weapon.primaryMagazine.contents = sItem.ammoamount;
            }

            if (sItem.mods != null)
                foreach (var mod in sItem.mods)
                    item.contents.AddItem(BuildItem(mod).info, 1);
            return item;
        }

        SavedItem SaveItem(Item item)
        {
            SavedItem iItem = new SavedItem
            {
                shortname = item.info?.shortname,
                amount = item.amount,
                mods = new List<SavedItem>(),
                skinid = item.skin,
                busyTime = item.busyTime,
            };
            if (item.HasFlag(global::Item.Flag.OnFire))
            {
                iItem.OnFire = true;
            }

            if (item.info == null) return iItem;
            iItem.itemid = item.info.itemid;
            iItem.weapon = false;

            iItem.name = item.name;
            if (item.hasCondition)
            {
                iItem.condition = item.condition;
                iItem.maxcondition = item.maxCondition;
            }

            FlameThrower flameThrower = item.GetHeldEntity()?.GetComponent<FlameThrower>();
            if (flameThrower != null)
                iItem.flamefuel = flameThrower.ammo;
            if (item.info.category.ToString() != "Weapon") return iItem;
            BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
            if (weapon == null) return iItem;
            if (weapon.primaryMagazine == null) return iItem;
            iItem.ammoamount = weapon.primaryMagazine.contents;
            iItem.ammotype = weapon.primaryMagazine.ammoType.shortname;
            iItem.weapon = true;
            if (item.contents != null)
                foreach (var mod in item.contents.itemList)
                    if (mod.info.itemid != 0)
                        iItem.mods.Add(SaveItem(mod));
            return iItem;
        }

        #endregion
        
        #region [Classes] / [Классы]

        public class BackpackStorage : MonoBehaviour
        {
            public StorageContainer container;
            public Item backpack;
            public BasePlayer player;

            public void Initialization(StorageContainer container, Item backpack, BasePlayer player)
            {
                this.container = container;
                this.backpack = backpack;
                this.player = player;

                container.ItemFilter(backpack, -1);

                BlockBackpackSlots(true);
            }

            public List<Item> GetItems => container.inventory.itemList.Where(i => i != null).ToList();

            public static StorageContainer CreateContainer(BasePlayer player)
            {
                var storage =
                    GameManager.server.CreateEntity("assets/prefabs/deployable/small stash/small_stash_deployed.prefab")
                        as StorageContainer;
                if (storage == null) return null;
                storage.transform.position = new Vector3(0f, 100f, 0);
                storage.panelName = "largewoodbox";
                ItemContainer container = new ItemContainer {playerOwner = player};
                container.ServerInitialize((Item) null, ins._config.backpackSize);
                if ((int) container.uid == 0)
                    container.GiveUID();
                storage.inventory = container;
                if (!storage) return null;
                storage.SendMessage("SetDeployedBy", player, (SendMessageOptions) 1);
                storage.Spawn();

                return storage;
            }

            private void PlayerStoppedLooting(BasePlayer player)
            {
                BlockBackpackSlots(false);

                ins.BackpackHide(backpack.uid, player.userID);
            }

            public void StartLoot()
            {
                container.SetFlag(BaseEntity.Flags.Open, true, false);
                player.inventory.loot.StartLootingEntity(container, false);
                player.inventory.loot.AddContainer(container.inventory);
                player.inventory.loot.SendImmediate();
                player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", container.panelName);
                container.DecayTouch();
                container.SendNetworkUpdate();
            }

            public static BackpackStorage Spawn(BasePlayer player)
            {
                player.EndLooting();
                var storage = CreateContainer(player);

                Item backpack = null;

                backpack = player.inventory.containerWear.itemList.Find(x => x.skin == ins._config.skinIdBackpack);

                if (backpack == null) return null;

                var box = storage.gameObject.AddComponent<BackpackStorage>();

                box.Initialization(storage, backpack, player);

                return box;
            }

            public void Close()
            {
                container.inventory.itemList.Clear();
                container.Kill();
            }

            public void Push(List<Item> items)
            {
                for (int i = items.Count - 1; i >= 0; i--)
                    items[i].MoveToContainer(container.inventory);
            }

            public void BlockBackpackSlots(bool state)
            {
                backpack.LockUnlock(state);

                foreach (var item in player.inventory.AllItems())
                    if (item.skin == ins._config.skinIdBackpack)
                        item.LockUnlock(state);
            }
        }

        #endregion

        #region [DataBase] / [База данных]

        class StoredData
        {
            public Dictionary<uint, List<SavedItem>> backpacks = new Dictionary<uint, List<SavedItem>>();
        }

        public class SavedItem
        {
            public string shortname;
            public int itemid;
            public float condition;
            public float maxcondition;
            public int amount;
            public int ammoamount;
            public string ammotype;
            public int flamefuel;
            public ulong skinid;
            public string name;
            public bool weapon;
            public float busyTime;
            public bool OnFire;
            public List<SavedItem> mods;
        }

        void SaveData()
        {
            BackpackData.WriteObject(storedData);
        }

        void LoadData()
        {
            BackpackData = Interface.Oxide.DataFileSystem.GetFile(_config.fileName);
            try
            {
                storedData =
                    Interface.Oxide.DataFileSystem.ReadObject<StoredData>(_config.fileName);
            }
            catch
            {
                storedData = new StoredData();
            }
        }

        StoredData storedData;
        private DynamicConfigFile BackpackData;

        #endregion

        #region [Configuration] / [Конфигурация]

        public void CheckConfig()
        {
            if (_config.backpackSize > 30)
                _config.backpackSize = 30;

            ServerMgr.Instance.StartCoroutine(DownloadImages());

            PrintError("Проверка конфига выполнена успешно!");
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();
        }

        protected override void LoadDefaultConfig()
        {
            _config = new Configuration()
            {
                backpackSize = 12,
                fileName = "Backpack/backpack",
                skinIdBackpack = 1720637353U,
                consoleCommand = "backpack.open",
                chatCommandOpen = "bp",
                chatCommand = "backpack",
                displayName = "Рюкзак",
            };
        }

        public Configuration _config;

        public class Configuration
        {
            [JsonProperty("Консольная команда для открытия рюкзака")]
            public string consoleCommand = "";

            [JsonProperty("Чат команда для открытия крафта рюкзака")]
            public string chatCommand = "";

            [JsonProperty("Чат команда для открытия самого рюкзака")]
            public string chatCommandOpen = "backpack";

            [JsonProperty("Количество слотов в рюкзаке")]
            public int backpackSize = 0;

            [JsonProperty("Название для рюкзака")] public string displayName = "";

            [JsonProperty("СкинИД рюкзака")] public ulong skinIdBackpack = 1719575499U;

            [JsonProperty("Расположение Data файла")]
            public string fileName = "Backpack/backpack";
        }

        public class ItemInfo
        {
            [JsonProperty("Шортнейм предмета")] public string shortname = "";
            [JsonProperty("Количество предмета")] public int amount = 0;
            [JsonProperty("СкинИД предмета")] public ulong skinID = 0U;
            [JsonProperty("АйтемИД предмета")] public int itemID = 0;
        }

        #endregion
    }
}