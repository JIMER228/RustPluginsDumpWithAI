using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using UnityEngine;
using System;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Restore Upon Death", "k1lly0u", "0.5.0")]
    [Description("Restores player inventories on death and removes the items from their corpse")]
    class RestoreUponDeath : RustPlugin
    {
        #region Fields
        private RestoreData restoreData;
        private DynamicConfigFile restorationData;

        private bool wipeData = false;

        #endregion

        #region Oxide Hooks
        private void OnServerInitialized()
        {
            LoadData();

            if (Configuration.DropActiveItem)
                Unsubscribe(nameof(CanDropActiveItem));

            foreach (string perm in Configuration.Permissions.Keys)
                permission.RegisterPermission(!perm.StartsWith("restoreupondeath.") ? $"restoreupondeath.{perm}" : perm, this);
        }

        private void Unload()
        {
            OnServerSave();
        }

        private void OnNewSave(string fileName)
        {
            if (Configuration.WipeOnNewSave)
                wipeData = false;
        }

        private void OnServerSave() => SaveData();

        private object CanDropActiveItem(BasePlayer player) => ShouldBlockRestore(player) ? null : (object)(!HasAnyPermission(player.UserIDString));

        private void OnEntityDeath(BasePlayer player, HitInfo info)
        {
            if (player == null || !player.userID.IsSteamId() || restoreData.HasRestoreData(player.userID))
                return;

            if (ShouldBlockRestore(player))
                return;

            if (Configuration.NoSuicideRestore && info != null && info.damageTypes.GetMajorityDamageType() == Rust.DamageType.Suicide)
                return;

            ConfigData.LossAmounts lossAmounts;
            if (HasAnyPermission(player.UserIDString, out lossAmounts))
                restoreData.AddData(player, lossAmounts);
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            if (player == null || !restoreData.HasRestoreData(player.userID))
                return;

            if (ShouldBlockRestore(player))
                return;

            TryRestorePlayer(player);
        }
        #endregion

        #region Functions
        private bool ShouldBlockRestore(BasePlayer player) => Interface.Call("isEventPlayer", player) != null || Interface.Call("OnRestoreUponDeath", player) != null;

        private void TryRestorePlayer(BasePlayer player)
        {
            if (player == null || player.IsDead())
                return;

            if (!player.IsSleeping())
            {
                if (!Configuration.DefaultItems)
                {
                    StripContainer(player.inventory.containerWear);
                    StripContainer(player.inventory.containerBelt);
                }

                restoreData.RestorePlayer(player);
            }
            else player.Invoke(() => TryRestorePlayer(player), 0.25f);
        }

        private bool HasAnyPermission(string playerId)
        {
            foreach (string key in Configuration.Permissions.Keys)
            {
                if (permission.UserHasPermission(playerId, key))
                    return true;
            }
            return false;
        }

        private bool HasAnyPermission(string playerId, out ConfigData.LossAmounts lossAmounts)
        {
            foreach (KeyValuePair<string, ConfigData.LossAmounts> kvp in Configuration.Permissions)
            {
                if (permission.UserHasPermission(playerId, kvp.Key))
                {
                    lossAmounts = kvp.Value;
                    return true;
                }
            }
            lossAmounts = null;
            return false;
        }

        private void StripContainer(ItemContainer container)
        {
            for (int i = container.itemList.Count - 1; i >= 0; i--)
            {
                Item item = container.itemList[i];
                item.RemoveFromContainer();
                item.Remove();
            }
        }
        #endregion

        #region Config
        private ConfigData Configuration;
        private class ConfigData
        {
            [JsonProperty("Give default items upon respawn if the players is having items restored")]
            public bool DefaultItems { get; set; }

            [JsonProperty("Can drop active item on death")]
            public bool DropActiveItem { get; set; }

            [JsonProperty("Don't restore items if player commited suicide")]
            public bool NoSuicideRestore { get; set; }

            [JsonProperty("Wipe stored data when the map wipes")]
            public bool WipeOnNewSave { get; set; }

            [JsonProperty("Percentage of total items lost (Permission Name | Percentage (0 - 100))")]
            public Dictionary<string, LossAmounts> Permissions { get; set; }

            public class LossAmounts
            {
                public int Belt { get; set; }
                public int Wear { get; set; }
                public int Main { get; set; }
            }

            public VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            Configuration = Config.ReadObject<ConfigData>();

            if (Configuration.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(Configuration, true);
        }

        protected override void LoadDefaultConfig() => Configuration = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                DefaultItems = false,
                DropActiveItem = false,
                Permissions = new Dictionary<string, ConfigData.LossAmounts>
                {
                    ["restoreupondeath.default"] = new ConfigData.LossAmounts()
                    {
                        Belt = 0,
                        Main = 0,
                        Wear = 0
                    },
                    ["restoreupondeath.beltonly"] = new ConfigData.LossAmounts()
                    {
                        Belt = 0,
                        Main = 0,
                        Wear = 0
                    },
                    ["restoreupondeath.admin"] = new ConfigData.LossAmounts()
                    {
                        Belt = 0,
                        Main = 0,
                        Wear = 0
                    }
                },
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(Configuration, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (Configuration.Version < new VersionNumber(0, 3, 0))
                Configuration.Permissions = baseConfig.Permissions;

            Configuration.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion

        #region Data Management
        public enum ContainerType { Main, Wear, Belt }

        private void SaveData() => restorationData.WriteObject(restoreData);

        private void LoadData()
        {
            restorationData = Interface.Oxide.DataFileSystem.GetFile("restoreupondeath_data");

            restoreData = restorationData.ReadObject<RestoreData>();

            if (restoreData?.restoreData == null || wipeData)
                restoreData = new RestoreData();
        }

        private class RestoreData
        {
            public Hash<ulong, PlayerData> restoreData = new Hash<ulong, PlayerData>();

            public void AddData(BasePlayer player, ConfigData.LossAmounts lossAmounts)
            {
                restoreData[player.userID] = new PlayerData(player, lossAmounts);
            }

            public void RemoveData(ulong playerId)
            {
                if (HasRestoreData(playerId))
                    restoreData.Remove(playerId);
            }

            public bool HasRestoreData(ulong playerId) => restoreData.ContainsKey(playerId);

            public void RestorePlayer(BasePlayer player)
            {
                PlayerData playerData;
                if (restoreData.TryGetValue(player.userID, out playerData))
                {
                    if (playerData == null || !player.IsConnected)
                        return;

                    RestoreAllItems(player, playerData);
                }
            }

            private void RestoreAllItems(BasePlayer player, PlayerData playerData)
            {
                if (RestoreItems(player, playerData.containerBelt, ContainerType.Belt) && RestoreItems(player, playerData.containerWear, ContainerType.Wear) && RestoreItems(player, playerData.containerMain, ContainerType.Main))
                {
                    RemoveData(player.userID);
                    player.ChatMessage("死亡不掉落，你的物品未丢失！（注意:安全区下线有可能导致背包物品丢失无法找回！");
                }
            }

            internal static bool RestoreItems(BasePlayer player, ItemData[] itemData, ContainerType containerType)
            {
                ItemContainer container = containerType == ContainerType.Belt ? player.inventory.containerBelt : containerType == ContainerType.Wear ? player.inventory.containerWear : player.inventory.containerMain;

                for (int i = 0; i < itemData.Length; i++)
                {
                    Item item = CreateItem(itemData[i], player);
                    if (item == null)
                        continue;

                    if (!item.MoveToContainer(container, itemData[i].position)
                        && !item.MoveToContainer(container)
                        && !player.inventory.GiveItem(item))
                    {
                        // If allowing default items, restored items might not fit, so drop them as a last resort.
                        item.Drop(player.inventory.containerMain.dropPosition, player.inventory.containerMain.dropVelocity);
                    }
                }
                return true;
            }

            internal static Item CreateItem(ItemData itemData, BasePlayer player)
            {
                Item item = ItemManager.CreateByItemID(itemData.itemid, Mathf.Max(1, itemData.amount), itemData.skin);
                if (item == null)
                    return null;

                item.condition = itemData.condition;
                item.maxCondition = itemData.maxCondition;

                if (itemData.displayName != null)
                {
                    item.name = itemData.displayName;
                }

                if (itemData.text != null)
                {
                    item.text = itemData.text;
                }

                item.flags |= itemData.flags;

                if (itemData.frequency > 0)
                {
                    ItemModRFListener rfListener = item.info.GetComponentInChildren<ItemModRFListener>();
                    if (rfListener != null)
                    {
                        PagerEntity pagerEntity = BaseNetworkable.serverEntities.Find(item.instanceData.subEntity) as PagerEntity;
                        if (pagerEntity != null)
                        {
                            pagerEntity.ChangeFrequency(itemData.frequency);
                            item.MarkDirty();
                        }
                    }
                }

                if (itemData.instanceData?.IsValid() ?? false)
                    itemData.instanceData.Restore(item);

                FlameThrower flameThrower = item.GetHeldEntity() as FlameThrower;
                if (flameThrower != null)
                    flameThrower.ammo = itemData.ammo;

                if (itemData.contents != null && item.contents != null)
                {
                    foreach (ItemData contentData in itemData.contents)
                    {
                        Item childItem = CreateItem(contentData, player);
                        if (childItem == null)
                            continue;

                        if (!childItem.MoveToContainer(item.contents, contentData.position)
                            && !childItem.MoveToContainer(item.contents)
                            && !player.inventory.GiveItem(item))
                        {
                            item.Drop(player.inventory.containerMain.dropPosition, player.inventory.containerMain.dropVelocity);
                        }
                    }
                }

                // Process weapon attachments/capacity after child items have been added.
                BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
                if (weapon != null)
                {
                    weapon.DelayedModsChanged();

                    if (!string.IsNullOrEmpty(itemData.ammotype))
                        weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(itemData.ammotype);
                    weapon.primaryMagazine.contents = itemData.ammo;
                }

                return item;
            }

            public class PlayerData
            {
                public ItemData[] containerMain;
                public ItemData[] containerWear;
                public ItemData[] containerBelt;

                public PlayerData() { }

                public PlayerData(BasePlayer player, ConfigData.LossAmounts lossAmounts)
                {
                    containerBelt = GetItems(player.inventory.containerBelt, Mathf.Clamp(lossAmounts.Belt, 0, 100));
                    containerMain = GetItems(player.inventory.containerMain, Mathf.Clamp(lossAmounts.Main, 0, 100));
                    containerWear = GetItems(player.inventory.containerWear, Mathf.Clamp(lossAmounts.Wear, 0, 100));
                }

                internal static ItemData GetItem(Item item)
                {
                    return new ItemData
                    {
                        itemid = item.info.itemid,
                        amount = item.amount,
                        displayName = item.name,
                        ammo = item.GetHeldEntity() is BaseProjectile ? (item.GetHeldEntity() as BaseProjectile).primaryMagazine.contents : item.GetHeldEntity() is FlameThrower ? (item.GetHeldEntity() as FlameThrower).ammo : 0,
                        ammotype = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.ammoType.shortname ?? null,
                        position = item.position,
                        skin = item.skin,
                        condition = item.condition,
                        maxCondition = item.maxCondition,
                        frequency = ItemModAssociatedEntity<PagerEntity>.GetAssociatedEntity(item)?.GetFrequency() ?? -1,
                        instanceData = new ItemData.InstanceData(item),
                        text = item.text,
                        flags = item.flags,
                        // Note: Always restore 100% of child items since adding the remainder to the parent container could cause overflow.
                        contents = item.contents?.itemList.Select(childItem => GetItem(childItem)).ToArray(),
                    };
                }

                internal static ItemData[] GetItems(ItemContainer container, int lossPercentage)
                {
                    int keepPercentage = 100 - lossPercentage;

                    int itemCount = keepPercentage == 100 ? container.itemList.Count : Mathf.CeilToInt((float)container.itemList.Count * (float)(keepPercentage / 100f));
                    if (itemCount == 0)
                        return new ItemData[0];

                    ItemData[] itemData = new ItemData[itemCount];

                    for (int i = 0; i < itemCount; i++)
                    {
                        Item item = container.itemList.GetRandom();
                        itemData[i] = GetItem(item);
                        item.RemoveFromContainer();
                        item.Remove();
                    }

                    // Sort items by position to ensure they are restored in original order if desired slot is taken by pre-existing items.
                    Array.Sort(itemData, (a, b) => a.position.CompareTo(b.position));

                    return itemData;
                }
            }
        }

        public class ItemData
        {
            public int itemid;
            public ulong skin;
            public string displayName;
            public int amount;
            public float condition;
            public float maxCondition;
            public int ammo;
            public string ammotype;
            public int position;
            public int frequency;
            public InstanceData instanceData;
            public string text;
            public Item.Flag flags;
            public ItemData[] contents;

            public class InstanceData
            {
                public int dataInt;
                public int blueprintTarget;
                public int blueprintAmount;

                public InstanceData() { }
                public InstanceData(Item item)
                {
                    if (item.instanceData == null)
                        return;

                    dataInt = item.instanceData.dataInt;
                    blueprintAmount = item.instanceData.blueprintAmount;
                    blueprintTarget = item.instanceData.blueprintTarget;
                }

                public void Restore(Item item)
                {
                    if (item.instanceData == null)
                        item.instanceData = new ProtoBuf.Item.InstanceData();

                    item.instanceData.ShouldPool = false;

                    item.instanceData.blueprintAmount = blueprintAmount;
                    item.instanceData.blueprintTarget = blueprintTarget;
                    item.instanceData.dataInt = dataInt;

                    item.MarkDirty();
                }

                public bool IsValid()
                {
                    return dataInt != 0 || blueprintAmount != 0 || blueprintTarget != 0;
                }
            }
        }
        #endregion
    }
}