// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Facepunch;

#region Changelogs and ToDo
/**********************************************************************
* 
* 1.0.0 :       - Initial release
* 1.0.1 :       - Coding Optimisations
*               - Added messaging on refunding items
* 1.0.2 :       - More Optimisations added/changed
* 1.0.3 :       - Fixed switched counts
*               - Added Prefix and Chaticon to cfg
* 1.0.4 :       - Possible Nre Fix
*               - Added correct ResourceId
* 1.0.5 :       - Excempt of renaming softcore respawn bags
* 1.0.6 :       - Added permission NOCD_Perm
* 1.0.7 :       - Patches done by : Solarix
* 1.0.8 :       - Fixes for pickup and messaging (Patched by BaluDog)
* 1.0.9 :       - Set timer on placement (ignore visual maptimer)
*               - Added command /bag check {playername or id}
*               - Added command /bag teleport {bagid}
*               - Added command /bag remove {bagid}
*               - Added command /bag purge {player}
*               - Added check when giving bags/beds to steamfriends
*               - Updated language file
* 1.0.10 :      - Fixed issue with sleepingbag removal through Map interface
*                              
**********************************************************************/
#endregion

namespace Oxide.Plugins
{
    [Info("Better Beds", "rustmods.ru", "1.0.10", ResourceId = 531)]
    [Description("Edit various settings on sleepingbags and beds")]

    class BetterBeds : RustPlugin
    {

        #region Variables

        const string RestrictDef_Perm = "betterbeds.restrictdefault";
        const string RestrictVip_Perm = "betterbeds.restrictvip";
        const string RenameBlock_Perm = "betterbeds.renameblock";
        const string Chat_Perm = "betterbeds.chat";
        const string Admin_Perm = "betterbeds.admin";
        const string NOCD_Perm = "betterbeds.nocd";
        const string DenyPickup_Perm = "betterbeds.denypickup";
        ulong chaticon = 0;
        string prefix;
        bool Debug = false;

        Dictionary<ulong, HashSet<ulong>> BagIDs = new Dictionary<ulong, HashSet<ulong>>();
        Dictionary<ulong, HashSet<ulong>> BedIDs = new Dictionary<ulong, HashSet<ulong>>();


        private int BagCount(BasePlayer player) => BagIDs.TryGetValue(player.userID, out var bags) ? bags.Count : 0;
        private int BedCount(BasePlayer player) => BedIDs.TryGetValue(player.userID, out var beds) ? beds.Count : 0;



        #endregion

        #region Configuration
        void Init()
        {
            if (!LoadConfigVariables())
            {
                Puts("Config file issue detected. Please delete file, or check syntax and fix.");
                return;
            }
            permission.RegisterPermission(Admin_Perm, this);
            permission.RegisterPermission(NOCD_Perm, this);
            permission.RegisterPermission(Chat_Perm, this);
            permission.RegisterPermission(RestrictDef_Perm, this);
            permission.RegisterPermission(RestrictVip_Perm, this);
            permission.RegisterPermission(RenameBlock_Perm, this);
            permission.RegisterPermission(DenyPickup_Perm, this);

            Debug = configData.PlugCFG.Debug;
            prefix = configData.PlugCFG.Prefix;
            chaticon = configData.PlugCFG.Chaticon;
            if (Debug) Puts($"[Debug] trigger for Debug is true");
        }

        private ConfigData configData;

        class ConfigData
        {
            [JsonProperty(PropertyName = "Settings Plugin")]
            public SettingsPlugin PlugCFG = new SettingsPlugin();
            [JsonProperty(PropertyName = "Settings Global")]
            public SettingsGlobal Global = new SettingsGlobal();
            [JsonProperty(PropertyName = "Settings Bags")]
            public SettingsBags SpawnBags = new SettingsBags();
            [JsonProperty(PropertyName = "Settings Beds")]
            public SettingsBeds SpawnBeds = new SettingsBeds();
        }

        class SettingsPlugin
        {
            [JsonProperty(PropertyName = "Debug")]
            public bool Debug = false;
            [JsonProperty(PropertyName = "Chat Steam64ID")]
            public ulong Chaticon = 0;
            [JsonProperty(PropertyName = "Chat Prefix")]
            public string Prefix = "[<color=yellow>Better Beds</color>] ";
        }

        class SettingsGlobal
        {
            [JsonProperty(PropertyName = "Bag cooldown")]
            public float BagCooldown = 30f;
            [JsonProperty(PropertyName = "Bed cooldown")]
            public float BedCooldown = 20f;
            [JsonProperty(PropertyName = "Only 1x rename per placement")]
            public bool OneRename = false;
            [JsonProperty(PropertyName = "NO bed/sleepingbag cooldown")]
            public bool NoBedCooldown = false;
        }

        class SettingsBags
        {
            [JsonProperty(PropertyName = "Refund Sleepingbags")]
            public bool RefundBags = true;
            [JsonProperty(PropertyName = "Max placements Default")]
            public int LimitBagsAmountDef = 5;
            [JsonProperty(PropertyName = "Max placements Vip")]
            public int LimitBagsAmountVip = 10;
        }

        class SettingsBeds
        {
            [JsonProperty(PropertyName = "Refund Beds")]
            public bool RefundBeds = true;
            [JsonProperty(PropertyName = "Max placements Default")]
            public int LimitBedsAmountDef = 1;
            [JsonProperty(PropertyName = "Max placements Vip")]
            public int LimitBedsAmountVip = 3;
        }


        private bool LoadConfigVariables()
        {
            try
            {
                configData = Config.ReadObject<ConfigData>();
            }
            catch
            {
                return false;
            }
            SaveConf();
            return true;
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Fresh install detected Creating a new config file.");
            configData = new ConfigData();
            SaveConf();
        }

        void SaveConf() => Config.WriteObject(configData, true);
        #endregion

        #region LanguageAPI
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string , string>
            {
                ["InvalidInput"] = "<color=red>Please enter a valid command!</color>" ,
                ["BagText"] = "Welcome to our server" ,
                ["Version"] = "Version : V" ,
                ["LimitBags"] = "You have been limited to {0} sleeping bag(s)" ,
                ["LimitBeds"] = "You have been limited to {0} bed(s)" ,
                ["MaxLimitDefault"] = "You have already placed the limit of {0} for a player" ,
                ["MaxLimitVip"] = "You have already placed the limit of {0} for VIPs" ,
                ["LimitHeader"] = "Your Restrictions and Placements:" ,
                ["Info"] = "\n<color=green>Available Commands</color>\n<color=green>/bag info</color> : Shows info on version/author and commands" ,
                ["InfoMyLimit"] = "\n<color=green>/bag mylimit</color> : Lists your restriction heights and placements" ,
                ["InventoryFull"] = "<color=red>You had no inventory space; no item was refunded!</color>" ,
                ["InventoryNotFull"] = "<color=green>Your item has been refunded!</color>" ,
                ["NoPermission"] = "<color=red>You do not have permission to use that command!</color>" ,
                ["RenameBlock"] = "<color=red>Renaming is blocked on this server</color>" ,
                ["RenameBlock2nd"] = "<color=red>Max 1 rename allowed on this server</color>" ,
                ["BagsUsed"] = "You have placed {0}" ,
                ["BagsLeft"] = "You have {0} placement(s) left" ,
                ["RestrictedLimit"] = "You are restricted to placing only 1 sleeping bag and 1 bed." ,
                ["TargetPlayerMaxLimit"] =  "The player {0} has reached their maximum number of beds/bags.",
                ["BagCheckHeader"] = "Bag/Bed count for player {0}:" ,
                ["BagCount"] = "Bags: {0}" ,
                ["BedCount"] = "Beds: {0}" ,
                ["PlayerNotFound"] = "Player {0} not found." ,
                ["InvalidInputCommand"] = "Invalid input. Please use '/bag check playerName'." ,
                ["InvalidBagID"] = "Invalid bag ID format. Please provide a valid ID." ,
                ["BagNotFound"] = "Bag with ID {0} not found." ,
                ["BagRemoved"] = "Bag with ID {0} has been removed." ,
                ["TeleportedToBag"] = "You have been teleported to the bag with ID {0}." ,
                ["PurgeSuccess"] = "All bags and beds for player {0} have been removed." ,
                ["PurgePlayerNotFound"] = "Player {0} not found. No bags were purged.",
                ["BagIds"] = "Bag IDs: {0}" ,
                ["BedIds"] = "Bed IDs: {0}"
            } , this);
        }

        #endregion

        #region Commands

        [ChatCommand("Bag")]
        private void cmdBags(BasePlayer player , string command , string[] args)
        {
            if (args.Length < 1)
            {
                Player.Message(player , prefix + msg("InvalidInputCommand") , chaticon);
                return;
            }

            switch (args[0].ToLower())
            {
                case "info":
                    Player.Message(player , prefix + string.Format(msg("Version" , player.UserIDString) , this.Version.ToString())
                        + " By : " + this.Author.ToString() + "\n"
                        + msg("Info") + "\n"
                        + msg("InfoMyLimit") , chaticon);
                    return;

                case "mylimit":
                    if (permission.UserHasPermission(player.UserIDString , Chat_Perm))
                    {
                        int limitAmountBag = 1;
                        int limitAmountBed = 1;

                        if (permission.UserHasPermission(player.UserIDString , RestrictVip_Perm))
                        {
                            limitAmountBag = configData.SpawnBags.LimitBagsAmountVip;
                            limitAmountBed = configData.SpawnBeds.LimitBedsAmountVip;
                        }
                        else if (permission.UserHasPermission(player.UserIDString , RestrictDef_Perm))
                        {
                            limitAmountBag = configData.SpawnBags.LimitBagsAmountDef;
                            limitAmountBed = configData.SpawnBeds.LimitBedsAmountDef;
                        }

                        RecalculateCounts(); // Force recalculation before displaying

                        Player.Message(player , prefix
                            + "<color=green>" + string.Format(msg("LimitHeader" , player.UserIDString)) + "</color>\n"
                            + string.Format(msg("LimitBags" , player.UserIDString) , limitAmountBag.ToString()) + "\n"
                            + string.Format(msg("BagsUsed" , player.UserIDString) , BagCount(player)) + "\n"
                            + string.Format(msg("LimitBeds" , player.UserIDString) , limitAmountBed.ToString()) + "\n"
                            + string.Format(msg("BedsUsed" , player.UserIDString) , BedCount(player)) , chaticon);
                        return;
                    }
                    else
                    {
                        Player.Message(player , prefix + string.Format(msg("NoPermission" , player.UserIDString)) , chaticon);
                    }
                    return;

                case "check":
                    if (!permission.UserHasPermission(player.UserIDString , Admin_Perm))
                    {
                        Player.Message(player , prefix + msg("NoPermission" , player.UserIDString) , chaticon);
                        return;
                    }

                    if (args.Length < 2)
                    {
                        Player.Message(player , prefix + msg("InvalidInputCommand") , chaticon);
                        return;
                    }

                    BasePlayer targetPlayer = FindPlayerByNameOrID(args[1]);
                    if (targetPlayer == null)
                    {
                        Player.Message(player , prefix + string.Format(msg("PlayerNotFound" , player.UserIDString) , args[1]) , chaticon);
                        return;
                    }

                    RecalculateCounts();

                    int targetBagCount = BagCount(targetPlayer);
                    int targetBedCount = BedCount(targetPlayer);

                    var bagIds = BagIDs.ContainsKey(targetPlayer.userID) ? BagIDs[targetPlayer.userID] : new HashSet<ulong>();
                    var bedIds = BedIDs.ContainsKey(targetPlayer.userID) ? BedIDs[targetPlayer.userID] : new HashSet<ulong>();

                    string bagIdList = bagIds.Count > 0 ? string.Join(", " , bagIds) : "None";
                    string bedIdList = bedIds.Count > 0 ? string.Join(", " , bedIds) : "None";

                    Player.Message(player , prefix
                        + msg("BagCheckHeader").Replace("{0}" , targetPlayer.displayName) + "\n"
                        + msg("BagCount").Replace("{0}" , targetBagCount.ToString()) + "\n"
                        + msg("BedCount").Replace("{0}" , targetBedCount.ToString()) + "\n"
                        + msg("BagIds").Replace("{0}" , bagIdList) + "\n"
                        + msg("BedIds").Replace("{0}" , bedIdList) + "\n"
                        , chaticon);

                    return;

                case "teleport":
                    if (args.Length < 2)
                    {
                        Player.Message(player , prefix + msg("InvalidInputCommand") , chaticon);
                        return;
                    }

                    if (!ulong.TryParse(args[1] , out ulong teleportBagID))
                    {
                        Player.Message(player , prefix + msg("InvalidBagID") , chaticon);
                        return;
                    }

                    var teleportBagEntity = BaseNetworkable.serverEntities.OfType<SleepingBag>()
                        .FirstOrDefault(bag => bag.net.ID.Value == teleportBagID);

                    if (teleportBagEntity == null)
                    {
                        Player.Message(player , prefix + msg("BagNotFound") , chaticon);
                        return;
                    }

                    player.transform.position = teleportBagEntity.transform.position + new Vector3(0 , 1f , 0); // Adjust height if necessary
                    Player.Message(player , prefix + string.Format(msg("TeleportedToBag" , player.UserIDString) , teleportBagID.ToString()) , chaticon);
                    return;

                case "remove":
                    if (args.Length < 2)
                    {
                        Player.Message(player , prefix + msg("InvalidInputCommand") , chaticon);
                        return;
                    }

                    if (!ulong.TryParse(args[1] , out ulong removeBagID))
                    {
                        Player.Message(player , prefix + msg("InvalidBagID") , chaticon);
                        return;
                    }

                    var removeBagEntity = BaseNetworkable.serverEntities.OfType<SleepingBag>()
                        .FirstOrDefault(bag => bag.net.ID.Value == removeBagID);

                    if (removeBagEntity == null)
                    {
                        Player.Message(player , prefix + msg("BagNotFound") , chaticon);
                        return;
                    }

                    if (removeBagEntity.OwnerID != player.userID && !permission.UserHasPermission(player.UserIDString , Admin_Perm))
                    {
                        Player.Message(player , prefix + msg("NoPermission") , chaticon);
                        return;
                    }

                    removeBagEntity.Kill();

                    Player.Message(player , prefix + string.Format(msg("BagRemoved" , player.UserIDString) , removeBagID.ToString()) , chaticon);
                    return;

                case "purge":
                    if (args.Length < 2)
                    {
                        Player.Message(player , prefix + msg("InvalidInputCommand") , chaticon);
                        return;
                    }

                    BasePlayer purgePlayer = FindPlayerByNameOrID(args[1]);
                    if (purgePlayer == null)
                    {
                        Player.Message(player , prefix + string.Format(msg("PurgePlayerNotFound") , args[1]) , chaticon);
                        return;
                    }

                    RecalculateCounts();

                    var bagsToRemove = BagIDs.ContainsKey(purgePlayer.userID) ? BagIDs[purgePlayer.userID] : new HashSet<ulong>();

                    var bagsToRemoveCopy = new HashSet<ulong>(bagsToRemove);

                    foreach (var bagIDToRemove in bagsToRemoveCopy)
                    {
                        var bagEntityToRemove = BaseNetworkable.serverEntities.OfType<SleepingBag>()
                            .FirstOrDefault(bag => bag.net.ID.Value == bagIDToRemove);

                        if (bagEntityToRemove != null)
                        {
                            bagEntityToRemove.Kill();
                        }
                    }

                    Player.Message(player , prefix + string.Format(msg("PurgeSuccess") , purgePlayer.displayName) , chaticon);
                    return;

                default:
                    Player.Message(player , prefix + msg("InvalidInputCommand") , chaticon);
                    break;
            }
        }

        #endregion

        #region Hooks

        object CanAssignBed(BasePlayer player , SleepingBag bag , ulong targetPlayerId)
        {
            int limit;
            bool isRestricted = true;

            if (permission.UserHasPermission(player.UserIDString , RestrictVip_Perm))
            {
                limit = bag.ShortPrefabName.Contains("bed") ? configData.SpawnBeds.LimitBedsAmountVip : configData.SpawnBags.LimitBagsAmountVip;
                isRestricted = false;
            }
            else if (permission.UserHasPermission(player.UserIDString , RestrictDef_Perm))
            {
                limit = bag.ShortPrefabName.Contains("bed") ? configData.SpawnBeds.LimitBedsAmountDef : configData.SpawnBags.LimitBagsAmountDef;
                isRestricted = false;
            }
            else
            {
                limit = 1;
            }

            var entityDict = bag.ShortPrefabName.Contains("bed") ? BedIDs : BagIDs;

            if (!entityDict.TryGetValue(targetPlayerId , out var targetEntities))
            {
                targetEntities = new HashSet<ulong>();
                entityDict[targetPlayerId] = targetEntities;
            }

            if (targetEntities.Count >= limit)
            {
                var targetPlayer = BasePlayer.FindByID(targetPlayerId);

                if (targetPlayer != null)
                {
                    Player.Message(player , prefix + string.Format(msg("TargetPlayerMaxLimit" , targetPlayer.displayName)) , chaticon);
                }

                return false;
            }

            RemoveEntityFromPlayer(player , bag);

            targetEntities.Add(bag.net.ID.Value);

            bag.OwnerID = targetPlayerId;
            bag.SendNetworkUpdate();

            return null;
        }

        void OnServerInitialized()
        {
            foreach (var bag in BaseNetworkable.serverEntities.OfType<SleepingBag>())
            {
                if (BedIDs.ContainsKey(bag.OwnerID) && BedIDs[bag.OwnerID].Contains(bag.net.ID.Value) ||
                    BagIDs.ContainsKey(bag.OwnerID) && BagIDs[bag.OwnerID].Contains(bag.net.ID.Value))
                    continue;

                if (bag.ShortPrefabName.Contains("bed"))
                    {
                        if (!BedIDs.ContainsKey(bag.OwnerID))
                            BedIDs[bag.OwnerID] = new HashSet<ulong>();
                        BedIDs[bag.OwnerID].Add(bag.net.ID.Value);
                    }
                else
                {
                    if (!BagIDs.ContainsKey(bag.OwnerID))
                        BagIDs[bag.OwnerID] = new HashSet<ulong>();
                    BagIDs[bag.OwnerID].Add(bag.net.ID.Value);
                }
            }
            RecalculateCounts();
            timer.Every(30f, RecalculateCounts); // Recalculate every 1 minutes
        }

        void OnEntityKill(SleepingBag bag)
        {
            if (bag.ShortPrefabName.Contains("bed"))
            {
                if (BedIDs.TryGetValue(bag.OwnerID, out var beds))
                    beds.Remove(bag.net.ID.Value);
            }
            else if (BagIDs.TryGetValue(bag.OwnerID, out var bags))
                bags.Remove(bag.net.ID.Value);

            RecalculateCounts();
        }

        void OnSleepingBagDestroyed(SleepingBag sleepingBag , ulong userID)
        {
            if (sleepingBag == null || userID == 0) return;

            if (sleepingBag.ShortPrefabName.Contains("bed"))
            {
                if (BedIDs.TryGetValue(userID , out var beds))
                {
                    beds.Remove(sleepingBag.net.ID.Value);
                    if (Debug) Puts($"Bed {sleepingBag.net.ID} removed from player's bed list.");
                }

                sleepingBag.Kill();
            }
            else
            {
                if (BagIDs.TryGetValue(userID , out var bags))
                {
                    bags.Remove(sleepingBag.net.ID.Value);
                    if (Debug) Puts($"Sleeping bag {sleepingBag.net.ID} removed from player's bag list.");
                }

                sleepingBag.Kill();
            }

            RecalculateCounts();
        }

        void OnEntityPickup(Item item, BasePlayer player)
        {
            if (item.info.shortname.Contains("sleepingbag") || item.info.shortname.Contains("bed") || item.info.shortname.Contains("beachtowel"))
            {
                BaseEntity entity = item.GetHeldEntity();
                if (entity != null)
                {
                    ulong entityId = entity.net.ID.Value;
                    if (item.info.shortname.Contains("bed"))
                    {
                        if (BedIDs.TryGetValue(player.userID, out var beds))
                        {
                            beds.Remove(entityId);
                        }
                    }
                    else
                    {
                        if (BagIDs.TryGetValue(player.userID, out var bags))
                        {
                            bags.Remove(entityId);
                        }
                    }
                }
            }

            RecalculateCounts();
        }

        private object CanRenameBed(BasePlayer player, SleepingBag bag)
        {

            if (bag == null || bag.OwnerID == 0 || player == null) return true;
            if (permission.UserHasPermission(player.UserIDString, Admin_Perm)) return null;

            if (bag.niceName != (lang.GetMessage("BagText", this)) && configData.Global.OneRename)
            {
                Player.Message(player, prefix + string.Format(msg("RenameBlock2nd", player.UserIDString)), chaticon);
                if (Debug) Puts($"[Debug] {player} attempted to rename a bag/bed but it is allready renamed once");
                return true;
            }

            if (bag.OwnerID != player.userID || permission.UserHasPermission(player.UserIDString, RenameBlock_Perm))
            {
                Player.Message(player, prefix + string.Format(msg("RenameBlock", player.UserIDString)), chaticon);
                if (Debug) Puts($"[Debug] {player} attempted to rename a bag/bed but had no permission");
                return true;
            }

            return null;
        }

        private void OnEntitySpawned(SleepingBag bag)
        {
            BasePlayer player = BasePlayer.FindByID(bag.OwnerID);
            if (player == null) return;

            int limit;
            Dictionary<ulong, HashSet<ulong>> entityDict;
            bool isRestricted = true;

            if (permission.UserHasPermission(player.UserIDString, RestrictVip_Perm))
            {
                limit = bag.ShortPrefabName.Contains("bed") ? configData.SpawnBeds.LimitBedsAmountVip : configData.SpawnBags.LimitBagsAmountVip;
                isRestricted = false;
            }
            else if (permission.UserHasPermission(player.UserIDString, RestrictDef_Perm))
            {
                limit = bag.ShortPrefabName.Contains("bed") ? configData.SpawnBeds.LimitBedsAmountDef : configData.SpawnBags.LimitBagsAmountDef;
                isRestricted = false;
            }
            else
            {
                limit = 1;
            }

            entityDict = bag.ShortPrefabName.Contains("bed") ? BedIDs : BagIDs;

            if (!entityDict.TryGetValue(player.userID, out var entities))
            {
                entities = new HashSet<ulong>();
                entityDict[player.userID] = entities;
            }

            if (entities.Count >= limit)
            {
                NextTick(() => 
                {
                    if (bag.ShortPrefabName.Contains("bed"))
                        RefundBed(bag, player);
                    else
                        RefundBag(bag, player);
                });

                if (isRestricted)
                {
                    Player.Message(player, prefix + string.Format(msg("RestrictedLimit", player.UserIDString)), chaticon);
                }
            }
            else
            {
                entities.Add(bag.net.ID.Value);
            }

            RecalculateCounts();
        }

        private object CanPickupEntity(BasePlayer player, BaseEntity entity)
        {
            if (entity.ShortPrefabName.Contains("sleepingbag") || entity.ShortPrefabName.Contains("bed") || entity.ShortPrefabName.Contains("beachtowel"))
            {
                if (permission.UserHasPermission(player.UserIDString, Admin_Perm)) return true;
                if (permission.UserHasPermission(player.UserIDString, DenyPickup_Perm)
)
                {
                    Player.Message(player, prefix + string.Format(msg("pickup is disabled", player.UserIDString)), chaticon);
                    if (Debug) Puts($"[Debug] {player} attempted to pickup a bag/bed but this is disabled");

                    return false;
                }
                return true;
            }
            return null;
        }

        private void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (!player) return;
            if (player.IsSleeping() == true || player.IsConnected == false) return;

            BagCooldownSet(player);
        }

        #endregion

        #region Methods

        private BasePlayer FindPlayerByNameOrID(string nameOrID)
        {
            BasePlayer targetPlayer = BasePlayer.Find(nameOrID);
            if (targetPlayer != null)
                return targetPlayer;

            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.displayName.ToLower().Contains(nameOrID.ToLower()))
                    return player;
            }

            foreach (var sleeper in BasePlayer.sleepingPlayerList)
            {
                if (sleeper.displayName.ToLower().Contains(nameOrID.ToLower()))
                    return sleeper;
            }

            return null;
        }

        void RemoveEntityFromPlayer(BasePlayer player , SleepingBag bag)
        {
            Dictionary<ulong , HashSet<ulong>> entityDict;
            if (bag.ShortPrefabName.Contains("bed"))
            {
                entityDict = BedIDs;
            }
            else
            {
                entityDict = BagIDs;
            }

            if (entityDict.TryGetValue(player.userID , out var entities))
            {
                entities.Remove(bag.net.ID.Value);

                if (entities.Count == 0)
                {
                    entityDict.Remove(player.userID);
                }
            }
        }

        private void BagCooldownSet(BasePlayer player)
        {
            if (!IsValidPlayer(player)) return;

            bool hasNoCooldownPermission = permission.UserHasPermission(player.UserIDString , NOCD_Perm);
            bool hasAdminPermission = permission.UserHasPermission(player.UserIDString , Admin_Perm);

            // Check permissions only once and handle global cooldown override
            bool shouldSetNoCooldown = configData.Global.NoBedCooldown && (hasAdminPermission || hasNoCooldownPermission);

            SleepingBag[] bags = SleepingBag.FindForPlayer(player.userID , true);

            foreach (SleepingBag bag in bags)
            {
                SetCooldownForBag(bag , player , shouldSetNoCooldown);
            }
        }

        private bool IsValidPlayer(BasePlayer player)
        {
            if (player == null || player.IsSleeping() || !player.IsConnected)
                return false;

            return true;
        }

        private void SetCooldownForBag(SleepingBag bag , BasePlayer player , bool noCooldown)
        {
            string prefabName = bag.ShortPrefabName;

            if (noCooldown)
            {
                bag.secondsBetweenReuses = 0;
                bag.unlockTime = 0;
                DebugLog($"No cooldown set for [{prefabName}] belonging to player [{player.displayName}]");
                return;
            }

            switch (prefabName)
            {
                case string name when name.Equals("bed") || name.Equals("beachtowel"):
                    bag.secondsBetweenReuses = configData.Global.BedCooldown;
                    bag.unlockTime = configData.Global.BedCooldown;
                    DebugLog($"Set bed or beachtowel cooldown to {configData.Global.BedCooldown} seconds for [{player.displayName}]");
                    break;

                case string name when name.Equals("sleepingbag"):
                    bag.secondsBetweenReuses = configData.Global.BagCooldown;
                    bag.unlockTime = configData.Global.BagCooldown;
                    DebugLog($"Set sleeping bag cooldown to {configData.Global.BagCooldown} seconds for [{player.displayName}]");
                    break;

                default:
                    DebugLog($"Unknown bag type [{prefabName}] for player [{player.displayName}]");
                    break;
            }
        }

        private void DebugLog(string message)
        {
            if (Debug) Puts($"[Debug] {message}");
        }

        void RefundBag(BaseEntity entity, BasePlayer player)
        {
            if (Debug) Puts($"[Debug] cancelling a sleepingbag placement");

            if (permission.UserHasPermission(player.UserIDString, RestrictVip_Perm) == true)
            {
                Player.Message(player, prefix + string.Format(msg("MaxLimitVip", player.UserIDString), configData.SpawnBags.LimitBagsAmountVip), chaticon);
            }

            else if (permission.UserHasPermission(player.UserIDString, RestrictDef_Perm) == true)
            {
                Player.Message(player, prefix + string.Format(msg("MaxLimitDefault", player.UserIDString), configData.SpawnBags.LimitBagsAmountDef), chaticon);
            }

            entity.KillMessage();

            if (configData.SpawnBags.RefundBags)
            {
                Int32 number = -1754948969;

                if (entity.ShortPrefabName.Contains("beachtowel"))
                    number = -8312704;

                var itemtogive = ItemManager.CreateByItemID(number, 1);

                if (!player.inventory.GiveItem(itemtogive))
                {
                    if (Debug) Puts($"[Debug] Bag refund canceled no room in inventory");
                    Player.Message(player, prefix + string.Format(msg("InventoryFull", player.UserIDString)), chaticon);
                    itemtogive.Remove(0f);   //// Destroy item if give isn't possible
                    ItemManager.DoRemoves(); ////
                }
                else
                {
                    if (Debug) Puts($"[Debug] Bag refunded");
                    Player.Message(player, prefix + string.Format(msg("InventoryNotFull", player.UserIDString)), chaticon);
                }
                if (Debug) Puts($"[Debug] Bag refund = true");
            }
        }

        void RefundBed(BaseEntity entity, BasePlayer player)
        {
            if (Debug) Puts($"[Debug] cancelling a bed placement");

            if (permission.UserHasPermission(player.UserIDString, RestrictVip_Perm) == true)
            {
                Player.Message(player, prefix + string.Format(msg("MaxLimitVip", player.UserIDString), configData.SpawnBeds.LimitBedsAmountVip), chaticon);
            }
            else if (permission.UserHasPermission(player.UserIDString, RestrictDef_Perm) == true)
            {
                Player.Message(player, prefix + string.Format(msg("MaxLimitDefault", player.UserIDString), configData.SpawnBeds.LimitBedsAmountDef), chaticon);
            }

            entity.KillMessage();

            if (configData.SpawnBeds.RefundBeds)
            {
                var itemtogive = ItemManager.CreateByItemID(-1273339005, 1);//bed
                if (!player.inventory.GiveItem(itemtogive))
                {
                    if (Debug) Puts($"[Debug] Bed refund canceled no room in inventory");
                    Player.Message(player, prefix + string.Format(msg("InventoryFull", player.UserIDString)), chaticon);
                    itemtogive.Remove(0f);
                    ItemManager.DoRemoves();

                }
                else
                {
                    if (Debug) Puts($"[Debug] Bed refunded");
                    Player.Message(player, prefix + string.Format(msg("InventoryNotFull", player.UserIDString)), chaticon); 
                }
                if (Debug) Puts($"[Debug] Bed refund = true");
            }
        }

        private void RecalculateCounts()
        {
            BagIDs.Clear();
            BedIDs.Clear();
            foreach (var bag in BaseNetworkable.serverEntities.OfType<SleepingBag>())
            {
                if (bag.ShortPrefabName.Contains("bed"))
                {
                    if (!BedIDs.ContainsKey(bag.OwnerID))
                        BedIDs[bag.OwnerID] = new HashSet<ulong>();
                    BedIDs[bag.OwnerID].Add(bag.net.ID.Value);
                }
                else
                {
                    if (!BagIDs.ContainsKey(bag.OwnerID))
                        BagIDs[bag.OwnerID] = new HashSet<ulong>();
                    BagIDs[bag.OwnerID].Add(bag.net.ID.Value);
                }
            }
        }

        #endregion

        #region Message helper

        private string msg(string key, string id = null) => lang.GetMessage(key, this, id);

        #endregion
    }
}