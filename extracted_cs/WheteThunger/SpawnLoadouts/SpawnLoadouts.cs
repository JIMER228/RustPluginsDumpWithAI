using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace Oxide.Plugins
{
    [Info("Spawn Loadouts", "WhiteThunder", "0.1.1")]
    [Description("Allows players to make their own spawn loadouts.")]
    internal class SpawnLoadouts : CovalencePlugin
    {
        #region Fields

        const string PermissionSave = "spawnloadouts.save";
        const string PermissionSetDefault = "spawnloadouts.setdefault";
        const string PermissionGetLoadout = "spawnloadouts.getloadout";

        const string DataDirectory = "SpawnLoadouts/";

        private Configuration _pluginConfig;

        #endregion

        private void Init()
        {
            permission.RegisterPermission(PermissionSave, this);
            permission.RegisterPermission(PermissionSetDefault, this);
            permission.RegisterPermission(PermissionGetLoadout, this);
        }

        #region Commands

        [Command("loadout")]
        void LoadoutCommand(IPlayer player, string command, string[] args)
        {
            if (player.IsServer)
                return;

            if (args.Length == 0)
            {
                ReplyToPlayer(player, "Error.Syntax");
                return;
            }

            var basePlayer = player.Object as BasePlayer;

            switch (args[0].ToLower())
            {
                //saves players current loadout with command '/loadout save'.
                case "save":
                    if (!player.HasPermission(PermissionSave))
                    {
                        ReplyToPlayer(player, "Error.NoPermission");
                        return;
                    }

                    var loadout = Loadout.BuildFromPlayer(basePlayer, _pluginConfig.DisallowedItems);
                    ReplyToPlayer(player, "Command.Save.Success");
                    SavePlayerLoadout(basePlayer, loadout);
                    return;

                //sets default for all players when they don't have a player data file '/loadout setdefault'.
                case "setdefault":
                    if (!player.HasPermission(PermissionSetDefault))
                    {
                        ReplyToPlayer(player, "Error.NoPermission");
                        return;
                    }
                    _pluginConfig.DefaultLoadout = Loadout.BuildFromPlayer(basePlayer, _pluginConfig.DisallowedItems);
                    SaveConfig();
                    ReplyToPlayer(player, "Command.SetDefault.Success");
                    return;

                case "reset":
                    if (!player.HasPermission(PermissionSave))
                    {
                        ReplyToPlayer(player, "Error.NoPermission");
                        return;
                    }
                    SavePlayerLoadout(basePlayer, null);
                    ReplyToPlayer(player, "Command.Reset.Success");
                    return;

                default:
                    ReplyToPlayer(player, "Error.Syntax");
                    return;
            }
        }

        #endregion

        #region Hooks

        void OnPlayerSpawn(BasePlayer player)
        {
            NextTick(() =>
            {
                if (player == null || player.IsDead())
                    return;

                if (permission.UserHasPermission(player.UserIDString, PermissionGetLoadout))
                {
                    GiveLoadoutToPlayer(player);
                }
            });
        }

        //Set player inventory when player clicks on respawn.
        void OnPlayerRespawned(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, PermissionGetLoadout))
            {
                GiveLoadoutToPlayer(player);
            }
        }

        #endregion

        #region Methods

        //Reads player file and returns this as a object.
        private Loadout GetLoadoutForPlayer(BasePlayer player)
        {
            var filename = DataDirectory + player.UserIDString;

            if (!Interface.Oxide.DataFileSystem.ExistsDatafile(filename))
                return _pluginConfig.DefaultLoadout;

            var loadout = Interface.Oxide.DataFileSystem.ReadObject<Loadout>(filename);
            return loadout ?? _pluginConfig.DefaultLoadout;
        }

        //Rewrites the player file.
        void SavePlayerLoadout(BasePlayer player, Loadout loadout)
        {
            Interface.Oxide.DataFileSystem.WriteObject<Loadout>(DataDirectory + player.UserIDString, loadout);
        }

        //Clears players inventory and gives loadout of the player
        public void GiveLoadoutToPlayer(BasePlayer player)
        {
            var loadout = GetLoadoutForPlayer(player);
            if (loadout == null)
                return;

            player.inventory.Strip();
            loadout.GiveToPlayer(player);
        }

        #endregion

        #region Classes

        public class PlayerLoadout
        {
            public string _userId;
            public string _username;
            public List<LoadoutItem> items = new List<LoadoutItem>();

            public PlayerLoadout()
            {
            }

            [JsonConstructor]
            public PlayerLoadout(string userId, string userName, List<LoadoutItem> Items)
            {
                _userId = userId;
                _username = userName;
                items = Items;
            }
        }

        #endregion

        internal class Loadout
        {
            [JsonProperty("MainItems", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public LoadoutItem[] MainItems;

            [JsonProperty("BeltItems", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public LoadoutItem[] BeltItems;

            [JsonProperty("WornItems", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public LoadoutItem[] WornItems;

            public static Loadout BuildFromPlayer(BasePlayer player, string[] disallowedItems)
            {
                var inventory = player.inventory;
                var mainItems = GetItemsFromContainer(inventory.containerMain, 24, disallowedItems);
                var beltItems = GetItemsFromContainer(inventory.containerBelt, 6, disallowedItems);
                var wornItems = GetItemsFromContainer(inventory.containerWear, 7, disallowedItems);

                if (mainItems == null && beltItems == null && wornItems == null)
                    return null;

                var loadout = new Loadout()
                {
                    MainItems = mainItems,
                    BeltItems = beltItems,
                    WornItems = wornItems,
                };

                return loadout;
            }

            private static LoadoutItem[] GetItemsFromContainer(ItemContainer container, int capacity, string[] disallowedItems)
            {
                var itemList = new List<LoadoutItem>();

                // Reduce capacity to known amount to ignore items in invisible slots.
                var actualCapacity = Math.Min(container.capacity, capacity);

                for (var slot = 0; slot < actualCapacity; slot++)
                {
                    var item = container.GetSlot(slot);
                    if (item != null && !disallowedItems.Contains(item.info.shortname))
                        itemList.Add(LoadoutItem.FromItem(item));
                }

                return itemList.Count > 0
                    ? itemList.ToArray()
                    : null;
            }

            private static void AddItemsToContainer(ItemContainer container, LoadoutItem[] itemList)
            {
                foreach (var itemData in itemList)
                {
                    var item = itemData.ToItem();
                    if (item != null)
                        item.MoveToContainer(container, item.position);
                }
            }

            public void GiveToPlayer(BasePlayer player)
            {
                var inventory = player.inventory;

                if (MainItems != null)
                    AddItemsToContainer(inventory.containerMain, MainItems);

                if (BeltItems != null)
                    AddItemsToContainer(inventory.containerBelt, BeltItems);

                if (WornItems != null)
                    AddItemsToContainer(inventory.containerWear, WornItems);
            }
        }

        internal class LoadoutItem
        {
            [JsonProperty("ItemShortName")]
            public string ItemShortName;

            [JsonProperty("Amount", DefaultValueHandling = DefaultValueHandling.Ignore)]
            [DefaultValue(1)]
            public int Amount = 1;

            [JsonProperty("SkinId", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public ulong SkinId = 0;

            [JsonProperty("Slot", DefaultValueHandling = DefaultValueHandling.Ignore)]
            [DefaultValue(-1)]
            public int Slot = -1;

            [JsonProperty("ChildItems", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string[] ChildItems;

            public static LoadoutItem FromItem(Item item)
            {
                if (item.amount == 0)
                    return null;

                var loadoutItem = new LoadoutItem();

                loadoutItem.ItemShortName = item.info.shortname;
                loadoutItem.Amount = item.amount;
                loadoutItem.SkinId = item.skin;
                loadoutItem.Slot = item.position;

                if (item.contents != null)
                {
                    var childItemList = new List<string>();
                    for (var slot = 0; slot < item.contents.capacity; slot++)
                    {
                        var childItem = item.contents.GetSlot(slot);
                        if (childItem != null)
                            childItemList.Add(childItem.info.shortname);
                    }

                    if (childItemList.Count > 0)
                        loadoutItem.ChildItems = childItemList.ToArray();
                }

                return loadoutItem;
            }

            public Item ToItem()
            {
                if (Amount <= 0)
                    // TODO: Error logging
                    return null;

                var item = ItemManager.CreateByName(ItemShortName, Amount, SkinId);
                if (item == null)
                    // TODO: Error logging
                    return null;

                item.position = Slot;

                // TODO: Support child items for other types of items
                if (item.contents != null && ChildItems != null)
                {
                    foreach (var childItemShortName in ChildItems)
                    {
                        var childItem = ItemManager.CreateByName(childItemShortName);
                        if (childItem == null)
                            // TODO: Error logging
                            continue;

                        if (!childItem.MoveToContainer(item.contents))
                            childItem.Remove();
                    }
                }

                var weaponEntity = item.GetHeldEntity() as BaseProjectile;
                if (weaponEntity != null)
                {
                    if (weaponEntity.primaryMagazine != null)
                    {
                        // TODO: Support other ammo types
                        weaponEntity.primaryMagazine.contents = weaponEntity.primaryMagazine.capacity;
                    }
                }

                return item;
            }
        }

        #region Configuration

        private Configuration GetDefaultConfig() => new Configuration();

        private class Configuration : SerializableConfiguration
        {
            [JsonProperty("DefaultLoadout")]
            public Loadout DefaultLoadout = new Loadout()
            {
                WornItems = new LoadoutItem[]
                {
                    new LoadoutItem()
                    {
                        ItemShortName = "roadsign.gloves",
                    },
                    new LoadoutItem()
                    {
                        ItemShortName = "roadsign.kilt",
                    },
                    new LoadoutItem()
                    {
                        ItemShortName = "metal.facemask",
                    },
                },
                BeltItems = new LoadoutItem[]
                {
                    new LoadoutItem()
                    {
                        ItemShortName = "rifle.ak",
                        Amount = 1,
                    },
                },
                MainItems = new LoadoutItem[]
                {
                    new LoadoutItem()
                    {
                        ItemShortName = "ammo.rifle",
                        Amount = 74,
                        ChildItems = new string[]
                        {
                            "weapon.mod.holosight"
                        }
                    },
                },
            };

            [JsonProperty("DisallowedItems")]
            public string[] DisallowedItems = new string[0];
        }

        #endregion

        #region Configuration Boilerplate

        private class SerializableConfiguration
        {
            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonHelper.Deserialize(ToJson()) as Dictionary<string, object>;
        }

        private static class JsonHelper
        {
            public static object Deserialize(string json) => ToObject(JToken.Parse(json));

            private static object ToObject(JToken token)
            {
                switch (token.Type)
                {
                    case JTokenType.Object:
                        return token.Children<JProperty>()
                                    .ToDictionary(prop => prop.Name,
                                                  prop => ToObject(prop.Value));

                    case JTokenType.Array:
                        return token.Select(ToObject).ToList();

                    default:
                        return ((JValue)token).Value;
                }
            }
        }

        private bool MaybeUpdateConfig(SerializableConfiguration config)
        {
            var currentWithDefaults = config.ToDictionary();
            var currentRaw = Config.ToDictionary(x => x.Key, x => x.Value);
            return MaybeUpdateConfigDict(currentWithDefaults, currentRaw);
        }

        private bool MaybeUpdateConfigDict(Dictionary<string, object> currentWithDefaults, Dictionary<string, object> currentRaw)
        {
            bool changed = false;

            foreach (var key in currentWithDefaults.Keys)
            {
                object currentRawValue;
                if (currentRaw.TryGetValue(key, out currentRawValue))
                {
                    var defaultDictValue = currentWithDefaults[key] as Dictionary<string, object>;
                    var currentDictValue = currentRawValue as Dictionary<string, object>;

                    if (defaultDictValue != null)
                    {
                        if (currentDictValue == null)
                        {
                            currentRaw[key] = currentWithDefaults[key];
                            changed = true;
                        }
                        else if (MaybeUpdateConfigDict(defaultDictValue, currentDictValue))
                            changed = true;
                    }
                }
                else
                {
                    currentRaw[key] = currentWithDefaults[key];
                    changed = true;
                }
            }

            return changed;
        }

        protected override void LoadDefaultConfig() => _pluginConfig = GetDefaultConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _pluginConfig = Config.ReadObject<Configuration>();
                if (_pluginConfig == null)
                {
                    throw new JsonException();
                }

                if (MaybeUpdateConfig(_pluginConfig))
                {
                    LogWarning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch
            {
                LogWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Log($"Configuration changes saved to {Name}.json");
            Config.WriteObject(_pluginConfig, true);
        }

        #endregion

        #region Localization

        private void ReplyToPlayer(IPlayer player, string messageName, params object[] args) =>
            player.Reply(string.Format(GetMessage(player, messageName), args));

        private void ChatMessage(BasePlayer player, string messageName, params object[] args) =>
            player.ChatMessage(string.Format(GetMessage(player.IPlayer, messageName), args));

        private string GetMessage(IPlayer player, string messageName, params object[] args) =>
            GetMessage(player.Id, messageName, args);

        private string GetMessage(string playerId, string messageName, params object[] args)
        {
            var message = lang.GetMessage(messageName, this, playerId);
            return args.Length > 0 ? string.Format(message, args) : message;
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Error.NoPermission"] = "You don't have the permsission to do that.",
                ["Error.Syntax"] = "Error: Invalid syntax.",
                ["Command.SetDefault.Success"] = "Default loadout has <color=#bfff00>succesfully been set!</color>",
                ["Command.Save.Success"] = "Loadout was <color=#bfff00>sucessfully saved!</color>",
                ["Command.Reset.Success"] = "Loadout was <color=#bfff00>sucessfully reset!</color>",
            }, this);
        }

        #endregion
    }
}
