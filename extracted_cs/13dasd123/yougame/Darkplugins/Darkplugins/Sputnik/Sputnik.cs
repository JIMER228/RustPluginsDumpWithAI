using System.Collections.Generic;
using Newtonsoft.Json;
using CompanionServer.Handlers;
using Oxide.Plugins.SputnikExtensionMethods;
using UnityEngine;
using System;
using Oxide.Core.Plugins;
using Network;
using Rust;
using System.Collections;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using System.Reflection;

namespace Oxide.Plugins
{
    [Info("Sputnik", "Sempai#3239", "1.0.8")]
    class Sputnik : RustPlugin
    {
        #region Variables
        [PluginReference] Plugin NpcSpawn, PveMode, GUIAnnouncements, DiscordMessages, ZoneManager, RaidableBases;
        const bool en = false;
        static Sputnik ins;
        EventClass eventClass;
        static Dictionary<string, List<EntData>> saveData;
        Coroutine findSpawnPointsCorountine;
        Coroutine autoEventCorountine;
        HashSet<string> subscribeMethods = new HashSet<string>
        {
            "OnCardSwipe",
            "CanHackCrate",
            "OnLootEntity",
            "CanLootEntity",
            "OnEntityDeath",
            "OnEntityKill",
            "OnCorpsePopulate",
            "CanPopulateLoot",
            "OnCustomLootContainer",
            "CanEntityTakeDamage",
            "OnTrapTrigger",
            "OnTurretTarget",
            "CanEntityBeTargeted",
            "OnEntityTakeDamage",
            "OnPlayerSleep"
        };
        #endregion Variables

        #region Hooks
        void Init() => Unsubscribes();

        void OnServerInitialized()
        {
			PrintWarning("\n-----------------------------\n" +
            " Author - Sempai#3239\n" +
            " VK - https://vk.com/rustnastroika/n" +
            " Forum - https://darkplugins.ru/n" +
            " Discord - https://discord.gg/5DPTsRmd3G/n" +
            "-----------------------------");
            ins = this;
            PostLoadCheck();
            UpdateConfig();
            LoadDefaultMessages();
            LoadData();
            FindNewEventPoints();
            if (_config.mainConfig.isAutoEvent) autoEventCorountine = ServerMgr.Instance.StartCoroutine(AutoEventCorountine());
        }

        void Unload()
        {
            StopEvent();
            if (findSpawnPointsCorountine != null) ServerMgr.Instance.StopCoroutine(findSpawnPointsCorountine);
            if (autoEventCorountine != null) ServerMgr.Instance.StopCoroutine(autoEventCorountine);
        }

        object OnCardSwipe(CardReader cardReader, Keycard keycard, BasePlayer player)
        {
            if (eventClass == null || player == null || cardReader == null || keycard == null) return null;
            SputnikClass sputnikClass = eventClass.GetSputnikClassFromCardReaderUid(cardReader.net.ID);
            Item keyCardItem = keycard.GetItem();
            if (keyCardItem == null) return null;

            if (keyCardItem.info.shortname == ins._config.customCardConfig.shortName && keyCardItem.skin == ins._config.customCardConfig.skinID)
            {
                if (sputnikClass != null && !sputnikClass.cardReaderOpen)
                {
                    keyCardItem.LoseCondition(_config.customCardConfig.helthLossScale);
                    sputnikClass.cardReaderOpen = true;
                    return true;
                }
                else if (keyCardItem.skin != 0) return true;
            }
            else if (sputnikClass != null) InformPlayer(player, "NeedUseCard", _config.prefix);

            return null;
        }

        object CanHackCrate(BasePlayer player, HackableLockedCrate crate)
        {
            if (eventClass == null || player == null || crate == null) return null;

            SputnikClass sputnikClass = eventClass.GetSputnikClassFromCrateUid(crate.net.ID);
            if (sputnikClass != null)
            {
                if (!sputnikClass.CanOpenCrate(crate.net.ID))
                {
                    InformPlayer(player, "NeedUseCard", _config.prefix);
                    return true;
                }
                else ActionEconomy(player.userID, "LockedCrate");
            }
            return null;
        }

        void OnLootEntity(BasePlayer player, LootContainer container)
        {
            if (eventClass == null || player == null || container == null) return;
            SputnikClass sputnikClass = eventClass.GetSputnikClassFromCrateUid(container.net.ID);
            if (sputnikClass != null)
            {
                sputnikClass.OnCratelooted(container.net.ID);
                ActionEconomy(player.userID, "Crates", container.PrefabName);
            }
        }

        object CanLootEntity(BasePlayer player, LootContainer container)
        {
            if (eventClass == null || player == null || container == null) return null;
            SputnikClass sputnikClass = eventClass.GetSputnikClassFromCrateUid(container.net.ID);
            if (sputnikClass != null)
            {
                if (!sputnikClass.CanOpenCrate(container.net.ID))
                {
                    InformPlayer(player, "NeedUseCard", _config.prefix);
                    return true;
                }
            }
            return null;
        }

        void OnLootSpawn(LootContainer container)
        {
            if (!_config.customCardConfig.enableSpawnInDefaultCrates || container == null || container.inventory == null) return;
            float chance = 0;
            if (!_config.customCardConfig.spawnSetting.TryGetValue(container.PrefabName, out chance) || chance == 0) return;

            if (UnityEngine.Random.Range(0f, 100f) <= chance)
            {
                Item item = CreateCustomCardItem();
                container.inventory.Remove(container.inventory.itemList.FirstOrDefault(x => true));
                if (!item.MoveToContainer(container.inventory)) item.Remove();
            }
        }

        object OnEntityTakeDamage(ScientistNPC scientistNPC, HitInfo info)
        {
            if (eventClass == null || scientistNPC == null || info == null || info.Initiator == null) return null;
            NpcConfig npcConfig = _config.npcConfigs.FirstOrDefault(x => x.name == scientistNPC.displayName);
            if (npcConfig == null) return null;
            AutoTurret autoTurret = info.Initiator as AutoTurret;
            if (autoTurret == null) return null;
            if (eventClass.IsEventTurret(autoTurret.net.ID)) return true;
            return null;
        }

        object OnEntityTakeDamage(AutoTurret autoTurret, HitInfo info)
        {
            if (eventClass == null || autoTurret == null) return null;
            if (eventClass.IsEventTurret(autoTurret.net.ID) && (info.InitiatorPlayer == null || !info.InitiatorPlayer.IsRealPlayer())) return true;
            return null;
        }

        void OnEntityDeath(ScientistNPC scientistNPC, HitInfo info)
        {
            if (eventClass == null || scientistNPC == null || info == null) return;
            if (info.InitiatorPlayer.IsRealPlayer() && _config.npcConfigs.Any(x => x != null && x.name == scientistNPC.displayName)) ActionEconomy(info.InitiatorPlayer.userID, "Npc");
        }

        void OnEntityDeath(BasePlayer player, HitInfo info)
        {
            OnPlayerSleep(player);
        }

        void OnPlayerSleep(BasePlayer player)
        {
            if (eventClass == null || !player.IsRealPlayer()) return;
            SputnikClass sputnikClass = eventClass.GetSputnikClassFromPLayerInZoneUid(player.userID);
            if (sputnikClass != null)
            {
                sputnikClass.RemovePlayerFromZone(player);
            }
        }

        void OnCorpsePopulate(BasePlayer entity, NPCPlayerCorpse corpse)
        {
            if (eventClass != null || entity == null || corpse == null) return;
            if (entity is ScientistNPC)
            {
                NpcConfig npcConfig = _config.npcConfigs.FirstOrDefault(x => x.name == entity.displayName);
                if (npcConfig == null) return;
                NextTick(() =>
                {
                    if (corpse == null) return;
                    ItemContainer container = corpse.containers[0];

                    if (npcConfig.typeLootTable == 0)
                    {
                        for (int i = container.itemList.Count - 1; i >= 0; i--)
                        {
                            Item item = container.itemList[i];
                            if (npcConfig.wearItems.Any(x => x.shortName == item.info.shortname))
                            {
                                item.RemoveFromContainer();
                                item.Remove();
                            }
                        }
                        if (npcConfig.deleteCorpse && corpse != null && !corpse.IsDestroyed) corpse.Kill();
                        return;
                    }

                    if (npcConfig.typeLootTable == 2 || npcConfig.typeLootTable == 3)
                    {
                        if (npcConfig.deleteCorpse && !corpse.IsDestroyed) corpse.Kill();
                        return;
                    }

                    container.ClearItemsContainer();

                    if (npcConfig.typeLootTable == 1 && npcConfig.lootTable.minAmount > 0) AddToContainerItem(container, npcConfig.lootTable, npcConfig.typeLootTable);

                    if (npcConfig.deleteCorpse && corpse != null && !corpse.IsDestroyed) corpse.Kill();
                });
            }
        }

        object OnTrapTrigger(BaseTrap baseTrap, GameObject gameObject)
        {
            if (baseTrap == null) return null;
            ScientistNPC scientistNPC = gameObject.ToBaseEntity() as ScientistNPC;
            if (scientistNPC != null && _config.npcConfigs.Any(x => x.name == scientistNPC.displayName)) return true;
            return null;
        }

        object OnTurretTarget(AutoTurret turret, BasePlayer player)
        {
            if (eventClass == null || !turret.IsExists() || !player.IsExists()) return null;
            if (!eventClass.IsEventTurret(turret.net.ID)) return null;
            if (!player.IsRealPlayer()) return true;
            return null;
        }

        #region OtherPlugins
        object CanPopulateLoot(ScientistNPC entity, NPCPlayerCorpse corpse)
        {
            if (eventClass == null || entity == null || corpse == null) return null;
            NpcConfig npcConfig = _config.npcConfigs.FirstOrDefault(x => x.name == entity.displayName);
            if (npcConfig != null && npcConfig.typeLootTable != 2) return true;
            return null;
        }

        object CanPopulateLoot(LootContainer container)
        {
            if (eventClass == null || container == null) return null;
            string cratePresetName = null;
            SputnikClass sputnikClass = eventClass.GetSputnikClassFromCrateUid(container.net.ID);
            if (sputnikClass == null) return null;

            if (container is HackableLockedCrate)
            {
                if (sputnikClass.lockedCrates.Any(y => y.Key != null && y.Key.net.ID == container.net.ID)) cratePresetName = sputnikClass.lockedCrates[container];
            }
            else if (sputnikClass.crates.Any(y => y.Key != null && y.Key.net.ID == container.net.ID)) cratePresetName = sputnikClass.crates[container];

            if (cratePresetName == null) return null;
            CrateConfig crateConfig = _config.crateConfigs.FirstOrDefault(x => x.presetName == cratePresetName);
            if (crateConfig != null && crateConfig.typeLootTable != 2) return true;
            return null;
        }

        object OnCustomLootContainer(uint netID)
        {
            if (eventClass == null) return null;
            SputnikClass sputnikClass = eventClass.GetSputnikClassFromCrateUid(netID);
            if (sputnikClass == null) return null;
            string cratePresetName = null;
            if (sputnikClass.lockedCrates.Any(y => y.Key != null && y.Key.net.ID == netID)) cratePresetName = sputnikClass.lockedCrates.FirstOrDefault(y => y.Key != null && y.Key.net.ID == netID).Value;
            else if (sputnikClass.crates.Any(y => y.Key != null && y.Key.net.ID == netID)) cratePresetName = sputnikClass.crates.FirstOrDefault(y => y.Key != null && y.Key.net.ID == netID).Value;

            if (cratePresetName == null) return null;
            CrateConfig crateConfig = _config.crateConfigs.FirstOrDefault(x => x.presetName == cratePresetName);
            if (crateConfig != null && crateConfig.typeLootTable != 3) return true;
            return null;
        }

        object CanEntityTakeDamage(AutoTurret autoTurret, HitInfo hitinfo)
        {
            if (eventClass == null || autoTurret == null || hitinfo == null) return null;
            if (eventClass.IsEventTurret(autoTurret.net.ID))
            {
                if (hitinfo.InitiatorPlayer == null || !hitinfo.InitiatorPlayer.IsRealPlayer()) return false;
                else return true;
            }
            return null;
        }

        object CanEntityTakeDamage(Landmine landmine, HitInfo hitinfo)
        {
            if (eventClass == null || landmine == null || hitinfo == null) return null;
            if (eventClass.IsEventMine(landmine.net.ID))
            {
                if (hitinfo.InitiatorPlayer == null || !hitinfo.InitiatorPlayer.IsRealPlayer()) return false;
                else return true;
            }
            return null;
        }

        object CanEntityTakeDamage(ScientistNPC scientistNPC, HitInfo hitinfo)
        {
            if (eventClass == null || scientistNPC == null || hitinfo == null || hitinfo.Initiator == null) return null;
            NpcConfig npcConfig = _config.npcConfigs.FirstOrDefault(x => x.name == scientistNPC.displayName);
            if (npcConfig == null) return null;
            AutoTurret autoTurret = hitinfo.Initiator as AutoTurret;
            if (autoTurret == null) return null;
            if (eventClass.IsEventTurret(autoTurret.net.ID)) return false;
            return null;
        }

        object CanEntityTakeDamage(BasePlayer victim, HitInfo hitinfo)
        {
            if (eventClass == null || victim == null || hitinfo == null || !victim.userID.IsSteamId()) return null;
            BasePlayer attacker = hitinfo.InitiatorPlayer;
            if (attacker != null)
            {
                SputnikClass sputnikClass = eventClass.GetSputnikClassFromPLayerInZoneUid(victim.userID);
                if (sputnikClass != null && sputnikClass.sputnikDebrisConfig.zoneConfig.isCreateZonePVP && (attacker == null || (attacker != null && eventClass.GetSputnikClassFromPLayerInZoneUid(attacker.userID) != null))) return true;
            }
            else if (hitinfo.Initiator != null)
            {
                AutoTurret autoTurret = hitinfo.Initiator as AutoTurret;
                if (autoTurret != null)
                {
                    if (eventClass.IsEventTurret(autoTurret.net.ID)) return true;
                    return null;
                }
                BaseTrap baseTrap = hitinfo.Initiator as BaseTrap;
                if (baseTrap != null)
                {
                    if (eventClass.IsEventMine(baseTrap.net.ID)) return true;
                    return null;
                }
            }
            return null;
        }

        object CanEntityTrapTrigger(BaseTrap trap, BasePlayer player)
        {
            if (eventClass == null || trap == null) return null;
            if (eventClass.IsEventMine(trap.net.ID)) return true;
            return null;
        }

        object CanEntityBeTargeted(BasePlayer player, BaseEntity turret)
        {
            if (eventClass == null || turret == null || !player.IsRealPlayer()) return null;
            if (eventClass.IsEventTurret(turret.net.ID)) return true;
            return null;
        }
        #endregion OtherPlugins
        #endregion Hooks

        #region Commands
        [ChatCommand("sputnikstart")]
        void ChatStartCommand(BasePlayer player, string command, string[] arg)
        {
            if (!player.IsAdmin) return;
            if (arg != null && arg.Length >= 1) StartEvent(arg[0], player);
            else StartEvent(activator: player);
        }

        [ChatCommand("sputnikstop")]
        void ChatStopCommand(BasePlayer player, string command, string[] arg)
        {
            if (!player.IsAdmin) return;
            StopEvent();
        }

        [ConsoleCommand("sputnikstart")]
        void ConsoleStartCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null) return;
            if (arg.Args != null && arg.Args.Length > 0) StartEvent(arg.Args[0]);
            StartEvent();
            Puts("Event activated");
        }

        [ConsoleCommand("sputnikstop")]
        void ConsoleStopCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) StopEvent();
        }

        [ChatCommand("givespacecard")]
        void GiveCustomItemChatCommand(BasePlayer player, string command, string[] arg)
        {
            if (player == null || !player.IsAdmin || arg == null) return;

            MoveItem(player, CreateCustomCardItem());
            PrintToChat(player, GetMessage("GetSpaceCard", player.UserIDString, _config.prefix));
        }

        [ConsoleCommand("givespacecard")]
        void GiveCustomItemCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            BasePlayer target = null;

            if (arg.Args.Length >= 1) target = BasePlayer.FindByID(Convert.ToUInt64(arg.Args[0]));

            if (target == null)
            {
                PrintToConsole(player, "Player not found");
                return;
            }
            MoveItem(target, CreateCustomCardItem());
            PrintToChat(target, GetMessage("GetSpaceCard", target.UserIDString, _config.prefix));
            Puts($"A space card was given to {target.displayName}");
        }
        #endregion Commands

        #region Methods
        void PostLoadCheck()
        {
            if (!plugins.Exists("NpcSpawn"))
            {
                PrintError("NpcSpawn plugin doesn`t exist! Please read the file ReadMe.txt");
                NextTick(() => Server.Command($"o.unload {Name}"));
                return;
            }
        }

        void UpdateConfig()
        {
            if (_config.version == Version) return;

            if (_config.version.Minor == 0)
            {
                if (_config.version.Patch <= 2)
                {
                    _config.supportedPluginsConfig.zoneManager = new ZoneManagerConfig
                    {
                        enable = false,
                        blockFlags = new HashSet<string>
                        {
                            "eject",
                            "pvegod"
                        }
                    };
                }
                if (_config.version.Patch <= 3)
                {
                    _config.supportedPluginsConfig.raidableBases = new RaidableBasesConfig();
                    _config.spawnConfig.minTCDistance = 25;
                    _config.mainConfig.destroyAfterLootingTime = 300;
                }
            }

            _config.version = Version;
            SaveConfig();
        }

        void LoadData()
        {
            saveData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, List<EntData>>>(ins.Title);
            if (saveData == null || saveData.Count == 0)
            {
                PrintError("Data file not found");
                NextTick(() => Server.Command($"o.unload {Name}"));
            }
        }

        void Unsubscribes()
        {
            foreach (string hook in subscribeMethods) Unsubscribe(hook);
            if (!_config.customCardConfig.enableSpawnInDefaultCrates) Unsubscribe("OnLootSpawn");
        }

        void Subscribes()
        {
            foreach (string hook in subscribeMethods) Subscribe(hook);
        }

        void StartEvent(string presetName = "", BasePlayer activator = null)
        {
            if (eventClass != null)
            {
                SendErrorMessage(GetMessage("EventActive", activator != null ? activator.UserIDString : null, _config.prefix), activator);
                return;
            }
            StopEvent();
            EventConfig eventConfig = DefineEventConfig(presetName);
            if (eventConfig == null)
            {
                SendErrorMessage("Event preset not found!", activator);
                return;
            }
            if (autoEventCorountine != null) ServerMgr.Instance.StopCoroutine(autoEventCorountine);
            LocationDefiner.CacheBuildingPrivilegeLocations();
            eventClass = new EventClass();
            eventClass.StartEvent(eventConfig);
            Interface.CallHook("OnSputnikEventStart");
        }

        void StopEvent()
        {
            if (eventClass != null)
            {
                if (autoEventCorountine != null) ServerMgr.Instance.StopCoroutine(autoEventCorountine);
                if (_config.mainConfig.isAutoEvent) autoEventCorountine = ServerMgr.Instance.StartCoroutine(AutoEventCorountine());
                eventClass.StopEvent();
                eventClass = null;
                FindNewEventPoints();
                InformAllPlayers("EndEvent", _config.prefix);
                Interface.CallHook("OnSputnikEventStop");
                DefineEventWinner();
                SendBalance();
                Unsubscribes();
            }
        }

        EventConfig DefineEventConfig(string presetName)
        {
            if (presetName != "")
            {
                return ins._config.eventConfigs.FirstOrDefault(x => x.presetName.Contains(presetName));
            }
            else if (ins._config.eventConfigs.Any(x => x.chance > 0))
            {
                while (true)
                {
                    foreach (EventConfig checkEventConfig in ins._config.eventConfigs)
                    {
                        float chance = UnityEngine.Random.Range(0, 100);
                        if (chance <= checkEventConfig.chance)
                        {
                            return checkEventConfig;
                        }
                    }
                }
            }
            return null;
        }

        void FindNewEventPoints()
        {
            if (findSpawnPointsCorountine != null) ServerMgr.Instance.StopCoroutine(findSpawnPointsCorountine);
            findSpawnPointsCorountine = ServerMgr.Instance.StartCoroutine(LocationDefiner.FindSpawnPosition());
        }

        IEnumerator AutoEventCorountine()
        {
            yield return CoroutineEx.waitForSeconds(UnityEngine.Random.Range(_config.mainConfig.minTimeBetweenEvent, _config.mainConfig.maxTimeBetweenEvent));
            StartEvent();
        }

        void AddToContainerItem(ItemContainer container, LootTableConfig lootTableConfig, int typeOfLootTable)
        {
            if (typeOfLootTable == 1) container.ClearItemsContainer();
            int CountLootInContainer = 0;
            int countLoot = UnityEngine.Random.Range(lootTableConfig.minAmount, lootTableConfig.maxAmount);
            if (typeOfLootTable == 4) container.capacity += countLoot;
            else container.capacity = countLoot;
            for (; CountLootInContainer < countLoot;)
            {
                foreach (ItemConfig item in lootTableConfig.itemsConfig)
                {
                    if (UnityEngine.Random.Range(0.0f, 100.0f) <= item.chance)
                    {
                        int amount = UnityEngine.Random.Range(item.minAmount, item.maxAmount);
                        Item newItem = item.isBluePrint ? ItemManager.CreateByName("blueprintbase") : ItemManager.CreateByName(item.shortName, amount, item.skinID);
                        if (item.isBluePrint) newItem.blueprintTarget = ItemManager.FindItemDefinition(item.shortName).itemid;
                        if (item.name != "") newItem.name = item.name;
                        if (!newItem.MoveToContainer(container)) newItem.Remove();
                        if (CountLootInContainer == countLoot) return;
                        CountLootInContainer++;
                    }
                }
            }
        }

        #region MoveItem
        void MoveItem(BasePlayer player, Item item)
        {
            int spaceCountItem = GetSpaceCountItem(player, item.info.shortname, item.MaxStackable(), item.skin);
            int inventoryItemCount;
            if (spaceCountItem > item.amount) inventoryItemCount = item.amount;
            else inventoryItemCount = spaceCountItem;

            if (inventoryItemCount > 0)
            {
                Item itemInventory = ItemManager.CreateByName(item.info.shortname, inventoryItemCount, item.skin);
                if (item.skin != 0) itemInventory.name = item.name;

                item.amount -= inventoryItemCount;
                MoveInventoryItem(player, itemInventory);
            }

            if (item.amount > 0) MoveOutItem(player, item);
        }

        int GetSpaceCountItem(BasePlayer player, string shortname, int stack, ulong skinID)
        {
            int slots = player.inventory.containerMain.capacity + player.inventory.containerBelt.capacity;
            int taken = player.inventory.containerMain.itemList.Count + player.inventory.containerBelt.itemList.Count;
            int result = (slots - taken) * stack;
            foreach (Item item in player.inventory.AllItems()) if (item.info.shortname == shortname && item.skin == skinID && item.amount < stack) result += stack - item.amount;
            return result;
        }

        void MoveInventoryItem(BasePlayer player, Item item)
        {
            if (item.amount <= item.MaxStackable())
            {
                foreach (Item itemInv in player.inventory.AllItems())
                {
                    if (itemInv.info.shortname == item.info.shortname && itemInv.skin == item.skin && itemInv.amount < itemInv.MaxStackable())
                    {
                        if (itemInv.amount + item.amount <= itemInv.MaxStackable())
                        {
                            itemInv.amount += item.amount;
                            itemInv.MarkDirty();
                            return;
                        }
                        else
                        {
                            item.amount -= itemInv.MaxStackable() - itemInv.amount;
                            itemInv.amount = itemInv.MaxStackable();
                        }
                    }
                }
                if (item.amount > 0) player.inventory.GiveItem(item);
            }
            else
            {
                while (item.amount > item.MaxStackable())
                {
                    Item thisItem = ItemManager.CreateByName(item.info.shortname, item.MaxStackable(), item.skin);
                    if (item.skin != 0) thisItem.name = item.name;
                    player.inventory.GiveItem(thisItem);
                    item.amount -= item.MaxStackable();
                }
                if (item.amount > 0) player.inventory.GiveItem(item);
            }
        }

        void MoveOutItem(BasePlayer player, Item item)
        {
            if (item.amount <= item.MaxStackable()) item.Drop(player.transform.position, Vector3.up);
            else
            {
                while (item.amount > item.MaxStackable())
                {
                    Item thisItem = ItemManager.CreateByName(item.info.shortname, item.MaxStackable(), item.skin);
                    if (item.skin != 0) thisItem.name = item.name;
                    thisItem.Drop(player.transform.position, Vector3.up);
                    item.amount -= item.MaxStackable();
                }
                if (item.amount > 0) item.Drop(player.transform.position, Vector3.up);
            }
        }

        Item CreateCustomCardItem()
        {
            Item item = ItemManager.CreateByName(_config.customCardConfig.shortName, 1, _config.customCardConfig.skinID);
            if (_config.customCardConfig.name != "") item.name = _config.customCardConfig.name;
            return item;
        }
        #endregion MoveItem

        #region Notify
        void SendErrorMessage(string message, BasePlayer player = null)
        {
            if (player != null) PrintToChat(player, message);
            else PrintError(message);
        }

        void InformAllPlayers(string langKey, params object[] args)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList) if (player != null) InformPlayer(player, langKey, args);
            SendDiscordMessage(langKey, args);
        }

        void InformPlayer(BasePlayer player, string langKey, params object[] args)
        {
            if (_config.notifyConfig.chat) PrintToChat(player, GetMessage(langKey, player.UserIDString, args));
            if (_config.supportedPluginsConfig.GUIAnnouncements.isGUIAnnouncements) GUIAnnouncements?.Call("CreateAnnouncement", ClearColorAndSize(GetMessage(langKey, player.UserIDString, args)), _config.supportedPluginsConfig.GUIAnnouncements.bannerColor, _config.supportedPluginsConfig.GUIAnnouncements.textColor, player, _config.supportedPluginsConfig.GUIAnnouncements.apiAdjustVPosition);
            if (_config.supportedPluginsConfig.notify.isNotify) player.SendConsoleCommand($"notify.show {_config.supportedPluginsConfig.notify.type} {ClearColorAndSize(GetMessage(langKey, player.UserIDString, args))}");
        }

        void SendDiscordMessage(string langKey, params object[] args)
        {
            if (CanSendDiscordMessage() && _config.supportedPluginsConfig.discord.keys.Contains(langKey))
            {
                object fields = new[] { new { name = Title, value = ClearColorAndSize(GetMessage(langKey, null, args)), inline = false } };
                DiscordMessages?.Call("API_SendFancyMessage", _config.supportedPluginsConfig.discord.webhookUrl, "", _config.supportedPluginsConfig.discord.embedColor, JsonConvert.SerializeObject(fields), null, this);
            }
        }

        string ClearColorAndSize(string message)
        {
            message = message.Replace("</color>", string.Empty);
            message = message.Replace("</size>", string.Empty);
            while (message.Contains("<color="))
            {
                int index = message.IndexOf("<color=");
                message = message.Remove(index, message.IndexOf(">", index) - index + 1);
            }
            while (message.Contains("<size="))
            {
                int index = message.IndexOf("<size=");
                message = message.Remove(index, message.IndexOf(">", index) - index + 1);
            }
            return message;
        }

        private bool CanSendDiscordMessage() => _config.supportedPluginsConfig.discord.isDiscord && !string.IsNullOrEmpty(_config.supportedPluginsConfig.discord.webhookUrl) && _config.supportedPluginsConfig.discord.webhookUrl != "https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks";

        string GetTimeMessage(string userIDString, int seconds)
        {
            string message = "";

            TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
            if (timeSpan.Hours > 0) message += $" {timeSpan.Hours} {GetMessage("Hour", userIDString)}";
            if (timeSpan.Minutes > 0) message += $" {timeSpan.Minutes} {GetMessage("Min", userIDString)}";
            if (message == "") message += $" {timeSpan.Seconds} {GetMessage("Sec", userIDString)}";

            return message;
        }
        #endregion Notify

        #region Economy
        [PluginReference] readonly Plugin Economics, ServerRewards, IQEconomic;

        readonly Dictionary<ulong, double> _playersBalance = new Dictionary<ulong, double>();

        void ActionEconomy(ulong playerId, string type, string arg = "")
        {
            switch (type)
            {
                case "Crates":
                    double economyCrateData;
                    if (_config.supportedPluginsConfig.economy.crates.TryGetValue(arg, out economyCrateData)) AddBalance(playerId, economyCrateData);
                    break;
                case "Npc":
                    AddBalance(playerId, _config.supportedPluginsConfig.economy.npcPoint);
                    break;
                case "LockedCrate":
                    AddBalance(playerId, _config.supportedPluginsConfig.economy.lockedCratePoint);
                    break;
                case "Turret":
                    AddBalance(playerId, _config.supportedPluginsConfig.economy.turretPoint);
                    break;
                case "Heli":
                    AddBalance(playerId, _config.supportedPluginsConfig.economy.heliPoint);
                    break;
            }
        }

        void AddBalance(ulong playerId, double balance)
        {
            if (balance == 0) return;
            if (_playersBalance.ContainsKey(playerId)) _playersBalance[playerId] += balance;
            else _playersBalance.Add(playerId, balance);
        }

        void SendBalance()
        {
            if (!_config.supportedPluginsConfig.economy.enable || _playersBalance.Count == 0)
            {
                _playersBalance.Clear();
                return;
            }
            foreach (KeyValuePair<ulong, double> dic in _playersBalance)
            {
                if (dic.Value < _config.supportedPluginsConfig.economy.minEconomyPiont) continue;
                int intCount = Convert.ToInt32(dic.Value);
                if (_config.supportedPluginsConfig.economy.plugins.Contains("Economics") && plugins.Exists("Economics") && dic.Value > 0) Economics.Call("Deposit", dic.Key.ToString(), dic.Value);
                if (_config.supportedPluginsConfig.economy.plugins.Contains("Server Rewards") && plugins.Exists("ServerRewards") && intCount > 0) ServerRewards.Call("AddPoints", dic.Key, intCount);
                if (_config.supportedPluginsConfig.economy.plugins.Contains("IQEconomic") && plugins.Exists("IQEconomic") && intCount > 0) IQEconomic.Call("API_SET_BALANCE", dic.Key, intCount);
                BasePlayer player = BasePlayer.FindByID(dic.Key);
                if (player != null) InformPlayer(player, "SendEconomy", _config.prefix, dic.Value);
            }

            double max = 0;
            ulong winnerId = 0;
            foreach (var a in _playersBalance)
            {
                if (a.Value > max)
                {
                    max = a.Value;
                    winnerId = a.Key;
                }
            }

            if (max >= _config.supportedPluginsConfig.economy.minCommandPoint) foreach (string command in _config.supportedPluginsConfig.economy.commands) Server.Command(command.Replace("{steamid}", $"{winnerId}"));
            _playersBalance.Clear();
        }

        void DefineEventWinner()
        {
            var winnerPair = _playersBalance.Max(x => (float)x.Value);
            if (winnerPair.Value > 0) Interface.CallHook("OnSputnikEventWin", winnerPair.Key);
        }
        #endregion Economy
        #endregion Methods

        #region Classes
        class EventClass
        {
            EventConfig eventConfig;
            Coroutine startEventCorountine;
            Coroutine eventCorountine;
            HashSet<SputnikClass> sputnikClasses = new HashSet<SputnikClass>();
            internal int eventTime;

            internal SputnikClass GetSputnikClassFromPLayerInZoneUid(ulong userId) => sputnikClasses.FirstOrDefault(x => x != null && x.IsPlayerInZone(userId));

            internal SputnikClass GetSputnikClassFromCrateUid(uint crateUID) => sputnikClasses.FirstOrDefault(x => x != null && (x.IsEventCrate(crateUID) || x.IsEventLockedCrate(crateUID)));

            internal SputnikClass GetSputnikClassFromCardReaderUid(uint cardReaderUID) => sputnikClasses.FirstOrDefault(x => x != null && x.IsClassCardReader(cardReaderUID));

            internal bool IsEventTurret(uint netID) => sputnikClasses.Any(x => x != null && x.IsDebrisTurret(netID));

            internal bool IsEventMine(uint netID) => sputnikClasses.Any(x => x != null && x.IsDebrisMine(netID));

            internal void StartEvent(EventConfig eventConfig)
            {
                this.eventConfig = eventConfig;
                eventTime = eventConfig.eventTime;
                startEventCorountine = ServerMgr.Instance.StartCoroutine(StartEventCorountine());
            }

            internal void StopEvent()
            {
                foreach (SputnikClass sputnikClass in sputnikClasses)
                {
                    if (sputnikClass != null && sputnikClass.gameObject != null) UnityEngine.Object.DestroyImmediate(sputnikClass.gameObject);
                }
                if (startEventCorountine != null) ServerMgr.Instance.StopCoroutine(startEventCorountine);
                if (eventCorountine != null) ServerMgr.Instance.StopCoroutine(eventCorountine);
            }

            IEnumerator StartEventCorountine()
            {
                if (ins._config.notifyConfig.chat)
                {
                    foreach (BasePlayer player in BasePlayer.activePlayerList)
                        if (player != null) ins.InformPlayer(player, "PreStartEvent", ins._config.prefix, eventConfig.displayName, ins.GetTimeMessage(player.UserIDString, ins._config.notifyConfig.preStartTime));
                }
                ins.SendDiscordMessage("PreStartEvent", ins._config.prefix, eventConfig.displayName, ins.GetTimeMessage(null, ins._config.notifyConfig.preStartTime));

                yield return CoroutineEx.waitForSeconds(ins._config.notifyConfig.preStartTime);
                if (ins.autoEventCorountine != null) ServerMgr.Instance.StopCoroutine(ins.autoEventCorountine);
                ins.InformAllPlayers("StartEvent", ins._config.prefix, eventConfig.displayName);
                CreateNewSputnikClasses();
                eventCorountine = ServerMgr.Instance.StartCoroutine(EventCorountine());
                ins.Subscribes();
            }

            IEnumerator EventCorountine()
            {
                while (eventTime > 0 && sputnikClasses.Any(x => x != null))
                {
                    eventTime -= 1;
                    if (ins._config.notifyConfig.chat && ins._config.notifyConfig.timeNotifications.Contains(eventTime))
                    {
                        foreach (BasePlayer player in BasePlayer.activePlayerList)
                        {
                            if (player != null) ins.PrintToChat(player, ins.GetMessage("RemainTime", player.UserIDString, ins._config.prefix, eventConfig.displayName, ins.GetTimeMessage(player.UserIDString, eventTime)));
                        }
                    }
                    foreach (SputnikClass sputnikClass in sputnikClasses) sputnikClass.UpdateZone();
                    yield return CoroutineEx.waitForSeconds(1);
                }
                ins.StopEvent();
            }

            void CreateNewSputnikClasses()
            {
                if (eventConfig.fixedSputniksPresets != null && eventConfig.fixedSputniksPresets.Count > 0)
                {
                    foreach (string sputnikDebrisPresetName in eventConfig.fixedSputniksPresets)
                    {
                        SputnikDebrisConfig sputnikDebrisConfig = ins._config.sputnikDebrisConfigs.FirstOrDefault(x => x.presetName == sputnikDebrisPresetName);
                        if (sputnikDebrisConfig == null)
                        {
                            ins.PrintError("Sputnik debris preset not found!");
                            continue;
                        }
                        CreateNewSputnikClass(sputnikDebrisConfig);
                    }
                }

                if (sputnikClasses.Count == 0 || !sputnikClasses.Any(x => x != null)) ins.StopEvent();
            }

            void CreateNewSputnikClass(SputnikDebrisConfig sputnikDebrisConfig)
            {
                GameObject gameObject = new GameObject();
                gameObject.layer = (int)Layer.Reserved1;
                SputnikClass sputnik = gameObject.AddComponent<SputnikClass>();
                sputnik.Init(sputnikDebrisConfig);
                sputnikClasses.Add(sputnik);
            }
        }

        class SputnikClass : FacepunchBehaviour
        {
            internal SputnikDebrisConfig sputnikDebrisConfig;
            Coroutine crushEffectsCorountine;
            Coroutine onSputnikFellCorountine;
            Coroutine destroyCorountine;
            FallSputnikClass fallSputnik;
            MapMarkerGenericRadius mapmarker;
            VendingMachineMapMarker vendingMarker;
            HashSet<BaseEntity> decorEntities = new HashSet<BaseEntity>();
            HashSet<AutoTurret> turrets = new HashSet<AutoTurret>();
            HashSet<BaseEntity> mines = new HashSet<BaseEntity>();
            ZoneClass zoneClass;
            CardReader cardReader;
            SputnikHeli sputnikHeli;
            internal bool cardReaderOpen = true;
            internal HashSet<ScientistNPC> npcs = new HashSet<ScientistNPC>();
            internal HashSet<uint> openedCrates = new HashSet<uint>();
            internal Dictionary<LootContainer, string> crates = new Dictionary<LootContainer, string>();
            internal Dictionary<LootContainer, string> lockedCrates = new Dictionary<LootContainer, string>();
            internal HashSet<uint> spaceCardCratesIDs = new HashSet<uint>();
            internal int destroyTime = 0;

            internal bool IsClassCardReader(uint netId) => cardReader != null && cardReader.net.ID == netId;

            internal bool CanOpenCrate(uint netId) => !spaceCardCratesIDs.Contains(netId) || !cardReader.IsExists() || cardReaderOpen;

            internal bool IsEventCrate(uint netId) => crates.Keys.Any(x => x.IsExists() && x.net.ID == netId);

            internal bool IsEventLockedCrate(uint netId) => lockedCrates.Keys.Any(x => x.IsExists() && x.net.ID == netId);

            internal bool IsPlayerInZone(ulong userId) => zoneClass != null && zoneClass.playersInZone.Any(x => x.userID == userId);

            internal bool IsDebrisTurret(uint netId) => turrets.Any(x => x.IsExists() && x.net.ID == netId);

            internal bool IsDebrisMine(uint netId) => mines.Any(x => x.IsExists() && x.net.ID == netId);

            internal void RemovePlayerFromZone(BasePlayer player)
            {
                if (zoneClass == null || !zoneClass.playersInZone.Any(x => x.userID == player.userID)) return;
                zoneClass.playersInZone.Remove(player);
                if (ins._config.guiConfig.IsGUI) CuiHelper.DestroyUi(player, "SputnikGui");
                if (sputnikDebrisConfig.zoneConfig.isCreateZonePVP) ins.InformPlayer(player, "ExitPVP", ins._config.prefix);
            }

            internal void OnCratelooted(uint netId)
            {
                if (destroyCorountine != null || openedCrates.Contains(netId) || ins._config.mainConfig.destroyAfterLootingTime > ins.eventClass.eventTime || ins._config.mainConfig.destroyAfterLootingTime == 0) return;
                openedCrates.Add(netId);
                if (crates.Keys.Any(x => x != null && !openedCrates.Contains(x.net.ID)) || lockedCrates.Keys.Any(x => x != null && !openedCrates.Contains(x.net.ID))) return;
                destroyCorountine = ServerMgr.Instance.StartCoroutine(DestroyCorountine());
            }

            internal void Init(SputnikDebrisConfig sputnikDebrisConfig)
            {
                this.sputnikDebrisConfig = sputnikDebrisConfig;
                DefineEventPosition();
                CreateFallSputnik();
            }

            internal void UpdateZone()
            {
                UpdateMapMarker();
                if (zoneClass != null) zoneClass.UpdateGui();
            }

            void DefineEventPosition()
            {
                Vector3 position = Vector3.zero;
                while (position == Vector3.zero && LocationDefiner.spawnPositions.Count > 0)
                {
                    Vector3 newPosition = LocationDefiner.spawnPositions.GetRandom();
                    if (LocationDefiner.PostChechPoint(newPosition))
                    {
                        position = newPosition;
                        break;
                    }
                    else LocationDefiner.spawnPositions.Remove(newPosition);
                }
                if (position != Vector3.zero)
                {
                    gameObject.transform.position = position;
                    LocationDefiner.spawnPositions.Remove(position);
                }
                else
                {
                    ins.PrintError("The event could not be started! Increase the number of cached spawn points!");
                    ins.NextTick(() => ins.StopEvent());
                }
            }

            void CreateFallSputnik()
            {
                fallSputnik = gameObject.AddComponent<FallSputnikClass>();
                fallSputnik.Init(this, gameObject.transform.position);
            }

            internal void OnSputnikFell() => onSputnikFellCorountine = ServerMgr.Instance.StartCoroutine(OnSputnikFellCorountine());

            IEnumerator OnSputnikFellCorountine()
            {
                crushEffectsCorountine = ServerMgr.Instance.StartCoroutine(CrushEffectsCorountine());
                yield return CoroutineEx.waitForSeconds(0.5f);
                CreateGrounSputnik();
                CreateCardReader();
                CreateNPCs();
                CreateCrates();
                CreateMines();
                SpawnMarkers();
                CreateHeli();
                CreateTurrets();
                CreateZone();
                yield return CoroutineEx.waitForSeconds(2f);
                if (fallSputnik != null) Destroy(fallSputnik);
                ins.InformAllPlayers("Crash", ins._config.prefix, PhoneController.PositionToGridCoord(gameObject.transform.position));
            }

            IEnumerator CrushEffectsCorountine()
            {
                for (int i = 0; i < ins._config.fallindConfig.countEffects; i++)
                {
                    Effect.server.Run("assets/bundled/prefabs/fx/explosions/explosion_03.prefab", gameObject.transform.position + new Vector3(UnityEngine.Random.Range(-7.5f, 7.5f), 0, UnityEngine.Random.Range(-7.5f, 7.5f)));
                    Effect.server.Run("assets/bundled/prefabs/fx/explosions/explosion_02.prefab", gameObject.transform.position + new Vector3(UnityEngine.Random.Range(-7.5f, 7.5f), 0, UnityEngine.Random.Range(-7.5f, 7.5f)));
                    yield return CoroutineEx.waitForSeconds(0.1f);
                }
            }

            IEnumerator DestroyCorountine()
            {
                destroyTime = ins._config.mainConfig.destroyAfterLootingTime;
                UpdateZone();
                while (destroyTime > 1)
                {
                    destroyTime -= 1;
                    yield return CoroutineEx.waitForSeconds(1);
                }

                UnityEngine.Object.DestroyImmediate(this.gameObject);
            }

            void CreateZone()
            {
                zoneClass = gameObject.AddComponent<ZoneClass>();
                zoneClass.Init(this);
            }

            void SpawnMarkers()
            {
                if (!sputnikDebrisConfig.markerConfig.enable) return;
                SpawnMapMarker();
                SpawnVendingMarker();
            }

            void SpawnMapMarker()
            {
                mapmarker = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", gameObject.transform.position) as MapMarkerGenericRadius;
                mapmarker.enableSaving = false;
                mapmarker.Spawn();
                mapmarker.radius = sputnikDebrisConfig.markerConfig.radius;
                mapmarker.alpha = sputnikDebrisConfig.markerConfig.alpha;
                mapmarker.color1 = new Color(sputnikDebrisConfig.markerConfig.color1.r, sputnikDebrisConfig.markerConfig.color1.g, sputnikDebrisConfig.markerConfig.color1.b);
                mapmarker.color2 = new Color(sputnikDebrisConfig.markerConfig.color2.r, sputnikDebrisConfig.markerConfig.color2.g, sputnikDebrisConfig.markerConfig.color2.b);
            }

            void SpawnVendingMarker()
            {
                vendingMarker = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab", gameObject.transform.position) as VendingMachineMapMarker;
                vendingMarker.enableSaving = false;
                vendingMarker.markerShopName = $"{sputnikDebrisConfig.markerConfig.displayName} {ins.GetTimeMessage(null, ins.eventClass.eventTime)}";
                vendingMarker.Spawn();
            }

            void UpdateMapMarker()
            {
                if (vendingMarker != null)
                {
                    string text = destroyTime > 0 ? $"{sputnikDebrisConfig.markerConfig.displayName} {ins.GetTimeMessage(null, destroyTime)}" : $"{sputnikDebrisConfig.markerConfig.displayName} {ins.GetTimeMessage(null, ins.eventClass.eventTime)}";
                    vendingMarker.markerShopName = text;
                    vendingMarker.SendNetworkUpdate();
                }

                if (mapmarker != null)
                {
                    mapmarker.SendUpdate();
                    mapmarker.SendNetworkUpdate();
                }
            }

            void CreateGrounSputnik()
            {
                List<EntData> entDatas;
                saveData.TryGetValue(sputnikDebrisConfig.locationPreset, out entDatas);
                if (saveData == null)
                {
                    ins.PrintError($"Sputnik debris preset not found! ({sputnikDebrisConfig.locationPreset})");
                    ins.StopEvent();
                    return;
                }
                foreach (EntData entData in entDatas)
                {
                    decorEntities.Add(Builder.CreateDecorEntity(entData, gameObject.transform));
                }
            }

            void CreateNPCs()
            {
                foreach (var pair in sputnikDebrisConfig.NPCs)
                {
                    NpcConfig npcConfig = ins._config.npcConfigs.FirstOrDefault(x => x.name == pair.Key);
                    if (npcConfig == null)
                    {
                        ins.PrintError("NPC preset not found!");
                        continue;
                    }
                    JObject npcConfigObject = Builder.GetNpcConfig(npcConfig);
                    foreach (string positionString in pair.Value)
                    {
                        npcs.Add((ScientistNPC)ins.NpcSpawn.Call("SpawnNpc", LocationDefiner.GetGroundPositionInPoint(LocationDefiner.GetGlobalPosition(gameObject.transform, positionString.ToVector3())), npcConfigObject));
                    }
                }
            }

            void CreateCrates()
            {
                foreach (var pair in sputnikDebrisConfig.groundCrates)
                {
                    foreach (LocationConfig LocationConfig in pair.Value)
                        CreateCrate(pair.Key, LocationDefiner.GetGroundPositionInPoint(LocationDefiner.GetGlobalPosition(gameObject.transform, LocationConfig.position.ToVector3())), LocationDefiner.GetGlobalRotation(gameObject.transform, LocationConfig.rotation.ToVector3()));
                }

                foreach (var pair in sputnikDebrisConfig.сrates)
                {
                    foreach (LocationConfig LocationConfig in pair.Value)
                        CreateCrate(pair.Key, LocationDefiner.GetGlobalPosition(gameObject.transform, LocationConfig.position.ToVector3()), LocationDefiner.GetGlobalRotation(gameObject.transform, LocationConfig.rotation.ToVector3()));
                }
            }

            void CreateCrate(string cratePresetName, Vector3 position, Quaternion rotation)
            {
                CrateConfig crateConfig = ins._config.crateConfigs.FirstOrDefault(x => x.presetName == cratePresetName);
                if (crateConfig == null)
                {
                    ins.PrintError("Crate configuration not found!");
                    return;
                }

                LootContainer lootContainer = Builder.CreateRegularEntity(crateConfig.prefab, position, rotation) as LootContainer;

                if (lootContainer == null) return;
                lootContainer.skinID = 1223451782;
                HackableLockedCrate hackableLockedCrate = lootContainer as HackableLockedCrate;
                if (hackableLockedCrate != null)
                {
                    hackableLockedCrate.DestroyShared();
                    hackableLockedCrate.shouldDecay = false;
                    hackableLockedCrate.decayTimer = float.MaxValue;
                    hackableLockedCrate.SendNetworkUpdate();
                    hackableLockedCrate.hackSeconds = HackableLockedCrate.requiredHackSeconds - crateConfig.crateUnlockTime;
                    lockedCrates.Add(hackableLockedCrate, crateConfig.presetName);
                }
                else crates.Add(lootContainer, crateConfig.presetName);
                if (crateConfig.typeLootTable == 1 || crateConfig.typeLootTable == 4) Invoke(() => ins.AddToContainerItem(lootContainer.inventory, crateConfig.ownLootTable, crateConfig.typeLootTable), 2f);
                if (cardReader.IsExists() && crateConfig.needSpaceCard) spaceCardCratesIDs.Add(lootContainer.net.ID);
            }

            void CreateCardReader()
            {
                if (!sputnikDebrisConfig.enableCardReader) return;
                cardReader = Builder.CreateRegularEntity("assets/prefabs/io/electric/switches/cardreader.prefab", LocationDefiner.GetGlobalPosition(gameObject.transform, sputnikDebrisConfig.cardRaderLocation.position.ToVector3()), LocationDefiner.GetGlobalRotation(gameObject.transform, sputnikDebrisConfig.cardRaderLocation.rotation.ToVector3())) as CardReader;
                if (cardReader.IsExists())
                {
                    cardReaderOpen = false;
                    cardReader.UpdateFromInput(100, 0);
                }
            }

            void CreateMines()
            {
                foreach (string positionString in sputnikDebrisConfig.mines)
                {
                    mines.Add(Builder.CreateRegularEntity("assets/prefabs/deployable/landmine/landmine.prefab", LocationDefiner.GetGroundPositionInPoint(LocationDefiner.GetGlobalPosition(gameObject.transform, positionString.ToVector3())), Quaternion.identity));
                }
            }

            void CreateHeli()
            {
                if (sputnikDebrisConfig.heliPresetName == "") return;
                HeliConfig heliConfig = ins._config.heliConfigs.FirstOrDefault(x => x.presetName == sputnikDebrisConfig.heliPresetName);
                if (heliConfig == null)
                {
                    ins.PrintError("Heli configuration not found!");
                    return;
                }
                BaseHelicopter heli = GameManager.server.CreateEntity("assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab", gameObject.transform.position + new Vector3(0, heliConfig.height, 0)) as BaseHelicopter;
                heli.enableSaving = false;
                heli.Spawn();
                heli.transform.position = gameObject.transform.position + new Vector3(0, heliConfig.height, 0);
                sputnikHeli = heli.gameObject.AddComponent<SputnikHeli>();
                sputnikHeli.InitHelicopter(heli, heliConfig, gameObject.transform.position);
            }

            void CreateTurrets()
            {
                foreach (var turretPair in sputnikDebrisConfig.turrets)
                {
                    TurretConfig turretConfig = ins._config.turretConfigs.FirstOrDefault(x => x.presetName == turretPair.Key);
                    if (turretConfig == null)
                    {
                        ins.PrintError("Turret configuration not found!");
                        continue;
                    }
                    foreach (LocationConfig locationConfig in turretPair.Value) CreateTurret(turretConfig, locationConfig);
                }
            }

            void CreateTurret(TurretConfig turretConfig, LocationConfig locationConfig)
            {
                Vector3 position = LocationDefiner.GetGlobalPosition(gameObject.transform, locationConfig.position.ToVector3());
                if (turretConfig.autoHeight) position = LocationDefiner.GetGroundPositionInPoint(position);
                AutoTurret autoTurret = Builder.CreateRegularEntity("assets/prefabs/npc/autoturret/autoturret_deployed.prefab", position, LocationDefiner.GetGlobalRotation(gameObject.transform, locationConfig.rotation.ToVector3())) as AutoTurret;
                ContainerIOEntity containerIO = autoTurret.GetComponent<ContainerIOEntity>();
                containerIO.inventory.Insert(ItemManager.CreateByName(turretConfig.shortNameWeapon));
                containerIO.inventory.Insert(ItemManager.CreateByName(turretConfig.shortNameAmmo, turretConfig.countAmmo));
                containerIO.SendNetworkUpdate();
                autoTurret.InitializeHealth(turretConfig.hp, turretConfig.hp);
                autoTurret.UpdateFromInput(10, 0);
                autoTurret.isLootable = false;
                autoTurret.dropFloats = false;
                autoTurret.dropsLoot = false;
                turrets.Add(autoTurret);
            }

            void OnDestroy()
            {
                if (crushEffectsCorountine != null) ServerMgr.Instance.StopCoroutine(crushEffectsCorountine);
                if (onSputnikFellCorountine != null) ServerMgr.Instance.StopCoroutine(onSputnikFellCorountine);
                if (destroyCorountine != null) ServerMgr.Instance.StopCoroutine(destroyCorountine);
                if (mapmarker.IsExists()) mapmarker.Kill();
                if (vendingMarker.IsExists()) vendingMarker.Kill();
                if (cardReader.IsExists()) cardReader.Kill();
                if (sputnikHeli != null && sputnikHeli.baseHelicopter.IsExists()) sputnikHeli.baseHelicopter.Kill();
                foreach (BaseEntity entity in decorEntities) if (entity.IsExists()) entity.Kill();
                foreach (BaseEntity entity in turrets) if (entity.IsExists()) entity.Kill();
                foreach (ScientistNPC entity in npcs) if (entity.IsExists()) entity.Kill();
                foreach (LootContainer entity in lockedCrates.Keys) if (entity.IsExists()) entity.Kill();
                foreach (LootContainer entity in crates.Keys) if (entity.IsExists()) entity.Kill();
                foreach (BaseEntity entity in mines) if (entity.IsExists()) entity.Kill();
            }
        }

        class ZoneClass : FacepunchBehaviour
        {
            SputnikClass sputnikClass;
            SphereCollider sphereCollider;
            TriggerRadiation radiation;
            HashSet<BaseEntity> spheres = new HashSet<BaseEntity>();
            internal HashSet<BasePlayer> playersInZone = new HashSet<BasePlayer>();

            internal void Init(SputnikClass sputnikClass)
            {
                this.sputnikClass = sputnikClass;
                CreateZone();
            }

            internal void UpdateGui()
            {
                if (ins._config.guiConfig.IsGUI)
                {
                    foreach (BasePlayer player in playersInZone)
                    {
                        if (player == null) continue;
                        MessageGUI(player, ins.GetMessage("GUI", player.UserIDString, sputnikClass != null && sputnikClass.destroyTime > 0 ? ins.GetTimeMessage(player.UserIDString, sputnikClass.destroyTime) : ins.GetTimeMessage(player.UserIDString, ins.eventClass.eventTime)));
                    }
                }
            }

            void OnDestroy()
            {
                foreach (BaseEntity sphere in spheres) if (sphere.IsExists()) sphere.Kill();
                foreach (BasePlayer player in BasePlayer.activePlayerList) if (player != null) CuiHelper.DestroyUi(player, "SputnikGui");
                playersInZone.Clear();
                if (ins._config.supportedPluginsConfig.pveMode.pve && ins.plugins.Exists("PveMode")) ins.PveMode.Call("EventRemovePveMode", ins.Name + sputnikClass.gameObject.transform.position, false);
            }

            void CreateZone()
            {
                sphereCollider = sputnikClass.gameObject.AddComponent<SphereCollider>();
                sphereCollider.isTrigger = true;
                sphereCollider.radius = sputnikClass.sputnikDebrisConfig.zoneConfig.radius;
                sphereCollider.transform.position = sputnikClass.gameObject.transform.position;

                if (sputnikClass.sputnikDebrisConfig.zoneConfig.radiation > 0)
                {
                    radiation = sphereCollider.gameObject.AddComponent<TriggerRadiation>();
                    radiation.RadiationAmountOverride = sputnikClass.sputnikDebrisConfig.zoneConfig.radiation;
                    radiation.interestLayers = 131072;
                    radiation.enabled = true;
                }

                if (ins._config.supportedPluginsConfig.pveMode.pve)
                {
                    JObject config = new JObject
                    {
                        ["Damage"] = ins._config.supportedPluginsConfig.pveMode.damage,
                        ["ScaleDamage"] = new JArray { ins._config.supportedPluginsConfig.pveMode.scaleDamage.Select(x => new JObject { ["Type"] = x.Type, ["Scale"] = x.Scale }) },
                        ["LootCrate"] = ins._config.supportedPluginsConfig.pveMode.lootCrate,
                        ["HackCrate"] = ins._config.supportedPluginsConfig.pveMode.hackCrate,
                        ["LootNpc"] = ins._config.supportedPluginsConfig.pveMode.lootNpc,
                        ["DamageNpc"] = ins._config.supportedPluginsConfig.pveMode.damageNpc,
                        ["DamageTank"] = false,
                        ["TargetNpc"] = ins._config.supportedPluginsConfig.pveMode.targetNpc,
                        ["TargetTank"] = false,
                        ["CanEnter"] = ins._config.supportedPluginsConfig.pveMode.canEnter,
                        ["CanEnterCooldownPlayer"] = ins._config.supportedPluginsConfig.pveMode.canEnterCooldownPlayer,
                        ["TimeExitOwner"] = ins._config.supportedPluginsConfig.pveMode.timeExitOwner,
                        ["AlertTime"] = ins._config.supportedPluginsConfig.pveMode.alertTime,
                        ["RestoreUponDeath"] = ins._config.supportedPluginsConfig.pveMode.restoreUponDeath,
                        ["CooldownOwner"] = ins._config.supportedPluginsConfig.pveMode.cooldownOwner,
                        ["Darkening"] = ins._config.supportedPluginsConfig.pveMode.darkening
                    };
                    HashSet<uint> npcs = new HashSet<uint>();
                    HashSet<uint> bradleys = new HashSet<uint>();
                    HashSet<uint> crates = new HashSet<uint>();

                    foreach (ScientistNPC scientistNPC in sputnikClass.npcs) if (scientistNPC.IsExists()) npcs.Add(scientistNPC.net.ID);
                    foreach (LootContainer lootContainer in sputnikClass.crates.Keys) if (lootContainer.IsExists()) crates.Add(lootContainer.net.ID);
                    foreach (LootContainer lootContainer in sputnikClass.lockedCrates.Keys) if (lootContainer.IsExists()) crates.Add(lootContainer.net.ID);
                    ins.PveMode.Call("EventAddPveMode", ins.Name + sputnikClass.gameObject.transform.position, config, sputnikClass.gameObject.transform.position, sputnikClass.sputnikDebrisConfig.zoneConfig.radius, crates, npcs, bradleys, new HashSet<ulong>(), null);
                }
                else if (sputnikClass.sputnikDebrisConfig.zoneConfig.isDome) CreateSphere();
            }

            void CreateSphere()
            {
                for (int i = 0; i < sputnikClass.sputnikDebrisConfig.zoneConfig.darkening; i++)
                {
                    BaseEntity sphere = GameManager.server.CreateEntity("assets/prefabs/visualization/sphere.prefab", sphereCollider.transform.position);
                    SphereEntity entity = sphere as SphereEntity;
                    entity.currentRadius = sputnikClass.sputnikDebrisConfig.zoneConfig.radius * 2;
                    entity.lerpSpeed = 0f;
                    sphere.enableSaving = false;
                    sphere.Spawn();
                    spheres.Add(sphere);
                }
            }

            void OnTriggerEnter(Collider other)
            {
                if (other.ToBaseEntity() == null) return;
                BasePlayer player = other.ToBaseEntity() as BasePlayer;
                if (player.IsRealPlayer() && !playersInZone.Contains(player))
                {
                    playersInZone.Add(player);
                    if (ins._config.guiConfig.IsGUI) MessageGUI(player, ins.GetMessage("GUI", player.UserIDString, sputnikClass != null && sputnikClass.destroyTime > 0 ? ins.GetTimeMessage(player.UserIDString, sputnikClass.destroyTime) : ins.GetTimeMessage(player.UserIDString, ins.eventClass.eventTime)));
                    if (sputnikClass.sputnikDebrisConfig.zoneConfig.isCreateZonePVP) ins.InformPlayer(player, "EnterPVP", ins._config.prefix);
                    if (radiation != null) player.EnterTrigger(radiation);
                }
            }

            void OnTriggerExit(Collider other)
            {
                if (other.ToBaseEntity() == null) return;
                BasePlayer player = other.ToBaseEntity() as BasePlayer;
                playersInZone.RemoveWhere(x => x == null);
                if (player.IsRealPlayer())
                {
                    playersInZone.Remove(player);
                    if (ins._config.guiConfig.IsGUI) CuiHelper.DestroyUi(player, "SputnikGui");
                    if (sputnikClass.sputnikDebrisConfig.zoneConfig.isCreateZonePVP) ins.InformPlayer(player, "ExitPVP", ins._config.prefix);
                    if (radiation != null) player.LeaveTrigger(radiation);
                }
            }

            void MessageGUI(BasePlayer player, string text)
            {
                CuiHelper.DestroyUi(player, "SputnikGui");

                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    Image = { Color = "0 0 0 0" },
                    RectTransform = { AnchorMin = ins._config.guiConfig.AnchorMin, AnchorMax = ins._config.guiConfig.AnchorMax },
                    CursorEnabled = false,
                }, "Hud", "SputnikGui");

                container.Add(new CuiElement
                {
                    Parent = "SputnikGui",
                    Components =
                    {
                        new CuiTextComponent() { Color = "1 1 1 1", FadeIn = 0f, Text = text, FontSize = 24, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" },
                        new CuiOutlineComponent { Distance = "1 1", Color = "0 0 0 1" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                    }
                });

                CuiHelper.AddUi(player, container);
            }
        }

        class FallSputnikClass : FacepunchBehaviour
        {
            SputnikClass baseSputnikClass;
            Vector3 startPosition;
            Vector3 targetPosition;
            Vector3 fallDirection;
            Coroutine sputnikFallCorountine;
            List<BaseEntity> fallEntities = new List<BaseEntity>();

            internal void Init(SputnikClass baseSputnikClass, Vector3 targetPosition)
            {
                this.baseSputnikClass = baseSputnikClass;
                this.targetPosition = targetPosition;
                startPosition = LocationDefiner.FindStartFallPosition(targetPosition);
                DefineFallDiretion();
                CreateEntities();
                Invoke(() => sputnikFallCorountine = ServerMgr.Instance.StartCoroutine(SputnikFallCorountine()), 0.5f);
            }

            void DefineFallDiretion()
            {
                fallDirection = (targetPosition - startPosition).normalized;
            }

            void CreateEntities()
            {
                BaseEntity mainEntity = Builder.CreateRegularEntity("assets/bundled/prefabs/oilfireballsmall.prefab", startPosition, Quaternion.identity);
                fallEntities.Add(mainEntity);
                foreach (EntityData entityData in ins.fallSputnicData)
                {
                    foreach (Location location in entityData.locations)
                    {
                        BaseEntity entity = Builder.CreateRegularEntity(entityData.prefabName, LocationDefiner.GetGlobalPosition(mainEntity.transform, location.position), Quaternion.identity);
                        fallEntities.Add(entity);
                    }
                }
            }

            IEnumerator SputnikFallCorountine()
            {
                while (fallEntities.Any(x => x.IsExists()) && fallEntities.FirstOrDefault(x => x.IsExists()).transform.position.y >= targetPosition.y)
                {
                    foreach (BaseEntity entity in fallEntities)
                    {
                        if (!entity.IsExists()) continue;
                        entity.transform.position = entity.transform.position += fallDirection * 0.5f * ins._config.fallindConfig.fallingSpeedScale;
                    }
                    yield return CoroutineEx.waitForSeconds(0.05f);
                }
                baseSputnikClass.OnSputnikFell();
            }

            void OnDestroy()
            {
                if (sputnikFallCorountine != null) ServerMgr.Instance.StopCoroutine(sputnikFallCorountine);
                foreach (BaseEntity entity in fallEntities) if (entity.IsExists()) entity.Kill();
            }
        }

        class SputnikHeli : FacepunchBehaviour
        {
            internal BaseHelicopter baseHelicopter;
            HeliConfig heliConfig;
            Vector3 position;
            Coroutine heliCoroutine;

            internal void InitHelicopter(BaseHelicopter baseHelicopter, HeliConfig heliConfig, Vector3 position)
            {
                this.heliConfig = heliConfig;
                this.baseHelicopter = baseHelicopter;
                this.position = position + new Vector3(0, heliConfig.height, 0);
                baseHelicopter.startHealth = heliConfig.hp;
                baseHelicopter.InitializeHealth(heliConfig.hp, heliConfig.hp);
                baseHelicopter.maxCratesToSpawn = heliConfig.cratesAmount;
                baseHelicopter.bulletDamage = heliConfig.bulletDamage;
                baseHelicopter.bulletSpeed = heliConfig.bulletSpeed;
                var weakspots = baseHelicopter.weakspots;
                if (weakspots != null && weakspots.Length > 1)
                {
                    weakspots[0].maxHealth = heliConfig.mainRotorHealth;
                    weakspots[0].health = heliConfig.mainRotorHealth;
                    weakspots[1].maxHealth = heliConfig.rearRotorHealth;
                    weakspots[1].health = heliConfig.rearRotorHealth;
                }
                heliCoroutine = ServerMgr.Instance.StartCoroutine(HelitCorountine());
            }

            IEnumerator HelitCorountine()
            {
                while (baseHelicopter.IsExists() && !baseHelicopter.myAI.isDead)
                {
                    baseHelicopter.myAI.spawnTime = UnityEngine.Time.realtimeSinceStartup;
                    if (baseHelicopter.Distance(position) > heliConfig.distance) baseHelicopter.myAI.SetTargetDestination(new Vector3(position.x + UnityEngine.Random.Range(-heliConfig.distance, heliConfig.distance), position.y, position.z + UnityEngine.Random.Range(-heliConfig.distance, heliConfig.distance)));
                    yield return CoroutineEx.waitForSeconds(2f);
                }
            }

            void OnDestroy()
            {
                if (heliCoroutine != null) ServerMgr.Instance.StopCoroutine(heliCoroutine);
            }
        }

        static class Builder
        {
            internal static BaseEntity CreateDecorEntity(EntData entData, Transform parentTransform)
            {
                BaseEntity entity = GameManager.server.CreateEntity(entData.prefab, LocationDefiner.GetGlobalPosition(parentTransform, entData.pos.ToVector3()), LocationDefiner.GetGlobalRotation(parentTransform, entData.rot.ToVector3()));
                if (entity == null) return null;
                entity.enableSaving = false;

                if (!entity.PrefabName.Contains("medium.rechargable.battery"))
                {
                    BaseEntity newEntity = entity.gameObject.AddComponent<BaseEntity>();
                    CopySerializableFields(entity, newEntity);
                    UnityEngine.GameObject.DestroyImmediate(entity, true);
                    entity = newEntity;
                }
                entity.Spawn();

                Rigidbody rigidbody = entity.GetComponent<Rigidbody>();
                if (rigidbody != null) UnityEngine.GameObject.DestroyImmediate(rigidbody);
                foreach (DestroyOnGroundMissing destroyOnGroundMissing in entity.GetComponentsInChildren<DestroyOnGroundMissing>()) if (destroyOnGroundMissing != null) UnityEngine.GameObject.DestroyImmediate(destroyOnGroundMissing);
                foreach (MeshCollider meshCollider in entity.GetComponentsInChildren<MeshCollider>()) if (meshCollider != null) UnityEngine.GameObject.DestroyImmediate(meshCollider);
                entity.SendNetworkUpdate();
                entity.SetFlag(BaseEntity.Flags.Busy, true);
                entity.SetFlag(BaseEntity.Flags.Locked, true);
                return entity;
            }

            internal static BaseEntity CreateRegularEntity(string prefab, Vector3 position, Quaternion rotation)
            {
                BaseEntity entity = GameManager.server.CreateEntity(prefab, position, rotation);
                if (entity == null) return null;
                entity.enableSaving = false;

                entity.Spawn();

                Rigidbody rigidbody = entity.GetComponent<Rigidbody>();
                if (rigidbody != null) rigidbody.isKinematic = true;
                foreach (DestroyOnGroundMissing destroyOnGroundMissing in entity.GetComponentsInChildren<DestroyOnGroundMissing>()) if (destroyOnGroundMissing != null) UnityEngine.GameObject.DestroyImmediate(destroyOnGroundMissing);
                foreach (MeshCollider meshCollider in entity.GetComponentsInChildren<MeshCollider>()) if (meshCollider != null) UnityEngine.GameObject.DestroyImmediate(meshCollider);

                entity.SendNetworkUpdate();
                return entity;
            }

            internal static JObject GetNpcConfig(NpcConfig config)
            {
                HashSet<string> states = new HashSet<string> { "RoamState", "ChaseState", "CombatState" };
                if (config.beltItems.Any(x => x.shortName == "rocket.launcher" || x.shortName == "explosive.timed")) states.Add("RaidState");
                return new JObject
                {
                    ["Name"] = config.name,
                    ["WearItems"] = new JArray { config.wearItems.Select(x => new JObject { ["ShortName"] = x.shortName, ["SkinID"] = x.skinID }) },
                    ["BeltItems"] = new JArray { config.beltItems.Select(x => new JObject { ["ShortName"] = x.shortName, ["Amount"] = x.amount, ["SkinID"] = x.skinID, ["Mods"] = new JArray { x.Mods.ToHashSet() }, ["Ammo"] = x.ammo }) },
                    ["Kit"] = config.kit,
                    ["Health"] = config.health,
                    ["RoamRange"] = config.roamRange,
                    ["ChaseRange"] = config.chaseRange,
                    ["DamageScale"] = config.damageScale,
                    ["TurretDamageScale"] = 1f,
                    ["AimConeScale"] = config.aimConeScale,
                    ["DisableRadio"] = config.disableRadio,
                    ["CanUseWeaponMounted"] = true,
                    ["CanRunAwayWater"] = true,
                    ["Speed"] = config.speed,
                    ["AreaMask"] = 1,
                    ["AgentTypeID"] = -1372625422,
                    ["HomePosition"] = string.Empty,
                    ["States"] = new JArray { states },
                    ["Sensory"] = new JObject
                    {
                        ["AttackRangeMultiplier"] = config.attackRangeMultiplier,
                        ["SenseRange"] = config.senseRange,
                        ["MemoryDuration"] = config.memoryDuration,
                        ["CheckVisionCone"] = config.checkVisionCone,
                        ["VisionCone"] = config.visionCone
                    }
                };
            }

            static void CopySerializableFields<T>(T src, T dst)
            {
                FieldInfo[] srcFields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (FieldInfo field in srcFields)
                {
                    object value = field.GetValue(src);
                    field.SetValue(dst, value);
                }
            }
        }

        static class LocationDefiner
        {
            static float radius = 8f;
            static HashSet<Vector3> tcPositions = new HashSet<Vector3>();
            internal static List<Vector3> spawnPositions = new List<Vector3>();

            internal static void CacheBuildingPrivilegeLocations()
            {
                tcPositions.Clear();
                foreach (BuildingPrivlidge buildingPrivlidge in BaseNetworkable.serverEntities.OfType<BuildingPrivlidge>().Where(x => x != null && x.IsExists()))
                {
                    tcPositions.Add(buildingPrivlidge.transform.position);
                }
            }

            internal static IEnumerator FindSpawnPosition()
            {
                int count = 0;
                bool inform = spawnPositions.Count == 0;
                int maxCount = ins._config.spawnConfig.countSpawnPoints * 50;
                if (inform) ins.PrintWarning("The search for event locations has begun!");
                FillZoneManagerData();
                CacheBuildingPrivilegeLocations();
                while (spawnPositions.Count < ins._config.spawnConfig.countSpawnPoints || count >= ins._config.spawnConfig.countSpawnPoints * maxCount)
                {
                    Vector3 position = GetRandomMapPoint();
                    if (ChechPoint(position)) spawnPositions.Add(position);
                    count++;
                    yield return CoroutineEx.waitForSeconds(0.01f);
                }
                if (inform) ins.PrintWarning($"Found {spawnPositions.Count} points for the event");
            }

            internal static bool PostChechPoint(Vector3 postition) => postition != Vector3.zero && CheckColliders(postition) && CheckRaidableBases(postition);

            internal static bool ChechPoint(Vector3 postition)
            {
                return postition != Vector3.zero && CheckGround(postition) && CheckUpSpace(postition) && ChechUnderWater(postition) && CheckNearPoints(postition)
                    && CheckMonuments(postition) && CheckDeltaHeightInRadius(postition) && CheckColliders(postition) && CheckNewSpawnEntity(postition) && CheckZoneManager(postition) && CheckBuildingPrivileges(postition) && CheckRaidableBases(postition);
            }

            static bool CheckGround(Vector3 postition) => Math.Abs(TerrainMeta.HeightMap.GetHeight(postition) - postition.y) < 0.05f;

            static bool ChechUnderWater(Vector3 postition) => -postition.y < ins._config.spawnConfig.maxDepth;

            static bool CheckNearPoints(Vector3 postition) => !spawnPositions.Any(x => Vector3.Distance(x, postition) < ins._config.spawnConfig.minDistance);

            static bool CheckUpSpace(Vector3 postition) => !Physics.Raycast(postition + new Vector3(0, 0.5f, 0), Vector3.up, radius);

            internal static bool CheckMonuments(Vector3 postition)
            {
                if (ins._config.spawnConfig.allowMonumentSpawn) return !TerrainMeta.Path.Monuments.Any(x => Vector3.Distance(postition, x.transform.position) < x.Bounds.size.x && ins._config.spawnConfig.disabledMonuments.Any(y => x.name.Contains(y)));
                else return !TerrainMeta.Path.Monuments.Any(x => Vector3.Distance(postition, x.transform.position) < x.Bounds.size.x);
            }

            static bool CheckDeltaHeightInRadius(Vector3 postition)
            {
                float maxDeltaHeight = 0.5f;

                float degree = 0;

                while (degree < 360)
                {
                    float radian = (2f * Mathf.PI / 360) * degree;
                    float x = postition.x + radius * Mathf.Cos(radian);
                    float z = postition.z + radius * Mathf.Sin(radian);

                    Vector3 positionInRadius = GetGroundPositionInPoint(new Vector3(x, postition.y, z));
                    if (Math.Abs(positionInRadius.y - postition.y) > maxDeltaHeight) return false;
                    if (Physics.Raycast(postition + new Vector3(0, 1, 0), (positionInRadius - postition).normalized, radius, 1 << 16 | 1 << 21)) return false;
                    degree += 45;
                }

                return true;
            }

            static bool CheckColliders(Vector3 postition)
            {
                foreach (Collider collider in UnityEngine.Physics.OverlapSphere(postition, 10f))
                {
                    if (collider.name.Contains("Rail Mesh") || collider.name.Contains("Road Mesh") || collider.name.Contains("train_track") || collider.name.Contains("Safe") || collider.name.Contains("Trigger (8)")) return false;
                    BaseEntity entity = collider.ToBaseEntity();
                    if (entity == null) continue;
                    if (entity is BuildingBlock) return false;
                }
                return true;
            }

            static bool CheckNewSpawnEntity(Vector3 position)
            {
                DoorCloser doorCloser = GameManager.server.CreateEntity("assets/prefabs/misc/doorcloser/doorcloser.prefab", position) as DoorCloser;
                doorCloser.enableSaving = false;
                doorCloser.Spawn();
                if (doorCloser.GetBuildingPrivilege() != null || (ins._config.supportedPluginsConfig.zoneManager.enable && ins.plugins.Exists("ZoneManager") && ins._config.supportedPluginsConfig.zoneManager.blockFlags.Any(x => (bool)ins.ZoneManager.Call("EntityHasFlag", doorCloser, x))))
                {
                    doorCloser.Kill();
                    return false;
                }
                doorCloser.Kill();
                return true;
            }

            static bool CheckBuildingPrivileges(Vector3 position)
            {
                return !tcPositions.Any(x => Vector3.Distance(x, position) < ins._config.spawnConfig.minTCDistance);
            }

            static Vector3 GetRandomMapPoint()
            {
                float mapSize = TerrainMeta.Size.x / 2;
                Vector3 randomPosition = new Vector3(UnityEngine.Random.Range(-mapSize, mapSize), 500f, UnityEngine.Random.Range(-mapSize, mapSize));
                randomPosition = GetGroundPositionInPoint(randomPosition);
                return randomPosition;
            }

            internal static Vector3 GetGroundPositionInPoint(Vector3 position)
            {
                position.y = 500f;
                RaycastHit raycastHit;
                Physics.Raycast(position, Vector3.down, out raycastHit, 550f, 1 << 16 | 1 << 23);
                position.y = raycastHit.point.y;
                if (position.y == 0) return Vector3.zero;
                return position;
            }

            internal static Vector3 FindStartFallPosition(Vector3 fallTargetPosition)
            {
                int counter = 0;
                while (counter < 10)
                {
                    float randomX = UnityEngine.Random.Range(ins._config.fallindConfig.minFallOffset, ins._config.fallindConfig.maxFallOffset);
                    if (UnityEngine.Random.Range(1, 10) < 5) randomX *= -1;
                    float randomZ = UnityEngine.Random.Range(ins._config.fallindConfig.minFallOffset, ins._config.fallindConfig.maxFallOffset);
                    if (UnityEngine.Random.Range(1, 10) < 5) randomZ *= -1;

                    Vector3 newFallStartPosition = fallTargetPosition + new Vector3(randomX, UnityEngine.Random.Range(ins._config.fallindConfig.minFallHeight, ins._config.fallindConfig.maxFallHeight), randomZ);
                    if (Physics.Raycast(newFallStartPosition, (fallTargetPosition - newFallStartPosition).normalized, Vector3.Distance(fallTargetPosition, newFallStartPosition) - 5, 1 << 16 | 1 << 21))
                    {
                        counter++;
                        continue;
                    }
                    else return newFallStartPosition;
                }
                return fallTargetPosition + new Vector3(0, UnityEngine.Random.Range(ins._config.fallindConfig.minFallHeight, ins._config.fallindConfig.maxFallHeight), 0);
            }

            internal static Vector3 GetGlobalPosition(Transform parentTransform, Vector3 position)
            {
                return parentTransform.transform.TransformPoint(position);
            }

            internal static Quaternion GetGlobalRotation(Transform parentTransform, Vector3 rotation)
            {
                return parentTransform.rotation * Quaternion.Euler(rotation);
            }

            static List<ZoneManagerData> zoneManagerDatas = new List<ZoneManagerData>();

            static bool CheckZoneManager(Vector3 position)
            {
                if (!ins._config.supportedPluginsConfig.zoneManager.enable || !ins.plugins.Exists("ZoneManager") || zoneManagerDatas.Count == 0) return true;
                if (zoneManagerDatas.Any(x => x != null && Vector3.Distance(x.position, position) < x.radius)) return false;
                return true;
            }

            internal static bool CheckRaidableBases(Vector3 position)
            {
                if (!ins._config.supportedPluginsConfig.raidableBases.enable || !ins.plugins.Exists("RaidableBases")) return true;
                return !(bool)ins.RaidableBases.Call("EventTerritory", position);
            }

            static void FillZoneManagerData()
            {
                zoneManagerDatas.Clear();

                if (!ins._config.supportedPluginsConfig.zoneManager.enable || !ins.plugins.Exists("ZoneManager")) return;
                string[] zoneArray = ins.ZoneManager?.Call("GetZoneIDs") as string[];
                if (zoneArray == null || zoneArray.Length == 0) return;
                foreach (string zoneName in zoneArray)
                {
                    if (!ins._config.supportedPluginsConfig.zoneManager.blockFlags.Any(x => (bool)ins.ZoneManager.Call("HasFlag", zoneName, x))) continue;
                    Vector3 zonePosition = (Vector3)ins.ZoneManager.Call("GetZoneLocation", zoneName);
                    if (zonePosition == Vector3.zero) continue;
                    float zoneRadius = (float)ins.ZoneManager.Call("GetZoneRadius", zoneName);
                    zoneManagerDatas.Add(new ZoneManagerData
                    {
                        position = zonePosition,
                        radius = zoneRadius
                    });
                }
                return;
            }

            public class ZoneManagerData
            {
                internal Vector3 position;
                internal float radius;
            }
        }

        public class EntData
        {
            public string prefab;
            public string pos;
            public string rot;
        }
        #endregion Classes

        #region Constructions
        HashSet<EntityData> fallSputnicData = new HashSet<EntityData>
        {
            new EntityData("assets/bundled/prefabs/oilfireballsmall.prefab",
                new HashSet<Location>
                {
                    new Location(new Vector3(1, 0, 0), Vector3.zero),
                    new Location(new Vector3(-1, 0, 0), Vector3.zero),
                    new Location(new Vector3(0, 0, 1), Vector3.zero),
                    new Location(new Vector3(0, 0, -1), Vector3.zero),
                    new Location(new Vector3(1, 0, 1), Vector3.zero),
                    new Location(new Vector3(-1, 0, -1), Vector3.zero),
                })
        };

        class EntityData
        {
            public string prefabName { get; set; }
            public HashSet<Location> locations { get; set; }

            public EntityData(string prefabName, HashSet<Location> locations)
            {
                this.prefabName = prefabName;
                this.locations = locations;
            }
        }

        class Location
        {
            public Vector3 position { get; set; }
            public Vector3 rotation { get; set; }

            public Location(Vector3 position, Vector3 rotation)
            {
                this.position = position;
                this.rotation = rotation;
            }
        }
        #endregion Constructions

        #region Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["EventActive"] = "{0} Ивент в данный момент активен, сначала завершите текущий ивент (<color=#ce3f27>/sputikstop</color>)!",
                ["PreStartEvent"] = "{0} <color=#738d43>{1}</color> войдет в атмосферу через <color=#738d43>{2}</color>!",
                ["StartEvent"] = "{0} <color=#738d43>{1}</color> вошел в атмосферу!",
                ["Crash"] = "{0} <color=#738d43>Обломки</color> обнаружены в квадрате <color=#ce3f27>{1}</color>!",
                ["NeedUseCard"] = "{0} Используйте <color=#ce3f27>космическую карту</color> чтобы разблокировать ящик!",
                ["RemainTime"] = "{0} {1} будет уничтожен через <color=#ce3f27>{2}</color>!",
                ["EndEvent"] = "{0} Ивент <color=#ce3f27>окончен</color>!",
                ["GetSpaceCard"] = "{0} Вы получили <color=#ce3f27>космическую карту</color>!",

                ["EnterPVP"] = "{0} Вы <color=#ce3f27>вошли</color> в PVP зону, теперь другие игроки <color=#ce3f27>могут</color> наносить вам урон!",
                ["ExitPVP"] = "{0} Вы <color=#738d43>вышли</color> из PVP зоны, теперь другие игроки <color=#738d43>не могут</color> наносить вам урон!",
                ["GUI"] = "Спутник будет уничтожен через <color=#ce3f27>{0}</color>",

                ["Hour"] = "ч.",
                ["Min"] = "м.",
                ["Sec"] = "с.",
            }, this, "ru");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["EventActive"] = "{0} This event is active now. To finish this event (<color=#ce3f27>/sputikstop</color>)!",
                ["PreStartEvent"] = "{0} <color=#738d43>{1}</color> will enter the atmosphere in <color=#738d43>{2}</color>!",
                ["StartEvent"] = "{0} <color=#738d43>{1}</color> entered the atmosphere!",
                ["Crash"] = "{0} <color=#738d43>Debris</color> detected at grid <color=#ce3f27>{1}</color>!",
                ["NeedUseCard"] = "{0} Use the <color=#ce3f27>space card</color> to unlock the crate!",
                ["RemainTime"] = "{0} {1} will be destroyed in <color=#ce3f27>{2}</color>!",
                ["EndEvent"] = "{0} The event is <color=#ce3f27>over</color>!",
                ["GetSpaceCard"] = "{0} You got a <color=#ce3f27>space card</color>!",

                ["EnterPVP"] = "{0} You <color=#ce3f27>have entered</color> the PVP zone, now other players <color=#ce3f27>can damage</color> you!",
                ["ExitPVP"] = "{0} You <color=#738d43>have gone out</color> the PVP zone, now other players <color=#738d43>can’t damage</color> you!",
                ["GUI"] = "The sputnik will be destroyed in <color=#ce3f27>{0}</color>",

                ["Hour"] = "h.",
                ["Min"] = "m.",
                ["Sec"] = "s.",
            }, this);
        }

        string GetMessage(string langKey, string userID) => lang.GetMessage(langKey, this, userID);

        string GetMessage(string langKey, string userID, params object[] args) => (args.Length == 0) ? GetMessage(langKey, userID) : string.Format(GetMessage(langKey, userID), args);
        #endregion Lang

        #region Config
        private PluginConfig _config;

        protected override void LoadDefaultConfig() => _config = PluginConfig.DefaultConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<PluginConfig>();
            Config.WriteObject(_config, true);
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        public class MainConfig
        {
            [JsonProperty(en ? "Enable automatic event holding [true/false]" : "Включить автоматическое проведение ивента [true/false]")] public bool isAutoEvent { get; set; }
            [JsonProperty(en ? "Minimum time between events [sec]" : "Минимальное вермя между ивентами [sec]")] public int minTimeBetweenEvent { get; set; }
            [JsonProperty(en ? "Maximum time between events [sec]" : "Максимальное вермя между ивентами [sec]")] public int maxTimeBetweenEvent { get; set; }
            [JsonProperty(en ? "The time until the destruction of the debris after the looting of all the crates (0 - do not destroy) [sec]" : "Время до уничтожения обломков спутника после лутания всех ящиков (0 - не уничтожать) [sec]")] public int destroyAfterLootingTime { get; set; }
        }

        public class SpawnConfig
        {
            [JsonProperty(en ? "Number of cached spawn points" : "Число кэшированных точек спавна")] public int countSpawnPoints { get; set; }
            [JsonProperty(en ? "Minimum distance between event points" : "Минимальное расстояние между точками падения")] public float minDistance { get; set; }
            [JsonProperty(en ? "Maximum depth for spawn point" : "Максимальная глубина точки спавна")] public float maxDepth { get; set; }
            [JsonProperty(en ? "Allow spawn on monuments" : "Разрешить спавн на монументах")] public bool allowMonumentSpawn { get; set; }
            [JsonProperty(en ? "Disable spawn on these monuments" : "Отключить спавн на этих монументах ")] public HashSet<string> disabledMonuments { get; set; }
            [JsonProperty(en ? "Minimum distance to player cupboards" : "Минимальное расстояние до шкафов игроков")] public float minTCDistance { get; set; }
        }

        public class FallindConfig
        {
            [JsonProperty(en ? "Falling Speed Multiplier" : "Множитель скорости падения")] public float fallingSpeedScale { get; set; }
            [JsonProperty(en ? "Minimum height of the beginning of the fall" : "Минимальная высота начала падения")] public float minFallHeight { get; set; }
            [JsonProperty(en ? "Maximum height of the beginning of the fall" : "Максимальная высота начала падения")] public float maxFallHeight { get; set; }
            [JsonProperty(en ? "Minimum offset from the vertical axis when falling" : "Минимальное смещение от вертикальной оси при падении")] public float minFallOffset { get; set; }
            [JsonProperty(en ? "Maximum offset from the vertical axis when falling" : "Максимальное смещение от вертикальной оси при падении")] public float maxFallOffset { get; set; }
            [JsonProperty(en ? "Number of effects when falling" : "Количество эффектов при падении")] public int countEffects { get; set; }
        }

        public class EventConfig
        {
            [JsonProperty(en ? "Preset name" : "Название пресета")] public string presetName { get; set; }
            [JsonProperty(en ? "Display name" : "Отображаемое имя")] public string displayName { get; set; }
            [JsonProperty(en ? "Duration [sec.]" : "Продолжительность [sec.]")] public int eventTime { get; set; }
            [JsonProperty(en ? "Probability" : "Вероятность")] public float chance { get; set; }
            [JsonProperty(en ? "Set of sputniks" : "Набор спутников")] public List<string> fixedSputniksPresets { get; set; }
        }

        public class SputnikDebrisConfig
        {
            [JsonProperty(en ? "Preset name" : "Название пресета")] public string presetName { get; set; }
            [JsonProperty(en ? "Location preset (Data file)" : "Пресет локации (Data файл)")] public string locationPreset { get; set; }
            [JsonProperty(en ? "NPC name - locations" : "Имя NPC - расположения")] public Dictionary<string, HashSet<string>> NPCs { get; set; }
            [JsonProperty(en ? "Turn on the card reader spawn? [true/false]" : "Включить спавн считывателя карт? [true/false]")] public bool enableCardReader { get; set; }
            [JsonProperty(en ? "Location of the card reader" : "Расположение считывателя карт")] public LocationConfig cardRaderLocation { get; set; }
            [JsonProperty(en ? "Heli preset name" : "Пресет вертолета")] public string heliPresetName { get; set; }
            [JsonProperty(en ? "Turret preset - locations" : "Пресет турели - расположения")] public Dictionary<string, HashSet<LocationConfig>> turrets { get; set; }
            [JsonProperty(en ? "Locations of crates with automatic ground level detection (Crate preset - locations)" : "Расположения ящиков с автоматическим определением уровня земли (Пресет крейта - расположения)")] public Dictionary<string, HashSet<LocationConfig>> groundCrates { get; set; }
            [JsonProperty(en ? "Locations of crates without automatic ground level detection (Crate preset - locations)" : "Расположения ящиков без автоматического определения уровня земли (Пресет крейта - расположения)")] public Dictionary<string, HashSet<LocationConfig>> сrates { get; set; }
            [JsonProperty(en ? "Locations of mines" : "Расположения мин")] public List<string> mines { get; set; }
            [JsonProperty(en ? "Map marker setting" : "Настройка маркера на карте")] public MarkerConfig markerConfig { get; set; }
            [JsonProperty(en ? "Zone Setting" : "Настройки зоны ивента")] public ZoneConfig zoneConfig { get; set; }
        }

        public class MarkerConfig
        {
            [JsonProperty(en ? "Do you use the Marker? [true/false]" : "Использовать ли маркер? [true/false]")] public bool enable { get; set; }
            [JsonProperty(en ? "Display name" : "Отображаемое имя")] public string displayName { get; set; }
            [JsonProperty(en ? "Radius" : "Радиус")] public float radius { get; set; }
            [JsonProperty(en ? "Alpha" : "Прозрачность")] public float alpha { get; set; }
            [JsonProperty(en ? "Marker color" : "Цвет маркера")] public ColorConfig color1 { get; set; }
            [JsonProperty(en ? "Outline color" : "Цвет контура")] public ColorConfig color2 { get; set; }
        }

        public class CrateConfig
        {
            [JsonProperty(en ? "Preset Name" : "Название пресета")] public string presetName { get; set; }
            [JsonProperty("Prefab")] public string prefab { get; set; }
            [JsonProperty(en ? "Do you need to use a space card to open the box? [true/false]" : "Для открытия ящика требуется применить космическую карту? [true/false]")] public bool needSpaceCard { get; set; }
            [JsonProperty(en ? "Time to unlock the crates (LockedCrate) [sec.]" : "Время до открытия заблокированного ящика (LockedCrate) [sec.]")] public float crateUnlockTime { get; set; }
            [JsonProperty(en ? "Which loot table should the plugin use? (0 - default; 1 - own; 2 - AlphaLoot; 3 - CustomLoot; 4 - Add Items)" : "Какую таблицу лута необходимо использовать? (0 - стандартную; 1 - собственную; 2 - AlphaLoot; 3 - CustomLoot; 4 - Добавить предметы)")] public int typeLootTable { get; set; }
            [JsonProperty(en ? "Own loot table" : "Собственная таблица предметов")] public LootTableConfig ownLootTable { get; set; }
        }

        public class NpcConfig
        {
            [JsonProperty(en ? "Name" : "Название")] public string name { get; set; }
            [JsonProperty(en ? "Health" : "Кол-во ХП")] public float health { get; set; }
            [JsonProperty(en ? "Wear items" : "Одежда")] public List<NpcWear> wearItems { get; set; }
            [JsonProperty(en ? "Belt items" : "Быстрые слоты")] public List<NpcBelt> beltItems { get; set; }
            [JsonProperty(en ? "Kit" : "Kit")] public string kit { get; set; }
            [JsonProperty(en ? "Roam Range" : "Дальность патрулирования местности")] public float roamRange { get; set; }
            [JsonProperty(en ? "Chase Range" : "Дальность погони за целью")] public float chaseRange { get; set; }
            [JsonProperty(en ? "Attack Range Multiplier" : "Множитель радиуса атаки")] public float attackRangeMultiplier { get; set; }
            [JsonProperty(en ? "Sense Range" : "Радиус обнаружения цели")] public float senseRange { get; set; }
            [JsonProperty(en ? "Memory duration [sec.]" : "Длительность памяти цели [sec.]")] public float memoryDuration { get; set; }
            [JsonProperty(en ? "Scale damage" : "Множитель урона")] public float damageScale { get; set; }
            [JsonProperty(en ? "Aim Cone Scale" : "Множитель разброса")] public float aimConeScale { get; set; }
            [JsonProperty(en ? "Detect the target only in the NPC's viewing vision cone?" : "Обнаруживать цель только в углу обзора NPC? [true/false]")] public bool checkVisionCone { get; set; }
            [JsonProperty(en ? "Vision Cone" : "Угол обзора")] public float visionCone { get; set; }
            [JsonProperty(en ? "Speed" : "Скорость")] public float speed { get; set; }
            [JsonProperty(en ? "Should remove the corpse?" : "Удалять труп?")] public bool deleteCorpse { get; set; }
            [JsonProperty(en ? "Disable radio effects? [true/false]" : "Отключать эффекты рации? [true/false]")] public bool disableRadio { get; set; }
            [JsonProperty(en ? "Which loot table should the plugin use? (0 - default, BetterLoot, MagicLoot; 1 - own)" : "Какую таблицу лута необходимо использовать? (0 - стандартную, BetterLoot, MagicLoot; 1 - собственную)")] public int typeLootTable { get; set; }
            [JsonProperty(en ? "Own loot table" : "Собственная таблица лута")] public LootTableConfig lootTable { get; set; }
        }

        public class HeliConfig
        {
            [JsonProperty(en ? "Name" : "Название пресета")] public string presetName { get; set; }
            [JsonProperty("HP")] public float hp { get; set; }
            [JsonProperty(en ? "HP of the main rotor" : "HP главного винта")] public float mainRotorHealth { get; set; }
            [JsonProperty(en ? "HP of tail rotor" : "HP хвостового винта")] public float rearRotorHealth { get; set; }
            [JsonProperty(en ? "Numbers of crates" : "Количество ящиков")] public int cratesAmount { get; set; }
            [JsonProperty(en ? "Flying height" : "Высота полета")] public float height { get; set; }
            [JsonProperty(en ? "Bullet speed" : "Скорость пуль")] public float bulletSpeed { get; set; }
            [JsonProperty(en ? "Bullet Damage" : "Урон пуль")] public float bulletDamage { get; set; }
            [JsonProperty(en ? "The distance to which the helicopter can move away from the sputnik" : "Дистанция, на которую вертолет может отдаляться от спутника")] public float distance { get; set; }
        }

        public class TurretConfig
        {
            [JsonProperty(en ? "Preset Name" : "Название пресета")] public string presetName { get; set; }
            [JsonProperty(en ? "Choose the spawn height automatically?" : "Выбирать высоту спавна автоматически?")] public bool autoHeight { get; set; }
            [JsonProperty(en ? "Health" : "Кол-во ХП")] public float hp { get; set; }
            [JsonProperty(en ? "Weapon ShortName" : "ShortName оружия")] public string shortNameWeapon { get; set; }
            [JsonProperty(en ? "Ammo ShortName" : "ShortName патронов")] public string shortNameAmmo { get; set; }
            [JsonProperty(en ? "Number of ammo" : "Кол-во патронов")] public int countAmmo { get; set; }
        }

        public class ZoneConfig
        {
            [JsonProperty(en ? "Create a PVP zone? (only for those who use the TruePVE plugin)[true/false]" : "Создавать зону PVP в зоне проведения ивента? (только для тех, кто использует плагин TruePVE) [true/false]")] public bool isCreateZonePVP { get; set; }
            [JsonProperty(en ? "Use the dome? [true/false]" : "Использовать ли купол? [true/false]")] public bool isDome { get; set; }
            [JsonProperty(en ? "Darkening the dome" : "Затемнение купола")] public int darkening { get; set; }
            [JsonProperty(en ? "Radius" : "Радиус")] public float radius { get; set; }
            [JsonProperty(en ? "Radiation power" : "Сила радиации")] public float radiation { get; set; }
        }

        public class GUIConfig
        {
            [JsonProperty(en ? "Use the Countdown GUI? [true/false]" : "Использовать ли GUI обратного отсчета? [true/false]")] public bool IsGUI { get; set; }
            [JsonProperty("AnchorMin")] public string AnchorMin { get; set; }
            [JsonProperty("AnchorMax")] public string AnchorMax { get; set; }
        }

        public class NotifyConfig
        {
            [JsonProperty(en ? "The time from the notification to the start of the event [sec]" : "Время от оповещения до начала ивента [sec]")] public int preStartTime { get; set; }
            [JsonProperty(en ? "Use a chat? [true/false]" : "Использовать ли чат? [true/false]")] public bool chat { get; set; }
            [JsonProperty(en ? "The time until the end of the event, when a message is displayed about the time until the end of the event [sec]" : "Время до конца ивента, когда выводится сообщение о сокром окончании ивента [sec]")] public HashSet<int> timeNotifications { get; set; }
        }

        public class SupportedPluginsConfig
        {
            [JsonProperty(en ? "PVE Mode Setting" : "Настройка PVE Mode")] public PveModeConfig pveMode { get; set; }
            [JsonProperty(en ? "Economy Setting" : "Настройка экономики")] public EconomyConfig economy { get; set; }
            [JsonProperty(en ? "GUI Announcements setting" : "Настройка GUI Announcements")] public GUIAnnouncementsConfig GUIAnnouncements { get; set; }
            [JsonProperty(en ? "Notify setting" : "Настройка Notify")] public NotifyPluginConfig notify { get; set; }
            [JsonProperty(en ? "DiscordMessages setting" : "Настройка DiscordMessages")] public DiscordConfig discord { get; set; }
            [JsonProperty(en ? "ZoneManager setting" : "Настройка ZoneManager")] public ZoneManagerConfig zoneManager { get; set; }
            [JsonProperty(en ? "RaidableBases setting" : "Настройка RaidableBases")] public RaidableBasesConfig raidableBases { get; set; }
        }

        public class PveModeConfig
        {
            [JsonProperty(en ? "Use the PVE mode of the plugin? [true/false]" : "Использовать PVE режим работы плагина? [true/false]")] public bool pve { get; set; }
            [JsonProperty(en ? "The amount of damage that the player has to do to become the Event Owner" : "Кол-во урона, которое должен нанести игрок, чтобы стать владельцем ивента")] public float damage { get; set; }
            [JsonProperty(en ? "Damage coefficients for calculate to become the Event Owner" : "Коэффициенты урона для подсчета, чтобы стать владельцем события")] public HashSet<ScaleDamageConfig> scaleDamage { get; set; }
            [JsonProperty(en ? "Can the non-owner of the event loot the crates? [true/false]" : "Может ли не владелец ивента грабить ящики? [true/false]")] public bool lootCrate { get; set; }
            [JsonProperty(en ? "Can the non-owner of the event hack locked crates? [true/false]" : "Может ли не владелец ивента взламывать заблокированные ящики? [true/false]")] public bool hackCrate { get; set; }
            [JsonProperty(en ? "Can the non-owner of the event loot NPC corpses? [true/false]" : "Может ли не владелец ивента грабить трупы NPC? [true/false]")] public bool lootNpc { get; set; }
            [JsonProperty(en ? "Can the non-owner of the event deal damage to the NPC? [true/false]" : "Может ли не владелец ивента наносить урон по NPC? [true/false]")] public bool damageNpc { get; set; }
            [JsonProperty(en ? "Can an Npc attack a non-owner of the event? [true/false]" : "Может ли Npc атаковать не владельца ивента? [true/false]")] public bool targetNpc { get; set; }
            [JsonProperty(en ? "Allow the non-owner of the event to enter the event zone? [true/false]" : "Разрешать входить внутрь зоны ивента не владельцу ивента? [true/false]")] public bool canEnter { get; set; }
            [JsonProperty(en ? "Allow a player who has an active cooldown of the Event Owner to enter the event zone? [true/false]" : "Разрешать входить внутрь зоны ивента игроку, у которого активен кулдаун на получение статуса владельца ивента? [true/false]")] public bool canEnterCooldownPlayer { get; set; }
            [JsonProperty(en ? "The time that the Event Owner may not be inside the event zone [sec.]" : "Время, которое владелец ивента может не находиться внутри зоны ивента [сек.]")] public int timeExitOwner { get; set; }
            [JsonProperty(en ? "The time until the end of Event Owner status when it is necessary to warn the player [sec.]" : "Время таймера до окончания действия статуса владельца ивента, когда необходимо предупредить игрока [сек.]")] public int alertTime { get; set; }
            [JsonProperty(en ? "Prevent the actions of the RestoreUponDeath plugin in the event zone? [true/false]" : "Запрещать работу плагина RestoreUponDeath в зоне действия ивента? [true/false]")] public bool restoreUponDeath { get; set; }
            [JsonProperty(en ? "The time that the player can`t become the Event Owner, after the end of the event and the player was its owner [sec.]" : "Время, которое игрок не сможет стать владельцем ивента, после того как ивент окончен и игрок был его владельцем [sec.]")] public double cooldownOwner { get; set; }
            [JsonProperty(en ? "Darkening the dome (0 - disables the dome)" : "Затемнение купола (0 - отключает купол)")] public int darkening { get; set; }
        }

        public class ScaleDamageConfig
        {
            [JsonProperty(en ? "Type of target" : "Тип цели")] public string Type { get; set; }
            [JsonProperty(en ? "Damage Multiplier" : "Множитель урона")] public float Scale { get; set; }
        }

        public class EconomyConfig
        {
            [JsonProperty(en ? "Enable economy" : "Включить экономику?")] public bool enable { get; set; }
            [JsonProperty(en ? "Which economy plugins do you want to use? (Economics, Server Rewards, IQEconomic)" : "Какие плагины экономики вы хотите использовать? (Economics, Server Rewards, IQEconomic)")] public HashSet<string> plugins { get; set; }
            [JsonProperty(en ? "The minimum value that a player must collect to get points for the economy" : "Минимальное значение, которое игрок должен заработать, чтобы получить баллы за экономику")] public double minEconomyPiont { get; set; }
            [JsonProperty(en ? "The minimum value that a winner must collect to make the commands work" : "Минимальное значение, которое победитель должен заработать, чтобы сработали команды")] public double minCommandPoint { get; set; }
            [JsonProperty(en ? "Looting of crates" : "Ограбление ящиков")] public Dictionary<string, double> crates { get; set; }
            [JsonProperty(en ? "Killing an NPC" : "Убийство NPC")] public double npcPoint { get; set; }
            [JsonProperty(en ? "Killing an Turret" : "Уничтожение Турели")] public double turretPoint { get; set; }
            [JsonProperty(en ? "Killing an Heli" : "Уничтожение Вертолета")] public double heliPoint { get; set; }
            [JsonProperty(en ? "Hacking a locked crate" : "Взлом заблокированного ящика")] public double lockedCratePoint { get; set; }
            [JsonProperty(en ? "List of commands that are executed in the console at the end of the event ({steamid} - the player who collected the highest number of points)" : "Список команд, которые выполняются в консоли по окончанию ивента ({steamid} - игрок, который набрал наибольшее кол-во баллов)")] public HashSet<string> commands { get; set; }
        }

        public class GUIAnnouncementsConfig
        {
            [JsonProperty(en ? "Do you use the GUI Announcements? [true/false]" : "Использовать ли GUI Announcements? [true/false]")] public bool isGUIAnnouncements { get; set; }
            [JsonProperty(en ? "Banner color" : "Цвет баннера")] public string bannerColor { get; set; }
            [JsonProperty(en ? "Text color" : "Цвет текста")] public string textColor { get; set; }
            [JsonProperty(en ? "Adjust Vertical Position" : "Отступ от верхнего края")] public float apiAdjustVPosition { get; set; }
        }

        public class NotifyPluginConfig
        {
            [JsonProperty(en ? "Do you use the Notify? [true/false]" : "Использовать ли Notify? [true/false]")] public bool isNotify { get; set; }
            [JsonProperty(en ? "Type" : "Тип")] public string type { get; set; }
        }

        public class DiscordConfig
        {
            [JsonProperty(en ? "Do you use the Discord? [true/false]" : "Использовать ли Discord? [true/false]")] public bool isDiscord { get; set; }
            [JsonProperty("Webhook URL")] public string webhookUrl { get; set; }
            [JsonProperty(en ? "Embed Color (DECIMAL)" : "Цвет полосы (DECIMAL)")] public int embedColor { get; set; }
            [JsonProperty(en ? "Keys of required messages" : "Ключи необходимых сообщений")] public HashSet<string> keys { get; set; }
        }

        public class ZoneManagerConfig
        {
            [JsonProperty(en ? "Do you use the ZoneManager? [true/false]" : "Использовать ли ZoneManager? [true/false]")] public bool enable { get; set; }
            [JsonProperty(en ? "List of zone flags that block spawn" : "Список флагов, при наличии в зоне которого спутник не будет спавниться")] public HashSet<string> blockFlags { get; set; }
        }

        public class RaidableBasesConfig
        {
            [JsonProperty(en ? "Do you use the RaidableBases? [true/false]" : "Использовать ли RaidableBases? [true/false]")] public bool enable { get; set; }
        }

        public class NpcWear
        {
            [JsonProperty(en ? "ShortName" : "ShortName")] public string shortName { get; set; }
            [JsonProperty(en ? "skinID (0 - default)" : "SkinID (0 - default)")] public ulong skinID { get; set; }
        }

        public class NpcBelt
        {
            [JsonProperty(en ? "ShortName" : "ShortName")] public string shortName { get; set; }
            [JsonProperty(en ? "Amount" : "Кол-во")] public int amount { get; set; }
            [JsonProperty(en ? "skinID (0 - default)" : "SkinID (0 - default)")] public ulong skinID { get; set; }
            [JsonProperty(en ? "Mods" : "Модификации на оружие")] public List<string> Mods { get; set; }
            [JsonProperty(en ? "Ammo" : "Патроны")] public string ammo { get; set; }
        }

        public class ColorConfig
        {
            [JsonProperty("r")] public float r { get; set; }
            [JsonProperty("g")] public float g { get; set; }
            [JsonProperty("b")] public float b { get; set; }
        }

        public class LocationConfig
        {
            [JsonProperty(en ? "Position" : "Позиция")] public string position { get; set; }
            [JsonProperty(en ? "Rotation" : "Вращение")] public string rotation { get; set; }
        }

        public class LootTableConfig
        {
            [JsonProperty(en ? "Minimum numbers of items" : "Минимальное кол-во элементов")] public int minAmount { get; set; }
            [JsonProperty(en ? "Maximum numbers of items" : "Максимальное кол-во элементов")] public int maxAmount { get; set; }
            [JsonProperty(en ? "List of items" : "Список предметов")] public List<ItemConfig> itemsConfig { get; set; }
        }

        public class ItemConfig
        {
            [JsonProperty("ShortName")] public string shortName { get; set; }
            [JsonProperty(en ? "Minimum" : "Минимальное кол-во")] public int minAmount { get; set; }
            [JsonProperty(en ? "Maximum" : "Максимальное кол-во")] public int maxAmount { get; set; }
            [JsonProperty(en ? "Chance [0.0-100.0]" : "Шанс выпадения предмета [0.0-100.0]")] public float chance { get; set; }
            [JsonProperty(en ? "Is this a blueprint? [true/false]" : "Это чертеж? [true/false]")] public bool isBluePrint { get; set; }
            [JsonProperty("SkinID (0 - default)")] public ulong skinID { get; set; }
            [JsonProperty(en ? "Name (empty - default)" : "Название (empty - default)")] public string name { get; set; }
        }

        public class CustomCardConfig
        {
            [JsonProperty("ShortName")] public string shortName { get; set; }
            [JsonProperty(en ? "Name (empty - default)" : "Название (empty - default)")] public string name { get; set; }
            [JsonProperty("SkinID (0 - default)")] public ulong skinID { get; set; }
            [JsonProperty(en ? "Multiplier of card health loss when using" : "Множитель потери прочности карты при использовании")] public float helthLossScale { get; set; }
            [JsonProperty(en ? "Enable spawn in crates" : "Включить спавн в ящиках")] public bool enableSpawnInDefaultCrates;
            [JsonProperty(en ? "Setting up spawn in crates (prefab - probability)" : "Настройка спавна в ящиках (префаб - вероятность)")] public Dictionary<string, float> spawnSetting { get; set; }
        }

        private class PluginConfig
        {
            [JsonProperty(en ? "Version" : "Версия")] public VersionNumber version { get; set; }
            [JsonProperty(en ? "Prefix of chat messages" : "Префикс в чате")] public string prefix { get; set; }
            [JsonProperty(en ? "Main Setting" : "Основные настройки")] public MainConfig mainConfig { get; set; }
            [JsonProperty(en ? "Settings of the falling sputnik" : "Настройка падения спутника")] public FallindConfig fallindConfig { get; set; }
            [JsonProperty(en ? "Settings of the spawning sputnik debris" : "Настройка спавна обломков спутника")] public SpawnConfig spawnConfig { get; set; }
            [JsonProperty(en ? "Event presets" : "Пресеты ивента")] public HashSet<EventConfig> eventConfigs { get; set; }
            [JsonProperty(en ? "Sputnik Debris Presets" : "Пресеты обломков спутников")] public HashSet<SputnikDebrisConfig> sputnikDebrisConfigs { get; set; }
            [JsonProperty(en ? "Space card setting" : "Настройка космической карты")] public CustomCardConfig customCardConfig { get; set; }
            [JsonProperty(en ? "Crate presets" : "Пресеты ящиков")] public HashSet<CrateConfig> crateConfigs { get; set; }
            [JsonProperty(en ? "NPC presets" : "Пресеты NPC")] public HashSet<NpcConfig> npcConfigs { get; set; }
            [JsonProperty(en ? "Heli presets" : "Пресеты вертолетов")] public HashSet<HeliConfig> heliConfigs { get; set; }
            [JsonProperty(en ? "Turrets presets" : "Пресеты турелей")] public HashSet<TurretConfig> turretConfigs { get; set; }
            [JsonProperty(en ? "Notification Settings" : "Настройки уведомлений")] public NotifyConfig notifyConfig { get; set; }
            [JsonProperty(en ? "GUI Setting" : "Настройки GUI")] public GUIConfig guiConfig { get; set; }
            [JsonProperty(en ? "Supported Plugins" : "Поддерживаемые плагины")] public SupportedPluginsConfig supportedPluginsConfig { get; set; }

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    version = new VersionNumber(1, 0, 8),
                    prefix = "[Sputnik]",
                    mainConfig = new MainConfig
                    {
                        isAutoEvent = true,
                        minTimeBetweenEvent = 7200,
                        maxTimeBetweenEvent = 7200,
                        destroyAfterLootingTime = 300
                    },
                    fallindConfig = new FallindConfig()
                    {
                        fallingSpeedScale = 1,
                        minFallHeight = 500,
                        maxFallHeight = 1000,
                        minFallOffset = 200,
                        maxFallOffset = 300,
                        countEffects = 10
                    },
                    spawnConfig = new SpawnConfig
                    {
                        countSpawnPoints = 25,
                        minDistance = 50,
                        maxDepth = 0.5f,
                        allowMonumentSpawn = true,
                        disabledMonuments = new HashSet<string>
                        {
                            "launch_site"
                        },
                        minTCDistance = 30
                    },
                    eventConfigs = new HashSet<EventConfig>
                    {
                        new EventConfig
                        {
                            presetName = "sputnik",
                            displayName = en ? "Sputnik" : "Спутник",
                            eventTime = 3600,
                            chance = 30,
                            fixedSputniksPresets = new List<string>
                            {
                                "sputnik_1",
                                "debris_3",
                            }
                        },
                        new EventConfig
                        {
                            presetName = "spaceship",
                            displayName = en ? "Spaceship" : "Космический корабль",
                            eventTime = 3600,
                            chance = 30,
                            fixedSputniksPresets = new List<string>
                            {
                                "sputnik_1",
                                "debris_1",
                                "debris_4",
                            }
                        },
                        new EventConfig
                        {
                            presetName = "station",
                            displayName = en ? "Fragment of the space station" : "Обломок космической станции",
                            eventTime = 3600,
                            chance = 20,
                            fixedSputniksPresets = new List<string>
                            {
                                "debris_2",
                                "debris_3",
                                "debris_4",
                            }
                        },
                        new EventConfig
                        {
                            presetName = "big_sputnik",
                            displayName = en ? "Huge sputnik" : "Огромный спутник",
                            eventTime = 3600,
                            chance = 20,
                            fixedSputniksPresets = new List<string>
                            {
                                "sputnik_1",
                                "debris_1",
                                "debris_3",
                                "debris_4",
                            }
                        }
                    },
                    sputnikDebrisConfigs = new HashSet<SputnikDebrisConfig>
                    {
                        new SputnikDebrisConfig
                        {
                            presetName = "sputnik_1",
                            locationPreset = "sputnik_1",
                            heliPresetName = "heli_1",
                            turrets = new Dictionary<string, HashSet<LocationConfig>>
                            {
                                ["turret_ak"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(-0.103, 0, 4.888)",
                                        rotation = "(0, 88, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-3.922, 0, 7.955)",
                                        rotation = "(0, 0, 0)"
                                    }
                                }
                            },
                            enableCardReader = true,
                            cardRaderLocation = new LocationConfig
                            {
                                position = "(-3.69, 0.055, 7.180)",
                                rotation = "(341.285, 38.870, 0.638)"
                            },
                            NPCs = new Dictionary<string, HashSet<string>>
                            {
                                ["Cosmonaut"] = new HashSet<string>
                                {
                                    "(4, 0, -1)",
                                    "(5, 0, 2.5)",
                                    "(1.5, 0, -1.5)",
                                    "(0.5, 0, 3.3)",
                                    "(-2.1, 0, 1.1)",
                                    "(-0.15, 0, 5.6)",
                                    "(-6.7, 0, 3.3)",
                                    "(-3.5, 0, 7.8)"
                                }
                            },
                            groundCrates = new Dictionary<string, HashSet<LocationConfig>>
                            {
                                ["crateelite_default"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(0.0, 0.0, 1.9)",
                                        rotation = "(0.0, 0.0, 0.0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-2.9, 0.0, 2.0)",
                                        rotation = "(0.0, 115.9, 0.0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(2.5, 0.0, -3.2)",
                                        rotation = "(0.0, 0.0, 0.0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-4.0, 0.0, -1.6)",
                                        rotation = "(0.0, 58.8, 0.0)"
                                    }
                                }
                            },
                            сrates = new Dictionary<string, HashSet<LocationConfig>>
                            {
                                ["chinooklockedcrate_spacecard"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(-3.279, 0.487, 6.231)",
                                        rotation = "(340.056, 38.203, 0)"
                                    }
                                },
                            },
                            mines = new List<string>
                            {
                                "(1.7, 0.0, 4.3)",
                                "(0.3, 0.0, 4.3)",
                                "(2.6, 0.0, 1.0)",
                                "(0.7, 0.0, 2.5)",
                                "(-7.2, 0.0, 6.6)",
                                "(-7.2, 0.0, 4.2)",
                                "(-0.9, 0.0, 6.1)",
                                "(-2.1, 0.0, 7.1)",
                                "(-3.6, 0.0, 8.2)",
                                "(-5.8, 0.0, 6.6)",
                                "(-1.7, 0.0, 0.5)",
                                "(-3.9, 0.0, 0.5)",
                                "(-4.4, 0.0, 2.3)",
                                "(-4.4, 0.0, 4.2)",
                                "(2.6, 0.0, -1.4)",
                                "(4.9, 0.0, -4.0)",
                                "(2.1, 0.0, -6.3)",
                                "(2.0, 0.0, -8.2)",
                                "(-1.7, 0.0, -1.4)",
                                "(-0.2, 0.0, -8.7)",
                                "(-3.4, 0.0, -8.7)",
                                "(-3.9, 0.0, -6.3)",
                                "(-5.4, 0.0, -2.6)",
                                "(-8.6, 0.0, -0.8)"
                            },
                            markerConfig = new MarkerConfig
                            {
                                enable = true,
                                displayName =  en ? "Sputnik" : "Спутник",
                                radius = 0.25f,
                                alpha = 0.6f,
                                color1 = new ColorConfig { r = 0.81f, g = 0.25f, b = 0.15f },
                                color2 = new ColorConfig { r = 0f, g = 0f, b = 0f }
                            },
                            zoneConfig = new ZoneConfig
                            {
                                isCreateZonePVP = false,
                                isDome = false,
                                darkening = 5,
                                radius = 25,
                                radiation = 10
                            },
                        },
                        new SputnikDebrisConfig
                        {
                            presetName = "debris_1",
                            locationPreset = "debris_1",
                            heliPresetName = "",
                            turrets = new Dictionary<string, HashSet<LocationConfig>>(),
                            enableCardReader = false,
                            cardRaderLocation = new LocationConfig
                            {
                                position = "(0, 0, 0)",
                                rotation = "(0, 0, 0)"
                            },
                            NPCs = new Dictionary<string, HashSet<string>>
                            {
                                ["Cosmonaut"] = new HashSet<string>
                                {
                                    "(0.9, 0.0, 3.6)",
                                    "(3.7, 0.0, -0.7)",
                                    "(-1.9, 0.0, -3.2)",
                                    "(-0.1, 0.0, -1.9)",
                                    "(-0.5, -12.1, -9.2)",
                                    "(-4.5, 0.0, -0.5)"
                                }
                            },
                            groundCrates = new Dictionary<string, HashSet<LocationConfig>>
                            {
                            },
                            сrates = new Dictionary<string, HashSet<LocationConfig>>
                            {
                                ["cratenormal_underwater_1"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(2.2, 0.0, 3.0)",
                                        rotation = "(0.0, 344.2, 0.0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-1.4, 0.0, -2.2)",
                                        rotation = "(0.0, 333.5, 0.0)"
                                    }
                                },
                                ["cratenormal_underwater_2"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(-0.4, 0.0, 0.2)",
                                        rotation = "(0.0, 0, 0.0)"
                                    }
                                }
                            },
                            mines = new List<string>
                            {
                                "(4.9, 0.0, 2.1)",
                                "(4.7, 0.0, 0.8)",
                                "(3.5, 0.0, 2.6)",
                                "(1.8, 0.0, 3.6)",
                                "(1.3, 0.0, 2.7)",
                                "(0.1, 0.0, 2.4)",
                                "(-1.8, 0.0, 3.2)",
                                "(-4.2, 0.0, 0.7)",
                                "(3.4, 0.0, -0.4)",
                                "(4.4, 0.0, -2.0)",
                                "(2.6, 0.0, -3.0)",
                                "(2.9, 0.0, -4.5)",
                                "(-5.4, 0.0, -2.5)",
                                "(-0.1, 0.0, -5.4)",
                                "(-2.7, 0.0, -4.6)",
                                "(-0.9, 0.0, -2.5)",

                            },
                            markerConfig = new MarkerConfig
                            {
                                enable = true,
                                displayName =  en ? "Radioactive space debris" : "Радиоактивные космические обломки",
                                radius = 0.2f,
                                alpha = 0.6f,
                                color1 = new ColorConfig { r = 0.81f, g = 0.25f, b = 0.15f },
                                color2 = new ColorConfig { r = 0f, g = 0f, b = 0f }
                            },
                            zoneConfig = new ZoneConfig
                            {
                                isCreateZonePVP = false,
                                isDome = false,
                                darkening = 5,
                                radius = 25,
                                radiation = 10
                            }
                        },
                        new SputnikDebrisConfig
                        {
                            presetName = "debris_2",
                            locationPreset = "debris_2",
                            heliPresetName = "",
                            turrets = new Dictionary<string, HashSet<LocationConfig>>(),
                            enableCardReader = false,
                            cardRaderLocation = new LocationConfig
                            {
                                position = "(0, 0, 0)",
                                rotation = "(0, 0, 0)"
                            },
                            NPCs = new Dictionary<string, HashSet<string>>
                            {
                                ["Cosmonaut"] = new HashSet<string>
                                {
                                    "(0, 0, -2)",
                                    "(-1, 0, 3)",
                                    "(3, 0, 0)",
                                }
                            },
                            groundCrates = new Dictionary<string, HashSet<LocationConfig>>
                            {
                                ["cratenormal_default"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(-2.2, 0, -1.8)",
                                        rotation = "(0, 50, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(0.75, 0, 2.25)",
                                        rotation = "(0, 50, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(3, 0, -2.25)",
                                        rotation = "(0, 304, 0)"
                                    }
                                },
                            },
                            сrates = new Dictionary<string, HashSet<LocationConfig>>
                            {
                            },
                            mines = new List<string>(),
                            markerConfig = new MarkerConfig
                            {
                                enable = true,
                                displayName =  en ? "Space debris" : "Космические обломки",
                                radius = 0.2f,
                                alpha = 0.6f,
                                color1 = new ColorConfig { r = 0.81f, g = 0.25f, b = 0.15f },
                                color2 = new ColorConfig { r = 0f, g = 0f, b = 0f }
                            },
                            zoneConfig = new ZoneConfig
                            {
                                isCreateZonePVP = false,
                                isDome = false,
                                darkening = 5,
                                radius = 25,
                                radiation = 0
                            }
                        },
                        new SputnikDebrisConfig
                        {
                            presetName = "debris_3",
                            locationPreset = "debris_3",
                            heliPresetName = "",
                            turrets = new Dictionary<string, HashSet<LocationConfig>>(),
                            enableCardReader = false,
                            cardRaderLocation = new LocationConfig
                            {
                                position = "(0, 0, 0)",
                                rotation = "(0, 0, 0)"
                            },
                            NPCs = new Dictionary<string, HashSet<string>>
                            {
                                ["Cosmonaut"] = new HashSet<string>
                                {
                                    "(-1.621, 0, 1.95)",
                                    "(1.08, 0, -2.04)",
                                    "(0.111, 0, 2.941)"
                                }
                            },
                            groundCrates = new Dictionary<string, HashSet<LocationConfig>>
                            {
                                ["cratenormal_default"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(0.13, 0, 1.9)",
                                        rotation = "(0, 50, 0)"
                                    }
                                },
                                ["crateelite_default"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(1.465, 0.0, -0.625)",
                                        rotation = "(0.0, 290, 0.0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-0.264, 0.0, -1.919)",
                                        rotation = "(0.0, 0, 0.0)"
                                    }
                                }
                            },
                            сrates = new Dictionary<string, HashSet<LocationConfig>>
                            {
                            },
                            mines = new List<string>(),
                            markerConfig = new MarkerConfig
                            {
                                enable = true,
                                displayName =  en ? "Space debris" : "Космические обломки",
                                radius = 0.2f,
                                alpha = 0.6f,
                                color1 = new ColorConfig { r = 0.81f, g = 0.25f, b = 0.15f },
                                color2 = new ColorConfig { r = 0f, g = 0f, b = 0f }
                            },
                            zoneConfig = new ZoneConfig
                            {
                                isCreateZonePVP = false,
                                isDome = false,
                                darkening = 5,
                                radius = 25,
                                radiation = 0
                            }
                        },
                        new SputnikDebrisConfig
                        {
                            presetName = "debris_4",
                            locationPreset = "debris_4",
                            heliPresetName = "",
                            turrets = new Dictionary<string, HashSet<LocationConfig>>(),
                            enableCardReader = false,
                            cardRaderLocation = new LocationConfig
                            {
                                position = "(0, 0, 0)",
                                rotation = "(0, 0, 0)"
                            },
                            NPCs = new Dictionary<string, HashSet<string>>
                            {
                                ["Cosmonaut"] = new HashSet<string>
                                {
                                    "(1.334, 0, 3.326",
                                    "(1.793, 0, -1.614)"
                                }
                            },
                            groundCrates = new Dictionary<string, HashSet<LocationConfig>>
                            {
                                ["cratenormal_default"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(2.481, 0, 2.517)",
                                        rotation = "(0, 25, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(0.911, 0, 2.207)",
                                        rotation = "(0, 320, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(2.186, 0, -0.275)",
                                        rotation = "(0, 347, 0)"
                                    }
                                },
                            },
                            сrates = new Dictionary<string, HashSet<LocationConfig>>
                            {
                            },
                            mines = new List<string>(),
                            markerConfig = new MarkerConfig
                            {
                                enable = true,
                                displayName =  en ? "Space debris" : "Космические обломки",
                                radius = 0.2f,
                                alpha = 0.6f,
                                color1 = new ColorConfig { r = 0.81f, g = 0.25f, b = 0.15f },
                                color2 = new ColorConfig { r = 0f, g = 0f, b = 0f }
                            },
                            zoneConfig = new ZoneConfig
                            {
                                isCreateZonePVP = false,
                                isDome = false,
                                darkening = 5,
                                radius = 25,
                                radiation = 0
                            }
                        }
                    },
                    customCardConfig = new CustomCardConfig
                    {
                        shortName = "keycard_green",
                        name = en ? "SPACE CARD" : "КОСМИЧЕСКАЯ КАРТА",
                        skinID = 2841475252,
                        helthLossScale = 1,
                        enableSpawnInDefaultCrates = false,
                        spawnSetting = new Dictionary<string, float>
                        {
                            ["assets/bundled/prefabs/radtown/crate_elite.prefab"] = 5f
                        }
                    },
                    crateConfigs = new HashSet<CrateConfig>
                    {
                        new CrateConfig
                        {
                            presetName = "chinooklockedcrate_spacecard",
                            prefab = "assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab",
                            needSpaceCard = true,
                            typeLootTable = 0,
                            crateUnlockTime = 0,
                            ownLootTable = new LootTableConfig
                            {
                                minAmount = 1,
                                maxAmount = 2,
                                itemsConfig = new List<ItemConfig>
                                {
                                    new ItemConfig
                                    {
                                        shortName = "scrap",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "wood",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    }
                                }
                            }
                        },
                        new CrateConfig
                        {
                            presetName = "chinooklockedcrate_default",
                            prefab = "assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab",
                            typeLootTable = 0,
                            crateUnlockTime = 0,
                            ownLootTable = new LootTableConfig
                            {
                                minAmount = 1,
                                maxAmount = 2,
                                itemsConfig = new List<ItemConfig>
                                {
                                    new ItemConfig
                                    {
                                        shortName = "scrap",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "wood",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    }
                                }
                            }
                        },
                        new CrateConfig
                        {
                            presetName = "crateelite_default",
                            prefab = "assets/bundled/prefabs/radtown/underwater_labs/crate_elite.prefab",
                            typeLootTable = 0,
                            crateUnlockTime = 0,
                            ownLootTable = new LootTableConfig
                            {
                                minAmount = 1,
                                maxAmount = 2,
                                itemsConfig = new List<ItemConfig>
                                {
                                    new ItemConfig
                                    {
                                        shortName = "scrap",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "wood",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    }
                                }
                            }
                        },
                        new CrateConfig
                        {
                            presetName = "cratenormal_underwater_1",
                            prefab = "assets/bundled/prefabs/radtown/underwater_labs/crate_normal.prefab",
                            typeLootTable = 0,
                            crateUnlockTime = 0,
                            ownLootTable = new LootTableConfig
                            {
                                minAmount = 1,
                                maxAmount = 2,
                                itemsConfig = new List<ItemConfig>
                                {
                                    new ItemConfig
                                    {
                                        shortName = "scrap",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "wood",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    }
                                }
                            }
                        },
                        new CrateConfig
                        {
                            presetName = "cratenormal_underwater_2",
                            prefab = "assets/bundled/prefabs/radtown/underwater_labs/crate_normal_2.prefab",
                            typeLootTable = 4,
                            crateUnlockTime = 0,
                            ownLootTable = new LootTableConfig
                            {
                                minAmount = 1,
                                maxAmount = 1,
                                itemsConfig = new List<ItemConfig>
                                {
                                    new ItemConfig
                                    {
                                        shortName = "keycard_green",
                                        minAmount = 1,
                                        maxAmount = 1,
                                        chance = 100f,
                                        isBluePrint = false,
                                        skinID = 2841475252,
                                        name = en ? "SPACE CARD" : "КОСМИЧЕСКАЯ КАРТА"
                                    }
                                }
                            }
                        },
                        new CrateConfig
                        {
                            presetName = "cratenormal_default",
                            prefab = "assets/bundled/prefabs/radtown/crate_normal.prefab",
                            typeLootTable = 0,
                            crateUnlockTime = 0,
                            ownLootTable = new LootTableConfig
                            {
                                minAmount = 1,
                                maxAmount = 2,
                                itemsConfig = new List<ItemConfig>
                                {
                                    new ItemConfig
                                    {
                                        shortName = "scrap",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "wood",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    }
                                }
                            }
                        }
                    },
                    npcConfigs = new HashSet<NpcConfig>
                    {
                        new NpcConfig
                        {
                            name = "Cosmonaut",
                            health = 200f,
                            wearItems = new List<NpcWear>
                            {
                                new NpcWear
                                {
                                    shortName = "hazmatsuit",
                                    skinID = 10180
                                }
                            },
                            beltItems = new List<NpcBelt>
                            {
                                new NpcBelt
                                {
                                    shortName = "rifle.lr300",
                                    amount = 1,
                                    skinID = 0,
                                    Mods = new List<string> { "weapon.mod.flashlight", "weapon.mod.holosight" },
                                    ammo = ""
                                },
                                new NpcBelt
                                {
                                    shortName = "syringe.medical",
                                    amount = 10,
                                    skinID = 0,
                                    Mods = new List<string> (),
                                    ammo = ""
                                },
                                new NpcBelt
                                {
                                    shortName = "grenade.f1",
                                    amount = 10,
                                    skinID = 0,
                                    Mods = new List<string> (),
                                    ammo = ""
                                }
                            },
                            kit = "",
                            deleteCorpse = true,
                            disableRadio = false,
                            roamRange = 5f,
                            chaseRange = 15,
                            attackRangeMultiplier = 1f,
                            senseRange = 100,
                            memoryDuration = 60f,
                            damageScale = 1f,
                            aimConeScale = 1f,
                            checkVisionCone = false,
                            visionCone = 135f,
                            speed = 7.5f,
                            typeLootTable = 0,
                            lootTable = new LootTableConfig
                            {
                                minAmount = 2,
                                maxAmount = 4,
                                itemsConfig = new List<ItemConfig>
                                {
                                    new ItemConfig
                                    {
                                        shortName = "rifle.semiauto",
                                        minAmount = 1,
                                        maxAmount = 1,
                                        chance = 0.09f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "shotgun.pump",
                                        minAmount = 1,
                                        maxAmount = 1,
                                        chance = 0.09f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "pistol.semiauto",
                                        minAmount = 1,
                                        maxAmount = 1,
                                        chance = 0.09f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                }
                            }
                        }
                    },
                    heliConfigs = new HashSet<HeliConfig>
                    {
                        new HeliConfig
                        {
                            presetName = "heli_1",
                            hp = 10000f,
                            cratesAmount = 3,
                            mainRotorHealth = 750f,
                            rearRotorHealth = 375f,
                            height = 50f,
                            bulletDamage = 20f,
                            bulletSpeed = 250f,
                            distance = 100f
                        }
                    },
                    turretConfigs = new HashSet<TurretConfig>
                    {
                        new TurretConfig
                        {
                            presetName = "turret_ak",
                            autoHeight = true,
                            hp = 250f,
                            shortNameWeapon = "rifle.ak",
                            shortNameAmmo = "ammo.rifle",
                            countAmmo = 200
                        },
                        new TurretConfig
                        {
                            presetName = "turret_m249",
                            autoHeight = false,
                            hp = 300f,
                            shortNameWeapon = "lmg.m249",
                            shortNameAmmo = "ammo.rifle",
                            countAmmo = 400
                        }
                    },
                    notifyConfig = new NotifyConfig
                    {
                        preStartTime = 10,
                        chat = true,
                        timeNotifications = new HashSet<int>
                        {
                            300,
                            60,
                            30,
                            5
                        }
                    },
                    guiConfig = new GUIConfig
                    {
                        IsGUI = true,
                        AnchorMin = "0 0.9",
                        AnchorMax = "1 0.95"
                    },
                    supportedPluginsConfig = new SupportedPluginsConfig
                    {
                        pveMode = new PveModeConfig
                        {
                            pve = false,
                            damage = 500f,
                            scaleDamage = new HashSet<ScaleDamageConfig>
                            {
                                new ScaleDamageConfig { Type = "NPC", Scale = 1f }
                            },
                            lootCrate = false,
                            hackCrate = false,
                            lootNpc = false,
                            damageNpc = false,
                            targetNpc = false,
                            canEnter = false,
                            canEnterCooldownPlayer = true,
                            timeExitOwner = 300,
                            alertTime = 60,
                            restoreUponDeath = true,
                            cooldownOwner = 86400,
                            darkening = 12
                        },
                        economy = new EconomyConfig
                        {
                            enable = false,
                            plugins = new HashSet<string> { "Economics", "Server Rewards", "IQEconomic" },
                            minCommandPoint = 0,
                            minEconomyPiont = 0,
                            crates = new Dictionary<string, double>
                            {
                                ["assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab"] = 0.4
                            },
                            npcPoint = 2,
                            lockedCratePoint = 5,
                            turretPoint = 2,
                            heliPoint = 5,
                            commands = new HashSet<string>()
                        },
                        GUIAnnouncements = new GUIAnnouncementsConfig
                        {
                            isGUIAnnouncements = false,
                            bannerColor = "Grey",
                            textColor = "White",
                            apiAdjustVPosition = 0.03f
                        },
                        notify = new NotifyPluginConfig
                        {
                            isNotify = false,
                            type = "0"
                        },
                        discord = new DiscordConfig
                        {
                            isDiscord = false,
                            webhookUrl = "https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks",
                            embedColor = 13516583,
                            keys = new HashSet<string>
                            {
                                "PreStartEvent",
                                "StartEvent",
                                "Crash"
                            }
                        },
                        zoneManager = new ZoneManagerConfig
                        {
                            enable = false,
                            blockFlags = new HashSet<string>
                            {
                                "eject",
                                "pvegod"
                            }
                        },
                        raidableBases = new RaidableBasesConfig
                        {

                        }
                    }
                };
            }
        }
        #endregion Config
    }
}

namespace Oxide.Plugins.SputnikExtensionMethods
{
    public static class ExtensionMethods
    {
        public static bool Any<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) return true;
            return false;
        }

        public static HashSet<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            HashSet<TSource> result = new HashSet<TSource>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) result.Add(enumerator.Current);
            return result;
        }

        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) return enumerator.Current;
            return default(TSource);
        }

        public static HashSet<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> predicate)
        {
            HashSet<TResult> result = new HashSet<TResult>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) result.Add(predicate(enumerator.Current));
            return result;
        }

        public static List<TResult> Select<TSource, TResult>(this IList<TSource> source, Func<TSource, TResult> predicate)
        {
            List<TResult> result = new List<TResult>();
            for (int i = 0; i < source.Count; i++)
            {
                TSource element = source[i];
                result.Add(predicate(element));
            }
            return result;
        }

        public static HashSet<T> OfType<T>(this IEnumerable<BaseNetworkable> source)
        {
            HashSet<T> result = new HashSet<T>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (enumerator.Current is T) result.Add((T)(object)enumerator.Current);
            return result;
        }

        public static bool IsExists(this BaseNetworkable entity) => entity != null && !entity.IsDestroyed;

        public static void ClearItemsContainer(this ItemContainer container)
        {
            for (int i = container.itemList.Count - 1; i >= 0; i--)
            {
                Item item = container.itemList[i];
                item.RemoveFromContainer();
                item.Remove();
            }
        }

        public static bool IsRealPlayer(this BasePlayer player) => player != null && player.userID.IsSteamId();

        public static List<TSource> OrderBy<TSource>(this IEnumerable<TSource> source, Func<TSource, float> predicate)
        {
            List<TSource> result = source.ToList();
            for (int i = 0; i < result.Count; i++)
            {
                for (int j = 0; j < result.Count - 1; j++)
                {
                    if (predicate(result[j]) > predicate(result[j + 1]))
                    {
                        TSource z = result[j];
                        result[j] = result[j + 1];
                        result[j + 1] = z;
                    }
                }
            }
            return result;
        }

        public static List<TSource> ToList<TSource>(this IEnumerable<TSource> source)
        {
            List<TSource> result = new List<TSource>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) result.Add(enumerator.Current);
            return result;
        }

        public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source)
        {
            HashSet<TSource> result = new HashSet<TSource>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) result.Add(enumerator.Current);
            return result;
        }

        public static TSource Max<TSource>(this IEnumerable<TSource> source, Func<TSource, float> predicate)
        {
            TSource result = source.ElementAt(0);
            float resultValue = predicate(result);
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    TSource element = enumerator.Current;
                    float elementValue = predicate(element);
                    if (elementValue > resultValue)
                    {
                        result = element;
                        resultValue = elementValue;
                    }
                }
            }
            return result;
        }

        public static TSource ElementAt<TSource>(this IEnumerable<TSource> source, int index)
        {
            int movements = 0;
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (movements == index) return enumerator.Current;
                    movements++;
                }
            }
            return default(TSource);
        }
    }
}