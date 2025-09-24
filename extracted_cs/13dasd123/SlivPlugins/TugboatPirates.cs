using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Oxide.Plugins.TugboatPiratesExt;
using Rust;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

/**
 * 
 * VERSION HISTORY
 * 
 * V 1.1.0
 * - clean up default config
 * - add hooks for event start and end
 * - fix config spelling error
 * - fix path finding issues
 * - fix bumping noise of locked crate
 * - add config option for boat speed
 * - add support for notify
 * - make boat visible across the map by default
 * - add config option to remove npc corpses
 * - fix conflict with Loottable
 * - add config option for map markers
 * 
 * V 1.2.0
 * - prevent decay of event tugboats
 * - stop boat when it is destroyed
 * - kill npcs when boat is sinking
 * - add config option to prevent damage to boat
 * - display sinking time to players near the boat
 * - configurable sinking time after captain has been killed
 * - configurable sinking time after crate has been looted
 * - fix captain mounting issues
 * - prevent players from mounting boat
 * - fix loot conflicts with CustomLoot
 * - add config option for notify notification type
 * - add config option for crate timer
 * - support pvp zones with TruePVE
 * - fix boats remaining after event
 * 
 * V 1.2.1
 * - change chat commands to universal commands
 * - fix hook conflicts with TruePVE
 * 
 * V 1.2.2
 * - fix CanEntityTakeDamage hook conflict with raidable bases
 * - fix captain's note missing sometimes
 * 
 * V 1.2.3
 * - fix parenting issues with npcs
 * 
 * V 1.2.4
 * - fix rigidbody error message
 * 
 */

namespace Oxide.Plugins
{
    [Info(nameof(TugboatPirates), "The_Kiiiing", "1.2.4")]
    internal class TugboatPirates : RustPlugin
    {
        #region Fields

        private const string PERM_ADMIN = "tugboatpirates.admin";

        private const string GUI_CONTAINER = "tugboatpirates.ui";

        // Change at your own risk
        private const float MAP_SIZE_SCALE = 0.55f;
        private const float NODE_TOLERANCE = 80f;

        [PluginReference]
        private Plugin NpcSpawn, Notify;

        private static TugboatPirates _instance;

        private Timer nextEventTimer;
        private TugboatController currentBoat;

        private readonly Dictionary<ulong, ValueTuple<string, bool>> npcProfiles = new Dictionary<ulong, ValueTuple<string, bool>>();
        private readonly Dictionary<ulong, string> captainNoteTexts = new Dictionary<ulong, string>();

        private static BasePlayer startPlayer;

        private readonly IReadOnlyDictionary<string, Vector3> npcSpawnPoints = new Dictionary<string, Vector3>
        {
            // Back low
            ["back_right"] = new Vector3(2f, 2.69f, -8.8f),
            ["back_left"] = new Vector3(-2f, 2.69f, -8.8f),

            // Upper front
            ["upper_front_right"] = new Vector3(2f, 4.6f, 5f),
            ["upper_front_left"] = new Vector3(-2f, 4.6f, 5f),

            // Upper back
            ["upper_back_right"] = new Vector3(2.8f, 4.6f, -5f),
            ["upper_back_left"] = new Vector3(-2.8f, 4.6f, -5f),

            // Back roof
            ["roof_back"] = new Vector3(0, 7.2f, -1.3f),

            // Entrance
            ["entrance_right"] = new Vector3(2.7f, 5.7f, 1.4f),
            ["entrance_left"] = new Vector3(-2.7f, 5.7f, 1.4f),

            // Roof
            ["roof_right"] = new Vector3(1.4f, 8.6f, 3.2f),
            ["roof_left"] = new Vector3(-1.4f, 8.6f, 3.2f),

            // Front
            ["front"] = new Vector3(0f, 2f, 9.4f),

            // Sides
            ["right"] = new Vector3(3.6f, 2f, 0f),
            ["left"] = new Vector3(-3.6f, 2f, 0f),
        };

        #endregion

        #region Configuration

        private static Configuration _config;

        private class Configuration
        {
            [JsonProperty("Time between events (minutes, set to -1 to disable scheduled events)")]
            public int eventDelay = 60;

            [JsonProperty("Event duration (seconds)")]
            public int eventDuration = 3600;

            [JsonProperty("Show toast when event starts")]
            public bool showToast = true;

            [JsonProperty("Announce event in chat")]
            public bool announceChat = true;

            [JsonProperty("Use Notify")]
            public bool notifyEnabled = false;

            [JsonProperty("Notify notification type")]
            public int notificationType = 0;

            [JsonProperty("Boat leave time before despawning (seconds, boat will return if value is too big)")]
            public int leaveTime = 90;

            [JsonProperty("Time before boat sinks after captain is killed (seconds)")]
            public int timeBeforeSinkingCaptain = 1200;

            [JsonProperty("Time before boat sinks after crate has been looted (seconds)")]
            public int timeBeforeSinkingLooted = 300;

            [JsonProperty("Time before boat is destroyed after sinking (seconds)")]
            public int destroyTime = 60;

            [JsonProperty("Render boat across the map (might impact client performance)")]
            public bool boatGlobalBroadcast = true;

            [JsonProperty("Create PVP zone around boat (requires TruePVE)")]
            public bool enablePvp = false;

            [JsonProperty("Zone radius (smaller than 25 not recommended)")]
            public float zoneRadius = 25f;

            [JsonProperty("Zone darkness (0 = invisible, higher value = darker)")]
            public int zoneDarkness = 0;

            [JsonProperty("Disable damage to pirate tugboat")]
            public bool disableDamage = true;

            [JsonProperty("Crate hack time (seconds, -1 for default time)")]
            public int crateTimerOverride = -1;

            [JsonProperty("Boat configuration", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public BoatConfig boatConfig = new BoatConfig
            {
                captainNpcProfile = "captain",
                npcSpawnProfiles = new Dictionary<string, string>
                {
                    ["back_right"] = "pirate_lr",
                    ["back_left"] = "pirate_lr",

                    ["upper_front_right"] = "pirate_lr",
                    ["upper_front_left"] = "pirate_lr",

                    ["upper_back_right"] = "pirate_lr",
                    ["upper_back_left"] = "pirate_lr",

                    ["roof_back"] = "pirate_lr",

                    ["entrance_right"] = "pirate_lr",
                    ["entrance_left"] = "pirate_lr",

                    ["roof_right"] = "pirate_lr",
                    ["roof_left"] = "pirate_lr",

                    ["front"] = "pirate_lr",

                    ["right"] = "pirate_mp5",
                    ["left"] = "pirate_mp5",
                },
                interior = new List<InteriorObject>
                {
                    new InteriorObject
                    {
                        prefabPath = "assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab",
                        //prefabPath = "assets/prefabs/deployable/large wood storage/box.wooden.large.prefab",
                        localPosition = new Vector3(0, 2f, 4.2f),
                        rotationY = 180f,
                        lootProfile = "",
                        skinId = 1394363785
                    },
                    new InteriorObject
                    {
                        prefabPath = "assets/prefabs/deployable/research table/researchtable_deployed.prefab",
                        localPosition = new Vector3(2.1f, 2f, -4f),
                        rotationY = -90f
                    },
                    new InteriorObject
                    {
                        prefabPath = "assets/prefabs/deployable/sofa/sofa.deployed.prefab",
                        localPosition = new Vector3(-1.9f, 2, -2.2f),
                        rotationY = 90f
                    },
                    new InteriorObject
                    {
                        prefabPath = "assets/prefabs/deployable/large wood storage/box.wooden.large.prefab",
                        localPosition = new Vector3(-1.9f, 2, -4.2f),
                        rotationY = 90f,
                        lootProfile = "crate_2",
                        skinId = 811157743ul
                    },
                    new InteriorObject
                    {
                        prefabPath = "assets/prefabs/deployable/bed/bed_deployed.prefab",
                        localPosition = new Vector3(2f, 2f, -1.8f),
                        rotationY = -90f
                    },
                    new InteriorObject
                    {
                        prefabPath = "assets/bundled/prefabs/radtown/crate_normal.prefab",
                        localPosition = new Vector3(-1.9f, 2f, 1f),
                        rotationY = 90f,
                        lootProfile = "crate_2"
                    },
                    new InteriorObject
                    {
                        prefabPath = "assets/prefabs/deployable/table/table.deployed.prefab",
                        localPosition = new Vector3(0.75f, 2f, 0.62f),
                        rotationY = 225f
                    },
                    new InteriorObject
                    {
                        prefabPath = "assets/bundled/prefabs/radtown/crate_basic.prefab",
                        localPosition = new Vector3(0.3f, 2f, 0.92f),
                        rotationY = 200f,
                        lootProfile = "crate_2"
                    },
                    new InteriorObject
                    {
                        prefabPath = "assets/prefabs/deployable/secretlab chair/secretlabchair.deployed.prefab",
                        localPosition = new Vector3(1.8f, 2f, 1.9f),
                        rotationY = 210f
                    },
                    new InteriorObject
                    {
                        prefabPath = "assets/prefabs/building/door.hinged/door.hinged.toptier.prefab",
                        localPosition = new Vector3(0.00f, 2.03f, -5.34f),
                        rotationY = 270f,
                        codelocked = true,
                        skinId = 850289896
                    },
                    new InteriorObject
                    {
                        prefabPath = "assets/prefabs/misc/permstore/factorydoor/door.hinged.industrial.d.prefab",
                        localPosition = new Vector3(2.00f, 5.75f, 1.39f),
                        rotationY = 180f,
                    },
                    new InteriorObject
                    {
                        prefabPath = "assets/prefabs/misc/permstore/factorydoor/door.hinged.industrial.d.prefab",
                        localPosition = new Vector3(-2.00f, 5.75f, 1.39f),
                        rotationY = 0f,
                    },
                    new InteriorObject
                    {
                        prefabPath = "assets/content/structures/excavator/prefabs/diesel_collectable.prefab",
                        localPosition = new Vector3(-1.8f, 2f, 3f),
                        rotationY = 110f,
                    }
                }
            };

            [JsonProperty("Npc profiles", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, NpcConfig> npcProfiles = new Dictionary<string, NpcConfig>
            {
                ["pirate_lr"] = new NpcConfig
                {
                    name = "Pirate",
                    health = 200f,
                    enableRadio = true,
                    senseRange = 50f,
                    memoryDuration = 60f,
                    visionCone = 135f,
                    damageScale = 1f,

                    clothing = new List<LootItem> {
                        new LootItem { shortName = "hat.boonie", skinId = 965553937ul },
                        new LootItem { shortName = "hoodie", skinId = 2984978438ul },
                        new LootItem { shortName = "pants", skinId = 2984977257ul },
                        new LootItem { shortName = "attire.hide.boots", skinId = 861468674ul },
                    },
                    belt = new List<LootItem>
                    {
                        new LootItem{ shortName = "rifle.lr300" }
                    },

                    kit = string.Empty,
                    lootProfile = "pirate"
                },

                ["pirate_mp5"] = new NpcConfig
                {
                    name = "Pirate",
                    health = 150f,
                    enableRadio = true,
                    senseRange = 50f,
                    memoryDuration = 60f,
                    visionCone = 135f,
                    damageScale = 1f,

                    clothing = new List<LootItem> {
                        new LootItem { shortName = "hat.boonie", skinId = 965553937ul },
                        new LootItem { shortName = "hoodie", skinId = 2984978438ul },
                        new LootItem { shortName = "pants", skinId = 2984977257ul },
                        new LootItem { shortName = "attire.hide.boots", skinId = 861468674ul },
                    },
                    belt = new List<LootItem>
                    {
                        new LootItem{ shortName = "smg.mp5" }
                    },

                    kit = string.Empty,
                    lootProfile = "pirate"
                },

                ["captain"] = new NpcConfig
                {
                    name = "Captain",
                    health = 100f,
                    enableRadio = false,
                    senseRange = 0f,
                    memoryDuration = 0f,
                    visionCone = 0f,
                    damageScale = 1f,

                    clothing = new List<LootItem> {
                        new LootItem { shortName = "hat.boonie", skinId = 965553937ul },
                        new LootItem { shortName = "tshirt", skinId = 811762477ul },
                        new LootItem { shortName = "pants.shorts", skinId = 849256923ul },
                        new LootItem { shortName = "attire.hide.boots", skinId = 861468674ul },
                    },
                    belt = new List<LootItem>
                    {
                        new LootItem { shortName = "mace.baseballbat" }
                    },

                    kit = string.Empty,
                    lootProfile = "pirate"
                }
            };

            [JsonProperty("Loot profiles", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, List<LootManager.LootItem>> lootProfiles = new Dictionary<string, List<LootManager.LootItem>>
            {
                ["crate_main"] = new List<LootManager.LootItem>
                {
                    new LootManager.LootItem("scrap", 8, 20, 1f),
                    new LootManager.LootItem("metal.refined", 10, 25, 0.7f),

                    new LootManager.LootItem("lmg.m249", 1, 1, 0.05f),
                    new LootManager.LootItem("rifle.ak.diver", 1, 1, 0.1f),
                    new LootManager.LootItem("rifle.bolt", 1, 1, 0.1f),
                    new LootManager.LootItem("smg.mp5", 1, 1, 0.2f),
                    new LootManager.LootItem("smg.thompson", 1, 1, 0.2f),

                    new LootManager.LootItem("ammo.shotgun", 4, 8, 0.2f),
                    new LootManager.LootItem("ammo.shotgun.fire", 4, 8, 0.2f),
                    new LootManager.LootItem("ammo.shotgun.slug", 4, 8, 0.2f),
                    new LootManager.LootItem("ammo.pistol", 15, 30, 0.2f),
                    new LootManager.LootItem("ammo.pistol.hv", 15, 30, 0.2f),
                    new LootManager.LootItem("ammo.pistol.fire", 15, 30, 0.2f),
                    new LootManager.LootItem("ammo.rifle", 12, 24, 0.2f),
                    new LootManager.LootItem("ammo.rifle.hv", 12, 24, 0.2f),
                    new LootManager.LootItem("ammo.rifle.incendiary", 12, 24, 0.2f),
                },
                ["crate_2"] = new List<LootManager.LootItem>
                {
                    new LootManager.LootItem("scrap", 2, 20, 1f),
                    new LootManager.LootItem("metal.refined", 4, 8, 0.5f),
                    new LootManager.LootItem("gears", 1, 3, 0.2f),
                    new LootManager.LootItem("sewingkit", 1, 3, 0.2f),
                    new LootManager.LootItem("rope", 1, 3, 0.2f),
                    new LootManager.LootItem("sheetmetal", 1, 2, 0.2f),

                    new LootManager.LootItem("grenade.molotov", 1, 2, 0.1f),
                    new LootManager.LootItem("grenade.f1", 1, 4, 0.1f),
                    new LootManager.LootItem("telephone", 1, 1, 0.1f),
                    new LootManager.LootItem("multiplegrenadelauncher", 1, 1, 0.1f),
                },
                ["pirate"] = new List<LootManager.LootItem>
                {
                    new LootManager.LootItem("scrap", 2, 6, 1f),
                    new LootManager.LootItem("bottle.vodka", 1, 1, 0.7f),
                    new LootManager.LootItem("pistol.eoka", 1, 1, 0.2f),
                    new LootManager.LootItem("ammo.handmade.shell", 5, 10, 0.2f),
                    new LootManager.LootItem("rope", 1, 3, 0.3f),
                    new LootManager.LootItem("sewingkit", 1, 2, 0.3f),
                },
            };

            [JsonProperty("Priate quotes (included in captain's note)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> pirateQuotes = new List<string>
            {
                "If rum can’t fix it, ye are not using enough rum.",
                "But… why is the rum gone?",
                "Be who you arrrr...",
                "All for rum and rum for all!",
                "Land was created to provide a place for boats to visit.",
                "If ye can read this ye be stupid."
            };

            public void RemoveUnsupportedItems()
            {
                boatConfig.interior.RemoveAll(x => x.prefabPath == "assets/bundled/prefabs/radtown/oil_barrel.prefab" || x.prefabPath == "assets/prefabs/deployable/waterpurifier/waterpurifier.deployed.prefab");
            }
        }

        private class BoatConfig
        {
            [JsonProperty("Npc profile for captain (must be a valid profile)")]
            public string captainNpcProfile;

            [JsonProperty("Boat speed multiplier")]
            public float speedMultiplier = 1f;

            [JsonProperty("Enable map marker")]
            public bool useMapMarker = true;

            [JsonProperty("Map marker color (hex format)")]
            public string markerColor = "#2480FB";

            [JsonProperty("Map marker name")]
            public string markerName = "Pirate Ship";

            [JsonProperty("Npc spawn locations and profiles", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, string> npcSpawnProfiles = new Dictionary<string, string>();

            [JsonProperty("Interior objects (crates, decoration, etc.)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<InteriorObject> interior = new List<InteriorObject>();
        }

        private class InteriorObject
        {
            [JsonProperty("Prefab path")]
            public string prefabPath;
            [JsonProperty("Rotation")]
            public float rotationY;
            [JsonProperty("Position on boat")]
            public Vector3 localPosition;
            [JsonProperty("Skin id")]
            public ulong skinId;
            [JsonProperty("Loot profile (only for crates, leave empty for default loot)", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string lootProfile = string.Empty;
            [JsonProperty("Add code lock (only for doors)", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool? codelocked = null;
        }

        private class NpcConfig
        {
            public string name = "Pirate";
            public float health = 150f;
            public bool enableRadio = true;

            public float senseRange = 50f;
            public float visionCone = 135f;
            public float damageScale = 1f;
            public float memoryDuration = 60f;

            public bool removeCorpseAfterDeath = false;

            public string lootProfile = string.Empty;

            public string kit = string.Empty;

            [JsonProperty("Clothing items", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<LootItem> clothing = new List<LootItem>();
            [JsonProperty("Belt items", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<LootItem> belt = new List<LootItem>();
        }

        private class LootItem
        {
            public string shortName;
            public int amount = 1;
            public ulong skinId;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null)
                {
                    throw new Exception();
                }

                _config.RemoveUnsupportedItems();
                SaveConfig();
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        protected override void LoadDefaultConfig() => _config = new Configuration();

        #endregion

        #region Loot Manager

        private static class LootManager
        {
            public static void FillWithLoot(ItemContainer container, List<LootItem> lootTable)
            {
                ClearContainer(container);

                int amt = 0;
                foreach (var itm in lootTable)
                {
                    if (UnityEngine.Random.Range(0f, 1f) <= itm.chance)
                    {
                        var item = itm.CreateItem();
                        if (item == null) continue;
                        if (!item.MoveToContainer(container)) item.Remove();
                        amt++;
                    }
                    if (amt >= container.capacity) break;
                }
            }

            public static void FillWithLoot(StorageContainer container, List<LootItem> lootTable) => FillWithLoot(container.inventory, lootTable);

            private static void ClearContainer(ItemContainer container)
            {
                container.itemList.Clear();
                container.OnChanged();
                ItemManager.DoRemoves();
            }

            public class LootItem
            {
                [JsonProperty("Short name")]
                public string shortname;
                [JsonProperty("Min amount")]
                public int min;
                [JsonProperty("Max amount")]
                public int max;
                [JsonProperty("Chance")]
                public float chance;
                [JsonProperty("Skin id")]
                public ulong skin = 0;
                [JsonIgnore]
                public string text;

                [JsonIgnore]
                public ItemDefinition ItemDefinition => ItemManager.FindItemDefinition(shortname);

                public LootItem()
                {
                    shortname = "scrap";
                    min = 5; max = 10;
                    chance = 1f;
                    skin = 0;
                }

                public LootItem(string shortname, int min, int max, float chance)
                {
                    this.shortname = shortname;
                    this.min = min;
                    this.max = max;
                    this.chance = chance;
                }

                public LootItem(string shortname, int min, int max, float chance, ulong skin)
                {
                    this.shortname = shortname;
                    this.min = min;
                    this.max = max;
                    this.chance = chance;
                    this.skin = skin;
                }

                public Item CreateItem()
                {
                    var itm = ItemManager.Create(ItemDefinition, UnityEngine.Random.Range(min, max + 1), skin);
                    if (text != null)
                    {
                        itm.text = text;
                    }
                    itm?.OnVirginSpawn();
                    return itm;
                }
            }
        }

        #endregion

        #region Commands

        void CmdStart(IPlayer iplayer)
        {
            if (!permission.UserHasPermission(iplayer.Id, PERM_ADMIN))
            {
                iplayer.Reply("You don't have permission to do that");
                return;
            }

            StartEvent(iplayer.Object as BasePlayer, true);
        }

        void CmdStop(IPlayer iplayer)
        {
            if (!permission.UserHasPermission(iplayer.Id, PERM_ADMIN))
            {
                iplayer.Reply("You don't have permission to do that");
                return;
            }

            EndEvent(true);
            iplayer.Reply("The event has ended");
        }

        #endregion

        #region Hooks

        void Init()
        {
            _instance = this;

            permission.RegisterPermission(PERM_ADMIN, this);

            AddCovalenceCommand("tugboatstart", nameof(CmdStart));
            AddCovalenceCommand("tugboatstop", nameof(CmdStop));

            Unsubscribe();
        }

        void OnServerInitialized()
        {
            TugboatPathFinder.GeneratePatrolPath();

            if (NpcSpawn == null)
            {
                Cerr("NpcSpawn is required to spawn NPCs. You can download it here: https://codefling.com/extensions/npc-spawn");
            }

            ScheduleEvent(0.5f);
        }


        void Unload()
        {
            currentBoat?.Destroy();
            nextEventTimer?.Destroy();

            _instance = null;
        }

        #region Event Hooks

        void OnCrateHack(HackableLockedCrate crate)
        {
            if (_config.crateTimerOverride >= 0 && currentBoat?.crates.Contains(crate) == true)
            {
                crate.hackSeconds = HackableLockedCrate.requiredHackSeconds - _config.crateTimerOverride;
            }
        }

        object CanMountEntity(BasePlayer player, BaseMountable entity)
        {
            if (!player.IsNpc && entity.GetParentEntity()?.net.ID == currentBoat?.Boat.net.ID)
            {
                return false;
            }

            return null;
        }

        void OnLootEntityEnd(BasePlayer player, HackableLockedCrate crate)
        {
            currentBoat?.OnCrateLooted(crate);
        }

        object OnEntityTakeDamage(Tugboat boat, HitInfo info)
        {
            if (_config.disableDamage && boat.net.ID == currentBoat?.Boat.net.ID)
            {
                return true;
            }

            return null;
        }

        void OnCorpsePopulate(ScientistNPC npc, NPCPlayerCorpse corpse)
        {
            if (npc == null || corpse == null)
            {
                return;
            }

            ulong netId = npc.net.ID.Value;
            ValueTuple<string, bool> profile;
            if (npcProfiles.TryGetValue(netId, out profile))
            {
                string captainNoteText = captainNoteTexts.GetValueOrDefault(netId, null);
                var lootTable = GetLootProfile(profile.Item1, captainNoteText);
                if (lootTable != null)
                {
                    _instance.NextFrame(() =>
                    {
                        if (!corpse.IsValid())
                        {
                            Cerr($"Failed to populate NPC corpse ({npc?.displayName}) - invalid corpse");
                            return;
                        }

                        LootManager.FillWithLoot(corpse.containers.First(), lootTable);

                        // Drop bag
                        if (profile.Item2 && !corpse.IsDestroyed)
                        {
                            corpse.Kill();
                        }
                    });
                }

                npcProfiles.Remove(netId);
                captainNoteTexts.Remove(netId);
            }
        }

        // Loottable
        object OnCorpsePopulate(LootableCorpse corpse)
        {
            var scientistNet = corpse.parentEnt.net;
            if (scientistNet != null && npcProfiles.ContainsKey(scientistNet.ID.Value))
            {
                Dprint("prevent populate corpse (Loottable)");
                return false;
            }

            return null;
        }

        // CustomLoot
        object OnCustomLootNPC(ulong netId)
        {
            if (npcProfiles.ContainsKey(netId))
            {
                Dprint("prevent populate corpse (Custom Loot)");
                return false;
            }

            return null;
        }

        // TruePVE
        object CanEntityTakeDamage(BasePlayer target, HitInfo info)
        {
            if (!_config.enablePvp || currentBoat == null)
            {
                return null;
            }

            BasePlayer attacker = info?.InitiatorPlayer;
            if (attacker?.IsNpc == false && target?.IsNpc == false)
            {
                return (currentBoat.Zone.Players.Contains(attacker) && currentBoat.Zone.Players.Contains(target)) ? true : null;
            }

            return null;
        }

        #endregion

        void OnTugboatPiratesEnded()
        {
            Unsubscribe();
            ScheduleEvent();
        }

#if DEBUG
        object OnHammerHit(BasePlayer player, HitInfo info)
        {
            var entity = info.HitEntity;
            var parent = entity?.GetParentEntity();
            if (entity != null && parent != null)
            {
                Dprint($"Local pos {entity.transform.localPosition} local rot {entity.transform.localEulerAngles.y:N0}");
                Dprint($"Calculated local pos {entity.transform.position - parent.transform.position}");
                Dprint($"new InteriorObject\r\n                        {{\r\n                            prefabPath = \"{entity.PrefabName}\",\r\n                            localPosition = new Vector3({entity.transform.localPosition.x:N2}f, {entity.transform.localPosition.y:N2}f, {entity.transform.localPosition.z:N2}f),\r\n                            rotationY = {entity.transform.localEulerAngles.y:N0}f,\r\n                            lootProfile = \"crate_main\"\r\n                        }},");
            }

            return null;
        }
#endif

        private void Subscribe()
        {
            Dprint("Subscribe");

            if (_config.disableDamage)
            {
                Subscribe(nameof(OnEntityTakeDamage));
            }
            if (_config.crateTimerOverride >= 0)
            {
                Subscribe(nameof(OnCrateHack));
            }
            if (_config.enablePvp)
            {
                Subscribe(nameof(CanEntityTakeDamage));
            }

            Subscribe(nameof(OnLootEntityEnd));
            Subscribe(nameof(CanMountEntity));
            Subscribe(nameof(CanMountEntity));
            Subscribe(nameof(OnCorpsePopulate));
            Subscribe(nameof(OnCustomLootNPC));
        }

        private void Unsubscribe()
        {
            Dprint("Unsubscribe");

            Unsubscribe(nameof(OnEntityTakeDamage));
            Unsubscribe(nameof(OnCrateHack));
            Unsubscribe(nameof(OnLootEntityEnd));
            Unsubscribe(nameof(CanMountEntity));
            Unsubscribe(nameof(CanMountEntity));
            Unsubscribe(nameof(OnCorpsePopulate));
            Unsubscribe(nameof(OnCustomLootNPC));
            Unsubscribe(nameof(CanEntityTakeDamage));
        }

        #endregion

        #region Path Finder

        private class TugboatPathFinder
        {
            private int targetNodeIndex = -1;
            private readonly float sqrVisitDistance;

            private int skip;

            private readonly List<Vector3> nodes;

            private bool leave;
            private Vector3 finalDestination;

            public TugboatPathFinder(List<Vector3> path, float visitDistance)
            {
                sqrVisitDistance = visitDistance * visitDistance;
                nodes = path;
                skip = 1;
            }

            public Vector3 GetCurrentNode()
            {
                return nodes[targetNodeIndex];
            }

            public Vector3 GetNextNode()
            {
                int idx = targetNodeIndex + skip;
                if (idx >= nodes.Count)
                {
                    idx = 0;
                }

                return nodes[idx];
            }

            public Vector3 GetRandomStartPosition(float height = 0)
            {
                float outside = TerrainMeta.Size.x * MAP_SIZE_SCALE;

                float x = UnityEngine.Random.Range(-outside, outside);
                float y = UnityEngine.Random.Range(-outside, outside);

                if (UnityEngine.Random.Range(0, 2) == 1)
                {
                    x = outside * Mathf.Sign(x);
                }
                else
                {
                    y = outside * Mathf.Sign(y);
                }

                return new Vector3(x, height, y);
            }

            public void SetFinalDestination()
            {
                finalDestination = GetDespawnPosition(GetCurrentNode());
                leave = true;
            }

            public Vector3 GetCurrentTarget(Vector3 currentPosition)
            {
                if (leave)
                {
                    return finalDestination;
                }

                if (targetNodeIndex < 0)
                {
                    targetNodeIndex = GetClosestNode(currentPosition);
                }

                Vector3 currentTarget = GetCurrentNode();

                if ((currentPosition - currentTarget).sqrMagnitude < sqrVisitDistance)
                {
                    targetNodeIndex += skip;
                }

                ValidateNodeIndex();
                

                return GetCurrentNode();
            }

            public void Reverse()
            {
                skip *= -1;
                targetNodeIndex += skip;
                ValidateNodeIndex();
            }

            private void ValidateNodeIndex()
            {
                if (targetNodeIndex >= nodes.Count)
                {
                    targetNodeIndex = 0;
                }
                else if (targetNodeIndex < 0)
                {
                    targetNodeIndex = nodes.Count - 1;
                }
            }

            private Vector3 GetDespawnPosition(Vector3 lastNode, float height = 0)
            {
                lastNode.y = 0;
                lastNode.Normalize();

                lastNode *= TerrainMeta.Size.x;

                lastNode.y = height;

                return lastNode;
            }

            private int GetClosestNode(Vector3 position)
            {
                int result = 0;
                float num = float.PositiveInfinity;
                for (int i = 0; i < nodes.Count; i++)
                {
                    Vector3 b = nodes[i];
                    float num2 = Vector3.Distance(position, b);
                    if (num2 < num)
                    {
                        result = i;
                        num = num2;
                    }
                }

                return result;
            }

            public static void GeneratePatrolPath(bool force = false)
            {
                if (TerrainMeta.Path.OceanPatrolClose == null || TerrainMeta.Path.OceanPatrolClose.Count < 1 || force)
                {
                    var sw = new Stopwatch();
                    Cwarn("Generating tugboat patrol path (expect some lag)");
                    sw.Start();
                    TerrainMeta.Path.OceanPatrolClose = BaseBoat.GenerateOceanPatrolPath(NODE_TOLERANCE * 1.1f);
                    sw.Stop();
                    Cprint($"Patrol path generated in {sw.Elapsed.TotalSeconds:N2}s");
                }
            }
        }

        #endregion

        #region Functions

        private void ScheduleEvent(float delayMpl = 1f)
        {
            if (_config.eventDelay < 1)
            {
                return;
            }

            Cprint($"Schedule next event in {(_config.eventDelay * delayMpl):N0} min");

            if (nextEventTimer != null && !nextEventTimer.Destroyed)
            {
                nextEventTimer.Destroy();
            }

            nextEventTimer = timer.In(_config.eventDelay * 60 * delayMpl, () => StartEvent(null));
        }

        private void StartEvent(BasePlayer player, bool force = false)
        {
            if (!EndEvent(force))
            {
                player?.ChatMessage("Failed to start event, another event is still running");
                Cprint("Failed to start event, another event is still running");
                return;
            }

            var pathFinder = new TugboatPathFinder(TerrainMeta.Path.OceanPatrolClose, NODE_TOLERANCE);

            #if DEBUG
            startPlayer = player;
            Vector3 startPosition = startPlayer?.transform.position ?? pathFinder.GetRandomStartPosition();
            #else
            Vector3 startPosition = pathFinder.GetRandomStartPosition();
            #endif

            Subscribe();

            var rotation = Quaternion.LookRotation(startPosition - pathFinder.GetCurrentTarget(startPosition)) * Quaternion.Euler(Vector3.up * 180);
            var boat = GameManager.server.CreateEntity("assets/content/vehicles/boats/tugboat/tugboat.prefab", startPosition, rotation) as Tugboat;
            if (boat == null)
            {
                Cerr("Failed to spawn boat - boat is null");
                Unsubscribe();
                return;
            }

            boat.Spawn();

            boat.EnableGlobalBroadcast(_config.boatGlobalBroadcast);
            boat.EnableSaving(false);

            // Prevent OnEntityTakeDamage hook conflict with NpcSpawn
            boat.skinID = 14922524;

            currentBoat = boat.gameObject.AddComponent<TugboatController>();
            currentBoat.PathFinder = pathFinder;
            currentBoat.Spawn(_config.boatConfig);
        }

        private bool EndEvent(bool force)
        {
            startPlayer = null;

            if (currentBoat == null)
            {
                return true;
            }

            if (currentBoat.State == EventState.ENDED || force)
            {
                currentBoat?.Destroy();
                return true;
            }

            return false;
        }

        #endregion

        #region Boat Controller

        public enum EventState {
            PREPARING = 1,
            RUNNING = 2,
            CAPTAIN_DEAD = 4,
            LOOTED = 8,
            LEAVING = 16,
            ENDED = 32,
        }

        private class TugboatController : FacepunchBehaviour
        {
            public EventState State { get; private set; }

            private VendingMachineMapMarker vendingMarker;
            private MapMarkerGenericRadius colorMarker;

            public readonly HashSet<ulong> npcs = new HashSet<ulong>();

            private readonly List<BaseEntity> entities = new List<BaseEntity>();
            public readonly HashSet<HackableLockedCrate> crates = new HashSet<HackableLockedCrate>();

            public Tugboat Boat { get; private set; }
            private BasePlayer captain;

            public TugboatPathFinder PathFinder { get; set; }

            public float speedMultiplier;
            public bool useMarker;
            public string markerColor;
            public string markerName;

            private bool boatDestroyed;
            private bool captainDead;

            private Vector3 target;
            private float turnScale;
            private float currentTurnSpeed;
            private float currentThrottle;
            private Vector3 currentVelocity;

            private string lockCode;

            private int timeRemaining;

            public Zone Zone { get; private set; }

            private bool calledEnd;

            #region Mono

            void Awake()
            {
                enabled = false;
                State = EventState.PREPARING;

                Boat = GetComponent<Tugboat>();
                if (Boat == null)
                {
                    Cerr("Tugboat is null");
                    Destroy(this);
                    return;
                }

                Boat.GetTriggerParent().ParentNPCPlayers = true;

                InvokeRepeating(NetworkUpdate, 1f, 5f);
                #if DEBUG
                InvokeRepeating(DebugPath, 1f, 1f);
                #endif

                CancelInvoke(Boat.BoatDecay);

                CreateZone();
            }

            void Update()
            {
                if (Boat.IsDying && !boatDestroyed)
                {
                    Dprint("ship destroyed");
                    boatDestroyed = true;
                    if (_config.announceChat)
                    {
                        BroadcastLang("ship_destroyed");
                    }

                    StartSinking();
                }

                if (captain != null && captain.IsDead() && !captainDead && !boatDestroyed)
                {
                    Dprint("captain is dead");
                    captainDead = true;
                    OnCaptainKilled();
                }
            }

            void FixedUpdate()
            {
                if (!boatDestroyed && !Boat.EngineOn())
                {
                    Boat.SetFlag(BaseEntity.Flags.Reserved1, true);
                }

                if ((!captainDead && !boatDestroyed) || currentThrottle > 0.001f)
                {
                    UpdateMovement();
                }
            }

            void OnDestroy()
            {
                try
                {
                    EndEvent();

                    StopAllCoroutines();
                    CancelInvoke();

                    Boat.DismountAllPlayers();

                    Zone.Destroy();

                    foreach (var ent in entities)
                    {
                        if (ent != null && !ent.IsDestroyed)
                        {
                            ent.Kill();
                        }
                    }

                    if (!Boat.IsDestroyed)
                    {
                        Boat.Kill();
                    }
                }
                catch(Exception ex)
                {
                    Cerr("Error in OnDestroy " + ex.Message);
                }
            }

            void NetworkUpdate()
            {
                foreach(var ent in crates)
                {
                    ent.SendNetworkUpdate();
                }
            }

            public void Destroy()
            {
                if (Boat?.IsDestroyed == false)
                {
                    Boat.Kill(BaseNetworkable.DestroyMode.Gib);
                }
                else
                {
                    Destroy(this);
                }
            }

            void StartSinking()
            {
                Dprint("start sinking");
                EndEvent();

                if (!Boat.IsDying)
                {
                    Boat.SetFlag(BaseEntity.Flags.Broken, b: true);
                    Boat.repair.enabled = false;
                    boatDestroyed = true;
                }

                CancelInvoke(WaterCheck);
                Invoke(WaterCheck, 10f);

                CancelInvoke(Destroy);
                Invoke(Destroy, _config.destroyTime);
            }

            void WaterCheck()
            {
                Dprint("Water check");

                Boat.SetFlag(BaseEntity.Flags.Reserved1, false);

                foreach (var ent in entities.ToArray())
                {
                    var npc = ent as ScientistNPC;
                    if (npc != null)
                    {
                        Dprint($"Water factor {npc.WaterFactor():N2}");
                        if (npc.WaterFactor() > 0.8f)
                        {
                            npc.Invoke(() => npc.Die(), UnityEngine.Random.Range(0.1f, 2f));
                            entities.Remove(ent);
                        }
                    }
                }
            }

            #endregion

            #region Event

            private void OnCaptainKilled()
            {
                if (_config.announceChat)
                {
                    BroadcastLang("captain_killed", _config.timeBeforeSinkingCaptain.ToString());
                }
                
                State = EventState.CAPTAIN_DEAD;
                timeRemaining = _config.timeBeforeSinkingCaptain;
            }

            public void OnCrateLooted(HackableLockedCrate crate)
            {
                if (State == EventState.LOOTED)
                {
                    return;
                }

                if (crates.Contains(crate))
                {
                    crates.Remove(crate);
                }

                if (crates.Count < 1)
                {
                    if (_config.announceChat)
                    {
                        BroadcastLang("ship_looted", _config.timeBeforeSinkingLooted.ToString());
                    }

                    State = EventState.LOOTED;
                    timeRemaining = _config.timeBeforeSinkingLooted;
                }
            }

            private void StartEvent()
            {
                State = EventState.RUNNING;
                enabled = true;

                timeRemaining = _config.eventDuration;

                StartCoroutine(EventUpdateCoro());

                Dprint("OnTugboatPiratesStarted");
                Interface.CallHook("OnTugboatPiratesStarted");
            }

            private void EndEvent()
            {
                if (calledEnd)
                {
                    return;
                }
                calledEnd = true;

                State = EventState.ENDED;

                foreach (var player in Zone.Players)
                {
                    DestroyGui(player);
                }

                Dprint("OnTugboatPiratesEnded");
                Interface.CallHook("OnTugboatPiratesEnded");
            }

            private IEnumerator EventUpdateCoro()
            {
                bool announcedStart = false;
                bool announcedLeave5m = false;
                bool announcedLeave = false;

                while (timeRemaining > 0 && State != EventState.ENDED)
                {
                    if (State == EventState.RUNNING)
                    {
                        if (!announcedStart)
                        {
                            announcedStart = true;
                            if (_config.announceChat)
                            {
                                BroadcastLang("ship_start", PhoneController.PositionToGridCoord(PathFinder.GetCurrentNode()));
                            }
                            if (_config.notifyEnabled)
                            {
                                BroadcastNotify("ship_start", PhoneController.PositionToGridCoord(PathFinder.GetCurrentNode()));
                            }
                            if (_config.showToast)
                            {
                                ShowToastLang("ship_start_toast");
                            }
                        }

                        if (!announcedLeave5m && timeRemaining <= 300 + _config.leaveTime)
                        {
                            announcedLeave5m = true;
                            if (_config.announceChat)
                            {
                                BroadcastLang("ship_leave_5m");
                            }
                            if (_config.notifyEnabled)
                            {
                                BroadcastNotify("ship_leave_5m");
                            }
                        }

                        if (!announcedLeave && timeRemaining <= _config.leaveTime)
                        {
                            announcedLeave = true;
                            if (_config.announceChat)
                            {
                                BroadcastLang("ship_leave");
                            }
                            if (_config.notifyEnabled)
                            {
                                BroadcastNotify("ship_leave");
                            }

                            State = EventState.LEAVING;
                            speedMultiplier *= 1.6f;
                            PathFinder.SetFinalDestination();
                        }
                    }

                    UpdateGui();

                    yield return new WaitForSecondsRealtime(1f);
                    timeRemaining--;
                }

                if (State != EventState.ENDED)
                {
                    StartSinking();
                }
            }

            #endregion

            #region Coroutines

            public void Spawn(BoatConfig config)
            {
                if (PathFinder == null)
                {
                    Cerr("Cannot spawn tugboat - path finder is null");
                    Destroy();
                    return;
                }

                Cprint($"Spawning event at {Boat.transform.position}");

                lockCode = UnityEngine.Random.Range(1111, 10000).ToString();

                StartCoroutine(SpawnCoro(config));
            }

            private IEnumerator SpawnCoro(BoatConfig config)
            {
                speedMultiplier = config.speedMultiplier;

                useMarker = config.useMapMarker;
                markerColor = config.markerColor;
                markerName = config.markerName;
                CreateMapMarker();
                yield return new WaitForEndOfFrame();

                SpawnCaptain(_instance.GetNpcConfig(config.captainNpcProfile));
                yield return new WaitForEndOfFrame();

                SpawnNpcs(config.npcSpawnProfiles.Select(x => _instance.GetSpawnConfig(x.Value, x.Key)));
                yield return new WaitForEndOfFrame();

                foreach (var obj in config.interior)
                {
                    var lootProfile = _instance.GetLootProfile(obj.lootProfile);
                    SpawnInteriorObject(obj, lootProfile);
                    yield return new WaitForEndOfFrame();
                }

                yield return new WaitForSeconds(2);

                StartEvent();
            }

            #endregion

            #region Movement

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void UpdateMovement()
            {
                target = PathFinder.GetCurrentTarget(Boat.transform.position);

                Vector3 forward = Boat.transform.forward;
                forward.y = 0;
                Vector3 normalized = (target - Boat.transform.position).normalized;
                float value = Vector3.Dot(forward, normalized);
                float b = (!captainDead && !Boat.IsDying) ? Mathf.InverseLerp(0f, 1f, value) : 0;
                float num = Vector3.Dot(Boat.transform.right, normalized);
                float b2 = Mathf.InverseLerp(0.05f, 0.5f, Mathf.Abs(num));
                turnScale = Mathf.Lerp(turnScale, b2, Time.deltaTime * 0.2f);
                float num3 = (float)((num < 0f) ? -1 : 1);
                currentTurnSpeed = 4f * speedMultiplier * turnScale * num3;
                Boat.transform.Rotate(Vector3.up, Time.deltaTime * currentTurnSpeed, Space.World);
                currentThrottle = Mathf.Lerp(currentThrottle, b, Time.deltaTime * 0.2f);
                currentVelocity = forward * (speedMultiplier * 8f * currentThrottle);
                Boat.transform.position += currentVelocity * Time.deltaTime;
            }

            void DebugPath()
            {
                if (startPlayer != null && startPlayer.IsConnected)
                {
                    DrawSphere(startPlayer, target, NODE_TOLERANCE, Color.red, 1f);
                    DrawSphere(startPlayer, PathFinder.GetNextNode(), NODE_TOLERANCE, Color.red, 1f);
                }
            }

            #endregion

            #region Interior

            public void SpawnInteriorObject(InteriorObject obj, List<LootManager.LootItem> lootConfig)
            {
                var entity = GameManager.server.CreateEntity(obj.prefabPath);
                if (entity == null)
                {
                    Cerr($"Failed to spawn prefab '{obj.prefabPath}' - prefab does not exist");
                    return;
                }

                var crate = entity as HackableLockedCrate;
                if (crate != null)
                {
                    crate.SendMessage("SetWasDropped", SendMessageOptions.DontRequireReceiver);
                    crates.Add(crate);
                }

                entity.skinID = obj.skinId;
                entity.Spawn();

                entity.enableSaving = false;

                var rigidbody = entity.GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    rigidbody.isKinematic = true;
                    //rigidbody.detectCollisions = false;
                }

                entity.SetParent(Boat);
                entity.transform.localPosition = obj.localPosition;
                entity.transform.localEulerAngles = Vector3.up * obj.rotationY;

                entity.SendNetworkUpdate();

                if (entity is StorageContainer container && lootConfig != null)
                {
                    LootManager.FillWithLoot(container, lootConfig);
                }

                if (entity is Door && obj.codelocked == true)
                {
                    var codeLock = GameManager.server.CreateEntity("assets/prefabs/locks/keypad/lock.code.prefab") as CodeLock;
                    if (codeLock != null)
                    {
                        codeLock.gameObject.Identity();
                        codeLock.SetParent(entity, entity.GetSlotAnchorName(BaseEntity.Slot.Lock));
                        codeLock.Spawn();
                        codeLock.code = lockCode;
                        codeLock.hasCode = true;
                        entity.SetSlot(BaseEntity.Slot.Lock, codeLock);
                        codeLock.SetFlag(BaseEntity.Flags.Locked, true);
                    }
                }

                if (entity is BaseOven)
                {
                    entity.SetFlag(BaseEntity.Flags.On, true);
                }

                entities.Add(entity);
            }

            #endregion

            #region NPC

            public void SpawnCaptain(NpcConfig config)
            {
                if (config == null)
                {
                    Cerr("Failed to spawn captain - npc profile is null. Make sure the captain npc profile in the config is valid");
                    Destroy();
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine(String.Join(string.Empty, Enumerable.Repeat("+", 36)));
                sb.AppendLine();
                sb.AppendLine(_config.pirateQuotes.GetRandom());
                sb.AppendLine();
                sb.AppendLine(String.Join(string.Empty, Enumerable.Repeat(" ", 24)) + lockCode);
                sb.AppendLine(String.Join(string.Empty, Enumerable.Repeat("+", 36)));

                config.health = 10f;
                captain = CreateNpc(config, new Vector3(0, 5.9f, 2.1f), true, sb.ToString());

                if (captain == null)
                {
                    Cerr("Failed to spawn captain");
                    return;
                }

                foreach(var item in config.belt)
                {
                    var def = ItemManager.FindItemDefinition(item.shortName);
                    if (def == null)
                    {
                        continue;
                    }

                    captain.inventory.containerMain.AddItem(def, item.amount, item.skinId);
                }

                Invoke(() => { Boat.AttemptMount(captain, false); }, 1f);
            }

            public void SpawnNpcs(IEnumerable<Tuple<NpcConfig, Vector3>> spawnProfiles)
            {
                foreach(var profile in spawnProfiles)
                {
                    if (profile == null)
                    {
                        continue;
                    }

                    CreateNpc(profile.Item1, profile.Item2);
                }
            }

            private ScientistNPC CreateNpc(NpcConfig config, Vector3 localPosition, bool idleOnly = false, string captainNoteText = null)
            {
                if (_instance.NpcSpawn == null)
                {
                    Cerr("Failed to spawn npc - NpcSpawn is not loaded");
                    return null;
                }

                NpcSpawnConfig npcConfig = new NpcSpawnConfig
                {
                    Name = config.name,
                    WearItems = config.clothing.Select(x => new NpcSpawnNpcWear { ShortName = x.shortName, SkinID = x.skinId }),
                    BeltItems = idleOnly ? new NpcSpawnNpcBelt[0] : config.belt.Select(x => new NpcSpawnNpcBelt { ShortName = x.shortName, Amount = x.amount, SkinID = x.skinId, Ammo = string.Empty, Mods = new string[0] }),
                    Kit = config.kit,
                    Health = config.health,
                    RoamRange = 0,
                    ChaseRange = 0,
                    SenseRange = config.senseRange,
                    ListenRange = config.senseRange / 2f,
                    AttackRangeMultiplier = 1f,
                    VisionCone = config.visionCone,
                    DamageScale = config.damageScale,
                    TurretDamageScale = 1f,
                    AimConeScale = 1f,
                    DisableRadio = !config.enableRadio,
                    CanRunAwayWater = true,
                    CanSleep = false,
                    Speed = 0,
                    AreaMask = 1,
                    AgentTypeID = -1372625422,
                    HomePosition = string.Empty,
                    MemoryDuration = config.memoryDuration,
                    States = idleOnly ? new HashSet<string> { "IdleState" } : new HashSet<string> { "IdleState", "CombatStationaryState" }
                };

                var scientist = _instance.NpcSpawn?.Call("SpawnNpc", Boat.transform.position, JObject.FromObject(npcConfig)) as ScientistNPC;
                if (scientist == null)
                {
                    Cerr("Failed to spawn npc - scientist is null");
                    return null;
                }

                scientist.SetParent(Boat);
                scientist.transform.localPosition = localPosition;

                entities.Add(scientist);
                npcs.Add(scientist.userID);

                _instance.RegisterNpc(scientist, config.lootProfile, config.removeCorpseAfterDeath, captainNoteText);

                return scientist;
            }

            #endregion

            #region Map marker

            public void CreateMapMarker()
            {
                if (!useMarker)
                {
                    return;
                }

                var color = new Color(36f / 255f, 128f / 255f, 251f / 255f);
                if (!ColorUtility.TryParseHtmlString(markerColor, out color))
                {
                    Cwarn($"Failed to parse map marker color '{color}' - make sure it is a valid hex color");
                }
                color.a = 0.6f;

                vendingMarker = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab", Boat.transform.position).GetComponent<VendingMachineMapMarker>();
                vendingMarker.markerShopName = markerName;
                vendingMarker.enableSaving = false;
                vendingMarker.Spawn();

                colorMarker = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab").GetComponent<MapMarkerGenericRadius>();
                colorMarker.color1 = color;
                colorMarker.color2 = colorMarker.color1;
                colorMarker.radius = 0.2f;
                colorMarker.alpha = 1f;
                colorMarker.enableSaving = false;
                colorMarker.SetParent(vendingMarker);
                colorMarker.Spawn();

                entities.Add(vendingMarker);
                entities.Add(colorMarker);

                InvokeRepeating(UpdateMapMarker, 2f, 2f);
            }

            void UpdateMapMarker()
            {
                var pos = Boat.transform.position;
                vendingMarker.transform.position = pos;
                colorMarker.transform.position = pos;

                vendingMarker.SendNetworkUpdate();
                colorMarker.SendNetworkUpdate();
                colorMarker.SendUpdate();
            }

            #endregion

            #region Zone

            private void CreateZone()
            {
                Zone = Zone.Create(Boat, _config.zoneRadius, _config.zoneDarkness, Vector3.up * 5);

                Zone.OnPlayerLeave += pl => DestroyGui(pl);
            }

            private readonly CuiElementContainer container = new CuiElementContainer();
            private readonly CuiElement panel = new CuiElement
            {
                Name = GUI_CONTAINER,
                Parent = "Hud",
                FadeOut = 0,
                DestroyUi = GUI_CONTAINER,
                Components =
                {
                    new CuiImageComponent { Color = "0 0 0 0" },
                    new CuiRectTransformComponent { AnchorMin = "0 0.88", AnchorMax = "1 1"}
                }
            };
            
            private void UpdateGui()
            {
                string key = State == EventState.RUNNING ? "timer_leave" : "timer_sink";
                int remaining = State == EventState.RUNNING ? timeRemaining - _config.destroyTime : timeRemaining;

                int minutes = Mathf.FloorToInt(remaining / 60f);
                int seconds = remaining % 60;

                foreach (var player in Zone.Players)
                {
                    container.Add(panel);
                    container.Add(new CuiElement
                    {
                        Parent = GUI_CONTAINER,
                        Components =
                        {
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                            new CuiTextComponent { Color = "1 1 1 1", FadeIn = 0f, Text = String.Format(GetMessage(key, player), minutes, seconds), FontSize = 24, Align = TextAnchor.LowerCenter, Font = "robotocondensed-bold.ttf" },
                            new CuiOutlineComponent { Distance = "1 1", Color = "0 0 0 1" }
                        }
                    });

                    CuiHelper.AddUi(player, container);

                    container.Clear();
                }
            }

            private void DestroyGui(BasePlayer player)
            {
                CuiHelper.DestroyUi(player, GUI_CONTAINER);
            }

            #endregion
        }

        #endregion

        #region Zone

        private class Zone : MonoBehaviour
        {
            public static Zone Create(BaseEntity parent, float radius, int sphereCount, Vector3 localPosition = default(Vector3))
            {
                var go = new GameObject();
                go.layer = (int)Layer.Reserved1;

                var zone = go.AddComponent<Zone>();
                zone.Setup(localPosition, radius, sphereCount, parent);

                return zone;
            }

            public static Zone Create(Vector3 position, float radius, int sphereCount)
            {
                var go = new GameObject();
                go.layer = (int)Layer.Reserved1;

                var zone = go.AddComponent<Zone>();
                zone.Setup(position, radius, sphereCount);

                return zone;
            }

            private SphereCollider collider;
            private readonly List<BaseEntity> spheres = new List<BaseEntity>();

            public HashSet<BasePlayer> Players { get; private set; } = new HashSet<BasePlayer>();

            public event Action<BasePlayer> OnPlayerEnter;
            public event Action<BasePlayer> OnPlayerLeave;

            void OnTriggerEnter(Collider other)
            {
                var player = other.ToBaseEntity() as BasePlayer;
                if (player != null && !player.IsNpc)
                {
                    Players.Add(player);
                    OnPlayerEnter?.Invoke(player);
                }
            }

            void OnTriggerExit(Collider other)
            {
                var player = other.ToBaseEntity() as BasePlayer;
                if (player != null && !player.IsNpc)
                {
                    Players.Remove(player);
                    OnPlayerLeave?.Invoke(player);
                }
            }

            void Setup(Vector3 position, float radius, int sphereCount, BaseEntity parent = null)
            {
                if (parent != null)
                {
                    gameObject.transform.SetParent(parent.transform, false);
                    gameObject.transform.localPosition = position;
                }
                else
                {
                    gameObject.transform.position = position;
                }

                collider = gameObject.AddComponent<SphereCollider>();
                collider.isTrigger = true;
                collider.radius = radius;

                for (int i = 0; i < sphereCount; i++)
                {
                    var sphere = (SphereEntity)GameManager.server.CreateEntity("assets/prefabs/visualization/sphere.prefab", collider.transform.position);
                    sphere.currentRadius = radius * 2;
                    sphere.lerpSpeed = 0f;
                    sphere.enableSaving = false;
                    sphere.Spawn();

                    if (parent != null)
                    {
                        sphere.SetParent(parent, true);
                        sphere.transform.localPosition = position;

                        sphere.SendNetworkUpdate();
                    }
                    
                    spheres.Add(sphere);
                }
            }

            void OnDestroy()
            {
                foreach (var sphere in spheres)
                {
                    if (!sphere.IsDestroyed)
                    {
                        sphere.Kill();
                    }
                }

                Players.Clear();
            }

            public void Destroy()
            {
                Destroy(gameObject);
            }
        }

        #endregion

        #region NpcSpawn

        internal class NpcSpawnNpcBelt { public string ShortName; public int Amount; public ulong SkinID; public IEnumerable<string> Mods; public string Ammo; }

        internal class NpcSpawnNpcWear { public string ShortName; public ulong SkinID; }

        internal class NpcSpawnConfig
        {
            public string Name { get; set; }
            public IEnumerable<NpcSpawnNpcWear> WearItems { get; set; }
            public IEnumerable<NpcSpawnNpcBelt> BeltItems { get; set; }
            public string Kit { get; set; }
            public float Health { get; set; }
            public float RoamRange { get; set; }
            public float ChaseRange { get; set; }
            public float SenseRange { get; set; }
            public float ListenRange { get; set; }
            public float AttackRangeMultiplier { get; set; }
            public bool CheckVisionCone { get; set; }
            public float VisionCone { get; set; }
            public float DamageScale { get; set; }
            public float TurretDamageScale { get; set; }
            public float AimConeScale { get; set; }
            public bool DisableRadio { get; set; }
            public bool CanRunAwayWater { get; set; }
            public bool CanSleep { get; set; }
            public float Speed { get; set; }
            public int AreaMask { get; set; }
            public int AgentTypeID { get; set; }
            public string HomePosition { get; set; }
            public float MemoryDuration { get; set; }
            public HashSet<string> States { get; set; }
        }

        #endregion

        #region Lang

        private static string GetMessage(string key, BasePlayer player)
        {
            return _instance.lang.GetMessage(key, _instance, player.UserIDString);
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["captain_killed"] = "The captain has been killed. Hurry up, the boat will start sinking in {0} seconds",
                ["ship_looted"] = "The pirate ship has been looted and will start sinking in {0} seconds",
                ["ship_destroyed"] = "The pirate ship has been destroyed and is sinking",

                ["timer_leave"] = "The ship will leave in {0}m {1}s",
                ["timer_sink"] = "The ship will sink in {0}m {1}s",

                ["ship_start"] = "Pirates are on their way to patrol the seas. They have been spotted near {0}",
                ["ship_start_toast"] = "Pirate ship inbound",
                ["ship_leave_5m"] = "Pirates will leave the seas in 5 minutes",
                ["ship_leave"] = "The Pirates have left the seas",
            }, this, "en");
        }

        #endregion

        #region Profiles

        private void RegisterNpc(ScientistNPC npc, string profile, bool removeCorpse, string captainNoteText = null)
        {
            if (String.IsNullOrEmpty(profile))
            {
                return;
            }

            npcProfiles[npc.net.ID.Value] = new ValueTuple<string, bool>(profile, removeCorpse);
            if (!String.IsNullOrEmpty(captainNoteText))
            {
                captainNoteTexts[npc.net.ID.Value] = captainNoteText;
            }
        }

        private List<LootManager.LootItem> GetLootProfile(string profile, string captainNoteText = null)
        {
            List<LootManager.LootItem> loot;
            if (_config.lootProfiles.TryGetValue(profile, out loot))
            {
                if (!String.IsNullOrEmpty(captainNoteText))
                {
                    loot = loot.ToList();
                    loot.Insert(0, new LootManager.LootItem("note", 1, 1, 1f) { text = captainNoteText });
                }

                return loot;
            }

            if (!String.IsNullOrEmpty(profile))
            {
                Cerr($"Loot profile '{profile}' not found. Check your config!");
            }

            return null;
        }

        private NpcConfig GetNpcConfig(string profile)
        {
            NpcConfig npcConfig;
            if (_config.npcProfiles.TryGetValue(profile, out npcConfig))
            {
                return npcConfig;
            }

            Cerr($"NPC profile '{profile}' not found. Check your config!");
            return null;
        }

        private Tuple<NpcConfig, Vector3> GetSpawnConfig(string profile, string spawnPoint)
        {
            NpcConfig npcConfig = GetNpcConfig(profile);
            if (npcConfig == null)
            {
                return null;
            }

            Vector3 spawnLocation;
            if (npcSpawnPoints.TryGetValue(spawnPoint, out spawnLocation))
            {
                return new Tuple<NpcConfig, Vector3>(npcConfig, spawnLocation);
            }

            Cerr($"NPC spawn point '{spawnPoint}' does not exist. Check your config!");
            return null;
        }

        #endregion

        #region Helpers

        public static void DrawSphere(BasePlayer player, Vector3 pos, float radius, Color color, float duration)
        {
            player.SendConsoleCommand("ddraw.sphere", duration, color, pos, radius);
        }

        private static void BroadcastNotify(string key, params string[] args)
        {
            if (_instance.Notify == null)
            {
                Cwarn("Failed to send notification. Notify is not installed");
                return;
            }

            foreach(var player in BasePlayer.activePlayerList)
            {
                string message = _instance.lang.GetMessage(key, _instance, player.UserIDString);
                _instance.Notify.Call("SendNotify", player, _config.notificationType, String.Format(message, args));
            }
        }

        private static void BroadcastLang(string key, params string[] args)
        {
            foreach(var player in BasePlayer.activePlayerList)
            {
                ChatMessageLang(player, key, args);
            }
        }

        private static void Broadcast(string message)
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                ChatMessage(player, message);
            }
        }

        private static void ChatMessage(BasePlayer player, string message)
        {
            player?.ChatMessage("<color=#00ffff>[Tugboat Event]</color> " + message);
        }

        private static void ChatMessageLang(BasePlayer player, string key, params string[] args)
        {
            var message = _instance.lang.GetMessage(key, _instance, player.UserIDString);
            player?.ChatMessage("<color=#00ffff>[Tugboat Event]</color> " + String.Format(message, args));
        }

        private static void ShowToastLang(string key, params string[] args)
        {
            foreach(var player in BasePlayer.activePlayerList)
            {
                string message = GetMessage(key, player);
                player.SendConsoleCommand("gametip.showtoast", 1, String.Format(message, args));
            }
        }

        private static void Dprint(string s)
        {
#if DEBUG
            _instance?.Puts("[DEBUG] " + s);
#endif
        }

        private static void Cprint(string s)
        {
            _instance?.Puts(s);
        }

        private static void Cwarn(string s)
        {
            _instance?.PrintWarning(s);
        }

        private static void Cerr(string s)
        {
            _instance?.PrintError(s);
        }

        #endregion
    }
}

namespace Oxide.Plugins.TugboatPiratesExt
{
    public static class Extensions
    {
        public static TriggerParent GetTriggerParent(this Tugboat tugboat)
        {
            // Carbonara compatibility
            var field = typeof(Tugboat).GetField("parentTrigger", BindingFlags.Instance | BindingFlags.Public);

            // Oxide whyyyyyyyyyyyy? at least we got good compiler now
            field ??= typeof(Tugboat).GetField("parentTrigger", BindingFlags.Instance | BindingFlags.NonPublic);

            if (field == null)
            {
                throw new FieldAccessException("Field Tugboat.parentTrigger not found");
            }

            return (TriggerParent)field.GetValue(tugboat);
        }
    }
}