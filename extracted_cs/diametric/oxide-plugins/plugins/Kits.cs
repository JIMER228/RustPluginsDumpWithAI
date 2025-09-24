using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using UnityEngine;
using Oxide.Plugins.KitsExtensions;

/*using Anchor = Oxide.Plugins.KitsUI.Anchor;
using RGBA = Oxide.Plugins.KitsUI.RGBA;
using UI = Oxide.Plugins.KitsUI.UI;*/

namespace Oxide.Plugins
{
    [Info("Kits", "Jacob", "4.0.0", ResourceId = 668)]
    class Kits : RustPlugin
    {
        /*
         * TODO: 
         * API methods
         * Spawn kits (previously called auotkits) -- TEST
         * Finish extension methods
         * Finish gift command
         * GUI
         * Internal CanUseKit method
         * Add support for additional containers
         */

        #region Command
       
        [ChatCommand("kit")]
        private void KitCommand(BasePlayer player, string command, string[] args)
        {
            if (!player.HasPermission("able"))
            {
                PrintToChat(player.Lang("NoPermission"));
                return;
            }

            if (args.Length == 0)
            {
                KitList(player);
                return;
            }

            switch (args[0])
            {
                case "all":
                    KitList(player, true);
                    break;

                case "add":
                    KitAdd(player, args);
                    break;

                case "cooldown":
                    KitCooldown(player, args);
                    break;

                case "edit":
                    KitEdit(player, args);
                    break;

                case "gift":
                    KitGift(player);
                    break;

                case "help":
                    KitHelp(player);
                    break;

                case "max":
                    KitMax(player, args);
                    break;

                case "remove":
                    KitRemove(player, args);
                    break;

                case "reset":
                    KitReset(player);
                    break;

                case "set":
                    KitSet(player);
                    break;

                default:
                    // TRY GIVE KIT
                    break;
            }
        }

        private void KitAdd(BasePlayer player, params string[] args)
        {
            if (!player.HasPermission("admin"))
            {
                PrintToChat(player, player.Lang("NoPermission"));
                return;
            }

            if (args.Length == 1)
            {
                PrintToChat(player, player.Lang("SyntaxError"));
                return;
            }

            var name = args[1].ToLower();
            if (data.GetKit(name) != null)
            {
                PrintToChat(player, player.Lang("AlreadyExists"), name);
                return;
            }

            data.AddKit(name, player);
            Instance.RegisterPermission(name);

            PrintToChat(player, player.Lang("Add"), name);
            if (kitEditor.ContainsKey(player.userID))
            {
                kitEditor[player.userID] = name;
                return;
            }

            kitEditor.Add(player.userID, name);
        }

        private void KitCooldown(BasePlayer player, params string[] args)
        {
            if (!player.HasPermission("admin"))
            {
                PrintToChat(player, player.Lang("NoPermission"));
                return;
            }

            if (args.Length < 3 || !Regex.IsMatch(args[2], @"^[0-9]+$"))
            {
                PrintToChat(player, player.Lang("SyntaxError"));
                return;
            }

            string name;
            if (!kitEditor.TryGetValue(player.userID, out name) || data.GetKit(name) == null)
            {
                PrintToChat(player, player.Lang("NotEditing"));
                return;
            }

            var kit = data.GetKit(name);
            switch (args[1].ToLower())
            {
                case "days":
                    PrintToChat(player, player.Lang("Cooldown"));
                    kit.Cooldown += new TimeSpan(Convert.ToInt32(args[2]), 0, 0, 0);
                    break;

                case "hours":
                    PrintToChat(player, player.Lang("Cooldown"));
                    kit.Cooldown += new TimeSpan(Convert.ToInt32(args[2]), 0, 0);
                    break;

                case "minutes":
                    PrintToChat(player, player.Lang("Cooldown"));
                    kit.Cooldown += new TimeSpan(0, Convert.ToInt32(args[2]), 0);
                    break;

                case "seconds":
                    PrintToChat(player, player.Lang("Cooldown"));
                    kit.Cooldown += new TimeSpan(0, 0, Convert.ToInt32(args[2]));
                    break;

                default:
                    PrintToChat(player, player.Lang("SyntaxError"));
                    break;
            }

            data.Kits[name] = kit;
        }

        private void KitEdit(BasePlayer player, params string[] args)
        {
            if (!player.HasPermission("admin"))
            {
                PrintToChat(player, player.Lang("NoPermission"));
                return;
            }

            if (args.Length == 1)
            {
                PrintToChat(player, player.Lang("SyntaxError"));
                return;
            }

            var name = args[1].ToLower();
            var kit = data.GetKit(args[1].ToLower());
            if (kit == null)
            {
                PrintToChat(player, player.Lang("Doesn'tExist"), name);
                return;
            }

            PrintToChat(player, player.Lang("Edit"), name);
            if (kitEditor.ContainsKey(player.userID))
            {
                kitEditor[player.userID] = name;
                return;
            }

            kitEditor.Add(player.userID, name);
        }

        private void KitGift(BasePlayer player, params string[] args)
        {
            
        }

        private void KitHelp(BasePlayer player)
        {
            PrintToChat(player, player.Lang("Help"));
            if (player.HasPermission("admin"))
                PrintToChat(player, player.Lang("HelpAdmin"));
        }

        private void KitList(BasePlayer player, bool all = false)
        {
            if (!player.HasPermission("admin"))
            {
                PrintToChat(player, player.Lang("NoPermission"));
                return;
            }

            var formattedKits = player.GetFormattedKits(all);
            PrintToChat(player, player.Lang(formattedKits != null ? "List" : "ListNone"), formattedKits);
        }

        private void KitMax(BasePlayer player, params string[] args)
        {
        }

        private void KitRemove(BasePlayer player, params string[] args)
        {

        }

        private void KitReset(BasePlayer player)
        {
            
        }

        private void KitSet(BasePlayer player)
        {
            
        }

        #endregion

        #region Configuration 

        public class Configuration
        {
            public bool ResetOnWipe;

            public bool IgnoreRestrictions;
            public List<object> DefaultKits = new List<object>
            {
                "super-special",
                "special",
                "normal"
            };

            public bool HideUnusableKits = true;

            public Configuration()
            {

                GetConfig(ref ResetOnWipe, "Wipe settings", "Reset all player data on wipe");

                GetConfig(ref IgnoreRestrictions, "Default kit settings", "Ignore restrictions");
                GetConfig(ref DefaultKits, "Default kit settings", "Default kit (set blank for nothing)");
                    
                GetConfig(ref HideUnusableKits, "List settings", "Hide unusable kits");

                Instance.SaveConfig();
            }

            private void GetConfig<T>(ref T variable, params string[] path)
            {
                if (path.Length == 0)
                    return;

                if (Instance.Config.Get(path) == null)
                {
                    SetConfig(ref variable, path);
                    Instance.PrintWarning($"Added field to config: {string.Join("/", path)}");
                }

                variable = (T)Convert.ChangeType(Instance.Config.Get(path), typeof(T));
            }

            private void SetConfig<T>(ref T variable, params string[] path) => Instance.Config.Set(path.Concat(new object[] { variable }).ToArray());
        }

        protected override void LoadDefaultConfig() => PrintWarning("Generating new config file...");

        #endregion

        #region Data

        public class Data
        {
            public Dictionary<string, Kit> Kits = new Dictionary<string, Kit>();
            public Dictionary<ulong, PlayerData> PlayerData = new Dictionary<ulong, PlayerData>();

            public Data()
            {
                GetData(ref Kits, "Kits");
                GetData(ref PlayerData, "PlayerData");
                foreach (var key in Kits.Keys)
                    Instance.RegisterPermission(key);
            }

            public Kit GetKit(string name) => Kits.FirstOrDefault(x => x.Key == name).Value;

            public PlayerData GetPlayer(BasePlayer player)
            {
                PlayerData data;
                if (PlayerData.TryGetValue(player.userID, out data))
                    return data;

                data = new PlayerData();
                PlayerData.Add(player.userID, data);
                return data;
            }

            public void AddKit(string name, BasePlayer player) => Kits.Add(name, new Kit(player));

            public void RemoveKit(string name) => Kits.Remove(name);

            public void GetData<T>(ref T data, string filename) => data = Interface.Oxide.DataFileSystem.ReadObject<T>($"{Instance.Name}/{filename}");

            public void SaveData<T>(T data, string filename) => Interface.Oxide.DataFileSystem.WriteObject($"{Instance.Name}/{filename}", data);
        }

        public class Kit
        {
            public int Max;
            public TimeSpan Cooldown;
            public SerializableInventory Items;

            public Kit()
            {
            }

            public Kit(BasePlayer player)
            {
                Items = new SerializableInventory(player);
            }

            public void GiveKit(BasePlayer player) => Items.Give(player);

            public void TryGiveKit(BasePlayer player)
            {
                // 
            }

            public bool CanUseKit(BasePlayer player) => true;
        }

        public class PlayerData
        {
            public Dictionary<string, DateTime> UsageTimes = new Dictionary<string, DateTime>();
            public Dictionary<string, int> Uses = new Dictionary<string, int>();

            public PlayerData()
            {
            }

            public void AddUsage(string name)
            {
                if (UsageTimes.ContainsKey(name))
                    UsageTimes[name] = DateTime.UtcNow;
                else
                    UsageTimes.Add(name, DateTime.UtcNow);

                if (Uses.ContainsKey(name))
                    Uses[name]++;
                else
                    Uses.Add(name, 1);
            }
        }

        #endregion

        #region Fields

        public static Kits Instance;

        public Configuration configuration;
        public Data data;

        private Dictionary<ulong, string> kitEditor = new Dictionary<ulong, string>();

        #endregion

        #region Item Storage

        public class SerializableItem
        {
            public string AmmoType;
            public int Amount;
            public Dictionary<string, float> Attachments = new Dictionary<string, float>();
            public bool Blueprint;
            public float Condition;
            public Item.Flag Flags;
            public int PrimaryMagazine;
            public string ShortName;
            public ulong SkinID;
            public int Slot;
            public bool Weapon;

            public SerializableItem()
            {
            }

            public SerializableItem(Item item)
            {
                Amount = item.amount;
                ShortName = item.IsBlueprint() ? ItemManager.FindItemDefinition(item.blueprintTarget).shortname : item.info.shortname;
                Slot = item.position;
                if (item.IsBlueprint())
                {
                    Blueprint = true;
                    return;
                }

                Condition = item.condition;
                Flags = item.flags;
                SkinID = item.skin;
                Weapon = false;
                var primaryMagazine = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine;
                if (primaryMagazine == null)
                    return;

                AmmoType = primaryMagazine.ammoType.shortname;
                PrimaryMagazine = primaryMagazine.contents;
                Weapon = item.info.category == ItemCategory.Weapon;
                foreach (var contentItem in item.contents.itemList)
                    Attachments.Add(contentItem.info.shortname, contentItem.hasCondition ? contentItem.condition : -1);
            }

            public Item Create()
            {
                if (Blueprint)
                {
                    var blueprint = ItemManager.CreateByName("blueprintbase");
                    blueprint.blueprintTarget = ItemManager.FindItemDefinition(ShortName).itemid;
                    blueprint.amount = Amount;
                    return blueprint;
                }

                var item = ItemManager.CreateByName(ShortName, Amount, SkinID);
                item.flags = Flags;
                if (item.hasCondition)
                    item.condition = Condition;

                if (!Weapon)
                    return item;

                var weapon = item.GetHeldEntity() as BaseProjectile;
                if (weapon != null)
                    weapon.primaryMagazine.contents = PrimaryMagazine;

                if (AmmoType != null && weapon != null)
                    weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(AmmoType);

                if (Attachments == null)
                    return item;

                foreach (var attachment in Attachments)
                {
                    var attachmentItem = ItemManager.CreateByName(attachment.Key);
                    attachmentItem.condition = attachment.Value;
                    attachmentItem.MoveToContainer(item.contents);
                }

                return item;
            }

            public void Give(BasePlayer player, ItemContainer container, bool force = true)
            {
                var item = Create();
                if (item.MoveToContainer(container, Slot))
                    player.SendConsoleCommand("note.inv", item.info.itemid, item.amount);
                else
                    player.GiveItem(item);

                if (!player.inventory.AllItems().Contains(item))
                    item.Drop(player.transform.position, Vector3.zero);
            }
        }

        public class SerializableContainer
        {
            public HashSet<SerializableItem> Items = new HashSet<SerializableItem>();

            public SerializableContainer()
            {
            }

            public SerializableContainer(ItemContainer container)
            {
                foreach (var item in container.itemList)
                    Items.Add(new SerializableItem(item));
            }

            public HashSet<Item> Create()
            {
                var items = new HashSet<Item>();
                foreach (var item in Items)
                    items.Add(item.Create());

                return items;
            }

            public void Give(BasePlayer player, ItemContainer container)
            {
                foreach (var item in Items)
                    item.Give(player, container);
            }

            public void Add(Item item) => Items.Add(new SerializableItem(item));

            public void Add(SerializableItem item) => Items.Add(item);

            public void Update(ItemContainer container)
            {
                Clear();
                foreach (var item in container.itemList)
                    Items.Add(new SerializableItem(item));
            }

            public void Clear() => Items.Clear();
        }

        public class SerializableInventory
        {
            public SerializableContainer ContainerWear = new SerializableContainer();
            public SerializableContainer ContainerMain = new SerializableContainer();
            public SerializableContainer ContainerBelt = new SerializableContainer();

            public SerializableInventory()
            {
            }

            public SerializableInventory(BasePlayer player)
            {
                Update(player);
            }

            public void Give(BasePlayer player, bool clear = false)
            {
                if (clear)
                    Instance.ClearInventory(player);

                ContainerWear.Give(player, player.inventory.containerWear);
                ContainerMain.Give(player, player.inventory.containerMain);
                ContainerBelt.Give(player, player.inventory.containerBelt);
            }

            public void Update(BasePlayer player)
            {
                Clear();
                ContainerWear.Update(player.inventory.containerWear);
                ContainerMain.Update(player.inventory.containerMain);
                ContainerBelt.Update(player.inventory.containerBelt);
            }

            public void Clear()
            {
                ContainerWear.Clear();
                ContainerMain.Clear();
                ContainerBelt.Clear();
            }
        }

        #endregion

        #region Localization

        protected override void LoadDefaultMessages() => lang.RegisterMessages(new Dictionary<string, string>
        {
            {"Add", "Sucessfully added kit [#ADD8E6]{0}[/#]."},
            {"AlreadyExists", "Error, a kit already exists by the name of [#ADD8E6]{0}[/#]."},
            {"Cooldown", "Cooldown sucessfully updated."},
            {"CooldownReset", "Cooldown sucessfully reset."},
            {"Doesn'tExist", "Error, no kit exists by the name of [#ADD8E6]{0}[/#]."},
            {"Edit", "Sucessfully started editing kit [#ADD8E6]{0}[/#]."},
            {"Help", "<size=16>Kits Command</size>\n<size=13>[#ADD8E6]/kit gift <player> <name>[/#]</size> <size=12>gifts a kit.</size>\n<size=13>[#ADD8E6]/kit <name>[/#]</size> <size=12> redeems a kit.</size>"},
            {"HelpAdmin", "<size=16>Kits Admin Commands</size>\n<size=13>[#ADD8E6]/kit add <name>[/#]</size> <size=12>creates a kit.</size>\n<size=13>[#ADD8E6]/kit all[/#]</size> <size=12>lists all kits.</size>\n<size=13>[#ADD8E6]/kit cooldown <days|hours|minutes|seconds> <#>[/#]</size> <size=12>sets a cooldown.</size>\n<size=13>[#ADD8E6]/kit edit <name>[/#]</size> <size=12>starts editing a kit.</size>\n<size=13>[#ADD8E6]/kit max <#>[/#]</size> <size=12>sets a usage limit.</size>\n<size=13>[#ADD8E6]/kit remove <name>[/#]</size> <size=12>removes a kit.</size>\n<size=13>[#ADD8E6]/kit reset[/#]</size> <size=12>resets all cooldowns.</size>\n<size=13>[#ADD8E6]/kit set[/#]</size> sets items."},
            {"List", "The following kits are available: [#ADD8E6]{0}[/#]."},
            {"ListNone", "Error, no kits are available."},
            {"Max", "Sucessfully set kit [#ADD8E6]{0}[/#]'s usage limit."},
            {"MultiplePlayersFound", "Error, multiple players found by the name of [#ADD8E6]{0}[/#]."},
            {"NoPermission", "Error, you lack permission."},
            {"NoPlayerFound", "Error, no player found by the name of [#ADD8E6]{0}[/#]."},
            {"NotEditing", "Error, you're not editing any kit."},
            {"Remove", "Sucessfully removed kit [#ADD8E6]{0}[/#]."},
            {"DataReset", "Sucessfully reset all player cooldowns."},
            {"Set", "Sucessfully set kit  [#ADD8E6]{0}[/#]'s items."},
            {"SyntaxError", "Error, incorrect arguments or syntax. Try [#ADD8E6]/kit help.[/#]"},
        }, this);

        #endregion

        #region Methods

        private void RegisterPermission(string name) => permission.RegisterPermission($"{Name}.{name}", this);

        public BasePlayer FindPlayer(BasePlayer player, string nameOrID)
        {
            var targets = BasePlayer.activePlayerList.FindAll(x => nameOrID.IsSteamId() ? x.UserIDString == nameOrID : x.displayName.ToLower().Contains(nameOrID));
            if (targets.Count == 1)
                return targets[0];

            PrintToChat(player, player.Lang(targets.Count == 0 ? "NoPlayerFound" : "MultiplePlayersFound"), nameOrID);
            return null;
        }

        [HookMethod("ClearInventory")]
        public void ClearInventory(BasePlayer player) => player.inventory.Clear();

        [HookMethod("ClearContainer")]
        public void ClearContainer(ItemContainer container) => container.Clear();

        #endregion

        #region Oxide Hooks

        private void Init()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            Instance = this;

            configuration = new Configuration();
            data = new Data();

            RegisterPermission("able");
            RegisterPermission("admin");
        }

        private void Unload() => OnServerSave();

        private void OnNewSave()
        {
            if (!configuration.ResetOnWipe)
                return;

            PrintWarning("Resetting player data...");
            data.PlayerData.Clear();
        }

        private void OnServerSave()
        {
            data.SaveData(data.Kits, "Kits");
            data.SaveData(data.PlayerData, "PlayerData");
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            var name = configuration.DefaultKits.FirstOrDefault(x => player.HasPermission(x.ToString()) && data.GetKit(x.ToString()) != null)?.ToString();
            if (name == null)
                return;

            var kit = data.GetKit(name);
            if (kit == null)
                return;

            if (Interface.CallHook("CanUseKit", player, kit) != null)
                return;

            if (configuration.IgnoreRestrictions && !kit.CanUseKit(player))
                return;

            player.inventory.Clear();
            kit.GiveKit(player);
        }

        private void OnPlayerSpawn(BasePlayer player) => OnPlayerRespawned(player);

        #endregion
    }

    namespace KitsExtensions
    {
        static class Extensions
        {
            private static Covalence covalence = Interface.GetMod().GetLibrary<Covalence>();
            private static Lang lang = Interface.GetMod().GetLibrary<Lang>();
            private static  Permission permission = Interface.GetMod().GetLibrary<Permission>();

            public static void Clear(this PlayerInventory inventory)
            {
                Kits.Instance.ClearContainer(inventory.containerWear);
                Kits.Instance.ClearContainer(inventory.containerMain);
                Kits.Instance.ClearContainer(inventory.containerBelt);
            }

            public static void Clear(this ItemContainer container)
            {
                foreach (var item in container.itemList)
                {
                    item.RemoveFromWorld();
                    item.RemoveFromContainer();
                }
            }

            public static string GetFormattedCooldown(this BasePlayer player, string name)
            {
                // TODO: finish this
                return "";
            }

            public static object GetFormattedKits(this BasePlayer player, bool all = false)
            {
                var kits = Kits.Instance.configuration.HideUnusableKits && !player.HasPermission("admin") && !all
                    ? (from kit in Kits.Instance.data.Kits where kit.Value.CanUseKit(player) select kit.Key)
                    : (from kit in Kits.Instance.data.Kits select kit.Key);

                var enumerable = kits as string[] ?? kits.ToArray();
                return enumerable.Any() ? enumerable.ToSentence().Replace(".", "") : null;
            }

            public static bool HasPermission(this BasePlayer player, string name) => permission.UserHasPermission(player.UserIDString, $"{Kits.Instance.Name}.{name}") || player.IsAdmin;

            public static string Lang(this BasePlayer player, string key)
            {
                var message = lang.GetMessage(key, Kits.Instance, player.UserIDString);
                return covalence.FormatText(message);
            }
        }
    }
}

/*namespace Oxide.Plugins.KitsUI
{
    public class Anchor
    {
        public float X { get; }
        public float Y { get; }

        public Anchor(float x, float y)
        {
            X = x;
            Y = y;
        }

        public string Format() => $"{X} {Y}";
    }

    public class RGBA
    {
        public float R { get; }
        public float B { get; }
        public float G { get; }
        public float A { get; }

        public RGBA(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public string Format => $"{R / 255} {G / 255} {B / 255} {A}";
    }

    public class UI
    {
        public CuiElementContainer Container(string name, RGBA color, Anchor min, Anchor max, string parent = "Overlay")
        {
            var container = new CuiElementContainer
            {
                new CuiElement
                {
                    Name = name,
                    Parent = parent,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = color.Format
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = min.Format(),
                            AnchorMax = max.Format()
                        }
                    }
                }
            };

            return container;
        }

        public void Panel(ref CuiElementContainer container, string name, RGBA color, Anchor min, Anchor max, string parent, bool cursor = false)
        {
            container.Add(new CuiPanel
            {
                CursorEnabled = cursor,
                Image =
                {
                    Color = color.Format
                },
                RectTransform =
                {
                    AnchorMin = min.Format(),
                    AnchorMax = max.Format()
                }         
            }, parent, name);
        }

        public void Text(ref CuiElementContainer container, string name, RGBA color, Anchor min, Anchor max, TextAnchor anchor, int fontSize, string text, string parent)
        {
            container.Add(new CuiElement
            {
                Name = name,
                Parent = parent,
                Components =
                {
                    new CuiTextComponent
                    {
                        Color = color.Format,
                        Align = anchor,
                        FontSize = fontSize,
                        Text = text
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = min.Format(),
                        AnchorMax = max.Format()
                    }
                }
            });
        }
    }
}*/