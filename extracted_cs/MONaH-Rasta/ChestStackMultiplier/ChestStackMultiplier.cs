using System.Collections.Generic;

using Newtonsoft.Json;
using UnityEngine;

using Pool = Facepunch.Pool;

namespace Oxide.Plugins
{
    [Info("Chest Stack Multiplier", "MON@H", "1.6.2")]
    [Description("Higher stack sizes in storage containers.")]

    public class ChestStackMultiplier : RustPlugin
    {
        #region Variables

        private const string PermissionUseShift = "cheststackmultiplier.useshift";
        private const int MaxDropLoops = 20;
        private const float DefaultMultiplierValue = 1f;

        private static readonly object True = true;

        private readonly Dictionary<ulong, float> _cacheMultipliers = new Dictionary<ulong, float>();
        private readonly HashSet<ulong> _cacheBackpackContainers = new HashSet<ulong>();
        private readonly HashSet<ulong> _cacheBackpackEntities = new HashSet<ulong>();

        // Performance: Batched config writes
        private Timer _configWriteTimer;

        // Performance: Player permission cache
        private readonly Dictionary<string, bool> _shiftPermissionCache = new Dictionary<string, bool>();

        private uint _backpackPrefabID;
        private uint _playerPrefabID;

        #endregion Variables

        #region Initialization

        private void Init()
        {
            HooksUnsubscribe();
        }

        private void OnServerInitialized()
        {
            RegisterPermissions();
            CachePrefabIDs();
            CacheMultipliers();
            HooksSubscribe();
        }

        private void Unload()
        {
            // Force write pending config changes on unload only if dirty
            if (_pluginConfig.IsDirty)
            {
                Config.WriteObject(_pluginConfig);
            }

            if (_configWriteTimer is not null)
            {
                _configWriteTimer.Destroy();
            }
        }

        #endregion Initialization

        #region Configuration

        private PluginConfig _pluginConfig;

        public class PluginConfig
        {
            [JsonProperty(PropertyName = "Default Multiplier for new containers")]
            public float DefaultMultiplier { get; set; } = DefaultMultiplierValue;

            [JsonProperty(PropertyName = "Containers list (PrefabName: multiplier)")]
            public SortedDictionary<string, float> ContainerMultipliers { get; set; }

            [JsonProperty(PropertyName = "Enable debug logging")]
            public bool DebugMode { get; set; }

            [JsonIgnore]
            public bool IsDirty { get; set; }
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Loading Default Config");
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            Config.Settings.DefaultValueHandling = DefaultValueHandling.Populate;
            _pluginConfig = AdditionalConfig(Config.ReadObject<PluginConfig>());
            Config.WriteObject(_pluginConfig);
        }

        public PluginConfig AdditionalConfig(PluginConfig config)
        {
            // Validate default multiplier
            if (config.DefaultMultiplier <= 0)
            {
                PrintWarning("LoadConfig: Default Multiplier can't be less than or equal to 0, resetting to 1");
                config.DefaultMultiplier = DefaultMultiplierValue;
            }

            // Initialize container multipliers if null
            if (config.ContainerMultipliers is null)
            {
                config.ContainerMultipliers = new SortedDictionary<string, float>();
            }

            // Add missing container multipliers
            PopulateDefaultContainerMultipliers(config);

            // Validate and fix invalid multipliers
            ValidateContainerMultipliers(config);

            return config;
        }

        public void PopulateDefaultContainerMultipliers(PluginConfig config)
        {
            foreach (ItemDefinition def in ItemManager.GetItemDefinitions())
            {
                ItemModDeployable deployable = def.GetComponent<ItemModDeployable>();
                if (deployable is null || deployable.entityPrefab is null)
                {
                    continue;
                }

                BaseEntity prefab = deployable.entityPrefab.GetEntity();
                if (prefab is null)
                {
                    continue;
                }

                BoxStorage entity = prefab.GetComponent<BoxStorage>();
                if (entity is null || config.ContainerMultipliers.ContainsKey(entity.PrefabName))
                {
                    continue;
                }

                config.ContainerMultipliers[entity.PrefabName] = config.DefaultMultiplier;
            }
        }

        public void ValidateContainerMultipliers(PluginConfig config)
        {
            List<string> invalidKeys = Pool.Get<List<string>>();

            foreach (KeyValuePair<string, float> kvp in config.ContainerMultipliers)
            {
                if (kvp.Value <= 0)
                {
                    PrintWarning("LoadConfig: " + kvp.Key + " Multiplier can't be less than or equal to 0, resetting to default");
                    invalidKeys.Add(kvp.Key);
                }
            }

            for (int i = 0; i < invalidKeys.Count; i++)
            {
                config.ContainerMultipliers[invalidKeys[i]] = config.DefaultMultiplier;
            }

            Pool.FreeUnmanaged(ref invalidKeys);
        }

        public void ScheduleConfigWrite()
        {
            _pluginConfig.IsDirty = true;

            // Only create timer if not already running
            if (_configWriteTimer is null || _configWriteTimer.Destroyed)
            {
                _configWriteTimer = timer.Once(5f, () =>
                {
                    if (_pluginConfig.IsDirty)
                    {
                        Config.WriteObject(_pluginConfig);
                        _pluginConfig.IsDirty = false;
                    }
                });
            }
        }

        #endregion Configuration

        #region Oxide Hooks

        private object OnMaxStackable(Item item)
        {
            // Early returns for non-stackable items
            if (item.info.stackable == 1 || item.info.itemType == ItemContainer.ContentsType.Liquid)
            {
                return null;
            }

            BaseEntity entity = item.GetEntityOwner();
            if (entity is null)
            {
                entity = item.GetOwnerPlayer();
            }

            if (!entity.IsValid())
            {
                return null;
            }

            float stackMultiplier = GetStackMultiplierForEntity(item, entity);

            if (stackMultiplier == DefaultMultiplierValue)
            {
                return null;
            }

            return Mathf.FloorToInt(stackMultiplier * item.info.stackable);
        }

        private object CanMoveItem(Item movedItem, PlayerInventory playerInventory, ItemContainerId targetContainerID, int targetSlot, int amount)
        {
            // Early validation
            (bool isValid, BasePlayer player) = ValidateMoveItemInput(movedItem, playerInventory);
            if (!isValid)
            {
                return null;
            }

            BaseEntity sourceEntity = movedItem.GetEntityOwner();
            if (sourceEntity is null)
            {
                sourceEntity = movedItem.GetOwnerPlayer();
            }

            if (IsExcluded(sourceEntity, player))
            {
                return null;
            }

            // Handle target container ID resolution
            if (!ResolveTargetContainer(ref targetContainerID, sourceEntity, player, playerInventory))
            {
                return null;
            }

            ItemContainer targetContainer = playerInventory.FindContainer(targetContainerID);
            if (targetContainer is null)
            {
                return null;
            }

            BaseEntity targetEntity = targetContainer.GetEntityOwner();
            if (targetEntity is null)
            {
                targetEntity = targetContainer.GetOwnerPlayer();
            }

            if (sourceEntity == targetEntity || IsExcluded(targetEntity, player))
            {
                return null;
            }

            ItemContainer lootContainer = null;
            if (playerInventory.loot is not null)
            {
                lootContainer = playerInventory.loot.FindContainer(targetContainerID);
            }

            // Handle different move scenarios
            return HandleMoveScenarios(movedItem, playerInventory, targetContainer, lootContainer, targetSlot, amount, player);
        }

        private void OnItemDropped(Item item, BaseEntity entity)
        {
            if (!ValidateDropInput(item, entity))
            {
                return;
            }

            item.RemoveFromContainer();
            int stackSize = item.MaxStackable();

            if (item.amount <= stackSize)
            {
                return;
            }

            ProcessOverstackDrop(item, entity, stackSize);
        }

        private void OnBackpackOpened(BasePlayer player, ulong backpackEntityId, ItemContainer backpackContainer)
        {
            try
            {
                if (backpackContainer is not null && !_cacheBackpackContainers.Contains(backpackContainer.uid.Value))
                {
                    CacheAddBackpack(backpackContainer);
                }
            }
            catch (System.Exception ex)
            {
                PrintError("OnBackpackOpened threw exception: " + ex);
                throw;
            }
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            _shiftPermissionCache.Remove(player.UserIDString);
        }

        #endregion Oxide Hooks

        #region Core Methods

        public float GetStackMultiplierForEntity(Item item, BaseEntity entity)
        {
            // Special case for player backpack containers
            if (entity.prefabID == _playerPrefabID && !item.parent.HasFlag(ItemContainer.Flag.IsPlayer))
            {
                if (_cacheMultipliers.TryGetValue(_backpackPrefabID, out float backpackMultiplier))
                {
                    return backpackMultiplier;
                }
                return DefaultMultiplierValue;
            }

            return GetStackMultiplier(entity);
        }

        public (bool isValid, BasePlayer player) ValidateMoveItemInput(Item movedItem, PlayerInventory playerInventory)
        {
            if (movedItem is null || playerInventory is null)
            {
                return (false, null);
            }
            return (playerInventory.baseEntity.IsValid(), playerInventory.baseEntity);
        }

        public bool ResolveTargetContainer(ref ItemContainerId targetContainerID, BaseEntity sourceEntity, BasePlayer player, PlayerInventory playerInventory)
        {
            if (targetContainerID.Value != 0)
            {
                return true;
            }

            // Moving from player inventory
            if (sourceEntity == player)
            {
                if (playerInventory.loot.containers.Count > 0)
                {
                    targetContainerID.Value = playerInventory.loot.containers[0].uid.Value;
                }
                else
                {
                    return false;
                }
            }
            // Moving from container to player
            else if (sourceEntity == playerInventory.loot.entitySource)
            {
                targetContainerID = playerInventory.containerMain.uid;
            }

            return true;
        }

        public object HandleMoveScenarios(Item movedItem, PlayerInventory playerInventory, ItemContainer targetContainer, ItemContainer lootContainer, int targetSlot, int amount, BasePlayer player)
        {
            // Right-click overstack handling
            if (targetSlot == -1)
            {
                return HandleRightClickMove(movedItem, playerInventory, targetContainer, lootContainer, player);
            }

            // Moving overstacks in chest
            if (amount > movedItem.info.stackable && lootContainer is not null)
            {
                return HandleOverstackMove(movedItem, targetContainer, targetSlot, amount, playerInventory);
            }

            // Prevent moving overstacks to inventory
            return HandleOverstackPreventMove(movedItem, targetContainer, targetSlot, lootContainer, player);
        }

        public object HandleRightClickMove(Item movedItem, PlayerInventory playerInventory, ItemContainer targetContainer, ItemContainer lootContainer, BasePlayer player)
        {
            // Right-click overstack into player inventory
            if (lootContainer is null)
            {
                return ProcessOverstackToInventory(movedItem, targetContainer, playerInventory, player);
            }

            // Shift right-click into storage container
            return ProcessShiftMoveToContainer(movedItem, lootContainer, playerInventory, player);
        }

        public object ProcessOverstackToInventory(Item movedItem, ItemContainer targetContainer, PlayerInventory playerInventory, BasePlayer player)
        {
            if (movedItem.amount <= movedItem.info.stackable)
            {
                return null;
            }

            bool isUsingShift = IsUsingShift(player);
            int loops = isUsingShift ?
                       Mathf.CeilToInt((float)movedItem.amount / movedItem.info.stackable) : 1;

            // Performance: Batch operations, single ServerUpdate at end
            bool anyMoved = false;

            for (int i = 0; i < loops; i++)
            {
                if (movedItem.amount <= movedItem.info.stackable)
                {
                    MoveItemToTarget(movedItem, targetContainer, playerInventory);
                    anyMoved = true;
                    break;
                }

                if (!ProcessItemSplit(movedItem, targetContainer, playerInventory))
                {
                    break;
                }
                anyMoved = true;
            }

            if (anyMoved)
            {
                movedItem.MarkDirty();
                playerInventory.ServerUpdate(0f);
            }

            return True;
        }

        public void MoveItemToTarget(Item item, ItemContainer targetContainer, PlayerInventory playerInventory)
        {
            if (targetContainer is not null)
            {
                item.MoveToContainer(targetContainer);
            }
            else
            {
                playerInventory.GiveItem(item);
            }
        }

        public bool ProcessItemSplit(Item movedItem, ItemContainer targetContainer, PlayerInventory playerInventory)
        {
            Item itemToMove = movedItem.SplitItem(movedItem.info.stackable);
            if (itemToMove is null)
            {
                return false;
            }

            bool moved;
            if (targetContainer is not null)
            {
                moved = itemToMove.MoveToContainer(targetContainer);
            }
            else
            {
                moved = playerInventory.GiveItem(itemToMove);
            }

            if (!moved)
            {
                movedItem.amount += itemToMove.amount;
                itemToMove.Remove();

                if (_pluginConfig.DebugMode)
                {
                    PrintWarning("Failed to move split item " + itemToMove.info.shortname + " (amount: " + itemToMove.amount + ")");
                }

                return false;
            }

            return true;
        }

        public object ProcessShiftMoveToContainer(Item movedItem, ItemContainer lootContainer, PlayerInventory playerInventory, BasePlayer player)
        {
            if (!IsUsingShift(player))
            {
                return null;
            }

            Dictionary<int, List<Item>> itemsByType = Pool.Get<Dictionary<int, List<Item>>>();

            try
            {
                // Collect all items of the same type efficiently
                CollectItemsByType(playerInventory.containerMain, itemsByType);
                CollectItemsByType(playerInventory.containerBelt, itemsByType);

                List<Item> itemsToMove;
                if (!itemsByType.TryGetValue(movedItem.info.itemid, out itemsToMove))
                {
                    return null;
                }

                // Remove the original item from the list
                itemsToMove.Remove(movedItem);

                // Performance: Use for loop for better performance
                int movedCount = 0;
                for (int i = 0; i < itemsToMove.Count; i++)
                {
                    if (!itemsToMove[i].MoveToContainer(lootContainer))
                    {
                        break;
                    }
                    movedCount++;
                }

                if (movedCount > 0)
                {
                    playerInventory.ServerUpdate(0f);
                }

                return null;
            }
            finally
            {
                // Clean up pooled objects properly
                foreach (KeyValuePair<int, List<Item>> kvp in itemsByType)
                {
                    List<Item> itemList = kvp.Value;
                    Pool.FreeUnmanaged(ref itemList);
                }
                Pool.FreeUnmanaged(ref itemsByType);
            }
        }

        public void CollectItemsByType(ItemContainer container, Dictionary<int, List<Item>> itemsByType)
        {
            for (int i = 0; i < container.itemList.Count; i++)
            {
                Item item = container.itemList[i];
                int itemId = item.info.itemid;

                List<Item> items;
                if (!itemsByType.TryGetValue(itemId, out items))
                {
                    items = Pool.Get<List<Item>>();
                    itemsByType[itemId] = items;
                }

                items.Add(item);
            }
        }

        public object HandleOverstackMove(Item movedItem, ItemContainer targetContainer, int targetSlot, int amount, PlayerInventory playerInventory)
        {
            Item targetItem = targetContainer.GetSlot(targetSlot);
            if (targetItem is null)
            {
                ProcessMoveToEmptySlot(movedItem, amount, targetContainer, targetSlot);
            }
            else
            {
                ProcessMoveToOccupiedSlot(movedItem, targetItem, amount, targetContainer, targetSlot, playerInventory);
            }

            playerInventory.ServerUpdate(0f);
            return True;
        }

        public void ProcessMoveToEmptySlot(Item movedItem, int amount, ItemContainer targetContainer, int targetSlot)
        {
            if (amount < movedItem.amount)
            {
                if (!ItemHelper.SplitMoveItem(movedItem, amount, targetContainer, targetSlot) && _pluginConfig.DebugMode)
                {
                    PrintWarning("Failed to split move item " + movedItem.info.shortname + " to empty slot");
                }
            }
            else
            {
                movedItem.MoveToContainer(targetContainer, targetSlot);
            }
        }

        public void ProcessMoveToOccupiedSlot(Item movedItem, Item targetItem, int amount, ItemContainer targetContainer, int targetSlot, PlayerInventory playerInventory)
        {
            if (!targetItem.CanStack(movedItem) && amount == movedItem.amount)
            {
                ItemHelper.SwapItems(movedItem, targetItem);
            }
            else
            {
                if (amount < movedItem.amount)
                {
                    if (!ItemHelper.SplitMoveItem(movedItem, amount, playerInventory) && _pluginConfig.DebugMode)
                    {
                        PrintWarning("Failed to split move item " + movedItem.info.shortname + " to occupied slot");
                    }
                }
                else
                {
                    movedItem.MoveToContainer(targetContainer, targetSlot);
                }
            }
        }

        public object HandleOverstackPreventMove(Item movedItem, ItemContainer targetContainer, int targetSlot, ItemContainer lootContainer, BasePlayer player)
        {
            if (lootContainer is null)
            {
                return null;
            }

            Item targetItem = targetContainer.GetSlot(targetSlot);
            if (targetItem is null || !targetItem.IsValid())
            {
                return null;
            }

            if (targetItem.amount > targetItem.info.stackable &&
                movedItem.GetOwnerPlayer() == player &&
                !movedItem.CanStack(targetItem))
            {
                return True;
            }

            return null;
        }

        public bool ValidateDropInput(Item item, BaseEntity entity)
        {
            return item is not null && entity.IsValid();
        }

        public void ProcessOverstackDrop(Item item, BaseEntity entity, int stackSize)
        {
            int loops = Mathf.FloorToInt((float)item.amount / stackSize);
            if (loops > MaxDropLoops)
            {
                return;
            }

            Vector3 dropPosition = entity.transform.position;
            Vector3 dropVelocity = entity.GetDropVelocity() + Vector3Ex.Range(-1f, 1f);

            for (int i = 0; i < loops; i++)
            {
                if (item.amount <= stackSize)
                {
                    break;
                }

                Item splitItem = item.SplitItem(stackSize);
                if (splitItem is not null)
                {
                    splitItem.Drop(dropPosition, dropVelocity);
                }
            }
        }

        public void CachePrefabIDs()
        {
            _playerPrefabID = StringPool.Get("assets/prefabs/player/player.prefab");

            // Ensure backpack multiplier is in config
            if (!_pluginConfig.ContainerMultipliers.ContainsKey("Backpack"))
            {
                _pluginConfig.ContainerMultipliers["Backpack"] = _pluginConfig.DefaultMultiplier;
                ScheduleConfigWrite();
            }

            // Find unused ID for backpack
            _backpackPrefabID = StringPool.closest;
            while (StringPool.toString.ContainsKey(_backpackPrefabID))
            {
                _backpackPrefabID++;
            }
        }

        public void CacheMultipliers()
        {
            List<string> invalidPrefabs = Pool.Get<List<string>>();

            foreach (KeyValuePair<string, float> container in _pluginConfig.ContainerMultipliers)
            {
                if (container.Key.Equals("Backpack"))
                {
                    _cacheMultipliers[_backpackPrefabID] = _pluginConfig.ContainerMultipliers["Backpack"];
                    continue;
                }

                uint id;
                if (StringPool.toNumber.TryGetValue(container.Key, out id))
                {
                    _cacheMultipliers[id] = container.Value;
                    continue;
                }

                // Log invalid prefabs with their values for history before removal
                if (container.Value != _pluginConfig.DefaultMultiplier)
                {
                    PrintWarning("Config contains invalid prefab: " + container.Key + " (value: " + container.Value + ") - removing from config");
                }
                else
                {
                    PrintWarning("Config contains invalid prefab: " + container.Key + " - removing from config");
                }

                invalidPrefabs.Add(container.Key);
            }

            // Remove invalid prefabs from config
            for (int i = 0; i < invalidPrefabs.Count; i++)
            {
                _pluginConfig.ContainerMultipliers.Remove(invalidPrefabs[i]);
            }

            // Schedule config write if we removed anything
            if (invalidPrefabs.Count > 0)
            {
                ScheduleConfigWrite();
            }

            Pool.FreeUnmanaged(ref invalidPrefabs);
        }

        public void CacheAddBackpack(ItemContainer itemContainer)
        {
            BaseEntity baseEntity = itemContainer.GetEntityOwner();

            if (baseEntity.IsValid() && !_cacheBackpackEntities.Contains(baseEntity.net.ID.Value))
            {
                _cacheBackpackContainers.Add(itemContainer.uid.Value);
                _cacheBackpackEntities.Add(baseEntity.net.ID.Value);
            }
            else if (_pluginConfig.DebugMode)
            {
                bool entityValid = baseEntity is not null && baseEntity.IsValid();
                ulong entityId = entityValid ? baseEntity.net.ID.Value : 0;
                bool alreadyCached = _cacheBackpackEntities.Contains(entityId);

                PrintWarning("Failed to cache backpack: entity valid=" + entityValid + ", already cached=" + alreadyCached);
            }
        }

        public bool IsExcluded(BaseEntity entity, BasePlayer player)
        {
            if (!entity.IsValid() || entity.HasFlag(BaseEntity.Flags.Locked))
            {
                return true;
            }

            // Traditional switch for better performance in Unity/Mono
            switch (entity)
            {
                case BigWheelBettingTerminal _:
                    return true;
                case ShopFront _:
                    return true;
                case VendingMachine vendingMachine:
                    return !vendingMachine.PlayerBehind(player);
                default:
                    return false;
            }
        }

        public bool IsUsingShift(BasePlayer player)
        {
            string userId = player.UserIDString;

            bool hasPermission;
            if (!_shiftPermissionCache.TryGetValue(userId, out hasPermission))
            {
                hasPermission = permission.UserHasPermission(userId, PermissionUseShift);
                _shiftPermissionCache[userId] = hasPermission;
            }

            return hasPermission && player.serverInput.IsDown(BUTTON.SPRINT);
        }

        public float GetStackMultiplier(BaseEntity entity)
        {
            // Fast path for excluded entity types
            switch (entity)
            {
                case LootContainer _:
                case BaseCorpse _:
                case BasePlayer _:
                    return DefaultMultiplierValue;
            }

            // Check backpack entities cache
            if (_cacheBackpackEntities.Contains(entity.net.ID.Value))
            {
                if (_cacheMultipliers.TryGetValue(_backpackPrefabID, out float backpackMultiplier))
                {
                    return backpackMultiplier;
                }
                return DefaultMultiplierValue;
            }

            // Check main cache
            if (_cacheMultipliers.TryGetValue(entity.prefabID, out float multiplier))
            {
                return multiplier;
            }

            return LoadAndCacheMultiplier(entity);
        }

        public float LoadAndCacheMultiplier(BaseEntity entity)
        {
            if (!_pluginConfig.ContainerMultipliers.TryGetValue(entity.PrefabName, out float multiplier))
            {
                multiplier = _pluginConfig.DefaultMultiplier;
                _pluginConfig.ContainerMultipliers[entity.PrefabName] = multiplier;
                ScheduleConfigWrite();
            }

            _cacheMultipliers[entity.prefabID] = multiplier;
            return multiplier;
        }

        #endregion Core Methods

        #region Helper Classes

        private static class ItemHelper
        {
            public static bool SplitMoveItem(Item item, int amount, ItemContainer targetContainer, int targetSlot)
            {
                Item splitItem = item.SplitItem(amount);
                if (splitItem is null)
                {
                    return false;
                }

                if (!splitItem.MoveToContainer(targetContainer, targetSlot))
                {
                    item.amount += splitItem.amount;
                    splitItem.Remove();
                    return false;
                }

                return true;
            }

            public static bool SplitMoveItem(Item item, int amount, PlayerInventory inventory)
            {
                Item splitItem = item.SplitItem(amount);
                if (splitItem is null)
                {
                    return false;
                }

                if (!inventory.GiveItem(splitItem))
                {
                    item.amount += splitItem.amount;
                    splitItem.Remove();
                    return false;
                }

                return true;
            }

            public static void SwapItems(Item item1, Item item2)
            {
                ItemContainer container1 = item1.parent;
                ItemContainer container2 = item2.parent;
                int slot1 = item1.position;
                int slot2 = item2.position;

                item1.RemoveFromContainer();
                item2.RemoveFromContainer();
                item1.MoveToContainer(container2, slot2);
                item2.MoveToContainer(container1, slot1);
            }
        }

        #endregion Helper Classes

        #region Helpers

        public void HooksUnsubscribe()
        {
            Unsubscribe(nameof(CanMoveItem));
            Unsubscribe(nameof(OnBackpackOpened));
            Unsubscribe(nameof(OnItemDropped));
            Unsubscribe(nameof(OnMaxStackable));
            Unsubscribe(nameof(OnPlayerDisconnected));
        }

        public void HooksSubscribe()
        {
            Subscribe(nameof(CanMoveItem));
            Subscribe(nameof(OnBackpackOpened));
            Subscribe(nameof(OnItemDropped));
            Subscribe(nameof(OnMaxStackable));
            Subscribe(nameof(OnPlayerDisconnected));
        }

        public void RegisterPermissions()
        {
            permission.RegisterPermission(PermissionUseShift, this);
        }

        #endregion Helpers
    }
}