using ConVar;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SaveLootDeath", "Mercury", "1.0.4")]
    [Description("SaveLootDeath")]
    internal class SaveLootDeath : RustPlugin
    {
        #region Reference

        [PluginReference] private readonly Plugin IQChat;

        public void SendChat(String Message, BasePlayer player, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            Configuration.Reference.IQChat Chat = config.References.IQChatSetting;
            if (IQChat)
            {
                if (Chat.UIAlertUse)
                    IQChat?.Call("API_ALERT_PLAYER_UI", player, Message);
                else IQChat?.Call("API_ALERT_PLAYER", player, Message, Chat.CustomPrefix, Chat.CustomAvatar);
            }
            else
            {
                player.SendConsoleCommand("chat.add", channel, 0, Message);
            }
        }

        #endregion

        #region Vars

        private const String PermissionAdmin = "savelootdeath.admin";

        #endregion

        #region Data

        public Dictionary<UInt64, ItemSaved> SaveItems = new Dictionary<UInt64, ItemSaved>();

        internal class ItemSaved
        {
            public List<SavedItem> Items = new List<SavedItem>();

            internal class SavedItem
            {
                public ItemContainer container;
                public Int32 TargetSlot;
                public String Shortname;
                public Int32 Itemid;
                public Single Condition;
                public Single Maxcondition;
                public Int32 Amount;
                public Int32 Ammoamount;
                public String Ammotype;
                public Int32 Flamefuel;
                public UInt64 Skinid;
                public String Name;
                public Boolean Weapon;
                public Int32 Blueprint;
                public Single BusyTime;
                public Boolean OnFire;
                public List<SavedItem> Mods;
            }
        }

        private void ReadData()
        {
            SaveItems = Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<UInt64, ItemSaved>>(
                "SaveLootDeath/Users");
        }

        private void WriteData()
        {
            Core.Interface.Oxide.DataFileSystem.WriteObject("SaveLootDeath/Users", SaveItems);
        }

        #endregion

        #region Configuration

        private static Configuration config = new Configuration();

        private class Configuration
        {
            [JsonProperty("Настройка поддерживающих плагинов")]
            public Reference References = new Reference();

            internal class Reference
            {
                [JsonProperty("Настройка IQChat")] public IQChat IQChatSetting = new IQChat();

                internal class IQChat
                {
                    [JsonProperty("IQChat : Кастомный префикс в чате")]
                    public String CustomPrefix = "[IQBackpack]";

                    [JsonProperty("IQChat : Кастомный аватар в чате(Если требуется)")]
                    public String CustomAvatar = "0";

                    [JsonProperty("IQChat : Использовать UI уведомления")]
                    public Boolean UIAlertUse = false;
                }
            }

            public Dictionary<String, List<SaveItem>> SavedsList = new Dictionary<String, List<SaveItem>>();

            internal class SaveItem
            {
                public String Shortname;
                public UInt64 SkinID;
            }

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    References = new Reference
                    {
                        IQChatSetting = new Reference.IQChat
                        {
                            CustomAvatar = "0",
                            CustomPrefix = "SaveLoot",
                            UIAlertUse = false,
                        }
                    },
                    SavedsList = new Dictionary<String, List<SaveItem>>
                    {
                        ["savelootdeath.default"] = new List<SaveItem>
                        {
                            new SaveItem
                            {
                                Shortname = "rifle.ak",
                                SkinID = 0,
                            },
                            new SaveItem
                            {
                                Shortname = "smg.thompson",
                                SkinID = 0,
                            },
                            new SaveItem
                            {
                                Shortname = "pickaxe",
                                SkinID = 859006499,
                            },
                        },
                        ["savelootdeath.vip"] = new List<SaveItem>
                        {
                            new SaveItem
                            {
                                Shortname = "rifle.ak",
                                SkinID = 0,
                            },
                            new SaveItem
                            {
                                Shortname = "smg.thompson",
                                SkinID = 0,
                            },
                            new SaveItem
                            {
                                Shortname = "pickaxe",
                                SkinID = 859006499,
                            },
                        },
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
                PrintWarning($"Ошибка #58 чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig()
        {
            config = Configuration.GetNewConfiguration();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

        #region Metods

        private void PushItem(BasePlayer player, Boolean IsDeath = false)
        {
            if (!SaveItems.ContainsKey(player.userID)) return;

            List<ItemInfo> items = RestoreItems(SaveItems[player.userID].Items);
            if (items == null || items.Count == 0) return;
            foreach (ItemInfo item in items)
            {
//                if (!item.item.MoveToContainer(item.container, item.item.position))
                player.GiveItem(item.item);
            }

            SaveItems[player.userID].Items.Clear();

            if (IsDeath)
                SendChat(GetLang("ITEM_PUSH", player.UserIDString), player);
        }

        private List<Item> GetSavedItemList(BasePlayer player)
        {
            List<Item> SaveList = new List<Item>();
            KeyValuePair<String, List<Configuration.SaveItem>> Information =
                config.SavedsList.FirstOrDefault(x => permission.UserHasPermission(player.UserIDString, x.Key));
            if (Information.Value == null) return null;
            if (!permission.UserHasPermission(player.UserIDString, PermissionAdmin))
            {
                SaveList.AddRange(from item in player.inventory.AllItems().ToList()
                    from Saveds in Information.Value
                    where item.info.shortname == Saveds.Shortname && item.skin == Saveds.SkinID
                    select item);
            }
            else
            {
                foreach (Item item in player.inventory.AllItems().ToList())
                    SaveList.Add(item);
            }

            return SaveList;
        }

        private void SavedDeath(BasePlayer player)
        {
            //  if (config.SavedsList.FirstOrDefault(x => permission.UserHasPermission(player.UserIDString, x.Key)).Value == null) return;
            // if (!permission.UserHasPermission(player.UserIDString, PermissionUse)) return;

            List<Item> SaveList = GetSavedItemList(player);
            if (SaveList == null || SaveList.Count == 0) return;
            SaveItems[player.userID].Items = SaveItemsList(SaveList);

            foreach (Item item in SaveList)
            {
                item.Remove();
            }
        }

        #region Help Metods

        // private System.Object CanDropActiveItem(BasePlayer player)
        // {
        //     return false;
        // }

        private static List<ItemSaved.SavedItem> SaveItemsList(List<Item> items)
        {
            return items.Select(SaveItem).ToList();
        }

        private static ItemSaved.SavedItem SaveItem(Item item)
        {
            ItemSaved.SavedItem iItem = new ItemSaved.SavedItem
            {
                container = item.GetRootContainer(),
                TargetSlot = item.position,
                Shortname = item.info?.shortname,
                Amount = item.amount,
                Mods = new List<ItemSaved.SavedItem>(),
                Skinid = item.skin,
                BusyTime = item.busyTime,
            };

            if (item.HasFlag(global::Item.Flag.OnFire))
            {
                iItem.OnFire = true;
            }

            if (item.info == null) return iItem;
            iItem.Itemid = item.info.itemid;
            iItem.Weapon = false;

            if (item.contents != null && item.info.category.ToString() != "Weapon")
            {
                foreach (Item itemCont in item.contents.itemList)
                {
                    Debug.Log(itemCont.info.shortname);

                    if (itemCont.info.itemid != 0)
                        iItem.Mods.Add(SaveItem(itemCont));
                }
            }

            iItem.Name = item.name;
            if (item.hasCondition)
            {
                iItem.Condition = item.condition;
                iItem.Maxcondition = item.maxCondition;
            }

            if (item.blueprintTarget != 0) iItem.Blueprint = item.blueprintTarget;

            FlameThrower flameThrower = item.GetHeldEntity()?.GetComponent<FlameThrower>();
            if (flameThrower != null)
                iItem.Flamefuel = flameThrower.ammo;
            if (item.info.category.ToString() != "Weapon") return iItem;
            BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
            if (weapon == null) return iItem;
            if (weapon.primaryMagazine == null) return iItem;
            iItem.Ammoamount = weapon.primaryMagazine.contents;
            iItem.Ammotype = weapon.primaryMagazine.ammoType.shortname;
            iItem.Weapon = true;

            if (item.contents != null)
            {
                foreach (Item mod in item.contents.itemList)
                {
                    if (mod.info.itemid != 0)
                        iItem.Mods.Add(SaveItem(mod));
                }
            }

            return iItem;
        }

        private static Item BuildItem(ItemSaved.SavedItem sItem)
        {
            if (sItem.Amount < 1) sItem.Amount = 799 > 0 ? 1 : 0;
            Item item = null;
            item = ItemManager.CreateByItemID(sItem.Itemid, sItem.Amount, sItem.Skinid);
//            item.position = sItem.TargetSlot;

            if (item.hasCondition)
            {
                item.condition = sItem.Condition;
                item.maxCondition = sItem.Maxcondition;
                item.busyTime = sItem.BusyTime;
            }

            if (sItem.Blueprint != 0)
                item.blueprintTarget = sItem.Blueprint;

            if (sItem.Mods != null)
            {
                if (sItem.Mods != null)
                {
                    foreach (ItemSaved.SavedItem mod in sItem.Mods)
                        item.contents.AddItem(BuildItem(mod).info, mod.Amount);
                }
            }

            if (sItem.Name != null)
                item.name = sItem.Name;

            if (sItem.OnFire)
                item.SetFlag(global::Item.Flag.OnFire, true);

            FlameThrower flameThrower = item.GetHeldEntity()?.GetComponent<FlameThrower>();
            if (flameThrower)
                flameThrower.ammo = sItem.Flamefuel;
            return item;
        }

        private static Item BuildWeapon(ItemSaved.SavedItem sItem)
        {
            Item item = null;
            item = ItemManager.CreateByItemID(sItem.Itemid, 1, sItem.Skinid);
//            item.position = sItem.TargetSlot;

            if (item.hasCondition)
            {
                item.condition = sItem.Condition;
                item.maxCondition = sItem.Maxcondition;
            }

            if (sItem.Blueprint != 0)
                item.blueprintTarget = sItem.Blueprint;

            BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
            if (weapon != null)
            {
                ItemDefinition def = ItemManager.FindItemDefinition(sItem.Ammotype);
                weapon.primaryMagazine.ammoType = def;
                weapon.primaryMagazine.contents = sItem.Ammoamount;
            }

            if (sItem.Mods != null)
            {
                foreach (ItemSaved.SavedItem mod in sItem.Mods)
                    item.contents.AddItem(BuildItem(mod).info, 1);
            }

            return item;
        }

        internal class ItemInfo
        {
            public Item item;
            public ItemContainer container;
        }

        private static List<ItemInfo> RestoreItems(List<ItemSaved.SavedItem> sItems)
        {
            List<ItemInfo> ItemInfoList = new List<ItemInfo>();

            foreach (ItemSaved.SavedItem sItem in sItems)
            {
                ItemInfo info = new ItemInfo();
                if (sItem.Weapon)
                    info.item = BuildWeapon(sItem);
                else
                    info.item = BuildItem(sItem);

                info.container = sItem.container;
                ItemInfoList.Add(info);
            }

            return ItemInfoList;
            //return sItems.Select(sItem =>
            //{
            //    if (sItem.Weapon)
            //        return BuildWeapon(sItem);
            //    return BuildItem(sItem);
            //}).Where(i => i != null).ToList();
        }

        #endregion

        #endregion

        #region Hooks

        private void Init()
        {
            ReadData();
        }

        private void Unload()
        {
            WriteData();
        }

        private void OnServerShutdown()
        {
            Unload();
        }

        private void OnServerInitialized()
        {
            foreach (KeyValuePair<String, List<Configuration.SaveItem>> SavedList in config.SavedsList)
                permission.RegisterPermission(SavedList.Key, this);

            permission.RegisterPermission(PermissionAdmin, this);

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            PushItem(player, true);
        }

        private void OnPlayerRecovered(BasePlayer player)
        {
            PushItem(player, true);
        }

//        private void OnNewSave(String filename)
//        {
//            SaveItems.Clear();
//            WriteData();
//            PrintWarning("Обнаружен вайп, очищаем все предметы игрокам");
//        }

        private void OnPlayerSleep(BasePlayer player)
        {
            if (player == null) return;
            SavedDeath(player);
        }

        private void OnServerSave()
        {
            WriteData();
        }

        private void OnPlayerSleepEnded(BasePlayer player)
        {
            PushItem(player, true);
        }

        private void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (player == null) return;
            SavedDeath(player);
        }

        private void OnPlayerCorpseSpawn(BasePlayer player, HitInfo info)
        {
            if (player == null) return;
            SavedDeath(player);
        }


        private void OnPlayerWound(BasePlayer player, HitInfo info)
        {
            if (player == null) return;
            SavedDeath(player);
        }


        private void OnPlayerConnected(BasePlayer player)
        {
            if (!SaveItems.ContainsKey(player.userID))
                SaveItems.Add(player.userID, new ItemSaved {Items = new List<ItemSaved.SavedItem> { }});

            PushItem(player);
        }

        private void OnPlayerDisconnected(BasePlayer player, String reason)
        {
            SavedDeath(player);
        }

        #endregion

        #region Lang

        public static StringBuilder sb = new StringBuilder();

        public String GetLang(String LangKey, String userID = null, params System.Object[] args)
        {
            sb.Clear();
            if (args != null)
            {
                sb.AppendFormat(lang.GetMessage(LangKey, this, userID), args);
                return sb.ToString();
            }

            return lang.GetMessage(LangKey, this, userID);
        }

        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<String, String>
            {
                ["ITEM_PUSH"] = "Some of your items were saved after death!",
            }, this);

            lang.RegisterMessages(new Dictionary<String, String>
            {
                ["ITEM_PUSH"] = "Некоторые ваши предметы были сохранены после смерти!",
            }, this, "ru");
        }

        #endregion
    }
}