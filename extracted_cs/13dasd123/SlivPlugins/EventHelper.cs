// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using Facepunch;
using JSON;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static RelationshipManager;

/* 
 * 
 */

/* 1.0.11
 * Added a discord webhook option to announce game starts.
 * Added support for Join messages to be handled via EventHelper.
 * Fixed an issue where manually started events would end when EventHelper's auto start kicked in.
 * Fixed an issue where players werent being removed from events properly.
 */

namespace Oxide.Plugins
{
    [Info("EventHelper", "imthenewguy", "1.0.11")]
    [Description("A plugin that devs can use to help get players into events.")]
    class EventHelper : RustPlugin
    {
        #region Config       

        private Configuration config;
        public class Configuration
        {
            [JsonProperty("A list of items that cannot be taken into an event")]
            public List<string> black_listed_items = new List<string>() { "cassette", "	cassette.medium", "cassette.short", "fun.casetterecorder", "boombox" };

            [JsonProperty("The command to recover items")]
            public string redeem_items_command = "recoveritems";

            [JsonProperty("How often should we cycle through events")]
            public float event_cycle_timer = 7200f;

            [JsonProperty("Set to true if the server is giving players a kit on respawn and you want players to receive their saved items back automatically")]
            public bool auto_rekit = false;

            [JsonProperty("Unban players on server wipe?")]
            public bool unban_players = true;

            [JsonProperty("Allow players to vote for games manually?")]
            public bool voting_enabled = true;

            [JsonProperty("Command to initiate a vote for an event")]
            public string vote_start_cmd = "eventvote";

            [JsonProperty("Command to vote for an event")]
            public string vote_cmd = "vote";

            [JsonProperty("Max vote time (seconds)")]
            public int max_vote_time = 300;

            [JsonProperty("Minimum temperature while at an event")]
            public float temperature_min = 25f;

            [JsonProperty("Maximum temperature while at an event")]
            public float temperature_max = 25f;
            
            [JsonProperty("How often should players be reminded of the voting feature (seconds)? [0 = off]")]
            public float voting_reminder_time = 1200f;

            [JsonProperty("Minimum online players before voting is allowed? [0 = no limit]")]
            public int minimum_players_online_vote = 0;

            [JsonProperty("Minimum online players before automatic arenas start? [0 = no limit]")]
            public int minimum_players_online_auto_start = 0;

            [JsonProperty("Remove entities that are stuck to the player on entry to the event?")]
            public bool clear_stuck_entities = true;

            [JsonProperty("Prevent players who are flagged with the NoEscape plugin from joining the event?")]
            public bool prevent_no_escape_joins = true;

            [JsonProperty("Announce in chat when a player joins an event?")]
            public bool announce_player_join = true;

            [JsonProperty("Settings for Discord plugin")]
            public DiscordSettings discordSettings = new DiscordSettings();

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        public class DiscordSettings
        {
            [JsonProperty("Webhook URL")]
            public string webhook;

            [JsonProperty("Send a discord notification when an event starts?")]
            public bool send_on_event = true;
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    throw new JsonException();
                }

                if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
                {
                    PrintToConsole("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch
            {
                PrintToConsole($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            PrintToConsole($"Configuration changes saved to {Name}.json");
            Config.WriteObject(config, true);
        }

        #endregion

        #region classes
        public Dictionary<string, EventInfo> Events = new Dictionary<string, EventInfo>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, bool> EventRunning = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        List<BasePlayer> FirstRespawnAfterGame;
        List<ulong> BagPreventDropList = new List<ulong>();

        public class EventInfo
        {
            public bool automatic_start;
            public bool strip_items;
            public bool leaves_event_on_death;
            public bool full_health_on_join;
            public bool give_items_back_automatically;
            public bool full_metabolism_on_join;
            public bool restricted_temp;
            public Vector3 teleport_destination;
            public List<BasePlayer> participants = new List<BasePlayer>();
            public bool manually_started;
            public bool allow_Teams;
            public string[] black_listed_commands;
            public ExternalPluginSettings external_plugin_settings = new ExternalPluginSettings(false, false, false, false, false, false);

            public EventInfo(bool automatic_start, bool strip_items, bool leaves_event_on_death, bool full_health_on_join, bool give_items_back_automatically, bool full_metabolism_on_join, Vector3 teleport_destination, bool restricted_temp = true, bool allow_Teams = true)
            {
                this.automatic_start = automatic_start;
                this.strip_items = strip_items;
                this.leaves_event_on_death = leaves_event_on_death;
                this.full_health_on_join = full_health_on_join;
                this.give_items_back_automatically = give_items_back_automatically;
                this.full_metabolism_on_join = full_metabolism_on_join;
                this.teleport_destination = teleport_destination;
                this.restricted_temp = restricted_temp;
                this.allow_Teams = allow_Teams;
            }

            public class ExternalPluginSettings
            {
                public bool canDropBackpack;
                public bool canEraseBackpack;
                public bool canOpenBackpack;
                public bool canBackpackAcceptItem;
                public bool canRedeemKit;
                public bool CanLoseXP;

                public ExternalPluginSettings(bool canDropBackpack, bool canEraseBackpack, bool canOpenBackpack, bool canBackpackAcceptItem, bool canRedeemKit, bool CanLoseXP)
                {
                    this.canDropBackpack = canDropBackpack;
                    this.canEraseBackpack = canEraseBackpack;
                    this.canOpenBackpack = canOpenBackpack;
                    this.canBackpackAcceptItem = canBackpackAcceptItem;
                    this.canRedeemKit = canRedeemKit;
                    this.CanLoseXP = CanLoseXP;
                }
            }
        }

        public enum ContainerType
        {
            Main,
            Belt,
            Wear,
            None
        }

        public class ItemInfo
        {
            public string shortname;
            public ulong skin;
            public int amount;
            public float condition;
            public float maxCondition;
            public int ammo;
            public string ammotype;
            public ContainerType container;
            public int position;
            public int frequency;
            public Item.Flag flags;
            public KeyInfo instanceData;
            public class KeyInfo
            {
                public int dataInt;
                public int blueprintTarget;
                public int blueprintAmount;
            }
            public ItemInfo[] contents;
            public List<ItemInfo> item_contents;
            public string text;
            public string name;
        }

        #endregion

        #region Data

        PlayerEntity pcdData;
        private DynamicConfigFile PCDDATA;
        const string perm_admin = "eventhelper.admin";

        void Init()
        {
            PCDDATA = Interface.Oxide.DataFileSystem.GetFile(this.Name);
            LoadData();
            permission.RegisterPermission(perm_admin, this);
            cmd.AddChatCommand(config.vote_cmd, this, "VoteForEvent");
            cmd.AddChatCommand(config.vote_start_cmd, this, "StartEventVote");

            Temperature_Min = config.temperature_min;
            Temperature_Max = config.temperature_max;

            if (config.voting_reminder_time > 0 && config.voting_enabled)
            {
                timer.Every(config.voting_reminder_time, () =>
                {
                    PrintToChat(string.Format(lang.GetMessage("VotingReminder", this), config.vote_start_cmd));
                });
            }
        }

        void Unload()
        {            
            cmd.RemoveChatCommand(config.redeem_items_command, this);
            cmd.RemoveChatCommand(config.vote_cmd, this);
            cmd.RemoveChatCommand(config.vote_start_cmd, this);
            foreach (var kvp in pcdData.pEntity)
            {
                kvp.Value.AtEvent = false;
                kvp.Value.Event = null;
            }
            foreach (var e in Events.ToList())
            {
                EMRemoveEvent(e.Key);
            }
            SaveData();

            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyTempRestriction(player);
            }

            WasAtEvent.Clear();
            WasAtEvent = null;
        }

        void SaveData()
        {
            PCDDATA.WriteObject(pcdData);
        }

        void LoadData()
        {
            try
            {
                pcdData = Interface.Oxide.DataFileSystem.ReadObject<PlayerEntity>(this.Name);
            }
            catch
            {
                Puts(lang.GetMessage("NoPlayerDataLoading", this));
                pcdData = new PlayerEntity();
            }
        }

        class PlayerEntity
        {
            public Dictionary<ulong, PCDInfo> pEntity = new Dictionary<ulong, PCDInfo>();
            public Dictionary<string, List<ulong>> banned_players = new Dictionary<string, List<ulong>>();
        }

        class PCDInfo
        {
            public string Event;
            public float Health;
            public float Food;
            public float Water;
            public bool AtEvent;
            public Vector3 Location;
            public List<ItemInfo> Items = new List<ItemInfo>();
        }
        

        [PluginReference]
        private Plugin Backpacks, NoEscape, RestoreUponDeath;

        #endregion;

        #region Localization

        protected override void LoadDefaultMessages()
        {

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["AtEvent"] = "You are already at an event.",
                ["ItemsStored"] = "You have items stored that must be claimed before joining another event. Type /{0} to recover your items (doing this will delete your current inventory).",
                ["NoCrafting"] = "You must stop crafting in order to join this event.",
                ["EscapeBlocked"] = "You cannot join the game while you are escape blocked.",
                ["BlackListedItem"] = "You cannot bring a {0} into a minigame.",
                ["BlackListedItemSub"] = "You cannot bring a {0} into a minigame. This was found inside of your {1}",
                ["AlreadyEnrolled"] = "You are already enrolled in this event.",
                ["FailedToStrip"] = "Failed to strip your items.",
                ["ManualItemRecovery"] = "You must type /{0} to recover them (doing this will delete your current inventory).",
                ["NoRecovery"] = "You cannot recover your items until you leave the event.",
                ["NoCommand"] = "You cannot use the {0} command at this event.",
                ["UnclaimedItems"] = "You have unclaimed items. Type /{0} to recover them.",
                ["SkipStart"] = "Skipping event start due to manual start.",
                ["ManualStart"] = "Manually started event {0}",
                ["RemovedEvent"] = "Removed Event: {0}",
                ["SetupEvent"] = "Setup Event: {0}",
                ["EndedEvent"] = "Ended the event",
                ["NotSetup"] = "{0} has not been setup.",
                ["EventNotSetupExternalPluginSettings"] = "Event: {0} has not been setup. Must call EMCreateEvent before EMExternalPluginSettings.",
                ["EventNotSetupBlackList"] = "Event: {0} has not been setup. Must call EMCreateEvent before EMBlackListCommands.",
                ["RegisteredCommands"] = "Registered {0} black listed commands.",
                ["NoPlayerDataLoading"] = "Couldn't load player data, creating new Playerfile",
                ["NoEventData"] = "You have no event data stored.",
                ["MorePlayersFound"] = "More than one player found: {0}",
                ["NoMatch"] = "No player was found that matched: {0}",
                ["EHBanUsage"] = "Usage: /ehban <player name/id>",
                ["BannedPlayer"] = "Banned {0} from all EventHelper events.",
                ["EHUnbanUsage"] = "Usage: /ehunban <player name/id>",
                ["UnbannedPlayer"] = "Unbanned {0} from all EventHelper events.",
                ["NoBan"] = "{0} does not have any bans.",
                ["VotingReminder"] = "You can vote to start your favourite event by typing <color=#ffec00>/{0} <event name></color>",
                ["EventNotRegistered"] = "Failed to join - Event: {0} has not been registered!",
                ["PreventTeamCreation"] = "The event you are in does not allow team creation.",
                ["DiscordMessageEventStarted"] = ":globe_with_meridians: Event Started: {0}.",
                ["JointAnnounceMsg_1"] = "<color=#00fff7>{0}</color> has decided to join the action at the <color=#00ff83>{1}</color> event!",
                ["JointAnnounceMsg_2"] = "<color=#00fff7>{0}</color> has joined the <color=#00ff83>{1}</color> event and is ready to take some names!",
                ["JointAnnounceMsg_3"] = "Oh no, <color=#00fff7>{0}</color> has joined the <color=#00ff83>{1}</color> event!",
            }, this);
        }

        #endregion

        #region Methods

        bool IsPlayerBanned(BasePlayer player, string eventName)
        {
            if (pcdData.banned_players.ContainsKey(eventName) && pcdData.banned_players[eventName].Contains(player.userID)) return true;
            if (pcdData.banned_players.ContainsKey("global") && pcdData.banned_players["global"].Contains(player.userID)) return true;
            return false;
        }

        bool CanJoinEvent(BasePlayer player, string eventName)
        {
            if (IsPlayerBanned(player, eventName))
            {
                PrintToChat(player, $"You are banned from the event and cannot join.");
                return false;
            }
            var playerData = GetPlayer(player.userID);
            if (playerData.AtEvent)
            {
                PrintToChat(player, lang.GetMessage("AtEvent", this, player.UserIDString));
                return false;
            }
            if (HasItemsStored(player.userID) && !playerData.AtEvent)
            {
                PrintToChat(player, string.Format(lang.GetMessage("ItemsStored", this, player.UserIDString), config.redeem_items_command));
                return false;
            }
            if (player.inventory.crafting.queue.Count != 0)
            {
                PrintToChat(player, lang.GetMessage("NoCrafting", this, player.UserIDString));
                return false;
            }
            if (NoEscape != null && config.prevent_no_escape_joins && Convert.ToBoolean(NoEscape.Call("IsEscapeBlocked", player.UserIDString)))
            {
                PrintToChat(player, lang.GetMessage("EscapeBlocked", this, player.UserIDString));
                return false;
            }
            if (HasProhibitedItems(player)) return false;

            if (Interface.CallHook("EMOnEventJoin", player, eventName) != null) return false;

            return true;
        }

        bool StorePlayerItems(BasePlayer player)
        {
            var playerData = GetPlayer(player.userID);
            playerData.Items.AddRange(GetItems(player, player.inventory.containerWear));
            playerData.Items.AddRange(GetItems(player, player.inventory.containerMain));
            playerData.Items.AddRange(GetItems(player, player.inventory.containerBelt));
            player.inventory.Strip();
            SaveData();
            return true;
        }

        List<ItemInfo> GetItems(BasePlayer player, ItemContainer container)
        {
            List<ItemInfo> result = new List<ItemInfo>();
            foreach (var item in container.itemList)
            {
                result.Add(new ItemInfo()
                {
                    shortname = item.info.shortname,
                    position = item.position,
                    container = GetContainer(player, container),
                    amount = item.amount,
                    ammo = item.GetHeldEntity() is BaseProjectile ? (item.GetHeldEntity() as BaseProjectile).primaryMagazine.contents : item.GetHeldEntity() is FlameThrower ? (item.GetHeldEntity() as FlameThrower).ammo : item.GetHeldEntity() is Chainsaw ? (item.GetHeldEntity() as Chainsaw).ammo : 0,
                    ammotype = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.ammoType.shortname ?? null,
                    skin = item.skin,
                    condition = item.condition,
                    maxCondition = item.maxCondition,
                    flags = item.flags,
                    instanceData = item.instanceData != null ? new ItemInfo.KeyInfo()
                    {
                        dataInt = item.instanceData.dataInt,
                        blueprintTarget = item.instanceData.blueprintTarget,
                        blueprintAmount = item.instanceData.blueprintAmount,
                    }
                    : null,
                    name = item.name ?? null,
                    text = item.text ?? null,
                    item_contents = item.contents?.itemList != null ? GetItems(player, item.contents) : null
                });
            }
            return result;
        }

        //bool StorePlayerItems(BasePlayer player)
        //{
        //    var playerData = GetPlayer(player.userID);
        //    foreach (var item in player.inventory.AllItems())
        //    {
        //        var item_save = new ItemInfo()
        //        {
        //            shortname = item.info.shortname,
        //            container = GetContainer(player, item.parent),
        //            position = item.position,
        //            amount = item.amount,
        //            ammo = item.GetHeldEntity() is BaseProjectile ? (item.GetHeldEntity() as BaseProjectile).primaryMagazine.contents : item.GetHeldEntity() is FlameThrower ? (item.GetHeldEntity() as FlameThrower).ammo : 0,
        //            ammotype = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.ammoType.shortname ?? null,
        //            skin = item.skin,
        //            condition = item.condition,
        //            maxCondition = item.maxCondition,
        //            contents = item.contents?.itemList.Select(item1 => new ItemInfo
        //            {
        //                shortname = item1.info.shortname,
        //                amount = item1.amount,
        //                condition = item1.condition,
        //                maxCondition = item1.maxCondition
        //            }).ToArray(),
        //        };
        //        if (item.instanceData != null)
        //        {
        //            item_save.instanceData = new ItemInfo.KeyInfo()
        //            {
        //                ShouldPool = item.instanceData.ShouldPool,
        //                dataInt = item.instanceData.dataInt,
        //                blueprintTarget = item.instanceData.blueprintTarget,
        //                blueprintAmount = item.instanceData.blueprintAmount,
        //                subEntity = item.instanceData.subEntity
        //            };
        //        }
        //        if (item.text != null) item_save.text = item.text;
        //        if (item.name != null) item_save.name = item.name;
        //        if (item.info.displayDescription.english != null) item_save.description = item.info.displayDescription.english;

        //        playerData.Items.Add(item_save);
        //    }
        //    player.inventory.Strip();
        //    return true;
        //}

        bool RestoreItems(BasePlayer player, bool strip = true)
        {
            if (player.IsDead() || !player.IsConnected) return false;
            var playerData = GetPlayer(player.userID);
            if (strip) player.inventory.Strip();
            if (playerData.Items == null || playerData.Items.Count == 0) return true;
            foreach (var item in playerData.Items)
            {
                if (item.amount < 1) continue;
                if (item.container == ContainerType.Main) GetRestoreItem(player, player.inventory.containerMain, item);
                else if (item.container == ContainerType.Wear) GetRestoreItem(player, player.inventory.containerWear, item);
                else if (item.container == ContainerType.Belt) GetRestoreItem(player, player.inventory.containerBelt, item);
            }
            return true;
        }

        Item GetRestoreItem(BasePlayer player, ItemContainer container, ItemInfo savedItem)
        {
            var item = ItemManager.CreateByName(savedItem.shortname, savedItem.amount, savedItem.skin);
            if (savedItem.name != null) item.name = savedItem.name;
            if (savedItem.text != null) item.text = savedItem.text;
            item.condition = savedItem.condition;
            item.maxCondition = savedItem.maxCondition;
            BaseEntity heldEntity = item.GetHeldEntity();
            if (heldEntity != null)
            {
                BaseProjectile weapon = heldEntity as BaseProjectile;
                if (weapon != null)
                {
                    if (!string.IsNullOrEmpty(savedItem.ammotype))
                        weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(savedItem.ammotype);
                    weapon.primaryMagazine.contents = savedItem.ammo;
                }
                else if (savedItem.ammo > 0)
                {
                    FlameThrower flameThrower = heldEntity as FlameThrower;
                    if (flameThrower != null) flameThrower.ammo = savedItem.ammo;
                    else
                    {
                        Chainsaw chainsaw = heldEntity as Chainsaw;
                        if (chainsaw != null) chainsaw.ammo = savedItem.ammo;
                    }
                }
            }
            item.flags = savedItem.flags;
            if (savedItem.instanceData != null)
            {
                item.instanceData = new ProtoBuf.Item.InstanceData();
                item.instanceData.ShouldPool = false;
                item.instanceData.dataInt = savedItem.instanceData.dataInt;
                item.instanceData.blueprintTarget = savedItem.instanceData.blueprintTarget;
                item.instanceData.blueprintAmount = savedItem.instanceData.blueprintAmount;
            }            
            if (savedItem.item_contents != null && savedItem.item_contents.Count > 0)
            {
                if (item.contents == null)
                {
                    item.contents = new ItemContainer();
                    item.contents.ServerInitialize(null, savedItem.item_contents.Count);
                    item.contents.GiveUID();
                    item.contents.parent = item;
                }
                foreach (var _item in savedItem.item_contents)
                {
                    GetRestoreItem(player, item.contents, _item);
                }
            }
            if (!item.MoveToContainer(container, savedItem.position)) player.GiveItem(item);
            return item;
        }

        //bool RestoreItems(BasePlayer player, bool strip = true)
        //{
        //    if (player.IsDead() || !player.IsConnected) return false;
        //    var playerData = GetPlayer(player.userID);
        //    if (strip) player.inventory.Strip();
        //    if (playerData.Items == null || playerData.Items.Count == 0) return true;
        //    List<Item> unpositioned_items = Pool.GetList<Item>();
        //    foreach (var saved_item in playerData.Items)
        //    {
        //        try
        //        {
        //            var item = ItemManager.CreateByName(saved_item.shortname, saved_item.amount, saved_item.skin);
        //            if (saved_item.name != null) item.name = saved_item.name;
        //            if (saved_item.description != null) item.info.displayDescription.english = saved_item.description;
        //            item.condition = saved_item.condition;
        //            item.maxCondition = saved_item.maxCondition;
        //            BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
        //            if (weapon != null)
        //            {
        //                if (!string.IsNullOrEmpty(saved_item.ammotype))
        //                    weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(saved_item.ammotype);
        //                weapon.primaryMagazine.contents = saved_item.ammo;
        //            }
        //            FlameThrower flameThrower = item.GetHeldEntity() as FlameThrower;
        //            if (flameThrower != null)
        //                flameThrower.ammo = saved_item.ammo;

        //            if (saved_item.contents != null)
        //            {
        //                foreach (ItemInfo contentData in saved_item.contents)
        //                {
        //                    Item newContent = ItemManager.CreateByName(contentData.shortname, contentData.amount);
        //                    if (newContent != null)
        //                    {
        //                        newContent.condition = contentData.condition;
        //                        newContent.MoveToContainer(item.contents);
        //                    }
        //                }
        //            }

        //            if (saved_item.instanceData != null)
        //            {
        //                item.instanceData = new ProtoBuf.Item.InstanceData();
        //                item.instanceData.ShouldPool = saved_item.instanceData.ShouldPool;
        //                item.instanceData.dataInt = saved_item.instanceData.dataInt;
        //                item.instanceData.blueprintTarget = saved_item.instanceData.blueprintTarget;
        //                item.instanceData.blueprintAmount = saved_item.instanceData.blueprintAmount;
        //                item.instanceData.subEntity = saved_item.instanceData.subEntity;
        //            }
        //            if (saved_item.text != null) item.text = saved_item.text;

        //            switch (saved_item.container)
        //            {
        //                case ContainerType.Belt:
        //                    item.MoveToContainer(player.inventory.containerBelt, saved_item.position);
        //                    break;
        //                case ContainerType.Wear:
        //                    item.MoveToContainer(player.inventory.containerWear, saved_item.position);
        //                    break;
        //                case ContainerType.Main:
        //                    item.MoveToContainer(player.inventory.containerMain, saved_item.position);
        //                    break;

        //                default:
        //                    unpositioned_items.Add(item);
        //                    break;
        //            }
        //        }
        //        catch { Puts($"Failed to return {saved_item?.name ?? saved_item?.shortname}"); }
        //    }
        //    if (unpositioned_items.Count > 0)
        //    {
        //        foreach (var item in unpositioned_items)
        //        {
        //            player.GiveItem(item);
        //        }
        //    }
        //    playerData.Items.Clear();
        //    Pool.FreeList(ref unpositioned_items);
        //    return true;
        //}

        ContainerType GetContainer(BasePlayer player, ItemContainer container)
        {
            if (container.uid == player.inventory.containerBelt.uid) return ContainerType.Belt;
            else if (container.uid == player.inventory.containerWear.uid) return ContainerType.Wear;
            else if (container.uid == player.inventory.containerMain.uid) return ContainerType.Main;
            else return ContainerType.None;
        }

        bool HasProhibitedItems(BasePlayer player)
        {
            if (config.black_listed_items == null || config.black_listed_items.Count == 0 || player.inventory.AllItems().Length == 0) return false;
            foreach (var item in player.inventory.AllItems())
            {
                if (config.black_listed_items.Contains(item.info.shortname))
                {
                    PrintToChat(player, string.Format(lang.GetMessage("BlackListedItem", this, player.UserIDString), item.info.displayName.english));
                    return true;
                }
                if (item.contents != null && item.contents.itemList != null && item.contents.itemList.Count > 1)
                {
                    foreach (var sub_item in item.contents.itemList)
                    {
                        if (config.black_listed_items.Contains(sub_item.info.shortname))
                        {
                            PrintToChat(player, string.Format(lang.GetMessage("BlackListedItemSub", this, player.UserIDString), item.info.displayName.english, item.info.displayName.english));
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool HasItemsStored(ulong id)
        {
            var playerData = GetPlayer(id);
            if (playerData.Items.Count == 0) return false;
            return true;
        }

        bool IsPlayerSetup(ulong id)
        {
            return pcdData.pEntity.ContainsKey(id);
        }

        PCDInfo GetPlayer(ulong id)
        {
            if (!pcdData.pEntity.ContainsKey(id)) pcdData.pEntity.Add(id, new PCDInfo());
            return pcdData.pEntity[id];
        }

        void RestoreStats(BasePlayer player)
        {
            if (player.IsDead() || !player.IsConnected) return;
            var playerData = GetPlayer(player.userID);
            if (playerData.Food > 0) player.metabolism.calories.SetValue(playerData.Food);
            if (playerData.Water > 0) player.metabolism.hydration.SetValue(playerData.Water);
            if (playerData.Health > 0) player.SetHealth(playerData.Health);
            if (playerData.Location != Vector3.zero) TeleportToEvent(player, playerData.Location);
            player.metabolism.bleeding.SetValue(0);
            playerData.Location = Vector3.zero;
            playerData.Food = 0;
            playerData.Water = 0;
            playerData.Health = 0;
            player.State.unHostileTimestamp = Network.TimeEx.currentTimestamp;
            player.MarkHostileFor(0);
            DestroyTempRestriction(player);
        }

        void StorePlayerInfo(BasePlayer player, bool StoreHealth, bool StoreStats, bool StoreTemp)
        {
            var playerData = GetPlayer(player.userID);
            if (StoreStats)
            {
                playerData.Food = player.metabolism.calories.value;
                playerData.Water = player.metabolism.hydration.value;
                player.metabolism.calories.SetValue(player.metabolism.calories.max);
                player.metabolism.hydration.SetValue(player.metabolism.hydration.max);
                player.metabolism.bleeding.SetValue(0);
                player.metabolism.radiation_level.SetValue(0);
                player.metabolism.radiation_poison.SetValue(0);
            }
            if (StoreHealth)
            {
                playerData.Health = player.health;
                player.SetHealth(100f);
            }
            if (StoreTemp) AddTempRestriction(player);
            playerData.Location = player.transform.position;
        }

        void TeleportToEvent(BasePlayer player, Vector3 loc)
        {
            if (loc == Vector3.zero) return;
            Player.Teleport(player, loc);
            player.StartSleeping();
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.ClientRPCPlayer(null, player, "StartLoading");
            player.SendEntityUpdate();
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);

            if (config.clear_stuck_entities)
            {
                if (player.children != null)
                {
                    List<BaseEntity> stuck = Pool.GetList<BaseEntity>();
                    foreach (var child in player.children)
                    {
                        if (child.PrefabName == "assets/prefabs/misc/burlap sack/generic_world.prefab")
                        {
                            var item = child.GetItem();
                            if (item != null)
                            {
                                var heldEntity = item.GetHeldEntity();
                                if ((heldEntity != null && heldEntity is BaseMelee) || item.info.shortname.StartsWith("arrow.") || item.info.shortname.Equals("ammo.nailgun.nails")) stuck.Add(child);

                            }
                        }
                    }
                    foreach (var child in stuck)
                    {
                        child.KillMessage();
                    }
                    Pool.FreeList(ref stuck);
                }                
            }
        }

        void RemoveFromEvent(BasePlayer player, string eventName)
        {
            if (!IsPlayerSetup(player.userID)) return;
            var playerData = GetPlayer(player.userID);
            playerData.AtEvent = false;
            EventInfo ei;
            if (eventName != null) Events.TryGetValue(eventName, out ei);
            else ei = null;

            ei.participants.Remove(player);
        }

        BasePlayer FindPlayerByID(string id, BasePlayer searchingPlayer = null)
        {
            if (!id.IsSteamId()) return null;
            var player = BasePlayer.allPlayerList.Where(x => x.UserIDString == id).FirstOrDefault();
            if (player == null)
            {
                if (searchingPlayer != null) PrintToChat(searchingPlayer, string.Format(lang.GetMessage("NoMatch", this), id));
                else Puts(string.Format(lang.GetMessage("NoMatch", this), id));
            }
            return player ?? null;
        }

        private BasePlayer FindPlayerByName(string Playername, BasePlayer SearchingPlayer = null)
        {
            var lowered = Playername.ToLower();
            var targetList = BasePlayer.allPlayerList.Where(x => x.displayName.ToLower().Contains(lowered)).OrderBy(x => x.displayName.Length);
            if (targetList.Count() == 1)
            {
                return targetList.First();
            }
            if (targetList.Count() > 1)
            {
                if (targetList.First().displayName.Equals(Playername, StringComparison.OrdinalIgnoreCase))
                {
                    return targetList.First();
                }
                if (SearchingPlayer != null)
                {
                    PrintToChat(SearchingPlayer, string.Format(lang.GetMessage("MorePlayersFound", this, SearchingPlayer.UserIDString), String.Join(",", targetList.Select(x => x.displayName))));
                }
                else Puts(string.Format(lang.GetMessage("MorePlayersFound", this), String.Join(",", targetList.Select(x => x.displayName))));
                return null;
            }
            if (targetList.Count() == 0)
            {
                if (SearchingPlayer != null)
                {
                    PrintToChat(SearchingPlayer, string.Format(lang.GetMessage("NoMatch", this, SearchingPlayer.UserIDString), Playername));
                }
                else Puts(string.Format(lang.GetMessage("NoMatch", this), Playername));
                return null;
            }
            return null;
        }

        public bool AddBan(BasePlayer player, string event_name)
        {
            if (!pcdData.banned_players.ContainsKey(event_name)) pcdData.banned_players.Add(event_name, new List<ulong>());
            if (!pcdData.banned_players[event_name].Contains(player.userID))
            {
                pcdData.banned_players[event_name].Add(player.userID);
                SaveData();
                return true;
            }
            return false;
        }

        public bool RemoveBan(BasePlayer player, string event_name)
        {
            if (!pcdData.banned_players.ContainsKey(event_name) || !pcdData.banned_players[event_name].Contains(player.userID)) return false;
            pcdData.banned_players[event_name].Remove(player.userID);
            SaveData();
            return true;
        }

        #endregion

        #region Event Stuff

        string LastEvent;
        List<string> automatic_Events = new List<string>();
        void StartNextEvent()
        {
            if (config.minimum_players_online_auto_start > 0 && BasePlayer.activePlayerList?.Count < config.minimum_players_online_auto_start)
            {
                Puts($"Attmempted to start a game, but there are not enough players online [{BasePlayer.activePlayerList?.Count}/{config.minimum_players_online_auto_start}].");
                return;
            }
            if (Events == null || Events.Count == 0 || automatic_Events.Count == 0) return;

            foreach (var _event in Events)
                if (_event.Value.manually_started)
                {
                    Puts($"Attempted to start a new event, but {_event.Key} is still flagged as manually started.");
                    return;
                }

            if (LastEvent != null && Events.ContainsKey(LastEvent) && !Events[LastEvent].manually_started)
            {
                Interface.CallHook("EMEndGame", LastEvent);
                EMEndEvent(LastEvent);
            }
            if (LastEvent == null)
            {
                Interface.CallHook("EMStartNextEvent", automatic_Events.First());
                LastEvent = automatic_Events.First();
            }
            else
            {
                var foundLastEvent = false;
                var setNewEvent = false;
                foreach (var e in automatic_Events)
                {
                    if (Events[e].manually_started) continue;
                    if (foundLastEvent)
                    {
                        Interface.CallHook("EMStartNextEvent", e);
                        LastEvent = e;
                        setNewEvent = true;
                        break;
                    }
                    else if (e == LastEvent) foundLastEvent = true;
                }
                if (!setNewEvent)
                {
                    var e = automatic_Events.First();
                    if (!Events[e].manually_started)
                    {
                        Interface.CallHook("EMStartNextEvent", e);
                        LastEvent = e;
                    }
                    else Puts(lang.GetMessage("SkipStart", this));
                    
                }
            }
        }

        [HookMethod("EMWasManuallyStarted")]
        public bool EMWasManuallyStarted(string eventName)
        {
            EventInfo ei;
            if (Events.TryGetValue(eventName, out ei))
            {
                return ei.manually_started;
            }
            return false;
        }

        [HookMethod("EMManuallyStarted")]
        public void EMManuallyStarted(string eventName)
        {
            EventInfo ei;
            if (Events.TryGetValue(eventName, out ei))
            {
                ei.manually_started = true;
                Puts(string.Format(lang.GetMessage("ManualStart", this), eventName));                
            }
        }

        [HookMethod("EMRemoveEvent")]
        public void EMRemoveEvent(string eventName)
        {
            Interface.CallHook("EMEndGame", eventName);
            EventInfo ei;
            if (Events.TryGetValue(eventName, out ei))
            {
                if (ei.participants.Count > 0)
                {
                    foreach (var player in ei.participants.ToList())
                    {
                        EMPlayerLeaveEvent(player, eventName);
                    }
                }
                if (ei.automatic_start) automatic_Events.Remove(eventName);
            }
            Events.Remove(eventName);
            Puts(string.Format(lang.GetMessage("RemovedEvent", this), eventName));
        }

        [HookMethod("EMCreateEvent")]
        private void EMCreateEvent(string eventName, bool automatic_start, bool stripItems, bool leaves_event_on_death, bool full_health_on_join, bool give_items_back_on_respawn, bool full_metabolism_on_join, Vector3 teleport_destination, bool restricted_metabolism = true, bool allow_teams = false)
        {
            if (Events.ContainsKey(eventName)) EMRemoveEvent(eventName);
            EventInfo ei;
            Events.Add(eventName, ei = new EventInfo(automatic_start, stripItems, leaves_event_on_death, full_health_on_join, give_items_back_on_respawn, full_metabolism_on_join, teleport_destination, restricted_metabolism, allow_teams));
            Puts(lang.GetMessage("SetupEvent", this), eventName);
            if (automatic_start && !automatic_Events.Contains(eventName))
            {
                automatic_Events.Add(eventName);
            }
        }

        [HookMethod("EMUpdateLobby")]
        private void EMUpdateLobby(string eventName, Vector3 pos)
        {
            EventInfo ei;
            if (!Events.TryGetValue(eventName, out ei)) return;
            ei.teleport_destination = pos;
        }

        [HookMethod("EMStartEvent")]
        private void EMStartEvent(string eventName)
        {
            EventInfo ei;
            if (!Events.TryGetValue(eventName, out ei)) return;
            if (!EventRunning.ContainsKey(eventName)) EventRunning.Add(eventName, true);
            else EventRunning[eventName] = true;

            SendDiscordMsg(string.Format(lang.GetMessage("DiscordMessageEventStarted", this), eventName.TitleCase()));
        }

        [HookMethod("EMEndEvent")]
        private void EMEndEvent(string eventName)
        {
            if (EventRunning.ContainsKey(eventName))
            {
                EventInfo ei;
                if (eventName == null || !Events.TryGetValue(eventName, out ei)) return;

                if (ei.participants.Count > 0)
                {
                    foreach (var player in ei.participants.ToList())
                    {
                        EMPlayerLeaveEvent(player, eventName);
                    }
                }
                ei.participants.Clear();
                ei.manually_started = false;
                if (!ei.allow_Teams)
                {
                    HandleEndGameRestoration();
                    SetLeaders();
                    ClearTeamCache();
                }                    
                EventRunning.Remove(eventName);
                //Interface.CallHook("EMEndGame", eventName);
                Puts(lang.GetMessage("EndedEvent", this));

                
            }
        }

        [HookMethod("EMEnrollPlayer")]
        private bool EMEnrollPlayer(BasePlayer player, string eventName)
        {
            EventInfo ei;
            if (!Events.TryGetValue(eventName, out ei))
            {
                Puts(string.Format(lang.GetMessage("EventNotRegistered", this), eventName));
                return false;
            }
            if (ei.participants.Contains(player))
            {
                PrintToChat(player, lang.GetMessage("AlreadyEnrolled", this, player.UserIDString));
                return false;
            }
            if (!CanJoinEvent(player, eventName)) return false;

            if (ei.strip_items && !StorePlayerItems(player))
            {                
                RestoreItems(player);
                pcdData.pEntity.Remove(player.userID);
                PrintToChat(player, lang.GetMessage("FailedToStrip", this, player.UserIDString));
                return false;
            }

            StorePlayerInfo(player, ei.full_health_on_join, ei.full_metabolism_on_join, ei.restricted_temp);

            if (!ei.allow_Teams) HandleTeam(player, true);

            ei.participants.Add(player);

            TeleportToEvent(player, ei.teleport_destination);
            var playerData = GetPlayer(player.userID);
            playerData.AtEvent = true;
            playerData.Event = eventName;

            Interface.CallHook("EMOnEventJoined", player, eventName);

            if (config.announce_player_join) AnnouncePlayerJoin(player, eventName);

            return true;
        }

        void AnnouncePlayerJoin(BasePlayer player, string eventName)
        {
            string prefix = Convert.ToString(Interface.CallHook("EMGetAnnounceJoinPrefix", player, eventName));
            string message = ValidJoinMessages.Count > 0 ? ValidJoinMessages.GetRandom() : "JointAnnounceMsg_1";

            var eventTitle = eventName.TitleCase();
            foreach (var p in BasePlayer.activePlayerList)
                PrintToChat(p, (!string.IsNullOrEmpty(prefix) ? prefix + " " : null) + string.Format(lang.GetMessage(message, this, p.UserIDString), player.displayName, eventTitle));
        }

        List<string> ValidJoinMessages = new List<string>();

        [HookMethod("EMPlayerLeaveEvent")]
        private void EMPlayerLeaveEvent(BasePlayer player, string eventName = null, bool manually_left = false)
        {
            if (!IsPlayerSetup(player.userID))
            {
                if (manually_left) PrintToChat(player, lang.GetMessage("NoEventData", this, player.UserIDString));
                return;
            }
            var playerData = GetPlayer(player.userID);

            EventInfo ei;
            if (eventName != null) Events.TryGetValue(eventName, out ei);
            else ei = null;
            RestoreStats(player);
            if (playerData.Items.Count > 0 && (ei == null || ei.give_items_back_automatically || manually_left))
            {
                if (RestoreItems(player))
                {
                    pcdData.pEntity.Remove(player.userID);
                }
            }
            else
            {
                player.inventory.Strip();
                playerData.AtEvent = false;
                if (playerData.Items.Count > 0) PrintToChat(player, string.Format(lang.GetMessage("ManualItemRecovery", this, player.UserIDString), config.redeem_items_command));
            }
            ei?.participants.Remove(player);

            if (!ei.allow_Teams) HandleTeam(player, false);

            Interface.CallHook("EMOnEventLeft", player, eventName);
        }        

        //Related to chat commandf
        void RestoreItemsCMD(BasePlayer player)
        {
            RestoreItemsAndStats(player, false);
        }

        void RestoreItemsAndStats(BasePlayer player, bool respawned = false, bool autokit = false)
        {
            if (!IsPlayerSetup(player.userID)) return;
            var playerData = GetPlayer(player.userID);
            if (playerData.AtEvent)
            {
                PrintToChat(player, lang.GetMessage("NoRecovery", this, player.UserIDString));
                return;
            }
            RestoreStats(player);
            if (autokit)
            {
                NextTick(() =>
                {
                    if (RestoreItems(player, respawned ? true : false))
                    {
                        pcdData.pEntity.Remove(player.userID);
                    }
                });                
            }
            else if (RestoreItems(player, respawned ? true : false))
            {
                pcdData.pEntity.Remove(player.userID);
            }
        }

        #endregion

        #region API Stuff

        [HookMethod("EMPlayerDiedAtEvent")]
        public bool EMPlayerDiedAtEvent(BasePlayer player) => WasAtEvent.ContainsKey(player.userID);

        object STOnPouchOpen(BasePlayer player)
        {
            if (EMAtEvent(player.userID)) return true;
            return null;
        }

        [HookMethod("EMExternalPluginSettings")]
        void EMExternalPluginSettings(string eventName, bool canDropBackpack = false, bool canEraseBackpack = false, bool canOpenBackpack = false, bool canBackpackAcceptItem = false, bool canRedeemKit = false, bool CanLoseXP= false)
        {
            EventInfo ei;
            if (!Events.TryGetValue(eventName, out ei))
            {
                Puts(string.Format(lang.GetMessage("EventNotSetupExternalPluginSettings", this), eventName));
                return;
            }
            Puts($"Setting up external plugin settings: eventName: {eventName}. canDropBackpack: {canDropBackpack}. canEraseBackpack: {canEraseBackpack}. canOpenBackpack: {canOpenBackpack}. canBackpackAcceptItem: {canBackpackAcceptItem}. canRedeemKit: {canRedeemKit}. CanLoseXP: {CanLoseXP}.");
            ei.external_plugin_settings = new EventInfo.ExternalPluginSettings(canDropBackpack, canEraseBackpack, canOpenBackpack, canBackpackAcceptItem, canRedeemKit, CanLoseXP);
        }

        [HookMethod("EMBlackListCommands")]
        void EMBlackListCommands(string eventName, string[] commands)
        {
            if (commands == null) return;
            EventInfo ei;
            if (!Events.TryGetValue(eventName, out ei))
            {
                Puts(string.Format(lang.GetMessage("EventNotSetupBlackList", this), eventName));
                return;
            }
            ei.black_listed_commands = commands;
            Puts(string.Format(lang.GetMessage("RegisteredCommands", this), ei.black_listed_commands.Length));
        }

        [HookMethod("EMIsParticipating")]
        bool EMIsParticipating(BasePlayer player, string eventName)
        {
            EventInfo ei;
            if (!Events.TryGetValue(eventName, out ei)) return false;
            return ei.participants.Contains(player);
        }

        [HookMethod("EMAtEvent")]
        bool EMAtEvent(ulong id)
        {
            if (IsPlayerSetup(id) && pcdData.pEntity[id].AtEvent) return true;
            return false;
        }

        object CanRedeemKit(BasePlayer player)
        {
            var eventData = GetEventInfo(player.userID);
            if (eventData != null && eventData.participants.Contains(player) && !eventData.external_plugin_settings.canRedeemKit) return false;
            return null;
        }

        object CanDropBackpack(ulong backpackOwnerID, Vector3 position)
        {
            if (BagPreventDropList.Contains(backpackOwnerID))
            {                
                BagPreventDropList.Remove(backpackOwnerID);
                return false;
            }
            return null;
        }

        object STOnPouchDrop(BasePlayer player)
        {
            var eventData = GetEventInfo(player.userID);
            if (eventData != null && eventData.participants.Contains(player) && !eventData.external_plugin_settings.canDropBackpack) return true;
            return null;
        }

        string CanOpenBackpack(BasePlayer player, ulong backpackOwnerID)
        {
            var eventData = GetEventInfo(backpackOwnerID);
            if (eventData != null && eventData.participants.Contains(player) && !eventData.external_plugin_settings.canBackpackAcceptItem)
            {
                try
                {
                    LogToFile("EventHelper_CanOpenBackpack", $"Failed to open backpack for {player.displayName} [{player.UserIDString}]. Participant count: {eventData.participants?.Count ?? 0}. WasAtEvent: {WasAtEvent.ContainsKey(player.userID)} . Last event: {LastEvent}", this, true, true);
                }
                catch { }
                
                return "You cannot open your backpack during an event.";
            }
            return null;
        }

        object CanBackpackAcceptItem(ulong backpackOwnerID, ItemContainer backpackContainer, Item item)
        {
            var player = BasePlayer.Find(backpackOwnerID.ToString());
            if (player == null) return null;
            var eventData = GetEventInfo(backpackOwnerID);
            if (eventData != null && eventData.participants.Contains(player) && !eventData.external_plugin_settings.canBackpackAcceptItem) return false;
            return null;
        }
        object CanEraseBackpack(ulong backpackOwnerID)
        {
            var player = BasePlayer.Find(backpackOwnerID.ToString());
            if (player == null) return null;
            var eventData = GetEventInfo(backpackOwnerID);
            if (eventData != null && eventData.participants.Contains(player) && !eventData.external_plugin_settings.canEraseBackpack) return false;
            return null;

        }
        object STOnLoseXP(BasePlayer player)
        {
            var eventData = GetEventInfo(player.userID);
            if (eventData != null && eventData.participants.Contains(player) && !eventData.external_plugin_settings.CanLoseXP) return true;
            return null;
        }

        EventInfo GetEventInfo(ulong userID)
        {
            if (!IsPlayerSetup(userID)) return null;
            var playerData = GetPlayer(userID);
            EventInfo eventData;
            string _event = !string.IsNullOrEmpty(playerData.Event) ? playerData.Event : WasAtEvent.ContainsKey(userID) ? WasAtEvent[userID] : null;
            if (string.IsNullOrEmpty(_event) || !Events.TryGetValue(_event, out eventData)) return null;
            return eventData;
        }

        object CanBePenalized(BasePlayer player)
        {
            var eventData = GetEventInfo(player.userID);
            if (eventData != null && eventData.participants.Contains(player) && !eventData.external_plugin_settings.CanLoseXP) return false;
            return null;
        }

        object OnRestoreUponDeath(BasePlayer player)
        {
            if (FirstRespawnAfterGame != null && FirstRespawnAfterGame.Contains(player))
            {
                FirstRespawnAfterGame.Remove(player);
                return true;
            }
            return null;
        }

        object CanRevivePlayer(BasePlayer player, Vector3 pos)
        {
            if (!WasAtEvent.ContainsKey(player.userID)) return null;
            return false;
        }

        #endregion

        #region Hooks

        void OnServerSave()
        {
            SaveData();
        }

        void OnNewSave(string filename)
        {
            pcdData.pEntity.Clear();
            if (config.unban_players) pcdData.pEntity.Clear();
            SaveData();
        }

        object OnPlayerCommand(BasePlayer player, string command, string[] args)
        {
            if (IsPlayerSetup(player.userID))
            {
                var playerData = GetPlayer(player.userID);
                EventInfo ei;
                if (!string.IsNullOrEmpty(playerData.Event) && Events.TryGetValue(playerData.Event, out ei) && ei.participants.Contains(player) && ei.black_listed_commands.Contains(command))
                {
                    PrintToChat(player, string.Format(lang.GetMessage("NoCommand", this, player.UserIDString), command));
                    return true;
                }
            }
            return null;
        }

        object OnServerCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || arg.cmd.FullName == "chat.say") return null;
            if (IsPlayerSetup(player.userID))
            {
                var playerData = GetPlayer(player.userID);
                EventInfo ei;
                if (!string.IsNullOrEmpty(playerData.Event) && Events.TryGetValue(playerData.Event, out ei) && ei.participants.Contains(player) && (ei.black_listed_commands.Contains(arg.cmd.Name) || ei.black_listed_commands.Contains(arg.cmd.FullName)))
                {
                    PrintToChat(player, string.Format(lang.GetMessage("NoCommand", this, player.UserIDString), arg.cmd.FullName));
                    return true;
                }
            }
            return null;
        }


        void OnPlayerRespawned(BasePlayer player)
        {
            if (player.IsNpc || !player.userID.IsSteamId()) return;
            if (!IsPlayerSetup(player.userID)) return;
            var playerData = GetPlayer(player.userID);
            if (playerData.AtEvent || playerData.Items == null || playerData.Items.Count == 0) return;
            if (playerData.Event != null && Events.ContainsKey(playerData.Event) && (Events[playerData.Event].give_items_back_automatically || config.auto_rekit))
            {
                RestoreItemsAndStats(player, true, config.auto_rekit);
            }
            else PrintToChat(player, string.Format(lang.GetMessage("UnclaimedItems", this, player.UserIDString), config.redeem_items_command));
        }

        void OnServerInitialized(bool initial)
        {
            cmd.AddChatCommand(config.redeem_items_command, this, "RestoreItemsCMD");
            if (config.event_cycle_timer > 0)
            {
                timer.Every(config.event_cycle_timer, () =>
                {
                    try
                    {
                        StartNextEvent();
                    }
                    catch { }
                });
            }
                
            if (RestoreUponDeath != null) FirstRespawnAfterGame = new List<BasePlayer>();

            WasAtEvent = new Dictionary<ulong, string>();

            for (int i = 1; i < 4; i++)
            {
                var message = lang.GetMessage($"JointAnnounceMsg_{i}", this);
                if (message != null && message != $"JointAnnounceMsg_{i}") ValidJoinMessages.Add($"JointAnnounceMsg_{i}");
            }
        }

        Dictionary<ulong, string> WasAtEvent;

        void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (!IsPlayerSetup(player.userID)) return;
            var playerData = GetPlayer(player.userID);
            if (playerData.Event == null || !Events.ContainsKey(playerData.Event) || !Events[playerData.Event].leaves_event_on_death) return;

            Interface.CallHook("OnPlayerDiedAtEvent", player, playerData.Event);                           

            if (RestoreUponDeath != null)
            {
                if (FirstRespawnAfterGame == null) FirstRespawnAfterGame = new List<BasePlayer>();
                if (!FirstRespawnAfterGame.Contains(player)) FirstRespawnAfterGame.Add(player);
            }
            EventInfo ei;
            if (Events.TryGetValue(playerData.Event, out ei) && ei.external_plugin_settings != null)
            {
                if (!ei.external_plugin_settings.canDropBackpack && !BagPreventDropList.Contains(player.userID))
                {
                    BagPreventDropList.Add(player.userID);
                    timer.Once(1f, () => BagPreventDropList.Remove(player.userID));
                }
                if (ei.leaves_event_on_death)
                {
                    if (!WasAtEvent.ContainsKey(player.userID))
                    {
                        WasAtEvent.Add(player.userID, playerData.Event);
                    }
                    RemoveFromEvent(player, playerData.Event);
                    timer.Once(1f, () => WasAtEvent.Remove(player.userID));
                }
            }
        }

        #endregion

        #region Chat commands

        [ConsoleCommand("ehban")]
        void BanPlayerCMD(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, perm_admin)) return;
            if (arg == null || arg.Args.Length == 0)
            {
                arg.ReplyWith(lang.GetMessage("EHBanUsage", this, player?.UserIDString ?? null));
                return;
            }

            var name = string.Join(" ", arg.Args);
            var target = name.IsNumeric() ? FindPlayerByID(name, player ?? null) : FindPlayerByName(name, player ?? null);
            if (target == null)
            {
                arg.ReplyWith($"No user matched {name}");
                return;
            }

            if (AddBan(target, "global")) arg.ReplyWith(string.Format(lang.GetMessage("BannedPlayer", this, player?.UserIDString ?? null), target.displayName));
        }

        [ChatCommand("ehban")]
        void BanPlayer(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, perm_admin)) return;
            if (args.Length == 0)
            {
                PrintToChat(player, lang.GetMessage("EHBanUsage", this, player.UserIDString));
                return;
            }
            var name = args.First();
            var target = name.IsNumeric() ? FindPlayerByID(name, player ?? null) : FindPlayerByName(name, player ?? null);
            if (target == null) return;
            if (AddBan(target, "global")) PrintToChat(player, string.Format(lang.GetMessage("BannedPlayer", this, player.UserIDString), target.displayName));
        }

        [ChatCommand("ehunban")]
        void UnbanPlayer(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, perm_admin)) return;
            if (args.Length == 0)
            {
                PrintToChat(player, lang.GetMessage("EHUnbanUsage", this, player.UserIDString));
                return;
            }
            var name = args.First();
            var target = name.IsNumeric() ? FindPlayerByID(name, player ?? null) : FindPlayerByName(name, player ?? null);
            if (target == null) return;
            var foundBan = false;
            foreach (var cat in pcdData.banned_players)
            {
                if (cat.Value.Contains(player.userID))
                {
                    RemoveBan(target, cat.Key);
                    foundBan = true;
                    PrintToChat(player, string.Format(lang.GetMessage("UnbannedPlayer", this, player.UserIDString), target.displayName));
                }                    
            }
            if (!foundBan) PrintToChat(player, string.Format(lang.GetMessage("NoBan", this, player.UserIDString), target.displayName));
        }

        #endregion

        #region Event vote

        public Dictionary<string, VoteInfo> Voting = new Dictionary<string, VoteInfo>();

        public class VoteInfo
        {
            public Timer _timer;
            public int votes;
            public int votes_required;
            public List<BasePlayer> voters = new List<BasePlayer>();
        }

        // <command> <event name>

        void StartEventVote(BasePlayer player, string command, string[] args)
        {
            if (!config.voting_enabled) return;

            if (config.minimum_players_online_vote > 0 && BasePlayer.activePlayerList?.Count < config.minimum_players_online_vote)
            {
                PrintToChat(player, $"There are not enough players online to initiate a vote. There must be at least {config.minimum_players_online_vote} players online.");
                return;
            }

            if (args == null || args.Length == 0)
            {
                PrintToChat(player, $"You can initiate a vote for an arena event by typing /{config.vote_start_cmd} <event name>. Available events: \n- {string.Join("\n- ", automatic_Events)}");
                return;
            }
            var eventName = automatic_Events.Where(x => x.Equals(string.Join(" ", args), StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            if (string.IsNullOrEmpty(eventName))
            {
                PrintToChat(player, $"Invalid event. Available events: \n{string.Join("\n", automatic_Events)}");
                return;
            }

            if (EventRunning.ContainsKey(eventName) && EventRunning[eventName])
            {
                PrintToChat(player, $"Event {eventName} is already running. Please wait for it to finish before calling a vote.");
                return;
            }

            VoteInfo vi;
            if (Voting.TryGetValue(eventName, out vi) && vi._timer != null && !vi._timer.Destroyed)
            {
                PrintToChat(player, $"Vote for {eventName} is already running [{vi.votes}/{vi.votes_required}]");
                return;
            }

            if (vi == null) Voting.Add(eventName, vi = new VoteInfo());
            vi.voters.Add(player);
            vi.votes++;
            var maxTime = config.max_vote_time;
            vi.votes_required = BasePlayer.activePlayerList.Count > 10 ? Convert.ToInt32(BasePlayer.activePlayerList.Count * 0.3) : 2; 
            PrintToChat($"A vote to start the event [<color=#42f105>{eventName}</color>] has been initiated by <color=#d6a500>{player.displayName}</color>. Type <color=#42f105>/{config.vote_cmd} {eventName}</color> to vote in favour [<color=#00d6ac>{vi.votes}</color>/<color=#00d6ac>{vi.votes_required}</color>]");
            vi._timer = timer.Every(1, () =>
            {
                maxTime--;
                if (maxTime <= 0)
                {
                    PrintToChat($"The vote for {eventName} has failed [{vi.votes}/{vi.votes_required}]");
                    RemoveVote(eventName, vi);
                }
                if (EventRunning.ContainsKey(eventName) && EventRunning[eventName])
                {
                    PrintToChat($"Vote for {eventName} has been cancelled as the game has started.");
                    RemoveVote(eventName, vi);
                }
                CheckVotes(eventName, vi);
            });
        }

        void VoteForEvent(BasePlayer player, string command, string[] args)
        {
            if (!config.voting_enabled) return;
            if (args == null || args.Length == 0)
            {
                if (Voting.Count == 0 || !IsActiveVote()) PrintToChat(player, $"There are no active votes. To initiate a vote, type /{config.vote_start_cmd} <event name>. \nAvailable events: \n- {string.Join("\n- ", automatic_Events)}");
                else
                {
                    List<string> activeVotes = Pool.GetList<string>();
                    activeVotes.AddRange(Voting.Where(x => x.Value._timer != null && !x.Value._timer.Destroyed).Select(y => y.Key));
                    PrintToChat(player, string.Format("There {0} {1} active {2} running.", activeVotes.Count > 1 ? "are" : "is", activeVotes.Count, activeVotes.Count > 1 ? "votes" : "vote"));
                    if (activeVotes.Count == 1) PrintToChat(player, $"Type /{config.vote_cmd} {activeVotes.First()} to vote in favour of the event.");
                    else
                    {
                        var vote_text = $"Type <color=#42f105>/{config.vote_cmd} <event name></color>. Active event votes:";
                        foreach (var name in activeVotes)
                        {
                            vote_text += $"\n- <color=#27fedd>{name}</color>";
                        }
                        PrintToChat(player, vote_text);
                    }
                    Pool.Free(ref activeVotes);
                }
                return;
            }

            var eventName = automatic_Events.Where(x => x.Equals(string.Join(" ", args), StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (string.IsNullOrEmpty(eventName))
            {
                PrintToChat(player, $"Invalid event. Available events: \n- {string.Join("\n- ", automatic_Events)}");
                return;
            }

            VoteInfo vi;
            if (Voting.TryGetValue(eventName, out vi))
            {
                if (vi.voters.Contains(player))
                {
                    PrintToChat(player, "You have already voted for this event.");
                    return;
                }
                vi.voters.Add(player);
                vi.votes++;
                PrintToChat($"<color=#42f105>{player.displayName}</color> has voted to initiate <color=#d6cc00>{eventName}</color> [<color=#00d6ac>{vi.votes}</color>/<color=#00d6ac>{vi.votes_required}</color>].");
                CheckVotes(eventName, vi);
            }
            else PrintToChat(player, "Event vote not available.");
        }

        bool IsActiveVote()
        {
            var eventName = Voting.Where(x => x.Value._timer != null && !x.Value._timer.Destroyed).FirstOrDefault().Key;
            return !string.IsNullOrEmpty(eventName);
        }

        void RemoveVote(string eventName, VoteInfo vi = null)
        {
            if (vi == null)
            {
                if (!Voting.TryGetValue(eventName, out vi)) return;
            }          
            if (vi._timer != null && !vi._timer.Destroyed) vi._timer.Destroy();
            Voting.Remove(eventName);
        }

        void CheckVotes(string eventName, VoteInfo vi = null)
        {
            if (vi == null)
            {
                if (!Voting.TryGetValue(eventName, out vi)) return;
            }
            if (vi.votes >= vi.votes_required)
            {
                PrintToChat($"The vote for {eventName} has been successful. Starting a new game.");
                Interface.CallHook("EMStartNextEvent", eventName);
                EMManuallyStarted(eventName);
                RemoveVote(eventName, vi);
            }            
        }

        #endregion

        #region monobehaviour

        void AddTempRestriction(BasePlayer player)
        {
            Puts("Adding temp restriction.");
            DestroyTempRestriction(player);
            player.gameObject.AddComponent<Temperature>();
        }

        void DestroyTempRestriction(BasePlayer player)
        {
            var gameObject = player.GetComponent<Temperature>();
            if (gameObject != null) GameObject.DestroyImmediate(gameObject);
        }

        public static float Temperature_Min;
        public static float Temperature_Max;

        public class Temperature : MonoBehaviour
        {
            private BasePlayer player;
            private float adjustmentDelay;

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                adjustmentDelay = Time.time + 1f;
            }

            public void FixedUpdate()
            {
                if (player == null) return;
                if (adjustmentDelay < Time.time)
                {
                    adjustmentDelay = Time.time + 1f;
                    DoChange();
                }
            }

            public void DoChange()
            {
                if (player == null || !player.IsConnected || !player.IsAlive()) return;
                if (player.metabolism.temperature.value < Temperature_Min) player.metabolism.temperature.value = Temperature_Min;
                else if (player.metabolism.temperature.value < Temperature_Max) player.metabolism.temperature.value = Temperature_Max;
            }

            private void OnDestroy()
            {
                enabled = false;
                CancelInvoke();
            }
        }

        #endregion

        #region Team management

        // ulong = TeamID.
        Dictionary<ulong, TeamData> TeamCache = new Dictionary<ulong, TeamData>();

        public class TeamData
        {
            public ulong leader;
            public List<ulong> members;
            public string name;
            public RelationshipManager.PlayerTeam _team;

            public TeamData(ulong leader, List<ulong> members, RelationshipManager.PlayerTeam _team, string name)
            {
                this.leader = leader;
                this.members = members;
                this._team = _team;
                this.name = name;
            }
        }

        void HandleEndGameRestoration()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                HandleTeam(player, false);
            }
        }

        void HandleTeam(BasePlayer player, bool joining)
        {
            if (joining) StoreTeamData(player);
            else RestoreTeamData(player);
        }

        TeamData FindExistingStoredTeam(BasePlayer player)
        {
            foreach (var kvp in TeamCache)
            {
                if (kvp.Value.members.Contains(player.userID))
                {
                    return kvp.Value;
                }
            }
            return null;
        }

        void StoreTeamData(BasePlayer player)
        {
            if (player.Team == null) return;

            TeamData teamData = FindExistingStoredTeam(player);
            if (teamData == null)
            {
                List<ulong> members = Pool.GetList<ulong>();
                members.AddRange(player.Team.members);
                TeamCache.Add(player.Team.teamID, teamData = new TeamData(player.Team.teamLeader, members, player.Team, player.Team.teamName));
            }

            player.Team.RemovePlayer(player.userID);
        }

        void RestoreTeamData(BasePlayer player)
        {
            var teamData = FindExistingStoredTeam(player);
            if (teamData == null) return;
            if (player.Team != null)
            {
                if (player.Team == teamData._team) return;
                player.Team.RemovePlayer(player.userID);
            }
            if (teamData._team == null || teamData._team.members.Count == 0)
            {
                //Puts("Test4");
                //var team = ServerInstance.CreateTeam();
                //team.teamLeader = player.userID;
                //team.AddPlayer(player);
                //team.teamName = teamData.name;
                //Facepunch.Rust.Analytics.Azure.OnTeamChanged("created", team.teamID, player.userID, player.userID, team.members);
                //teamData._team = team;
                //Puts("Test4.5");
                //return;

                PlayerTeam playerTeam = ServerInstance.CreateTeam();
                PlayerTeam playerTeam2 = playerTeam;
                playerTeam2.teamLeader = player.userID;
                playerTeam2.AddPlayer(player);
                Facepunch.Rust.Analytics.Azure.OnTeamChanged("created", playerTeam2.teamID, player.userID, player.userID, playerTeam2.members);

                teamData._team = playerTeam2;
                return;
            }
            teamData._team.AddPlayer(player);
            if (teamData.leader == player.userID) teamData._team.SetTeamLeader(player.userID);
        }

        void SetLeaders()
        {
            foreach (var kvp in TeamCache)
            {
                if (kvp.Value._team == null) continue;
                if (kvp.Value._team.teamLeader != kvp.Value.leader && kvp.Value.members.Contains(kvp.Value.leader)) kvp.Value._team.SetTeamLeader(kvp.Value.leader);
            }
        }

        void ClearTeamCache()
        {
            foreach (var kvp in TeamCache)
            {
                if (kvp.Value.members != null) Pool.FreeList(ref kvp.Value.members);
            }
            Puts("Clearing team data");
            TeamCache.Clear();
        }

        object OnTeamCreate(BasePlayer player)
        {
            if (IsPlayerSetup(player.userID))
            {
                var playerData = GetPlayer(player.userID);
                EventInfo ei;
                if (!string.IsNullOrEmpty(playerData.Event) && Events.TryGetValue(playerData.Event, out ei) && ei.participants.Contains(player) && !ei.allow_Teams)
                {
                    PrintToChat(player, lang.GetMessage("PreventTeamCreation", this, player.UserIDString));
                    return true;
                }
            }
            return null;
        }


        #endregion

        #region Discord

        public void SendDiscordMsg(string message)
        {
            if (string.IsNullOrEmpty(config.discordSettings.webhook) || !config.discordSettings.webhook.StartsWith("https://discord.com/api/webhooks/")) return;
            try
            {
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Content-Type", "application/json");
                string payload = "{\"content\": \"" + message + "\"}";
                webrequest.Enqueue(config.discordSettings.webhook, payload, (code, response) =>
                {
                    if (code != 200 && code != 204)
                    {
                        if (response == null)
                        {
                            PrintWarning($"Discord didn't respond. Error Code: {code}. Try removing escape characters from your string such as \\n.");
                        }
                    }
                }, this, Core.Libraries.RequestMethod.POST, headers);
            }
            catch (Exception e)
            {
                Puts($"Failed. Error: {e?.Message}.\nIf this error was related to Bad Request, you may need to remove any escapes from your lang such as \\n.");
            }

        }

        #endregion
    }
}