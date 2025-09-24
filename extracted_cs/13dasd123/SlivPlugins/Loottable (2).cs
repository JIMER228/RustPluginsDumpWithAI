// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿// Reference: 0Harmony
using Harmony;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Oxide.Plugins.LoottableExtensions;
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static TrainCarUnloadable;
using StaticLootableModels = Oxide.Plugins.LoottableExtensions.StaticLootableModels;
#if CARBON
using Carbon.Base;
using Carbon.Modules;
#endif

/**
 * VERSION HISTORY
 * 
 * V 1.0.1
 * - replaced FindObjectsOfType with BaseNetworkable.serverEntities.Find
 * 
 * V 1.0.2
 * - added gui item selection
 * - auto refresh loot after closing the editor
 * - added gather manager ui
 * - fixed editor sometimes not opening bug
 * - loot distribution rework
 * - stacksize bugfixes
 * 
 * V 1.0.3
 * - fixed category item distribution
 * - item.stackable now adjusts with stacksize
 * - fixed item amount in crate config ignored
 * 
 * V 1.0.4
 * - updated input fields to work with latest oxide version
 * - changed input field clamping to match min/max values
 * - fixed npc corpse custom loot profiles
 * - added gather rates for animals
 * 
 * V 1.0.5
 * - fixed plugin not loading on server startup
 * - fixed players without admin permission unable to open editor
 * 
 * V 1.0.6
 * - fixed invalid cast exception when adjusting item count of npcs
 * - added page navigation to crate loot editor
 * - added hooks for container population
 * - fixed invisible cursor in some input fields
 * - improved image loading
 * 
 * V 1.0.7
 * - added flags to bypass editor restrictions
 * - added addition loot option
 * - added default loot config for npcs and some crates
 * - added the option to import loot tables
 * - fixed a bug where the despawn time of a locked crate would be reset upon reload
 * 
 * V 1.0.7 HOTFIX / V 1.0.8
 * - fixed plugin not working after Rust update
 * 
 * V 1.0.9
 * - fixed several stacksize related bugs
 * - added spray can decal item to ignore list
 * - excluded player quarries form quarry config
 * - fixed max item amount of crates ignored in some cases
 * - loot distribution improvements (again)
 * - fixed editor not opening without image library
 * - removed stacking of items w/ durability field as it caused confusion
 * - replaced GatherControl and StacksizeControl lootable with GenericLootable
 * - wear container now only accepts 1 item (no stacked clothes)
 * - heli fuel can now be adjusted in the editor
 * - smelting speed can now be controlled in the editor
 * - updated images for some crates
 * 
 * V 1.0.10
 * - added bbq and campfire to smelting config
 * - new icons for mini and srappy fuel config
 * - added the option to multiply an entire loot table
 * - slightly changed category section to look better
 * - fixed invalid cast exception when editing category item amounts
 * - fixed typo in 'UnlockGahterMultiplier' that caused an error when changing gather multipliers
 * - fixed recycler not stopping when items are removed
 * - fixed a bug where too much items are in a crate (again)
 * - allowed multiple items with same itemid but different skin id
 * - added the option to add extra items to an item
 * - added air drop config options
 * - added scientistnpc_roam to npc config
 * - fixed gather items not droping when inventory is full
 * - added support for static lootables (wip)
 * - disable quick smelt if speed multiplier is 1
 * - added category stack size multipliers
 * 
 * V 1.0.11
 * - fixed plugin not initializing
 * 
 * V 1.0.12
 * - fixed server freezing (caused by infinite loop)
 * - fixed unable to edit gather items
 * - items can now have drop chances as low as 0.01% (was 1% before)
 * - added the option to multiply items of collectables
 * - added the option to load the default config for collectables
 * - improved loot distribution
 * - added indicator for extra items
 * 
 * V 1.0.13
 * - fixed unable to edit excavator/quarry items
 * - allowed multiple items in excavator config
 * - support for static lootables (beta)
 * - changed ui parent from hud.menu to overlay
 * - fixed recycler bugs
 * - added mixing table to smelting speed config
 * - added drop chances for gather items
 * 
 * V 1.0.14
 * - fixed some typos
 * - fixed missing delete button when editing items
 * - fixed supply drop bug with loot defender (and fancy drop)
 * - added charcoal config for furnaces
 * - more config options for static lootables (wip)
 * 
 * V 1.0.15
 * - removed deprecated hook OnCollectiblePickup and replaced it
 * - cleaned up api code
 * - fixed weird console/debug message behavior
 * - fixed null reference exception when populating npc corpses
 * - added invisible crates
 * - slightly changed loot refresh function and included loot wagons
 * - added train wagon loot config
 * 
 * V 1.0.16
 * - fixed null reference exception in InitializeEditorConfig
 * - fixed null reference exception in OnCollectiblePickup
 * - added locomotive to list of ignored items
 * - added support for custom items
 * - fixed issue with xlevels OnCollectible pickup
 * - fixed issue when splitting clones
 * - fixed issues with simple splitter
 * - added hook OnCargoPlaneSignaled to custom supply drops
 * - fixed null reference exception when calling a supply drop
 * 
 * V 1.0.17
 * - patched for rust update
 * 
 * V 1.0.18
 * - fixed problems caused by update
 * - re-enabled supply drop
 * - re-enabled mixing table
 * - fixed npc loot issues
 * - fixed error when creating items
 * - fixed null reference exception in OnEntitySpawned
 * 
 * V 1.0.19
 * - fix IndexOutOfRangeException when selecting last category in stack size editor
 * - added TracedAction to find origin of NullReferenceException in timer callback in OnEntitySpawned(LootableCorpse corpse)
 * - fixed furnaces not working after server reboot
 * - fixed stacking of world items with different skins
 * 
 * V 1.0.20
 * - fix bug with category stack size multiplier
 * - fix error when npc corpse of BetterNpc NPC spawned
 * - added flag to disable stacking hooks
 * 
 * V 1.0.21
 * - call hook OnSupplyDropDropped for custom air drops
 * - added the option to disable loot refresh (should fix problems with increased entity count)
 * - fixed fuel duplication glitch with miner hats
 * - fixed stacking bugs with dropped items
 * 
 * V 1.0.22
 * - fix problem with BradleyDrops
 * - add blueprint option for items
 * - items can now have condition
 * - fix blueprint stacking issues
 * - furnace and airdrop config can be disabled
 * - add electrical furnace to smelting config
 * 
 * V 1.0.23
 * - fix KeyNotFoundException in OnDispenserGather
 * - put gahtered items in hotbar
 * - fix issue where adding an item would add it as extra item
 * - added gather multipliers for growable plants
 * 
 * V 1.0.24
 * - fix global gather multiplier not working for animals
 * - fix furnace speed issues
 * - add support for HeliSignals
 * - add support for DefendableHomes
 * - add support for Carbon and it's ImageDatabase
 * - rework train loot configuration
 * - change plugin type to RustPlugin
 * 
 * V 1.0.25
 * - change command system to work with RustPlugin
 * - add warning if sandbox is enabled
 * 
 * V 1.0.26
 * - remove sandbox check
 * 
 * V 1.0.27
 * - extend custom item api
 * - add the ability to create custom items in the editor
 * - add page navigation for custom items
 * - fix smelting speed for electric furnace
 * - fix display bug with info message for train loot config
 * - add validation to furnace config (only enable when SimpleSplitter is not present)
 * - handle splitting of items with container (BagOfHolding compatibility)
 * - remove debug message for default loot profile (no more console spam)
 * - add the ability to create / load config backups
 * - fix gathered items always going into hotbar
 * 
 * V 1.0.28
 * - lower min item time for quarry config from 0.1 to 0.01
 * - change uint net ids to NetworkableId
 * 
 * V 1.0.29
 * - rework npc loot system
 * - add npc configs for lauch site, missile silo, power plant
 * - persist item.text when splitting items, don't stack items with different item.text
 * - fix furnace speed not working when using igniter or button to start furnace
 * - fix conflict with deployable nature
 * - add support for "everything" quarry
 * 
 * V 1.0.30
 * - fix possible NRE in OnPluginLoaded
 * - change MiniCopter to PlayerHelicopter
 * - add AttackHelicopter to fuel config
 * - fix loot additions not working properly
 * - rework loot refresh to be more performant
 * - misc improvements
 * 
 * V 1.0.31
 * - fix supply drop issues
 * - add page navigation for static lootables
 * - misc fixes/improvements
 * 
**/

namespace Oxide.Plugins
{

    [Info("Loottable", "The_Kiiiing", "1.0.31")]
    [Description("A configurable loot table with GUI")]
    public class Loottable : RustPlugin
    {

        #region Fields

        [PluginReference]
        private Plugin ImageLibrary, StaticLootables, LootDefender, FancyDrop, SimpleSplitter, BetterNpc, DefendableHomes, DeployableNature;

        private static bool UseStaticLootables => (bool)_instance.StaticLootables;

        private const int configVersion = 8;

        private const string COMMAND = "loottable";
        private const string UI_COMMAND = "loottable.cmd";
        private const string BACKUP_COMMAND = "loottable.backup";
        private const string PERM_EDIT = "loottable.edit";
        private const string PERM_DEBUG = "loottable.debug";

        private static readonly NumberFormatInfo DEFAULT_NUMBER_FORMAT = new CultureInfo("en", false).NumberFormat;
        private static NumberFormatInfo NUMBER_FORMAT = DEFAULT_NUMBER_FORMAT;

        private static new string Name => ((RustPlugin)_instance).Name;

        private static EditorConfig Flags;

        private static Loottable _instance;

#if CARBON
        ImageDatabaseModule imageDatabase;
#endif

        #endregion

        #region Editor Config

        private static void InitializeEditorConfig()
        {
            Flags = Interface.Oxide.DataFileSystem.ReadObject<EditorConfig>(DataFilePath("editorconfig"));
            if (Flags == null)
            {
                CErr("Editor config is corrupted, creating a new one");
                Flags = new EditorConfig();
            }
            Flags.Validate();
            SaveEditorConfig();
        }

        private static void SaveEditorConfig()
        {
            Interface.Oxide.DataFileSystem.WriteObject<EditorConfig>(DataFilePath("editorconfig"), Flags);
        }

        private class EditorConfig
        {
            [JsonProperty("version")]
            private int version = configVersion;

            public int ItemLimit { get; private set; } = 1000000;

            public MinMaxFloat GatherMultiplier { get; private set; } = new MinMaxFloat(1f, 1000f);

            public MinMaxFloat FurnaceMultiplier { get; private set; } = new MinMaxFloat(0.1f, 100f);

            public MinMaxFloat ItemMultiplier { get; private set; } = new MinMaxFloat(0.1f, 10f);

            [JsonIgnore]
            public bool Debug => flags["Debug"];
            [JsonIgnore]
            public bool UnlockGatherMultiplier => flags["UnlockGatherMultiplier"];
            [JsonIgnore]
            public bool DisableItemLimit => flags["DisableItemLimit"];
            [JsonIgnore]
            public bool UnlockFurnaceMultiplier => flags["UnlockFurnaceMultiplier"];
            [JsonIgnore]
            public bool UnlockItemMultiplier => flags["UnlockItemMultiplier"];
            [JsonIgnore]
            public bool DisableStackingHooks => flags["DisableStackingHooks"];
            [JsonIgnore]
            public bool RefreshLootOnExit => flags["RefreshLootOnExit"];

            [JsonIgnore]
            public bool Update { get; } = false;

            public Dictionary<string, bool> flags { get; private set; } = new Dictionary<string, bool>
            {
                {"Debug", false },
                {"UnlockGatherMultiplier", false },
                {"DisableItemLimit", false },
                {"UnlockFurnaceMultiplier", false },
                {"UnlockItemMultiplier", false },
                {"DisableStackingHooks", false },
                {"RefreshLootOnExit", true }
            };

            public EditorConfig() { }

            [JsonConstructor]
            public EditorConfig(int version, int ItemLimit, int ItemLimitUnlocked, MinMaxFloat GatherMultiplier, MinMaxFloat FurnaceMultiplier, Dictionary<string, bool> flags)
            {
                if (version != configVersion) Update = true;

                this.version = configVersion;
                this.ItemLimit = ItemLimit;
                this.GatherMultiplier = GatherMultiplier;
                this.FurnaceMultiplier = FurnaceMultiplier;

                foreach(var flag in flags)
                    this.flags[flag.Key] = flag.Value;

                Save();

                if (Update) CPrint("Update detected, performing reload of remote content");
            }

            public void Validate()
            {
                EditorConfig validator = new EditorConfig();

                if (ItemLimit == 0)
                    ItemLimit = validator.ItemLimit;

                if (GatherMultiplier == MinMaxFloat.Null)
                    GatherMultiplier = validator.GatherMultiplier;

                if (FurnaceMultiplier == MinMaxFloat.Null)
                    FurnaceMultiplier = validator.FurnaceMultiplier;

                foreach (var flag in flags.ToList())
                {
                    if (!validator.flags.ContainsKey(flag.Key))
                        flags.Remove(flag.Key);
                }

                foreach (var vflag in validator.flags)
                {
                    if (!flags.ContainsKey(vflag.Key))
                        flags.Add(vflag.Key, false);
                }
            }

            public bool TrySetFlag(string flag, string value, out bool newValue)
            {
                bool val;
                newValue = false;
                value = value.Replace("0", Boolean.FalseString).Replace("1", Boolean.TrueString);

                if (!flags.ContainsKey(flag)) return false;
                if (!Boolean.TryParse(value, out val)) return false;

                flags[flag] = val;
                newValue = val;
                Save();
                return true;
            }

            public void Save() => SaveEditorConfig();
        }

        private static string DataFilePathFull() => Path.Combine(Interface.Oxide.DataDirectory, Name);
        private static string DataFilePathFull(string file) => Path.Combine(Interface.Oxide.DataDirectory, Name, file);
        private static string DataFilePathFull(string folder, string file) => Path.Combine(Interface.Oxide.DataDirectory, Name, folder, file);

        private static string DataFilePath(string file) => String.Format("{0}/{1}", Name, file);
        private static string DataFilePath(string folder, string file) => String.Format("{0}/{1}/{2}", Name, folder, file);

        private static long CurrentUnixTime() => ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();

        #endregion

        #region Image Library

        private void LoadImages()
        {
            Dictionary<string, string> images = LootManager.GetImageDictionary();
            images = images.MergeWith(ConfigManager.GetCustomImages());

            Puts($"Import {images.Count} images");

#if CARBON
            imageDatabase.Queue(Flags?.Update ?? false, images);
#else
            timer.In(1f, () =>
            {
                if (!ImageLibrary)
                    PrintError("ERROR: ImageLibrary not found, you can download it here: https://umod.org/plugins/image-library");
                else
                    ImageLibrary?.Call("ImportImageList", Title, images, 0UL, Flags?.Update ?? false);
            });
#endif
        }

        private CuiImageComponent CreateItemImage(int itemid, ulong skin = 0)
        {
            if (skin == 0)
                return new CuiImageComponent { ItemId = itemid };
            else
                return new CuiImageComponent { ItemId = itemid, SkinId = skin };
        }

        private CuiRawImageComponent CreateImage(string key)
        {
#if CARBON
            return new CuiRawImageComponent
            {
                Png = imageDatabase.GetImageString(key, silent: true)
            };
#else
            return new CuiRawImageComponent
            {
                Png = ImageLibrary?.Call<string>("GetImage", key)
            };
#endif
        }

        #endregion

        #region Backup

        private void CmdBackup(IPlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.Id, PERM_DEBUG))
            {
                player.Reply("You don't have permission to do that");
                return;
            }

            if (args.Length < 2)
            {
                player.Reply($"Invalid args! Usage:\n    {command} load <name> - Load backup with the given name from the backups folder" +
                    $"\n    {command} create <name> - Create backup of everything with the given name");
                    // TODO uncomment when enabling scoped backups
                    //$"\n    {command} create <name> <crates|gathering|stacksize|config|all> - Create a backup of the specified configurations with the given name");
                return;
            }

            string name = args[1];
            if (args[0] == "load")
            {
                string result = BackupManager.LoadBackupFile(name);
                player.Reply(result);
            }
            else if (args[0] == "create")
            {
                string target = args.Length == 3 ? args[2] : "all";
                string result = BackupManager.CreateBackupFile(name, target);
                player.Reply(result);
            }
        }

        private static class BackupManager
        {
            public enum BackupScope 
            { 
                NONE = 0x00,
                CRATES = 0x01, 
                GATHERING = 0x02, 
                STACKSIZE = 0x04, 
                CONFIG = 0x08,
                
                ALL = CRATES | GATHERING | STACKSIZE | CONFIG
            }

            private const string BACKUP_FOLDER = "backups";
            private const string BACKUP_EXTENSION = "lootprofile";

            private static bool ExcludeFile(string nameWithExtension)
            {
                // Protect from potentially malicious files
                if (!nameWithExtension.EndsWith(".json"))
                {
                    return true;
                }

                // Other unwanted files
                return nameWithExtension == "editorconfig.json"
                    || nameWithExtension == "custom_items.json"
                    || nameWithExtension == "custom_items_v2.json";
            }

            // TODO apply backup scope
            public static string CreateBackupFile(string name, string target)
            {
                BackupScope scope;
                if (!Enum.TryParse<BackupScope>(target, true, out scope) || scope == BackupScope.NONE)
                {
                    return $"Failed to create backup - Invalid selector '{target}'";
                }

                CheckBackupDirectory();

                string backupFile = DataFilePathFull(BACKUP_FOLDER, $"{name}.{BACKUP_EXTENSION}");
                if (File.Exists(backupFile))
                {
                    return $"Failed to create backup - File {backupFile} already exists";
                }

                List<FileBackup> backups = new List<FileBackup>();
                try
                {
                    foreach (var file in Directory.GetFiles(DataFilePathFull()))
                    {
                        string fileName = Path.GetFileName(file);

                        if (ExcludeFile(fileName)) continue;

                        string contents = File.ReadAllText(file);
                        var backup = new FileBackup(fileName);
                        backup.CompressData(contents);

                        backups.Add(backup);
                    }

                    var fileStream = File.Create(backupFile);
                    SerializeMany(backups, fileStream);
                    fileStream.Dispose();
                }
                catch (Exception ex)
                {
                    return $"An exception occured while creating a backup: {ex}";
                }
                
                return $"Backup {backupFile} successfully created";
            }

            public static string LoadBackupFile(string name)
            {
                CheckBackupDirectory();

                string backupFile = DataFilePathFull(BACKUP_FOLDER, $"{name}.{BACKUP_EXTENSION}");
                if (!File.Exists(backupFile))
                {
                    return $"Failed to load backup - file {backupFile} does not exist";
                }

                try
                {
                    var fileStream = File.OpenRead(backupFile);
                    List<FileBackup> backups = DeserializeMany<FileBackup>(fileStream);

                    foreach (var backup in backups)
                    {
                        if (ExcludeFile(backup.OriginalName)) continue;

                        string fileName = DataFilePathFull(backup.OriginalName);
                        if (File.Exists(fileName))
                        {
                            File.Delete(fileName);
                        }
                        string data = backup.DecompressData();
                        File.WriteAllText(fileName, data);
                    }
                }
                catch (Exception ex)
                {
                    return $"An exception occured while loading a backup: {ex}";
                }

                _instance.timer.In(2f, () => ConsoleSystem.Run(ConsoleSystem.Option.Server, "o.reload", Name));

                return "Backup sucessfully loaded, plugin will reload to apply the changes";
            }

            private static void CheckBackupDirectory()
            {
                string path = DataFilePathFull(BACKUP_FOLDER);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }

            #region Protobuf

            [ProtoContract]
            private class FileBackup
            {
                [ProtoMember(1)]
                public string OriginalName { get; set; }
                [ProtoMember(2)]
                public byte[] CompressedData { get; set; }

                public FileBackup() { }

                public FileBackup(string fileName)
                {
                    OriginalName = fileName;
                }

                #region Compress / Decompress

                public void CompressData(string json)
                {
                    var bytes = Encoding.UTF8.GetBytes(json);

                    var msi = new MemoryStream(bytes);
                    var mso = new MemoryStream();
                    var gs = new GZipStream(mso, CompressionMode.Compress);

                    msi.CopyTo(gs);
                    gs.Dispose();

                    CompressedData = mso.ToArray();

                    // WHY DOES THIS ANCIENT COMPILER NOT SUPPORT FUCKING USINGS?
                    msi.Dispose();
                    mso.Dispose();
                }

                public string DecompressData()
                {
                    var msi = new MemoryStream(CompressedData);
                    var mso = new MemoryStream();
                    var gs = new GZipStream(msi, CompressionMode.Decompress);

                    gs.CopyTo(mso);
                    gs.Dispose();

                    string result = Encoding.UTF8.GetString(mso.ToArray());

                    msi.Dispose();
                    mso.Dispose();

                    return result;
                }

                #endregion
            }

            private static void SerializeMany<T>(IEnumerable<T> data, Stream stream)
            {
                foreach (var obj in data)
                {
                    Serializer.SerializeWithLengthPrefix<T>(stream, obj, PrefixStyle.Fixed32);
                }
            }

            private static List<T> DeserializeMany<T>(Stream stream)
            {
                List<T> list = new List<T>();

                T obj;
                do
                {
                    obj = Serializer.DeserializeWithLengthPrefix<T>(stream, PrefixStyle.Fixed32);
                    if (obj != null)
                    {
                        list.Add(obj);
                    }
                } while (obj != null);

                return list;
            }

            #endregion
        }

        #endregion

        #region Custom Hooks

        private static bool CanPopulateContainerHook(LootContainer container)
        {
            object result = Interface.CallHook("OnContainerPopulate", container);
            return result == null;
        }

        private static bool CanPopulateCorpseHook(LootableCorpse corpse)
        {
            object result = Interface.CallHook("OnCorpsePopulate", corpse);
            return result == null;
        }

        private static bool CanUseCustomAirdrop(SupplySignal signal)
        {
            object result = Interface.CallHook("OnCustomAirdrop", signal);
            return result == null;
        }

        #endregion

        #region Hooks

        #region Loot

        object OnLootSpawn(LootContainer container)
        {
            LootableConfig lootConfig = LootManager.GetContainerConfig(container);
            if (lootConfig == null) return null;

            if (!CanPopulateContainerHook(container)) return null;

            if (lootConfig.Enabled)
            {
                if (lootConfig.lootType != LootableConfig.LootType.Custom)
                {
                    container.PopulateLoot();
                }

                lootConfig.ApplyToCrate(container);

                if (container.shouldRefreshContents)
                {
                    DPrint($"{container.ShortPrefabName} should refresh");

                    container.CancelInvoke(container.SpawnLoot);
                    container.Invoke(container.SpawnLoot, UnityEngine.Random.Range(container.minSecondsBetweenRefresh, container.maxSecondsBetweenRefresh));
                }

                return false;
            }

            return null;
        }

        #endregion

        #region Quarries

        void OnQuarryToggled(MiningQuarry quarry, BasePlayer player)
        {
            if (!quarry.isStatic) return;

            var config = LootManager.GetQuarryConfig(quarry);

            if (quarry.IsEngineOn())
                config.QuarryInit(ref quarry);
            else
                config.ResetQuarry(ref quarry);
        }

        void OnQuarryGather(MiningQuarry quarry, Item item)
        {
            if (!quarry.isStatic) return;

            var config = LootManager.GetQuarryConfig(quarry);
            config.QuarryUpdate(ref quarry, ref item);
        }

        void OnExcavatorMiningToggled(ExcavatorArm arm)
        {
            var config = LootManager.GetExcavatorConfig();
            config.Apply(ref arm);
        }

        object OnExcavatorGather(ExcavatorArm arm, Item item)
        {
            var config = LootManager.GetExcavatorConfig();
            config.GatherUpdate(ref arm, item);
            return null;
        }

        #endregion

        #region NPCs

        void OnEntitySpawned(LootableCorpse corpse)
        {
            var config = LootManager.GetNpcConfig(corpse);
            if (config == null) return;

            if (!CanPopulateCorpseHook(corpse)) return;

            if (config.Enabled)
            {
                timer.In(0.1f, () =>
                {
                    if (!corpse.IsDestroyed)
                    {
                        config.ApplyToCorpse(corpse?.containers[0], true);
                    }
                });
            }
        }

        void OnEntitySpawned(BasePlayer npc)
        {
            if (npc.IsNpc) LootManager.CacheNpc(npc);
        }

        #endregion

        #region Gathering

        object OnCollectiblePickup(CollectibleEntity entity, BasePlayer player)
        {
            if (player == null || entity == null) return null;

            if (IsDeployableNature(entity))
            {
                return null;
            }

            CollectibleConfig config = LootManager.GetCollectibleConfig(entity);
            if (config == null) return null;
            if (!config.enabled) return null;

            config.DoPickup(entity, player);
            return false;
        }

        object OnDispenserGather(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            BaseEntity ent = dispenser.GetComponent<BaseEntity>();

            GatherConfig config = GatherManager.gatherConfig;
            if (config == null) return null;
            if (!config.enabled) return null;

            item.amount = config.GetNewAmount(ent.ShortPrefabName, item.info.itemid, item.amount);

            return null;
        }

        void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            BaseEntity ent = dispenser.GetComponent<BaseEntity>();

            GatherConfig config = GatherManager.gatherConfig;
            if (config == null) return;
            if (!config.enabled) return;

            item.amount = config.GetNewAmount(ent.ShortPrefabName, item.info.itemid, item.amount);
        }

        void OnGrowableGathered(GrowableEntity plant, Item item, BasePlayer player)
        {
            GatherConfig config = GatherManager.gatherConfig;
            if (config == null) return;
            if (!config.enabled) return;

            item.amount = config.GetNewGrowableAmount(plant.ShortPrefabName, item.amount);
        }

        private bool IsDeployableNature(BaseEntity entity)
        {
            if (DeployableNature == null)
            {
                return false;
            }

            var b = DeployableNature.Call("STCanGainXP", null, entity) as bool?;
            if (b == false)
            {
                return true;
            }

            return false;
        }

        #endregion

        #region Stacking

        bool? CanStackItem(Item target, Item item, bool combine = false)
        {
            if (Flags.DisableStackingHooks) return null;

            if ((target.GetOwnerPlayer().IsUnityNull() || item.GetOwnerPlayer().IsUnityNull()) && !combine) return null;
            if (target.info.itemid != item.info.itemid) return false;
            if (target.skin != item.skin) return false;
            if (target.name != item.name) return false;
            if (target.text != item.text) return false;
            if (target.condition != item.condition) return false;
            if (target.blueprintTarget != item.blueprintTarget) return false;

            if (target.info.amountType == ItemDefinition.AmountType.Genetics || item.info.amountType == ItemDefinition.AmountType.Genetics)
            {
                if ((target.instanceData?.dataInt ?? -1) != (item.instanceData?.dataInt ?? -1))
                {
                    return false;
                }
            }

            if (item.GetHeldEntity() is BaseProjectile && !combine)
            {
                BaseProjectile.Magazine mag = (item.GetHeldEntity() as BaseProjectile).primaryMagazine;
                if (mag.contents > 0)
                {
                    target.parent.GetOwnerPlayer().GiveItem(ItemManager.Create(mag.ammoType, mag.contents));
                    mag.contents = 0;
                }
            }

            if (item.GetHeldEntity() is Chainsaw && !combine)
            {
                Chainsaw chainsaw = item.GetHeldEntity() as Chainsaw;
                if (chainsaw.ammo > 0)
                {
                    target.GetOwnerPlayer().GiveItem(ItemManager.Create(chainsaw.fuelType, chainsaw.ammo));
                    chainsaw.ammo = 0;
                }
            }

            if (item.GetHeldEntity() is FlameThrower && !combine)
            {
                FlameThrower flameThrower = item.GetHeldEntity() as FlameThrower;
                if (flameThrower.ammo > 0)
                {
                    target.GetOwnerPlayer().GiveItem(ItemManager.Create(flameThrower.fuelType, flameThrower.ammo));
                    flameThrower.ammo = 0;
                }
            }

            // target.info.stackable > target.amount prevents infinite fuel glitch
            if ((item.info.shortname == "hat.miner" || item.info.shortname == "hat.candle") && (item.contents?.itemList.Count ?? 0) > 0 && !combine && target.info.stackable > target.amount)
            {
                // Check if all items can be moved to new stack
                // Only take out fuel if old stack gets removed
                if (item.amount - (item.info.stackable - target.amount) <= 0)
                {
                    Item content = item.contents.itemList.First();

                    Item drop = ItemManager.Create(content.info, content.amount, content.skin);
                    item.GetOwnerPlayer().GiveItem(drop);
                }
            }

            if (item.GetHeldEntity() is BaseLiquidVessel)
            {
                if (!item.contents.IsEmpty() || !target.contents.IsEmpty()) return false;
            }

            return true;
        }

        Item OnItemSplit(Item item, int amount)
        {
            if (Flags.DisableStackingHooks) return null;

            if (DefendableHomes != null)
            {
                if (item.skin == 2591851360
                    || item.skin == 2817854052
                    || item.skin == 2817854377
                    || item.skin == 2817854677
                    || item.skin == 2888602635
                    || item.skin == 2888602942
                    || item.skin == 2888603247)
                    return null;
            }

            item.amount -= amount;
            Item split = ItemManager.Create(item.info, amount, item.skin);
            split.name = item.name;
            split.text = item.text;
            split.condition = item.condition;
            if (item.IsBlueprint()) split.blueprintTarget = item.blueprintTarget;

            item.MarkDirty();

            if (item.info.amountType == ItemDefinition.AmountType.Genetics && item.instanceData != null && item.instanceData.dataInt != 0)
            {
                split.instanceData = new ProtoBuf.Item.InstanceData()
                {
                    dataInt = item.instanceData.dataInt,
                    ShouldPool = false
                };
            }

            if (split.GetHeldEntity() is BaseProjectile)
            {
                BaseProjectile weapon = split.GetHeldEntity() as BaseProjectile;
                weapon.primaryMagazine.contents = 0;
                weapon.SendNetworkUpdateImmediate();
            }

            if (split.GetHeldEntity() is Chainsaw)
            {
                Chainsaw chainsaw = split.GetHeldEntity() as Chainsaw;
                if (chainsaw.ammo > 0)
                {
                    chainsaw.ammo = 0;
                    chainsaw.SendNetworkUpdateImmediate();
                }
            }

            if (split.GetHeldEntity() is FlameThrower)
            {
                FlameThrower flameThrower = split.GetHeldEntity() as FlameThrower;
                if (flameThrower.ammo > 0)
                {
                    flameThrower.ammo = 0;
                    flameThrower.SendNetworkUpdateImmediate();
                }
            }

            if (split.GetHeldEntity() is BaseLiquidVessel)
            {
                split.contents.ForceClear();
                //item.contents.Clear();
            }

            if (item.contents != null)
            {
                if (split.contents == null)
                {
                    split.contents = new ItemContainer();
                    split.contents.ServerInitialize(split, item.contents.capacity);
                    split.contents.GiveUID();
                }
                else
                {
                    split.contents.capacity = item.contents.capacity;
                }
            }

            split.MarkDirty();
            return split;
        }

        private void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (Flags.DisableStackingHooks) return;

            BasePlayer player = container.GetOwnerPlayer();

            if (player?.IsNpc ?? true) return;

            if (player.inventory.containerWear.uid == container.uid && item.amount > 1)
            {
                int amount = item.amount - 1;
                item.amount = 1;
                Item split = ItemManager.Create(item.info, amount, item.skin);
                split.name = item.name;
                split.text = item.text;
                split.condition = item.condition;

                item.MarkDirty();
                split.MarkDirty();

                split.MoveToContainer(player.inventory.containerMain);
            }
        }

        bool? CanCombineDroppedItem(DroppedItem item1, DroppedItem item2)
        {
            bool? res = CanStackItem(item1.item, item2.item, true);

            // Return null if hook returned null or true
            if (res ?? true)
            {
                return null;
            }
            return false;
        }

        #endregion

        #region Heli Fuel

        private void OnEntitySpawned(PlayerHelicopter heli)
        {
            if (!ConfigManager.IsReady) return;

            NextTick(() =>
            {
                if (heli.creatorEntity == null) return;

                EntityFuelSystem fs = heli?.GetFuelSystem();
                if (fs == null) return;

                int fuel = -1;
                if (heli is ScrapTransportHelicopter)
                {
                    fuel = ConfigManager.AirwolfConfig.scrapHeliFuel;
                }
                else if (heli is Minicopter)
                {
                    fuel = ConfigManager.AirwolfConfig.minicopterFuel;
                }
                else if (heli is AttackHelicopter)
                {
                    fuel = ConfigManager.AirwolfConfig.attackHeliFuel;
                }

                if (fuel < 0) return;

                Item fuelItem = fs.GetFuelItem();
                if (fuelItem != null)
                {
                    if (fuel == 0)
                    {
                        fuelItem.Remove();
                    }
                    else if (fuel != fuelItem.amount)
                    {
                        fuelItem.amount = fuel;
                        fuelItem.MarkDirty();
                    }
                }
            });
        }

        #endregion

        #region Smelting & Mixing

        private void OnOvenStarted(BaseOven oven)
        {
            if (!ConfigManager.IsReady || !ConfigManager.FurnaceConfig.enabled)
            {
                return;
            }

            ConfigManager.FurnaceConfig.UpdateOvenSpeed(oven);
        }


        private void OnMixingTableToggle(MixingTable table, BasePlayer player)
        {
            if (!ConfigManager.IsReady || !ConfigManager.FurnaceConfig.enabled) return;

            ConfigManager.FurnaceConfig.ToggleMixingTable(table);
        }

        #endregion

        #region Recycler

        object OnRecyclerToggle(Recycler recycler, BasePlayer player)
        {
            if (!(ConfigManager.FurnaceConfig?.recyclerEnabled ?? false)) return null;

            ConfigManager.FurnaceConfig.ToggleRecycler(recycler);

            return null;
        }

        #endregion

        #region Air Drop

        void OnExplosiveDropped(BasePlayer player, BaseEntity entity, ThrownWeapon item) => OnExplosiveThrown(player, entity, item);

        void OnExplosiveThrown(BasePlayer player, BaseEntity entity, ThrownWeapon item)
        {
            if (!ConfigManager.IsReady || !ConfigManager.AirdropConfig.enabled || entity is not SupplySignal signal)
            {
                return;
            }

            ConfigManager.AirdropConfig.OnSupplySignalThrown(signal);
        }

        #endregion

        #region General

        void OnServerInitialized()
        {
            PrintWarning("\n-----------------------------------------------------------------------------------------\n" +
           "     Загрузка плагина...\n" +
           "     Этот плагин был скачан с канала Discord [Rust Plugin Sliv]\n" +
           "     Этот плагин исправлен Инкубом под заказ [Rust Plugin Sliv]\n" +
           "     DISCORD: https://discord.gg/pFgKw6Dyyq\n" +
           "     Приятного использования!\n" +
           "-----------------------------------------------------------------------------------------");
            LootManager.Initialize();
            LoadImages();

            StackManager.Initialize();
            GatherManager.Initialize();
            ConfigManager.Initialize();
            TrainLootManager.Initialize();

            LootManager.PopulateNpcCache();
            LootManager.RefreshCrateLoot(null);
        }

        void Init()
        {
            _instance = this;

            InitializeEditorConfig();

#if CARBON
            DPrint("Using Carbon's built-in Image Database");
            imageDatabase = BaseModule.GetModule<ImageDatabaseModule>();
#endif

            AddCovalenceCommand(COMMAND, nameof(CMD_loottable));
            AddCovalenceCommand(UI_COMMAND, nameof(CMD_cmd));
            AddCovalenceCommand(BACKUP_COMMAND, nameof(CmdBackup));

            permission.RegisterPermission(PERM_EDIT, this);
            permission.RegisterPermission(PERM_DEBUG, this);

            UiHelper.EditorCache.Clear();
            CustomItemStorage.LoadCustomItems();

            if (Flags.DisableStackingHooks)
            {
                Unsubscribe(nameof(CanStackItem));
                Unsubscribe(nameof(CanCombineDroppedItem));
                Unsubscribe(nameof(OnItemSplit));
                Unsubscribe(nameof(OnItemAddedToContainer));
            }
        }

        void Unload()
        {
            SaveEditorConfig();
            UiHelper.DestroyUI();
            TrainLootManager.Shutdown();
            ConfigManager.FurnaceConfig.ResetBurnable();
            ConfigManager.FurnaceConfig.InitializeFurnaces();
            StackManager.RevertStacksize(true);
            LootManager.RefreshCrateLoot(null, true);
            CustomItemStorage.SaveCustomItems();
        }

        object CanSpectateTarget(BasePlayer player, string filter)
        {
            if (permission.UserHasPermission(player.UserIDString, PERM_EDIT) && UiHelper.uiUser == player.userID)
            {
                return false;
            }

            return null;
        }

        void OnPluginUnloaded(Plugin name)
        {
            CustomItemStorage.RemoveCustomItems(name);
        }

        void OnPluginLoaded(Plugin name)
        {
            if (name != null && name.Name == nameof(SimpleSplitter))
            {
                ConfigManager.FurnaceConfig.Validate();
            }
        }

        #endregion

        #endregion

        #region Train Wagon Loot

        private static class TrainLootManager
        {
            private const string HARMONY_ID = "com.the_kiiiing.loottable";

            private static HarmonyInstance _harmony;

            public static void Initialize()
            {
                _harmony = HarmonyInstance.Create(HARMONY_ID);
                _harmony.Patch(AccessTools.Method(typeof(TrainCarUnloadable), "FillWithLoot"), new HarmonyMethod(typeof(TrainCarUnloadable_FillWithLoot), "Prefix"));
            }

            public static void Shutdown()
            {
                _harmony.UnpatchAll(HARMONY_ID);
            }
        }

        private static class TrainCarUnloadable_FillWithLoot
        {
            private static bool Prefix(StorageContainer sc, TrainCarUnloadable __instance)
            {
                if (__instance.wagonType == WagonType.Lootboxes)
                {
                    // Continue with original
                    return true;
                }

                sc.inventory.Clear();
                ItemManager.DoRemoves();
                int lootTypeIndex;
                TrainWagonLootData.LootOption lootOption = TrainWagonLootData.instance.GetLootOption(__instance.wagonType, out lootTypeIndex);
                __instance.SetLootTypeIndex(lootTypeIndex);

                LootableConfig lootConfig = LootManager.GetTrainCarConfig(lootTypeIndex);
                if (lootConfig == null || !lootConfig.Enabled)
                {
                    DPrint("Wagon config is null or disabled");
                    int amount = UnityEngine.Random.Range(lootOption.minLootAmount, lootOption.maxLootAmount);
                    ItemDefinition itemToCreate = ItemManager.FindItemDefinition(lootOption.lootItem.itemid);
                    sc.inventory.AddItem(itemToCreate, amount, 0uL, ItemContainer.LimitStack.All);
                }
                else
                {
                    DPrint($"Apply wagon loot {lootTypeIndex}");
                    lootConfig.ApplyToTrain(sc);
                }

                sc.inventory.SetLocked(true);
                __instance.SetVisualOreLevel(__instance.GetOrePercent());
                __instance.SendNetworkUpdate();

                return false;
            }
        }

        #endregion

        #region Custom Item API

        private void ClearCustomItems(Plugin plugin)
        {
            CustomItemStorage.RemoveCustomItems(plugin, true);
        }

        private void AddCustomItem(Plugin plugin, int itemId, ulong skinId)
        {
            AddCustomItem(plugin, itemId, skinId, null, false);
        }

        private void AddCustomItem(Plugin plugin, int itemId, ulong skinId, bool persistent)
        {
            AddCustomItem(plugin, itemId, skinId, null, persistent);
        }

        private void AddCustomItem(Plugin plugin, int itemId, ulong skinId, string customName)
        {
            AddCustomItem(plugin, itemId, skinId, customName, false);
        }

        private void AddCustomItem(Plugin plugin, int itemId, ulong skinId, string customName, bool persistent)
        {
            CustomItemStorage.AddCustomItem(new CustomItemStorage.CustomItem { plugin = plugin.Name, itemId = itemId, skinId = skinId, customName = customName, persistent = persistent });
        }

        #endregion

        #region Custom Items

        private class CustomItemStorage
        {
            [JsonIgnore]
            private const string dataFile = "custom_items_v2";

            [JsonProperty]
            public List<CustomItem> persistent = new List<CustomItem>();
            [JsonProperty]
            public List<CustomItem> temp = new List<CustomItem>();
            [JsonProperty]
            private DateTime lastSave;

            [JsonIgnore]
            public const string itemCategory = "custom";
            [JsonIgnore]
            private static CustomItemStorage _storage;
            [JsonIgnore]
            public static List<CustomItem> CustomItems { get; private set; } = new List<CustomItem>();

            // Used for determinig pages in select menu
            [JsonIgnore]
            private const int r_max = 4;
            [JsonIgnore]
            private const int c_max = 10;

            [JsonIgnore]
            public static CustomItem originalItem;
            [JsonIgnore]
            public static CustomItem editingItem;

            #region Load & Save

            public static void LoadCustomItems()
            {
                _storage = Interface.Oxide.DataFileSystem.ReadObject<CustomItemStorage>(DataFilePath(dataFile));
                if (_storage == null || _storage.persistent == null || _storage.temp == null)
                {
                    CErr("Failed to load custom item list");
                    return;
                }

                if (DateTime.UtcNow.Subtract(_storage.lastSave).TotalSeconds < 20)
                {
                    DPrint("Restore custom items from temp storage");
                    CustomItems = _storage.temp;
                }
                else
                {
                    DPrint("Load custom items from persistent storage");
                    CustomItems = _storage.persistent;
                }
            }

            public static void SaveCustomItems()
            {
                _storage = new CustomItemStorage
                {
                    persistent = CustomItems.Where(x => x.persistent).ToList(),
                    temp = CustomItems,
                    lastSave = DateTime.UtcNow
                };

                Interface.Oxide.DataFileSystem.WriteObject(DataFilePath(dataFile), _storage);
            }

            #endregion

            #region Editor

            public static void CreateEditItem(LootItem item)
            {
                originalItem = null;
                editingItem = CustomItem.Default();

                editingItem.itemId = item.itemid;
                editingItem.skinId = item.skin;
                editingItem.customName = item.displayname;
            }

            public static void CreateEditItem()
            {
                originalItem = null;
                editingItem = CustomItem.Default();
            }

            public static void StartEditItem(string guid)
            {
                var item = CustomItems.Find(x => x.Uid == guid);
                if (item == null)
                {
                    CErr($"Failed to find custom item with guid {guid}");
                    return;
                }
                originalItem = item;
                editingItem = item.Clone<CustomItem>();
                editingItem.plugin = Name;
            }

            public static void UpdateEditItem()
            {
                CustomItems.Remove(originalItem);
                CustomItems.Add(editingItem);
            }

            public static void DeleteEditItem()
            {
                CustomItems.Remove(originalItem);
            }

            public static void FinishEditItem()
            {
                originalItem = null;
                editingItem = null;
            }

            #endregion

            public static void AddCustomItem(CustomItem item)
            {
                CustomItem match = CustomItems.Find(x => x.plugin == item.plugin && x.skinId == item.skinId);
                if (match != null)
                {
                    CustomItems.Remove(match);
                    CWarn($"Plugin {item.plugin} is overwriting existing custom item with skin id {item.skinId}. Consider removing custom items before re-registering them");
                }

                CustomItems.Add(item);
                DPrint($"{item.plugin} added custom item {item.customName} persistent: {item.persistent}");
            }

            public static void RemoveCustomItems(Plugin plugin, bool removePersistent = false)
            {
                DPrint($"Remove custom items of {plugin.Name} includePersistent: {removePersistent}");
                CustomItems.RemoveAll(x => x.plugin == plugin.Name && (removePersistent || !x.persistent));
            }

            public static void SortItemList()
            {
                CustomItems = CustomItems.OrderBy(x => x.plugin).ToList();
            }

            public static int GetPages()
            {
                return GetPagesInternal();
            }

            public static List<CustomItem> GetPage(int idx)
            {
                List<CustomItem> page = new List<CustomItem>();
                Action<int, CustomItem> callback = (p_idx, itm) =>
                {
                    if (p_idx == idx)
                    {
                        page.Add(itm);
                    }
                };

                GetPagesInternal(callback, idx);
                return page;
            }

            private static int GetPagesInternal(Action<int, CustomItem> onIteration = null, int breakAfter = -1)
            {
                string plugin = null;
                int pageIdx = 0;
                int r = 0; int c = 0;

                Action check_r = () =>
                {
                    if (r > r_max)
                    {
                        pageIdx++;
                        r = 0;
                    }
                };

                foreach (var item in CustomItems)
                {
                    if (plugin != item.plugin)
                    {
                        if (c != 0)
                        {
                            r++;
                            check_r();
                        }
                        c = 0;
                        plugin = item.plugin;
                    }

                    onIteration?.Invoke(pageIdx, item);

                    //DPrint($"GetPages row {r}");
                    c++;
                    if (c > c_max)
                    {
                        c = 0;
                        r++;
                        check_r();
                    }

                    if (breakAfter >= 0 && pageIdx > breakAfter)
                    {
                        break;
                    }
                }

                //DPrint($"Get pages: {pageIdx}");
                return pageIdx + 1;
            }

            [Serializable]
            public class CustomItem
            {
                public string plugin;
                public int itemId;
                public ulong skinId;
                public string customName;
                public bool persistent = false;

                [JsonIgnore]
                public string Uid { get; } = Guid.NewGuid().ToString();
                [JsonIgnore]
                public bool CanEdit => plugin == Name;

                public static CustomItem Default()
                {
                    return new CustomItem
                    {
                        plugin = Name,
                        itemId = -1579932985,
                        skinId = 420,
                        customName = "Stinky Horse Dung",
                        persistent = true,
                    };
                }
            }
        }

        #endregion

        #region StackManager

        private static class StackManager
        {
            // Vanilla stack size file https://pastebin.com/raw/vtjnscHq

            public static bool stacksizeIsReverted { get; private set; } = true;
            public static StacksizeConfig stacksizeConfig { get; private set; }

            public static Dictionary<string, List<int>> itemCategoryList { get; private set; }
            public static List<ItemDefinition> itemList { get; private set; }

            public static readonly List<int> ignoreItems = new List<int>
            {
                -1779180711, -277057363, // Water + Salt water
                -1759188988, 1426574435, -1884328185, //HAB, MC, ScrapHeli repair
                -810326667, -1364246987, 1768112091, 1394042569, 1878053256, -1449152644, //Wcart, Snowmobile, TSnowmobile, RHIB, Boat, MLRS
                1770744540, 878301596, -44066790, -44066823, -44066600, //Generic VChassis, Generic VModule, 4x chassis, 3x chassis, 2x chassis
                1015352446, -187031121, 550753330, // Duo/Solo Sub, Snowball Ammo
                996757362, -1366326648, -2027988285 // train wagon, spray can decal, locomotive
            };

            public static bool StacksizeIsVanilla(int itemid) => stacksizeConfig.GetVanillaStacksize(itemid) == stacksizeConfig.GetCustomStacksize(itemid);

            public static int GetCurrentStacksize(int itemid) => GetCurrentStacksize(ItemManager.FindItemDefinition(itemid));

            public static int GetCurrentStacksize(ItemDefinition itemDefinition)
            {
                int itemid = itemDefinition.itemid;

                if (ignoreItems.Contains(itemid)) return itemDefinition.stackable;

                int vs = stacksizeConfig.GetVanillaStacksize(itemid),
                    cs = stacksizeConfig.GetCustomStacksize(itemid);

                if (stacksizeConfig.enabled)
                {
                    float cat_mpl = stacksizeConfig.categoryMultipliers[MapItemCategory(itemDefinition.category)];
                    if (vs == cs) return Mathf.RoundToInt(vs * stacksizeConfig.globalMultiplier * cat_mpl);
                    return cs;
                }
                else
                {
                    return vs;
                }
            }

            public static void Initialize()
            {
                itemCategoryList = new Dictionary<string, List<int>>();

                LoadData();
                RefreshItemLists();
                ApplyStacksize();
            }

            public static void Save()
            {
                SaveData();
            }

            private static void RefreshItemLists()
            {
                itemCategoryList = new Dictionary<string, List<int>>();
                itemList = new List<ItemDefinition>();

                if (!itemCategoryList.ContainsKey(CustomItemStorage.itemCategory))
                    itemCategoryList.Add(CustomItemStorage.itemCategory, new List<int>());

                foreach (var item in ItemManager.itemList)
                {
                    if (ignoreItems.Contains(item.itemid)) continue;
                    var cat = item.category.ToString();
                    if (!itemCategoryList.ContainsKey(cat))
                    {
                        itemCategoryList[cat] = new List<int>();
                    }
                    itemList.Add(item);
                    itemCategoryList[cat].Add(item.itemid);
                }
            }

            public static void RevertStacksize(bool force = false)
            {
                if (stacksizeIsReverted && !force) return;

                foreach (ItemDefinition item in ItemManager.itemList)
                {
                    if (ignoreItems.Contains(item.itemid)) continue;
                    item.stackable = stacksizeConfig.GetVanillaStacksize(item.itemid);
                }
                stacksizeIsReverted = true;
            }

            public static void ApplyStacksize()
            {
                if (!stacksizeConfig.enabled)
                {
                    RevertStacksize();
                    return;
                }

                stacksizeIsReverted = false;
                foreach (ItemDefinition item in ItemManager.itemList)
                {
                    if (ignoreItems.Contains(item.itemid)) continue;
                    item.stackable = GetCurrentStacksize(item);
                }
            }

            private static int MapItemCategory(ItemCategory itemCat)
            {
                int cat = 0;
                List<int> cats = new List<int>();
                foreach (ItemDefinition itemdef in ItemManager.itemList)
                {
                    if (!cats.Contains((int)itemdef.category))
                    {
                        cats.Add((int)itemdef.category);
                        if (itemdef.category == itemCat) return cat;
                        cat++;
                    }
                }
                return -1;
            }

            private static void LoadData()
            {
                StacksizeConfig config = Interface.Oxide.DataFileSystem.ReadObject<StacksizeConfig>($"{Name}/stacksize");
                stacksizeConfig = config;
            }

            private static void SaveData()
            {
                Interface.Oxide.DataFileSystem.WriteObject($"{Name}/stacksize", stacksizeConfig);
            }
        }

        private class StacksizeConfig
        {
            [JsonProperty("ssc")]
            private Dictionary<int, int> itemStacksize;
            [JsonProperty("ssv")]
            private Dictionary<int, int> vanillaStacksize;
            [JsonProperty("vanilla")]
            public bool enabled { get; private set; }
            [JsonProperty("catMpl")]
            public Dictionary<int, float> categoryMultipliers;
            [JsonProperty("multiplier")]
            public float globalMultiplier;

            [JsonConstructor]
            public StacksizeConfig(Dictionary<int, int> itemStacksize, Dictionary<int, int> vanillaStacksize, bool enabled, Dictionary<int, float> categoryMultipliers, float globalMultiplier)
            {
                this.itemStacksize = itemStacksize;
                this.vanillaStacksize = vanillaStacksize;
                this.enabled = enabled;
                this.categoryMultipliers = categoryMultipliers;
                this.globalMultiplier = globalMultiplier;
                if (this.categoryMultipliers.IsNullOrEmpty())
                    InitCategoryMultipliers();
            }

            public StacksizeConfig()
            {
                itemStacksize = new Dictionary<int, int>();
                vanillaStacksize = new Dictionary<int, int>();
                enabled = false;
                InitCategoryMultipliers();
                globalMultiplier = 1f;
                SaveVanillaStacksize();
            }

            public int GetVanillaStacksize(int itemid)
            {
                if (!vanillaStacksize.ContainsKey(itemid))
                    vanillaStacksize[itemid] = ItemManager.FindItemDefinition(itemid).stackable;

                return vanillaStacksize[itemid];
            }
            
            public int GetCustomStacksize(int itemid)
            {
                if (!itemStacksize.ContainsKey(itemid))
                    itemStacksize[itemid] = ItemManager.FindItemDefinition(itemid).stackable;

                return itemStacksize[itemid];
            }

            public void SetStacksize(int itemid, int amount)
            {
                amount = Mathf.Clamp(amount, 1, Int32.MaxValue);
                itemStacksize[itemid] = amount;
            }

            public void ResetCategory(int catid)
            {
                foreach(int itemid in StackManager.itemCategoryList.Values.ToList()[catid])
                {
                    itemStacksize[itemid] = vanillaStacksize[itemid];
                }
            }

            public void TryEnable(bool enabled) => this.enabled = enabled;

            private void SaveVanillaStacksize()
            {
                foreach(ItemDefinition item in ItemManager.itemList)
                {
                    if (StackManager.ignoreItems.Contains(item.itemid)) continue;

                    vanillaStacksize[item.itemid] = item.stackable;
                    itemStacksize[item.itemid] = item.stackable;
                }
            }

            private void InitCategoryMultipliers()
            {
                categoryMultipliers = new Dictionary<int, float>();
                int cat = 0;
                List<int> cats = new List<int>();
                foreach(ItemDefinition itemdef in ItemManager.itemList)
                {
                    if (!cats.Contains((int)itemdef.category))
                    {
                        cats.Add((int)itemdef.category);
                        categoryMultipliers[cat] = 1f;
                        cat++;
                    }
                }
            }
        }

        #endregion

        #region GatherManager

        private static class GatherManager
        {
            public static GatherConfig gatherConfig { get; private set; }

            public static void Initialize()
            {
                LoadData();
            }

            public static void Save() => SaveData();

            private static void LoadData()
            {
                GatherConfig config = Interface.Oxide.DataFileSystem.ReadObject<GatherConfig>($"{Name}/gatherconfig");
                
                // Add new items if necessary
                foreach(var entry in GetGatherItems())
                {
                    if (!config.multipliers.ContainsKey(entry.Key))
                    {
                        config.multipliers.Add(entry.Key, entry.Value);
                    }
                }

                gatherConfig = config;
            }

            private static void SaveData()
            {
                Interface.Oxide.DataFileSystem.WriteObject($"{Name}/gatherconfig", gatherConfig);
            }

            public static Dictionary<string, Dictionary<int, float>> GetGatherItems()
            {
                var temp = new Dictionary<string, Dictionary<int, float>>();
                temp["default"] = new Dictionary<int, float> { { -151838493, 1f }, { -2099697608, 1f }, { -4031221, 1f }, { -1982036270, 1f },  { -1157596551, 1f } };
                temp["bear.corpse"] = new Dictionary<int, float> { { -1018587433, 1f }, { 1719978075, 1f }, { 1381010055, 1f }, { -858312878, 1f }, { -1520560807, 1f } };
                temp["polarbear.corpse"] = new Dictionary<int, float> { { -1018587433, 1f }, { 1719978075, 1f }, { 1381010055, 1f }, { -858312878, 1f }, { -1520560807, 1f } };
                temp["boar.corpse"] = new Dictionary<int, float> { { -1018587433, 1f }, { 1719978075, 1f }, { 1381010055, 1f }, { -858312878, 1f }, { 621915341, 1f } };
                temp["wolf.corpse"] = new Dictionary<int, float> { { -1018587433, 1f }, { 1719978075, 1f }, { 1381010055, 1f }, { -858312878, 1f }, { -395377963, 1f }, { 2048317869, 1f} };
                temp["stag.corpse"] = new Dictionary<int, float> { { -1018587433, 1f }, { 1719978075, 1f }, { 1381010055, 1f }, { -858312878, 1f }, { 1422530437, 1f } };
                temp["chicken.corpse"] = new Dictionary<int, float> { { -858312878, 1f }, { 1719978075, 1f }, { -1440987069, 1f } };

                // Growable
                temp["red_berry.entity"] = new Dictionary<int, float> { { 1272194103, 1f } };
                temp["white_berry.entity"] = new Dictionary<int, float> { { 854447607, 1f } };
                temp["yellow_berry.entity"] = new Dictionary<int, float> { { 1660145984, 1f } };
                temp["green_berry.entity"] = new Dictionary<int, float> { { 858486327, 1f } };
                temp["black_berry.entity"] = new Dictionary<int, float> { { 1771755747, 1f } };
                temp["blue_berry.entity"] = new Dictionary<int, float> { { 1112162468, 1f } };
                temp["corn.entity"] = new Dictionary<int, float> { { 1367190888, 1f } };
                temp["potato.entity"] = new Dictionary<int, float> { { -2086926071, 1f } };
                temp["hemp.entity"] = new Dictionary<int, float> { { -858312878, 1f } };
                temp["pumpkin.entity"] = new Dictionary<int, float> { { -567909622, 1f } };

                return temp;
            }
        }

        private class GatherConfig
        {
            [JsonProperty("g")]
            public float globalMultiplier;
            [JsonProperty("mpl")]
            public Dictionary<string, Dictionary<int, float>> multipliers;
            [JsonProperty("enabled")]
            public bool enabled { get; private set; }

            [JsonConstructor]
            public GatherConfig(Dictionary<string, Dictionary<int, float>> multipliers, bool enabled, float globalMultiplier)
            {
                this.multipliers = multipliers;
                this.enabled = enabled;
                this.globalMultiplier = globalMultiplier;
            }

            public GatherConfig()
            {
                this.multipliers = new Dictionary<string, Dictionary<int, float>>();
                this.enabled = false;
                this.globalMultiplier = 1f;
                SetDefaults();
            }

            private void SetDefaults() => multipliers = GatherManager.GetGatherItems();

            public void TryEnable(bool enabled) => this.enabled = enabled;

            public void Reset() => SetDefaults();

            public void SetMultiplier(string prefabName, int itemid, float multi)
            {
                multipliers[prefabName][itemid] = multi;
            }

            public int GetNewAmount(string prefabName, int itemid, int amount)
            {
                if (!enabled) return amount;
                if (multipliers.ContainsKey(prefabName))
                {
                    if (multipliers[prefabName].ContainsKey(itemid)) {
                        if (multipliers[prefabName][itemid] == 1f)
                        {
                            return Mathf.RoundToInt(amount * globalMultiplier);
                        }

                        return Mathf.RoundToInt(amount * multipliers[prefabName][itemid]);
                    }

                    return Mathf.RoundToInt(amount * globalMultiplier);
                }
                else if (multipliers["default"].ContainsKey(itemid))
                {
                    if (multipliers["default"][itemid] == 1f)
                    {
                        return Mathf.RoundToInt(amount * globalMultiplier);
                    }

                    return Mathf.RoundToInt(amount * multipliers["default"][itemid]);
                }

                return Mathf.RoundToInt(amount * globalMultiplier);
            }

            public int GetNewGrowableAmount(string prefabName, int amount)
            {
                if (!enabled) return amount;

                if (multipliers.ContainsKey(prefabName) && multipliers[prefabName].Count > 0)
                {
                    float multi = multipliers[prefabName].First().Value;
                    if (multi != 1f)
                        return Mathf.RoundToInt(amount * multi);
                }

                return Mathf.RoundToInt(amount * globalMultiplier);
            }
        }

        #endregion

        #region ConfigManager

        private static class ConfigManager
        {
            public static readonly List<GenericLootable> genericLootableList = new List<GenericLootable>
            {
                new GenericLootable(LootManager.Lootables.StackSizeControl, "stacksize", "Stack Size Control", "https://cdn.icon-icons.com/icons2/2518/PNG/512/stack_icon_151083.png"),
                new GenericLootable(LootManager.Lootables.GatherConfig, "gathering", "Gather Control", "https://static.wikia.nocookie.net/play-rust/images/8/86/Pick_Axe_icon.png"),
                new GenericLootable(LootManager.Lootables.FurnaceConfig, "smelting", "Furnace Config", "https://static.wikia.nocookie.net/play-rust/images/e/ee/Large_Furnace_icon.png"),
                new GenericLootable(LootManager.Lootables.AirwolfConfig, "airwolf", "Air Wolf Config", "https://static.wikia.nocookie.net/play-rust/images/9/95/494EDB03-BAEA-42BB-8FAE-748F0D2B50A9.png"),
                new GenericLootable(LootManager.Lootables.RecyclerConfig, "recycling", "Recycler Config", "https://static.wikia.nocookie.net/play-rust/images/e/ef/Recycler_icon.png"),
                new GenericLootable(LootManager.Lootables.AirDropConfig, "airdrop", "Air Drop Config", "https://static.wikia.nocookie.net/play-rust/images/2/24/Supply_Signal_icon.png")
            };

            public static readonly LootManager.Lootables[] labelIgnore = new LootManager.Lootables[] {
                LootManager.Lootables.StackSizeControl,
                LootManager.Lootables.AirwolfConfig,
                LootManager.Lootables.FurnaceConfig,
                LootManager.Lootables.RecyclerConfig,
                LootManager.Lootables.AirDropConfig
            };

            public static bool IsReady { get; private set; } = false;

            private static string FurnaceConfigFile => DataFilePath("config_smelting");
            private static string AirwolfConfigFile => DataFilePath("config_airwolf");
            private static string AirdropConfigFile => DataFilePath("config_airdrop");

            public static FurnaceConfiguration FurnaceConfig { get; private set; }
            public static AirwolfConfiguration AirwolfConfig { get; private set; }
            public static AirDropConfiguration AirdropConfig { get; private set; }

            public static void Initialize()
            {
                LoadData();
                FurnaceConfig.Validate();
                FurnaceConfig.UpdateBurnable();
                FurnaceConfig.InitializeFurnaces();
                IsReady = true;
            }

            public static void Save() => SaveData();

            public static void Reset()
            {
                FurnaceConfig = new FurnaceConfiguration();
                AirwolfConfig = new AirwolfConfiguration();
                AirdropConfig = new AirDropConfiguration();
            }

            private static void LoadData()
            {
                FurnaceConfig = Interface.Oxide.DataFileSystem.ReadObject<FurnaceConfiguration>(FurnaceConfigFile);
                AirwolfConfig = Interface.Oxide.DataFileSystem.ReadObject<AirwolfConfiguration>(AirwolfConfigFile);
                AirdropConfig = Interface.Oxide.DataFileSystem.ReadObject<AirDropConfiguration>(AirdropConfigFile);
            }

            private static void SaveData()
            {
                Interface.Oxide.DataFileSystem.WriteObject(FurnaceConfigFile, FurnaceConfig);
                Interface.Oxide.DataFileSystem.WriteObject(AirwolfConfigFile, AirwolfConfig);
                Interface.Oxide.DataFileSystem.WriteObject(AirdropConfigFile, AirdropConfig);
            }

            public static Dictionary<string, string> GetCustomImages()
            {
                return new Dictionary<string, string>
                {
                    ["minicopter"] = "https://static.wikia.nocookie.net/play-rust/images/9/95/494EDB03-BAEA-42BB-8FAE-748F0D2B50A9.png",
                    ["scrapheli"] = "https://cdn.discordapp.com/attachments/948915845587959838/986780519981269052/scrappy0.png",
                    // TODO update image for attack heli
                    ["attack"] = "https://cdn.discordapp.com/attachments/948915845587959838/986780519981269052/scrappy0.png",
                    ["recycler"] = "https://static.wikia.nocookie.net/play-rust/images/e/ef/Recycler_icon.png"
                };
            }
        }

        private class AirDropConfiguration
        {
            [JsonIgnore]
            public const string SMOKE_EFFECT = "assets/bundled/prefabs/fx/smoke_signal_full.prefab";
            [JsonIgnore]
            public const string PLANE_PREFAB = "assets/prefabs/npc/cargo plane/cargo_plane.prefab";
            [JsonIgnore]
            public const string DROP_PREFAB = "assets/prefabs/misc/supply drop/supply_drop.prefab";

            [JsonProperty("enabled")]
            public bool enabled = false;
            [JsonProperty("planeSpeed")]
            public float planeSpeedMultiplier = 1f;
            [JsonProperty("planeHeight")]
            public int planeHeight = 0;
            [JsonProperty("smokeDuration")]
            public float smokeDuration = 60f;
            [JsonProperty("exactDropPosition")]
            public bool exactDropPosition = false;
            [JsonProperty("dropFallSpeed")]
            public float dropFallSpeed = 2f;
            [JsonProperty("dropFallSmoke")]
            public bool dropFallSmoke = false;
            [JsonProperty("randomPosTolerance")]
            public float randomPosTolerance = 20f;

            [JsonIgnore]
            private readonly HashSet<NetworkableId> signals = new();
            
            public void OnSupplySignalThrown(SupplySignal signal)
            {
                if (!enabled || signal == null || _instance.LootDefender != null || _instance.FancyDrop != null 
                    || Interface.CallHook("IsBradleyDrop", signal.skinID) != null 
                    || Interface.CallHook("IsHeliSignalObject", signal.skinID) != null)
                {
                    return;
                }

                if (!CanUseCustomAirdrop(signal))
                {
                    return;
                }

                var netId = signal.net.ID;
                if (signals.Contains(netId)) return;
                signals.Add(netId);

                _instance.timer.In(3f, () =>
                {
                    signal.CancelInvoke(signal.Explode);
                });

                _instance.timer.In(3.3f, () =>
                {
                    signals.Remove(netId);

                    if (signal == null)
                    {
                        CErr("Failed to spawn cargo plane: supply signal is null");
                        return;
                    }

                    signal.Invoke(signal.FinishUp, smokeDuration);

                    signal.SetFlag(BaseEntity.Flags.On, true, false);
                    signal.SendNetworkUpdateImmediate();

                    Vector3 dropPos = signal.transform.position;
                    if (!exactDropPosition)
                    {
                        dropPos.x += UnityEngine.Random.Range(-randomPosTolerance, randomPosTolerance);
                        dropPos.z += UnityEngine.Random.Range(-randomPosTolerance, randomPosTolerance);
                    }

                    CargoPlane plane = (CargoPlane)GameManager.server.CreateEntity(PLANE_PREFAB);

                    Interface.CallHook("OnCargoPlaneSignaled", plane, signal);

                    InitPlane(plane, dropPos);
                });
            }

            private void InitPlane(CargoPlane plane, Vector3? dropPos = null)
            {
                float y = TerrainMeta.HighestPoint.y + planeHeight;

                DropController dc = plane.gameObject.AddComponent<DropController>();
                dc.config = this;
                
                plane.dropped = true;
                if (dropPos != null) plane.InitDropPosition((Vector3)dropPos);

                plane.Spawn();

                plane.startPos.y = y;
                plane.endPos.y = y;
                plane.secondsToTake /= planeSpeedMultiplier;

                dc.Invoke(nameof(dc.SetRunning), 1f);
            }

            public class DropController : MonoBehaviour
            {
                private CargoPlane plane;

                private float lastDist;
                private bool dropped;
                private bool running;

                public AirDropConfiguration config;

                void Awake()
                {
                    plane = GetComponent<CargoPlane>();
                    lastDist = 0;
                    dropped = false;
                    running = false;
                }

                void Update()
                {
                    if (dropped || !running) return;
                    
                    Vector2 pPos = new Vector2(plane.transform.position.x, plane.transform.position.z);
                    Vector2 dPos = new Vector2(plane.dropPosition.x, plane.dropPosition.z);
                    float dist = Vector2.Distance(pPos, dPos);

                    if ((dist > lastDist && lastDist > 0) || dist < 2f)
                    {
                        dropped = true;
                        Vector3 dropPos = plane.dropPosition;
                        dropPos.y = plane.transform.position.y-2f;
                        Drop(dropPos, plane.transform.rotation);
                    }
                    
                    lastDist = dist;
                }

                void Drop(Vector3 pos, Quaternion rot)
                {
                    SupplyDrop drop = (SupplyDrop)GameManager.server.CreateEntity(DROP_PREFAB, pos, rot);
                    
                    drop.GetComponent<Rigidbody>().drag = config.dropFallSpeed; // default 2, lowest 0.6

                    drop.Spawn();

                    Interface.CallHook("OnSupplyDropDropped", drop, plane);

                    if (config.dropFallSmoke)
                    {
                        Effect.server.Run(SMOKE_EFFECT, drop, 0, Vector3.zero, Vector3.up, null, true);
                    }
                }

                public void SetRunning() => running = true;
            }
        }

        private class AirwolfConfiguration
        {
            public int scrapHeliFuel = -1;
            public int minicopterFuel = -1;
            public int attackHeliFuel = -1;
        }

        private class FurnaceConfiguration
        {
            [JsonIgnore]
            private const float recyclerTickRate = 5f;
            [JsonIgnore]
            private const float defaultTickTime = 0.5f;

            [JsonProperty("enabled")]
            public bool enabled = false;
            [JsonProperty("recycler")]
            public bool recyclerEnabled = false;
            [JsonProperty("smallFurnaceSpeed")]
            public float sFsmeltingSpeedMultiplier = 1f;
            [JsonProperty("largeFurnaceSpeed")]
            public float lFsmeltingSpeedMultiplier = 1f;
            [JsonProperty("refinerySpeed")]
            public float rFsmeltingSpeedMultiplier = 1f;
            [JsonProperty("campfireSpeed")]
            public float campfireSmeltingSpeedMultiplier = 1f;
            [JsonProperty("bbqSpeed")]
            public float bbqSmeltingSpeedMultiplier = 1f;
            [JsonProperty("electricSpeed")]
            public float electricFurnaceSpeed = 1f;

            [JsonProperty("recyclerSpeed")]
            public float recyclingSpeedMultiplier = 1f;

            [JsonProperty("mixingSpeed")]
            public float mixingSpeedMultiplier = 1f;

            [JsonProperty("charcoalChance")]
            public float charcoalChance = 0.75f;
            [JsonProperty("charcoalAmount")]
            public int charcoalAmount = 1;

            public void Validate()
            {
                if (_instance.SimpleSplitter != null)
                {
                    enabled = false;
                }
            }

            public void ResetBurnable()
            {
                ItemModBurnable burnable = ItemManager.FindItemDefinition(-151838493).GetComponent<ItemModBurnable>();

                burnable.byproductChance = 0.25f;
                burnable.byproductAmount = 1;
            }

            public void UpdateBurnable()
            {
                if (!enabled)
                {
                    return;
                }

                ItemModBurnable burnable = ItemManager.FindItemDefinition(-151838493).GetComponent<ItemModBurnable>();

                // Invert chance due to a bug in ConsumeFuel:
                // UnityEngine.Random.Range(0f, 1f) > burnable.byproductChance
                burnable.byproductChance = 1f - charcoalChance;

                burnable.byproductAmount = charcoalAmount;
            }

            public void InitializeFurnaces()
            {
                foreach (var oven in BaseNetworkable.serverEntities.OfType<BaseOven>())
                {
                    UpdateOvenSpeed(oven);
                }
            }

            public void ToggleMixingTable(MixingTable table)
            {
                if (!enabled || table.IsOn())
                {
                    return;
                }

                _instance.NextTick(() =>
                {
                    table.RemainingMixTime /= mixingSpeedMultiplier;
                    table.TotalMixTime /= mixingSpeedMultiplier;
                    table.SendNetworkUpdateImmediate();

                    if (table.RemainingMixTime < 1f)
                    {
                        table.CancelInvoke(table.TickMix);
                        table.Invoke(table.TickMix, table.RemainingMixTime);
                    }
                });
            }

            public void ToggleRecycler(Recycler recycler)
            {
                if (!recyclerEnabled || recycler.IsOn())
                {
                    return;
                }

                float recyclerTickTime = recyclerTickRate / recyclingSpeedMultiplier;

                recycler.CancelInvoke(recycler.RecycleThink);

                _instance.NextTick(() =>
                {
                    recycler.InvokeRepeating(recycler.RecycleThink, recyclerTickTime, recyclerTickTime);
                });
            }

            public void UpdateOvenSpeed(BaseOven oven)
            {
                if (!oven.IsOn())
                {
                    return;
                }

                float? speed = GetFurnaceSpeed(oven);
                if (speed == null)
                {
                    return;
                }

                float tickTime = defaultTickTime / (float)speed;
                _instance.NextTick(() =>
                {
                    // Oven go brrrrrrrrrrr
                    oven.CancelInvoke(oven.Cook);
                    oven.InvokeRepeating(oven.Cook, tickTime, tickTime);
                });
            }

            private float? GetFurnaceSpeed(BaseOven oven)
            {
                if (oven.ShortPrefabName == "small_refinery_static" ||
                    oven.ShortPrefabName == "refinery_small_deployed")
                    return rFsmeltingSpeedMultiplier;

                if (oven.ShortPrefabName == "furnace")
                    return sFsmeltingSpeedMultiplier;

                if (oven.ShortPrefabName == "furnace.large")
                    return lFsmeltingSpeedMultiplier;

                if (oven.ShortPrefabName == "campfire")
                    return campfireSmeltingSpeedMultiplier;

                if (oven.ShortPrefabName == "bbq.deployed" ||
                    oven.ShortPrefabName == "bbq.static")
                    return bbqSmeltingSpeedMultiplier;

                if (oven.ShortPrefabName == "electricfurnace.deployed" ||
                    oven.ShortPrefabName == "electricfurnace")
                    return electricFurnaceSpeed;

                return null;
            }
        }

        #endregion

        #region UI Helper

        private static string AspectAdjust(float x, float y, string adjust = "", float aspect = 16f / 9f)
        {
            float nx = x; float ny = y;

            //if (x < 0 && adjust == "x") x = (1 / aspect) + x;
            if (x < 0) x = 1 + x;

            //if (y < 0 && adjust == "y") y = aspect + y;
            if (y < 0) y = 1 + y;

            if (adjust == "x")
            {
                nx = x * (1 / aspect);
            }
            if (adjust == "y")
            {
                ny = y * aspect;
            }

            return $"{nx} {ny}";
        }

        private static string Hex2RGBA(string hex, float a)
        {
            UnityEngine.Color color = UnityEngine.ColorUtility.TryParseHtmlString(hex, out color) ? color : UnityEngine.Color.black;
            int r = Mathf.RoundToInt(color.r * 255f);
            int g = Mathf.RoundToInt(color.g * 255f);
            int b = Mathf.RoundToInt(color.b * 255f);
            return $"{r / 255f} {g / 255f} {b / 255f} {a}";
        }


        private static class UiHelper
        {
            public static void DestroyUI()
            {
                if (uiUser != 0)
                {
                    BasePlayer player = BasePlayer.FindByID(UiHelper.uiUser);
                    Loottable.DestroyUI(player);
                    UnfreezePlayer(player);
                }
            }

            public static void UnfreezePlayer(BasePlayer player)
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, false);
            }

            public static void FreezePlayer(BasePlayer player)
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, true);
                player.CancelInvoke("ServerUpdate");
            }

            public static void SetItemPages()
            {
                LootableConfig config = EditorCache.lootConfig;
                EditorCache.ia_pages.Clear();

                int old_cat = -1;
                IEnumerable<LootItem> displayList;
                bool orderByCat = itemFilter == 1 || itemFilter == 0;
                switch (itemFilter)
                {
                    case 0:
                        displayList = config.items.OrderByDescending(x => x.chance);
                        displayList = displayList.OrderBy(x => x.category);
                        break;
                    case 1:
                        displayList = config.items.OrderByDescending(x => x.chance);
                        displayList = displayList.OrderByDescending(x => x.category);
                        break;
                    case 2:
                        displayList = config.items.OrderBy(x => x.chance);
                        break;
                    case 3:
                        displayList = config.items.OrderByDescending(x => x.chance);
                        break;

                    default:
                        displayList = config.items.OrderBy(x => x.category);
                        break;
                }

                int row = -1; int row_max = 9;
                int col = 0; int col_max = 6;
                int page = 0;

                int ia = 0;
                foreach (var item in displayList)
                {
                    if (old_cat == -1) old_cat = item.category;
                    row++;
                    if (row > row_max || (item.category != old_cat && orderByCat))
                    {
                        row = 0;
                        col++;
                        old_cat = item.category;
                        if (col > col_max)
                        {
                            col = 0;
                            EditorCache.ia_pages[page] = ia;
                            ia = 0;
                            page++;
                        }
                    }

                    ia++;
                }

                EditorCache.pages = page + 1;
            }

            public static List<string> categoryNames = new List<string>
            {
                "Default",
                "Green",
                "Blue",
                "Yellow",
                "Purple",
                "Cyan",
                "Red",
            };

            public static List<string> categoryColors = new List<string>
            {
                "0.22 0.22 0.22 1",          
                Hex2RGBA("#4FD88F", 0.5f),
                Hex2RGBA("#14B5FF", 0.5f),
                Hex2RGBA("#FFC157", 0.5f),
                Hex2RGBA("#E7187C", 0.5f),
                Hex2RGBA("#004093", 0.5f),
                Hex2RGBA("#FF0000", 0.5f)
            };

            public static int maxCategories { get { return categoryColors.Count; } }

            public static string colorRed { get; } = Hex2RGBA("#cc2128", 0.3f);
            public static string colorGreen { get; } = Hex2RGBA("#8ac926", 0.3f);
            public static string colorBlue { get; } = Hex2RGBA("#2185d0", 0.3f);
            public static string colorYellow { get; } = Hex2RGBA("#ffca3a", 0.3f);
            public static string colorYellowBright { get; } = Hex2RGBA("#ffca3a", 0.8f);

            public const string greenButtonColor = "0.415 0.5 0.258 0.4";
            public const string redButtonColor = "0.8 0.254 0.254 0.4";
            public const string greyButtonColor = "0.4 0.4 0.4 0.4";
            public const string transparentColor = "0 0 0 0";

            public const string buttonTextColor = "0.8 0.8 0.8 1";
            public const string textColor = "0.9 0.9 0.9 1";
            public const string greyTextColor = "0.7 0.7 0.7 1";

            public const string bgColor = "0.08 0.08 0.08 1";

            public const string panelColor = "0.22 0.22 0.22 0.5";
            public const string panelColorBright = "0.17 0.17 0.17 1";
            //public const string panelHighlightColor = "0.6 0.6 0.6 0.5";

            public const string regularFont = "robotocondensed-regular.ttf";
            public const string boldFont = "robotocondensed-bold.ttf";

            public const string selectPanelName = "loottable.select";
            public const string editPanelName = "loottable.edit";
            public const string quarryEditPanelName = "loottable.quarryconfig";
            public const string excavatorEditPanelName = "loottable.excavatorconfig";
            public const string stacksizePanelName = "loottable.stacksizeconfig";
            public const string collectiblePanelName = "loottable.collectibleconfig";
            public const string customItemPanelName = "loottable.customitems";
            public const string itemEditPanelName = "loottable.itemedit";
            public const string itemSelectPanelName = "loottable.itemselect";
            public const string gatherPanelName = "loottable.gatherconfig";
            public const string configPanelName = "loottable.genericconfig";
            public const string multiplierOverlayName = "loottable.mpoverlay";
            public const string staticLootablePanelName = "loottable.staticlootables";
            public const string rootPanelName = "loottable.root";
            public const string rootHeaderPanelName = "loottable.root.header";
            public const string rootHeaderButtonPanelName = "loottable.root.header.buttons";

            public static ulong uiUser = 0;

            public static int slPage = 0;

            public static LootableConfig.LootType editLootType = LootableConfig.LootType.Custom;

            public static int select_page = 0;
            public const string sl_page_key = "Static Lootables (Beta)";
            public static Dictionary<string, List<KeyValuePair<string, int[]>>> selectPages = new Dictionary<string, List<KeyValuePair<string, int[]>>>()
            {
                ["Crates"] = new List<KeyValuePair<string, int[]>>
                {
                    new KeyValuePair<string, int[]>("", new int[]{ 60, 1, 2, 3, 12, 13, 14, 15, 28, 24, 25 }),
                    new KeyValuePair<string, int[]>("Diving", new int[]{ 16, 17 }),
                    new KeyValuePair<string, int[]>("Roadside", new int[]{ 22, 23, 40 }),
                    new KeyValuePair<string, int[]>("Special", new int[]{ 18, 19, 20, 21, 39 })
                },
                ["Underwater / Invisible Crates"] = new List<KeyValuePair<string, int[]>>
                {
                    new KeyValuePair<string, int[]>("Underwater Labs", new int[]{ 4, 5, 6, 7, 8, 9, 10, 11, 26, 27 }),
                    new KeyValuePair<string, int[]>("Invisible Crates", new int[]{ 84, 87, 86, 85, 88, 89, 90, 91, 92 })
                },              
                ["NPCs"] = new List<KeyValuePair<string, int[]>>
                {
                    new KeyValuePair<string, int[]>("", new int[]{ 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 102, 101, 103 }),
                },
                ["Gathering"] = new List<KeyValuePair<string, int[]>>
                {
                    new KeyValuePair<string, int[]>("Quarries", new int[]{ 41, 42, 43, 44, 45, 104 }),
                    new KeyValuePair<string, int[]>("Collectable Resources", new int[]{ 62, 63, 64, 65, 66 }),
                    new KeyValuePair<string, int[]>("Collectable Plants", new int[]{ 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77 }),
                    new KeyValuePair<string, int[]>("Gather Control", new int[]{ 78 })
                },
                ["Configuration & Train Wagons"] = new List<KeyValuePair<string, int[]>>
                {
                    new KeyValuePair<string, int[]>("Stack Size", new int[]{ 61 }),
                    new KeyValuePair<string, int[]>("More Configurations", new int[]{ 79, 80, 81, 82 }),
                    new KeyValuePair<string, int[]>("Train Wagons", new int[]{ 93, 94, 95, 96, 97, 98, 99, 100 })
                },
                [sl_page_key] = new List<KeyValuePair<string, int[]>>
                {
                    new KeyValuePair<string, int[]>("Static Lootables not installed", new int[0]),
                },
                ["Deathmatch Crates"] = new List<KeyValuePair<string, int[]>>
                {
                    new KeyValuePair<string, int[]>("", new int[]{ 29, 30, 31, 32, 33, 34, 35, 36, 37, 38 })
                }
            };

            public static readonly List<string> lootConfigStates = new List<string>
            {
                "Vanilla", "Custom Loot", "Black List", "Vanilla + Additions"
            };

            public static readonly List<string> lootConfigStateColors = new List<string>
            {
                colorYellow, colorGreen, colorRed, colorBlue
            };

            public static int itemFilter = 0;
            public static int itemFilterMax { get { return itemFilterNames.Count - 1; } }
            public static List<string> itemFilterNames = new List<string>
            {
                "CATEGORY (ASC)",
                "CATEGORY (DESC)",
                "CHANCE (ASC)",
                "CHANCE (DESC)"
            };

            public static bool gatherPage = false;

            public static LootManager.LootableType editingType;
            public static BaseLootable currentLootable { get; private set; }

            public static CopyPaste copyPaste = new CopyPaste();

            public static class EditorCache
            {
                public enum ItemType { Base, Extra }

                public static int page = 0;
                public static int pages = 0;

                public static Dictionary<int, int> ia_pages = new Dictionary<int, int>();
                public static List<int> ia_pages_values { get { return ia_pages.Values.ToList(); } }

                private static BaseLootable _lootable;
                public static BaseLootable Lootable { set { _lootable = value; currentLootable = value; } get { return _lootable; } }
                public static LootableConfig lootConfig;

                //Item editing
                public static float work = 1f;
                public static bool replace_item = false;
                public static LootItem old_item;
                public static ItemType itemSelectType = ItemType.Base;
                public static int Itemid
                {
                    get;
                    private set;
                } = -932201673;
                public static ItemDefinition ItemDefinition => ItemManager.FindItemDefinition(Itemid);
                public static string Shortname
                {
                    set
                    {
                        var itm = ItemManager.FindItemDefinition(value);
                        if (itm != null) Itemid = itm.itemid;
                    }
                    get
                    {
                        var itm = ItemManager.FindItemDefinition(Itemid);
                        return itm.shortname;
                    }
                }
                public static int item_amount_min = 1;
                public static int item_amount_max = 1;
                public static float item_chance = 0.5f;
                public static ulong item_skin = 0;
                public static int item_category = 0;
                public static string item_name = "";
                public static List<ExtraItem> extras = new List<ExtraItem>();
                public static MinMaxFloat condition = MinMaxFloat.One;
                public static bool isBlueprint;

                public static void Clear()
                {
                    //crate = null;
                    lootConfig = null;

                    work = 1f;
                    replace_item = false;
                    old_item = null;
                    Shortname = "scrap";                  
                    item_amount_min = 1;
                    item_amount_max = 1;
                    item_chance = 0.5f;
                    item_skin = 0;
                    item_category = 0;
                    item_name = "";
                    extras = new List<ExtraItem>();
                    condition = MinMaxFloat.One;
                    isBlueprint = false;
                }

                public static void CacheItem(LootItem item)
                {
                    work = item.work;
                    replace_item = true;
                    old_item = item;
                    Shortname = item.Shortname;
                    item_amount_min = item.amount.min;
                    item_amount_max = item.amount.max;
                    item_chance = item.chance;
                    item_skin = item.skin;
                    item_category = item.category;
                    item_name = item.displayname;
                    extras = item.extras.ToList();
                    condition = item.condition;
                    isBlueprint = item.isBlueprint;
                }

                public static LootItem CreateItemFromCache(bool liquid = false) => new LootItem(Shortname, item_amount_min, item_amount_max, item_chance, item_skin, item_category, item_name, work, liquid, extras.ToList(), condition, isBlueprint);

                public static LootItem FlushItemCache()
                {
                    var itm = new LootItem(Shortname, item_amount_min, item_amount_max, item_chance, item_skin, item_category, item_name, work, extras: extras, condition: condition, isBlueprint: isBlueprint);
                    work = 1f;
                    replace_item = false;
                    old_item = null;
                    Shortname = "scrap";
                    item_amount_min = 1;
                    item_amount_max = 1;
                    item_chance = 0.5f;
                    item_skin = 0;
                    item_category = 0;
                    item_name = "";
                    extras = new List<ExtraItem>();
                    condition = MinMaxFloat.One;
                    isBlueprint = false;
                    return itm;
                }
            }

            public static class QuarryEditorCache
            {
                private static Quarry _dispenser;
                public static Quarry quarry { set { currentLootable = value; _dispenser = value; } get { return _dispenser; } }

                public static QuarryConfig config;

                public static ExcavatorConfig excavatorConfig;
                public static int collectionId;
            }

            public static class Stacksize
            {
                public static int stacksizeCat = 1;
                public static int stacksizePages = 1;
                public static int stacksizePage = 0;
                public const int itemsPerStacksizePage = 36;
            }

            public static class Items
            {
                public static int cat = 0;
                public static int page = 0;
                public static int pages = 0;
                public static int itemsPerPage = 66;
            }

            public static class CollectibleEditorCache
            {
                private static Collectible _coll;
                public static Collectible Collectible { set { currentLootable = value; _coll = value; } get { return _coll; } }

                public static CollectibleConfig config;
            }

            public static class StaticLootables
            {
                public static StaticLootableModels.LootableDefinition Lootable { get; private set; }

                public static StaticLootableModels.LootableDefinition FlushCache()
                {
                    var clone = Lootable.Clone();
                    Lootable = null;
                    return clone;
                }

                public static void SetLootable(StaticLootableModels.LootableDefinition def)
                {
                    Lootable = def.Clone();
                }
            }

            public static class Mutliplier
            {
                public static IMultiplyable target;
                public static float multi = 1f;
            }
        }

        #endregion

        #region Player Commands

        private void CMD_loottable(IPlayer player, string command, string[] args)
        {
            BasePlayer pl;
            if (!player.ToBasePlayer(out pl)) return;

            if (!permission.UserHasPermission(player.Id, PERM_EDIT))
            {
                player.Reply("You don't have permission to do this");
                return;
            }

            if (args.Length > 0)
            {
                switch (args[0].ToLower())
                {
                    case "refresh":
                        LootManager.RefreshCrateLoot(pl);
                        break;

                    case "flags":
                        if (args.Length == 1)
                        {
                            var sb = new StringBuilder("Flag overview:\n");
                            foreach(var flag in Flags.flags)
                            {
                                sb.AppendLine($"  {flag.Key}: {flag.Value}");
                            }
                            player.Reply(sb.ToString());
                        }
                        else if (args.Length == 3)
                        {
                            string key = args[1];
                            string val = args[2];
                            bool v;
                            bool s = Flags.TrySetFlag(key, val, out v);
                            if (s)
                                player.Reply($"The flag {key} has been set to {v}");
                            else
                                player.Reply($"Invalid flag or invalid value");
                        }
                        break;

                    case "import":
                        // Import loot table form json file in oxide/data/Loottable/import 
                        if (args.Length < 2) break;
                        string name = args[1];
                        player.Reply($"Importing Loot table {name}");
                        LootManager.ImportLootTable(name, player);
                        break;

                    case "reload_vanilla_profiles":
                        player.Reply("Re-downloading vanilla loot profiles...");
                        LootManager.LoadDefaultConfigs(true, player);
                        break;

                    default:
                        break;
                }
                return;
            }

            if (UiHelper.uiUser == 0 || UiHelper.uiUser == pl.userID)
            {
                if (UiHelper.uiUser == pl.userID) UiHelper.DestroyUI();
                try
                {
                    var ci = new CultureInfo(lang.GetLanguage(player.Id));
                    NUMBER_FORMAT = ci.NumberFormat;
                }
                catch
                {
                    NUMBER_FORMAT = DEFAULT_NUMBER_FORMAT;
                }
                CreateBaseUI(pl);
                CreateSelectUI(pl);
                UiHelper.FreezePlayer(pl);
            }
            else
            {
                player.Reply("Only one person at a time can edit loot profiles");
            }
        }

        #endregion

        #region GUI Commands

        private void CMD_cmd(IPlayer player, string command, string[] args_0)
        {
            BasePlayer pl;
            if (!player.ToBasePlayer(out pl)) return;

            if (!permission.UserHasPermission(player.Id, PERM_EDIT))
            {
                pl.ChatMessage("You don't have permission to do this");
                return;
            }

            if (args_0.Length < 1) return;

            string[] args = args_0.Trim(1);

            #if DEBUG
            StringBuilder sb = new StringBuilder();
            foreach (var a in args_0) { sb.Append($"{a} "); }
            DPrint($"Action: {sb}");
            #endif

            string action_base = args_0[0];
            switch (action_base)
            {

                #region Select Panel
                // Close ui
                case "close":
                    if (args.Length < 1)
                    {
                        DestroyUI(pl);
                        LootManager.RefreshCrateLoot(pl);
                        StackManager.ApplyStacksize();
                    }           
                    else
                        DestroyUI(pl, args[0]);
                    break;

                case "togglerefresh":
                    bool _;
                    Flags.TrySetFlag("RefreshLootOnExit", (!Flags.RefreshLootOnExit).ToString(), out _);
                    DestroyUI(pl, UiHelper.rootHeaderButtonPanelName);
                    DrawHeaderExtensions(null, pl);
                    break;

                // Open profile select ui
                case "open_select":
                    if (args.Length > 0)
                    {
                        DestroyUI(pl, args[0]);
                        
                        if (args[0] == UiHelper.editPanelName)
                        {
                            LootManager.SetCrateConfig(UiHelper.EditorCache.lootConfig);
                        }
                        if (args[0] == UiHelper.quarryEditPanelName)
                        {
                            LootManager.SetQuarryConfig(UiHelper.QuarryEditorCache.config);
                        }
                        if (args[0] == UiHelper.excavatorEditPanelName)
                        {
                            LootManager.SetExcavatorConfig(UiHelper.QuarryEditorCache.excavatorConfig);
                        }
                        if (args[0] == UiHelper.stacksizePanelName)
                        {
                            StackManager.Save();
                        }
                        if (args[0] == UiHelper.collectiblePanelName)
                        {
                            LootManager.SetCollectibleConfig(UiHelper.CollectibleEditorCache.config);
                        }
                        if (args[0] == UiHelper.gatherPanelName)
                        {
                            GatherManager.Save();
                        }
                        if (args[0] == UiHelper.configPanelName)
                        {
                            ConfigManager.Save();
                            ConfigManager.FurnaceConfig.InitializeFurnaces();
                        }
                    }
                    CreateSelectUI(pl);
                    break;
                
                // Page switching for select ui
                case "select_page":
                    if (args.Length < 1) break;
                    UiHelper.select_page = Int32.Parse(args[0]);
                    DestroyUI(pl, UiHelper.selectPanelName);
                    CreateSelectUI(pl);
                    break;

                case "select_slpage":
                    if (args.Length < 1) break;

                    int mod = Boolean.Parse(args[0]) ? 1 : -1;
                    UiHelper.slPage = Mathf.Clamp(UiHelper.slPage + mod, 0, LootManager.CachedTotalSlPages - 1);

                    DestroyUI(pl, UiHelper.selectPanelName);
                    CreateSelectUI(pl);
                    break;

                // Open profile ui
                case "open_edit":
                    if (args.Length < 1) break;
                    if (args.Length > 1) DestroyUI(pl, args[1]);

                    var lootable = LootManager.GetLootableBySaveName(args[0]);

                    if (lootable.type == LootManager.LootableType.Crate || lootable.type == LootManager.LootableType.NpcCorpse)
                    {
                        var crate = lootable;
                        LootableConfig lootConfig = LootManager.crateConfigCache[crate.id];

                        UiHelper.EditorCache.lootConfig = lootConfig;
                        UiHelper.EditorCache.Lootable = crate;
                        UiHelper.editLootType = lootConfig.lootType;
                        UiHelper.editingType = LootManager.LootableType.Crate;


                        UiHelper.EditorCache.page = 0;
                        UiHelper.EditorCache.pages = 0;
                        UiHelper.SetItemPages();

                        DestroyUI(pl, UiHelper.selectPanelName);
                        CreateEditUI(pl);
                    }
                    else if (lootable.type == LootManager.LootableType.Quarry)
                    {
                        var dispenser = (Quarry)lootable;
                        QuarryConfig config = LootManager.quarryConfigCache[dispenser.id];
                        
                        UiHelper.QuarryEditorCache.config = config;
                        UiHelper.QuarryEditorCache.quarry = dispenser;
                        UiHelper.editingType = LootManager.LootableType.Quarry;

                        DestroyUI(pl, UiHelper.selectPanelName);
                        CreateQuarryEditUI(pl);
                    }
                    else if (lootable.type == LootManager.LootableType.Excavator)
                    {
                        var excavator = (Quarry)lootable;
                        ExcavatorConfig config = LootManager.excavatorConfigCache;

                        UiHelper.QuarryEditorCache.excavatorConfig = config;
                        UiHelper.QuarryEditorCache.quarry = excavator;
                        UiHelper.editingType = LootManager.LootableType.Excavator;

                        DestroyUI(pl, UiHelper.selectPanelName);
                        CreateExcavatorEditUI(pl);
                    }
                    else if (lootable.type == LootManager.LootableType.Collectible)
                    {
                        var coll = (Collectible)lootable;
                        CollectibleConfig config = LootManager.collectibleConfigCache[coll.id];

                        UiHelper.CollectibleEditorCache.Collectible = coll;
                        UiHelper.CollectibleEditorCache.config = config;
                        UiHelper.editingType = LootManager.LootableType.Collectible;

                        DestroyUI(pl, UiHelper.selectPanelName);
                        CreateCollectibleEditUI(pl);
                    }

                    else if (lootable.Lootable == LootManager.Lootables.StackSizeControl)
                    {
                        List<int> currentCat = StackManager.itemCategoryList.Values.ToList()[UiHelper.Stacksize.stacksizeCat];
                        int pages = Mathf.CeilToInt(((float)currentCat.Count) / ((float)UiHelper.Stacksize.itemsPerStacksizePage));
                        UiHelper.Stacksize.stacksizePage = 0;
                        UiHelper.Stacksize.stacksizePages = pages;

                        DestroyUI(pl, UiHelper.selectPanelName);
                        CreateStacksizeUI(pl);
                    }
                    else if (lootable.Lootable == LootManager.Lootables.GatherConfig)
                    {
                        DestroyUI(pl, UiHelper.selectPanelName);
                        CreateGatherUI(pl);
                    }
                    else if (lootable.type == LootManager.LootableType.Generic)
                    {
                        DestroyUI(pl, UiHelper.selectPanelName);
                        CreateConfigUI(pl);
                    }
                    
                    break;

                #endregion

                #region Edit Panel

                case "action":
                    if (args.Length < 1) break;

                    string action = args[0];

                    switch (action)
                    {
                        case "sort":
                            if (args.Length < 2) break;
                            var filter = Int32.Parse(args[1]);
                            if (filter > UiHelper.itemFilterMax) filter = 0;
                            UiHelper.itemFilter = filter;
                            UiHelper.EditorCache.page = 0;
                            UiHelper.SetItemPages();
                            UiHelper.EditorCache.page = 0;

                            goto refresh;

                        case "page":
                            bool fwd = Boolean.Parse(args[1]);
                            int page = UiHelper.EditorCache.page;
                            int pages = UiHelper.EditorCache.pages;

                            if (fwd && page+1 < pages)
                                UiHelper.EditorCache.page++;                             
                            else if (fwd)
                                break;
                            else if (!fwd && page > 0)
                                UiHelper.EditorCache.page--;
                            else if (!fwd)
                                break;

                            DPrint(UiHelper.EditorCache.page.ToString());
                            DPrint(UiHelper.EditorCache.pages.ToString());
                            foreach (var x in UiHelper.EditorCache.ia_pages)
                            {
                                DPrint($"{x.Key} => {x.Value}");
                            }

                            goto refresh;

                        case "load_default":
                            if (!UiHelper.EditorCache.Lootable.HasVanillaConfig()) break;
                            LootableConfig defaultConfig = LootManager.defaultConfigCache[UiHelper.EditorCache.Lootable.id].Clone<LootableConfig>();
                            if (defaultConfig == null) break;
                            defaultConfig.SetCrate(UiHelper.EditorCache.Lootable.Lootable);
                            UiHelper.EditorCache.lootConfig = defaultConfig;
                            UiHelper.SetItemPages();
                            goto refresh;

                        case "set_enabled":
                            UiHelper.EditorCache.lootConfig.TryEnable(Boolean.Parse(args[1]));
                            goto refresh;

                        case "change_loot_type":
                            UiHelper.EditorCache.lootConfig.ChangeLootType();
                            goto refresh;

                        case "loot_type":
                            LootableConfig.LootType type = (LootableConfig.LootType)Int32.Parse(args[1]);
                            UiHelper.editLootType = type;
                            goto refresh;

                        case "item_add":
                            UiHelper.EditorCache.itemSelectType = UiHelper.EditorCache.ItemType.Base;
                            UiHelper.EditorCache.FlushItemCache();
                            DestroyUI(pl, UiHelper.editPanelName);
                            CreateItemEditUI(pl, true);
                            break;

                        case "item_edit":
                            string skinitemid = args[1];

                            LootItem itm = null;

                            if (UiHelper.editingType == LootManager.LootableType.StaticLootable)
                                itm = UiHelper.StaticLootables.Lootable.Items.Find(x => x.SkinItemId == skinitemid);
                            else if (UiHelper.editLootType == LootableConfig.LootType.Custom)
                                itm = UiHelper.EditorCache.lootConfig.items.Find(x => x.SkinItemId == skinitemid);
                            else if (UiHelper.editLootType == LootableConfig.LootType.Addition)
                                itm = UiHelper.EditorCache.lootConfig.additions.Find(x => x.SkinItemId == skinitemid);

                            if (itm == null)
                            {
                                CErr($"Item with id {skinitemid} not found");
                                break;
                            }

                            UiHelper.EditorCache.FlushItemCache();
                            UiHelper.EditorCache.CacheItem(itm);
                            UiHelper.EditorCache.old_item = itm;
                            UiHelper.EditorCache.replace_item = true;

                            DestroyUI(pl, UiHelper.editPanelName);
                            CreateItemEditUI(pl);
                            break;

                        case "extra_amount":
                            int itemid = Int32.Parse(args[1]);
                            string amount = args.Trim(2).Join();
                            string[] vals = amount.Trim(' ').Split('-');
                            if (vals.Count() > 0)
                            {
                                int am;
                                if (!Int32.TryParse(vals[0], out am)) goto end;
                                MinMax amt = new MinMax(am);
                                if (vals.Count() > 1)
                                {
                                    int amax;
                                    if (!Int32.TryParse(vals[1], out amax)) goto end;
                                    if (amax < amt.max)
                                        amt.min = amax;
                                    else
                                        amt.max = amax;
                                }
                                var ex = LootItem.FindExtra(UiHelper.EditorCache.extras, itemid);
                                if (ex != null) ex.amount = amt;
                            }
                            end:
                            DestroyUI(pl, UiHelper.editPanelName);
                            CreateItemEditUI(pl);
                            break;

                        case "extra_del":
                            int itmid = Int32.Parse(args[1]);
                            LootItem.RemoveExtra(ref UiHelper.EditorCache.extras, itmid);
                            DestroyUI(pl, UiHelper.editPanelName);
                            CreateItemEditUI(pl);
                            break;


                #endregion

                #region Item Edit

                        case "item_save":
                            bool replace = UiHelper.EditorCache.replace_item;

                            if (UiHelper.editingType == LootManager.LootableType.Crate)
                            {
                                var lootConfig2 = UiHelper.EditorCache.lootConfig;
                                if (UiHelper.editLootType != LootableConfig.LootType.BlackList)
                                {
                                    var new_item = UiHelper.EditorCache.CreateItemFromCache();
                                    if (replace)
                                        lootConfig2.AddItem(UiHelper.editLootType, new_item, true, UiHelper.EditorCache.old_item);
                                    else
                                        lootConfig2.AddItem(UiHelper.editLootType, new_item);

                                    if (UiHelper.editLootType == LootableConfig.LootType.Custom)
                                        UiHelper.SetItemPages();                   
                                }
                                else
                                {
                                    var new_item = new BlacklistItem(UiHelper.EditorCache.Itemid);
                                    lootConfig2.AddBlacklistItem(new_item);
                                }
                            }
                            else if (UiHelper.editingType == LootManager.LootableType.Quarry)
                            {
                                var quarryConfig = UiHelper.QuarryEditorCache.config;
                                var new_item = UiHelper.EditorCache.CreateItemFromCache(quarryConfig.liquid);

                                if (replace)
                                    quarryConfig.AddItem(new_item, true, UiHelper.EditorCache.old_item);
                                else
                                    quarryConfig.AddItem(new_item);
                            }
                            else if (UiHelper.editingType == LootManager.LootableType.Excavator)
                            {
                                var excavatorConfig = UiHelper.QuarryEditorCache.excavatorConfig;
                                var new_item = UiHelper.EditorCache.CreateItemFromCache();
                                int collectionid = UiHelper.QuarryEditorCache.collectionId;

                                if (replace)
                                    excavatorConfig.AddItem(collectionid, new_item, true, UiHelper.EditorCache.old_item);
                                else
                                    excavatorConfig.AddItem(collectionid, new_item);
                            }
                            else if (UiHelper.editingType == LootManager.LootableType.Collectible)
                            {
                                var config = UiHelper.CollectibleEditorCache.config;
                                var new_item = UiHelper.EditorCache.CreateItemFromCache();

                                if (replace)
                                    config.AddItem(new_item, true, UiHelper.EditorCache.old_item);
                                else
                                    config.AddItem(new_item);
                            }
                            else if (UiHelper.editingType == LootManager.LootableType.StaticLootable)
                            {
                                var cfg = UiHelper.StaticLootables.Lootable;
                                var new_item = UiHelper.EditorCache.CreateItemFromCache();
                                new_item.category = 0;

                                if (replace)
                                    cfg.AddItem(new_item, true, UiHelper.EditorCache.old_item);
                                else
                                    cfg.AddItem(new_item);
                            }

                            UiHelper.EditorCache.FlushItemCache();

                            DestroyUI(pl, UiHelper.itemEditPanelName);

                            if (UiHelper.editingType == LootManager.LootableType.Quarry)
                                CreateQuarryEditUI(pl);
                            else if (UiHelper.editingType == LootManager.LootableType.Crate)
                                CreateEditUI(pl);
                            else if (UiHelper.editingType == LootManager.LootableType.Excavator)
                                CreateExcavatorEditUI(pl);
                            else if (UiHelper.editingType == LootManager.LootableType.Collectible)
                                CreateCollectibleEditUI(pl);
                            else if (UiHelper.editingType == LootManager.LootableType.StaticLootable)
                                CreateSlEditUI(pl);

                            break;

                        case "item_del":
                            if (UiHelper.editingType == LootManager.LootableType.Crate)
                            {
                                var lootConfig3 = UiHelper.EditorCache.lootConfig;

                                if (UiHelper.editLootType != LootableConfig.LootType.BlackList)
                                    lootConfig3.RemoveItem(UiHelper.editLootType, UiHelper.EditorCache.old_item.SkinItemId);
                                else
                                    lootConfig3.RemoveBlacklistItem(Int32.Parse(args[1]));

                                lootConfig3.Validate();
                                UiHelper.SetItemPages();
                                if (UiHelper.EditorCache.page >= UiHelper.EditorCache.pages) UiHelper.EditorCache.page = UiHelper.EditorCache.pages - 1;
                            }
                            else if (UiHelper.editingType == LootManager.LootableType.Quarry)
                            {
                                var dispConfig = UiHelper.QuarryEditorCache.config;
                                dispConfig.RemoveItem(UiHelper.EditorCache.old_item.SkinItemId);
                                dispConfig.Validate();
                            }
                            else if (UiHelper.editingType == LootManager.LootableType.Excavator)
                            {
                                var cfg = UiHelper.QuarryEditorCache.excavatorConfig;
                                cfg.RemoveItem(UiHelper.QuarryEditorCache.collectionId, UiHelper.EditorCache.old_item.itemid);
                                cfg.Validate();
                            }
                            else if(UiHelper.editingType == LootManager.LootableType.Collectible)
                            {
                                var cfg = UiHelper.CollectibleEditorCache.config;
                                cfg.RemoveItem(UiHelper.EditorCache.old_item.SkinItemId);
                                cfg.Validate();
                            }
                            else if (UiHelper.editingType == LootManager.LootableType.StaticLootable)
                            {
                                UiHelper.StaticLootables.Lootable.RemoveItem(UiHelper.EditorCache.old_item.SkinItemId);
                            }

                            UiHelper.EditorCache.FlushItemCache();

                            DestroyUI(pl, UiHelper.itemEditPanelName);
                            if (UiHelper.editingType == LootManager.LootableType.Crate)
                                CreateEditUI(pl);
                            else if (UiHelper.editingType == LootManager.LootableType.Quarry)
                                CreateQuarryEditUI(pl);
                            else if (UiHelper.editingType == LootManager.LootableType.Excavator)
                                CreateExcavatorEditUI(pl);
                            else if (UiHelper.editingType == LootManager.LootableType.Collectible)
                                CreateCollectibleEditUI(pl);
                            else if (UiHelper.editingType == LootManager.LootableType.StaticLootable)
                                CreateSlEditUI(pl);

                            break;

                        refresh:
                            DestroyUI(pl, UiHelper.editPanelName);
                            CreateEditUI(pl);
                            break;

                        default:
                            break;
                    }
                    break;

                case "item_cache":
                    if (args.Length < 2)
                    {
                        DestroyUI(pl, UiHelper.itemEditPanelName);
                        CreateItemEditUI(pl);
                        break;
                    }
                    string key = args[0];
                    string val = args[1];

                    switch (key)
                    {
                        case "item_sname":
                            if (UiHelper.editingType == LootManager.LootableType.Crate)
                            {
                                if (UiHelper.EditorCache.lootConfig.items.FindAll(x => x.Shortname == val).Count > 0)
                                    break;
                            }
                            if (UiHelper.editingType == LootManager.LootableType.Quarry)
                            {
                                if (UiHelper.QuarryEditorCache.config.ContainsItem(val))
                                    break;
                            }
                            UiHelper.EditorCache.Shortname = val;
                            break;

                        case "item_min":
                            int min;
                            if (!Int32.TryParse(val, out min)) break;
                            min = Mathf.Clamp(min, 1, Flags.DisableItemLimit ? Int32.MaxValue : Flags.ItemLimit);
                            if (min > UiHelper.EditorCache.item_amount_max) UiHelper.EditorCache.item_amount_max = min;
                            UiHelper.EditorCache.item_amount_min = min;
                            break;

                        case "item_max":
                            int max;
                            if (!Int32.TryParse(val, out max)) break;
                            max = Mathf.Clamp(max, 1, Flags.DisableItemLimit ? Int32.MaxValue : Flags.ItemLimit);
                            if (max < UiHelper.EditorCache.item_amount_min) UiHelper.EditorCache.item_amount_min = max;
                            UiHelper.EditorCache.item_amount_max = max;
                            break;

                        case "item_minmax":
                            int minmax;
                            if (!Int32.TryParse(val, out minmax)) break;
                            minmax = Mathf.Clamp(minmax, 1, Flags.DisableItemLimit ? Int32.MaxValue : Flags.ItemLimit);
                            UiHelper.EditorCache.item_amount_max = minmax;
                            UiHelper.EditorCache.item_amount_min = minmax;
                            break;

                        case "item_skin":
                            ulong s;
                            if (!UInt64.TryParse(val, out s)) break;
                            UiHelper.EditorCache.item_skin = s;
                            break;

                        case "item_proba":
                            float p;
                            val = val.Replace(',', '.');
                            if (!Single.TryParse(val, out p)) break; 
                            var c = Mathf.Clamp(p, 0.01f, 100) / 100f;
                            UiHelper.EditorCache.item_chance = c;
                            break;

                        case "item_cat":
                            int cat;
                            if (!Int32.TryParse(val, out cat)) break;
                            cat = Mathf.Clamp(cat, 0, UiHelper.categoryColors.Count-1);
                            if (cat != 0) UiHelper.EditorCache.item_chance = 0f;
                            UiHelper.EditorCache.item_category = cat;
                            break;

                        case "item_name":
                            string new_name;
                            if (val == "###reset###") new_name = "";
                            else new_name = String.Join(" ", args.Trim(1));
                            UiHelper.EditorCache.item_name = new_name;
                            break;

                        case "item_work":
                            float work;
                            if (!float.TryParse(val, out work)) break;
                            work = Mathf.Clamp(work, 0.01f, 60f * 60f);
                            UiHelper.EditorCache.work = work;
                            break;

                        case "cond_min":
                            float condMin;
                            if (!Single.TryParse(val, out condMin)) break;
                            condMin /= 100f;
                            condMin = Mathf.Clamp(condMin, 0, 1);
                            if (condMin > UiHelper.EditorCache.condition.max)
                            {
                                UiHelper.EditorCache.condition.max = condMin;
                            }
                            UiHelper.EditorCache.condition.min = condMin;
                            break;

                        case "cond_max":
                            float condMax;
                            if (!Single.TryParse(val, out condMax)) break;
                            condMax /= 100f;
                            condMax = Mathf.Clamp(condMax, 0, 1);
                            if (condMax < UiHelper.EditorCache.condition.min)
                            {
                                UiHelper.EditorCache.condition.min = condMax;
                            }
                            UiHelper.EditorCache.condition.max = condMax;
                            break;

                        case "bptoggle":
                            UiHelper.EditorCache.isBlueprint = !UiHelper.EditorCache.isBlueprint;
                            break;

                        default:
                            break;
                    }

                    DestroyUI(pl, UiHelper.itemEditPanelName);
                    CreateItemEditUI(pl);
                    break;
                #endregion

                #region Multiplier

                case "multiplier":

                    string mpa = args[0];

                    switch (mpa)
                    {
                        case "open":
                            UiHelper.Mutliplier.target = UiHelper.EditorCache.lootConfig;
                            CreateMultiplierOverlay(pl);
                            break;

                        case "close":
                            DestroyUI(pl, UiHelper.multiplierOverlayName);
                            break;

                        case "set":
                            float mpl;
                            if (args.Length < 2) break;
                            if (!Single.TryParse(args[1], out mpl)) break;
                            if (!Flags.UnlockItemMultiplier) mpl = Mathf.Clamp(mpl, Flags.ItemMultiplier.min, Flags.ItemMultiplier.max);
                            UiHelper.Mutliplier.multi = mpl;
                            DestroyUI(pl, UiHelper.multiplierOverlayName);
                            CreateMultiplierOverlay(pl);
                            break;

                        case "apply":
                            var target = UiHelper.Mutliplier.target;
                            target.Multiply(UiHelper.Mutliplier.multi);

                            DestroyUI(pl, UiHelper.multiplierOverlayName);
                            if (target is LootableConfig)
                            {
                                DestroyUI(pl, UiHelper.editPanelName);
                                CreateEditUI(pl);
                            }
                            else if (target is CollectibleConfig)
                            {
                                DestroyUI(pl, UiHelper.collectiblePanelName);
                                CreateCollectibleEditUI(pl);
                            }
                            UiHelper.Mutliplier.target = null;
                            break;
                    }

                    break;

                #endregion

                #region Edit Panel Categories

                case "category_set":
                    if (args.Length < 3)
                    {
                        DestroyUI(pl, UiHelper.editPanelName);
                        CreateEditUI(pl);
                        break;
                    }

                    string catid = args[0];
                    LootCategory lootCat = UiHelper.EditorCache.lootConfig.GetCategory(catid);
                    BaseLootable l = UiHelper.EditorCache.Lootable;

                    int maxSlots = 0;
                    if (l is Crate) maxSlots = ((Crate)l).MaxSlots;
                    if (l is NpcCorpse) maxSlots = NpcCorpse.defaultSlots;

                    string ckey = args[1];
                    string cval = args[2];

                    switch (ckey)
                    {
                        case "cat_chance":
                            int chance;
                            if (lootCat == null || !Int32.TryParse(cval, out chance))
                                break;
                            float c = Mathf.Clamp(chance, 0, 100)/100f;
                            lootCat.chance = c;
                            break;

                        case "cat_max":
                            int max;
                            if (lootCat == null || !Int32.TryParse(cval, out max)) break;
                            max = Mathf.Clamp(max, 0, maxSlots);
                            if (max < lootCat.itemAmount.min) lootCat.itemAmount.min = max;
                            lootCat.itemAmount.max = max;
                            break;

                        case "cat_min":
                            int min;
                            if (lootCat == null || !Int32.TryParse(cval, out min)) break;
                            min = Mathf.Clamp(min, 0, maxSlots);
                            if (min > lootCat.itemAmount.max) lootCat.itemAmount.max = min;
                            lootCat.itemAmount.min = min;
                            break;

                        default:
                            break;
                    }

                    DestroyUI(pl, UiHelper.editPanelName);
                    CreateEditUI(pl);
                    break;

                case "profile_set":
                    if (args.Length < 2)
                    {
                        DestroyUI(pl, UiHelper.editPanelName);
                        CreateEditUI(pl);
                        break;
                    }

                    LootableConfig lootConfig1 = UiHelper.EditorCache.lootConfig;
                    BaseLootable lootable1 = LootManager.FindLootable((int)UiHelper.EditorCache.lootConfig.crate);
                    int max_slots;
                    if (lootable1.type == LootManager.LootableType.Crate)
                    {   // Crate
                        max_slots = ((Crate)lootable1).MaxSlots;
                    }
                    else
                    {   // NPC
                        max_slots = NpcCorpse.defaultSlots;
                    }
                    string pkey = args[0];
                    string pval = args[1];

                    switch (pkey)
                    {
                        case "items_max":
                            int max;
                            if (!Int32.TryParse(pval, out max)) break;
                            max = Mathf.Clamp(max, 1, max_slots);
                            if (max < lootConfig1.item_amount.min) lootConfig1.item_amount.min = max;
                            lootConfig1.item_amount.max = max;
                            break;

                        case "items_min":
                            int min;
                            if (!Int32.TryParse(pval, out min)) break;
                            min = Mathf.Clamp(min, 1, max_slots);
                            if (min > lootConfig1.item_amount.max) lootConfig1.item_amount.max = min;
                            lootConfig1.item_amount.min = min;
                            break;

                        case "seed":
                            uint seed;
                            if (!UInt32.TryParse(pval, out seed)) break;
                            seed = (uint)Mathf.Clamp(seed, 1, UInt32.MaxValue);
                            lootConfig1.seed = seed;
                            break;

                        default:
                            break;
                    }

                    DestroyUI(pl, UiHelper.editPanelName);
                    CreateEditUI(pl);
                    break;
                #endregion

                #region Quarry Edit

                case "disp_action":
                    if (args.Length < 1) break;

                    string a = args[0];

                    switch (a)
                    {
                        case "set_enabled":
                            UiHelper.QuarryEditorCache.config.TryEnable(Boolean.Parse(args[1]));
                            DestroyUI(pl, UiHelper.quarryEditPanelName);
                            CreateQuarryEditUI(pl);
                            break;

                        case "item_add":
                            UiHelper.EditorCache.FlushItemCache();
                            DestroyUI(pl, UiHelper.quarryEditPanelName);
                            CreateItemEditUI(pl, true);
                            break;

                        case "item_edit":
                            string skinitemid = args[1];
                            int eitemid = Int32.Parse(skinitemid.Split(':').First());

                            UiHelper.EditorCache.FlushItemCache();
                            var itm = UiHelper.QuarryEditorCache.config.GetItem(eitemid);
                            UiHelper.EditorCache.CacheItem(itm);
                            UiHelper.EditorCache.old_item = itm;
                            UiHelper.EditorCache.replace_item = true;

                            DestroyUI(pl, UiHelper.quarryEditPanelName);
                            CreateItemEditUI(pl);
                            break;

                        default:
                            break;
                    }
                    break;

                case "excv_action":
                    if (args.Length < 1) break;

                    string ac = args[0];

                    switch (ac)
                    {
                        case "set_enabled":
                            UiHelper.QuarryEditorCache.excavatorConfig.TryEnable(Boolean.Parse(args[1]));
                            DestroyUI(pl, UiHelper.excavatorEditPanelName);
                            CreateExcavatorEditUI(pl);
                            break;


                        case "item_add":
                            UiHelper.EditorCache.FlushItemCache();
                            UiHelper.QuarryEditorCache.collectionId = Int32.Parse(args[1]);
                            DestroyUI(pl, UiHelper.excavatorEditPanelName);
                            CreateItemEditUI(pl, true);
                            break;

                        case "item_edit":
                            string skinitemid = args[2];
                            int eitemid = Int32.Parse(skinitemid.Split(':').First());
                            var collectionid = Int32.Parse(args[1]);

                            UiHelper.EditorCache.FlushItemCache();
                            var itm = UiHelper.QuarryEditorCache.excavatorConfig.GetItem(collectionid, eitemid);
                            UiHelper.EditorCache.CacheItem(itm);
                            UiHelper.EditorCache.old_item = itm;
                            UiHelper.EditorCache.replace_item = true;
                            UiHelper.QuarryEditorCache.collectionId = collectionid;

                            DestroyUI(pl, UiHelper.excavatorEditPanelName);
                            CreateItemEditUI(pl);
                            break;

                        default:
                            break;
                    }
                    break;

                #endregion

                #region Stacksize

                case "stacksize":
                    if (args.Length < 1) break;

                    string sa = args[0];

                    switch (sa)
                    {
                        case "set_enabled":
                            StackManager.stacksizeConfig.TryEnable(Boolean.Parse(args[1]));
                            goto refresh;

                        case "set_multi":
                            float m;
                            string multi = args[1].Replace(',', '.');
                            if (!Single.TryParse(multi, out m)) goto refresh;
                            m = Mathf.Clamp(m, 1f, 1000f);
                            StackManager.stacksizeConfig.globalMultiplier = m;
                            goto refresh;

                        case "set_cat_multi":
                            float mu;
                            string mult = args[1].Replace(',', '.');
                            if (!Single.TryParse(mult, out mu)) goto refresh;
                            mu = Mathf.Clamp(mu, 1f, 1000f);
                            // Add -1 to compensate custom category
                            StackManager.stacksizeConfig.categoryMultipliers[UiHelper.Stacksize.stacksizeCat-1] = mu;
                            goto refresh;

                        case "reset":
                            int ctid = Int32.Parse(args[1]);
                            StackManager.stacksizeConfig.ResetCategory(ctid);
                            goto refresh;

                        case "switch_cat":
                            int cat = Int32.Parse(args[1]);
                            UiHelper.Stacksize.stacksizeCat = cat;
                            List<int> currentCat = StackManager.itemCategoryList.Values.ToList()[UiHelper.Stacksize.stacksizeCat];
                            int pages = Mathf.CeilToInt(((float)currentCat.Count) / ((float)UiHelper.Stacksize.itemsPerStacksizePage));
                            UiHelper.Stacksize.stacksizePage = 0;
                            UiHelper.Stacksize.stacksizePages = pages;
                            goto refresh;

                        case "page":
                            bool fwd = Boolean.Parse(args[1]);
                            if (fwd && UiHelper.Stacksize.stacksizePage + 1 < UiHelper.Stacksize.stacksizePages)
                                UiHelper.Stacksize.stacksizePage++;
                            else if (fwd)
                                break;
                            else if (!fwd && UiHelper.Stacksize.stacksizePage - 1 >= 0)
                                UiHelper.Stacksize.stacksizePage--;
                            else if (!fwd)
                                break;

                            goto refresh;

                        case "change":
                            if (args.Length < 3) goto refresh;
                            int itemid = Int32.Parse(args[1]);
                            string[] x = args.Trim(2);
                            string amtstring = String.Join("", x) .Replace(",", "").Replace(".", "").Replace(" ", "");
                            int amt;
                            if (!Int32.TryParse(amtstring, out amt)) goto refresh;
                            amt = Mathf.Clamp(amt, 1, Flags.DisableItemLimit ? Int32.MaxValue : Flags.ItemLimit);
                            StackManager.stacksizeConfig.SetStacksize(itemid, amt);
                            goto refresh;

                        refresh:
                            DestroyUI(pl, UiHelper.stacksizePanelName);
                            CreateStacksizeUI(pl);
                            break;

                        default:
                            break;
                    }
                    break;

                #endregion

                #region Copy Paste

                case "copypaste":
                    if (args.Length < 1) break;

                    string cp = args[0];

                    switch (cp)
                    {
                        case "copy":
                            UiHelper.copyPaste = new CopyPaste(UiHelper.EditorCache.lootConfig);
                            DestroyUI(pl, UiHelper.editPanelName);
                            CreateEditUI(pl);
                            break;

                        case "paste":
                            if (UiHelper.copyPaste.IsEmpty) break;
                            UiHelper.EditorCache.lootConfig = UiHelper.copyPaste.Paste(UiHelper.currentLootable.Lootable);
                            DestroyUI(pl, UiHelper.editPanelName);
                            CreateEditUI(pl);
                            break;

                    }
                    break;

                #endregion

                #region Collectables

                case "coll_action":
                    if (args.Length < 1) break;

                    string ca = args[0];

                    switch (ca)
                    {
                        case "set_enabled":
                            UiHelper.CollectibleEditorCache.config.TryEnable(Boolean.Parse(args[1]));
                            DestroyUI(pl, UiHelper.collectiblePanelName);
                            CreateCollectibleEditUI(pl);
                            break;

                        case "item_add":
                            UiHelper.EditorCache.FlushItemCache();
                            DestroyUI(pl, UiHelper.collectiblePanelName);
                            CreateItemEditUI(pl, true);
                            break;

                        case "item_edit":
                            string skinitemid = args[1];
                            int eitemid = Int32.Parse(skinitemid.Split(':').First());

                            UiHelper.EditorCache.FlushItemCache();
                            var itm = UiHelper.CollectibleEditorCache.config.GetItem(eitemid);
                            UiHelper.EditorCache.CacheItem(itm);
                            UiHelper.EditorCache.old_item = itm;
                            UiHelper.EditorCache.replace_item = true;

                            DestroyUI(pl, UiHelper.collectiblePanelName);
                            CreateItemEditUI(pl);
                            break;

                        case "load_default":
                            UiHelper.CollectibleEditorCache.config.LoadDefaultConfig();

                            DestroyUI(pl, UiHelper.collectiblePanelName);
                            CreateCollectibleEditUI(pl);
                            break;

                        case "multiply":
                            UiHelper.Mutliplier.target = UiHelper.CollectibleEditorCache.config;
                            CreateMultiplierOverlay(pl);
                            break;

                        default:
                            break;
                    }
                    break;

                #endregion

                #region Item Select Panel

                case "itemselect":
                    if (args.Length < 1) break;

                    string ci = args[0];

                    switch (ci)
                    {
                        case "closed":
                            DestroyUI(pl, UiHelper.itemSelectPanelName);
                            break;

                        case "open_extra":
                            UiHelper.EditorCache.itemSelectType = UiHelper.EditorCache.ItemType.Extra;
                            goto open;

                        case "open":
                            UiHelper.EditorCache.itemSelectType = UiHelper.EditorCache.ItemType.Base;
                            open:
                            UiHelper.Items.page = 0;
                            CreateItemSelectOverlay(pl, updatePages: true);
                            break;

                        case "search":
                            if (args.Length < 2) break;
                            DestroyUI(pl, UiHelper.itemSelectPanelName);
                            CreateItemSelectOverlay(pl, args[1]);
                            break;

                        case "switch_cat":
                            int cat = Int32.Parse(args[1]);
                            UiHelper.Items.cat = cat;
                            UiHelper.Items.page = 0;
                            DestroyUI(pl, UiHelper.itemSelectPanelName);
                            CreateItemSelectOverlay(pl, updatePages: true);
                            break;

                        case "page":
                            bool fwd = Boolean.Parse(args[1]);
                            if (fwd && UiHelper.Items.page + 1 < UiHelper.Items.pages)
                                UiHelper.Items.page++;
                            else if (fwd)
                                break;
                            else if (!fwd && UiHelper.Items.page - 1 >= 0)
                                UiHelper.Items.page--;
                            else if (!fwd)
                                break;

                            DestroyUI(pl, UiHelper.itemSelectPanelName);
                            CreateItemSelectOverlay(pl);
                            break;

                        case "select":
                            if (UiHelper.EditorCache.itemSelectType == UiHelper.EditorCache.ItemType.Base)
                            {
                                UiHelper.EditorCache.Shortname = args[1];
                            }
                            else if (UiHelper.EditorCache.itemSelectType == UiHelper.EditorCache.ItemType.Extra)
                            {
                                int itemid = ItemManager.FindItemDefinition(args[1]).itemid;
                                if (LootItem.FindExtra(UiHelper.EditorCache.extras, itemid) == null)
                                {
                                    var ex = new ExtraItem(itemid);
                                    UiHelper.EditorCache.extras.Add(ex);
                                }  
                            }
                            DestroyUI(pl, UiHelper.itemSelectPanelName);
                            DestroyUI(pl, UiHelper.itemEditPanelName);
                            CreateItemEditUI(pl);
                            break;

                        case "selectcustom":
                            if (UiHelper.EditorCache.itemSelectType == UiHelper.EditorCache.ItemType.Extra) break;

                            UiHelper.EditorCache.Shortname = args[1];
                            UiHelper.EditorCache.item_skin = UInt64.Parse(args[2]);

                            if (args.Length > 3)
                                UiHelper.EditorCache.item_name = String.Join(" ", args.Trim(3));
                            else
                                UiHelper.EditorCache.item_name = String.Empty;

                            DestroyUI(pl, UiHelper.itemSelectPanelName);
                            DestroyUI(pl, UiHelper.itemEditPanelName);
                            CreateItemEditUI(pl);
                            break;

                        default:
                            break;
                    }
                    break;

                #endregion

                #region Gathering

                case "gather":
                    if (args.Length < 1) break;

                    string ga = args[0];

                    switch (ga)
                    {
                        case "page":
                            UiHelper.gatherPage = !UiHelper.gatherPage;
                            goto refresh;

                        case "set_enabled":
                            GatherManager.gatherConfig.TryEnable(Boolean.Parse(args[1]));
                            goto refresh;

                        case "set_multi":
                            if (args.Length < 2) goto refresh;
                            float m;
                            string multi = args[1].Replace(',', '.');
                            if (!Single.TryParse(multi, out m)) goto refresh;
                            if (!Flags.UnlockGatherMultiplier) m = Mathf.Clamp(m, Flags.GatherMultiplier.min, Flags.GatherMultiplier.max);
                            GatherManager.gatherConfig.globalMultiplier = m;
                            goto refresh;

                        case "reset":
                            GatherManager.gatherConfig.Reset();
                            goto refresh;

                        case "change":
                            if (args.Length < 4) goto refresh;
                            string group = args[1];
                            int itemid = Int32.Parse(args[2]);
                            string multi1 = args[3].Replace(',', '.');
                            float amt;
                            if (!Single.TryParse(multi1, out amt)) goto refresh;
                            if (!Flags.UnlockGatherMultiplier) amt = Mathf.Clamp(amt, Flags.GatherMultiplier.min, Flags.GatherMultiplier.max);
                            GatherManager.gatherConfig.SetMultiplier(group, itemid, amt);
                            goto refresh;

                        refresh:
                            DestroyUI(pl, UiHelper.gatherPanelName);
                            CreateGatherUI(pl);
                            break;

                    }
                    break;

                #endregion

                #region Config

                case "config":
                    if (args.Length < 1) break;

                    string cfga = args[0];

                    switch (cfga)
                    {
                        case "reset":
                            ConfigManager.Reset();
                            goto refresh;

                        case "rc_enable":
                            ConfigManager.FurnaceConfig.recyclerEnabled = !ConfigManager.FurnaceConfig.recyclerEnabled;
                            goto refresh;

                        case "fc_enable":
                            ConfigManager.FurnaceConfig.enabled = !ConfigManager.FurnaceConfig.enabled;
                            ConfigManager.FurnaceConfig.Validate();
                            if (!ConfigManager.FurnaceConfig.enabled)
                            {
                                ConfigManager.FurnaceConfig.ResetBurnable();
                            }
                            else
                            {
                                ConfigManager.FurnaceConfig.UpdateBurnable();
                            }
                            goto refresh;

                        case "fc_speed":
                            if (args.Length < 3) break;
                            string f = args[1];
                            float v;
                            if (!Single.TryParse(args[2].Replace(',', '.'), out v)) goto refresh;

                            if (!Flags.UnlockFurnaceMultiplier) v = Mathf.Clamp(v, Flags.FurnaceMultiplier.min, Flags.FurnaceMultiplier.max);

                            if (f == "sf")
                                ConfigManager.FurnaceConfig.sFsmeltingSpeedMultiplier = v;
                            else if (f == "lf")
                                ConfigManager.FurnaceConfig.lFsmeltingSpeedMultiplier = v;
                            else if (f == "rf")
                                ConfigManager.FurnaceConfig.rFsmeltingSpeedMultiplier = v;
                            else if (f == "bbq")
                                ConfigManager.FurnaceConfig.bbqSmeltingSpeedMultiplier = v;
                            else if (f == "cf")
                                ConfigManager.FurnaceConfig.campfireSmeltingSpeedMultiplier = v;
                            else if (f == "rec")
                                ConfigManager.FurnaceConfig.recyclingSpeedMultiplier = v;
                            else if (f == "mx")
                                ConfigManager.FurnaceConfig.mixingSpeedMultiplier = v;
                            else if (f == "ef")
                                ConfigManager.FurnaceConfig.electricFurnaceSpeed = v;

                            goto refresh;

                        case "char_chance":
                            if (args.Length < 2) break;
                            float c;
                            if (!Single.TryParse(args[1], out c)) goto refresh;
                            c = Mathf.Clamp(c, 0f, 100f);

                            ConfigManager.FurnaceConfig.charcoalChance = c / 100f;
                            ConfigManager.FurnaceConfig.UpdateBurnable();

                            goto refresh;

                        case "char_amt":
                            if (args.Length < 2) break;
                            int amt;
                            if (!Int32.TryParse(args[1], out amt)) goto refresh;
                            if (!Flags.DisableItemLimit) amt = Mathf.Clamp(amt, 1, Flags.ItemLimit);

                            ConfigManager.FurnaceConfig.charcoalAmount = amt;
                            ConfigManager.FurnaceConfig.UpdateBurnable();

                            goto refresh;

                        case "aw_fuel":
                            if (args.Length < 3) break;
                            string t = args[1];
                            int fuel;

                            if (!Int32.TryParse(args[2], out fuel)) goto refresh;

                            if (!Flags.DisableItemLimit) fuel = Mathf.Clamp(fuel, -1, Flags.ItemLimit);

                            if (t == "mini")
                                ConfigManager.AirwolfConfig.minicopterFuel = fuel;
                            else if (t == "tcop")
                                ConfigManager.AirwolfConfig.scrapHeliFuel = fuel;
                            else if (t == "atck")
                                ConfigManager.AirwolfConfig.attackHeliFuel = fuel;

                            goto refresh;

                        case "ad_enable":
                            ConfigManager.AirdropConfig.enabled = !ConfigManager.AirdropConfig.enabled;
                            goto refresh;

                        case "ad_plane_speed":
                            if (args.Length < 2) break;
                            float s;
                            if (!Single.TryParse(args[1], out s)) goto refresh;
                            s = Mathf.Clamp(s, 0.1f, 100f);
                            ConfigManager.AirdropConfig.planeSpeedMultiplier = s;
                            goto refresh;

                        case "ad_height":
                            if (args.Length < 2) break;
                            int h;
                            if (!Int32.TryParse(args[1], out h)) goto refresh;
                            h = Mathf.Clamp(h, -400, 1000);
                            ConfigManager.AirdropConfig.planeHeight = h;
                            goto refresh;

                        case "ad_smoke_d":
                            if (args.Length < 2) break;
                            int d;
                            if (!Int32.TryParse(args[1], out d)) goto refresh;
                            d = Mathf.Clamp(d, 0, 300);
                            ConfigManager.AirdropConfig.smokeDuration = d;
                            goto refresh;

                        case "ad_speed":
                            if (args.Length < 2) break;
                            float sp;
                            if (!Single.TryParse(args[1], out sp)) goto refresh;
                            sp = Mathf.Clamp(sp, 0.6f, 10f);
                            ConfigManager.AirdropConfig.dropFallSpeed = sp;
                            goto refresh;

                        case "ad_smoke":
                            ConfigManager.AirdropConfig.dropFallSmoke = !ConfigManager.AirdropConfig.dropFallSmoke;
                            goto refresh;

                        case "ad_exact_drop":
                            ConfigManager.AirdropConfig.exactDropPosition = !ConfigManager.AirdropConfig.exactDropPosition;
                            goto refresh;

                        case "ad_pos_tolerance":
                            if (args.Length < 2) break;
                            int to;
                            if (!Int32.TryParse(args[1], out to)) goto refresh;
                            to = Mathf.Clamp(to, 0, 200);
                            ConfigManager.AirdropConfig.randomPosTolerance = to;
                            goto refresh;


                        refresh:
                            DestroyUI(pl, UiHelper.configPanelName);
                            CreateConfigUI(pl);
                            break;

                        default:
                            break;
                    }

                    break;

                #endregion

                #region Static Lootables

                case "sledit":
                    string cmd = args[0];
                    var staticLootable = UiHelper.StaticLootables.Lootable;
                    switch (cmd)
                    {
                        case "open":
                            string uid = args[1];
                            string oldPanel = args[2];

                            UiHelper.editingType = LootManager.LootableType.StaticLootable;
                            UiHelper.editLootType = LootableConfig.LootType.Custom;

                            UiHelper.EditorCache.page = 0;
                            UiHelper.EditorCache.pages = 0;

                            var sl = LootManager.GetStaticLootable(uid);
                            sl.LoadItems();

                            UiHelper.StaticLootables.SetLootable(sl);

                            DestroyUI(pl, oldPanel);
                            CreateSlEditUI(pl);
                            break;

                        case "save":
                            var sl2 = UiHelper.StaticLootables.FlushCache();
                            sl2.SaveItems();
                            LootManager.SetStaticLootable(sl2);

                            DestroyUI(pl, UiHelper.staticLootablePanelName);
                            CreateSelectUI(pl);
                            break;

                        case "container_size":
                            if (args.Length < 2) break;
                            int size;
                            if (!Int32.TryParse(args[1], out size)) break;

                            size = Mathf.Clamp(size, 1, Int32.MaxValue);
                            staticLootable.ContainerSize = size;
                            goto refresh;

                        case "refill_rate":
                            if (args.Length < 2) break;
                            float rr;
                            if (!Single.TryParse(args[1], out rr)) break;

                            staticLootable.Rule.RefillRate = rr;
                            goto refresh;

                        case "lock":
                            if (staticLootable.Lock == null)
                                staticLootable.Lock = new StaticLootableModels.RootLock();
                            else
                                staticLootable.Lock = null;

                            goto refresh;

                        case "lock_hp":
                            if (staticLootable.Lock == null || args.Length < 2) goto refresh;
                            int h;
                            if (!Int32.TryParse(args[1], out h)) goto refresh;

                            staticLootable.Lock.Health = h;

                            goto refresh;

                        case "hack":
                            if (staticLootable.Hack == null)
                                staticLootable.Hack = new StaticLootableModels.RootHack();
                            else
                                staticLootable.Hack = null;

                            goto refresh;

                        case "hack_time":
                            if (staticLootable.Hack == null || args.Length < 2) goto refresh;
                            int t;
                            if (!Int32.TryParse(args[1], out t)) goto refresh;

                            staticLootable.Hack.WaitTime = t;

                            goto refresh;

                        case "hack_reset":
                            if (staticLootable.Hack == null || args.Length < 2) goto refresh;
                            int r;
                            if (!Int32.TryParse(args[1], out r)) goto refresh;

                            staticLootable.Hack.CodeResetRate = r;

                            goto refresh;

                        case "timer":
                            if (args.Length < 2) goto refresh;
                            float tm;
                            if (!Single.TryParse(args[1], out tm)) goto refresh;

                            staticLootable.Timer = tm;

                            goto refresh;

                        refresh:
                            DestroyUI(pl, UiHelper.staticLootablePanelName);
                            CreateSlEditUI(pl);
                            break;

                        default:
                            break; 
                    }
                    break;

                #endregion

                #region Custom Items

                case "custom":
                    string cmd2 = args[0];
                    switch (cmd2)
                    {
                        case "edit":
                            string guid = args[1];
                            CustomItemStorage.StartEditItem(guid);
                            DestroyUI(pl, UiHelper.itemSelectPanelName);
                            CreateCustomItemOverlay(pl);
                            break;

                        case "from_item":
                            var lootItem = UiHelper.EditorCache.CreateItemFromCache();
                            CustomItemStorage.CreateEditItem(lootItem);
                            CreateCustomItemOverlay(pl);
                            break;

                        case "open":
                            CustomItemStorage.CreateEditItem();
                            CreateCustomItemOverlay(pl);
                            break;

                        case "delete":
                            CustomItemStorage.DeleteEditItem();
                            goto close;

                        case "save":
                            CustomItemStorage.UpdateEditItem();
                            goto close;

                        case "close":
                            close:
                            CustomItemStorage.FinishEditItem();
                            DestroyUI(pl, UiHelper.customItemPanelName);
                            break;

                        case "itemid":
                            int itemid;
                            if (!Int32.TryParse(args[1], out itemid)) goto refresh;
                            CustomItemStorage.editingItem.itemId = itemid;
                            goto refresh;

                        case "skin":
                            ulong skin;
                            if (!UInt64.TryParse(args[1], out skin)) goto refresh;
                            CustomItemStorage.editingItem.skinId = skin;
                            goto refresh;

                        case "name":
                            string name = String.Join(" ", args.Trim(1));
                            if (name == "###reset###") name = null;
                            CustomItemStorage.editingItem.customName = name;
                            goto refresh;

                            refresh:
                            DestroyUI(pl, UiHelper.customItemPanelName);
                            CreateCustomItemOverlay(pl);
                            break;
                    }
                    break;

                #endregion

                default:
                    #if DEBUG
                    DPrint($"Command {sb} does not exist");
                    #endif
                    break;
                    
            }
            
        }

        #endregion

        #region GUI

        private void CreateBaseUI(BasePlayer player)
        {
            DestroyUI(player);
            UiHelper.uiUser = player.userID;

            CuiElementContainer result = new CuiElementContainer();
            string rootPanelName = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = "0.01 0.01 0.02 1"
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                },
                CursorEnabled = true
            }, "Overlay", UiHelper.rootPanelName);

            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                },
                Text =
                {
                    Text = "If you can read this, the editor probably crashed :(\nTry closing and opening it again",
                    Align = TextAnchor.MiddleCenter,
                    Color = UiHelper.textColor,
                    FontSize = 22
                }
            }, rootPanelName);

            #region Header

            string headerPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColorBright
                },
                RectTransform =
                {
                    AnchorMin = "0 0.95",
                    AnchorMax = "1 1"
                }
            }, rootPanelName, UiHelper.rootHeaderPanelName);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = "0.01 0",
                    AnchorMax = "0.4 1"
                },
                Text =
                {
                    Text = $"Loot Table UI v{Version}",
                    Align = TextAnchor.MiddleLeft,
                    Color = UiHelper.textColor,
                    FontSize = 22
                }
            }, headerPanel);

            DrawHeaderExtensions(result, null);

            // Create custom item button
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.4f, 0.1f),
                    AnchorMax = AspectAdjust(0.5f, 0.81f)
                },
                Button =
                {
                    Command = $"loottable.cmd custom open",
                    Color = UiHelper.greenButtonColor
                },
                Text =
                {
                    Text = "New Custom Item",
                    Align = TextAnchor.MiddleCenter,
                    Color = UiHelper.buttonTextColor,
                    FontSize = 16
                },
            }, headerPanel);

            // Close
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.97f, 0.1f),
                    AnchorMax = AspectAdjust(0.993f, 0.81f)
                },
                Text =
                {
                    Text = "X",
                    Align = TextAnchor.MiddleCenter,
                    Color = UiHelper.buttonTextColor,
                    FontSize = 20
                },
                Button =
                {
                    Command = "loottable.cmd close",
                    Color = UiHelper.redButtonColor
                }
            }, headerPanel);

            #endregion

            CuiHelper.AddUi(player, result);
            return;
        }

        private void CreateSelectUI(BasePlayer player)
        {
            if (UiHelper.uiUser != player.userID) return;
            CuiElementContainer result = new CuiElementContainer();

            //Content panel
            string contentPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.bgColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.01f, 0.01f, "x"),
                    AnchorMax = AspectAdjust(0.99f, 0.935f)
                }
            }, UiHelper.rootPanelName, UiHelper.selectPanelName);

            #region Head & Navigation

            string headPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColorBright
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0, 0.96f),
                    AnchorMax = AspectAdjust(1, 1)
                }
            }, UiHelper.selectPanelName);

            string page_key = "";
            int p = 0;
            foreach (var page in UiHelper.selectPages.Keys)
            {
                if (page == UiHelper.sl_page_key && !UseStaticLootables) continue;
                if (p == UiHelper.select_page) page_key = page;
                result.Add(new CuiButton
                    {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0f+p*0.14f, 0f),
                        AnchorMax = AspectAdjust(0.14f+p*0.14f, 0.97f)
                    },
                    Button =
                    {
                        Command =  $"loottable.cmd select_page {p}",
                        Color = p == UiHelper.select_page ? Hex2RGBA("#151515", 1) : UiHelper.panelColorBright,
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Text = page,
                        Color = UiHelper.buttonTextColor,
                        FontSize = 14
                    }
                }, headPanel);
                p++;
            }
            #endregion

            int i = 0; int i_max = 8;
            int col = 0; int col_max = 2;
            var current_page = UiHelper.selectPages[page_key];

            #region Static Lootables

            if (page_key == UiHelper.sl_page_key && UseStaticLootables)
            {
                foreach (var lootable in LootManager.GetStaticLootables(UiHelper.slPage))
                {
                    //DPrint(lootable.Uid);
                    string crate_panel = result.Add(new CuiPanel
                    {
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust((0.007f + col*0.333f), (0.85f - i*0.1f)),
                            AnchorMax = AspectAdjust((0.33f + col*0.333f), (0.94f - i*0.1f))
                        },
                        Image = new CuiImageComponent
                        {
                            Color = UiHelper.panelColor
                        }
                    }, contentPanel);

                    result.Add(new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust(0.8f, 0.1f),
                            AnchorMax = AspectAdjust(0.98f, 0.9f)
                        },
                        Button =
                        {
                            Command =  $"loottable.cmd sledit open {lootable.Uid} {UiHelper.selectPanelName}",
                            Color = UiHelper.greenButtonColor,
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            Text = "EDIT",
                            Color = UiHelper.buttonTextColor,
                            FontSize = 15
                        }
                    }, crate_panel);

                    result.Add(new CuiLabel
                    {
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust(0.16f, 0.1f),
                            AnchorMax = AspectAdjust(0.58f, 0.85f)
                        },
                        Text =
                        {
                            Align = TextAnchor.UpperLeft,
                            Text = lootable.DisplayName,
                            Color = UiHelper.textColor,
                            FontSize = 14
                        }
                    }, crate_panel);

                    if (lootable.HasWorldPos)
                    result.Add(new CuiLabel
                    {
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust(0.16f, 0.1f),
                            AnchorMax = AspectAdjust(0.58f, 0.4f)
                        },
                        Text =
                        {
                            Align = TextAnchor.LowerLeft,
                            Text = $"at {lootable.WorldPos}",
                            Color = UiHelper.textColor,
                            Font = UiHelper.regularFont,
                            FontSize = 12
                        }
                    }, crate_panel);

                    var image_size = 45;
                    if (lootable.HasImage)
                    result.Add(new CuiElement
                    {
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = ImageLibrary?.Call<string>("GetImage", lootable.Image),
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.02 0.1", AnchorMax = "0.02 0.1",
                                OffsetMin = "0 0", OffsetMax = $"{image_size} {image_size}"
                            }
                        },
                        Parent = crate_panel
                    });

                    i++;
                    if (i > i_max)
                    {
                        i = 0;
                        col++;
                        if (col > col_max) break;
                    }
                }

                #region Page navigation

                result.Add(new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.006f, 0.015f),
                        AnchorMax = AspectAdjust(0.06f, 0.05f)
                    },
                    Text =
                    {
                        Align = TextAnchor.LowerLeft,
                        Text = $"Page {UiHelper.slPage+1} of {LootManager.CachedTotalSlPages}",
                        Color = UiHelper.textColor,
                        Font = UiHelper.regularFont,
                        FontSize = 13
                    }
                }, contentPanel);

                result.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.065f, 0.01f),
                        AnchorMax = AspectAdjust(0.1f, 0.04f)
                    },
                    Button =
                    {
                        Command = $"loottable.cmd select_slpage false",
                        Color = UiHelper.slPage-1 >= 0 ? UiHelper.colorBlue : UiHelper.greyButtonColor
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Text = "<-",
                        Color = UiHelper.buttonTextColor,
                        FontSize = 15
                    }
                }, contentPanel);

                result.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.105f, 0.01f),
                        AnchorMax = AspectAdjust(0.140f, 0.04f)
                    },
                    Button =
                    {
                        Command = $"loottable.cmd select_slpage true",
                        Color = UiHelper.slPage+1 < LootManager.CachedTotalSlPages ? UiHelper.colorBlue : UiHelper.greyButtonColor
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Text = "->",
                        Color = UiHelper.buttonTextColor,
                        FontSize = 15
                    }
                }, contentPanel);

                #endregion
            }

            #endregion

            #region Default Crates

            else
            foreach (var sub_cat in current_page)
            {
                if (sub_cat.Key != "")
                {
                    if (i == i_max) { i = 0; col++; }

                    result.Add(new CuiLabel
                    {
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust((0.007f + col*0.333f), (0.85f - i*0.1f)),
                            AnchorMax = AspectAdjust((0.33f + col*0.333f), (0.94f - i*0.1f))
                        },
                        Text =
                        {
                            Align = TextAnchor.LowerLeft,
                            Text = sub_cat.Key,
                            Color = UiHelper.textColor,
                            FontSize = 15
                        }
                    }, contentPanel);

                    i++;
                    if (i > i_max)
                    {
                        i = 0;
                        col++;
                        if (col > col_max) break;
                    }
                }

                foreach (var id in sub_cat.Value)
                {
                    BaseLootable lootable = LootManager.GetLootable(id);

                    string crate_panel = result.Add(new CuiPanel
                    {
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust((0.007f + col*0.333f), (0.85f - i*0.1f)),
                            AnchorMax = AspectAdjust((0.33f + col*0.333f), (0.94f - i*0.1f))
                        },
                        Image = new CuiImageComponent
                        {
                            Color = UiHelper.panelColor
                        }
                    }, contentPanel);

                    result.Add(new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust(0.8f, 0.1f),
                            AnchorMax = AspectAdjust(0.98f, 0.9f)
                        },
                        Button =
                        {
                            Command =  $"loottable.cmd open_edit {lootable.saveName} {UiHelper.selectPanelName}",
                            Color = UiHelper.greenButtonColor,
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            Text = "EDIT",
                            Color = UiHelper.buttonTextColor,
                            FontSize = 16
                        }
                    }, crate_panel);

                    result.Add(new CuiLabel
                    {
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust(0.2f, 0.1f),
                            AnchorMax = AspectAdjust(0.78f, 0.85f)
                        },
                        Text =
                        {
                            Align = TextAnchor.UpperLeft,
                            Text = lootable.displayName,
                            Color = UiHelper.textColor,
                            FontSize = 14
                        }
                    }, crate_panel);

                    if (!ConfigManager.labelIgnore.Contains(lootable.Lootable))
                    {
                        int config_state = LootManager.GetConfigState(lootable.id);
                        string tagPanel = result.Add(new CuiPanel
                            {
                            RectTransform =
                            {
                                AnchorMin = AspectAdjust(0.2f, 0.1f),
                                AnchorMax = AspectAdjust(0.45f, 0.4f)
                            },
                            Image = new CuiImageComponent
                            {
                                Color = UiHelper.lootConfigStateColors[config_state]
                            }
                        }, crate_panel);

                        result.Add(new CuiLabel
                        {
                            RectTransform =
                            {
                                AnchorMin = AspectAdjust(0, 0),
                                AnchorMax = AspectAdjust(1, 1)
                            },
                            Text =
                            {
                                Align = TextAnchor.MiddleCenter,
                                Text = UiHelper.lootConfigStates[config_state],
                                Color = UiHelper.textColor,
                                Font = UiHelper.regularFont,
                                FontSize = 13
                            }
                        }, tagPanel);
                    }

                    var image_size = 45;
                    result.Add(new CuiElement
                    {
                        Components =
                        {
                            CreateImage(lootable.saveName),
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.02 0.1", AnchorMax = "0.02 0.1",
                                OffsetMin = "0 0", OffsetMax = $"{image_size} {image_size}"
                            }
                        },
                        Parent = crate_panel
                    });

                    i++;
                    if (i > i_max)
                    {
                        i = 0;
                        col++;
                        if (col > col_max) break;
                    }
                }
            }

            #endregion

            CuiHelper.AddUi(player, result);
            return;
        }

        private void CreateEditUI(BasePlayer player)
        {
            if (UiHelper.uiUser != player.userID) return;
            CuiElementContainer result = new CuiElementContainer();

            BaseLootable crate = UiHelper.EditorCache.Lootable;
            LootableConfig lootConfig = UiHelper.EditorCache.lootConfig;

            //Content panel
            string profilePanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.bgColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.01f, 0.01f, "x"),
                    AnchorMax = AspectAdjust(0.99f, 0.935f)
                }
            }, UiHelper.rootPanelName, UiHelper.editPanelName);

            #region Header

            //Header panel
            string profileHeadPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColorBright
                },
                RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.005f, 0.94f),
                        AnchorMax = AspectAdjust(0.995f, 0.99f)
                    }
            }, profilePanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.01f, 0f),
                        AnchorMax = AspectAdjust(0.2f, 1f)
                    },
                Text =
                    {
                        Align = TextAnchor.MiddleLeft,
                        Text = crate.displayName,
                        Color = UiHelper.textColor,
                        FontSize = 16
                    }
            }, profileHeadPanel);

            // Save Button
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.85f, 0.2f),
                    AnchorMax = AspectAdjust(0.99f, 0.8f)
                },
                Button =
                {
                    Command = $"loottable.cmd open_select {UiHelper.editPanelName}",
                    Color = UiHelper.greenButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "SAVE",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, profileHeadPanel);
            
            // Copy Button
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.69f, 0.2f),
                    AnchorMax = AspectAdjust(0.83f, 0.8f)
                },
                Button =
                {
                    Command = $"loottable.cmd copypaste copy",
                    Color = UiHelper.greenButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "COPY CONFIG",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, profileHeadPanel);

            if (!UiHelper.copyPaste.IsEmpty)
            {
                // Paste Button
                result.Add(new CuiButton
                    {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.58f, 0.2f),
                        AnchorMax = AspectAdjust(0.67f, 0.8f)
                    },
                    Button =
                    {
                        Command = $"loottable.cmd copypaste paste",
                        Color = UiHelper.redButtonColor
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Text = "PASTE",
                        Color = UiHelper.buttonTextColor,
                        FontSize = 15
                    }
                }, profileHeadPanel);

                // Paste Label
                result.Add(new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.38f, 0f),
                        AnchorMax = AspectAdjust(0.57f, 1f)
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleRight,
                        Text = $"Paste config of {UiHelper.copyPaste.CopyName}\n(overwrite current config)",
                        Color = UiHelper.textColor,
                        FontSize = 13
                    }
                }, profileHeadPanel);
            }

            #endregion

            // Enable loot table
            CreateButtonPanel(ref result, profilePanel, "Custom loot configuration", $"loottable.cmd action set_enabled {!lootConfig.Enabled}",
                lootConfig.Enabled ? "ENABLED" : "DISABLED", lootConfig.Enabled ? UiHelper.greenButtonColor : UiHelper.redButtonColor,
                AspectAdjust(0.005f, 0.85f), AspectAdjust(0.150f, 0.93f));

            // Loot type
            CreateButtonPanel(ref result, profilePanel, "Loot type", $"loottable.cmd action change_loot_type", 
                lootConfig.lootType.ToString().AddSpacesBeforeUppercase().ToUpper(), UiHelper.colorBlue,
                AspectAdjust(0.155f, 0.85f), AspectAdjust(0.300f, 0.93f));
           
            // Add item
            CreateButtonPanel(ref result, profilePanel, "Add item", "loottable.cmd action item_add", "ADD ITEM", UiHelper.greenButtonColor,
                AspectAdjust(0.305f, 0.85f), AspectAdjust(0.450f, 0.93f));

            #region Vanilla Config

            string tipPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.455f, 0.85f),
                    AnchorMax = AspectAdjust(0.600f, 0.93f)
                }
            }, profilePanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.6f),
                    AnchorMax = AspectAdjust(0.95f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "Load default loot table",
                    Color = UiHelper.textColor,
                    FontSize = 14
                }
            }, tipPanel);


            // Reset button
            if (crate.HasVanillaConfig())
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.1f),
                    AnchorMax = AspectAdjust(0.95f, 0.6f)
                },
                Button =
                {
                    Command = $"loottable.cmd action load_default",
                    Color = UiHelper.redButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "LOAD",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15,
                }
            }, tipPanel);
            
            // config not available label
            else
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.1f),
                    AnchorMax = AspectAdjust(0.95f, 0.6f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "No default loot table available",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 12,
                    Font = UiHelper.regularFont
                }
            }, tipPanel);

            #endregion

            #region Items

            // Base panel
            string itemListPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.005f, 0.01f),
                    AnchorMax = AspectAdjust(0.6f, 0.84f)
                }
            }, profilePanel);
            
            // Head panel
            string itemListHeadPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColorBright
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.005f, 0.81f),
                    AnchorMax = AspectAdjust(0.6f, 0.84f)
                }
            }, profilePanel);
            
            // Item Tab
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0f, 0f),
                    AnchorMax = AspectAdjust(0.2f, 1f)
                },
                Button =
                {
                    Command = "loottable.cmd action loot_type 0",
                    Color = UiHelper.editLootType == LootableConfig.LootType.Custom ? Hex2RGBA("#222222", 1) : UiHelper.transparentColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "Loot Table",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 14
                }
            }, itemListHeadPanel);

            // Blacklist Tab
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.2f, 0f),
                    AnchorMax = AspectAdjust(0.4f, 1f)
                },
                Button =
                {
                    Command = "loottable.cmd action loot_type 1",
                    Color = UiHelper.editLootType == LootableConfig.LootType.BlackList ? Hex2RGBA("#222222", 1) : UiHelper.transparentColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "Black List",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 14
                }
            }, itemListHeadPanel);

            // Additions Tab
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.4f, 0f),
                    AnchorMax = AspectAdjust(0.6f, 1f)
                },
                Button =
                {
                    Command = "loottable.cmd action loot_type 2",
                    Color = UiHelper.editLootType == LootableConfig.LootType.Addition ? Hex2RGBA("#222222", 1) : UiHelper.transparentColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "Additions",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 14
                }
            }, itemListHeadPanel);

            // Filter label
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.6f, 0f),
                    AnchorMax = AspectAdjust(0.785f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleRight,
                    Text = "Sort by",
                    Color = UiHelper.textColor,
                    FontSize = 14
                }
            }, itemListHeadPanel);

            // Filter Button
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.80f, 0.2f),
                    AnchorMax = AspectAdjust(0.99f, 0.8f)
                },
                Button =
                {
                    Command = $"loottable.cmd action sort {UiHelper.itemFilter+1}",
                    Color = UiHelper.greyButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = UiHelper.itemFilterNames[UiHelper.itemFilter],
                    Color = UiHelper.buttonTextColor,
                    FontSize = 12
                }
            }, itemListHeadPanel);

            if ((UiHelper.editLootType == LootableConfig.LootType.Custom && lootConfig.items.Count < 1) ||
                (UiHelper.editLootType == LootableConfig.LootType.BlackList && lootConfig.blacklist.Count < 1) ||
                (UiHelper.editLootType == LootableConfig.LootType.Addition && lootConfig.additions.Count < 1))
            {
                result.Add(new CuiLabel
                {
                        RectTransform =
                    {
                        AnchorMin = AspectAdjust(0f, 0.1f),
                        AnchorMax = AspectAdjust(1f, 1f)
                    },
                        Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Text = "No items yet, let's add some!",
                        Color = UiHelper.textColor,
                        FontSize = 16
                    }
                }, itemListPanel);

                result.Add(new CuiButton
                {
                        RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.35f, 0.45f),
                        AnchorMax = AspectAdjust(0.65f, 0.5f)
                    },
                        Button =
                    {
                        Command = $"loottable.cmd action item_add",
                        Color = UiHelper.greenButtonColor
                    },
                        Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Text = "ADD ITEM",
                        Color = UiHelper.buttonTextColor,
                        FontSize = 15
                    }
                }, itemListPanel);
            }
            else
            {
                int row = 0; int row_max = 9;
                int col = 0; int col_max = 6;

                if (UiHelper.editLootType == LootableConfig.LootType.BlackList)
                {
                    foreach (var item in lootConfig.blacklist.ToList())
                    {

                        CreateItemPanel(ref result, itemListPanel, item, AspectAdjust(0.01f + row * 0.099f, 0.82f - col * 0.13f),
                            AspectAdjust(0.10f + row * 0.099f, 0.94f - col * 0.13f));

                        row++;
                        if (row > row_max)
                        {
                            row = 0;
                            col++;
                            if (col > col_max) break;
                        }
                    }
                }
                else if (UiHelper.editLootType == LootableConfig.LootType.Addition)
                {
                    foreach (var item in lootConfig.additions.ToList())
                    {

                        CreateItemPanel(ref result, itemListPanel, item, AspectAdjust(0.01f + row * 0.099f, 0.82f - col * 0.13f),
                            AspectAdjust(0.10f + row * 0.099f, 0.94f - col * 0.13f), true);

                        row++;
                        if (row > row_max)
                        {
                            row = 0;
                            col++;
                            if (col > col_max) break;
                        }
                    }
                }
                else if (UiHelper.editLootType == LootableConfig.LootType.Custom)
                {
                    int old_cat = -1;
                    IEnumerable<LootItem> displayList;
                    bool orderByCat = (UiHelper.itemFilter == 1 || UiHelper.itemFilter == 0);
                    switch (UiHelper.itemFilter)
                    {
                        case 0:
                            displayList = lootConfig.items.OrderByDescending(x => x.chance);
                            displayList = displayList.OrderBy(x => x.category);
                            break;
                        case 1:
                            displayList = lootConfig.items.OrderByDescending(x => x.chance);
                            displayList = displayList.OrderByDescending(x => x.category);
                            break;
                        case 2:
                            displayList = lootConfig.items.OrderBy(x => x.chance);
                            break;
                        case 3:
                            displayList = lootConfig.items.OrderByDescending(x => x.chance);
                            break;

                        default:
                            displayList = lootConfig.items.OrderBy(x => x.category);
                            break;
                    }

                    var l = displayList.ToList();
                    int c = UiHelper.EditorCache.ia_pages_values.GetRange(0, UiHelper.EditorCache.page).Sum();
                    l.RemoveRange(0, c);
                    displayList = l;

                    int ia = 0;
                    foreach (var item in displayList)
                    {
                        if (old_cat == -1) old_cat = item.category;
                        if (item.category != old_cat && orderByCat) { if (row != 0) col++; row = 0; old_cat = item.category; }
                        if (col > col_max) break;

                        CreateItemPanel(ref result, itemListPanel, item, AspectAdjust(0.01f + row * 0.099f, 0.83f - col * 0.13f),
                            AspectAdjust(0.10f + row * 0.099f, 0.95f - col * 0.13f), true);
                        ia++;

                        row++;
                        if (row > row_max)
                        {
                            row = 0;
                            col++;
                            if (col > col_max) break;
                        }
                    }

                }           
            }

            #region Train Warning

            if (UiHelper.currentLootable.Lootable.IsOneOf(
                LootManager.Lootables.TrainWagonCharcoal,
                LootManager.Lootables.TrainWagonMetalOre,
                LootManager.Lootables.TrainWagonSulfurOre,
                LootManager.Lootables.TrainWagonFuel
                ))
            {
                string text = UiHelper.currentLootable.Lootable == LootManager.Lootables.TrainWagonFuel ?
                "For the fuel wagon config to work, it has to contain at least:\n<b>Low Grade Fuel x1</b>\nwith a 100% spawn chance." :

                "For the train loot config to work, it has to contain at least:\n<b>Metal Ore x1</b> or <b>Sulfur Ore x1</b> or <b>Charcoal x1</b>\ndepending on the type of the wagon." +
                "The spawn chance of this item has to be <b>100%</b>\n\n" +
                "Also the fill level of the wagon is determined by the amount of Metal Ore / Sulfur Ore / Charcoal. e.g. if a Charcoal Wagon only contains 200 charcoal it will look empty.";

                // Base panel
                string warningPanel = result.Add(new CuiPanel
                {
                    Image = new CuiImageComponent
                    {
                        Color = UiHelper.colorRed
                    },
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.01f, 0.06f),
                        AnchorMax = AspectAdjust(0.99f, 0.3f)
                    }
                }, itemListPanel);

                result.Add(new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.02f, 0.8f),
                        AnchorMax = AspectAdjust(0.8f, 1f)
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleLeft,
                        Text = "! IMPORTANT !",
                        Color = UiHelper.colorRed,
                        FontSize = 18
                    }
                }, warningPanel);

                result.Add(new CuiLabel
                {
                        RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.02f, 0.02f),
                        AnchorMax = AspectAdjust(0.8f, 0.8f)
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleLeft,
                        Text = text,
                        Color = UiHelper.greyTextColor,
                        FontSize = 12,
                        Font = UiHelper.regularFont
                    }
                }, warningPanel);
            }

            #endregion

            #endregion

            #region Page navigation

            if (UiHelper.editLootType == LootableConfig.LootType.Custom) {
                result.Add(new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.01f, 0.01f),
                        AnchorMax = AspectAdjust(0.09f, 0.06f)
                    },
                    Text =
                    {
                        Align = TextAnchor.LowerLeft,
                        Text = $"Page {UiHelper.EditorCache.page+1} of {UiHelper.EditorCache.pages}",
                        Color = UiHelper.textColor,
                        Font = UiHelper.regularFont,
                        FontSize = 13
                    }
                }, itemListPanel);

                result.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.1f, 0.01f),
                        AnchorMax = AspectAdjust(0.14f, 0.04f)
                    },
                    Button =
                    {
                        Command = $"loottable.cmd action page false",
                        Color = UiHelper.EditorCache.page-1 >= 0 ? UiHelper.colorBlue : UiHelper.greyButtonColor
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Text = "<-",
                        Color = UiHelper.buttonTextColor,
                        FontSize = 15
                    }
                }, itemListPanel);

                result.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.145f, 0.01f),
                        AnchorMax = AspectAdjust(0.185f, 0.04f)
                    },
                    Button =
                    {
                        Command = $"loottable.cmd action page true",
                        Color = UiHelper.EditorCache.page+1 < UiHelper.EditorCache.pages ? UiHelper.colorBlue : UiHelper.greyButtonColor
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Text = "->",
                        Color = UiHelper.buttonTextColor,
                        FontSize = 15
                    }
                }, itemListPanel);
            }

            #endregion

            #region Categories

            string categoryOverviewPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.605f, 0.01f),
                    AnchorMax = AspectAdjust(0.995f, 0.93f)
                }
            }, profilePanel);

            if (UiHelper.editLootType == LootableConfig.LootType.BlackList)
            {
                result.Add(new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0f, 0.92f),
                        AnchorMax = AspectAdjust(1f, 1f)
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Text = "Black List",
                        Color = UiHelper.textColor,
                        FontSize = 16
                    }
                }, categoryOverviewPanel);

                result.Add(new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.02f, 0f),
                        AnchorMax = AspectAdjust(0.98f, 0.92f)
                    },
                    Text =
                    {
                        Align = TextAnchor.UpperLeft,
                        Text = "Crates and NPCs using a black list contain vanilla loot. If they contain items on the black list, these items will be removed when the loot spawns.\n" +
                                "To enable the black list, enable the field 'Custom loot configuration' and set 'Loot type' to 'BLACK LIST'." +
                                " Note that the black list can only be enabled as long as it contains at least one item.",
                        Color = UiHelper.textColor,
                        Font = UiHelper.regularFont,
                        FontSize = 14
                    }
                }, categoryOverviewPanel);
            }
            else if (UiHelper.editLootType == LootableConfig.LootType.Addition)
            {
                result.Add(new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0f, 0.92f),
                        AnchorMax = AspectAdjust(1f, 1f)
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Text = "Additional Items",
                        Color = UiHelper.textColor,
                        FontSize = 16
                    }
                }, categoryOverviewPanel);

                result.Add(new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.02f, 0f),
                        AnchorMax = AspectAdjust(0.98f, 0.92f)
                    },
                    Text =
                    {
                        Align = TextAnchor.UpperLeft,
                        Text = "All items specified in the 'Additions' section will be added to the vanilla loot of this crate / NPC providing they are randomly selected (depending on their chance).\n" +
                                " To enable additional items, set 'Loot type' to 'ADDITION' and enable the field 'Custom loot configuration'." +
                                " Note that additional items can only be enabled if there is at least one item in this section.",
                        Color = UiHelper.textColor,
                        Font = UiHelper.regularFont,
                        FontSize = 14
                    }
                }, categoryOverviewPanel);
            }
            else if (UiHelper.editLootType == LootableConfig.LootType.Custom)
            {
                //Title
                result.Add(new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.02f, 0.95f),
                        AnchorMax = AspectAdjust(0.9f, 0.99f)
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleLeft,
                        Text = "Container Settings",
                        Color = UiHelper.textColor,
                        FontSize = 14
                    }
                }, categoryOverviewPanel);

                CreateInputPanel(ref result, categoryOverviewPanel, "Min items in container", lootConfig.item_amount.min.ToString(),
                    $"loottable.cmd profile_set items_min", AspectAdjust(0.02f, 0.84f), AspectAdjust(0.34f, 0.95f));
                CreateInputPanel(ref result, categoryOverviewPanel, "Max items in container", lootConfig.item_amount.max.ToString(),
                    $"loottable.cmd profile_set items_max", AspectAdjust(0.35f, 0.84f), AspectAdjust(0.67f, 0.95f));
                CreateButtonPanel(ref result, categoryOverviewPanel, "Multiply item amount", "loottable.cmd multiplier open", "MULTIPLY", UiHelper.greenButtonColor,
                    AspectAdjust(0.68f, 0.84f), AspectAdjust(0.98f, 0.95f));
                /*CreateInputPanel(ref result, categoryOverviewPanel, "Random seed", lootConfig.seed.ToString(),
                    $"loottable.cmd profile_set seed", AspectAdjust(0.68f, 0.84f), AspectAdjust(0.98f, 0.95f), fontSize: 13);*/

                //Title
                result.Add(new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.02f, 0.79f),
                        AnchorMax = AspectAdjust(0.9f, 0.82f)
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleLeft,
                        Text = "Categories",
                        Color = UiHelper.textColor,
                        FontSize = 14
                    }
                }, categoryOverviewPanel);

                int i = 0; int i_max = 5;
                foreach (var category in lootConfig.categories.Values)
                {
                    if (category.isDefault) continue;

                    float min = 0.66f - i * 0.13f;
                    float max = 0.78f - i * 0.13f;

                    // Color panel
                    result.Add(new CuiPanel
                    {
                        Image = new CuiImageComponent
                        {
                            Color = UiHelper.categoryColors[category.id]
                        },
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust(0.02f, min),
                            AnchorMax = AspectAdjust(0.05f, max)
                        }   
                    }, categoryOverviewPanel);

                    CreateInputPanel(ref result, categoryOverviewPanel, "Category chance (%)", (category.chance * 100).ToString(),
                        $"loottable.cmd category_set {category.id} cat_chance", AspectAdjust(0.06f, min), AspectAdjust(0.36f, max));
                    CreateInputPanel(ref result, categoryOverviewPanel, "Minimum item count", category.itemAmount.min.ToString(),
                        $"loottable.cmd category_set {category.id} cat_min", AspectAdjust(0.37f, min), AspectAdjust(0.67f, max));
                    CreateInputPanel(ref result, categoryOverviewPanel, "Maximum item count", category.itemAmount.max.ToString(),
                        $"loottable.cmd category_set {category.id} cat_max", AspectAdjust(0.68f, min), AspectAdjust(0.98f, max));                
                    
                    i++;
                    if (i > i_max) break;
                }
            }

            #endregion

            CuiHelper.AddUi(player, result);
            return;
        }

        private void CreateSlEditUI(BasePlayer player)
        {
            if (UiHelper.uiUser != player.userID) return;
            CuiElementContainer result = new CuiElementContainer();

            var lootable = UiHelper.StaticLootables.Lootable;
            lootable.LoadItems();
            if (lootable == null)
            {
                PrintError("Lootable is null");
                return;
            }

            //Content panel
            string profilePanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.bgColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.01f, 0.01f, "x"),
                    AnchorMax = AspectAdjust(0.99f, 0.935f)
                }
            }, UiHelper.rootPanelName, UiHelper.staticLootablePanelName);

            #region Header

            //Header panel
            string profileHeadPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColorBright
                },
                RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.005f, 0.94f),
                        AnchorMax = AspectAdjust(0.995f, 0.99f)
                    }
            }, profilePanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.01f, 0f),
                    AnchorMax = AspectAdjust(0.6f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleLeft,
                    Text = lootable.UniqueId == null ? lootable.PrefabFilter : $"{lootable.PrefabFilter} at {StaticLootableDisplay.GetWorldPos(lootable.UniqueId)}",
                    Color = UiHelper.textColor,
                    FontSize = 16
                }
            }, profileHeadPanel);

            // Save Button
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.85f, 0.2f),
                    AnchorMax = AspectAdjust(0.99f, 0.8f)
                },
                Button =
                {
                    Command = $"loottable.cmd sledit save",
                    Color = UiHelper.greenButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "SAVE",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, profileHeadPanel);
            /*
            // Copy Button
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.69f, 0.2f),
                    AnchorMax = AspectAdjust(0.83f, 0.8f)
                },
                Button =
                {
                    Command = $"loottable.cmd copypaste copy_sl",
                    Color = UiHelper.greenButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "COPY CONFIG",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, profileHeadPanel);

            if (!UiHelper.copyPaste.IsEmpty)
            {
                // Paste Button
                result.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.58f, 0.2f),
                        AnchorMax = AspectAdjust(0.67f, 0.8f)
                    },
                    Button =
                    {
                        Command = $"loottable.cmd copypaste paste_sl",
                        Color = UiHelper.redButtonColor
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Text = "PASTE",
                        Color = UiHelper.buttonTextColor,
                        FontSize = 15
                    }
                }, profileHeadPanel);

                // Paste Label
                result.Add(new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.38f, 0f),
                        AnchorMax = AspectAdjust(0.57f, 1f)
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleRight,
                        Text = $"Paste config of {UiHelper.copyPaste.CopyName}\n(overwrite current config)",
                        Color = UiHelper.textColor,
                        FontSize = 13
                    }
                }, profileHeadPanel);
            }*/

            #endregion

            // Add item
            CreateButtonPanel(ref result, profilePanel, "Add item", "loottable.cmd action item_add", "ADD ITEM", UiHelper.greenButtonColor,
                AspectAdjust(0.005f, 0.85f), AspectAdjust(0.150f, 0.93f));

            #region Note

            string tipPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.155f, 0.85f),
                    AnchorMax = AspectAdjust(0.600f, 0.93f)
                }
            }, profilePanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.01f, 0.6f),
                    AnchorMax = AspectAdjust(0.95f, 0.9f)
                },
                Text =
                {
                    Align = TextAnchor.UpperLeft,
                    Text = "Note:",
                    Color = UiHelper.textColor,
                    FontSize = 14,
                }
            }, tipPanel);

            // Text
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.01f, 0f),
                    AnchorMax = AspectAdjust(0.95f, 0.6f)
                },
                Text =
                {
                    Align = TextAnchor.UpperLeft,
                    Text = "Support for StaticLootables is still in Beta, i.e. there might be some bugs. More functions will be added with the next updates.",
                    Color = UiHelper.textColor,
                    FontSize = 13,
                    Font = UiHelper.regularFont
                }
            }, tipPanel);

            #endregion

            #region Items

            // Base panel
            string itemListPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.005f, 0.01f),
                    AnchorMax = AspectAdjust(0.6f, 0.84f)
                }
            }, profilePanel);

            if (lootable.Items.Count < 1)
            {
                result.Add(new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0f, 0.1f),
                        AnchorMax = AspectAdjust(1f, 1f)
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Text = "No items yet, let's add some!",
                        Color = UiHelper.textColor,
                        FontSize = 16
                    }
                }, itemListPanel);

                result.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.35f, 0.45f),
                        AnchorMax = AspectAdjust(0.65f, 0.5f)
                    },
                    Button =
                    {
                        Command = $"loottable.cmd action item_add",
                        Color = UiHelper.greenButtonColor
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Text = "ADD ITEM",
                        Color = UiHelper.buttonTextColor,
                        FontSize = 15
                    }
                }, itemListPanel);
            }
            else
            {
                int row = 0; int row_max = 9;
                int col = 0; int col_max = 6;

                foreach (var item in lootable.Items)
                {
                    CreateItemPanel(ref result, itemListPanel, item, AspectAdjust(0.01f + row * 0.099f, 0.83f - col * 0.13f),
                        AspectAdjust(0.10f + row * 0.099f, 0.95f - col * 0.13f), true);

                    row++;
                    if (row > row_max)
                    {
                        row = 0;
                        col++;
                        if (col > col_max) break;
                    }
                }
                
            }

            #endregion

            #region Page navigation

            if (UiHelper.editLootType == LootableConfig.LootType.Custom)
            {
                result.Add(new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.01f, 0.01f),
                        AnchorMax = AspectAdjust(0.09f, 0.06f)
                    },
                    Text =
                    {
                        Align = TextAnchor.LowerLeft,
                        Text = $"Page {UiHelper.EditorCache.page+1} of {UiHelper.EditorCache.pages}",
                        Color = UiHelper.textColor,
                        Font = UiHelper.regularFont,
                        FontSize = 13
                    }
                }, itemListPanel);

                result.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.1f, 0.01f),
                        AnchorMax = AspectAdjust(0.14f, 0.04f)
                    },
                    Button =
                    {
                        Command = $"loottable.cmd action page false",
                        Color = UiHelper.EditorCache.page-1 >= 0 ? UiHelper.colorBlue : UiHelper.greyButtonColor
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Text = "<-",
                        Color = UiHelper.buttonTextColor,
                        FontSize = 15
                    }
                }, itemListPanel);

                result.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.145f, 0.01f),
                        AnchorMax = AspectAdjust(0.185f, 0.04f)
                    },
                    Button =
                    {
                        Command = $"loottable.cmd action page true",
                        Color = UiHelper.EditorCache.page+1 < UiHelper.EditorCache.pages ? UiHelper.colorBlue : UiHelper.greyButtonColor
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Text = "->",
                        Color = UiHelper.buttonTextColor,
                        FontSize = 15
                    }
                }, itemListPanel);
            }

            #endregion

            #region Sidepanel

            string categoryOverviewPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.605f, 0.01f),
                    AnchorMax = AspectAdjust(0.995f, 0.93f)
                }
            }, profilePanel);

            //Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.02f, 0.95f),
                    AnchorMax = AspectAdjust(0.9f, 0.99f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleLeft,
                    Text = "Static Lootable Settings",
                    Color = UiHelper.textColor,
                    FontSize = 14
                }
            }, categoryOverviewPanel);

            CreateInputPanel(ref result, categoryOverviewPanel, "Container size", lootable.ContainerSize.ToString(),
                $"loottable.cmd sledit container_size", AspectAdjust(0.02f, 0.86f), AspectAdjust(0.34f, 0.95f));
            CreateInputPanel(ref result, categoryOverviewPanel, "Refill rate (minutes)", lootable.Rule.RefillRate.ToString(),
                $"loottable.cmd sledit refill_rate", AspectAdjust(0.35f, 0.86f), AspectAdjust(0.67f, 0.95f));
            CreateInputPanel(ref result, categoryOverviewPanel, "Holding timer (seconds)", lootable.Timer.ToString(),
                $"loottable.cmd sledit timer", AspectAdjust(0.68f, 0.86f), AspectAdjust(0.99f, 0.95f));

            CreateButtonPanel(ref result, categoryOverviewPanel, "Lock", "loottable.cmd sledit lock", lootable.Lock == null ? "DISABLED" : "ENABLED",
                lootable.Lock == null ? UiHelper.redButtonColor : UiHelper.greenButtonColor, AspectAdjust(0.02f, 0.76f), AspectAdjust(0.34f, 0.85f));
            if (lootable.Lock != null)
            CreateInputPanel(ref result, categoryOverviewPanel, "Lock Health", lootable.Lock.Health.ToString("N0"),
                $"loottable.cmd sledit lock_hp", AspectAdjust(0.35f, 0.76f), AspectAdjust(0.67f, 0.85f));

            CreateButtonPanel(ref result, categoryOverviewPanel, "Hack", "loottable.cmd sledit hack", lootable.Hack == null ? "DISABLED" : "ENABLED",
                lootable.Hack == null ? UiHelper.redButtonColor : UiHelper.greenButtonColor, AspectAdjust(0.02f, 0.66f), AspectAdjust(0.34f, 0.75f));
            if (lootable.Hack != null)
            {
                CreateInputPanel(ref result, categoryOverviewPanel, "Hack Time (seconds)", lootable.Hack.WaitTime.ToString("N0"),
                    $"loottable.cmd sledit hack_time", AspectAdjust(0.35f, 0.66f), AspectAdjust(0.67f, 0.75f));
                CreateInputPanel(ref result, categoryOverviewPanel, "Code Reset Rate (min)", lootable.Hack.CodeResetRate.ToString("N0"),
                    $"loottable.cmd sledit hack_reset", AspectAdjust(0.68f, 0.66f), AspectAdjust(0.99f, 0.75f));
            }
                

            //CreateButtonPanel(ref result, categoryOverviewPanel, "Multiply item amount", "loottable.cmd multiplier open", "MULTIPLY", UiHelper.greenButtonColor,
            //AspectAdjust(0.68f, 0.84f), AspectAdjust(0.98f, 0.95f));

            //Title
            /*result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.02f, 0.79f),
                    AnchorMax = AspectAdjust(0.9f, 0.82f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleLeft,
                    Text = "Categories",
                    Color = UiHelper.textColor,
                    FontSize = 14
                }
            }, categoryOverviewPanel);*/

            #endregion

            CuiHelper.AddUi(player, result);
            return;
        }

        private void CreateQuarryEditUI(BasePlayer player)
        {
            if (UiHelper.uiUser != player.userID) return;
            CuiElementContainer result = new CuiElementContainer();

            Quarry dispenser = UiHelper.QuarryEditorCache.quarry;
            QuarryConfig config = UiHelper.QuarryEditorCache.config;

            //Content panel
            string profilePanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.bgColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.01f, 0.01f, "x"),
                    AnchorMax = AspectAdjust(0.99f, 0.935f)
                }
            }, UiHelper.rootPanelName, UiHelper.quarryEditPanelName);

            #region Header

            //Header panel
            string profileHeadPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColorBright
                },
                RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.005f, 0.94f),
                        AnchorMax = AspectAdjust(0.995f, 0.99f)
                    }
            }, profilePanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.01f, 0f),
                        AnchorMax = AspectAdjust(0.2f, 1f)
                    },
                Text =
                    {
                        Align = TextAnchor.MiddleLeft,
                        Text = dispenser.displayName,
                        Color = UiHelper.textColor,
                        FontSize = 16
                    }
            }, profileHeadPanel);

            // Save Button
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.85f, 0.2f),
                    AnchorMax = AspectAdjust(0.99f, 0.8f)
                },
                Button =
                {
                    Command = $"loottable.cmd open_select {UiHelper.quarryEditPanelName}",
                    Color = UiHelper.greenButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "SAVE",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, profileHeadPanel);

            #endregion

            #region Enable loot table

            string profileEnabledPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.005f, 0.85f),
                    AnchorMax = AspectAdjust(0.150f, 0.93f)
                }
            }, profilePanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.6f),
                    AnchorMax = AspectAdjust(0.95f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "Custom configuration",
                    Color = UiHelper.textColor,
                    FontSize = 14
                }
            }, profileEnabledPanel);


            // Enabled Button
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.1f),
                    AnchorMax = AspectAdjust(0.95f, 0.6f)
                },
                Button =
                {
                    Command = $"loottable.cmd disp_action set_enabled {!config.enabled}",
                    Color = config.enabled ? UiHelper.greenButtonColor : UiHelper.redButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = config.enabled ? "ENABLED" : "DISABLED",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, profileEnabledPanel);

            #endregion

            #region Add item

            string addItemPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.155f, 0.85f),
                    AnchorMax = AspectAdjust(0.3f, 0.93f)
                }
            }, profilePanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.6f),
                    AnchorMax = AspectAdjust(0.95f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "Add item",
                    Color = UiHelper.textColor,
                    FontSize = 14
                }
            }, addItemPanel);

            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.1f),
                    AnchorMax = AspectAdjust(0.95f, 0.6f)
                },
                Button =
                {
                    Command = $"loottable.cmd disp_action item_add",
                    Color = UiHelper.greenButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "ADD ITEM",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, addItemPanel);

            #endregion

            #region Tip

            string tipPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.305f, 0.85f),
                    AnchorMax = AspectAdjust(0.995f, 0.93f)
                }
            }, profilePanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.005f, 0.6f),
                    AnchorMax = AspectAdjust(0.995f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleLeft,
                    Text = "Note:",
                    Color = UiHelper.textColor,
                    FontSize = 14
                }
            }, tipPanel);


            // Text
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.005f, 0.05f),
                    AnchorMax = AspectAdjust(0.995f, 0.95f)
                },
                Text =
                {
                    Align = TextAnchor.LowerLeft,
                    Text = "This configuration <b>ONLY WORKS FOR STATIC QUARRIES</b>!",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 13,
                    Font = UiHelper.regularFont
                }
            }, tipPanel);

            #endregion

            #region Items

            // Base panel
            string configPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.005f, 0.01f),
                    AnchorMax = AspectAdjust(0.995f, 0.84f)
                }
            }, profilePanel);



            int j = 0; int j_max = 7;
            int c = 0; int c_max = 1;
            foreach(var override_itm in config.items)
            {
                CreateItemPanel(ref result, configPanel, override_itm, AspectAdjust(0.01f+j*0.06f, 0.86f-c*0.13f), AspectAdjust(0.06f+j*0.06f, 0.98f-c*0.13f),
                    true, $"loottable.cmd disp_action item_edit {override_itm.SkinItemId}");

                j++;
                if (j > j_max)
                {
                    j = 0; c++;
                    if (c > c_max) break;
                }
            }

            #endregion

            CuiHelper.AddUi(player, result);
            return;
        }

        private void CreateCollectibleEditUI(BasePlayer player)
        {
            if (UiHelper.uiUser != player.userID) return;
            CuiElementContainer result = new CuiElementContainer();

            Collectible collectible = UiHelper.CollectibleEditorCache.Collectible;
            CollectibleConfig config = UiHelper.CollectibleEditorCache.config;

            //Content panel
            string profilePanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.bgColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.01f, 0.01f, "x"),
                    AnchorMax = AspectAdjust(0.99f, 0.935f)
                }
            }, UiHelper.rootPanelName, UiHelper.collectiblePanelName);

            #region Header

            //Header panel
            string profileHeadPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColorBright
                },
                RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.005f, 0.94f),
                        AnchorMax = AspectAdjust(0.995f, 0.99f)
                    }
            }, profilePanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.01f, 0f),
                        AnchorMax = AspectAdjust(0.2f, 1f)
                    },
                Text =
                    {
                        Align = TextAnchor.MiddleLeft,
                        Text = collectible.displayName,
                        Color = UiHelper.textColor,
                        FontSize = 16
                    }
            }, profileHeadPanel);

            // Save Button
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.85f, 0.2f),
                    AnchorMax = AspectAdjust(0.99f, 0.8f)
                },
                Button =
                {
                    Command = $"loottable.cmd open_select {UiHelper.collectiblePanelName}",
                    Color = UiHelper.greenButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "SAVE",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, profileHeadPanel);

            #endregion

            #region Enable loot table

            string profileEnabledPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.005f, 0.85f),
                    AnchorMax = AspectAdjust(0.150f, 0.93f)
                }
            }, profilePanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.6f),
                    AnchorMax = AspectAdjust(0.95f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "Custom loot table",
                    Color = UiHelper.textColor,
                    FontSize = 14
                }
            }, profileEnabledPanel);


            // Enabled Button
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.1f),
                    AnchorMax = AspectAdjust(0.95f, 0.6f)
                },
                Button =
                {
                    Command = $"loottable.cmd coll_action set_enabled {!config.enabled}",
                    Color = config.enabled ? UiHelper.greenButtonColor : UiHelper.redButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = config.enabled ? "ENABLED" : "DISABLED",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, profileEnabledPanel);

            #endregion

            #region Add item

            string addItemPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.155f, 0.85f),
                    AnchorMax = AspectAdjust(0.3f, 0.93f)
                }
            }, profilePanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.6f),
                    AnchorMax = AspectAdjust(0.95f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "Add item",
                    Color = UiHelper.textColor,
                    FontSize = 14
                }
            }, addItemPanel);

            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.1f),
                    AnchorMax = AspectAdjust(0.95f, 0.6f)
                },
                Button =
                {
                    Command = $"loottable.cmd coll_action item_add",
                    Color = UiHelper.greenButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "ADD ITEM",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, addItemPanel);

            #endregion

            #region Default config

            string defaultConfPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.305f, 0.85f),
                    AnchorMax = AspectAdjust(0.45f, 0.93f)
                }
            }, profilePanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.6f),
                    AnchorMax = AspectAdjust(0.95f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "Load default config",
                    Color = UiHelper.textColor,
                    FontSize = 14
                }
            }, defaultConfPanel);

            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.1f),
                    AnchorMax = AspectAdjust(0.95f, 0.6f)
                },
                Button =
                {
                    Command = $"loottable.cmd coll_action load_default",
                    Color = UiHelper.redButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "LOAD",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, defaultConfPanel);

            #endregion

            #region Multiply

            string multiPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.455f, 0.85f),
                    AnchorMax = AspectAdjust(0.6f, 0.93f)
                }
            }, profilePanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.6f),
                    AnchorMax = AspectAdjust(0.95f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "Multiply item amount",
                    Color = UiHelper.textColor,
                    FontSize = 14
                }
            }, multiPanel);

            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.1f),
                    AnchorMax = AspectAdjust(0.95f, 0.6f)
                },
                Button =
                {
                    Command = $"loottable.cmd coll_action multiply",
                    Color = UiHelper.greenButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "MULTIPLY",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, multiPanel);

            #endregion

            #region Tip

            string tipPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.605f, 0.85f),
                    AnchorMax = AspectAdjust(0.995f, 0.93f)
                }
            }, profilePanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.005f, 0.6f),
                    AnchorMax = AspectAdjust(0.995f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleLeft,
                    Text = "Note:",
                    Color = UiHelper.textColor,
                    FontSize = 14
                }
            }, tipPanel);


            // Text
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.005f, 0.05f),
                    AnchorMax = AspectAdjust(0.995f, 0.95f)
                },
                Text =
                {
                    Align = TextAnchor.LowerLeft,
                    Text = "When the config is enabled it will <b>OVERRIDE</b> the default loot items of this entity",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 13,
                    Font = UiHelper.regularFont
                }
            }, tipPanel);

            #endregion

            #region Items

            // Base panel
            string configPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.005f, 0.01f),
                    AnchorMax = AspectAdjust(0.995f, 0.84f)
                }
            }, profilePanel);



            int j = 0; int j_max = 7;
            int c = 0; int c_max = 1;
            foreach (var override_itm in config.items)
            {
                CreateItemPanel(ref result, configPanel, override_itm, AspectAdjust(0.01f + j * 0.06f, 0.86f - c * 0.13f), AspectAdjust(0.06f + j * 0.06f, 0.98f - c * 0.13f),
                    true, $"loottable.cmd coll_action item_edit {override_itm.SkinItemId}");

                j++;
                if (j > j_max)
                {
                    j = 0; c++;
                    if (c > c_max) break;
                }
            }

            #endregion

            CuiHelper.AddUi(player, result);
            return;
        }

        private void CreateExcavatorEditUI(BasePlayer player)
        {
            if (UiHelper.uiUser != player.userID) return;
            CuiElementContainer result = new CuiElementContainer();

            Quarry excavator = UiHelper.QuarryEditorCache.quarry;
            ExcavatorConfig config = UiHelper.QuarryEditorCache.excavatorConfig;

            //Content panel
            string profilePanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.bgColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.01f, 0.01f, "x"),
                    AnchorMax = AspectAdjust(0.99f, 0.935f)
                }
            }, UiHelper.rootPanelName, UiHelper.excavatorEditPanelName);

            #region Header

            //Header panel
            string profileHeadPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColorBright
                },
                RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.005f, 0.94f),
                        AnchorMax = AspectAdjust(0.995f, 0.99f)
                    }
            }, profilePanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.01f, 0f),
                        AnchorMax = AspectAdjust(0.2f, 1f)
                    },
                Text =
                    {
                        Align = TextAnchor.MiddleLeft,
                        Text = excavator.displayName,
                        Color = UiHelper.textColor,
                        FontSize = 16
                    }
            }, profileHeadPanel);

            // Save Button
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.85f, 0.2f),
                    AnchorMax = AspectAdjust(0.99f, 0.8f)
                },
                Button =
                {
                    Command = $"loottable.cmd open_select {UiHelper.excavatorEditPanelName}",
                    Color = UiHelper.greenButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "SAVE",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, profileHeadPanel);

            #endregion

            #region Enable loot table

            string profileEnabledPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.005f, 0.85f),
                    AnchorMax = AspectAdjust(0.150f, 0.93f)
                }
            }, profilePanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.6f),
                    AnchorMax = AspectAdjust(0.95f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "Custom configuration",
                    Color = UiHelper.textColor,
                    FontSize = 14
                }
            }, profileEnabledPanel);


            // Enabled Button
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.1f),
                    AnchorMax = AspectAdjust(0.95f, 0.6f)
                },
                Button =
                {
                    Command = $"loottable.cmd excv_action set_enabled {!config.enabled}",
                    Color = config.enabled ? UiHelper.greenButtonColor : UiHelper.redButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = config.enabled ? "ENABLED" : "DISABLED",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, profileEnabledPanel);

            #endregion

            #region Belt Speed

            //CreateInputPanel(ref result, profilePanel, "Mining Speed", config.dieselBarrelDuration.ToString("N0"), "lel", AspectAdjust(0.155f, 0.85f), AspectAdjust(0.300f, 0.93f));

            #endregion

            #region Tip

            string tipPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.305f, 0.85f),
                    AnchorMax = AspectAdjust(0.995f, 0.93f)
                }
            }, profilePanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.005f, 0.6f),
                    AnchorMax = AspectAdjust(0.995f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleLeft,
                    Text = "Tip:",
                    Color = UiHelper.textColor,
                    FontSize = 14
                }
            }, tipPanel);


            // Text
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.005f, 0.05f),
                    AnchorMax = AspectAdjust(0.995f, 0.95f)
                },
                Text =
                {
                    Align = TextAnchor.LowerLeft,
                    Text =  "For each item of the excavator you can specify an override item. Item amount is the amount yielded by one diesel barrel. The exact item amount is random and may vary up to 5%",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 13,
                    Font = UiHelper.regularFont
                }
            }, tipPanel);

            #endregion

            #region Panels

            // Base panel
            string itemListPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.005f, 0.01f),
                    AnchorMax = AspectAdjust(0.995f, 0.84f)
                }
            }, profilePanel);

            int i = 0; int i_max = 1;
            int col = 0; int col_max = 1;
            foreach (var baseItem in config.items)
            {
                // Base panel
                string configPanel = result.Add(new CuiPanel
                {
                    Image = new CuiImageComponent
                    {
                        Color = UiHelper.panelColorBright
                    },
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.01f+col*0.5f, 0.52f-i*0.5f),
                        AnchorMax = AspectAdjust(0.49f+col*0.5f, 0.98f-i*0.5f)
                    }
                }, itemListPanel);

                #region Head

                // Default item image
                result.Add(new CuiElement
                {
                    Components =
                    {
                        CreateItemImage(ExcavatorConfig.GetItemIdFromKey(baseItem.Key)),
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.02 0.8", AnchorMax = "0.02 0.8",
                            OffsetMin = "0 0", OffsetMax = $"50 50"
                        }
                    },
                    Parent = configPanel
                });

                // Title
                result.Add(new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.12f, 0.82f),
                        AnchorMax = AspectAdjust(0.48f, 0.95f)
                    },
                    Text =
                    {
                        Align = TextAnchor.LowerLeft,
                        Text = $"Edit override for {ExcavatorConfig.GetShortnameFromKey(baseItem.Key)}",
                        Color = UiHelper.textColor,
                        FontSize = 15
                    }
                }, configPanel);

                // Add item
                result.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.75f, 0.81f),
                        AnchorMax = AspectAdjust(0.985f, 0.97f)
                    },
                    Button =
                    {
                        Command = $"loottable.cmd excv_action item_add {baseItem.Key}",
                        Color = UiHelper.greenButtonColor
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Text = "ADD ITEM",
                        Color = UiHelper.buttonTextColor,
                        FontSize = 15
                    }
                }, configPanel);

                #endregion

                #region Items

                int j = 0; int j_max = 7;
                int c = 0; int c_max = 1;
                foreach (var override_itm in baseItem.Value)
                {
                    CreateItemPanel(ref result, configPanel, override_itm, AspectAdjust(0.02f + j * 0.123f, 0.5f - c * 0.3f), AspectAdjust(0.12f + j * 0.123f, 0.75f - c * 0.3f),
                        true, $"loottable.cmd excv_action item_edit {baseItem.Key} {override_itm.SkinItemId}");

                    j++;
                    if (j > j_max)
                    {
                        j = 0; c++;
                        if (c > c_max) break;
                    }
                }

                i++;
                if (i > i_max)
                {
                    i = 0; col++;
                    if (col > col_max) break;
                }

                #endregion
            }

            #endregion

            CuiHelper.AddUi(player, result);
            return;
        }

        private void CreateStacksizeUI(BasePlayer player)
        {
            if (UiHelper.uiUser != player.userID) return;
            CuiElementContainer result = new CuiElementContainer();

            //Content panel
            string profilePanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.bgColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.01f, 0.01f, "x"),
                    AnchorMax = AspectAdjust(0.99f, 0.935f)
                }
            }, UiHelper.rootPanelName, UiHelper.stacksizePanelName);

            #region Header

            //Header panel
            string profileHeadPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColorBright
                },
                RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.005f, 0.94f),
                        AnchorMax = AspectAdjust(0.995f, 0.99f)
                    }
            }, profilePanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.01f, 0f),
                        AnchorMax = AspectAdjust(0.2f, 1f)
                    },
                Text =
                    {
                        Align = TextAnchor.MiddleLeft,
                        Text = "Stack Size Editor",
                        Color = UiHelper.textColor,
                        FontSize = 16
                    }
            }, profileHeadPanel);

            // Save Button
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.85f, 0.2f),
                    AnchorMax = AspectAdjust(0.99f, 0.8f)
                },
                Button =
                {
                    Command = $"loottable.cmd open_select {UiHelper.stacksizePanelName}",
                    Color = UiHelper.greenButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "SAVE",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, profileHeadPanel);

            #endregion

            #region Enable loot table

            string profileEnabledPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.005f, 0.85f),
                    AnchorMax = AspectAdjust(0.150f, 0.93f)
                }
            }, profilePanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.6f),
                    AnchorMax = AspectAdjust(0.95f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "Custom stack size",
                    Color = UiHelper.textColor,
                    FontSize = 14
                }
            }, profileEnabledPanel);


            // Enabled Button
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.1f),
                    AnchorMax = AspectAdjust(0.95f, 0.6f)
                },
                Button =
                {
                    Command = $"loottable.cmd stacksize set_enabled {!StackManager.stacksizeConfig.enabled}",
                    Color = StackManager.stacksizeConfig.enabled ? UiHelper.greenButtonColor : UiHelper.redButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = StackManager.stacksizeConfig.enabled ? "ENABLED" : "DISABLED",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, profileEnabledPanel);

            #endregion

            #region Multiplier Panel

            string multiplierPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.155f, 0.85f),
                    AnchorMax = AspectAdjust(0.300f, 0.93f)
                }
            }, profilePanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.6f),
                    AnchorMax = AspectAdjust(0.95f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "Global stack size multiplier",
                    Color = UiHelper.textColor,
                    FontSize = 14
                }
            }, multiplierPanel);

            // Input background
            string inputPanelA = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColorBright
                },
                RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.05f, 0.1f),
                        AnchorMax = AspectAdjust(0.95f, 0.55f)
                    }
            }, multiplierPanel);

            // Input
            result.Add(new CuiElement
            {
                Parent = inputPanelA,
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        FontSize = 14,
                        Align = TextAnchor.MiddleLeft,
                        Command = "loottable.cmd stacksize set_multi",
                        Text = StackManager.stacksizeConfig.globalMultiplier.ToString("N1", NUMBER_FORMAT),
                        Color = UiHelper.textColor,
                        CharsLimit = 80,
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.02 0", AnchorMax = "1 1",
                    }
                }
            });

            #endregion

            #region Cat Multiplier Panel

            string catMultiplierPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.305f, 0.85f),
                    AnchorMax = AspectAdjust(0.450f, 0.93f)
                }
            }, profilePanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.6f),
                    AnchorMax = AspectAdjust(0.95f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "Category stack size multiplier",
                    Color = UiHelper.textColor,
                    FontSize = 13
                }
            }, catMultiplierPanel);

            // Input background
            string inputPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColorBright
                },
                RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.05f, 0.1f),
                        AnchorMax = AspectAdjust(0.95f, 0.55f)
                    }
            }, catMultiplierPanel);

            // Input
            result.Add(new CuiElement
            {
                Parent = inputPanel,
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        FontSize = 14,
                        Align = TextAnchor.MiddleLeft,
                        Command = "loottable.cmd stacksize set_cat_multi",

                        // Use -1 to compensate index of custom items
                        Text = StackManager.stacksizeConfig.categoryMultipliers[UiHelper.Stacksize.stacksizeCat-1].ToString("N1", NUMBER_FORMAT),
                        Color = UiHelper.textColor,
                        CharsLimit = 80,
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.02 0", AnchorMax = "1 1",
                    }
                }
            });

            #endregion

            #region Reset category

            string resetPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.455f, 0.85f),
                    AnchorMax = AspectAdjust(0.600f, 0.93f)
                }
            }, profilePanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.6f),
                    AnchorMax = AspectAdjust(0.95f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "Reset category to vanilla",
                    Color = UiHelper.textColor,
                    FontSize = 14
                }
            }, resetPanel);


            // Enabled Button
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.1f),
                    AnchorMax = AspectAdjust(0.95f, 0.6f)
                },
                Button =
                {
                    Command = $"loottable.cmd stacksize reset {UiHelper.Stacksize.stacksizeCat}",
                    Color = UiHelper.redButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "RESET CATEGORY",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, resetPanel);

            #endregion

            #region Tip

            string tipPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.605f, 0.85f),
                    AnchorMax = AspectAdjust(0.995f, 0.93f)
                }
            }, profilePanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.02f, 0.6f),
                    AnchorMax = AspectAdjust(0.98f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleLeft,
                    Text = "Tip:",
                    Color = UiHelper.textColor,
                    FontSize = 14
                }
            }, tipPanel);


            // Text
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.02f, 0.1f),
                    AnchorMax = AspectAdjust(0.98f, 0.65f)
                },
                Text =
                {
                    Align = TextAnchor.LowerLeft,
                    Text = "When an item is using a custom stacksize, a yellow bar will be displayed next to it in the editor and it will not be affected by the global stacksize multiplier",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 12,
                    Font = UiHelper.regularFont
                }
            }, tipPanel);

            #endregion

            // Base panel
            string itemListPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.005f, 0.01f),
                    AnchorMax = AspectAdjust(0.995f, 0.84f)
                }
            }, profilePanel);

            #region Page navigation

            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.005f, 0.91f),
                    AnchorMax = AspectAdjust(0.06f, 0.95f)
                },
                Text =
                {
                    Align = TextAnchor.LowerLeft,
                    Text = $"Page {UiHelper.Stacksize.stacksizePage+1} of {UiHelper.Stacksize.stacksizePages}",
                    Color = UiHelper.textColor,
                    Font = UiHelper.regularFont,
                    FontSize = 13
                }
            }, itemListPanel);

            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.065f, 0.91f),
                    AnchorMax = AspectAdjust(0.1f, 0.94f)
                },
                Button =
                {
                    Command = $"loottable.cmd stacksize page false",
                    Color = UiHelper.Stacksize.stacksizePage-1 >= 0 ? UiHelper.colorBlue : UiHelper.greyButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "<-",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, itemListPanel);

            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.105f, 0.91f),
                    AnchorMax = AspectAdjust(0.140f, 0.94f)
                },
                Button =
                {
                    Command = $"loottable.cmd stacksize page true",
                    Color = UiHelper.Stacksize.stacksizePage+1 < UiHelper.Stacksize.stacksizePages ? UiHelper.colorBlue : UiHelper.greyButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "->",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, itemListPanel);

            #endregion

            #region Tabs

            // Head panel
            string itemListHeadPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.transparentColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.005f, 0.8f),
                    AnchorMax = AspectAdjust(0.995f, 0.84f)
                }
            }, profilePanel);

            // Skip first element (custom items)
            int i = 1;
            foreach (var cat in StackManager.itemCategoryList.Skip(1))
            {
                result.Add(new CuiButton
                {
                    RectTransform =
                    {
                        // Subtract 1 to fix i = 1
                        AnchorMin = AspectAdjust(0f+(i-1)*0.071f, 0f),
                        AnchorMax = AspectAdjust(0.071f+(i-1)*0.071f, 1f)
                    },
                    Button =
                    {
                        Command = $"loottable.cmd stacksize switch_cat {i}",
                        Color = UiHelper.Stacksize.stacksizeCat == i ? UiHelper.transparentColor : UiHelper.panelColorBright
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Text = cat.Key.ToUpper(),
                        Color = UiHelper.buttonTextColor,
                        FontSize = 13
                    }
                }, itemListHeadPanel);
                i++;
            }

            #endregion

            CuiHelper.AddUi(player, result);

            CuiElementContainer items = new CuiElementContainer();

            #region Items

            int page = UiHelper.Stacksize.stacksizePage;
            int items_per_page = UiHelper.Stacksize.itemsPerStacksizePage;

            List<int> currentCat = StackManager.itemCategoryList.Values.ToList()[UiHelper.Stacksize.stacksizeCat];

            DPrint($"total {currentCat.Count} idx {page * items_per_page} ct {(page * items_per_page + items_per_page < currentCat.Count ? items_per_page : currentCat.Count - page * items_per_page)}");

            int r = 0; int r_max = 8;
            int c = 0; int c_max = 3;
            List<int> currentPage = currentCat.GetRange(page * items_per_page, page * items_per_page + items_per_page < currentCat.Count ? items_per_page : currentCat.Count - page * items_per_page);

            foreach (var itemid in currentPage)
            {
                ItemDefinition itemdef = ItemManager.FindItemDefinition(itemid);

                // Base panel
                string itemPanel = items.Add(new CuiPanel
                {
                    Image = new CuiImageComponent
                    {
                        Color = UiHelper.panelColor
                    },
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.005f+c*0.25f, 0.81f-r*0.1f),
                        AnchorMax = AspectAdjust(0.245f+c*0.25f, 0.90f-r*0.1f)
                    }
                }, itemListPanel);

                // Color panel
                if (!StackManager.StacksizeIsVanilla(itemid))
                    items.Add(new CuiPanel
                    {
                        Image = new CuiImageComponent
                        {
                            Color = UiHelper.colorYellowBright
                        },
                        RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.00f, 0),
                        AnchorMax = AspectAdjust(0.01f, 1)
                    }
                    }, itemPanel);

                items.Add(new CuiElement
                {
                    Components =
                    {
                        CreateItemImage(itemid),
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.025 0.1", AnchorMax = "0.025 0.1",
                            OffsetMin = "0 0", OffsetMax = $"40 40"
                        }
                    },
                    Parent = itemPanel
                });

                items.Add(new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.18f, 0.1f),
                        AnchorMax = AspectAdjust(0.5f, 1f)
                    },
                    Text =
                    {
                        Align = TextAnchor.LowerLeft,
                        Text = itemdef.displayName.english,//$"{itemdef.shortname}\n{itemdef.itemid}",
                        Color = UiHelper.textColor,
                        Font = UiHelper.regularFont,
                        FontSize = 13
                    }
                }, itemPanel);

                items.Add(new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.7f, 0.5f),
                        AnchorMax = AspectAdjust(0.95f, 0.9f)
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleLeft,
                        Text = StackManager.stacksizeConfig.GetVanillaStacksize(itemdef.itemid).ToString("N0", NUMBER_FORMAT),
                        Color = UiHelper.textColor,
                        Font = UiHelper.regularFont,
                        FontSize = 11
                    }
                }, itemPanel);

                items.Add(new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.55f, 0.5f),
                        AnchorMax = AspectAdjust(0.67f, 0.9f)
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleRight,
                        Text = "Vanilla:",
                        Color = UiHelper.textColor,
                        Font = UiHelper.regularFont,
                        FontSize = 11
                    }
                }, itemPanel);

                items.Add(new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.55f, 0.1f),
                        AnchorMax = AspectAdjust(0.67f, 0.5f)
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleRight,
                        Text = "Custom:",
                        Color = UiHelper.textColor,
                        Font = UiHelper.regularFont,
                        FontSize = 11
                    }
                }, itemPanel);

                CreateInputPanelSmall(ref items, itemPanel, StackManager.stacksizeConfig.GetCustomStacksize(itemdef.itemid).ToString("N0", NUMBER_FORMAT),
                    $"loottable.cmd stacksize change {itemdef.itemid}", AspectAdjust(0.7f, 0.1f), AspectAdjust(0.95f, 0.5f), 12);

                r++;
                if (r > r_max)
                {
                    r = 0;
                    c++;
                    if (c > c_max) break;
                }
            }

            CuiHelper.AddUi(player, items);
            return;
            #endregion

        }

        private void CreateGatherUI(BasePlayer player)
        {
            if (UiHelper.uiUser != player.userID) return;
            CuiElementContainer result = new CuiElementContainer();

            //Content panel
            string contentPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.bgColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.01f, 0.01f, "x"),
                    AnchorMax = AspectAdjust(0.99f, 0.935f)
                }
            }, UiHelper.rootPanelName, UiHelper.gatherPanelName);

            #region Header

            //Header panel
            string profileHeadPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColorBright
                },
                RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.005f, 0.94f),
                        AnchorMax = AspectAdjust(0.995f, 0.99f)
                    }
            }, contentPanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.01f, 0f),
                        AnchorMax = AspectAdjust(0.2f, 1f)
                    },
                Text =
                    {
                        Align = TextAnchor.MiddleLeft,
                        Text = "Gather Configuration",
                        Color = UiHelper.textColor,
                        FontSize = 16
                    }
            }, profileHeadPanel);

            // Save Button
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.85f, 0.2f),
                    AnchorMax = AspectAdjust(0.99f, 0.8f)
                },
                Button =
                {
                    Command = $"loottable.cmd open_select {UiHelper.gatherPanelName}",
                    Color = UiHelper.greenButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "SAVE",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, profileHeadPanel);

            #endregion

            #region Enable loot table

            string profileEnabledPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.005f, 0.85f),
                    AnchorMax = AspectAdjust(0.150f, 0.93f)
                }
            }, contentPanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.6f),
                    AnchorMax = AspectAdjust(0.95f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "Custom gather rates",
                    Color = UiHelper.textColor,
                    FontSize = 14
                }
            }, profileEnabledPanel);


            // Enabled Button
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.1f),
                    AnchorMax = AspectAdjust(0.95f, 0.6f)
                },
                Button =
                {
                    Command = $"loottable.cmd gather set_enabled {!GatherManager.gatherConfig.enabled}",
                    Color = GatherManager.gatherConfig.enabled ? UiHelper.greenButtonColor : UiHelper.redButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = GatherManager.gatherConfig.enabled ? "ENABLED" : "DISABLED",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, profileEnabledPanel);

            #endregion

            #region Multiplier Panel

            string multiplierPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.155f, 0.85f),
                    AnchorMax = AspectAdjust(0.300f, 0.93f)
                }
            }, contentPanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.6f),
                    AnchorMax = AspectAdjust(0.95f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "Global gather multiplier",
                    Color = UiHelper.textColor,
                    FontSize = 14
                }
            }, multiplierPanel);

            // Input background
            result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColorBright
                },
                RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.05f, 0.1f),
                        AnchorMax = AspectAdjust(0.95f, 0.55f)
                    }
            }, multiplierPanel);

            // Input default
            /*result.Add(new CuiLabel
            {
                RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.07f, 0.1f),
                        AnchorMax = AspectAdjust(0.93f, 0.55f)
                    },
                Text =
                    {
                        Align = TextAnchor.MiddleLeft,
                        Text = GatherManager.gatherConfig.globalMultiplier.ToString("N1", NUMBER_FORMAT),
                        Color = UiHelper.greyTextColor,
                        FontSize = 13,
                        Font = UiHelper.regularFont
                    }
            }, multiplierPanel);*/

            // Input
            result.Add(new CuiElement
            {
                Parent = multiplierPanel,
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        FontSize = 14,
                        Align = TextAnchor.MiddleLeft,
                        Command = "loottable.cmd gather set_multi",
                        Color = UiHelper.textColor,
                        CharsLimit = 8,
                        Text = GatherManager.gatherConfig.globalMultiplier.ToString("N1", NUMBER_FORMAT)
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.07 0.1", AnchorMax = "0.93 0.6",
                    }
                }
            });

            #endregion

            #region Reset

            string resetPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.305f, 0.85f),
                    AnchorMax = AspectAdjust(0.450f, 0.93f)
                }
            }, contentPanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.6f),
                    AnchorMax = AspectAdjust(0.95f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "Reset multipliers to vanilla",
                    Color = UiHelper.textColor,
                    FontSize = 14
                }
            }, resetPanel);


            // Enabled Button
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.1f),
                    AnchorMax = AspectAdjust(0.95f, 0.6f)
                },
                Button =
                {
                    Command = $"loottable.cmd gather reset",
                    Color = UiHelper.redButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "RESET",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, resetPanel);

            #endregion

            #region Tip

            string tipPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.455f, 0.85f),
                    AnchorMax = AspectAdjust(0.995f, 0.93f)
                }
            }, contentPanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.02f, 0.6f),
                    AnchorMax = AspectAdjust(0.98f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleLeft,
                    Text = "Tip:",
                    Color = UiHelper.textColor,
                    FontSize = 14
                }
            }, tipPanel);


            // Text
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.02f, 0.1f),
                    AnchorMax = AspectAdjust(0.98f, 0.65f)
                },
                Text =
                {
                    Align = TextAnchor.LowerLeft,
                    Text = "When an item is using a custom multiplier it will not be affected by the global multiplier",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 12,
                    Font = UiHelper.regularFont
                }
            }, tipPanel);

            #endregion

            // Base panel
            string itemListPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.005f, 0.01f),
                    AnchorMax = AspectAdjust(0.995f, 0.84f)
                }
            }, contentPanel);

            #region Page navigation

            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.005f, 0.96f),
                    AnchorMax = AspectAdjust(0.06f, 1)
                },
                Text =
                {
                    Align = TextAnchor.LowerLeft,
                    Text = $"Page {(UiHelper.gatherPage ? "2" : "1")} of 2",
                    Color = UiHelper.textColor,
                    Font = UiHelper.regularFont,
                    FontSize = 13
                }
            }, itemListPanel);

            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.065f, 0.96f),
                    AnchorMax = AspectAdjust(0.1f, 0.99f)
                },
                Button =
                {
                    Command = $"loottable.cmd gather page",
                    Color = UiHelper.gatherPage ? UiHelper.colorBlue : UiHelper.greyButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "<-",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, itemListPanel);

            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.105f, 0.96f),
                    AnchorMax = AspectAdjust(0.140f, 0.99f)
                },
                Button =
                {
                    Command = $"loottable.cmd gather page",
                    Color = !UiHelper.gatherPage ? UiHelper.colorBlue : UiHelper.greyButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "->",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, itemListPanel);

            #endregion

            #region Items

            int r = 0; int r_max = 10;
            int c = 0; int c_max = 3;

            var list = UiHelper.gatherPage ? GatherManager.gatherConfig.multipliers.Where(x => x.Key.EndsWith(".corpse")) : GatherManager.gatherConfig.multipliers.Where(x => !x.Key.EndsWith(".corpse"));

            foreach (var group in list)
            {
                if (r + 1 >= r_max)
                {
                    r = 0;
                    c++;
                }

                // Header panel
                string headerPanel = result.Add(new CuiPanel
                {
                    Image = new CuiImageComponent
                    {
                        Color = UiHelper.panelColor
                    },
                    RectTransform =
                        {
                            AnchorMin = AspectAdjust(0.005f+c*0.25f, 0.91f-r*0.09f),
                            AnchorMax = AspectAdjust(0.245f+c*0.25f, 0.95f-r*0.09f)
                        }
                }, itemListPanel);

                result.Add(new CuiLabel
                {
                    RectTransform =
                        {
                            AnchorMin = AspectAdjust(0.05f, 0.1f),
                            AnchorMax = AspectAdjust(0.9f, 1f)
                        },
                    Text =
                        {
                            Align = TextAnchor.LowerLeft,
                            Text = group.Key.Split('.')[0].TitleCase() + (group.Key.EndsWith(".entity") ? " (growable only)" : ""),
                            Color = UiHelper.textColor,
                            Font = UiHelper.boldFont,
                            FontSize = 14
                        }
                }, headerPanel);

                r++;
                if (r > r_max)
                {
                    r = 0;
                    c++;
                    if (c > c_max) break;
                }

                foreach (var item in group.Value)
                {
                    ItemDefinition itemdef = ItemManager.FindItemDefinition(item.Key);

                    // Base panel
                    string itemPanel = result.Add(new CuiPanel
                    {
                        Image = new CuiImageComponent
                        {
                            Color = UiHelper.panelColor
                        },
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust(0.005f+c*0.25f, 0.91f-r*0.09f),
                            AnchorMax = AspectAdjust(0.245f+c*0.25f, 0.99f-r*0.09f)
                        }
                    }, itemListPanel);

                    result.Add(new CuiElement
                    {
                        Components =
                        {
                            CreateItemImage(itemdef.itemid),
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.025 0.1", AnchorMax = "0.025 0.1",
                                OffsetMin = "0 0", OffsetMax = $"40 40"
                            }
                        },
                        Parent = itemPanel
                    });

                    result.Add(new CuiLabel
                    {
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust(0.18f, 0.1f),
                            AnchorMax = AspectAdjust(0.5f, 1f)
                        },
                        Text =
                        {
                            Align = TextAnchor.LowerLeft,
                            Text = itemdef.displayName.english,
                            Color = UiHelper.textColor,
                            Font = UiHelper.regularFont,
                            FontSize = 13
                        }
                    }, itemPanel);

                    result.Add(new CuiLabel
                    {
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust(0.51f, 0.1f),
                            AnchorMax = AspectAdjust(0.67f, 0.5f)
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleRight,
                            Text = "Multiplier:",
                            Color = UiHelper.textColor,
                            Font = UiHelper.regularFont,
                            FontSize = 11
                        }
                    }, itemPanel);

                    CreateInputPanelSmall(ref result, itemPanel, item.Value.ToString("N1", NUMBER_FORMAT),
                        $"loottable.cmd gather change {group.Key} {itemdef.itemid}", AspectAdjust(0.7f, 0.1f), AspectAdjust(0.95f, 0.5f), 12, 7);

                    r++;
                    if (r > r_max)
                    {
                        r = 0;
                        c++;
                        if (c > c_max) break;
                    }
                }
                
            }

            CuiHelper.AddUi(player, result);
            return;
            #endregion

        }

        private void CreateConfigUI(BasePlayer player)
        {
            if (UiHelper.uiUser != player.userID) return;
            CuiElementContainer result = new CuiElementContainer();

            //Content panel
            string contentPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.bgColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.01f, 0.01f, "x"),
                    AnchorMax = AspectAdjust(0.99f, 0.935f)
                }
            }, UiHelper.rootPanelName, UiHelper.configPanelName);

            #region Header

            //Header panel
            string profileHeadPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColorBright
                },
                RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.005f, 0.94f),
                        AnchorMax = AspectAdjust(0.995f, 0.99f)
                    }
            }, contentPanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.01f, 0f),
                        AnchorMax = AspectAdjust(0.2f, 1f)
                    },
                Text =
                    {
                        Align = TextAnchor.MiddleLeft,
                        Text = "Configuration Manager",
                        Color = UiHelper.textColor,
                        FontSize = 16
                    }
            }, profileHeadPanel);

            // Save Button
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.85f, 0.2f),
                    AnchorMax = AspectAdjust(0.99f, 0.8f)
                },
                Button =
                {
                    Command = $"loottable.cmd open_select {UiHelper.configPanelName}",
                    Color = UiHelper.greenButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "SAVE",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, profileHeadPanel);

            #endregion

            #region Reset

            string resetPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.005f, 0.85f),
                    AnchorMax = AspectAdjust(0.150f, 0.93f)
                }
            }, contentPanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.6f),
                    AnchorMax = AspectAdjust(0.95f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "Restore default values",
                    Color = UiHelper.textColor,
                    FontSize = 14
                }
            }, resetPanel);


            // Enabled Button
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.1f),
                    AnchorMax = AspectAdjust(0.95f, 0.6f)
                },
                Button =
                {
                    Command = $"loottable.cmd config reset",
                    Color = UiHelper.redButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "RESET",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, resetPanel);

            #endregion

            #region Tip

            string tipPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.155f, 0.85f),
                    AnchorMax = AspectAdjust(0.995f, 0.93f)
                }
            }, contentPanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.004f, 0.6f),
                    AnchorMax = AspectAdjust(0.98f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleLeft,
                    Text = "Tip:",
                    Color = UiHelper.textColor,
                    FontSize = 14
                }
            }, tipPanel);


            // Text
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.004f, 0.1f),
                    AnchorMax = AspectAdjust(0.98f, 0.65f)
                },
                Text =
                {
                    Align = TextAnchor.LowerLeft,
                    Text = "Set the value Fuel in the Airwolf Configuration to -1 to use the default fuel amount which is usually 10% of the fuel stack size.",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 12,
                    Font = UiHelper.regularFont
                }
            }, tipPanel);

            #endregion

            #region Items
            // Base panel
            string itemListPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.005f, 0.01f),
                    AnchorMax = AspectAdjust(0.995f, 0.84f)
                }
            }, contentPanel);

            int r = 0; int c = 0;

            CreateHeaderPanel(ref result, itemListPanel, ref r, c, "Furnace Config");

            string text = SimpleSplitter != null ? "Furnace config is disabled when SimpleSplitter is installed" : "Enable custom furnace speed";
            CreateBooleanPanel(ref result, itemListPanel, ref r, c, $"loottable.cmd config fc_enable", text, ConfigManager.FurnaceConfig.enabled);

            CreateTextInputPanel(ref result, itemListPanel, ref r, c, $"loottable.cmd config fc_speed ef", "Multiplier:", "Smelting Speed",
                ConfigManager.FurnaceConfig.electricFurnaceSpeed.ToString("N2"), -1196547867);

            CreateTextInputPanel(ref result, itemListPanel, ref r, c, $"loottable.cmd config fc_speed sf", "Multiplier:", "Smelting Speed",
                ConfigManager.FurnaceConfig.sFsmeltingSpeedMultiplier.ToString("N2"), -1999722522);

            CreateTextInputPanel(ref result, itemListPanel, ref r, c, $"loottable.cmd config fc_speed lf", "Multiplier:", "Smelting Speed",
                ConfigManager.FurnaceConfig.lFsmeltingSpeedMultiplier.ToString("N2"), -1992717673);

            CreateTextInputPanel(ref result, itemListPanel, ref r, c, $"loottable.cmd config fc_speed rf", "Multiplier:", "Refinery Speed",
                ConfigManager.FurnaceConfig.rFsmeltingSpeedMultiplier.ToString("N2"), -1293296287);

            CreateTextInputPanel(ref result, itemListPanel, ref r, c, $"loottable.cmd config fc_speed bbq", "Multiplier:", "Cooking Speed",
                ConfigManager.FurnaceConfig.bbqSmeltingSpeedMultiplier.ToString("N2"), 1099314009);

            CreateTextInputPanel(ref result, itemListPanel, ref r, c, $"loottable.cmd config fc_speed cf", "Multiplier:", "Cooking Speed",
                ConfigManager.FurnaceConfig.campfireSmeltingSpeedMultiplier.ToString("N2"), 1946219319);

            CreateTextInputPanel(ref result, itemListPanel, ref r, c, $"loottable.cmd config fc_speed mx", "Multiplier:", "Mixing Speed",
                ConfigManager.FurnaceConfig.mixingSpeedMultiplier.ToString("N2"), 1259919256);

            //CreateHeaderPanel(ref result, itemListPanel, ref r, c, "Charcoal Config");

            CreateTextInputPanel(ref result, itemListPanel, ref r, c, $"loottable.cmd config char_chance", "Chance (%)", "Chance for wood to turn into coal",
                (ConfigManager.FurnaceConfig.charcoalChance * 100).ToString(), -1938052175);

            CreateTextInputPanel(ref result, itemListPanel, ref r, c, $"loottable.cmd config char_amt", "Amount:", "Charcoal amount per piece of wood",
                ConfigManager.FurnaceConfig.charcoalAmount.ToString(), -1938052175);

            

            r = 0; c = 1;

            CreateHeaderPanel(ref result, itemListPanel, ref r, c, "Air Wolf Config");

            CreateTextInputPanel(ref result, itemListPanel, ref r, c, $"loottable.cmd config aw_fuel mini", "Fuel:", "Default Minicopter Fuel",
                ConfigManager.AirwolfConfig.minicopterFuel.ToString(), "minicopter");

            CreateTextInputPanel(ref result, itemListPanel, ref r, c, $"loottable.cmd config aw_fuel tcop", "Fuel:", "Default Scrap Heli Fuel",
                ConfigManager.AirwolfConfig.scrapHeliFuel.ToString(), "scrapheli");

            CreateTextInputPanel(ref result, itemListPanel, ref r, c, $"loottable.cmd config aw_fuel atck", "Fuel:", "Default Attack Heli Fuel",
                ConfigManager.AirwolfConfig.attackHeliFuel.ToString(), "attack");

            r = 0; c = 2;

            CreateHeaderPanel(ref result, itemListPanel, ref r, c, "Supply Drop Config");

            if (LootDefender == null && FancyDrop == null)
            {
                CreateBooleanPanel(ref result, itemListPanel, ref r, c, $"loottable.cmd config ad_enable", "Enable custom configuration", ConfigManager.AirdropConfig.enabled);

                CreateTextInputPanel(ref result, itemListPanel, ref r, c, $"loottable.cmd config ad_plane_speed", "Multiplier:", "Cargo Plane Speed",
                    ConfigManager.AirdropConfig.planeSpeedMultiplier.ToString("N2"));

                CreateTextInputPanel(ref result, itemListPanel, ref r, c, $"loottable.cmd config ad_height", "Height:", "Cargo Plane Height\n(can also be negative)",
                    ConfigManager.AirdropConfig.planeHeight.ToString());

                CreateTextInputPanel(ref result, itemListPanel, ref r, c, $"loottable.cmd config ad_smoke_d", "Duration:", "Supply Signal Smoke Duration (seconds)",
                    ConfigManager.AirdropConfig.smokeDuration.ToString());

                CreateTextInputPanel(ref result, itemListPanel, ref r, c, $"loottable.cmd config ad_speed", "Speed:", "Supply Drop Fall Speed (lower = faster)",
                    ConfigManager.AirdropConfig.dropFallSpeed.ToString("N2"));

                CreateBooleanPanel(ref result, itemListPanel, ref r, c, $"loottable.cmd config ad_smoke", "Smoke Effect for Supply Drop", ConfigManager.AirdropConfig.dropFallSmoke);

                CreateBooleanPanel(ref result, itemListPanel, ref r, c, $"loottable.cmd config ad_exact_drop", "Exact Position (the drop lands exactly where the supply signal was thrown)", ConfigManager.AirdropConfig.exactDropPosition);

                if (!ConfigManager.AirdropConfig.exactDropPosition)
                    CreateTextInputPanel(ref result, itemListPanel, ref r, c, $"loottable.cmd config ad_pos_tolerance", "Tolerance:", "Drop Position Tolerance",
                        ConfigManager.AirdropConfig.randomPosTolerance.ToString());
            }
            else
            {
                CreateInfoPanel(ref result, itemListPanel, ref r, c, "Supply drop config is disabled when LootDefender or FancyDrop is installed. Use the suply drop config of LootDefender / FancyDrop instead. To use the built-in config remove FancyDrop / LootDefender");
            }

            r = 0; c = 3;

            CreateHeaderPanel(ref result, itemListPanel, ref r, c, "Recycler Config");

            CreateBooleanPanel(ref result, itemListPanel, ref r, c, $"loottable.cmd config rc_enable", "Enable custom recycler speed", ConfigManager.FurnaceConfig.recyclerEnabled);

            CreateTextInputPanel(ref result, itemListPanel, ref r, c, $"loottable.cmd config fc_speed rec", "Multiplier:", "Recycling Speed",
                ConfigManager.FurnaceConfig.recyclingSpeedMultiplier.ToString("N2"), "recycler");

            CuiHelper.AddUi(player, result);
            return;
            #endregion

        }

        private void CreateButtonPanel(ref CuiElementContainer result, string parent, string label, string command, string buttonText, string buttonColor, string anchorMin, string anchorMax)
        {
            string buttonPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = anchorMin,
                    AnchorMax = anchorMax
                }
            }, parent);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.6f),
                    AnchorMax = AspectAdjust(0.95f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = label,
                    Color = UiHelper.textColor,
                    FontSize = 14
                }
            }, buttonPanel);


            // Enabled Button
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.1f),
                    AnchorMax = AspectAdjust(0.95f, 0.6f)
                },
                Button =
                {
                    Command = command,
                    Color = buttonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = buttonText,
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, buttonPanel);
        }

        private void CreateTextInputPanel(ref CuiElementContainer result, string parent, ref int r, int c, string command, string label, string name, string value, int itemid)
        {
            // Base panel
            string itemPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                        {
                            AnchorMin = AspectAdjust(0.005f+c*0.25f, 0.91f-r*0.09f),
                            AnchorMax = AspectAdjust(0.245f+c*0.25f, 0.99f-r*0.09f)
                        }
            }, parent);

            result.Add(new CuiElement
            {
                Components =
                {
                    CreateItemImage(itemid),
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.025 0.05", AnchorMax = "0.025 0.1",
                        OffsetMin = "0 0", OffsetMax = $"40 40"
                    }
                },
                Parent = itemPanel
            });

            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.18f, 0.1f),
                    AnchorMax = AspectAdjust(0.5f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.LowerLeft,
                    Text = name,
                    Color = UiHelper.textColor,
                    Font = UiHelper.regularFont,
                    FontSize = 13
                }
            }, itemPanel);

            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.51f, 0.1f),
                    AnchorMax = AspectAdjust(0.67f, 0.5f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleRight,
                    Text = label,
                    Color = UiHelper.textColor,
                    Font = UiHelper.regularFont,
                    FontSize = 11
                }
            }, itemPanel);

            CreateInputPanelSmall(ref result, itemPanel, value, command, AspectAdjust(0.7f, 0.1f), AspectAdjust(0.95f, 0.5f), 12, 7);

            r++;
        }

        private void CreateTextInputPanel(ref CuiElementContainer result, string parent, ref int r, int c, string command, string label, string name, string value, string image = null)
        {
            // Base panel
            string itemPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                        {
                            AnchorMin = AspectAdjust(0.005f+c*0.25f, 0.91f-r*0.09f),
                            AnchorMax = AspectAdjust(0.245f+c*0.25f, 0.99f-r*0.09f)
                        }
            }, parent);

            if (image != null)
                result.Add(new CuiElement
                {
                    Components =
                    {
                        CreateImage(image),
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.025 0.05", AnchorMax = "0.025 0.1",
                            OffsetMin = "0 0", OffsetMax = $"37 37"
                        }
                    },
                    Parent = itemPanel
                });

            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = image == null ? AspectAdjust(0.03f, 0.1f) : AspectAdjust(0.18f, 0.1f),
                    AnchorMax = AspectAdjust(0.5f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.LowerLeft,
                    Text = name,
                    Color = UiHelper.textColor,
                    Font = UiHelper.regularFont,
                    FontSize = 13
                }
            }, itemPanel);

            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.51f, 0.1f),
                    AnchorMax = AspectAdjust(0.67f, 0.5f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleRight,
                    Text = label,
                    Color = UiHelper.textColor,
                    Font = UiHelper.regularFont,
                    FontSize = 11
                }
            }, itemPanel);

            CreateInputPanelSmall(ref result, itemPanel, value, command, AspectAdjust(0.7f, 0.1f), AspectAdjust(0.95f, 0.5f), 12, 7);

            r++;
        }

        private void CreateBooleanPanel(ref CuiElementContainer result, string parent, ref int r, int c, string command, string name, bool value, int? itemid = null)
        {
            // Base panel
            string itemPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                        {
                            AnchorMin = AspectAdjust(0.005f+c*0.25f, 0.91f-r*0.09f),
                            AnchorMax = AspectAdjust(0.245f+c*0.25f, 0.99f-r*0.09f)
                        }
            }, parent);

            if (itemid != null)
                result.Add(new CuiElement
                {
                    Components =
                {
                    CreateItemImage((int)itemid),
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.025 0.05", AnchorMax = "0.025 0.1",
                        OffsetMin = "0 0", OffsetMax = $"40 40"
                    }
                },
                    Parent = itemPanel
                });

            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = itemid == null ? AspectAdjust(0.03f, 0.1f) : AspectAdjust(0.18f, 0.1f),
                    AnchorMax = AspectAdjust(0.68f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.LowerLeft,
                    Text = name,
                    Color = UiHelper.textColor,
                    Font = UiHelper.regularFont,
                    FontSize = 13
                }
            }, itemPanel);

            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.7f, 0.2f),
                    AnchorMax = AspectAdjust(0.95f, 0.8f)
                },
                Button =
                {
                    Command = command,
                    Color = value ? UiHelper.greenButtonColor : UiHelper.redButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = value ? "ENABLED" : "DISABLED",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, itemPanel);

            r++;
        }

        private void CreateHeaderPanel(ref CuiElementContainer result, string parent, ref int r, int c, string text)
        {
            // Header panel
            string headerPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.005f+c*0.25f, 0.91f-r*0.09f),
                    AnchorMax = AspectAdjust(0.245f+c*0.25f, 0.95f-r*0.09f)
                }
            }, parent);

            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.1f),
                    AnchorMax = AspectAdjust(0.5f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.LowerLeft,
                    Text = text,
                    Color = UiHelper.textColor,
                    Font = UiHelper.boldFont,
                    FontSize = 14
                }
            }, headerPanel);

            r++;
        }

        private void CreateInfoPanel(ref CuiElementContainer result, string parent, ref int r, int c, string text)
        {
            // Base panel
            string itemPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                        {
                            AnchorMin = AspectAdjust(0.005f+c*0.25f, 0.91f-r*0.09f),
                            AnchorMax = AspectAdjust(0.245f+c*0.25f, 0.99f-r*0.09f)
                        }
            }, parent);

            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.03f, 0.1f),
                    AnchorMax = AspectAdjust(0.97f, 0.9f)
                },
                Text =
                {
                    Align = TextAnchor.UpperLeft,
                    Text = text,
                    Color = UiHelper.textColor,
                    Font = UiHelper.regularFont,
                    FontSize = 13
                }
            }, itemPanel);

            r++;
        }

        private void CreateItemEditUI(BasePlayer player, bool create_new = false)
        {
            if (UiHelper.uiUser != player.userID) return;
            CuiElementContainer result = new CuiElementContainer();

            string title = "";
            bool canUseExtras = UiHelper.editingType == LootManager.LootableType.Crate && UiHelper.editLootType != LootableConfig.LootType.BlackList;

            if (UiHelper.editingType.IsOneOf(LootManager.LootableType.Crate))
            {
                BaseLootable lootable = UiHelper.EditorCache.Lootable;
                if (UiHelper.editLootType != LootableConfig.LootType.BlackList)
                {
                    title = create_new ? $"Add new item to {lootable.displayName}" : $"Edit {UiHelper.EditorCache.Shortname} in {lootable.displayName}";
                }
                else
                {
                    title = $"Add new item to Blacklist for {lootable.displayName}";
                }
            }
            else if (UiHelper.editingType == LootManager.LootableType.Quarry || UiHelper.editingType == LootManager.LootableType.Excavator) 
            {
                Quarry dispenser = UiHelper.QuarryEditorCache.quarry;
                title = create_new ? $"Add item to {dispenser.displayName}" : $"Edit {UiHelper.EditorCache.Shortname} in {dispenser.displayName}";
            }
            else if (UiHelper.editingType == LootManager.LootableType.Collectible)
            {
                BaseLootable lootable = UiHelper.CollectibleEditorCache.Collectible;
                title = create_new ? $"Add new item to {lootable.displayName}" : $"Edit {UiHelper.EditorCache.Shortname} in {lootable.displayName}";
            }
            else if (UiHelper.editingType == LootManager.LootableType.StaticLootable)
            {
                var l = UiHelper.StaticLootables.Lootable;
                title = create_new ? $"Add item to {l.PrefabFilter}" : $"Edit {UiHelper.EditorCache.Shortname} in {l.PrefabFilter}";
            }

            //Content panel
            string profilePanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.bgColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.01f, 0.01f, "x"),
                    AnchorMax = AspectAdjust(0.99f, 0.935f)
                }
            }, UiHelper.rootPanelName, UiHelper.itemEditPanelName);

            #region Header

            //Header panel
            string profileHeadPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColorBright
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.005f, 0.94f),
                    AnchorMax = AspectAdjust(0.995f, 0.99f)
                }
            }, profilePanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.01f, 0f),
                    AnchorMax = AspectAdjust(0.5f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleLeft,
                    Text =  title,
                    Color = UiHelper.textColor,
                    FontSize = 16
                }
            }, profileHeadPanel);

            // Save Button
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.85f, 0.2f),
                    AnchorMax = AspectAdjust(0.99f, 0.8f)
                },
                Button =
                {
                    Command = $"loottable.cmd action item_save",
                    Color = UiHelper.greenButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "SAVE",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, profileHeadPanel);

            // Discard Button
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.69f, 0.2f),
                    AnchorMax = AspectAdjust(0.83f, 0.8f)
                },
                Button =
                {
                    Command = UiHelper.editingType == LootManager.LootableType.StaticLootable 
                        ? $"loottable.cmd sledit open {UiHelper.StaticLootables.Lootable?.Uid} {UiHelper.itemEditPanelName}" 
                        : $"loottable.cmd open_edit {UiHelper.currentLootable?.saveName} {UiHelper.itemEditPanelName}",
                    Color = UiHelper.redButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "CANCEL",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, profileHeadPanel);

            // Delete Button
            if (UiHelper.EditorCache.replace_item && (UiHelper.editingType != LootManager.LootableType.Excavator || UiHelper.QuarryEditorCache.excavatorConfig?.items[UiHelper.QuarryEditorCache.collectionId].Count > 1))
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.6f, 0.2f),
                    AnchorMax = AspectAdjust(0.67f, 0.8f)
                },
                Button =
                {
                    Command = $"loottable.cmd action item_del",
                    Color = UiHelper.redButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "DELETE ITEM",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, profileHeadPanel);

            #endregion

            #region Input Fields

            CreateInputPanel(ref result, profilePanel, "Item Shortname", UiHelper.EditorCache.Shortname, 
                "loottable.cmd item_cache item_sname", AspectAdjust(0.005f, 0.85f), AspectAdjust(0.200f, 0.93f),
                hasButton: true, buttonCommand: "loottable.cmd itemselect open", buttonColor: UiHelper.greenButtonColor, buttonText: "SELECT");

            if (UiHelper.editLootType != LootableConfig.LootType.BlackList)
            {
                if (UiHelper.editingType.IsOneOf(LootManager.LootableType.Crate, LootManager.LootableType.Collectible, LootManager.LootableType.StaticLootable))
                {
                    CreateInputPanel(ref result, profilePanel, "Min item amount", UiHelper.EditorCache.item_amount_min.ToString(),
                        "loottable.cmd item_cache item_min", AspectAdjust(0.205f, 0.85f), AspectAdjust(0.300f, 0.93f));
                    CreateInputPanel(ref result, profilePanel, "Max item amount", UiHelper.EditorCache.item_amount_max.ToString(),
                        "loottable.cmd item_cache item_max", AspectAdjust(0.305f, 0.85f), AspectAdjust(0.400f, 0.93f));

                    if (UiHelper.EditorCache.item_category == 0)
                    {
                        CreateInputPanel(ref result, profilePanel, "Drop chance (%)", (UiHelper.EditorCache.item_chance * 100).ToString(),
                            "loottable.cmd item_cache item_proba", AspectAdjust(0.405f, 0.85f), AspectAdjust(0.500f, 0.93f));
                    }
                }
                if (!UiHelper.editingType.IsOneOf(LootManager.LootableType.Excavator))
                {
                    CreateInputPanel(ref result, profilePanel, "Custom name (leave blank for default)", UiHelper.EditorCache.item_name,
                        "loottable.cmd item_cache item_name", AspectAdjust(0.005f, 0.76f), AspectAdjust(0.200f, 0.84f), fontSize: 13, enableResetButton: true, resetValue: "###reset###");
                    CreateInputPanel(ref result, profilePanel, "Item skin id", UiHelper.EditorCache.item_skin.ToString(),
                        "loottable.cmd item_cache item_skin", AspectAdjust(0.205f, 0.76f), AspectAdjust(0.400f, 0.84f));
                }

                if (UiHelper.editingType == LootManager.LootableType.Crate && UiHelper.editLootType != LootableConfig.LootType.BlackList)
                {
                    if (UiHelper.editLootType == LootableConfig.LootType.Custom)
                    {
                        CreateInputPanel(ref result, profilePanel, "Item category", UiHelper.EditorCache.item_category.ToString(),
                            "loottable.cmd item_cache item_cat", AspectAdjust(0.005f, 0.67f), AspectAdjust(0.100f, 0.75f));
                    }

                    CreateButtonPanel(ref result, profilePanel, "Blueprint", "loottable.cmd item_cache bptoggle x", UiHelper.EditorCache.isBlueprint ? "YES" : "NO", UiHelper.EditorCache.isBlueprint ? UiHelper.greenButtonColor : UiHelper.redButtonColor,
                        AspectAdjust(0.105f, 0.67f), AspectAdjust(0.200f, 0.75f));

                    if (UiHelper.EditorCache.ItemDefinition.condition.enabled)
                    {
                        CreateInputPanel(ref result, profilePanel, "Condition min (%)", (UiHelper.EditorCache.condition.min * 100).ToString(),
                        "loottable.cmd item_cache cond_min", AspectAdjust(0.205f, 0.67f), AspectAdjust(0.300f, 0.75f));

                        CreateInputPanel(ref result, profilePanel, "Condition max (%)", (UiHelper.EditorCache.condition.max * 100).ToString(),
                            "loottable.cmd item_cache cond_max", AspectAdjust(0.305f, 0.67f), AspectAdjust(0.400f, 0.75f));
                    }
                }
                
                if (UiHelper.editingType == LootManager.LootableType.Quarry)
                {
                    CreateInputPanel(ref result, profilePanel, "Work required to generate item", UiHelper.EditorCache.work.ToString(),
                        "loottable.cmd item_cache item_work", AspectAdjust(0.205f, 0.85f), AspectAdjust(0.400f, 0.93f));
                }
                if (UiHelper.editingType == LootManager.LootableType.Excavator)
                {
                    CreateInputPanel(ref result, profilePanel, "Amount per diesel barrel", UiHelper.EditorCache.item_amount_min.ToString(),
                        "loottable.cmd item_cache item_minmax", AspectAdjust(0.205f, 0.85f), AspectAdjust(0.400f, 0.93f));
                }
            }
            #endregion

            #region Extras

            if (canUseExtras)
            {
                //Base panel
                string extraPanel = result.Add(new CuiPanel
                {
                    Image = new CuiImageComponent
                    {
                        Color = UiHelper.panelColor
                    },
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.005f, 0.01f),
                        AnchorMax = AspectAdjust(0.7f, 0.5f)
                    }
                }, profilePanel);
                
                // Title
                result.Add(new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.005f, 0.565f),
                        AnchorMax = AspectAdjust(0.7f, 0.65f)
                    },
                    Text =
                    {
                        Align = TextAnchor.LowerLeft,
                        Text =  "Extra items",
                        Color = UiHelper.textColor,
                        FontSize = 15
                    }
                }, profilePanel);

                // Description
                result.Add(new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.005f, 0.51f),
                        AnchorMax = AspectAdjust(0.59f, 0.56f)
                    },
                    Text =
                    {
                        Align = TextAnchor.LowerLeft,
                        Text =  "Extra items are added to the loot container alongside the main item. " +
                                "They can be used for instance to add suitable ammo to a weapon. " +
                                "Extra items do not count to the max item amount of a loot container. " +
                                "Tip: to use a ranged item amount simply enter a range (e.g. 1-2) in the amount field.",
                        Color = UiHelper.textColor,
                        FontSize = 13,
                        Font = UiHelper.regularFont
                    }
                }, profilePanel);

                // Add button
                result.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.6f, 0.51f),
                        AnchorMax = AspectAdjust(0.7f, 0.55f)
                    },
                    Button =
                    {
                        Command = $"loottable.cmd itemselect open_extra",
                        Color = UiHelper.greenButtonColor
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Text = "ADD EXTRA ITEM",
                        Color = UiHelper.buttonTextColor,
                        FontSize = 14
                    }
                }, profilePanel);

                int r = 0; int r_max = 3;
                int c = 0; int c_max = 2;

                Vector2 widgetSize = new Vector2(0.236f, 0.24f);
                Vector2 widgetDist = new Vector2(0.01f, 0.97f);

                foreach(var extra in UiHelper.EditorCache.extras)
                {
                    string itemPanel = result.Add(new CuiPanel
                    {
                        Image = new CuiImageComponent
                        {
                            Color = UiHelper.panelColorBright
                        },
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust((r+1)*widgetDist.x+r*widgetSize.x, widgetDist.y-(c+1)*widgetSize.y-c*(1-widgetDist.y)),
                            AnchorMax = AspectAdjust((r+1)*widgetDist.x+(r+1)*widgetSize.x, widgetDist.y-c*widgetSize.y-c*(1-widgetDist.y))
                        }
                    }, extraPanel);

                    // Image
                    result.Add(new CuiElement
                    {
                        Components =
                        {
                            CreateItemImage(extra.itemid),
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.02 0.05", AnchorMax = "0.02 0.05",
                                OffsetMin = "0 0", OffsetMax = "60 60"
                            }
                        },
                        Parent = itemPanel
                    });

                    // Title
                    result.Add(new CuiLabel
                    {
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust(0.04f, 0.75f),
                            AnchorMax = AspectAdjust(1, 0.95f)
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleLeft,
                            Text =  ItemManager.FindItemDefinition(extra.itemid).displayName.english,
                            Color = "1 1 1 1",
                            FontSize = 12,
                            Font = UiHelper.regularFont
                        }
                    }, itemPanel);

                    // Amount label
                    result.Add(new CuiLabel
                    {
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust(0.01f, 0.1f),
                            AnchorMax = AspectAdjust(0.4f, 0.3f)
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            Text =  "x"+extra.amount.ToString(),
                            Color = "1 1 1 1",
                            FontSize = 11,
                            Font = UiHelper.regularFont
                        }
                    }, itemPanel);

                    // Amount input label
                    result.Add(new CuiLabel
                    {
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust(0.4f, 0.4f),
                            AnchorMax = AspectAdjust(0.7f, 0.7f)
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleLeft,
                            Text =  "Amount:",
                            Color = "1 1 1 1",
                            FontSize = 13,
                            Font = UiHelper.regularFont
                        }
                    }, itemPanel);

                    //Amount input
                    CreateInputPanelSmall(ref result, itemPanel, extra.amount.ToString(), $"loottable.cmd action extra_amount {extra.itemid}",
                        AspectAdjust(0.7f, 0.4f), AspectAdjust(0.95f, 0.7f), 13, 8);

                    // Delete button
                    result.Add(new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust(0.4f, 0.1f),
                            AnchorMax = AspectAdjust(0.95f, 0.3f)
                        },
                        Button =
                        {
                            Command = $"loottable.cmd action extra_del {extra.itemid}",
                            Color = UiHelper.redButtonColor
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            Text = "DELETE",
                            Color = UiHelper.buttonTextColor,
                            FontSize = 13
                        }
                    }, itemPanel);

                    r++;
                    if (r > r_max)
                    {
                        r = 0;
                        c++;
                        if (c > c_max) break;
                    }
                }

                if (UiHelper.EditorCache.extras.Count < 1)
                {
                    // Title
                    result.Add(new CuiLabel
                    {
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust(0f, 0.2f),
                            AnchorMax = AspectAdjust(1f, 1f)
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            Text =  "Pretty empty here, let's add some items!",
                            Color = UiHelper.textColor,
                            FontSize = 15
                        }
                    }, extraPanel);

                    result.Add(new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust(0.4f, 0.45f),
                            AnchorMax = AspectAdjust(0.6f, 0.55f)
                        },
                        Button =
                        {
                            Command = $"loottable.cmd itemselect open_extra",
                            Color = UiHelper.greenButtonColor
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            Text = "ADD EXTRA ITEM",
                            Color = UiHelper.buttonTextColor,
                            FontSize = 14
                        }
                    }, extraPanel);
                }
            }

            #endregion

            #region Preview

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.57f, 0.89f),
                    AnchorMax = AspectAdjust(0.63f, 0.93f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text =  "Preview",
                    Color = UiHelper.textColor,
                    FontSize = 14
                }
            }, profilePanel);

            CreateItemPanel(ref result, profilePanel, UiHelper.EditorCache.CreateItemFromCache(),
                AspectAdjust(0.568f, 0.77f), AspectAdjust(0.632f, 0.89f), image_size: 50);

            #endregion

            #region Create Custom Item

            if (UiHelper.editingType.IsOneOf(LootManager.LootableType.Crate, LootManager.LootableType.Collectible, LootManager.LootableType.StaticLootable) && UiHelper.editLootType != LootableConfig.LootType.BlackList)
            {
                result.Add(new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.5f, 0.68f),
                        AnchorMax = AspectAdjust(0.7f, 0.73f)
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Text =  "Create a Custom Item from this Item",
                        Color = UiHelper.textColor,
                        Font = UiHelper.regularFont,
                        FontSize = 12
                    }
                }, profilePanel);

                result.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.55f, 0.65f),
                        AnchorMax = AspectAdjust(0.65f, 0.69f)
                    },
                    Button =
                    {
                        Command = "loottable.cmd custom from_item",
                        Color = UiHelper.greenButtonColor
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Text = "CREATE",
                        Color = UiHelper.buttonTextColor,
                        FontSize = 14
                    }
                }, profilePanel);
            }

            

            #endregion

            #region Category Sidepanel

            if (UiHelper.editLootType == LootableConfig.LootType.Custom && UiHelper.editingType == LootManager.LootableType.Crate)
            {
                string categoryOverviewPanel = result.Add(new CuiPanel
                {
                    Image = new CuiImageComponent
                    {
                        Color = UiHelper.panelColor
                    },
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.705f, 0.01f),
                        AnchorMax = AspectAdjust(0.995f, 0.93f)
                    }
                }, profilePanel);

                result.Add(new CuiLabel
                {
                    RectTransform =
                {
                    AnchorMin = AspectAdjust(0.1f, 0.95f),
                    AnchorMax = AspectAdjust(0.9f, 1f)
                },
                    Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "Category Overview",
                    Color = UiHelper.textColor,
                    FontSize = 16
                }
                }, categoryOverviewPanel);

                for (int i = 0; i < UiHelper.maxCategories; i++)
                {

                    // Base panel
                    string categoryPanel = result.Add(new CuiPanel
                    {
                        Image = new CuiImageComponent
                        {
                            Color = UiHelper.panelColorBright
                        },
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust(0.05f, 0.82f - i*0.12f),
                            AnchorMax = AspectAdjust(0.95f, 0.93f - i*0.12f)
                        }
                    }, categoryOverviewPanel);


                    // Color panel
                    result.Add(new CuiPanel
                        {
                            Image = new CuiImageComponent
                            {
                                Color = i == 0 ? UiHelper.panelColor : UiHelper.categoryColors[i]
                            },
                            RectTransform =
                            {
                                AnchorMin = AspectAdjust(0.03f, 0.1f),
                                AnchorMax = AspectAdjust(0.2f, 0.9f)
                            }
                    }, categoryPanel);

                    // Name
                    result.Add(new CuiLabel
                        {
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust(0.25f, 0f),
                            AnchorMax = AspectAdjust(0.9f, 0.9f)
                        },
                        Text =
                        {
                            Align = TextAnchor.UpperLeft,
                            Text = $"{i} - {UiHelper.categoryNames[i]}",
                            Color = UiHelper.textColor,
                            FontSize = 16
                        }
                    }, categoryPanel);

                    // Default description
                    if (i == 0)
                    result.Add(new CuiLabel
                    {
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust(0.25f, 0f),
                            AnchorMax = AspectAdjust(0.9f, 0.62f)
                        },
                        Text =
                        {
                            Align = TextAnchor.UpperLeft,
                            Text = "This is the default category, drop chance and drop amount must be configured for each item individually",
                            Color = UiHelper.textColor,
                            FontSize = 11,
                            Font = UiHelper.regularFont
                        }
                    }, categoryPanel);
                }
            }
            #endregion

            CuiHelper.AddUi(player, result);
            if (create_new) CreateItemSelectOverlay(player, updatePages: true);
            return;
        }

        private void CreateInputPanelSmall(ref CuiElementContainer result, string parent, string defaultValue, string command, string anchorMin, string anchorMax, int fontSize = 14, int charLimit = 80, bool disabled = false)
        {
            string shortnamePanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = disabled ? UiHelper.transparentColor : UiHelper.panelColor
                },
                RectTransform =
                    {
                        AnchorMin = anchorMin,
                        AnchorMax = anchorMax
                    }
            }, parent);

            // Input default
            if (disabled)
            result.Add(new CuiLabel
            {
                RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.01f, 0),
                        AnchorMax = AspectAdjust(1, 1)
                    },
                Text =
                    {
                        Align = TextAnchor.MiddleLeft,
                        Text = defaultValue,
                        Color = UiHelper.greyTextColor,
                        FontSize = fontSize-1,
                        Font = UiHelper.regularFont
                    }
            }, shortnamePanel);

            // Input
            else
            result.Add(new CuiElement
            {
                Parent = shortnamePanel,
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        FontSize = fontSize,
                        Align = TextAnchor.MiddleLeft,
                        Command = command,
                        Color = UiHelper.textColor,
                        CharsLimit = charLimit,
                        Text = defaultValue,
                        Font = UiHelper.regularFont
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.01 0", AnchorMax = "1 1",
                        //OffsetMin = "10 0", OffsetMax = "0 0"
                    }
                }
            });
        }

        private void CreateItemSelectOverlay(BasePlayer player, string search = "", bool updatePages = false)
        {
            if (UiHelper.uiUser != player.userID) return;
            CuiElementContainer result = new CuiElementContainer();

            if (updatePages)
            {
                int pages;
                if (UiHelper.Items.cat == 0)
                {
                    pages = CustomItemStorage.GetPages();
                }
                else
                {
                    List<int> currentCat2 = StackManager.itemCategoryList.Values.ToList()[UiHelper.Items.cat];
                    pages = Mathf.CeilToInt(((float)currentCat2.Count) / ((float)UiHelper.Items.itemsPerPage));
                }
                UiHelper.Items.pages = pages;
            }

            bool searching = search != "";
            Vector2 panelSize = new Vector2(900, 600);

            #region Overlay

            string root = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = "0.1 0.1 0.1 0.95",
                },
                RectTransform =
                {
                    AnchorMin = "0 0", AnchorMax = "1 1"
                }
            }, UiHelper.rootPanelName, UiHelper.itemSelectPanelName);

            result.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = "" },
                Button =
                {
                    Color = UiHelper.transparentColor,
                    Close = UiHelper.itemSelectPanelName,
                    Command = "loottable.cmd itemselect closed"
                }
            }, root);

            string basePanel = result.Add(new CuiPanel
            {
                Image =
                {
                    Color = UiHelper.bgColor
                },
                RectTransform =
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5",
                    OffsetMin = $"{panelSize.x*-0.5f} {panelSize.y*-0.5f}",
                    OffsetMax = $"{panelSize.x* 0.5f} {panelSize.y* 0.5f}"
                }
            }, root);

            #endregion

            #region Head

            string headPanel = result.Add(new CuiPanel
            {
                Image =
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = "0.005 0.95",
                    AnchorMax = "0.995 0.99",
                }
            }, basePanel);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.01f, 0f),
                    AnchorMax = AspectAdjust(0.2f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleLeft,
                    Text = "Select item",
                    Color = UiHelper.textColor,
                    FontSize = 14
                }
            }, headPanel);

            // Search panel
            string searchPanel = result.Add(new CuiPanel
            {
                Image =
                {
                    Color = UiHelper.panelColorBright
                },
                RectTransform =
                {
                    AnchorMin = "0.8 0.05",
                    AnchorMax = "0.995 0.95",
                }
            }, headPanel);

            // Input Background
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0f),
                    AnchorMax = AspectAdjust(1f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleLeft,
                    Text = "Search",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 11,
                    Font = UiHelper.regularFont
                }
            }, searchPanel);

            // Search Field
            result.Add(new CuiElement
            {
                Parent = searchPanel,
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        FontSize = 13,
                        Align = TextAnchor.MiddleLeft,
                        Command = "loottable.cmd itemselect search",
                        Color = UiHelper.textColor,
                        CharsLimit = 10,
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.05 0", AnchorMax = "0.95 1"
                    }
                }
            });

            #endregion

            string contentPanel = result.Add(new CuiPanel
            {
                Image =
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = "0.005 0.005",
                    AnchorMax = "0.995 0.94",
                }
            }, basePanel);

            #region Page navigation

            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.005f, 0.01f),
                    AnchorMax = AspectAdjust(0.08f, 0.06f)
                },
                Text =
                {
                    Align = TextAnchor.LowerLeft,
                    Text = $"Page {UiHelper.Items.page+1} of {UiHelper.Items.pages}",
                    Color = UiHelper.textColor,
                    Font = UiHelper.regularFont,
                    FontSize = 13
                }
            }, contentPanel);

            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.085f, 0.01f),
                    AnchorMax = AspectAdjust(0.12f, 0.04f)
                },
                Button =
                {
                    Command = $"loottable.cmd itemselect page false",
                    Color = UiHelper.Items.page-1 >= 0 ? UiHelper.colorBlue : UiHelper.greyButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "<-",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, contentPanel);

            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.125f, 0.01f),
                    AnchorMax = AspectAdjust(0.160f, 0.04f)
                },
                Button =
                {
                    Command = $"loottable.cmd itemselect page true",
                    Color = UiHelper.Items.page+1 < UiHelper.Items.pages ? UiHelper.colorBlue : UiHelper.greyButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "->",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 15
                }
            }, contentPanel);

            #endregion

            #region Tabs

            // Tab panel
            string tabPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.transparentColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.005f, 0.89f),
                    AnchorMax = AspectAdjust(0.995f, 0.94f)
                }
            }, basePanel);

            int i = 0;
            float tabSize = 0.0666f;

            bool extra = UiHelper.EditorCache.itemSelectType == UiHelper.EditorCache.ItemType.Extra;
            //if (extra && UiHelper.Items.cat == 0) UiHelper.Items.cat = 1;

            foreach (var cat in StackManager.itemCategoryList)
            {
                result.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0f+i*tabSize, 0f),
                        AnchorMax = AspectAdjust(tabSize+i*tabSize, 1f)
                    },
                    Button =
                    {
                        Command = $"loottable.cmd itemselect switch_cat {i}",
                        Color = UiHelper.Items.cat == i ? Hex2RGBA("#222222", 1f) : UiHelper.panelColorBright
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Text = cat.Key.ToUpper(),
                        Color = UiHelper.buttonTextColor,
                        FontSize = 9,
                        Font = UiHelper.regularFont
                    }
                }, tabPanel);
                i++;
            }

            #endregion

            CuiHelper.AddUi(player, result);

            CuiElementContainer items = new CuiElementContainer();

            #region Items

            int page = UiHelper.Items.page;
            int items_per_page = UiHelper.Items.itemsPerPage;

            List<int> currentCat = StackManager.itemCategoryList.Values.ToList()[UiHelper.Items.cat];
#if DEBUG
            Puts($"total {currentCat.Count} idx {page * items_per_page} ct {(page * items_per_page + items_per_page < currentCat.Count ? items_per_page : currentCat.Count - page * items_per_page)}");
#endif
            // When changing this also change it in CustomItemStorage
            int r = 0; int r_max = 5;
            int c = 0; int c_max = 10;

            // Custom item list
            if (currentCat.IsEmpty() && !searching)
            {
                if (CustomItemStorage.CustomItems.IsEmpty() || extra)
                {
                    items.Add(new CuiLabel
                    {
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust(0f, 0.4f),
                            AnchorMax = AspectAdjust(1f, 0.5f)
                        },
                        Text =
                        {
                            Align = TextAnchor.UpperCenter,
                            Text =  extra ? "Custom items cannot be used as extra items" : "Custom items of other plugins will show up here. If you don't see your plugin here, try reloading it and open this window again",
                            Color = "0.6 0.6 0.6 1",
                            Font = UiHelper.regularFont,
                            FontSize = 12
                        }
                    }, contentPanel);
                }
                else
                {
                    CustomItemStorage.SortItemList();
                }
                
                string plugin = null;
                float offsetY = 0f;
                
                if (!extra)
                foreach (var ci in CustomItemStorage.GetPage(UiHelper.Items.page))
                {
                    if (plugin != ci.plugin)
                    {
                        if (c != 0) r++;
                        c = 0;

                        items.Add(new CuiLabel
                        {
                            RectTransform =
                            {
                                AnchorMin = AspectAdjust(0.005f, 0.90f-r*0.13f-offsetY),
                                AnchorMax = AspectAdjust(0.4f, 0.94f-r*0.13f-offsetY)
                            },
                            Text =
                            {
                                Align = TextAnchor.LowerLeft,
                                Text =  ci.plugin,
                                Color = UiHelper.textColor,
                                Font = UiHelper.boldFont,
                                FontSize = 12
                            }
                        }, contentPanel);

                        offsetY += 0.05f;
                    }
                    plugin = ci.plugin;

                    ItemDefinition itemdef = ItemManager.FindItemDefinition(ci.itemId);

                    // Base panel
                    string itemPanel = items.Add(new CuiPanel
                    {
                        Image = new CuiImageComponent
                        {
                            Color = UiHelper.panelColor
                        },
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust(0.005f+c*0.09f, 0.82f-r*0.13f-offsetY),
                            AnchorMax = AspectAdjust(0.087f+c*0.09f, 0.94f-r*0.13f-offsetY)
                        }
                    }, contentPanel);

                    items.Add(new CuiElement
                    {
                        Components =
                        {
                            CreateItemImage(ci.itemId, ci.skinId),
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.2 0.35", AnchorMax = "0.2 0.35",
                                OffsetMin = "0 0", OffsetMax = $"40 40"
                            }
                        },
                        Parent = itemPanel
                    });

                    items.Add(new CuiLabel
                    {
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust(0f, 0.05f),
                            AnchorMax = AspectAdjust(1f, 0.3f)
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            Text =  ci.customName ?? itemdef.displayName.english,
                            Color = UiHelper.textColor,
                            Font = UiHelper.regularFont,
                            FontSize = 10
                        }
                    }, itemPanel);

                    items.Add(new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust(0, 0.1f),
                            AnchorMax = AspectAdjust(1, 1)
                        },
                        Button =
                        {
                            Command = $"loottable.cmd itemselect selectcustom {itemdef.shortname} {ci.skinId} {ci.customName ?? string.Empty}",
                            Color = UiHelper.transparentColor
                        },
                        Text =
                        {
                            Text = ""
                        },
                    }, itemPanel);

                    if (ci.CanEdit)
                    items.Add(new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust(0.65f, 0.8f),
                            AnchorMax = AspectAdjust(0.95f, 0.95f)
                        },
                        Button =
                        {
                            Command = $"loottable.cmd custom edit {ci.Uid}",
                            Color = UiHelper.colorYellowBright
                        },
                        Text =
                        {
                            FontSize = 9,
                            Color = "0 0 0 1",
                            Align = TextAnchor.MiddleCenter,
                            Font = UiHelper.regularFont,
                            Text = "EDIT"
                        }
                    }, itemPanel);

                    c++;
                    if (c > c_max)
                    {
                        c = 0;
                        r++;
                        if (r > r_max) break;
                    }
                }
            }

            // Default categories
            else
            {
                List<int> currentPage;

                if (searching)
                    currentPage = StackManager.itemList.FindAll(itm => itm.shortname.Contains(search)).ConvertAll(itm => itm.itemid);
                else
                    currentPage = currentCat.GetRange(page * items_per_page, (page+1) * items_per_page < currentCat.Count ? items_per_page : currentCat.Count - page * items_per_page);

                foreach (var itemid in currentPage)
                {
                    ItemDefinition itemdef = ItemManager.FindItemDefinition(itemid);

                    // Base panel
                    string itemPanel = items.Add(new CuiPanel
                    {
                        Image = new CuiImageComponent
                        {
                            Color = UiHelper.panelColor
                        },
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust(0.005f+c*0.09f, 0.80f-r*0.13f),
                            AnchorMax = AspectAdjust(0.087f+c*0.09f, 0.92f-r*0.13f)
                        }
                    }, contentPanel);

                    items.Add(new CuiElement
                    {
                        Components =
                        {
                            CreateItemImage(itemid),
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.2 0.35", AnchorMax = "0.2 0.35",
                                OffsetMin = "0 0", OffsetMax = $"40 40"
                            }
                        },
                        Parent = itemPanel
                    });

                    items.Add(new CuiLabel
                    {
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust(0f, 0.05f),
                            AnchorMax = AspectAdjust(1f, 0.3f)
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            Text = itemdef.displayName.english,//$"{itemdef.shortname}\n{itemdef.itemid}",
                            Color = UiHelper.textColor,
                            Font = UiHelper.regularFont,
                            FontSize = 10
                        }
                    }, itemPanel);

                    items.Add(new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust(0, 0.1f),
                            AnchorMax = AspectAdjust(1, 1)
                        },
                        Button =
                        {
                            Command = $"loottable.cmd itemselect select {itemdef.shortname}",
                            Color = UiHelper.transparentColor
                        },
                        Text =
                        {
                            Text = ""
                        },
                    }, itemPanel);

                    c++;
                    if (c > c_max)
                    {
                        c = 0;
                        r++;
                        if (r > r_max) break;
                    }
                }
            }

            

            CuiHelper.AddUi(player, items);
            return;
            #endregion

        }

        private void CreateCustomItemOverlay(BasePlayer player)
        {
            if (UiHelper.uiUser != player.userID) return;
            CuiElementContainer result = new CuiElementContainer();

            Vector2 panelSize = new Vector2(450, 250);
            int image_size = 64;

            #region Overlay

            string root = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = "0.1 0.1 0.1 0.95",
                },
                RectTransform =
                {
                    AnchorMin = "0 0", AnchorMax = "1 1"
                }
            }, UiHelper.rootPanelName, UiHelper.customItemPanelName);

            result.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = "" },
                Button =
                {
                    Color = UiHelper.transparentColor,
                    Close = UiHelper.multiplierOverlayName,
                    Command = "loottable.cmd custom close"
                }
            }, root);

            string basePanel = result.Add(new CuiPanel
            {
                Image =
                {
                    Color = UiHelper.bgColor
                },
                RectTransform =
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5",
                    OffsetMin = $"{panelSize.x*-0.5f} {panelSize.y*-0.5f}",
                    OffsetMax = $"{panelSize.x* 0.5f} {panelSize.y* 0.5f}"
                }
            }, root);

            #endregion

            #region Head

            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0, 0.8f),
                    AnchorMax = AspectAdjust(1, 0.95f)
                },
                Text =
                {
                    Align = TextAnchor.UpperCenter,
                    Text = "Edit Custom Item",
                    Color = UiHelper.textColor,
                    FontSize = 16
                }
            }, basePanel);

            #endregion


            CreateInputPanel(ref result, basePanel, "Item Id", CustomItemStorage.editingItem.itemId.ToString(), "loottable.cmd custom itemid", "0.05 0.55", "0.3 0.8");
            CreateInputPanel(ref result, basePanel, "Skin Id", CustomItemStorage.editingItem.skinId.ToString(), "loottable.cmd custom skin", "0.33 0.55", "0.65 0.8");

            string name = CustomItemStorage.editingItem.customName ?? ItemManager.FindItemDefinition(CustomItemStorage.editingItem.itemId)?.displayName.english;
            CreateInputPanel(ref result, basePanel, "Custom Name (empty = default name)", CustomItemStorage.editingItem.customName ?? "", "loottable.cmd custom name", "0.05 0.25", "0.65 0.5",
                enableResetButton: true, resetValue: "###reset###");

            #region Item Preview

            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.7f, 0.65f),
                    AnchorMax = AspectAdjust(0.95f, 0.8f)
                },
                Text =
                {
                    Align = TextAnchor.UpperCenter,
                    Text = "Preview:",
                    Color = UiHelper.textColor,
                    FontSize = 13
                }
            }, basePanel);

            string baseItemPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColorBright
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.7f, 0.25f),
                    AnchorMax = AspectAdjust(0.95f, 0.7f),
                }
            }, basePanel);

            result.Add(new CuiElement
            {
                Components =
                {
                    CreateItemImage(CustomItemStorage.editingItem.itemId, CustomItemStorage.editingItem.skinId),
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.64", AnchorMax =  "0.5 0.64",
                        OffsetMin = $"{image_size*-0.5f} {image_size*-0.5f}", OffsetMax = $"{image_size*0.5f} {image_size*0.5f}"
                    }
                },
                Parent = baseItemPanel
            });

            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0f, 0f),
                    AnchorMax = AspectAdjust(1f, 0.3f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = name,
                    Color = UiHelper.textColor,
                    FontSize = 10,
                    Font = UiHelper.regularFont
                }
            }, baseItemPanel);

            #endregion

            #region Bottom

            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.06f),
                    AnchorMax = AspectAdjust(0.2f, 0.17f)
                },
                Button =
                {
                    Command = $"loottable.cmd custom delete",
                    Color = UiHelper.redButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "DELETE",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 16
                }
            }, basePanel);

            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.23f, 0.06f),
                    AnchorMax = AspectAdjust(0.5f, 0.17f)
                },
                Button =
                {
                    Command = $"loottable.cmd custom close",
                    Color = UiHelper.redButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "CANCEL",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 16
                }
            }, basePanel);

            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.53f, 0.06f),
                    AnchorMax = AspectAdjust(0.95f, 0.17f)
                },
                Button =
                {
                    Command = $"loottable.cmd custom save",
                    Color = UiHelper.greenButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "SAVE",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 16
                }
            }, basePanel);

            #endregion

            CuiHelper.AddUi(player, result);
        }

        private void CreateMultiplierOverlay(BasePlayer player)
        {
            if (UiHelper.uiUser != player.userID) return;
            CuiElementContainer result = new CuiElementContainer();

            Vector2 panelSize = new Vector2(250, 150);

            #region Overlay

            string root = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = "0.1 0.1 0.1 0.95",
                },
                RectTransform =
                {
                    AnchorMin = "0 0", AnchorMax = "1 1"
                }
            }, UiHelper.rootPanelName, UiHelper.multiplierOverlayName);

            result.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = "" },
                Button =
                {
                    Color = UiHelper.transparentColor,
                    Close = UiHelper.multiplierOverlayName,
                    Command = "loottable.cmd multiplier close"
                }
            }, root);

            string basePanel = result.Add(new CuiPanel
            {
                Image =
                {
                    Color = UiHelper.bgColor
                },
                RectTransform =
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5",
                    OffsetMin = $"{panelSize.x*-0.5f} {panelSize.y*-0.5f}",
                    OffsetMax = $"{panelSize.x* 0.5f} {panelSize.y* 0.5f}"
                }
            }, root);

            #endregion

            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0, 0.8f),
                    AnchorMax = AspectAdjust(1, 0.95f)
                },
                Text =
                {
                    Align = TextAnchor.UpperCenter,
                    Text = "Multiply items",
                    Color = UiHelper.textColor,
                    FontSize = 16
                }
            }, basePanel);

            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.6f),
                    AnchorMax = AspectAdjust(0.95f, 0.8f)
                },
                Text =
                {
                    Align = TextAnchor.UpperLeft,
                    Text = "When you click apply, the item amount of every item will be multiplied with the entered value.",
                    Color = UiHelper.textColor,
                    FontSize = 11,
                    Font = UiHelper.regularFont
                }
            }, basePanel);

            CreateInputPanelSmall(ref result, basePanel, UiHelper.Mutliplier.multi.ToString("N1"), "loottable.cmd multiplier set", "0.05 0.35", "0.95 0.55");

            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.08f),
                    AnchorMax = AspectAdjust(0.95f, 0.3f)
                },
                Button =
                {
                    Command = $"loottable.cmd multiplier apply",
                    Color = UiHelper.redButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "APPLY",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 16
                }
            }, basePanel);

            CuiHelper.AddUi(player, result);
        }

        private void CreateInputPanel(ref CuiElementContainer result, string parent, string fieldName, string defaultValue,
            string command, string anchorMin, string anchorMax, bool disabled = false, int fontSize = 14, bool enableResetButton = false,
            string resetValue = "", bool hasButton = false, string buttonCommand = "", string buttonColor = UiHelper.greyButtonColor, string buttonText = "")
        {
            string shortnamePanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                    {
                        AnchorMin = anchorMin,
                        AnchorMax = anchorMax
                    }
            }, parent);

            // Title
            result.Add(new CuiLabel
            {
                RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.05f, 0.6f),
                        AnchorMax = AspectAdjust(0.95f, 1f)
                    },
                Text =
                    {
                        Align = TextAnchor.MiddleLeft,
                        Text = fieldName,
                        Color = UiHelper.textColor,
                        FontSize = fontSize
                    }
            }, shortnamePanel);

            // Input background
            string inputPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColor
                },
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.05f, 0.1f),
                    AnchorMax = AspectAdjust(0.95f, 0.5f)
                }
            }, shortnamePanel);

            // Input
            if (!disabled)
            result.Add(new CuiElement
            {
                Parent = inputPanel,
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        FontSize = fontSize,
                        Align = TextAnchor.MiddleLeft,
                        Command = command,
                        Color = UiHelper.textColor,
                        CharsLimit = 80,
                        Text = defaultValue,
                        Font = UiHelper.regularFont
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.05 0", AnchorMax = "1 1",
                        //OffsetMin = "10 0", OffsetMax = "0 0"
                    }
                }
            });

            if (enableResetButton)
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.75f, 0.1f),
                    AnchorMax = AspectAdjust(0.95f, 0.5f)
                },
                Button =
                {
                    Command = $"{command} {resetValue}",
                    Color = UiHelper.greyButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "RESET",
                    Color = UiHelper.buttonTextColor,
                    FontSize = fontSize-1
                }
            }, shortnamePanel);

            if (hasButton)
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.75f, 0.1f),
                    AnchorMax = AspectAdjust(0.95f, 0.5f)
                },
                Button =
                {
                    Command = buttonCommand,
                    Color = buttonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = buttonText,
                    Color = UiHelper.buttonTextColor,
                    FontSize = fontSize-1
                }
            }, shortnamePanel);
        }

        private void CreateItemPanel(ref CuiElementContainer result, string parent, LootItem item, string anchorMin, string anchorMax, bool enableEdit = false, string customCommand = "", float image_size = 42)
        {
            string itemText = "";

            if (UiHelper.editingType.IsOneOf(LootManager.LootableType.Crate, LootManager.LootableType.Collectible, LootManager.LootableType.StaticLootable))
            {
                if (item.category != 0)
                    itemText = $"x{item.amount.ToString()}";
                else
                    itemText = $"x{item.amount.ToString()} {item.chance * 100f}%";
                
                if (UiHelper.editLootType == LootableConfig.LootType.BlackList) itemText = "";
            }
            if (UiHelper.editingType == LootManager.LootableType.Quarry)
            {
                itemText = $"{item.work}w";
            }
            if (UiHelper.editingType == LootManager.LootableType.Excavator)
            {
                itemText = $"~{item.amount.min.ToString("N0", NUMBER_FORMAT)}";
            }

            // Base panel
            string itemListItemPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = item.HasCategory() ? UiHelper.categoryColors[item.category] : UiHelper.panelColorBright
                },
                RectTransform =
                    {
                        AnchorMin = anchorMin,
                        AnchorMax = anchorMax,
                    }
            }, parent);

            if (item.isBlueprint)
            {
                result.Add(new CuiElement
                {
                    Components =
                    {
                        CreateItemImage(-996920608),
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.64", AnchorMax =  "0.5 0.64",
                            OffsetMin = $"{image_size*-0.5f} {image_size*-0.5f}", OffsetMax = $"{image_size*0.5f} {image_size*0.5f}"
                        }
                    },
                    Parent = itemListItemPanel
                });
            }

            result.Add(new CuiElement
            {
                Components =
                {
                    CreateItemImage(item.itemid, item.skin),
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.64", AnchorMax =  "0.5 0.64",
                        OffsetMin = $"{image_size*-0.5f} {image_size*-0.5f}", OffsetMax = $"{image_size*0.5f} {image_size*0.5f}"
                    }
                },
                Parent = itemListItemPanel
            });

            if (item.HasExtras)
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.07f, 0.7f),
                    AnchorMax = AspectAdjust(1f, 1f)
                },
                Text =
                {
                    Align = TextAnchor.UpperLeft,
                    Text = String.Join(" ", Enumerable.Repeat('\u2022', item.extras.Count)),
                    Color = UiHelper.textColor,
                    FontSize = 15,
                    Font = UiHelper.regularFont
                }
            }, itemListItemPanel);

            if (item.HasCondition && !item.isBlueprint && UiHelper.editingType == LootManager.LootableType.Crate && UiHelper.editLootType != LootableConfig.LootType.BlackList)
            {
                if (item.condition.max < 0.98f)
                {
                    result.Add(new CuiPanel
                    {
                        Image = new CuiImageComponent
                        {
                            Color = "0.16 0.16 0.16 1",
                        },
                        RectTransform =
                        {
                            AnchorMin = AspectAdjust(0.05f, 0.05f),
                            AnchorMax = AspectAdjust(0.1f, 0.95f),
                        }
                    }, itemListItemPanel);
                }
                
                if (item.condition.max > item.condition.min)
                {
                    result.Add(new CuiPanel
                    {
                        Image = new CuiImageComponent
                        {
                            Color = Hex2RGBA("#8ac926", 0.5f),
                        },
                            RectTransform =
                        {
                            AnchorMin = AspectAdjust(0.05f, 0.05f),
                            AnchorMax = AspectAdjust(0.1f, Mathf.Lerp(0.05f, 0.95f, item.condition.max)),
                        }
                    }, itemListItemPanel);
                }

                result.Add(new CuiPanel
                {
                    Image = new CuiImageComponent
                    {
                        Color = Hex2RGBA("#8ac926", 1f),
                    },
                    RectTransform =
                    {
                        AnchorMin = AspectAdjust(0.05f, 0.05f),
                        AnchorMax = AspectAdjust(0.1f, Mathf.Lerp(0.05f, 0.95f, item.condition.min)),
                    }
                }, itemListItemPanel);
            }

            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0f, 0f),
                    AnchorMax = AspectAdjust(1f, 0.3f)
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = itemText,
                    Color = UiHelper.textColor,
                    FontSize = 10,
                    Font = UiHelper.regularFont
                }
            }, itemListItemPanel);

            if (enableEdit)
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0, 0),
                    AnchorMax = AspectAdjust(1, 1)
                },
                Button =
                {
                    Command = customCommand == "" ? $"loottable.cmd action item_edit {item.SkinItemId}" : customCommand,
                    Color = UiHelper.transparentColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 12
                }
            }, itemListItemPanel);
        }

        private void CreateItemPanel(ref CuiElementContainer result, string parent, BlacklistItem item, string anchorMin, string anchorMax)
        {
            // Base panel
            string itemListItemPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.panelColorBright
                },
                RectTransform =
                    {
                        AnchorMin = anchorMin,
                        AnchorMax = anchorMax,
                    }
            }, parent);

            result.Add(new CuiElement
            {
                Components =
                {
                    CreateItemImage(item.itemid),
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1 0.32", AnchorMax = "0.9 0.98",
                    }
                },
                Parent = itemListItemPanel
            });

            result.Add(new CuiButton
                {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.1f, 0.05f),
                    AnchorMax = AspectAdjust(0.9f, 0.3f)
                },
                Button =
                {
                    Command = $"loottable.cmd action item_del {item.itemid}",
                    Color = UiHelper.redButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "DELETE",
                    Color = UiHelper.buttonTextColor,
                    FontSize = 12
                }
            }, itemListItemPanel);
        }

        private void DrawHeaderExtensions(CuiElementContainer result, BasePlayer player)
        {
            if (result == null)
            {
                result = new CuiElementContainer();
            }

            string buttonPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = UiHelper.transparentColor
                },
                RectTransform =
                {
                    AnchorMin = "0.4 0",
                    AnchorMax = "0.9 1"
                }
            }, UiHelper.rootHeaderPanelName, UiHelper.rootHeaderButtonPanelName);

            // Refresh label
            result.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "0.73 0.98"
                },
                Text =
                {
                    Text = "Refresh loot when closing\n(only recommended for testing, might increase entity count)",
                    Align = TextAnchor.MiddleRight,
                    Color = UiHelper.textColor,
                    FontSize = 12,
                    Font = UiHelper.regularFont
                }
            }, buttonPanel);

            // Refresh button
            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = AspectAdjust(0.75f, 0.1f),
                    AnchorMax = AspectAdjust(1f, 0.81f)
                },
                Text =
                {
                    Text = Flags.RefreshLootOnExit ? "ENABLED" : "DISABLED",
                    Align = TextAnchor.MiddleCenter,
                    Color = UiHelper.buttonTextColor,
                    FontSize = 16
                },
                Button =
                {
                    Command = "loottable.cmd togglerefresh",
                    Color = Flags.RefreshLootOnExit ? UiHelper.greenButtonColor : UiHelper.redButtonColor
                }
            }, buttonPanel);

            if (player != null)
            {
                CuiHelper.AddUi(player, result);
            }
        }

        private static void DestroyUI(BasePlayer player, string name = "")
        {
            if (UiHelper.uiUser != player.userID)
                return;

            if (name == "")
            {
                UiHelper.uiUser = 0;
                UiHelper.EditorCache.Clear();
                CuiHelper.DestroyUi(player, UiHelper.rootPanelName);
                UiHelper.UnfreezePlayer(player);
            }
            else
            {
                CuiHelper.DestroyUi(player, name);
            }
        }

        #endregion

        #region Loot Manager

        public static class LootManager
        {
            public enum LootableType { Crate, Quarry, Excavator, NpcCorpse, Collectible, Generic, StaticLootable };

            public static Dictionary<int, BaseLootable> baseLootableCache { get; private set; } = new Dictionary<int, BaseLootable>();

            public static Dictionary<int, LootableConfig> crateConfigCache { get; private set; } = new Dictionary<int, LootableConfig>();

            public static Dictionary<int, QuarryConfig> quarryConfigCache { get; private set; } = new Dictionary<int, QuarryConfig>();

            public static ExcavatorConfig excavatorConfigCache { get; private set; } = new ExcavatorConfig();

            public static Dictionary<int, CollectibleConfig> collectibleConfigCache { get; private set; } = new Dictionary<int, CollectibleConfig>();

            private static Dictionary<int, List<ulong>> npcCache = new Dictionary<int, List<ulong>>();

            public static Dictionary<int, LootableConfig> defaultConfigCache { get; private set; } = new Dictionary<int, LootableConfig>();

            #region Lootable List

            private static readonly List<NpcCorpse> npcList = new List<NpcCorpse>
            {
                new NpcCorpse(47, new HashSet<uint>{ 
                    StringPool.Get("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_any.prefab"),
                    StringPool.Get("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_lr300.prefab"),
                    StringPool.Get("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_mp5.prefab"),
                    StringPool.Get("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_pistol.prefab"),
                    StringPool.Get("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_shotgun.prefab"),
                }, null, "npc_militunnel", "Military Tunnel NPC", defaultConfig: "N0TnhbRp"),

                new NpcCorpse(52, new HashSet<uint>{
                    StringPool.Get("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_cargo.prefab"),
                    StringPool.Get("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_cargo_turret_any.prefab"),
                    StringPool.Get("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_cargo_turret_lr300.prefab"),
                }, null, "npc_cargoship", "Cargo Ship NPC", defaultConfig: "Dw5cWXWM"),
                
                new NpcCorpse(54, new HashSet<uint>{
                    StringPool.Get("assets/rust.ai/agents/npcplayer/humannpc/tunneldweller/npc_tunneldwellerspawned.prefab"),
                    StringPool.Get("assets/rust.ai/agents/npcplayer/humannpc/tunneldweller/npc_tunneldweller.prefab")
                }, null, "npc_tunneldweller", "Tunneldweller NPC", defaultConfig: "D7v6ddn9"),

                new NpcCorpse(51, new HashSet<uint>{
                    StringPool.Get("assets/prefabs/npc/scarecrow/scarecrow.prefab"),
                    StringPool.Get("assets/prefabs/npc/scarecrow/scarecrow_dungeon.prefab"),
                    StringPool.Get("assets/prefabs/npc/scarecrow/scarecrow_dungeonnoroam.prefab"),
                }, null, "npc_scarecrow", "Scarecrow NPC", defaultConfig: "Uc9CbkH2"),

                new NpcCorpse(46, StringPool.Get("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_excavator.prefab"), null, "npc_excavator", "Excavator NPC", defaultConfig: "XpRTM1aJ"),
                new NpcCorpse(53, StringPool.Get("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_heavy.prefab"), null, "npc_heavy", "Heavy Scientist NPC", defaultConfig: "0k912dhE"),
                new NpcCorpse(48, StringPool.Get("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_oilrig.prefab"), null, "npc_oilrig", "Oil Rig NPC", defaultConfig: "rD8TYfpw"),
                new NpcCorpse(49, StringPool.Get("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_ch47_gunner.prefab"), null, "npc_chinook", "CH47 NPC"),
                new NpcCorpse(50, StringPool.Get("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_junkpile_pistol.prefab"), null, "npc_junkpile", "Junkpile NPC", defaultConfig: "M4thL4H8"),
                new NpcCorpse(55, StringPool.Get("assets/rust.ai/agents/npcplayer/humannpc/underwaterdweller/npc_underwaterdweller.prefab"), null, "npc_underwaterdweller", "Underwaterdweller NPC", defaultConfig: "kUgd4aL9"),
                new NpcCorpse(58, StringPool.Get("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_roamtethered.prefab"), null, "npc_desert", "Desert Base NPC", defaultConfig: "Jzhcqbte"),
                new NpcCorpse(101, StringPool.Get("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_roam_nvg_variant.prefab"), null, "npc_missilesilo_nvg", "NVG Missile Silo NPC"),

                new NpcCorpse(103, StringPool.Get("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_roam.prefab"), "missile_silo", "npc_missilesilo", "Missile Silo NPC"),
                new NpcCorpse(56, StringPool.Get("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_patrol.prefab"), "trainyard", "npc_trainyard", "Train Yard NPC", defaultConfig: "uArXCFye"),
                new NpcCorpse(57, StringPool.Get("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_patrol.prefab"), "airfield", "npc_airfield", "Airfield NPC", defaultConfig: "wuWVamUF"),
                new NpcCorpse(102, StringPool.Get("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_patrol.prefab"), "launch_site", "npc_launchsite", "Launch Site NPC"),

                new NpcCorpse(59, new HashSet<uint>{
                    StringPool.Get("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_patrol.prefab"),
                    StringPool.Get("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_roam.prefab"),
                }, "arctic_research_base", "npc_arctic", "Arctic Base NPC", defaultConfig: "1XJiXGR7"),
                
            };

            private static readonly List<Crate> crateList = new List<Crate>
            {
                //Normal
                new Crate(60, new string[] {"assets/bundled/prefabs/radtown/crate_basic.prefab"}, "crate_basic", "Basic Crate", "https://cdn.discordapp.com/attachments/901242337257193502/984178403752169554/crate_basic0.png",
                    defaultConfig: "pK6mg7rr"),
                new Crate(1, new string[] {"assets/bundled/prefabs/radtown/crate_normal_2.prefab" }, "crate_normal", "Normal Crate", "https://files.facepunch.com/rust/item/cratecostume_512.png",
                    defaultConfig: "iJV3q75X"),
                new Crate(2, new string[] {"assets/bundled/prefabs/radtown/crate_normal.prefab" }, "crate_military", "Military Crate", "https://cdn.discordapp.com/attachments/901242337257193502/968488126915936266/dm_t20.png",
                    defaultConfig: "wNwBm3rG"),
                new Crate(3, new string[] {"assets/bundled/prefabs/radtown/crate_elite.prefab" }, "crate_elite", "Elite Crate", "https://cdn.discordapp.com/attachments/901242337257193502/968487021100281886/crate_t30.png",
                    defaultConfig: "uhZNzFRQ"),

                //Invisible
                new Crate(84, new string[] {"assets/bundled/prefabs/modding/lootables/invisible/invisible_lootable_prefabs/invisible_crate_basic.prefab"}, "crate_basic_invisible",
                    "Invisible Basic Crate", "https://cdn.discordapp.com/attachments/901242337257193502/984178403752169554/crate_basic0.png",
                    defaultConfig: "pK6mg7rr"),
                new Crate(87, new string[] {"assets/bundled/prefabs/modding/lootables/invisible/invisible_lootable_prefabs/invisible_crate_normal_2.prefab" }, "crate_normal_invisible", 
                    "Invisible Normal Crate", "https://files.facepunch.com/rust/item/cratecostume_512.png",
                    defaultConfig: "iJV3q75X"),
                new Crate(86, new string[] {"assets/bundled/prefabs/modding/lootables/invisible/invisible_lootable_prefabs/invisible_crate_normal.prefab" }, "crate_military_invisible", 
                    "Invisible Military Crate", "https://cdn.discordapp.com/attachments/901242337257193502/968488126915936266/dm_t20.png",
                    defaultConfig: "wNwBm3rG"),
                new Crate(85, new string[] {"assets/bundled/prefabs/modding/lootables/invisible/invisible_lootable_prefabs/invisible_crate_elite.prefab" }, "crate_elite_invisible", 
                    "Invisible Elite Crate", "https://cdn.discordapp.com/attachments/901242337257193502/968487021100281886/crate_t30.png",
                    defaultConfig: "uhZNzFRQ"),
                new Crate(88, new string[] {"assets/bundled/prefabs/modding/lootables/invisible/invisible_lootable_prefabs/invisible_crate_normal_2_food.prefab" }, "crate_food_invisible",
                    "Invisible Food Crate", "https://cdn.discordapp.com/attachments/901242337257193502/984178592332279838/crate_food0.png",
                    defaultConfig: "wpKKFfk8"),
                new Crate(89, new string[] {"assets/bundled/prefabs/modding/lootables/invisible/invisible_lootable_prefabs/invisible_crate_normal_2_medical.prefab" }, "crate_medical_invisible",
                    "Invisible Medical Crate", "https://cdn.discordapp.com/attachments/901242337257193502/984178638792556574/crate_medical0.png",
                    defaultConfig: "Zzmvc8Qv"),
                new Crate(90, new string[] {"assets/bundled/prefabs/modding/lootables/invisible/invisible_lootable_prefabs/invisible_crate_tools.prefab" }, "crate_tools_invisible",
                    "Invisible Tool Crate", "https://cdn.discordapp.com/attachments/901242337257193502/984178507556986951/crate_tools0.png",
                    defaultConfig: "iTpemWGA"),
                new Crate(91, new string[] { "assets/bundled/prefabs/modding/lootables/invisible/invisible_lootable_prefabs/invisible_foodbox.prefab" }, "crate_food_2_invisible",
                    "Invisible Food Box", "https://cdn.discordapp.com/attachments/901242337257193502/984178852232306778/crate_food_20.png",
                    defaultConfig: "kPMyegdY"),
                new Crate(92, new string[] { "assets/bundled/prefabs/modding/lootables/invisible/invisible_lootable_prefabs/invisible_vehicle_parts.prefab" }, "crate_vehicle_parts_invisible",
                    "Invisible Vehicle Parts Crate", "https://cdn.discordapp.com/attachments/901242337257193502/968487020601163806/vp0.png",
                    defaultConfig: "0BWnWbqK"),

                //Underwater
                new Crate(4, new string[] {"assets/bundled/prefabs/radtown/underwater_labs/crate_normal_2.prefab" }, "crate_basic_underwater", "Underwater Crate", "https://cdn.discordapp.com/attachments/901242337257193502/968487019938471977/normal_uw0.png",
                    defaultConfig: "sFtPTBGM"),
                new Crate(5, new string[] {"assets/bundled/prefabs/radtown/underwater_labs/crate_normal.prefab" }, "crate_military_underwater", "Underwater Military Crate", "https://cdn.discordapp.com/attachments/901242337257193502/968487019594522665/military_uw0.png",
                    defaultConfig: "v1ZuJ4HS"),
                new Crate(6, new string[] {"assets/bundled/prefabs/radtown/underwater_labs/crate_elite.prefab" }, "crate_elite_underwater", "Underwater Elite Crate", "https://cdn.discordapp.com/attachments/901242337257193502/968487021100281886/crate_t30.png",
                    defaultConfig: "qBWGLy30"),
                new Crate(7, new string[] {"assets/bundled/prefabs/radtown/underwater_labs/crate_ammunition.prefab" }, "crate_ammo_underwater", "Underwater Ammo Crate", "https://cdn.discordapp.com/attachments/901242337257193502/968487020873805844/ammo_uw0.png",
                    defaultConfig: "V6wjiZjh"),
                new Crate(8, new string[] {"assets/bundled/prefabs/radtown/underwater_labs/crate_food_1.prefab", "assets/bundled/prefabs/radtown/underwater_labs/crate_food_2.prefab"},
                    "crate_food_underwater", "Underwater Food Crate", "https://cdn.discordapp.com/attachments/901242337257193502/968487021414866994/food_uw0.png",
                    defaultConfig: "dhtjm8Es"),
                new Crate(9, new string[] {"assets/bundled/prefabs/radtown/underwater_labs/crate_fuel.prefab" }, "crate_fuel_underwater", "Underwater Fuel Crate", "https://cdn.discordapp.com/attachments/901242337257193502/968487021725253714/fuel_uw0.png",
                    defaultConfig: "bpgUnZbF"),
                new Crate(10, new string[] {"assets/bundled/prefabs/radtown/underwater_labs/crate_medical.prefab" }, "crate_medical_underwater", "Underwater Medical Crate", "https://cdn.discordapp.com/attachments/901242337257193502/968487022069170186/meds_uw0.png",
                    defaultConfig: "0yDE0uSG"),
                new Crate(11, new string[] {"assets/bundled/prefabs/radtown/underwater_labs/crate_tools.prefab" }, "crate_tools_underwater", "Underwater Tool Crate", "https://cdn.discordapp.com/attachments/901242337257193502/984178507556986951/crate_tools0.png",
                    defaultConfig: "JgDdqU3w"),
                new Crate(26, new string[] {"assets/bundled/prefabs/radtown/underwater_labs/tech_parts_1.prefab",
                                            "assets/bundled/prefabs/radtown/underwater_labs/tech_parts_2.prefab"}, "crate_techparts_underwater", "Underwater Tech Parts Crate", "https://cdn.discordapp.com/attachments/901242337257193502/968487020286595122/techparts_uw0.png",
                    defaultConfig: "DDjwh69R"),
                new Crate(27, new string[] {"assets/bundled/prefabs/radtown/underwater_labs/vehicle_parts.prefab" }, "crate_vehicle_parts_underwater", "Underwater Vehicle Parts Crate", "https://cdn.discordapp.com/attachments/901242337257193502/968487020601163806/vp0.png",
                    defaultConfig: "2CpfCTLR"),

                //Other
                new Crate(12, new string[] {"assets/bundled/prefabs/radtown/crate_mine.prefab" }, "crate_mine", "Mine Crate", "https://files.facepunch.com/rust/item/cratecostume_512.png",
                    defaultConfig: "P48CD5r4"),
                new Crate(13, new string[] {"assets/bundled/prefabs/radtown/crate_normal_2_food.prefab" }, "crate_food", "Food Crate", "https://cdn.discordapp.com/attachments/901242337257193502/984178592332279838/crate_food0.png",
                    defaultConfig: "wpKKFfk8"),
                new Crate(14, new string[] {"assets/bundled/prefabs/radtown/crate_normal_2_medical.prefab" }, "crate_medical", "Medical Crate", "https://cdn.discordapp.com/attachments/901242337257193502/984178638792556574/crate_medical0.png",
                    defaultConfig: "Zzmvc8Qv"),
                new Crate(15, new string[] {"assets/bundled/prefabs/radtown/crate_tools.prefab" }, "crate_tools", "Tool Crate", "https://cdn.discordapp.com/attachments/901242337257193502/984178507556986951/crate_tools0.png",
                    defaultConfig: "iTpemWGA"),
                new Crate(24, new string[] {"assets/bundled/prefabs/radtown/foodbox.prefab", "assets/bundled/prefabs/autospawn/resource/loot/trash-pile-1.prefab" }, "crate_food_2", "Food Box", "https://cdn.discordapp.com/attachments/901242337257193502/984178852232306778/crate_food_20.png",
                    defaultConfig: "kPMyegdY"),
                new Crate(25, new string[] {"assets/bundled/prefabs/radtown/minecart.prefab" }, "crate_minecart", "Minecart", "https://cdn.discordapp.com/attachments/901242337257193502/984178906288513085/minecart0.png",
                    defaultConfig: "r6Cr3kFW"),
                new Crate(28, new string[] {"assets/bundled/prefabs/radtown/vehicle_parts.prefab" }, "crate_vehicle_parts", "Vehicle Parts Crate", "https://cdn.discordapp.com/attachments/901242337257193502/968487020601163806/vp0.png",
                    defaultConfig: "0BWnWbqK"),

                // Diving
                new Crate(16, new string[] {"assets/bundled/prefabs/radtown/crate_underwater_basic.prefab" }, "crate_underwater_basic", "Basic Underwater Crate", "https://cdn.discordapp.com/attachments/901242337257193502/984178403752169554/crate_basic0.png",
                    defaultConfig: "mvsKr0WJ"),
                new Crate(17, new string[] {"assets/bundled/prefabs/radtown/crate_underwater_advanced.prefab" }, "crate_underwater_advanced", "Advanced Underwater Crate", "https://cdn.discordapp.com/attachments/901242337257193502/968487884497776700/adv_underwater0.png",
                    defaultConfig: "ntn5ntkr"),

                // Special
                new Crate(18, new string[] {"assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab" }, "crate_hackable", "Hackable Crate", "https://cdn.discordapp.com/attachments/901242337257193502/984179057245683772/crate_hackable0.png", 36, 18,
                    defaultConfig: "h8gAqQNt"),
                new Crate(19, new string[] {"assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate_oilrig.prefab" }, "crate_hackable_oilrig", "Oilrig Hackable Crate", "https://cdn.discordapp.com/attachments/901242337257193502/984179057245683772/crate_hackable0.png", 36, 18,
                    defaultConfig: "zMNfjt34"),
                new Crate(20, new string[] {"assets/prefabs/npc/m2bradley/bradley_crate.prefab" }, "crate_apc", "APC Crate", "https://cdn.discordapp.com/attachments/901242337257193502/968487021100281886/crate_t30.png", defaultSlots: 12,
                    defaultConfig: "yr6HcY3M"),
                new Crate(21, new string[] {"assets/prefabs/npc/patrol helicopter/heli_crate.prefab" }, "crate_heli", "Helicopter Crate", "https://cdn.discordapp.com/attachments/901242337257193502/968488126915936266/dm_t20.png",
                    defaultConfig: "yjauqcKJ"),
                new Crate(39, new string[] {"assets/prefabs/misc/supply drop/supply_drop.prefab" }, "crate_supplydrop", "Supply Drop", "https://cdn.discordapp.com/attachments/901242337257193502/968488031810097232/spl0.png", 18, 18,
                    defaultConfig: "GdXHFJWT"),

                // Barrels
                new Crate(22, new string[] {"assets/bundled/prefabs/radtown/loot_barrel_1.prefab","assets/bundled/prefabs/autospawn/resource/loot/loot-barrel-2.prefab",
                                            "assets/bundled/prefabs/radtown/loot_barrel_2.prefab", "assets/bundled/prefabs/autospawn/resource/loot/loot-barrel-1.prefab"},
                                            "barrel", "Loot Barrel", "https://cdn.discordapp.com/attachments/901242337257193502/984179217778503760/barrel0.png", defaultConfig: "u3Sqt4DZ"),
                new Crate(23, new string[] {"assets/bundled/prefabs/radtown/oil_barrel.prefab" }, "barrel_oil", "Oil Barrel", "https://cdn.discordapp.com/attachments/901242337257193502/984179140645257306/barrel_oil0.png",
                    defaultConfig: "JmXP8bf0"),

                // Roadsigns
                new Crate(40, new string[] {"assets/content/props/roadsigns/roadsign1.prefab", "assets/content/props/roadsigns/roadsign2.prefab",
                                            "assets/content/props/roadsigns/roadsign3.prefab", "assets/content/props/roadsigns/roadsign4.prefab",
                                            "assets/content/props/roadsigns/roadsign5.prefab", "assets/content/props/roadsigns/roadsign6.prefab",
                                            "assets/content/props/roadsigns/roadsign7.prefab", "assets/content/props/roadsigns/roadsign8.prefab",
                                            "assets/content/props/roadsigns/roadsign9.prefab" }, "roadsign", "Road Sign", "https://static.wikia.nocookie.net/play-rust/images/a/a5/Road_Signs_icon.png/revision/latest/scale-to-width-down/350",
                    defaultConfig: "ALUMaC0P"),

                // Deathmatch
                new Crate(29, new string[] {"assets/bundled/prefabs/radtown/dmloot/dm ammo.prefab" }, "dm_ammo", "DM Ammo Crate", "https://cdn.discordapp.com/attachments/901242337257193502/968488536099676230/dm_ammo0.png"),
                new Crate(30, new string[] {"assets/bundled/prefabs/radtown/dmloot/dm c4.prefab" }, "dm_c4", "DM C4 Crate", "https://cdn.discordapp.com/attachments/901242337257193502/968488535755722752/dm_c40.png"),
                new Crate(31, new string[] {"assets/bundled/prefabs/radtown/dmloot/dm construction resources.prefab" }, "dm_construction_resources", "DM Construction Res. Crate"),
                new Crate(32, new string[] {"assets/bundled/prefabs/radtown/dmloot/dm construction tools.prefab" }, "dm_construction_tools", "DM Construction Tools Crate"),
                new Crate(33, new string[] {"assets/bundled/prefabs/radtown/dmloot/dm food.prefab" }, "dm_food", "DM Food Crate", "https://cdn.discordapp.com/attachments/901242337257193502/984178592332279838/crate_food0.png"),
                new Crate(34, new string[] {"assets/bundled/prefabs/radtown/dmloot/dm medical.prefab" }, "dm_medical", "DM Medical Crate", "https://cdn.discordapp.com/attachments/901242337257193502/984178638792556574/crate_medical0.png"),
                new Crate(35, new string[] {"assets/bundled/prefabs/radtown/dmloot/dm res.prefab" }, "dm_resources", "DM Resources Crate"),
                new Crate(36, new string[] {"assets/bundled/prefabs/radtown/dmloot/dm tier1 lootbox.prefab" }, "dm_tier1", "DM Tier 1 Crate"),
                new Crate(37, new string[] {"assets/bundled/prefabs/radtown/dmloot/dm tier2 lootbox.prefab" }, "dm_tier2", "DM Tier 2 Crate", "https://cdn.discordapp.com/attachments/901242337257193502/968488126915936266/dm_t20.png"),
                new Crate(38, new string[] {"assets/bundled/prefabs/radtown/dmloot/dm tier3 lootbox.prefab" }, "dm_tier3", "DM Tier 3 Crate", "https://cdn.discordapp.com/attachments/901242337257193502/968487021100281886/crate_t30.png"),

                // Train Wagons
                new Crate(93, "assets/content/vehicles/trains/wagons/trainwagonunloadable.entity.prefabA", "wagon_metal", "Metal Ore Wagon", defaultConfig: "3460LCSC"),
                new Crate(94, "assets/content/vehicles/trains/wagons/trainwagonunloadable.entity.prefabB", "wagon_sulfur", "Sulfur Ore Wagon", defaultConfig: "BRvi4c4K"),
                new Crate(95, "assets/content/vehicles/trains/wagons/trainwagonunloadable.entity.prefabC", "wagon_charcoal", "Charcoal Wagon", defaultConfig: "FLjpBzuX"),
                new Crate(96, "assets/content/vehicles/trains/wagons/trainwagonunloadablefuel.entity.prefab", "wagon_fuel", "Fuel Wagon", defaultConfig: "Kyu8AJK0"),
                new Crate(97, "assets/content/vehicles/trains/wagons/subents/wagon_crate_normal_2.prefab", "wagon_crate_normal", "Wagon Crate", "https://files.facepunch.com/rust/item/cratecostume_512.png",
                    defaultConfig: "iJV3q75X"),
                new Crate(98, "assets/content/vehicles/trains/wagons/subents/wagon_crate_normal.prefab", "wagon_crate_military", "Military Wagon Crate","https://cdn.discordapp.com/attachments/901242337257193502/968488126915936266/dm_t20.png",
                    defaultConfig: "wNwBm3rG"),
                new Crate(99, "assets/content/vehicles/trains/wagons/subents/wagon_crate_normal_2_food.prefab", "wagon_crate_food", "Food Wagon Crate", "https://cdn.discordapp.com/attachments/901242337257193502/984178592332279838/crate_food0.png",
                    defaultConfig: "wpKKFfk8"),
                new Crate(100, "assets/content/vehicles/trains/wagons/subents/wagon_crate_normal_2_medical.prefab", "wagon_crate_medical", "Medical Wagon Crate", "https://cdn.discordapp.com/attachments/901242337257193502/984178638792556574/crate_medical0.png",
                    defaultConfig: "Zzmvc8Qv"),
            };

            private static readonly List<Quarry> quarryList = new List<Quarry>
            {
                new Quarry(41, Quarry.Type.Excavator, new int[]{ -2099697608, -1157596551, 69511070, -1982036270}, "quarry_excavator", "Giant Excavator", "https://cdn.discordapp.com/attachments/901242337257193502/968489840905699328/excv0.png", LootableType.Excavator),
                new Quarry(42, Quarry.Type.Quarry, new int[] {-321733511  }, "quarry_oil", "Pump Jack", "https://static.wikia.nocookie.net/play-rust/images/c/c9/Pump_Jack_icon.png"),
                new Quarry(43, Quarry.Type.Quarry, new int[] {-2099697608, -4031221 }, "quarry_stone", "Stone Quarry", "https://static.wikia.nocookie.net/play-rust/images/b/b8/Mining_Quarry_icon.png"),
                new Quarry(44, Quarry.Type.Quarry, new int[] {-1157596551 }, "quarry_sulfur", "Sulfur Quarry", "https://static.wikia.nocookie.net/play-rust/images/b/b8/Mining_Quarry_icon.png"),
                new Quarry(45, Quarry.Type.Quarry, new int[] {-1982036270 }, "quarry_hqm", "HQM Quarry", "https://static.wikia.nocookie.net/play-rust/images/b/b8/Mining_Quarry_icon.png"),
                new Quarry(104, Quarry.Type.Quarry, new int[] {-1982036270, -1157596551, -2099697608, -4031221 }, "quarry_any", "'Everything' Quarry", "https://static.wikia.nocookie.net/play-rust/images/b/b8/Mining_Quarry_icon.png"),
            };

            private static readonly List<Collectible> collectibleList = new List<Collectible>
            {
                new Collectible(62, new string[]{"assets/content/structures/excavator/prefabs/diesel_collectable.prefab"}, new ItemAmount[]{
                    new ItemAmount(ItemManager.FindItemDefinition("diesel_barrel"), 1)}, "collectible_diesel", "Diesel Barrel", "https://cdn.discordapp.com/attachments/901242337257193502/968490295551471636/diesel0.png"),
                new Collectible(63, new string[]{"assets/bundled/prefabs/autospawn/collectable/wood/wood-collectable.prefab", "assets/bundled/prefabs/autospawn/collectable/stone/halloween/halloween-wood-collectable.prefab"},
                    new ItemAmount[]{ new ItemAmount(ItemManager.FindItemDefinition("wood"), 50)}, "collectible_wood", "Collectable Wood", "https://cdn.discordapp.com/attachments/901242337257193502/968490298483306506/wood_coll0.png"),
                new Collectible(64, new string[]{ "assets/bundled/prefabs/autospawn/collectable/stone/stone-collectable.prefab", "assets/bundled/prefabs/autospawn/collectable/stone/halloween/halloween-stone-collectable.prefab"},
                    new ItemAmount[]{ new ItemAmount(ItemManager.FindItemDefinition("stones"), 50)}, "collectible_stone", "Collectable Stone", "https://cdn.discordapp.com/attachments/901242337257193502/968490296998506516/stone_coll0.png"),
                new Collectible(65, new string[]{ "assets/bundled/prefabs/autospawn/collectable/stone/sulfur-collectable.prefab", "assets/bundled/prefabs/autospawn/collectable/stone/halloween/halloween-sulfur-collectible.prefab"},
                    new ItemAmount[]{ new ItemAmount(ItemManager.FindItemDefinition("sulfur.ore"), 50)}, "collectible_sulfur", "Collectable Sulfur", "https://cdn.discordapp.com/attachments/901242337257193502/968490297942237194/sulfur_ore0.png"),
                new Collectible(66, new string[]{ "assets/bundled/prefabs/autospawn/collectable/stone/halloween/halloween-metal-collectable.prefab", "assets/bundled/prefabs/autospawn/collectable/stone/metal-collectable.prefab"},
                    new ItemAmount[]{ new ItemAmount(ItemManager.FindItemDefinition("metal.ore"), 50)}, "collectible_metal", "Collectable Metal", "https://cdn.discordapp.com/attachments/901242337257193502/968490299519287366/metal_ore0.png"),
                new Collectible(67, new string[]{ "assets/bundled/prefabs/autospawn/collectable/pumpkin/pumpkin-collectable.prefab"}, new ItemAmount[]{
                    new ItemAmount(ItemManager.FindItemDefinition("pumpkin"), 1), new ItemAmount(ItemManager.FindItemDefinition("seed.pumpkin"), 1)}, "collectible_pumpkin", "Collectable Pumpkin", "https://cdn.discordapp.com/attachments/901242337257193502/968490296516173865/pumpkin_coll.png"),
                new Collectible(68, new string[]{ "assets/bundled/prefabs/autospawn/collectable/potato/potato-collectable.prefab"}, new ItemAmount[]{
                    new ItemAmount(ItemManager.FindItemDefinition("potato"), 1), new ItemAmount(ItemManager.FindItemDefinition("seed.potato"), 1)}, "collectible_potato", "Collectable Potato", "https://cdn.discordapp.com/attachments/901242337257193502/968490296117690388/potato_coll.png"),
                new Collectible(69, new string[]{ "assets/bundled/prefabs/autospawn/collectable/mushrooms/mushroom-cluster-6.prefab", "assets/bundled/prefabs/autospawn/collectable/mushrooms/mushroom-cluster-5.prefab"},
                    new ItemAmount[]{ new ItemAmount(ItemManager.FindItemDefinition("mushroom"), 1)}, "collectible_mushroom", "Collectable Mushroom",  "https://static.wikia.nocookie.net/play-rust/images/a/a8/Mushroom_icon.png/revision/latest/scale-to-width-down/256"),
                new Collectible(70, new string[]{ "assets/bundled/prefabs/autospawn/collectable/hemp/hemp-collectable.prefab"}, new ItemAmount[]{
                    new ItemAmount(ItemManager.FindItemDefinition("cloth"), 10), new ItemAmount(ItemManager.FindItemDefinition("seed.hemp"), 1)}, "collectible_hemp", "Collectable Hemp", "https://cdn.discordapp.com/attachments/901242337257193502/968491133812490280/hemp_coll0.png"),
                new Collectible(71, new string[]{ "assets/bundled/prefabs/autospawn/collectable/corn/corn-collectable.prefab"}, new ItemAmount[]{
                    new ItemAmount(ItemManager.FindItemDefinition("corn"), 1), new ItemAmount(ItemManager.FindItemDefinition("seed.corn"), 1)}, "collectible_corn", "Collectable Corn", "https://static.wikia.nocookie.net/play-rust/images/0/0a/Corn_icon.png"),
                new Collectible(72, new string[]{ "assets/bundled/prefabs/autospawn/collectable/berry-yellow/berry-yellow-collectable.prefab"}, new ItemAmount[]{
                    new ItemAmount(ItemManager.FindItemDefinition("yellow.berry"), 1), new ItemAmount(ItemManager.FindItemDefinition("seed.yellow.berry"), 1)}, "collectible_berry_yellow", "Collectable Yellow Berry", "https://cdn.discordapp.com/attachments/901242337257193502/968491570003337246/berry_yellow0.png"),
                new Collectible(73, new string[]{ "assets/bundled/prefabs/autospawn/collectable/berry-white/berry-white-collectable.prefab"}, new ItemAmount[]{
                    new ItemAmount(ItemManager.FindItemDefinition("white.berry"), 1), new ItemAmount(ItemManager.FindItemDefinition("seed.white.berry"), 1)}, "collectible_berry_white", "Collectable White Berry", "https://cdn.discordapp.com/attachments/901242337257193502/968491571605540884/berry_white0.png"),
                new Collectible(74, new string[]{ "assets/bundled/prefabs/autospawn/collectable/berry-red/berry-red-collectable.prefab"}, new ItemAmount[]{
                    new ItemAmount(ItemManager.FindItemDefinition("red.berry"), 1), new ItemAmount(ItemManager.FindItemDefinition("seed.red.berry"), 1)}, "collectible_berry_red", "Collectable Red Berry", "https://cdn.discordapp.com/attachments/901242337257193502/968491571324543017/berry_red0.png"),
                new Collectible(75, new string[]{ "assets/bundled/prefabs/autospawn/collectable/berry-green/berry-green-collectable.prefab"}, new ItemAmount[]{
                    new ItemAmount(ItemManager.FindItemDefinition("green.berry"), 1), new ItemAmount(ItemManager.FindItemDefinition("seed.green.berry"), 1)}, "collectible_berry_green", "Collectable Green Berry", "https://cdn.discordapp.com/attachments/901242337257193502/968491570879942676/berry_green0.png"),
                new Collectible(76, new string[]{ "assets/bundled/prefabs/autospawn/collectable/berry-blue/berry-blue-collectable.prefab"}, new ItemAmount[]{
                    new ItemAmount(ItemManager.FindItemDefinition("blue.berry"), 1), new ItemAmount(ItemManager.FindItemDefinition("seed.blue.berry"), 1)}, "collectible_berry_blue", "Collectable Blue Berry", "https://cdn.discordapp.com/attachments/901242337257193502/968491570598916106/berry_blue0.png"),
                new Collectible(77, new string[]{ "assets/bundled/prefabs/autospawn/collectable/berry-black/berry-black-collectable.prefab"}, new ItemAmount[]{
                    new ItemAmount(ItemManager.FindItemDefinition("black.berry"), 1), new ItemAmount(ItemManager.FindItemDefinition("seed.black.berry"), 1)}, "collectible_berry_black", "Collectable Black Berry", "https://cdn.discordapp.com/attachments/901242337257193502/968491570296946698/berry_black0.png"),
            };

            // RoamNpc (83) is deprecated
            public enum Lootables
            {
                None, Normal, Military, Elite, NormalUnderwater, MilitaryUnderwater, EliteUnderwater, AmmoUnderwater, FoodUnderwater, FuelUnderwater, MedicalUnderwater, ToolsUnderwater,
                Mine, Food, Medical, Tools, UnderwaterBasic, UnderwaterAdvanced, Hackable, HackableOilrig, Bradley, Heli, Barrel, OilBarrel, FoodBox, Minecart, TechPartsUnderwater, VehiclePartsUnderwater,
                VehicleParts, DmAmmo, DmC4, DmConstructionRes, DmConstructionTools, DmFood, DmMedical, DmResources, DmTier1, DmTier2, DmTier3, SupplyDrop, RoadSign,
                Excavator, OilQuarry, StoneQuarry, SulfurQuarry, HqmQuarry, ExcavatorNpc, MilitaryNpc, OilrigNpc, ChinookNpc, JunkpileNpc, ScarecrowNpc, CargoNpc, HeavyNpc, TunnelNpc, UnderwaterNpc,
                TrainyardNpc, AirfieldNpc, DesertNpc, ArcticNpc, Basic, StackSizeControl, CollectibleDiesel, CollectibleWood, CollectibleStones, CollectibleSulfur, CollectibleMetal, CollectiblePumpkin,
                CollectiblePotato, CollectibleMushroom, CollectibleHemp, CollectibleCorn, CollectibleBerryY, CollectibleBerryW, CollectibleBerryR, CollectibleBerryG, CollectibleBerryB, CollectibleBerryBlack,
                GatherConfig, FurnaceConfig, AirwolfConfig, RecyclerConfig, AirDropConfig, RoamNpc, BasicInvis, EliteInvis, MilitaryInvis, NormalInvis, FoodInvis, MedicalInvis, ToolsInvis, FoodBoxInvis,
                VehiclePartsInvis, TrainWagonMetalOre, TrainWagonSulfurOre, TrainWagonCharcoal, TrainWagonFuel, TrainWagonCrate, TrainWagonMilitaryCrate, TrainWagonFoodCrate, TrainWagonMedCrate, 
                NpcMissileSiloNvg, NpcLaunchSite, NpcMissileSiloOverground, EverythingQuarry
            };

            #endregion

            public static void Initialize()
            {
                crateConfigCache = new Dictionary<int, LootableConfig>();
                quarryConfigCache = new Dictionary<int, QuarryConfig>();
                baseLootableCache = new Dictionary<int, BaseLootable>();
                excavatorConfigCache = new ExcavatorConfig();
                collectibleConfigCache = new Dictionary<int, CollectibleConfig>();
                npcCache = new Dictionary<int, List<ulong>>();
                defaultConfigCache = new Dictionary<int, LootableConfig>();

                PopulateLootableCache();
                LoadDefaultConfigs();
                LoadData();
            }

            public static void Save() => SaveData();

            #region Static Lootables

            public static readonly List<StaticLootablePrototype> staticLootableProtoList = new()
            {
                new StaticLootablePrototype("supermarket_cash_register", "https://d1nhio0ox7pgb.cloudfront.net/_img/o_collection_png/green_dark_grey/256x256/plain/cash_register.png"),
            };

            public static readonly List<StaticLootableModels.LootableDefinition> staticLootableDefinitionCache = new();

            public const int MAX_LOOTABLES_PER_PAGE = 27;
            public static int CachedTotalSlPages { get; private set; }

            public static IEnumerable<StaticLootableDisplay> GetStaticLootables(int page)
            {
                if (!UseStaticLootables)
                {
                    return new StaticLootableDisplay[0];
                }

                staticLootableDefinitionCache.Clear();

                JObject[] staticLootables = _instance.StaticLootables.Call<JObject[]>("GetLootables");

                CachedTotalSlPages = Mathf.CeilToInt((float)staticLootables.Length / MAX_LOOTABLES_PER_PAGE);
                DPrint($"{staticLootables.Length} lootables, {CachedTotalSlPages} pages");

                foreach(var lootable in staticLootables)
                {
                    var def = lootable.ToObject<StaticLootableModels.LootableDefinition>();
                    staticLootableDefinitionCache.Add(def);
                }

                int start = MAX_LOOTABLES_PER_PAGE * Mathf.Clamp(page, 0, CachedTotalSlPages - 1);

                return staticLootableDefinitionCache.GetRange2(start, MAX_LOOTABLES_PER_PAGE).Select(x => new StaticLootableDisplay(x));
            }

            public static StaticLootableModels.LootableDefinition GetStaticLootable(string uid)
            {
                return staticLootableDefinitionCache.Find(x => x.Uid == uid);
            }

            public static void SetStaticLootable(StaticLootableModels.LootableDefinition lootable)
            {
                DPrint($"update lootable with uniqueid {lootable.UniqueId ?? "null"}");

                JObject l = JObject.FromObject(lootable);
                if (_instance.StaticLootables.Call<bool>("CreateOrEditLootable", l, false))
                {
                    staticLootableDefinitionCache.RemoveAll(x => x.Uid == lootable.Uid);
                    staticLootableDefinitionCache.Add(lootable);
                }
                else
                {
                    CErr($"Failed to update static lootable with uid {lootable.Uid}");
                }
            }

            #endregion

            #region Import Loot Table

            public static void ImportLootTable(string name, IPlayer player)
            {
                LootableConfig config = Interface.Oxide.DataFileSystem.ReadObject<LootableConfig>(DataFilePath("import", name));
                if (config == null) return;

                config.TryEnable(false);
                BaseLootable crate = FindLootable((int)config.crate);
                if (crate is Crate)
                    config.item_amount.max = ((Crate)crate).DefaultSlots;
                else if (crate is NpcCorpse)
                    config.item_amount.max = NpcCorpse.defaultSlots;

                player.Reply($"Importing config for {config.crate}");
                SetCrateConfig(config);
            }

            #endregion

            #region Default Config

            public static void LoadDefaultConfigs(bool reload = false, IPlayer player = null) => InvokeHandler.Instance.StartCoroutine(LoadDefaultConfigsCoro(reload, player));

            private static IEnumerator LoadDefaultConfigsCoro(bool reload, IPlayer player)
            {
                var sw = new Stopwatch();
                sw.Start();

                yield return null;

                if (!reload && (Flags?.Update ?? false)) reload = true;
                if (reload) defaultConfigCache.Clear();

                foreach(BaseLootable crate in crateList.Concat<BaseLootable>(npcList))
                {
                    if (crate.DefaultConfig == null) continue;

                    var defaultConfig = Interface.Oxide.DataFileSystem.ReadObject<LootableConfig>(DataFilePath("default", crate.saveName));

                    if (!defaultConfig.IsValid() || reload)
                    {
                        CPrint($"Downloading vanilla loot profile for {crate.saveName}");

                        UnityWebRequest request = UnityWebRequest.Get($"https://pastebin.com/raw/{crate.DefaultConfig}");
                        yield return request.SendWebRequest();

                        if (request.isNetworkError)
                        {
                            CErr($"Failed to download vanilla loot profile for {crate.saveName}: Network error");
                            request.Dispose();
                            continue;
                        }

                        string json = request.downloadHandler.text;
                        LootableConfig config = JsonConvert.DeserializeObject<LootableConfig>(json);

                        if (!config.IsValid())
                        {
                            DErr($"Invalid config for {crate.saveName}, skipping");
                            request.Dispose();
                            continue;
                        }

                        config.TryEnable(false);
                        defaultConfig = config;
                        Interface.Oxide.DataFileSystem.WriteObject(DataFilePath("default", crate.saveName), defaultConfig);
                    }

                    defaultConfigCache.Add(crate.id, defaultConfig);
                }
                sw.Stop();
                if (player != null) player.Reply($"Refreshed vanilla loot profiles in {sw.ElapsedMilliseconds}ms");
                CPrint($"Refreshed vanilla loot profiles in {sw.ElapsedMilliseconds}ms");

                yield break;
            }

            #endregion

            #region GetConfig

            public static LootableConfig GetContainerConfig(LootContainer container)
            {
                LootableConfig config;
                Crate crate = crateList.Find(x => x.HasPrefab(container.PrefabName));
                if (crate == default(Crate)) return null;

                if (crateConfigCache.TryGetValue(crate.id, out config))
                    return config;
                return null;
            }

            public static LootableConfig GetTrainCarConfig(int lootTypeIndex)
            {
                Lootables wagonLootable = Lootables.None;
                switch (lootTypeIndex)
                {
                    case 1001:
                        wagonLootable = Lootables.TrainWagonFuel;
                        break;
                    case 0:
                        wagonLootable = Lootables.TrainWagonCharcoal;
                        break;
                    case 1:
                        wagonLootable = Lootables.TrainWagonMetalOre;
                        break;
                    case 2:
                        wagonLootable = Lootables.TrainWagonSulfurOre;
                        break;
                }

                LootableConfig config;
                if (crateConfigCache.TryGetValue((int)wagonLootable, out config))
                {
                    return config;
                }
                    
                return null;
            }

            public static QuarryConfig GetQuarryConfig(MiningQuarry quarry)
            {
                int id;
                switch (quarry.staticType)
                {
                    case MiningQuarry.QuarryType.Basic:
                        id = 43;
                        break;

                    case MiningQuarry.QuarryType.HQM:
                        id = 45;
                        break;

                    case MiningQuarry.QuarryType.None:
                        if (quarry.canExtractLiquid)
                        {
                            id = 42;
                        }
                        else
                        {
                            id = 104;
                        }
                        break;

                    case MiningQuarry.QuarryType.Sulfur:
                        id = 44;
                        break;

                    default:
                        return null;
                }

                return quarryConfigCache[id];
            }

            public static ExcavatorConfig GetExcavatorConfig() => excavatorConfigCache;
            
            public static LootableConfig GetNpcConfig(LootableCorpse corpse)
            {
                int lootable_id = -1;

                foreach(var npcType in npcCache)
                {
                    if (npcType.Value.Contains(corpse.playerSteamID))
                    {
                        lootable_id = npcType.Key;
                        break;
                    }
                }

                if (lootable_id == -1) return null;

                LootableConfig config = crateConfigCache[lootable_id];
                return config;
            }

            public static CollectibleConfig GetCollectibleConfig(CollectibleEntity entity)
            {
                CollectibleConfig config;
                Collectible c = collectibleList.Find(x => x.prefabNames.Contains(entity.PrefabName));
                if (c == default(Collectible)) return null;

                if (collectibleConfigCache.TryGetValue(c.id, out config))
                    return config;
                return null;
            }

            public static int GetConfigState(int id)
            {
                if (crateConfigCache.TryGetValue(id, out var config))
                {
                    if (config.Enabled)
                    {
                        return config.lootType switch
                        {
                            LootableConfig.LootType.Custom => 1,
                            LootableConfig.LootType.BlackList => 2,
                            LootableConfig.LootType.Addition => 3,
                            _ => 0
                        };
                    }

                    return 0;
                }

                if (quarryConfigCache.TryGetValue(id, out var conf))
                {
                    if (conf.enabled) return 1;
                    return 0;
                }

                if (collectibleConfigCache.TryGetValue(id, out var ccf))
                {
                    if (ccf.enabled) return 1;
                    return 0;
                }

                if ((int)excavatorConfigCache.quarry == id)
                {
                    if (excavatorConfigCache.enabled) return 1;
                    return 0;
                }

                if ((int)Lootables.GatherConfig == id)
                {
                    if (GatherManager.gatherConfig.enabled) return 1;
                    return 0;
                }

                return 0;
            }

            #endregion

            #region NPCs

            public static List<string> PopulateNpcCache()
            {
                List<string> messages = new List<string>();
                foreach (BasePlayer player in BaseNetworkable.serverEntities.OfType<BasePlayer>())
                {
                    if (!player.IsNpc) continue;
                    string msg = CacheNpc(player);
                    if (!messages.Contains(msg)) messages.Add(msg);
                }
                return messages;
            }

            public static string CacheNpc(BasePlayer npc)
            {
                string npc_name = npc.PrefabName;
                uint id = npc.prefabID;

                string spawnPoint = npc.GetComponent<SpawnPointInstance>()?.parentSpawnPoint?.GetComponentInParent<PrefabParameters>()?.ToString();

                int npc_type = -1;
                foreach(var npcType in npcList)
                {
                    if (spawnPoint == null && npcType.spawnPointFilter != null)
                    {
                        continue;
                    }

                    if (npcType.prefabIds.Contains(id))
                    {
                        if (npcType.spawnPointFilter != null && !spawnPoint.Contains(npcType.spawnPointFilter))
                        {
                            continue;
                        }

                        //DPrint($"MAP {npc.ShortPrefabName} to {npcType.saveName} pool id: {npc.prefabID} spawnPoint: {spawnPoint}");

                        npc_type = npcType.id;
                        break;
                    }
                }

                if (npc_type == -1)
                {
                    return $"FAILED TO CACHE {npc.ShortPrefabName}";
                }

                if (!npcCache.ContainsKey(npc_type))
                {
                    npcCache.Add(npc_type, new List<ulong>());
                }

                npcCache[npc_type].Add(npc.userID);

                return $"SUCCESSFULLY CACHED {npc_name}";
            }

            #endregion

            #region Data Handling

            public static void SetCrateConfig(LootableConfig lootTable)
            {
                crateConfigCache[(int)lootTable.crate] = lootTable;
                SaveData();
            }

            public static void SetQuarryConfig(QuarryConfig config)
            {
                quarryConfigCache[(int)config.type] = config;
                SaveData();
            }

            public static void SetExcavatorConfig(ExcavatorConfig config)
            {
                excavatorConfigCache = config;
                SaveData();
            }

            public static void SetCollectibleConfig(CollectibleConfig config)
            {
                collectibleConfigCache[(int)config.collectible] = config;
                SaveData();
            }

            public static BaseLootable GetLootable(int id) => baseLootableCache[id];

            public static BaseLootable GetLootable(string idString) => GetLootable(Int32.Parse(idString));

            public static BaseLootable GetLootableBySaveName(string saveName) => baseLootableCache.Values.ToList().Find(x => x.saveName == saveName);

            public static Dictionary<string, string> GetImageDictionary()
            {
                var temp = new Dictionary<string, string>();

                foreach (var lootable in baseLootableCache.Values)
                    temp.Add(lootable.saveName, lootable.image);

                if (UseStaticLootables)
                    temp = temp.MergeWith(StaticLootablePrototype.GetImageDictionary());

                return temp;
            }

            private static void PopulateLootableCache()
            {
                baseLootableCache = new Dictionary<int, BaseLootable>();

                foreach (var config in crateList)
                    baseLootableCache.Add(config.id, (BaseLootable)config);
                foreach (var config in quarryList)
                    baseLootableCache.Add(config.id, (BaseLootable)config);
                foreach (var config in npcList)
                    baseLootableCache.Add(config.id, (BaseLootable)config);
                foreach (var config in collectibleList)
                    baseLootableCache.Add(config.id, (BaseLootable)config);
                foreach (var config in ConfigManager.genericLootableList)
                    baseLootableCache.Add(config.id, (BaseLootable)config);
            }

            private static void LoadData()
            {
                foreach(var crate in crateList)
                {
                    var config = Interface.Oxide.DataFileSystem.ReadObject<LootableConfig>(DataFilePath(crate.saveName));
                    if (!config.IsValid() && defaultConfigCache.ContainsKey(crate.id)) config = defaultConfigCache[crate.id];

                    config.SetCrate(crate.Lootable);
                    crateConfigCache.Add(crate.id, config);
                }

                foreach (var npc in npcList)
                {
                    var config = Interface.Oxide.DataFileSystem.ReadObject<LootableConfig>(DataFilePath(npc.saveName));

                    config.SetCrate(npc.Lootable);
                    crateConfigCache.Add(npc.id, config);
                }

                foreach (var quarry in quarryList)
                {
                    if (quarry.quarryType == Quarry.Type.Excavator)
                    {
                        var config = Interface.Oxide.DataFileSystem.ReadObject<ExcavatorConfig>(DataFilePath(quarry.saveName));
                        excavatorConfigCache = config;
                    }
                    else
                    {
                        var config = Interface.Oxide.DataFileSystem.ReadObject<QuarryConfig>(DataFilePath(quarry.saveName));

                        config.SetQuarry(quarry.Lootable);
                        quarryConfigCache.Add(quarry.id, config);
                    }             
                }

                foreach (var collectible in collectibleList)
                {
                    var config = Interface.Oxide.DataFileSystem.ReadObject<CollectibleConfig>(DataFilePath(collectible.saveName));

                    config.SetCollectible(collectible.Lootable);
                    collectibleConfigCache.Add(collectible.id, config);
                }
            }

            private static void SaveData()
            {
                foreach(var config in crateConfigCache)
                {
                    BaseLootable lootable = FindLootable(config.Key);
                    Interface.Oxide.DataFileSystem.WriteObject(DataFilePath(lootable.saveName), config.Value);
                }

                foreach (var config in quarryConfigCache)
                {
                    BaseLootable quarry = FindLootable(config.Key);
                    Interface.Oxide.DataFileSystem.WriteObject(DataFilePath(quarry.saveName), config.Value);
                }

                foreach (var config in collectibleConfigCache)
                {
                    BaseLootable collectible = FindLootable(config.Key);
                    Interface.Oxide.DataFileSystem.WriteObject(DataFilePath(collectible.saveName), config.Value);
                }

                var excavator = ExcavatorQuarry();
                Interface.Oxide.DataFileSystem.WriteObject(DataFilePath(excavator.saveName), excavatorConfigCache);
            }

            #endregion

            #region Helpers

            public static BaseLootable FindLootable(int id) => baseLootableCache[id];
            public static Quarry ExcavatorQuarry() => quarryList.Find(x => x.id == 41);
  
            public static uint RandomSeed() => (uint)UnityEngine.Random.Range(1, UInt32.MaxValue);

            public static string Itemid2Shortname(int itemid) => ItemManager.FindItemDefinition(itemid).shortname;
            public static int? Shortname2Itemid(string shortname) => ItemManager.FindItemDefinition(shortname)?.itemid;
            public static string Itemid2DisplayName(int itemid) => ItemManager.FindItemDefinition(itemid).displayName.english;

            #endregion

            #region Crate Refresh

            public static void RefreshCrateLoot(BasePlayer player, bool toVanilla = false)
            {
                if (!Flags.RefreshLootOnExit)
                {
                    DPrint("Loot refresh canceled - disabled");
                    return;
                }

                InvokeHandler.Instance.StartCoroutine(RefreshCrateLootCoro(player, toVanilla));
            }

            private static IEnumerator RefreshCrateLootCoro(BasePlayer player, bool toVanilla)
            {
                yield return toVanilla ? null : CoroutineEx.waitForEndOfFrame;

                bool console = player == null;

                var sw = new Stopwatch();
                sw.Start();

                List<LootContainer> containers = BaseNetworkable.serverEntities.OfType<LootContainer>().ToList();
                List<TrainCarUnloadable> wagons = BaseNetworkable.serverEntities.OfType<TrainCarUnloadable>().ToList();

                if (!console) player.ChatMessage("Refreshing loot...");

                foreach (var container in containers)
                {
                    if (container.inventory == null)
                    {
                        CErr("LootContainer has null inventory");
                        continue;
                    }

                    if (container is HackableLockedCrate crate && !crate.IsFullyHacked())
                    {
                        DPrint("Skipping locked crate");
                        continue;
                    }

                    container.inventory.Clear();
                    ItemManager.DoRemoves();

                    if (toVanilla || _instance.OnLootSpawn(container) == null)
                    {
                        container.PopulateLoot();
                        if (container.shouldRefreshContents)
                        {
                            container.CancelInvoke(container.SpawnLoot);
                            container.Invoke(container.SpawnLoot, UnityEngine.Random.Range(container.minSecondsBetweenRefresh, container.maxSecondsBetweenRefresh));
                        }
                    }
                }

                foreach(var wagon in wagons)
                {
                    StorageContainer storageContainer = wagon.GetStorageContainer();
                    if (storageContainer.IsValid() && !wagon.IsEmpty())
                        wagon.FillWithLoot(storageContainer);
                }

                sw.Stop();

                string msg = $"Refreshed {containers.Count} crates and {wagons.Count} train wagons in {sw.ElapsedMilliseconds}ms";
                if (!console) player.ChatMessage(msg);
                else CPrint(msg);

                yield break;
            }

            #endregion
        }

        #endregion

        #region Collectible Config

        public class CollectibleConfig : IMultiplyable
        {
            [JsonProperty("enabled")]
            public bool enabled { get; protected set; }
            [JsonProperty("type")]
            public LootManager.Lootables collectible { get; protected set; }
            [JsonProperty("items")]
            public List<LootItem> items;
            [JsonIgnore]
            private bool isNew = false;

            [JsonConstructor]
            public CollectibleConfig(Dictionary<int, LootCategory> categories, List<LootItem> items, LootManager.Lootables collectible, bool enabled)
            {
                this.items = items;
                this.collectible = collectible;
                this.enabled = enabled;
            }

            public CollectibleConfig(LootManager.Lootables collectible, List<LootItem> items = null)
            {
                this.enabled = false;
                this.collectible = collectible;
                this.items = items ?? new List<LootItem>();
            }

            public CollectibleConfig()
            {
                this.enabled = false;
                this.collectible = LootManager.Lootables.None;
                this.items = new List<LootItem>();
                isNew = true;
            }

            public void Multiply(float multiplier)
            {
                if (multiplier == 1) return;

                foreach (var item in items)
                {
                    int min = item.amount.min;
                    int max = item.amount.max;

                    int newmin = Mathf.RoundToInt(min * multiplier);
                    int newmax = Mathf.RoundToInt(max * multiplier);

                    newmin = Mathf.Clamp(newmin, 1, Flags.DisableItemLimit ? Int32.MaxValue : Flags.ItemLimit);
                    newmax = Mathf.Clamp(newmax, 1, Flags.DisableItemLimit ? Int32.MaxValue : Flags.ItemLimit);

                    item.amount.min = newmin;
                    item.amount.max = newmax;
                }
            }

            public void AddItem(LootItem new_item, bool replace_old = false, LootItem old_item = null)
            {
                // Replace old item
                if (replace_old)
                {
                    var old_itms = items.FindAll(x => x.SkinItemId == old_item.SkinItemId);
                    foreach (var old_itm in old_itms)
                        items.Remove(old_itm);
                }

                // Remove conflicting items
                var conflict_items = items.FindAll(x => x.SkinItemId == new_item.SkinItemId);
                foreach (var conflict in conflict_items)
                    items.Remove(conflict);
                
                // Add new item
                items.Add(new_item);
            }

            public void RemoveItem(string skinItemId)
            {
                var old_items = items.FindAll(x => x.SkinItemId == skinItemId);
                foreach (var old_item in old_items)
                    items.Remove(old_item);
            }

            public LootItem GetItem(int itemid) => items.Find(x => x.itemid == itemid);

            public void TryEnable(bool enable)
            {
                enabled = enable;
                Validate();
            }

            public void Validate()
            {
                if (!IsValid())
                    enabled = false;
            }

            public bool IsValid() => !(items.IsNullOrEmpty() || collectible == default(LootManager.Lootables));

            public void SetCollectible(LootManager.Lootables collectible)
            {
                this.collectible = collectible;
                if (isNew) SetDefaultItems();
            }

            public void LoadDefaultConfig() => SetDefaultItems(true);

            private void SetDefaultItems(bool force = false)
            {
                if (force) items = new List<LootItem>();
                if (!items.IsNullOrEmpty()) return;

                Collectible coll = (Collectible)LootManager.GetLootable((int)collectible);
                foreach (var itm in coll.vanillaDrop)
                    items.Add(LootItem.CreateFromItemAmount(itm)); 
            }

            public void DoPickup(CollectibleEntity entity, BasePlayer player)
            {
                if (entity == null || player == null)
                {
                    DErr("Collectible entity or player is null");
                    return;
                }

                foreach (var itm in items)
                {
                    if (itm.RandomRoll())
                    {
                        //itm.AddToContainer(player.inventory.containerBelt, sendNote: true, drop: true, altContainer: player.inventory.containerMain);
                        // Fix for weird behavior w/ hotbar
                        var item = itm.CreateItem();
                        player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
                    }
                        
                }

                
                if (entity.pickupEffect.isValid)
                {
                    Effect.server.Run(entity.pickupEffect.resourcePath, entity.transform.position, entity.transform.up, null, false);
                }

                _instance.NextFrame(() =>
                {
                    entity?.Kill(BaseNetworkable.DestroyMode.None);
                });
            }
        }

        #endregion

        #region Quarry Config

        public class ExcavatorConfig
        {
            [JsonProperty("enabled")]
            public bool enabled { get; private set; }
            [JsonProperty("type")]
            public LootManager.Lootables quarry { get; private set; }
            [JsonProperty("duration")]
            public float dieselBarrelDuration;
            [JsonProperty("items")]
            public Dictionary<int, List<LootItem>> items;
            [JsonIgnore]
            private Dictionary<LootItem, float> pendingResources = null;
            [JsonIgnore]
            private int lastMiningIdx = -1;
            [JsonIgnore]
            private int currentPile = 0;

            [JsonConstructor]
            public ExcavatorConfig(bool enabled, LootManager.Lootables quarry, float dieselBarrelDuration, Dictionary<int, List<LootItem>> items)
            {
                this.enabled = enabled;
                this.quarry = quarry;
                this.dieselBarrelDuration = dieselBarrelDuration;
                this.items = items;
            }

            public ExcavatorConfig()
            {
                enabled = false;
                quarry = LootManager.Lootables.Excavator;
                dieselBarrelDuration = 120f;
                items = new Dictionary<int, List<LootItem>>();
                RestoreDefaults();
            }

            public void AddItem(int collectionid, LootItem new_item, bool replace_old = false, LootItem old_item = null)
            {
                // Replace old item
                if (replace_old)
                {
                    var old_itms = items[collectionid].FindAll(x => x.itemid == old_item.itemid);
                    foreach (var old_itm in old_itms)
                    {
                        items[collectionid].Remove(old_itm);
                    }
                }

                // Remove conflicting items
                var conflict_items = items[collectionid].FindAll(x => x.itemid == new_item.itemid);
                foreach (var conflict in conflict_items)
                {
                    items[collectionid].Remove(conflict);
                }

                // Add new item
                items[collectionid].Add(new_item);
                Validate();
            }

            public void RemoveItem(int collectionid, int itemid)
            {
                if (items[collectionid].Count <= 1) return;

                var old_items = items[collectionid].FindAll(x => x.itemid == itemid);
                foreach (var old_item in old_items)
                {
                    items[collectionid].Remove(old_item);
                }
                Validate();
            }

            public LootItem GetItem(int collectionid, int itemid) => items[collectionid].Find(x => x.itemid == itemid);
            

            public void Apply(ref ExcavatorArm arm)
            {
                if (!enabled) return;

                ItemAmount[] new_res = new ItemAmount[]
                {
                    items[0][0].CreateItemAmount(),
                    items[1][0].CreateItemAmount(),
                    items[2][0].CreateItemAmount(),
                    items[3][0].CreateItemAmount()
                };

                //DPrint($"time for full res: {arm.timeForFullResources} new: {dieselBarrelDuration}");
                //arm.timeForFullResources = dieselBarrelDuration;
                arm.resourcesToMine = new_res;
                arm.resourceProductionTickRate = 1f;

                lastMiningIdx = arm.resourceMiningIndex;

                pendingResources = null;
            }

            public void GatherUpdate(ref ExcavatorArm arm, Item item)
            {
                if (!enabled) return;

                if (currentPile == 0) currentPile = 1;
                else if (currentPile == 1) currentPile = 0;


                int res_idx = arm.resourceMiningIndex;
               
                if (lastMiningIdx != res_idx)
                {
                    pendingResources = null;
                    lastMiningIdx = res_idx;
                }

                if (pendingResources == null)
                {
                    pendingResources = new Dictionary<LootItem, float>();
                }

                int itemAmount = item.amount;
                LootItem mainItem = items[res_idx].Find(x => x.itemid == item.info.itemid);
                if (mainItem == null)
                {
                    CErr($"Error while processing excavator resouces");
                    return;
                }

                float percentage = ((float)itemAmount) / ((float)mainItem.amount.min);
                //DPrint($"main percentage {percentage:N5} (IA {itemAmount}) (TA {mainItem.amount.min})");

                foreach(var itm in items[res_idx].ToArray().Trim(1))
                {
                    float newPending;
                    if (!pendingResources.ContainsKey(itm)) pendingResources[itm] = 0f;

                    int amount = CalculateAmount(itm.amount.min, percentage, pendingResources[itm], out newPending);
                    pendingResources[itm] = newPending;

                    if (amount < 1) continue;
                    itm.AddToContainer(arm.outputPiles[currentPile].inventory, amount);
                }
            }

            private int CalculateAmount(int total, float percentage, float pending, out float newPending)
            {
                float next_amount_f = total*percentage + pending;
                int next_amount = Mathf.FloorToInt(next_amount_f);
                newPending = next_amount_f - next_amount;
                return next_amount;
            }

            public static string GetShortnameFromKey(int key)
            {
                switch (key)
                {
                    case 0:
                        return "hq.metal.ore";

                    case 1:
                        return "sulfur.ore";

                    case 2:
                        return "stones";

                    case 3:
                        return "metal.fragments";

                    default:
                        return "";
                }
            }

            public static int GetItemIdFromKey(int key)
            {
                switch (key)
                {
                    case 0:
                        return -1982036270;

                    case 1:
                        return -1157596551;

                    case 2:
                        return -2099697608;

                    case 3:
                        return 69511070;

                    default:
                        return 0;
                }
            }

            public void TryEnable(bool enabled)
            {
                this.enabled = enabled;
                Validate();
            }

            public void Validate()
            {
                bool valid = true;
                if (items[0].Count < 1) valid = false;
                if (items[1].Count < 1) valid = false;
                if (items[2].Count < 1) valid = false;
                if (items[3].Count < 1) valid = false;

                if (!valid) enabled = false;
                foreach (var kv in items.ToList())
                    items[kv.Key] = kv.Value.OrderByDescending(x => x.amount.min).ToList();
            }

            private void RestoreDefaults()
            {
                items[0] = new List<LootItem> { new LootItem("hq.metal.ore", 100, 100, 1f) };
                items[1] = new List<LootItem> { new LootItem("sulfur.ore", 2000, 2000, 1f) };
                items[2] = new List<LootItem> { new LootItem("stones", 10000, 10000, 1f) };
                items[3] = new List<LootItem> { new LootItem("metal.fragments", 5000, 5000, 1f) };
            }

        }

        public class QuarryConfig
        {
            [JsonProperty("enabled")]
            public bool enabled { get; private set; }
            [JsonProperty("type")]
            public LootManager.Lootables type { get; private set; }
            [JsonProperty("items")]
            public List<LootItem> items;
            [JsonIgnore]
            public bool liquid { get { return type == LootManager.Lootables.OilQuarry; } }
            [JsonIgnore]
            private List<ResourceDepositManager.ResourceDeposit.ResourceDepositEntry> vanillaConfig;

            [JsonConstructor]
            public QuarryConfig(bool enabled, LootManager.Lootables type, List<LootItem> items)
            {
                this.enabled = enabled;
                this.type = type;
                this.items = items;
                this.vanillaConfig = null;
            }

            public QuarryConfig()
            {
                this.enabled = false;
                this.type = LootManager.Lootables.StoneQuarry;
                this.items = new List<LootItem>();
                this.vanillaConfig = null;
            }

            public void AddItem(LootItem new_item, bool replace_old = false, LootItem old_item = null)
            {
                // Replace old item
                if (replace_old)
                {
                    var old_itms = items.FindAll(x => x.SkinItemId == old_item.SkinItemId);
                    foreach (var old_itm in old_itms)
                    {
                        items.Remove(old_itm);
                    }
                }

                // Remove conflicting items
                var conflict_items = items.FindAll(x => x.SkinItemId == new_item.SkinItemId);
                foreach (var conflict in conflict_items)
                {
                    items.Remove(conflict);
                }

                // Add new item
                items.Add(new_item);
            }

            public void RemoveItem(string skinItemId)
            {
                var old_items = items.FindAll(x => x.SkinItemId == skinItemId);
                foreach (var old_item in old_items)
                {
                    items.Remove(old_item);
                }
            }

            public LootItem GetItem(int itemid) => items.Find(x => x.itemid == itemid);

            public bool ContainsItem(string skinItemId) => items.FindAll(x => x.SkinItemId == skinItemId).Count > 0;

            public void QuarryInit(ref MiningQuarry quarry)
            {
                if (vanillaConfig == null) vanillaConfig = quarry._linkedDeposit._resources;
                if (!enabled) return;

                vanillaConfig = quarry._linkedDeposit._resources;
                quarry._linkedDeposit._resources.Clear();

                foreach(var item in items)
                {
                    item.AddToQuarry(ref quarry);
                }
            }

            public void ResetQuarry(ref MiningQuarry quarry) 
            {
                if (quarry._linkedDeposit._resources != vanillaConfig && vanillaConfig != null)
                    quarry._linkedDeposit._resources = vanillaConfig; 
            }          

            public void QuarryUpdate(ref MiningQuarry quarry, ref Item item)
            {
                if (!enabled)
                {                
                    ResetQuarry(ref quarry);
                    return;
                }

                string skinItemId = LootItem.GetSkinItemId(item);
                var result = items.FindAll(x => x.SkinItemId == skinItemId);

                if (result.Count < 1) return;
                var lootItem = result[0];

                if (lootItem.skin != 0)
                    item.skin = lootItem.skin;
                if (lootItem.displayname != "")
                    item.name = lootItem.displayname;

                return;
            }

            public void SetQuarry(LootManager.Lootables quarry)
            {
                this.type = quarry;
                AddDefaultItems();
            }

            private void AddDefaultItems()
            {
                if (!items.IsNullOrEmpty()) return;
                switch (type)
                {
                    case LootManager.Lootables.StoneQuarry:
                        items.Add(new LootItem("stones", 1, 1, 1f, work: 0.3f));
                        items.Add(new LootItem("metal.ore", 1, 1, 1f, work: 2f));
                        break;

                    case LootManager.Lootables.SulfurQuarry:
                        items.Add(new LootItem("sulfur.ore", 1, 1, 1f, work: 2f));
                        break;

                    case LootManager.Lootables.HqmQuarry:
                        items.Add(new LootItem("hq.metal.ore", 1, 1, 1f, work: 30f));
                        break;

                    case LootManager.Lootables.OilQuarry:
                        items.Add(new LootItem("crude.oil", 1, 1, 1f, work: 10f, liquid: true));
                        break;

                    case LootManager.Lootables.EverythingQuarry:
                        items.Add(new LootItem("stones", 1, 1, 1f, work: 0.3f));
                        items.Add(new LootItem("metal.ore", 1, 1, 1f, work: 5f));
                        items.Add(new LootItem("sulfur.ore", 1, 1, 1f, work: 7.5f));
                        items.Add(new LootItem("hq.metal.ore", 1, 1, 1f, work: 75f));
                        break;

                    default:
                        break;
                }
            }           

            public void TryEnable(bool enabled)
            {
                this.enabled = enabled;
                Validate();
            }

            public void Validate()
            {
                if (items.Count < 1)
                    enabled = false;
            }

        }

        #endregion

        #region Crate Config

        [Serializable]
        public class LootableConfig : IMultiplyable
        {
            public enum LootType { Custom, BlackList, Addition }

            [JsonProperty("enabled")]
            public bool Enabled { get; protected set; }
            [JsonProperty("type")]
            public LootManager.Lootables crate { get; protected set; }
            [JsonProperty("amount")]
            public MinMax item_amount;
            [JsonProperty("categories")]
            public Dictionary<int, LootCategory> categories;
            [JsonProperty("items")]
            public List<LootItem> items;
            [JsonProperty("seed")]
            public uint seed;
            [JsonProperty("blacklist")]
            public List<BlacklistItem> blacklist;
            [JsonProperty("addtions")]
            public List<LootItem> additions;
            [JsonProperty("loottype")]
            public LootType lootType { get; protected set; }

            [JsonConstructor]
            public LootableConfig(Dictionary<int, LootCategory> categories, List<LootItem> items, MinMax item_amount, LootManager.Lootables crate, bool enabled, 
                uint seed, List<BlacklistItem> blacklist, List<LootItem> additions, LootType lootType)
            {
                this.categories = categories;
                this.items = items;
                this.item_amount = item_amount;
                this.crate = crate;
                this.Enabled = enabled;
                this.seed = seed;
                this.blacklist = blacklist;
                this.additions = additions;
                this.lootType = lootType;
            }

            public LootableConfig(LootManager.Lootables crate, List<LootItem> items = null)
            {
                this.categories = new Dictionary<int, LootCategory>();
                this.crate = crate;
                this.items = items ?? new List<LootItem>();
                this.item_amount = new MinMax(1);
                this.Enabled = false;
                this.seed = LootManager.RandomSeed();
                this.blacklist = new List<BlacklistItem>();
                this.additions = new List<LootItem>();
                this.lootType = LootType.Custom;
                AddDefaultCategories();
            }

            public LootableConfig()
            {
                this.categories = new Dictionary<int, LootCategory>();
                this.crate = LootManager.Lootables.Basic;
                this.items = new List<LootItem>();
                this.item_amount = new MinMax(1);
                this.Enabled = false;
                this.seed = LootManager.RandomSeed();
                this.blacklist = new List<BlacklistItem>();
                this.additions = new List<LootItem>();
                this.lootType = LootType.Custom;
                AddDefaultCategories();
            }

            public void Multiply(float multiplier)
            {
                if (multiplier == 1) return;

                foreach(var item in items)
                {
                    int min = item.amount.min;
                    int max = item.amount.max;

                    int newmin = Mathf.RoundToInt(min * multiplier);
                    int newmax = Mathf.RoundToInt(max * multiplier);

                    newmin = Mathf.Clamp(newmin, 1, Flags.DisableItemLimit ? Int32.MaxValue : Flags.ItemLimit);
                    newmax = Mathf.Clamp(newmax, 1, Flags.DisableItemLimit ? Int32.MaxValue : Flags.ItemLimit);

                    item.amount.min = newmin;
                    item.amount.max = newmax;
                }
            }

            public void AddItem(LootType itemType, LootItem new_item, bool replace_old = false, LootItem old_item = null)
            {
                List<LootItem> itms = itemType == LootType.Custom ? items : additions;

                // Replace old item
                if (replace_old)
                {
                    var old_itms = itms.FindAll(x => x.SkinItemId == old_item.SkinItemId);
                    foreach (var old_itm in old_itms)
                    {
                        itms.Remove(old_itm);
                    }
                }

                // Remove conflicting items
                var conflict_items = itms.FindAll(x => x.SkinItemId == new_item.SkinItemId);
                foreach (var conflict in conflict_items)
                {
                    itms.Remove(conflict);
                }

                // Add new item
                itms.Add(new_item);
            }

            public void RemoveItem(LootType itemType, string skinItemId)
            {
                List<LootItem> itms = itemType == LootType.Custom ? items : additions;

                var old_items = itms.FindAll(x => x.SkinItemId == skinItemId);
                foreach(var old_item in old_items)
                {
                    itms.Remove(old_item);
                }
            }

            public void AddBlacklistItem(BlacklistItem new_item)
            {
                foreach(var itm in blacklist.ToList())
                {
                    if (itm.itemid == new_item.itemid)
                    {
                        blacklist.Remove(itm);
                    }
                }

                blacklist.Add(new_item);
            }

            public void RemoveBlacklistItem(int itemid)
            {
                var old_items = blacklist.FindAll(x => x.itemid == itemid);
                foreach (var old_item in old_items)
                {
                    blacklist.Remove(old_item);
                }
            }

            protected void AddDefaultCategories()
            {
                for(int i = 0; i < UiHelper.maxCategories; i++)
                {
                    if (!categories.ContainsKey(i))
                    {
                        var cat = new LootCategory(i);
                        SetCategory(cat);
                    }
                }
            }

            public void SetCategory(LootCategory category)
            {
                categories[category.id] = category;
            }

            public LootCategory GetCategory(int id)
            {
                if (categories.ContainsKey(id))
                    return categories[id];
                return null;
            }

            public LootCategory GetCategory(string idString)
            {
                var id = Int32.Parse(idString);
                return GetCategory(id);
            }

            public void TryEnable(bool enable)
            {
                if (!IsValid())
                    Enabled = false;
                else
                    Enabled = enable;
            }

            public void ChangeLootType()
            {
                if (lootType == LootType.Custom)
                    lootType = LootType.BlackList;

                else if (lootType == LootType.BlackList)
                    lootType = LootType.Addition;

                else if (lootType == LootType.Addition)
                    lootType = LootType.Custom;

                Validate();
            }

            public void Validate()
            {
                if (!IsValid())
                    Enabled = false;
            }

            public bool IsValid()
            {
                if (crate == default(LootManager.Lootables)) return false;

                if (lootType == LootType.Custom && items.IsNullOrEmpty()) return false;
                if (lootType == LootType.BlackList && blacklist.IsNullOrEmpty()) return false;
                if (lootType == LootType.Addition && additions.IsNullOrEmpty()) return false;

                if (lootType == LootType.Custom)
                {
                    if (crate == LootManager.Lootables.TrainWagonFuel)
                    {
                        return ContainsItem(-946369541);
                    }
                    if (crate == LootManager.Lootables.TrainWagonCharcoal)
                    {
                        return ContainsItem(-1938052175);
                    }
                    if (crate == LootManager.Lootables.TrainWagonSulfurOre)
                    {
                        return ContainsItem(-1157596551);
                    }
                    if (crate == LootManager.Lootables.TrainWagonMetalOre)
                    {
                        return ContainsItem(-4031221);
                    }
                }

                if (crate.IsOneOf(
                    LootManager.Lootables.TrainWagonCharcoal,
                    LootManager.Lootables.TrainWagonMetalOre,
                    LootManager.Lootables.TrainWagonSulfurOre,
                    LootManager.Lootables.TrainWagonFuel
                ) && lootType == LootType.BlackList)
                {
                    return false;
                }

                return true;
            }

            public void SetCrate(LootManager.Lootables crate) => this.crate = crate;

            public void ApplyToCrate(LootContainer container, bool clear = false, bool cover = false) => Apply(container.inventory, clear, cover, false, false);

            public void ApplyToTrain(StorageContainer container, bool capStacks = true) => Apply(container.inventory, false, false, false, capStacks);

            public void ApplyToCorpse(ItemContainer container, bool clear = false, bool cover = false) => Apply(container, clear, cover, true, false);

            private void Apply(ItemContainer itemContainer, bool clear, bool cover, bool npc, bool capStacks)
            {
                if (!Enabled) return;
                if (itemContainer == null) return;

                if (lootType == LootType.Custom)       
                    ApplyCustomLoot(itemContainer, clear, cover, npc, capStacks);

                else if (lootType == LootType.BlackList)
                    ApplyBlacklist(itemContainer);

                else if (lootType == LootType.Addition)
                    ApplyAddition(itemContainer);
            }

            private void ApplyCustomLoot(ItemContainer container, bool clear, bool cover, bool npc, bool capStacks)
            {
                int maxItems = container.availableSlots.Count;

                List<LootItem> containerLoot = new List<LootItem>();
                items.Shuffle(seed);
                int max_retries = 20;
                int retries = 0;

                Dictionary<int, int> ia = new Dictionary<int, int>();

                // Pick items without category
                foreach (var item in items.FindAll(x => x.category == 0))
                {
                    if (item.RandomRoll())
                    {
                        containerLoot.Add(item);
                        //DPrint($"N A {item.shortname}");
                    }
                }

                // Pick items from each category
                foreach (var cat in categories.Values)
                {
                    if (cat.isDefault) continue;
                    if (!cat.RandomRoll()) continue;

                    var cat_items = items.FindAll(x => x.category == cat.id);
                    var item_amount = cat.itemAmount.Random();
                                        
                    var selected_items = cat_items.GetRandomElements(item_amount);
                    containerLoot.AddRange(selected_items);
                    ia[cat.id] = selected_items.Count();

                    //foreach (var i in selected_items)
                        //DPrint($"{cat.id} A {i.shortname}");
                }

                // Too many items, remove some if possible
                if (containerLoot.Count > item_amount.max)
                {
                    int difftotal = containerLoot.Count - item_amount.max;
                    int diffcurrent = difftotal;

                    var catlist = ia.ToList();
                    catlist.Shuffle(seed);

                    foreach(var catia in catlist)
                    {
                        var cat = categories[catia.Key];
                        if (catia.Value > cat.itemAmount.min)
                        {
                            int cdiff = catia.Value - cat.itemAmount.min;
                            var itms = containerLoot.FindAll(x => x.category == cat.id);
                            //DPrint($"{cat.id} IA:{catia.Value} MIN:{cat.itemAmount.min} DIFF:{cdiff}");

                            for (int i = 0; i < cdiff; i++)
                            {
                                var itm = itms.GetRandom();
                                itms.Remove(itm);
                                containerLoot.Remove(itm);
                                //DPrint($"{cat.id} R {itm.shortname}");

                                diffcurrent--;
                                if (diffcurrent <= 0) goto end;
                            }

                            if (diffcurrent <= 0) goto end;
                        }
                    }

                    if (containerLoot.Count <= item_amount.max) goto end;

                    catlist.Shuffle(seed);
                    foreach(var catid in catlist)
                    {
                        var cat = categories[catid.Key];
                        if (!cat.RandomRoll())
                        {
                            var itms = containerLoot.FindAll(x => x.category == cat.id);
                            foreach(var itm in itms)
                            {
                                containerLoot.Remove(itm);
                            }
                        }
                    }

                    int r = 0;
                    while (containerLoot.Count > item_amount.max && r < max_retries){
                        //DPrint($"CONT_AMT {containerLoot.Count} max{item_amount.max}");
                        foreach(var item in containerLoot.ToList())
                        {
                            if (!item.RandomRoll())
                                containerLoot.Remove(item);
                            if (containerLoot.Count <= item_amount.max) goto end;
                        }
                        r++;
                    }

                }
                end:

                // Not enough items, add some if possible
                while (containerLoot.Count < item_amount.min)
                {
                    foreach (var item in items.FindAll(x => x.category == 0))
                    {
                        if (item.RandomRoll() && !containerLoot.Contains(item, LootItem.EqualityComparer))
                        {
                            containerLoot.Add(item);
                            //DPrint($"N A {item.shortname}");
                        }
                        if (containerLoot.Count >= item_amount.max) break;
                    }

                    foreach (var cat in categories.Values)
                    {
                        if (cat.isDefault) continue;
                        if (!cat.RandomRoll()) continue;
                        if (ia.ContainsKey(cat.id)) continue;

                        var cat_items = items.FindAll(x => x.category == cat.id);
                        var cat_item_amount = cat.itemAmount.Random();

                        if (containerLoot.Count + cat_item_amount > item_amount.max)
                        {
                            cat_item_amount = item_amount.max - containerLoot.Count;
                        }

                        var selected_items = cat_items.GetRandomElements(cat_item_amount);
                        containerLoot.AddRange(selected_items);
                        ia[cat.id] = selected_items.Count();

                        if (containerLoot.Count >= item_amount.max) break;
                    }

                    retries++;
                    if (retries > max_retries) break;
                }

                // Clear container if necessary
                if (clear)
                {
                    container.ForceClear();
                }

                // Adjust capacity and add items to box
                if (container.capacity < containerLoot.Count) container.capacity = containerLoot.Count;

                foreach (var item in containerLoot)
                {
                    item.AddToContainer(container, capStack: capStacks);

                    if (item.HasExtras)
                    {
                        foreach (var ex in item.extras)
                        {
                            if (container.itemList.Count >= container.capacity)
                                container.capacity++;
                            ex.AddToContainer(container);
                        } 
                    }
                }

                // Apply cover and adjust capacity
                if (container.capacity > container.itemList.Count || cover) container.capacity = container.itemList.Count;
            }

            private void ApplyBlacklist(ItemContainer container)
            {
                foreach(var item in container.itemList.ToList())
                {
                    if (IsBlacklisted(item.info.itemid))
                    {
                        item.Remove();
                    }
                }
            }

            private void ApplyAddition(ItemContainer container)
            {
                additions.Shuffle(seed);

                foreach (var item in additions)
                {
                    if (item.RandomRoll())
                    {
                        if (container.IsFull())
                        {
                            CErr($"Failed to add additional items to {container.entityOwner?.ShortPrefabName} - container is full");
                            break;
                        }

                        item.AddToContainer(container);
                    }
                }
            }

            private bool IsBlacklisted(int itemid) => blacklist.Find(x => x.itemid == itemid) != null;

            private bool ContainsItem(int itemid, float minChance = 1f)
            {
                return items.Find(x => x.itemid == itemid && x.chance >= minChance) != null;
            }
        }

        [Serializable]
        public class LootCategory
        {
            [JsonProperty("id")]
            public int id { get; private set; }
            [JsonProperty("amount")]
            public MinMax itemAmount;
            [JsonProperty("chance")]
            public float chance;
            [JsonIgnore]
            public bool isDefault { get { return id == 0; } }

            [JsonConstructor]
            public LootCategory(int id, MinMax amount, float chance)
            {
                this.id = id;
                itemAmount = amount;
                this.chance = chance;
            }

            public LootCategory(int id)
            {
                this.id = id;
                itemAmount = new MinMax(1);
                chance = 0f;
            }

            public bool RandomRoll() => UnityEngine.Random.Range(0f, 1f) <= chance;
        }

        #endregion

        #region Item Config

        [Serializable]
        public class LootItem
        {
            public static string GetSkinItemId(Item item) => $"{item.info.itemid}:{item.skin}";

            [JsonIgnore]
            public static LootItemEqualityComparer EqualityComparer { get; } = new LootItemEqualityComparer();
            [JsonProperty("itemid")]
            public int itemid;
            [JsonProperty("amount")]
            public MinMax amount;
            [JsonProperty("chance")]
            public float chance;
            [JsonProperty("skin")]
            public ulong skin;
            [JsonProperty("category")]
            public int category;
            [JsonProperty("displayname")]
            public string displayname;
            [JsonProperty("work")]
            public float work;
            [JsonProperty("liquid")]
            public bool liquid;
            [JsonProperty("extras")]
            public List<ExtraItem> extras;
            [JsonProperty("condition")]
            public MinMaxFloat condition;
            [JsonProperty("blueprint")]
            public bool isBlueprint;
            [JsonIgnore]
            public bool HasExtras => extras.Count > 0;
            [JsonIgnore]
            public string SkinItemId => $"{itemid}:{skin}:{(isBlueprint ? 1 : 0)}";
            [JsonIgnore]
            public string Shortname => ItemDefinition().shortname;
            [JsonIgnore]
            public bool HasCondition => ItemDefinition().condition.enabled;

            [JsonConstructor]
            public LootItem(int itemid, MinMax amount, float chance, ulong skin, int category, string displayname, float work = 1f, bool liquid = false, List<ExtraItem> extras = null, MinMaxFloat condition = default(MinMaxFloat), bool isBlueprint = false)
            {
                // Fix for chocolate bar rename
                if (itemid == 363467698) itemid = -965336208;

                this.itemid = itemid;
                this.amount = amount;
                this.chance = chance;
                this.skin = skin;
                this.category = category;
                this.displayname = displayname;
                this.work = work;
                this.liquid = liquid;
                this.extras = extras ?? new List<ExtraItem>();
                this.isBlueprint = isBlueprint;
                if (condition == default(MinMaxFloat))
                {
                    condition = MinMaxFloat.One;
                }
                this.condition = condition;
            }

            public LootItem(string shortname, int amt_min, int amt_max, float chance, ulong skin = 0, int category = 0, string displayname = "", float work = 1f, bool liquid = false, List<ExtraItem> extras = null, MinMaxFloat condition = default(MinMaxFloat), bool isBlueprint = false)
            {
                // Fix for chocolate bar rename
                if (shortname == "chocholate") shortname = "chocolate";

                itemid = ItemManager.FindItemDefinition(shortname).itemid;
                amount = new MinMax(amt_min, amt_max);
                this.chance = chance;
                this.skin = skin;
                this.category = category;
                this.displayname = displayname;
                this.work = work;
                this.liquid = liquid;
                this.extras = extras ?? new List<ExtraItem>();
                if (condition == default(MinMaxFloat))
                {
                    condition = MinMaxFloat.One;
                }
                this.condition = condition;
                this.condition = condition;
                this.isBlueprint = isBlueprint;
            }

            public static ExtraItem FindExtra(List<ExtraItem> extras, int itemid)
            {
                var res = extras.FindAll(x => x.itemid == itemid);
                if (res.Count < 1) return null;
                return res.First();
            }

            public static void RemoveExtra(ref List<ExtraItem> extras, int itemid)
            {
                var ex = FindExtra(extras, itemid);
                if (ex == null) return;
                extras.Remove(ex);
            }

            public static LootItem CreateFromItemAmount(ItemAmount itemAmount, float chance = 1f)
            {
                return new LootItem(itemAmount.itemDef.shortname, (int)itemAmount.amount, (int)itemAmount.amount, chance);
            }

            public Item CreateItem()
            {
                var i = ItemManager.Create(ItemDefinition(), amount.Random(), skin);
                if (displayname != "") i.name = displayname;
                return i;
            }

            public ItemAmount CreateItemAmount(bool useMax = false)
            {
                ItemAmount a;
                if (useMax)
                    a = new ItemAmount(ItemDefinition(), amount.max);
                else
                    a = new ItemAmount(ItemDefinition(), amount.min);
                return a;
            }

            public int AddToContainer(ItemContainer container, int? overrideAmount = null, bool sendNote = false, bool drop = false, ItemContainer altContainer = null, bool capStack = false)
            {
                int amountTotal = overrideAmount ?? amount.Random();
                int amountLeft = amountTotal;

                ItemDefinition itemDefinition = ItemDefinition();
                if (itemDefinition == null)
                {
                    DErr($"Failed to find item with id {itemid}");
                    return 0;
                }

                BasePlayer player = container.GetOwnerPlayer();
                if (player?.IsNpc ?? true) sendNote = false;

                int stack = capStack ? itemDefinition.stackable : amountLeft;

                while(amountLeft > 0)
                {
                    int amt = Mathf.Min(stack, amountLeft);
                    amountLeft -= stack;

                    Item item;
                    if (isBlueprint)
                    {
                        item = ItemManager.CreateByItemID(-996920608, amt, 0);
                        item.blueprintTarget = itemid;
                    }
                    else
                    {
                        item = ItemManager.Create(itemDefinition, amt, skin);
                        if (HasCondition)
                        {
                            item.conditionNormalized = condition.Random();
                        }
                    }

                    if (displayname != String.Empty)
                    {
                        item.name = displayname;
                    }

                    item.OnVirginSpawn();

                    if (sendNote) SendNote(player, amt);
                    if (!item.MoveToContainer(container))
                    {
                        if (altContainer != null && item.MoveToContainer(altContainer))
                        {
                            return amt;
                        }

                        if (drop)
                        {
                            item.Drop(container.dropPosition, container.dropVelocity);
                            if (sendNote) SendNote(player, -amt);
                        }
                        else
                        {
                            item.Remove(0f);
                        }
                    }
                }

                return amountTotal;
            }

            public void AddToQuarry(ref MiningQuarry quarry)
            {
                var type = liquid ? ResourceDepositManager.ResourceDeposit.surveySpawnType.OIL : ResourceDepositManager.ResourceDeposit.surveySpawnType.ITEM;
                quarry._linkedDeposit.Add(ItemDefinition(), 1f, 1000, work, type, liquid);
            }

            private void SendNote(BasePlayer player, int? overrideAmount = null)
            {
                string amt = overrideAmount == null ? amount.Random().ToString() : overrideAmount.ToString();
                if (displayname == "")
                    player.Command("note.inv", itemid, amt);
                else
                    player.Command("note.inv", itemid, amt, displayname);
            }

            public ItemDefinition ItemDefinition() => ItemManager.FindItemDefinition(itemid);

            public bool RandomRoll() => UnityEngine.Random.value <= chance;

            public bool HasCategory() => category >= 0;

        }

        [Serializable]
        public class ExtraItem
        {
            [JsonProperty("itemid")]
            public int itemid;
            [JsonProperty("amount")]
            public MinMax amount;

            public ExtraItem()
            {
                itemid = -932201673;
                amount = new MinMax(1);
            }

            public ExtraItem(int itemid)
            {
                this.itemid = itemid;
                amount = new MinMax(1);
            }

            public int AddToContainer(ItemContainer container, int? overrideAmount = null)
            {
                int amt = overrideAmount ?? amount.Random();

                ItemDefinition itemdef = ItemManager.FindItemDefinition(itemid);
                Item item = ItemManager.Create(itemdef, amt);
                item.OnVirginSpawn();

                if (!item.MoveToContainer(container, -1, true))
                    item.Remove(0f);

                return amt;
            }
        }

        [Serializable]
        public class BlacklistItem
        {
            [JsonProperty("itemid")]
            public int itemid;
            [JsonIgnore]
            public string Shortname => ItemManager.FindItemDefinition(itemid).shortname;

            [JsonConstructor]
            public BlacklistItem(int itemid)
            {
                this.itemid = itemid;
            }
        }

        public class LootItemEqualityComparer : IEqualityComparer<LootItem>
        {
            public bool Equals(LootItem lootItem1, LootItem lootItem2)
            {
                return lootItem1.SkinItemId == lootItem2.SkinItemId;
            }

            public int GetHashCode(LootItem lootItem)
            {
                return lootItem.itemid;
            }
        }

        #endregion

        #region Static Lootables

        public class StaticLootablePrototype
        {
            public const string prefix = "st_";
            public const string defaultImage = prefix + "default";

            private static Dictionary<string, string> imageDictionary = new Dictionary<string, string> { [defaultImage] = "https://i.kym-cdn.com/photos/images/original/001/923/870/d41" };

            public static Dictionary<string, string> GetImageDictionary() => imageDictionary;

            public static string GetImage(string prefabFilter)
            {
                if (!imageDictionary.ContainsKey(prefix + prefabFilter)) return null;
                return prefix + prefabFilter;
            }

            public string PrefabFilter { get; }
            public string Image { get; }

            public StaticLootablePrototype(string prefabFilter, string image = null)
            {
                if (image != null && !imageDictionary.ContainsKey(prefix + prefabFilter)) imageDictionary.Add(prefix + prefabFilter, image);
                Image = image == null ? null : prefix + prefabFilter;
                PrefabFilter = prefabFilter;
            }
        }

        public class StaticLootableDisplay
        {
            public string Uid { get; }
            public string PrefabFilter { get; }
            public string DisplayName { get; }

            public string Image { get; }
            public bool HasImage => Image != null;

            private Vector3? _worldPos;
            public Vector3 WorldPos => (Vector3)_worldPos;
            public bool HasWorldPos => _worldPos != null;

            public StaticLootableDisplay(StaticLootableModels.LootableDefinition def)
            {
                Uid = def.Uid;
                _worldPos = ParseWorldPos(def.UniqueId);
                PrefabFilter = def.PrefabFilter;
                Image = StaticLootablePrototype.GetImage(PrefabFilter);
                DisplayName = GetDisplayname();
            }

            private Vector3? ParseWorldPos(string uniqueId)
            {
                if (uniqueId == null) return null;

                string[] coords = uniqueId.Split('_');

                Vector3 pos = new Vector3();
                pos.x = Single.Parse(coords[0]);
                pos.y = Single.Parse(coords[1]);
                pos.z = Single.Parse(coords[2]);
                return pos;
            }

            private string GetDisplayname()
            {
                string prefab = Path.GetFileNameWithoutExtension(PrefabFilter);
                StringBuilder dp = new StringBuilder(prefab);
                dp = dp.Replace('_', ' ');

                int idx = 0;
                foreach (var c in dp.ToString())
                {
                    if (idx == 0)
                    {
                        dp[idx] = Char.ToUpper(dp[idx]);
                    }
                    else if (c == ' ' && idx + 1 < dp.Length)
                    {
                        char old = dp[idx + 1];
                        char n = Char.ToUpper(old);
                        dp[idx + 1] = n;
                    }
                    idx++;
                }

                return dp.ToString().AddSpacesBeforeUppercase();
            }

            public static Vector3 GetWorldPos(string uniqueId)
            {
                if (uniqueId == null) return Vector3.zero;

                string[] coords = uniqueId.Split('_');

                Vector3 pos = new Vector3();
                pos.x = Single.Parse(coords[0]);
                pos.y = Single.Parse(coords[1]);
                pos.z = Single.Parse(coords[2]);
                return pos;
            }
        }

        #endregion

        #region More Classes

        public class BaseLootable
        {
            public LootManager.LootableType type;
            public string saveName;
            public int id;
            public string image;
            public string displayName;

            public LootManager.Lootables Lootable { get { return (LootManager.Lootables)id; } }
            public string DefaultConfig { get; protected set; } = null;

            public virtual bool HasVanillaConfig() => false;

            protected BaseLootable() { }

            public BaseLootable(int id, LootManager.LootableType type, string saveName, string displayName, string image)
            {
                this.id = id;
                this.saveName = saveName;
                this.type = type;
                this.image = image;
                this.displayName = displayName;
            }
        }

        private class GenericLootable : BaseLootable
        {
            public GenericLootable(LootManager.Lootables lootable, string saveName, string displayName, string image = "https://www.schulz-grafik.de/wp-content/uploads/2018/03/placeholder.png")
            {
                this.id = (int)lootable;
                this.type = LootManager.LootableType.Generic;
                this.saveName = saveName;
                this.image = image;
                this.displayName = displayName;
            }
        }

        private class Collectible : BaseLootable
        {
            public string[] prefabNames;
            public ItemAmount[] vanillaDrop;

            public Collectible(int id, string[] prefabNames, ItemAmount[] vanillaDrop, string saveName, string displayName, string image = "https://cdn.discordapp.com/attachments/901242337257193502/967009588698300446/unknown.png")
            {
                this.type = LootManager.LootableType.Collectible;
                this.id = id;
                this.prefabNames = prefabNames;
                this.vanillaDrop = vanillaDrop;
                this.saveName = saveName;
                this.displayName = displayName;
                this.image = image;
            }
        }

        private class NpcCorpse : BaseLootable
        {
            public HashSet<uint> prefabIds;
            public string spawnPointFilter;

            public const int defaultSlots = 24;

            public NpcCorpse(int id, HashSet<uint> prefabIds, string spawnPointFilter, string saveName, string displayName, string image = "https://files.facepunch.com/rust/item/hazmatsuit_scientist_512.png", string defaultConfig = null)
            {
                this.type = LootManager.LootableType.NpcCorpse;
                this.id = id;
                this.prefabIds = prefabIds;
                this.spawnPointFilter = spawnPointFilter;
                this.saveName = saveName;
                this.displayName = displayName;
                this.image = image;
                this.DefaultConfig = defaultConfig;
            }

            public NpcCorpse(int id, uint prefabId, string spawnPointFilter, string saveName, string displayName, string image = "https://files.facepunch.com/rust/item/hazmatsuit_scientist_512.png", string defaultConfig = null)
            {
                this.type = LootManager.LootableType.NpcCorpse;
                this.id = id;
                this.prefabIds = new HashSet<uint>{prefabId};
                this.spawnPointFilter = spawnPointFilter;
                this.saveName = saveName;
                this.displayName = displayName;
                this.image = image;
                this.DefaultConfig = defaultConfig;
            }

            public override bool HasVanillaConfig() => LootManager.defaultConfigCache.ContainsKey(id);
        }

        public class Quarry : BaseLootable
        {
            public enum Type { Excavator, Quarry };

            public Type quarryType;
            public int[] quarryItems;

            public Quarry(int id, Type quarryType, int[] quarryItems, string saveName, string displayName, string image = "https://files.facepunch.com/rust/item/cratecostume_512.png",
                LootManager.LootableType type = LootManager.LootableType.Quarry)
            {
                this.type = type;
                this.id = id;
                this.quarryType = quarryType;
                this.saveName = saveName;
                this.image = image;
                this.displayName = displayName;
                this.quarryItems = quarryItems;
            }
        }

        private class Crate : BaseLootable
        {
            public string[] prefabNames;
            public int MaxSlots { get; }
            public int DefaultSlots { get; }

            private const int maxSlots = 12;
            private const int defaultSlots = 6;

            public Crate(int id, string[] prefabNames, string saveName, string displayName, string image = "https://files.facepunch.com/rust/item/cratecostume_512.png", int maxSlots = maxSlots,
                int defaultSlots = defaultSlots, string defaultConfig = null)
            {
                this.type = LootManager.LootableType.Crate;
                this.id = id;
                this.prefabNames = prefabNames;
                this.saveName = saveName;
                this.image = image;
                this.displayName = displayName;
                this.MaxSlots = maxSlots;
                this.DefaultSlots = defaultSlots;
                this.DefaultConfig = defaultConfig;
            }

            public Crate(int id, string prefabName, string saveName, string displayName, string image = "https://files.facepunch.com/rust/item/cratecostume_512.png", int maxSlots = maxSlots,
                int defaultSlots = defaultSlots, string defaultConfig = null)
            {
                this.type = LootManager.LootableType.Crate;
                this.id = id;
                this.prefabNames = new[] { prefabName };
                this.saveName = saveName;
                this.image = image;
                this.displayName = displayName;
                this.MaxSlots = maxSlots;
                this.DefaultSlots = defaultSlots;
                this.DefaultConfig = defaultConfig;
            }

            public bool HasPrefab(string prefab) => prefabNames.Contains(prefab);

            public override bool HasVanillaConfig() => LootManager.defaultConfigCache.ContainsKey(id);
        }

        private class CopyPaste
        {
            private LootableConfig copy;
            public bool IsEmpty { get; private set; }
            public string CopyName { get
                {
                    if (IsEmpty) return "Empty";
                    else return LootManager.FindLootable((int)copy.crate).displayName;
                } }

            public CopyPaste(LootableConfig copy)
            {
                this.copy = copy;
                this.IsEmpty = false;
            }

            public CopyPaste()
            {
                this.IsEmpty = true;
            }

            public LootableConfig Paste(LootManager.Lootables crate)
            {
                LootableConfig clone = copy.Clone<LootableConfig>();
                clone.SetCrate(crate);
                return clone;
            }
        }

        #endregion

        #region Interfaces

        public interface IMultiplyable
        {
            void Multiply(float multi);
        }

        #endregion

        #region Value Types

        [Serializable]
        public struct MinMax
        {
            [JsonProperty("min")]
            public int min;
            [JsonProperty("max")]
            public int max;

            [JsonConstructor]
            public MinMax(int min, int max)
            {
                this.min = min;
                this.max = max;
            }

            public MinMax(float min, float max, bool validate = false)
            {
                this.min = Mathf.RoundToInt(min);
                this.max = Mathf.RoundToInt(max);
                if (this.max < this.min && validate) this.max = this.min;
            }

            public MinMax(int amount)
            {
                this.min = amount;
                this.max = amount;
            }

            public override string ToString()
            {
                if (min == max)
                    return $"{min}";
                return $"{min}-{max}";
            }

            public int Random() => UnityEngine.Random.Range(min, max+1);
        }

        [Serializable]
        public struct MinMaxFloat
        {
            [JsonProperty("min")]
            public float min;
            [JsonProperty("max")]
            public float max;

            public static MinMaxFloat Null { get; } = new MinMaxFloat(0, 0);

            public static MinMaxFloat One { get; } = new MinMaxFloat(1, 1);

            public MinMaxFloat(float min, float max)
            {
                this.min = min;
                this.max = max;
            }

            public float Random()
            {
                return UnityEngine.Random.Range(min, max);
            }

            public static bool operator == (MinMaxFloat a, MinMaxFloat b)
            {
                return a.Equals(b);
            }

            public static bool operator != (MinMaxFloat a, MinMaxFloat b)
            {
                return !a.Equals(b);
            }

            public bool Equals(MinMaxFloat v)
            {
                return min == v.min && max == v.max;
            }

            public override bool Equals(object obj)
            {
                if (obj == null || GetType() != obj.GetType())
                    return false;    

                return Equals((MinMaxFloat)obj);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();  
            }
        }

        #endregion

        #region Debug

        private static void CPrint(string s)
        {
            _instance?.Puts(s);
        }

        private static void CWarn(string s)
        {
            _instance?.PrintWarning(s);
        }

        public static void CErr(string s)
        {
            _instance?.PrintError(s);
        }

        private static void DPrint(string s)
        {
            #if !DEBUG
            if (!Flags?.Debug ?? true) return;
            #endif
            _instance?.Puts("[DEBUG] " + s);
        }

        private static void DWarn(string s)
        {
            #if !DEBUG
            if (!Flags?.Debug ?? true) return;
            #endif
            _instance?.PrintWarning("[DEBUG] " + s);
        }

        private static void DErr(string s)
        {
            #if !DEBUG
            if (!Flags?.Debug ?? true) return;
            #endif
            _instance?.PrintError("[DEBUG] " + s);
        }

        #endregion

    }

}

#region Static Lootables

namespace Oxide.Plugins.LoottableExtensions.StaticLootableModels
{
    public class LootableDefinition
    {
        [JsonIgnore]
        public string Uid => PrefabFilter + ':' + (UniqueId ?? "0");
        [JsonIgnore]
        public List<Loottable.LootItem> Items { get; private set; }

        // Has to be lowercase to prevent api issue
        [JsonProperty("uniqueId", NullValueHandling = NullValueHandling.Include)]
        public string UniqueId { get; set; }
        public string PrefabFilter { get; set; }

        public int InteractionIndex { get; set; }// = 0;
        public int ContainerSize { get; set; } = 4;
        public bool AllowStack { get; set; } = false;
        public bool Liquid { get; set; } = false;
        public bool Persistent { get; set; } = false;
        public float Timer { get; set; } = 0f;
        public RootLock Lock { get; set; } = null;
        public RootHack Hack { get; set; } = null;
        public Rule Rule { get; set; } = new Rule();
        [JsonProperty]
        private List<RootLootableItemDefinition> Contents { get; set; } = new();

        public void LoadItems()
        {
            if (Contents != null)
            {
                Items = Contents.Select(i => i.ToLootItem()).ToList();
            }
            else
            {
                Items = new();
            }
        }

        public void SaveItems()
        {
            Contents = Items.Select(i => RootLootableItemDefinition.FromLootItem(i)).ToList();
        }

        public void AddItem(Loottable.LootItem new_item, bool replace_old = false, Loottable.LootItem old_item = null)
        {
            // Replace old item
            if (replace_old)
            {
                var old_itms = Items.FindAll(x => x.SkinItemId == old_item.SkinItemId);
                foreach (var old_itm in old_itms)
                {
                    Items.Remove(old_itm);
                }
            }

            // Remove conflicting items
            var conflict_items = Items.FindAll(x => x.SkinItemId == new_item.SkinItemId);
            foreach (var conflict in conflict_items)
            {
                Items.Remove(conflict);
            }

            // Add new item
            Items.Add(new_item);

            SaveItems();
        }

        public void RemoveItem(string skinItemId)
        {
            var old_items = Items.FindAll(x => x.SkinItemId == skinItemId);
            foreach (var old_item in old_items)
            {
                Items.Remove(old_item);
            }

            SaveItems();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"Uid: {Uid}\n");
            sb.Append($"UniqueId: {UniqueId}\n");
            sb.Append($"PrefabFilter: {PrefabFilter}");
            return sb.ToString();
        }

        public LootableDefinition Clone()
        {
            SaveItems();
            string serialized = JsonConvert.SerializeObject(this);
            var clone = JsonConvert.DeserializeObject<LootableDefinition>(serialized);
            clone.LoadItems();
            return clone;
        }
    }
    public class RootLock
    {
        public float Health { get; set; } = 250f;
    }
    public class RootHack
    {
        [JsonProperty("Wait Time (in Seconds)")]
        public float WaitTime { get; set; } = 10f;

        [JsonProperty("Code Resetting Rate (in Minutes)")]
        public float CodeResetRate { get; set; } = 300f;

        [JsonIgnore] public string Code { get; set; } = "0000";
        [JsonIgnore] public bool IsHacking { get; set; } = false;
        [JsonIgnore] public long HackStartTick { get; set; }
        [JsonIgnore] public Timer HackingTimer { get; set; }
        [JsonIgnore] public int HackedTimes { get; set; } = 1;
    }
    public class RootLootableItemDefinition
    {
        public string ShortName { get; set; }
        public string CustomName { get; set; }
        public ulong SkinId { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }
        public bool UseRandomSkins { get; set; }
        public ulong[] RandomSkins { get; set; } = new ulong[0];
        public int MinimumAmount { get; set; } = 1;
        public int MaximumAmount { get; set; } = 1;
        public float ConditionMinimumAmount { get; set; } = 1;
        public float ConditionMaximumAmount { get; set; } = 1;
        public int SpawnChanceTimes { get; set; } = 2;
        public int SpawnChanceScale { get; set; } = 5;
        public List<RootLootableItemDefinition> Contents { get; set; } = new List<RootLootableItemDefinition>();

        public Loottable.LootItem ToLootItem()
        {
            float chance = ((float)SpawnChanceTimes) / ((float)SpawnChanceScale);
            return new Loottable.LootItem(ShortName, MinimumAmount, MaximumAmount, chance, SkinId, 0, CustomName);
        }

        public static RootLootableItemDefinition FromLootItem(Loottable.LootItem item)
        {
            return new RootLootableItemDefinition
            {
                ShortName = item.Shortname,
                CustomName = item.displayname,
                SkinId = item.skin,
                MinimumAmount = item.amount.min,
                MaximumAmount = item.amount.max,
                SpawnChanceScale = item.chance < 0.01f ? 1000 : 100,
                SpawnChanceTimes = item.chance < 0.01f ? (int)(item.chance * 1000f) : (int)(item.chance * 100f),
                UseRandomSkins = false
            };
        }
    }
    public class Rule
    {
        public float RefillRate { get; set; } = 40f;
        public List<string> OnlyIfParentFilter { get; set; } = new List<string>();
        public List<string> OnlyIfNotParentFilter { get; set; } = new List<string>();
        public List<string> OnlyIfInZone { get; set; } = new List<string>();
        public List<string> OnlyIfNotInZone { get; set; } = new List<string>();
    }

    public static class Debug
    {
        public static bool TryGetClosestRayPoint(Ray ray, out Collider closestEnt)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray);
            float closestdist = 10f;
            closestEnt = null;

            foreach (var hit in hits)
            {
                var colliderName = hit.collider.name.ToLower().Trim();
                if (hit.distance < closestdist
                    && !colliderName.Contains("preventbuilding")
                    && !colliderName.Contains("prevent_building")
                    && !colliderName.Contains("prevent_movement")
                    && !colliderName.Contains("terrain")
                    && !colliderName.Contains("player.prefab"))
                {
                    closestdist = hit.distance;
                    closestEnt = hit.collider;
                    return true;
                }
            }

            return false;
        }
    }
}

#endregion

#region Extension Methods

namespace Oxide.Plugins.LoottableExtensions
{
    public static class ReflectionEx
    {
        public static int GetLootTypeIndex(this TrainCarUnloadable obj)
        {
            #if CARBON
            return obj.lootTypeIndex;
            #endif

            FieldInfo field = typeof(TrainCarUnloadable).GetField("lootTypeIndex", BindingFlags.Instance | BindingFlags.NonPublic);

            if (field == null)
            {
                Loottable.CErr($"Failed to get field TrainCarUnloadable.lootTypeIndex");
                return -1;
            }

            return (int)field.GetValue(obj);
        }

        public static void SetLootTypeIndex(this TrainCarUnloadable obj, int value)
        {
            #if CARBON
            obj.lootTypeIndex = value;
            return;
            #endif

            FieldInfo field = typeof(TrainCarUnloadable).GetField("lootTypeIndex", BindingFlags.Instance | BindingFlags.NonPublic);

            if (field == null)
            {
                Loottable.CErr($"Failed to get field TrainCarUnloadable.lootTypeIndex");
                return;
            }

            field.SetValue(obj, value);
        }
    }

    public static class Extensions
    {
        public static List<T> GetRange2<T>(this List<T> list, int start, int count)
        {
            if (start < 0 || start >= list.Count)
            {
                throw new IndexOutOfRangeException("Start index must be non-negative and smaller than the size of the array");
            }

            int length = Mathf.Min(count, list.Count - start);

            var result = new List<T>(length);
            result.AddRange(list.GetRange(start, length));

            return result;
        }

        public static bool IsOneOf<T>(this T value, params T[] values) where T : struct
        {
            if (!typeof(T).IsEnum) throw new NotSupportedException("Only enums are supported");

            foreach(var val in values)
            {
                if (value.Equals(val)) return true;
            }

            return false;
        }

        public static string Join(this IEnumerable<string> arr) => arr.Join(string.Empty);

        public static string Join(this IEnumerable<string> arr, string separator)
        {
            string result = "";
            foreach(string v in arr)
            {
                if (result != "")
                    result += separator;
                result += v;
            }
            return result;
        }

        public enum DuplicateKeyHandling { Replace, Skip, Error }

        public static Dictionary<Tk, Tv> MergeWith<Tk, Tv>(this Dictionary<Tk, Tv> source, Dictionary<Tk, Tv> dict, DuplicateKeyHandling duplicateKeyHandling = DuplicateKeyHandling.Replace)
        {
            var result = new Dictionary<Tk, Tv>(source);

            foreach(var kv in dict)
            {
                if (dict.ContainsKey(kv.Key))
                {
                    if (duplicateKeyHandling == DuplicateKeyHandling.Skip)
                        continue;
                    if (duplicateKeyHandling == DuplicateKeyHandling.Error)
                        throw new Exception("Duplicate key detected when merging dictionaryies");
                }

                result[kv.Key] = kv.Value;
            }

            return result;
        }

        public static IEnumerable<T> GetRandomElements<T>(this IEnumerable<T> list, int amount)
        {
            int count = list.Count();
            if (amount <= 0) return new List<T>();
            if (amount > count) amount = count;
            if (amount == count) return list;

            List<T> elements = new List<T>();
            List<T> all = list.ToList();

            for(int i = 0; i<amount; i++)
            {
                int idx = UnityEngine.Random.Range(0, all.Count);
                elements.Add(all[idx]);
                all.RemoveAt(idx);
            }

            return elements;
        }

        public static T Clone<T>(this object objSource)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, objSource);
                stream.Position = 0;
                return (T)formatter.Deserialize(stream);
            }
        }
     
        public static T[] RemoveDuplicates<T>(this T[] array)
        {
            List<T> temp = new List<T>();
            foreach (var x in array)
            {
                if (!temp.Contains(x))
                    temp.Add(x);
            }
            return temp.ToArray();
        }

        public static T[] Trim<T>(this T[] array, int lower_bound = 0, int upper_bound = 0)
        {
            var temp = array.ToList();
            if (lower_bound > 0)
                temp.RemoveRange(0, lower_bound);
            if (upper_bound > 0)
                temp.RemoveRange(upper_bound, temp.Count - 1);
            return temp.ToArray();
        }

        public static BasePlayer ToBasePlayer(this IPlayer iplayer)
        {
            if (iplayer.IsServer) return null;
            return iplayer.Object as BasePlayer;
        }

        public static bool ToBasePlayer(this IPlayer iplayer, out BasePlayer player)
        {
            player = null;
            if (iplayer.IsServer) return false;

            player = iplayer.Object as BasePlayer;
            if (player == null) return false;
            return true;
        }

        public static void ForceClear(this ItemContainer container)
        {
            container.Clear();
            ItemManager.DoRemoves();
        }

        public static string AddSpacesBeforeUppercase(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";
            StringBuilder newText = new StringBuilder(text.Length * 2);
            newText.Append(text[0]);
            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]) && text[i - 1] != ' ')
                    newText.Append(' ');
                newText.Append(text[i]);
            }
            return newText.ToString();
        }

    }
}

#endregion