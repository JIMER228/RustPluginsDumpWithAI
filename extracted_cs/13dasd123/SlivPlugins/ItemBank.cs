using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using VLB;

namespace Oxide.Plugins
{
    [Info("Item Bank", "Orange", "2.1.4")]
    [Description("https://rustworkshop.space/resources/item-bank.27/")]
    public class ItemBank : RustPlugin
    {
        #region Vars

        private const string permModerator = "itembank.moderator";

        #endregion
        
        #region Oxide Hooks

        private void Init()
        {
            permission.RegisterPermission(permModerator, this);
            BuildUI();

            foreach (var command in config.commands)
            {
                cmd.AddChatCommand(command, this, nameof(cmdControlChat));
            }

            foreach (var bank in config.banks)
            {
                if (string.IsNullOrEmpty(bank.permission) == false &&
                    permission.PermissionExists(bank.permission) == false)
                {
                    permission.RegisterPermission(bank.permission, this);
                }

                foreach (var extra in bank.extraCapacity.Keys)
                {
                    if (string.IsNullOrEmpty(extra) == false && permission.PermissionExists(extra) == false)
                    {
                        permission.RegisterPermission(extra, this);
                    }
                }
            }

            cmd.AddConsoleCommand("itembank", this, nameof(cmdControlConsole));
        }

        private void OnServerInitialized()
        {
            Data.Load(this);
        }

        private void Unload()
        {
            var values = Data.GetAllValues;
            Data.Unload();

            foreach (var value in values)
            {
                foreach (var bank in value.banks)
                {
                    if (bank.inventory != null)
                    {
                        bank.inventory.SetFlag(ItemContainer.Flag.IsLocked, true);
                        bank.inventory.capacity = 0;
                        bank.inventory.MarkDirty();
                    }
                }
            }
        }

        #endregion

        #region Commands

        private void cmdControlChat(BasePlayer player, string command, string[] args)
        {
            if (args == null)
            {
                Message.Send(player, MessageKey.Usage);
                return;
            }

            switch (args.Length)
            {
                default:
                    OpenBank(player, "any");
                    break;
                
                case 1:
                    OpenBank(player,  args[0]);
                    break;
                
                case 2:
                    if (permission.UserHasPermission(player.UserIDString, permModerator) == true)
                    {
                        var name = args[0];
                        var target = args[1];
                        OpenSideBank(player, name, target);
                    }
                    else
                    {
                        OpenBank(player,  args[0]);
                    }
                    break;
            }
            
        }

        private void cmdControlConsole(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin == false)
            {
                SendReply(arg, "No permission");
                return;
            }

            var args = arg.Args;
            if (args == null || args.Length < 1)
            {
                SendReply(arg, $"Usage: itembank <convert/wipe> <opt: name>");
                return;
            }

            var action = args[0].ToLower();
            switch (action)
            {
                case "convert":
                    SendReply(arg, "Converting old banks...");
                    ConvertOld();
                    SendReply(arg, "Converting was completed!");
                    return;

                case "wipe":
                    if (args.Length < 2)
                    {
                        SendReply(arg, "You need to specify bank name!");
                        return;
                    }

                    var name = args[1];
                    SendReply(arg, $"Wiping all banks with name '{name}'...");

                    foreach (var value in Data.GetAllValues)
                    {
                        value.banks.RemoveAll(x => string.Equals(x.shortname, name));
                    }

                    SendReply(arg, $"All banks with name '{name}' was wiped successfully!");
                    Data.Save();
                    return;

                default:
                    SendReply(arg, "Unknown action!");
                    break;
            }
        }

        #endregion

        #region Core

        private void OpenBank(BasePlayer player, string name)
        {
            if (Interface.CallHook("CanBank", player) != null)
            {
                Message.Send(player, MessageKey.NotAvailable);
                return;
            }
            
            if (name == "any")
            {
                name = config.banks.FirstOrDefault(x => permission.UserHasPermission(player.UserIDString, x.permission))?.shortname;
            }

            Message.Send(player, MessageKey.OpeningBankChat, "{name}", name);
            var pendingSlots = 0;
            var data = Data.Get(player.UserIDString, true);
            var bank = data.banks.FirstOrDefault(x => string.Equals(x.shortname, name));
            var definition = config.banks.FirstOrDefault(x => string.Equals(x.shortname, name));

            // When bank not exists
            if (bank == null) 
            {
                if (definition == null)
                {
                    Message.Send(player, MessageKey.BankNotFound, "{name}", name);
                    return;
                }
                else
                {
                    if (permission.UserHasPermission(player.UserIDString, definition.permission) == false)
                    {
                        Message.Send(player, MessageKey.NoPermission);
                        return;
                    }
                    
                    bank = new BankData();
                    bank.shortname = definition.shortname;
                    data.banks.Add(bank);
                    
                    var message = Message.GetMessage(MessageKey.OpeningBankUI, player.UserIDString, "{name}", name);
                    if (string.IsNullOrEmpty(message) == false)
                    {
                        var ui = buildedUI.Replace("%TEXT%", message);
                        TemporaryUI.Show(player, ui, elemMain, 3f);
                    }
                }
            }
            // When bank exists
            else 
            {   
                if (definition == null)
                {
                    // ignored
                }
                else 
                {
                    if (permission.UserHasPermission(player.UserIDString, definition.permission) == false)
                    {
                        Message.Send(player, MessageKey.NoPermission);
                        definition = null;
                        
                        var message = Message.GetMessage(MessageKey.NoInsertUI, player.UserIDString, "{name}", name);
                        if (string.IsNullOrEmpty(message) == false)
                        {
                            var ui = buildedUI.Replace("%TEXT%", message);
                            TemporaryUI.Show(player, ui, elemMain, 3f);
                        }
                    }
                }
            }

            bank.Load(definition);

            if (definition != null)
            {
                var extraSlots = 0;

                foreach (var extra in definition.extraCapacity)
                {
                    if (permission.UserHasPermission(player.UserIDString, extra.Key) == false)
                    {
                        continue;
                    }

                    if (extra.Value > extraSlots)
                    {
                        extraSlots = extra.Value;
                    }
                }

                pendingSlots = definition.capacity + extraSlots;
            }

            bank.inventory.capacity = Math.Max(bank.inventory.itemList.Count, pendingSlots);
            OpenContainer(player, bank.inventory);
        }

        private void OpenSideBank(BasePlayer player, string name, string target)
        {
            player.ChatMessage($"Opening bank of other player... ({target})");
            
            if (target.StartsWith("765") == false)
            {
                player.ChatMessage($"Trying to find player by name... ({target})");
                var targetPlayer = BasePlayer.Find(target) ?? BasePlayer.FindSleeping(target);
                if (targetPlayer == null)
                {
                    player.ChatMessage($"Failed to find player! ({target})");
                    return;
                }

                target = targetPlayer.UserIDString;
            }
            
            var data = Data.Get(target, false);
            if (data == null)
            {
                player.ChatMessage($"That player doesn't have any banks! ({target})");
                return;
            }

            var bank = data.banks.FirstOrDefault(x => x.shortname == name);
            if (bank == null)
            {
                player.ChatMessage($"That player doesn't have bank with name ({name}, {target})");
                return;
            }
            
            player.ChatMessage($"Opening inventory... ({target})");
            bank.Load(null);
            bank.inventory.capacity = 42;
            OpenContainer(player, bank.inventory);
        }

        private void OpenContainer(BasePlayer player, ItemContainer container, BaseEntity entity = null, string panel = "generic_resizable")
        {
            player.EndLooting();
            var loot = player.inventory.loot;
            if (entity.IsValid() == false)
            {
                var position = player.transform.position - new Vector3(0, 500, 0);
                entity = GameManager.server.CreateEntity("assets/prefabs/visualization/sphere.prefab", position);
                if (entity != null)
                {
                    entity.enableSaving = false;
                    entity.Spawn();
                }
            }

            container.entityOwner = entity;
            container.playerOwner = player;

            timer.Once(0.2f, () =>
            {
                if (player == null)
                {
                    return;
                }

                loot.Clear();
                loot.PositionChecks = false;
                loot.entitySource = entity;
                loot.itemSource = (Item) null;
                loot.MarkDirty();
                loot.AddContainer(container);
                loot.SendImmediate();
                player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", panel);
                player.SendNetworkUpdateImmediate();
            });
        }

        private static bool CanAcceptItem(Item item, ItemContainer container, BankDefinition definition)
        {
            var obj = (object) null;

            var parent = item.GetRootContainer();
            if (parent == container || parent == null)
            {
                return true;
            }

            if (definition != null)
            {
                if (definition.whitelist.Length > 0)
                {
                    foreach (var key in definition.whitelist)
                    {
                        if (item.info.category.ToString() == key)
                        {
                            obj = true;
                            break;
                        }

                        if (item.info.shortname == key)
                        {
                            obj = true;
                            break;
                        }

                        if (item.skin.ToString() == key)
                        {
                            obj = true;
                            break;
                        }

                        if (item.name != null && item.name.Contains(key))
                        {
                            obj = true;
                            break;
                        }
                    }

                    if (obj == null)
                    {
                        obj = false;
                    }
                }

                if (definition.blacklist.Length > 0)
                {
                    foreach (var key in definition.blacklist)
                    {
                        if (item.info.category.ToString() == key)
                        {
                            obj = false;
                            break;
                        }

                        if (item.info.shortname == key)
                        {
                            obj = false;
                            break;
                        }

                        if (item.skin.ToString() == key)
                        {
                            obj = false;
                            break;
                        }

                        if (item.name != null && item.name.Contains(key))
                        {
                            obj = false;
                            break;
                        }
                    }

                    if (obj == null)
                    {
                        obj = true;
                    }
                }
            }
            else
            {
                obj = false;
            }

            var flagBool = obj == null ? true : (bool) obj;
            if (flagBool == false)
            {
                var player = item.GetOwnerPlayer() ?? container.GetOwnerPlayer();
                if (player != null)
                {
                    var ui = buildedUI.Replace("%TEXT%", Message.GetMessage(MessageKey.BlockedItemUI));
                    TemporaryUI.Show(player, ui, elemMain, 3);
                }
            }

            return flagBool;
        }

        #endregion

        #region Graphical Interface

        private static string buildedUI;
        private const string elemMain = "itembank.panel";

        private void BuildUI()
        {
            var container = new CuiElementContainer
            {
                new CuiElement
                {
                    Name = elemMain,
                    Parent = "Overlay",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = "%TEXT%",
                            Align = TextAnchor.LowerCenter,
                        },
                        new CuiOutlineComponent
                        {
                            Color = config.outlineColor,
                            Distance = config.outlineDistance,
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-200 40",
                            OffsetMax = "180 240"
                        }
                    }
                }
            };
            buildedUI = container.ToString();
        }

        #endregion

        #region Classes

        private static ConfigDefinition config = new ConfigDefinition();

        private class ConfigDefinition
        {
            [JsonProperty("Commands")] 
            public string[] commands =
            {
                "bank", "ibank", "storage"
            };

            [JsonProperty("Banks")] 
            public BankDefinition[] banks =
            {
                new BankDefinition
                {
                    shortname = "single",
                    permission = "itembank.single",
                    capacity = 1,
                    extraCapacity = new Dictionary<string, int>()
                },
                new BankDefinition
                {
                    shortname = "weapons",
                    permission = "itembank.weapons",
                    capacity = 12,
                    whitelist = new[] {"Weapons"},
                    blacklist = { }
                },
                new BankDefinition
                {
                    shortname = "vip",
                    permission = "itembank.vip",
                    capacity = 42,
                },
            };

            [JsonProperty(PropertyName = "Outline color")]
            public string outlineColor = "0 0 0 1";

            [JsonProperty(PropertyName = "Outline distance")]
            public string outlineDistance = "1.0 -0.5";
        }

        private class BankDefinition
        {
            [JsonProperty("Bank shortname")] 
            public string shortname = "default";

            [JsonProperty("Permission")] 
            public string permission = "itembank.default";

            [JsonProperty("Capacity")] 
            public int capacity = 12;

            [JsonProperty("Max stack size per item")]
            public int maxStackSize = 0;

            [JsonProperty("Extra capacity per permission", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, int> extraCapacity = new Dictionary<string, int>
            {
                {"itembank.default.extra.6", 6},
                {"itembank.default.extra.12", 12},
            };

            [JsonProperty("Whitelist")] 
            public string[] whitelist = {"wood", "Weapon", "Ammunition"};

            [JsonProperty("Blacklist")] 
            public string[] blacklist =
                {"explosive.timed", "ammo.rocket.basic", "Resources", "12345"};
        }

        private class DataEntry
        {
            [JsonProperty("Banks")]
            public List<BankData> banks = new List<BankData>();
        }

        private class BankData
        {
            [JsonIgnore] 
            public ItemContainer inventory;

            [JsonProperty("Shortname")] 
            public string shortname;

            [JsonProperty("Inventory in bytes")] 
            public byte[] inventoryData;

            [JsonProperty("Items in bytes")] 
            public byte[][] itemsData;

            public void Load(BankDefinition definition)
            {
                if (inventory == null)
                {
                    inventory = new ItemContainer();
                    inventory.isServer = true;
                    inventory.ServerInitialize(null, 42);
                    inventory.GiveUID();

                    if (inventoryData != null)
                    {
                        var data = ProtoBuf.ItemContainer.Deserialize(inventoryData);
                        itemsData = data.contents.Select(x => x.ToProtoBytes()).ToArray();
                        inventoryData = null;
                    }

                    if (itemsData != null)
                    {
                        foreach (var value in itemsData)
                        {
                            var protoItem = ProtoBuf.Item.Deserialize(value);
                            var item = ItemManager.Load(protoItem, null, true);
                            if (item != null)
                            {
                                inventory.Insert(item);
                            }
                        }
                    }
                    
                    foreach (var item in inventory.itemList)
                    {
                        item.OnItemCreated();
                        item.MarkDirty();
                        item.GetHeldEntity()?.SendNetworkUpdate();
                    }

                    inventory.onDirty += () =>
                    {
                        itemsData = inventory.itemList.Select(x => x.Save(true).ToProtoBytes()).ToArray();
                    };
                }

                if (definition != null && definition.maxStackSize > 0)
                {
                    inventory.maxStackSize = definition.maxStackSize;
                }
                
                inventory.canAcceptItem = (item, i) =>
                {
                    return CanAcceptItem(item, inventory, definition);
                };
            }
        }

        #endregion

        #region Data v2.2

        private void OnServerSave()
        {
            timer.Once(Data.SaveDelay, () => { Data.Save(); });
        }

        private class Data
        {
            private static bool valid;
            private static Plugin plugin;
            private static Definition definition;
            private static string fileName;
            private static string pluginName = "Unknown Plugin";
            public const float SaveDelay = 5f;
            public static DataEntry[] GetAllValues => definition.AllValues.Values.Select(x => x.value).ToArray();

            private class Definition
            {
                [JsonProperty("Values")] public Dictionary<string, Entry> AllValues = new Dictionary<string, Entry>();

                [JsonIgnore] public Dictionary<string, Entry> CachedValues = new Dictionary<string, Entry>();
            }

            private class Entry
            {
                [JsonProperty("Key")] public string key = string.Empty;

                [JsonProperty("Content")] public DataEntry value = new DataEntry();
            }

            public static void Load(Plugin Plugin)
            {
                if (definition != null)
                {
                    Debug.LogError($"[{pluginName}] Trying to load data twice!");
                    return;
                }

                valid = false;

                if (Plugin == null)
                {
                    Debug.LogError($"[Data] Failed to load data because plugin value was null...");
                    return;
                }

                plugin = Plugin;
                pluginName = plugin.Name;
                fileName = $"{pluginName}\\Data";

                try
                {
                    definition = Interface.Oxide.DataFileSystem.ReadObject<Definition>(fileName) ?? new Definition();
                }
                catch
                {
                    Debug.LogWarning($"[{pluginName}] Data was not loaded because its corrupted!");
                    return;
                }

                valid = definition != null;
                Debug.Log($"[{pluginName}] Data was loaded successfully (Values: {definition.AllValues.Count})");
            }

            public static void Unload()
            {
                Save();
                plugin = null;
                definition = null;
            }

            public static void Save()
            {
                if (valid == false)
                {
                    Debug.LogWarning($"[{pluginName}] Can't save data because its not valid!");
                    return;
                }

                definition.CachedValues.Clear();
                Debug.Log($"[{pluginName}] Saving data...");
                Interface.Oxide.DataFileSystem.WriteObject(fileName, definition);
                Debug.Log($"[{pluginName}] Saving was successful! (Values: {definition.AllValues.Count})");
            }

            public static void Wipe()
            {
                if (valid == false)
                {
                    Debug.LogWarning($"[{pluginName}] Can't wipe data because its not valid!");
                    return;
                }

                definition.AllValues.Clear();
                definition.CachedValues.Clear();
                Debug.Log($"[{pluginName}] Data was wiped successfully!");
            }

            public static DataEntry Get(string key, bool createIfMissing)
            {
                if (valid == false)
                {
                    Debug.LogWarning($"[{pluginName}] Can't get value because DATA is not valid!");
                    return null;
                }

                var entry = (Entry) null;
                if (definition.CachedValues.TryGetValue(key, out entry) == true)
                {
                    return entry.value;
                }

                if (definition.AllValues.TryGetValue(key, out entry) == false)
                {
                    if (createIfMissing == false)
                    {
                        return null;
                    }

                    entry = new Entry();
                    entry.key = key;
                    definition.AllValues.Add(key, entry);
                }

                definition.CachedValues.Add(key, entry);
                return entry.value;
            }
        }

        #endregion

        #region Configuration v2.1

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigDefinition>();
                if (config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                for (var i = 0; i < 3; i++)
                {
                    PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                }

                LoadDefaultConfig();
                return;
            }

            ValidateConfig();
            SaveConfig();
        }

        private void ValidateConfig()
        {
            if (Interface.Oxide.CallHook("OnConfigValidate") != null)
            {
                PrintWarning("Using default configuration...");
                config = new ConfigDefinition();
            }
        }

        protected override void LoadDefaultConfig()
        {
            config = new ConfigDefinition();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

        #region Language System v2.2

        protected override void LoadDefaultMessages()
        {
            Message.Load(lang, this);
        }

        private enum MessageKey
        {
            NotAvailable,
            NoPermission,
            BankNotFound,
            OpeningBankChat,
            OpeningBankUI,
            BlockedItemChat,
            BlockedItemUI,
            NoInsertChat,
            NoInsertUI,
            Usage,
        }

        private class Message
        {
            private static Dictionary<MessageKey, object> langMessages = new Dictionary<MessageKey, object>
            {
                {MessageKey.Usage, "/bank NAME"},
                {MessageKey.NotAvailable, "<color=#ff0000>You can't do that right now</color>"},
                {MessageKey.BankNotFound, "<color=#ff0000>Can't find bank</color> with name '<color=#ffff00>{name}</color>'"},
                {MessageKey.NoPermission, "<color=#ff0000>You don't have permission to do that!</color>"},
                {MessageKey.OpeningBankChat, "Bank <color=#ffff00>{name}</color> is opening..."},
                {MessageKey.OpeningBankUI, "<size=20>Bank <color=#ffff00>{name}</color> is opening...</size>"},
                {MessageKey.BlockedItemChat, "Sorry, <color=#ff0000>you can't move</color> that item in bank!"},
                {MessageKey.BlockedItemUI, "<size=20>Sorry, <color=#ff0000>you can't move</color> that item in bank!</size>"},
                {MessageKey.NoInsertChat, "<color=#ff0000>You don't have access to that bank, you can only TAKE items</color>"}, 
                {MessageKey.NoInsertUI, "<size=20>You don't have access to that bank, <color=#ff0000>you can only TAKE items</color></size>"},
            };

            public enum Type
            {
                Normal,
                Warning,
                Error
            }

            private static Plugin plugin;
            private static Lang lang;

            public static void Load(Lang v1, Plugin v2)
            {
                lang = v1;
                plugin = v2;

                var dictionary = new Dictionary<string, string>();
                foreach (var pair in langMessages)
                {
                    var key = pair.Key.ToString();
                    var value = pair.Value.ToString();
                    dictionary.TryAdd(key, value);
                }

                lang.RegisterMessages(dictionary, plugin);
            }

            public static void Unload()
            {
                lang = null;
                plugin = null;
            }

            public static void Console(string message, Type type = Type.Normal)
            {
                message = $"[{plugin.Name}] {message}";
                switch (type)
                {
                    case Type.Normal:
                        Debug.Log(message);
                        break;

                    case Type.Warning:
                        Debug.LogWarning(message);
                        break;

                    case Type.Error:
                        Debug.LogError(message);
                        break;
                }
            }

            public static void Send(object receiver, string message, params object[] args)
            {
                message = FormattedMessage(message, args);
                SendMessage(receiver, message);
            }

            public static void Send(object receiver, MessageKey key, params object[] args)
            {
                var userID = (receiver as BasePlayer)?.UserIDString;
                var message = GetMessage(key, userID, args);
                SendMessage(receiver, message);
            }

            public static void Broadcast(string message, params object[] args)
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    message = FormattedMessage(message, args);
                    SendMessage(player.IPlayer, message);
                }
            }

            public static void Broadcast(MessageKey key, params object[] args)
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    var message = GetMessage(key, player.UserIDString, args);
                    SendMessage(player.IPlayer, message);
                }
            }

            public static string GetMessage(MessageKey key, string playerID = null, params object[] args)
            {
                var keyString = key.ToString();
                var message = lang.GetMessage(keyString, plugin, playerID);
                if (message == keyString)
                {
                    return $"{keyString} is not defined in plugin!";
                }

                if (Interface.CallHook("OnLanguageValidate") != null)
                {
                    message = langMessages.FirstOrDefault(x => x.Key == key).Value as string;
                }

                return FormattedMessage(message, args);
            }

            public static string FormattedMessage(string message, params object[] args)
            {
                if (args != null && args.Length > 0)
                {
                    var organized = OrganizeArgs(args);
                    return ReplaceArgs(message, organized);
                }

                return message;
            }

            private static void SendMessage(object receiver, object message)
            {
                if (receiver == null || message == null)
                {
                    return;
                }

                var messageString = message.ToString();
                if (string.IsNullOrEmpty(messageString))
                {
                    return;
                }

                var console = receiver as ConsoleSystem.Arg;
                if (console != null)
                {
                    // TODO: Finish me!
                    return;
                }

                var iPlayer = receiver as IPlayer ?? (receiver as BasePlayer)?.IPlayer;
                if (iPlayer != null)
                {
                    iPlayer.Message(messageString);
                    return;
                }
            }

            private static Dictionary<string, object> OrganizeArgs(object[] args)
            {
                var dic = new Dictionary<string, object>();
                for (var i = 0; i < args.Length; i += 2)
                {
                    var value = args[i].ToString();
                    var nextValue = i + 1 < args.Length ? args[i + 1] : null;
                    dic.TryAdd(value, nextValue);
                }

                return dic;
            }

            private static string ReplaceArgs(string message, Dictionary<string, object> args)
            {
                if (args == null || args.Count < 1)
                {
                    return message;
                }

                foreach (var pair in args)
                {
                    var s0 = "{" + pair.Key + "}";
                    var s1 = pair.Key;
                    var s2 = pair.Value != null ? pair.Value.ToString() : "null";
                    message = message.Replace(s0, s2, StringComparison.InvariantCultureIgnoreCase);
                    message = message.Replace(s1, s2, StringComparison.InvariantCultureIgnoreCase);
                }

                return message;
            }
        }

        #endregion

        #region Temporary UI v1.0

        private class TemporaryUI : MonoBehaviour
        {
            private BasePlayer player;
            private string lastParent;

            public static void Show(BasePlayer player, string json, string uiParent, float duration)
            {
                var obj = player.GetOrAddComponent<TemporaryUI>();
                obj.ShowUI(json, uiParent, duration);
            }

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
            }

            private void Start()
            {
                InvokeRepeating(nameof(CheckPlayer), 300f, 300f);
            }

            private void OnDestroy()
            {
                TimedDestroy();
            }

            private void ShowUI(string json, string uiParent, float duration)
            {
                TimedDestroy();
                CuiHelper.AddUi(player, json);
                lastParent = uiParent;

                if (IsInvoking(nameof(TimedDestroy)) == true)
                {
                    CancelInvoke(nameof(TimedDestroy));
                }

                Invoke(nameof(TimedDestroy), duration);
            }

            private void TimedDestroy()
            {
                if (lastParent != null)
                {
                    CuiHelper.DestroyUi(player, lastParent);
                }
            }

            private void CheckPlayer()
            {
                if (player.IsValid() == false || player.IsConnected == false)
                {
                    Destroy(this);
                }
            }
        }

        #endregion

        #region Old Data Migration

        private void ConvertOld()
        {
            var files = Interface.Oxide.DataFileSystem.GetFiles("ItemBank/Players/");
            foreach (var file in files)
            {
                var start = file.IndexOf("765", StringComparison.OrdinalIgnoreCase);
                if (start < 1)
                {
                    PrintWarning($"Failed to get steam id from {file}");
                    continue;
                }

                var userid = file.Substring(start, 17);
                var data = Data.Get(userid, true);
                var bank = data.banks.FirstOrDefault(x => x.shortname == "old");
                if (bank == null)
                {
                    bank = new BankData();
                    data.banks.Add(bank);
                    bank.shortname = "old";
                }
                
                bank.Load(null);

                var items = Interface.Oxide.DataFileSystem.ReadObject<List<OldBaseItem>>(file.Replace(".json", string.Empty));
                foreach (var def in items)
                {
                    var item = OldCreateItem(def);
                    if (item != null)
                    {
                        bank.inventory.capacity++;
                        item.MoveToContainer(bank.inventory);
                    }
                }
            }

            PrintWarning($"Converted {files.Length} old item banks...");
            Data.Save();
        }

        private class OldBaseItem
        {
            public string shortname = string.Empty;
            public int amount = 1;
            public ulong skinId = 0;
            public string displayName = null;
            public float condition = 0;
            public float maxCondition = 0;
            public float fuel = 0;
            public bool isBlueprint = false;
#pragma warning disable 414
            public int slot = 0;
#pragma warning restore 414
            // ReSharper disable once CollectionNeverUpdated.Local
            public Dictionary<string, int> contents = new Dictionary<string, int>();
        }

        private Item OldCreateItem(OldBaseItem def)
        {
            if (def.isBlueprint)
            {
                var blueprint = ItemManager.CreateByName("blueprintbase", def.amount);
                blueprint.blueprintTarget = ItemManager.FindItemDefinition(def.shortname).itemid;
                return blueprint;
            }

            var item = ItemManager.CreateByName(def.shortname, def.amount, def.skinId);
            if (item == null)
            {
                PrintWarning($"Can't create item ({def.shortname})");
                return null;
            }

            item.name = def.displayName;
            item.maxCondition = def.maxCondition;
            item.condition = def.condition;
            item.fuel = def.fuel;

            var weapon = item?.GetHeldEntity()?.GetComponent<BaseProjectile>();
            foreach (var defC in def.contents)
            {
                var name = defC.Key;
                var amount = defC.Value;

                if ((name.StartsWith("arrow.") || name.StartsWith("ammo.")) && weapon != null)
                {
                    weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(name);
                    weapon.primaryMagazine.contents = amount;
                    continue;
                }

                var content = ItemManager.CreateByName(name, amount);
                content.MoveToContainer(item.contents);
            }

            weapon?.SendNetworkUpdateImmediate();
            return item;
        }

        #endregion
    }
}
