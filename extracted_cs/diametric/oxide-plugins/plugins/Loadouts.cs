using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins.LoadoutsExtensions;

namespace Oxide.Plugins
{
    [Info("Loadouts", "Jacob", "1.0.0")]
    class Loadouts : RustPlugin
    {
        #region Command

        [ChatCommand("loadout")]
        private void LoadoutCommand(BasePlayer player, string command, string[] args)
        {
            if (!player.HasPermission())
            {
                PrintToChat(player, player.Lang("NoPermission"));
                return;
            }

            if (args.Length == 0)
            {
                PrintToChat(player, player.Lang("SyntaxError"));
                return;
            }

            switch (args[0].ToLower())
            {
                case "help":
                    PrintToChat(player, player.Lang("Help"));
                    break;

                case "items":
                    var items = configuration.AllowedItems.Take(8).Select(x => string.Format(player.Lang("AmountFormatting"), ItemManager.FindItemDefinition(x.Key).displayName.english, x.Value)).ToList();
                    if (configuration.AllowedItems.Count > 8)
                        items.Add($"{configuration.AllowedItems.Count - 8} more");

                    PrintToChat(player, player.Lang(items.Count != 0 ? "Items" : "ItemsNone"), covalence.FormatText(items.ToSentence().Replace(".", "")));
                    break;

                case "reset":
                    if (!data.Loadouts.ContainsKey(player.userID))
                    {
                        PrintToChat(player, player.Lang("ResetNone"));
                        return;
                    }

                    PrintToChat(player, player.Lang("Reset"));
                    break;

                case "save":
                    if (player.inventory.containerBelt.itemList.Any(x => !configuration.AllowedItems.ContainsKey(x.info.shortname) || Convert.ToInt32(configuration.AllowedItems[x.info.shortname]) < x.amount))
                    {
                        PrintToChat(player, player.Lang("SaveError"));
                        return;
                    }

                    PrintToChat(player, player.Lang("Save"));
                    data.Loadouts.Remove(player.userID);
                    data.Loadouts.Add(player.userID, new SerializableContainer(player.inventory.containerBelt));
                    break;

                default:
                    PrintToChat(player, player.Lang("SyntaxError"));
                    break;
            }
        }

        #endregion

        #region Configuration

        public class Configuration
        {
            public bool ResetOnWipe;
            public Dictionary<string, object> AllowedItems = new Dictionary<string, object>
            {
                {"rifle.ak", 1},
                {"syringe.medical", 10}
            };

            public Configuration()
            {
                GetConfig(ref ResetOnWipe, "Settings", "Reset loadouts on wipe");
                GetConfig(ref AllowedItems, "Settings", "Allowed items");

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

        private class Data
        {
            public Dictionary<ulong, SerializableContainer> Loadouts = new Dictionary<ulong, SerializableContainer>();

            public Data()
            {
                ReadData(ref Loadouts);
            }

            public void ReadData<T>(ref T data, string filename = "Loadouts") => Interface.Oxide.DataFileSystem.ReadObject<T>($"{Instance.Name}/{filename}");

            public void SaveData<T>(T data, string filename = "Loadouts") => Interface.Oxide.DataFileSystem.WriteObject($"{Instance.Name}/{filename}", data);
        }

        private class SerializableItem
        {
            public int Amount;
            public Dictionary<string, float> Attachments = new Dictionary<string, float>();
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
                ShortName = item.info.shortname;
                Slot = item.position;
                SkinID = item.skin;
                Weapon = false;
                var primaryMagazine = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine;
                if (primaryMagazine == null)
                    return;

                PrimaryMagazine = primaryMagazine.contents;
                Weapon = item.info.category == ItemCategory.Weapon;
                foreach (var contentItem in item.contents.itemList)
                    Attachments.Add(contentItem.info.shortname, contentItem.condition);
            }

            public Item Create()
            {
                var item = ItemManager.CreateByName(ShortName, Amount, SkinID);
                if (!Weapon)
                    return item;

                var weapon = item.GetHeldEntity() as BaseProjectile;
                if (weapon != null)
                    weapon.primaryMagazine.contents = PrimaryMagazine;

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

            public void Give(BasePlayer player, ItemContainer container)
            {
                var item = Create();
                if (item.MoveToContainer(container, Slot))
                    player.SendConsoleCommand("note.inv", item.info.itemid, item.amount);
            }
        }

        private class SerializableContainer
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

            public void Give(BasePlayer player)
            {
                foreach (var item in Items)
                    item.Give(player, player.inventory.containerBelt);
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

        #endregion

        #region Fields

        private Data data;
        private Configuration configuration;
        public static Loadouts Instance;

        #endregion

        #region Localization

        protected override void LoadDefaultMessages() => lang.RegisterMessages(new Dictionary<string, string>
        {
            {"AmountFormatting", "[#ADD8E6]{0}[/#] [#FFFFFF]([/#]{1}[#FFFFFF])[/#]"},
            {"Help", "<size=16>Loadout Help</size>\n<size=13>[#ADD8E6]/loadout items[/#]</size> <size=12>lists all allowed items with amounts.</size>\n<size=13>[#ADD8E6]/loadout reset[/#]</size> <size=12>resets your saved loadout.</size>\n<size=13>[#ADD8E6]/loadout save[/#]</size> <size=12>saves your belt contents.</size>"},
            {"NoPermission", "Error, you lack permission."},
            {"Save", "Sucessfully saved loadout."},
            {"SaveError", "Error, restricted items. Try looking at [#ADD8E6]/loadout items[/#]."},
            {"Reset", "Sucessfully reset loadout."},
            {"ResetNone", "Error, no loadout saved."},
            {"Items", "<size=16>Loadout Items</size>\n<size=13>The following items are allowed:</size> <size=12>{0}.</size>"},
            {"ItemsNone", "Error, no items are allowed, try contacting your administrator if you believe this is an error."},
            {"SyntaxError", "Error, invalid syntax. Try looking at [#ADD8E6]/loadout help[/#]."}
        }, this);

        #endregion

        #region Methods

        private void RegisterPermission(string name) => permission.RegisterPermission($"{Name}.{name}", this);

        #endregion

        #region Oxide Hooks

        private void Init()
        {
            Instance = this;

            RegisterPermission("able");

            data = new Data();
            configuration = new Configuration();
        }

        private void OnNewSave()
        {
            if (configuration.ResetOnWipe)
                data.Loadouts.Clear();
        }

        private void OnServerSave() => data.SaveData(data.Loadouts);

        private void Unload() => OnServerSave();

        private void OnPlayerSpawn(BasePlayer player)
        {
            SerializableContainer loadout;
            if (!data.Loadouts.TryGetValue(player.userID, out loadout))
                return;

            timer.Once(1f, () => loadout.Give(player));
        }

        private void OnPlayerRespawned(BasePlayer player) => OnPlayerSpawn(player);

        #endregion
    }

    namespace LoadoutsExtensions
    {
        public static class Extensions
        {
            private static readonly Covalence covalence = Interface.GetMod().GetLibrary<Covalence>();
            private static readonly Lang lang = Interface.GetMod().GetLibrary<Lang>();
            private static readonly Permission permission = Interface.GetMod().GetLibrary<Permission>();

            public static bool HasPermission(this BasePlayer player, string name = "able") => permission.UserHasPermission(player.UserIDString, $"{Loadouts.Instance.Name}.{name}") || player.IsAdmin;

            public static string Lang(this BasePlayer player, string key)
            {
                var message = lang.GetMessage(key, Loadouts.Instance, player.UserIDString);
                return covalence.FormatText(message);
            }

            public static void ClearBelt(this BasePlayer player)
            {
                foreach (var item in player.inventory.containerBelt.itemList)
                {
                    item.RemoveFromWorld();
                    item.RemoveFromContainer();
                }
            }
        }
    }
}