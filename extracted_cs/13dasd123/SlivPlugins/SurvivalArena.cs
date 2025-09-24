using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;
using Oxide.Game.Rust.Cui;
using Newtonsoft.Json.Linq;
using Rust;
using static Steamworks.InventoryItem;


/* 
* Create new arenas.
* Spectator mode.
* Make it so servers can disable item prizes.
* Add scoreboard for wipe and all time.
*/

/* 1.0.9
 * Fixed the death mechanic when a player jumps off the edge of the arena. It will not strip the body and force the player to die.
 * Changed the player death to drop a container instead of a corpse.
 * Fixed a bug where some DroppedItemContainers would drop to the floor after an event.
 * Added support for EventHelpers team management system.
 * Added support for EventHelper join messages.
 * Added option to allow players to loot while out of the circle.
 * Fixed an issue with the prizes being deducted per roll, rather than per command run.
 * Added support to prevent CustomLoot from overwriting the container contents.
 * Fixed a death-loop bug that would kill the player over and over again.
 * Fixed an issue where players could not use commands after leaving the event (requires EventHelper v1.0.11+)
 * Added support for NTeleportation. Players who teleport to a player who is in the arena, will be teleported back to their old position immediately.
 */

namespace Oxide.Plugins
{
    [Info("Survival Arena", "imthenewguy", "1.0.9")]
    [Description("Spawns a battle royal arena in the sky")]
    class SurvivalArena : RustPlugin
    {
        #region Config

        private Configuration config;
        public class Configuration
        {
            [JsonProperty("Game settings")]
            public GameSettings gameSettings = new GameSettings();

            [JsonProperty("Event Helper settings")]
            public EventHelperSettings eventHelperSettings = new EventHelperSettings();

            [JsonProperty("Sound settings")]
            public SoundEffects soundsSettings = new SoundEffects();

            [JsonProperty("Radiation zone settings")]
            public SphereInfo radiationZoneSettings = new SphereInfo();

            [JsonProperty("Bush prefabs")]
            public Dictionary<BiomeType, List<string>> biome_bushes = new Dictionary<BiomeType, List<string>>();

            [JsonProperty("Tree prefabs")]
            public Dictionary<BiomeType, List<string>> biome_trees = new Dictionary<BiomeType, List<string>>();

            [JsonProperty("Dead log prefabs")]
            public Dictionary<BiomeType, List<string>> biome_logs = new Dictionary<BiomeType, List<string>>();

            [JsonProperty("Prize settings")]
            public Rewards prize_settings = new Rewards();

            [JsonProperty("Command settings")]
            public CommandInfo command_settings = new CommandInfo();

            [JsonProperty("Loot settings")]
            public Dictionary<string, LootSettings> lootSettings = new Dictionary<string, LootSettings>(StringComparer.OrdinalIgnoreCase);

            [JsonProperty("How many delete/spawn actions should we do per game tick when building/removing the arena?")]
            public int max_procs_per_tick = 50;

            [JsonProperty("Anchor settings")]
            public Anchors anchors = new Anchors();

            [JsonProperty("Notifications settings")]
            public NotificationsInfo notifications = new NotificationsInfo();

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
            config.biome_trees = DefaultBiomeTrees;
            config.biome_bushes = DefaultBiomeBushes;
            config.biome_logs = DefaultBiomeLogs;
            config.lootSettings = DefaultLootSettings;
            config.prize_settings.prizes = DefaultPrizes;
        }

        public class CommandInfo
        {
            [JsonProperty("Command to join an active lobby")]
            public string join_command = "survival";

            [JsonProperty("Command to leave an active lobby")]
            public string leave_command = "saleave";
        }

        public class SphereInfo
        {
            [JsonProperty("How many spheres should we overlap to make it darker [higher number = darker]?")]
            public int darkness = 8;

            [JsonProperty("How many stacks of radiation should players accumulate for each second spent outside of the sphere?")]
            public int rads_per_second = 3;

            [JsonProperty("How many points of radiation should the player accumulate per second while outside of the dome?")]
            public int rad_increase_per_tick = 3;

            [JsonProperty("Radiation check interval (seconds)")]
            public int check_interval = 1;

            [JsonProperty("Final circle size")]
            public int min_ring_size = 20;

            [JsonProperty("Seconds after game start before the circle spawns")]
            public int circle_delay = 60;

            [JsonProperty("Radiation panel colour [Red Green Blue Alpha][1.0 = full colour]")]
            public string panel_colour = "0.5 0 0 0.90";
        }

        public class Rewards
        {
            [JsonProperty("How many prizes should the player receive per claim?")]
            public int rolls_per_claim = 1;

            [JsonProperty("Prizes")]
            public List<PrizeInfo> prizes = new List<PrizeInfo>();

            [JsonProperty("Economic dollars for winning a match [requires: Economics]")]
            public CurrencyReward economic_reward = new CurrencyReward(0, 0);

            [JsonProperty("Server reward points for winning a match [requires: ServerRewards]")]
            public CurrencyReward srp_reward = new CurrencyReward(0, 0);

            [JsonProperty("Skill Tree XP given to the player when they win the event [Requires: SkillTree]")]
            public double SkillTree_XP_Reward = 1000;

            [JsonProperty("Automatically award the player with their prize (false means the player must type the /sprize command to redeem their prize)")]
            public bool auto_award = false;

            public class CurrencyReward
            {
                public int min_amount;
                public int max_amount;
                public CurrencyReward(int min_amount, int max_amount)
                {
                    this.min_amount = min_amount;
                    this.max_amount = max_amount;
                }
            }
        }

        public class PrizeInfo
        {
            public string shortname;
            public int min_quantity;
            public int max_quantity;
            public ulong skin;
            public string displayName;
            public int dropWeight;

            public PrizeInfo(string shortname, int min_quantity, int max_quantity, int dropWeight = 100, ulong skin = 0, string displayName = null)
            {
                this.shortname = shortname;
                this.min_quantity = min_quantity;
                this.max_quantity = max_quantity;
                this.skin = skin;
                this.displayName = displayName;
                this.dropWeight = dropWeight;
            }
        }

        public class Anchors
        {
            [JsonProperty("Player counter anchor [Key: Left/Right, Value: Up/Down]")]
            public KeyValuePair<float, float> player_count_anchor_adjustment = new KeyValuePair<float, float>(-40f, 7f);
        }

        public class SoundEffects
        {
            [JsonProperty("Sound effect when the game is about to start")]
            public string sound_for_starting = "assets/prefabs/missions/effects/mission_victory.prefab";

            [JsonProperty("Sound effect for killing another player")]
            public string sound_for_killing = "assets/prefabs/missions/effects/mission_accept.prefab";

            [JsonProperty("Sound effect when a player dies (players to all participants)")]
            public string sound_for_death = "assets/prefabs/npc/m2bradley/effects/maincannonattack.prefab";

            [JsonProperty("Sound when a player redeems a prize")]
            public string sound_for_prize = "assets/prefabs/misc/easter/painted eggs/effects/gold_open.prefab";
        }

        public class EventHelperSettings
        {
            [JsonProperty("Return the players items back immediately after respawn? (setting false requries the player to use a command to get their items back)")]
            public bool give_items_back_on_death = true;

            [JsonProperty("Use EventHelper to schedule the events (recommended)?")]
            public bool use_event_helper_timer = true;

            [JsonProperty("Commands to prevent when a player are at the event")]
            public string[] prevent_commands = { "kit", "backpack", "backpack.open" };

            [JsonProperty("Allow players to stay in a team when they join the event? [set to true if using any sort of clans or team management plugin]")]
            public bool allow_teams = true;
        }

        public class GameSettings
        {
            [JsonProperty("Minimum players required for the event to proceed")]
            public int min_player = 2;

            [JsonProperty("Send a message to the contestants when a player dies?")]
            public bool message_on_death = true;

            [JsonProperty("Minimum respawn time for crates")]
            public float min_crate_respawn_time = 30f;

            [JsonProperty("Maximum respawn time for crates")]
            public float max_crate_respawn_time = 90f;

            [JsonProperty("Attempt to clear old arenas when the plugin loads?")]
            public bool clear_arena_on_server_start = true;

            [JsonProperty("Default lobby time")]
            public int defaultStartTime = 300;

            [JsonProperty("Commands to run for each player when the game starts and the gates open. Use {id} in replacement of the players steam id")]
            public List<string> commands_on_start = new List<string>();

            [JsonProperty("Use the NightVision plugin?")]
            public bool use_nightvision = true;

            [JsonProperty("Allow players to loot while they are outside of the circle (in the radiation zone)?")]
            public bool allow_looting_in_rad_zone = false;

            [JsonProperty("Settings to help prevent non-participants from getting to the arena")]
            public InterferenceSettings interference_settings = new InterferenceSettings();
        }

        public class InterferenceSettings
        {
            [JsonProperty("Constantly check to see if non-participants are near the arena? [They will be warned then killed if found]")]
            public bool check_for_outsiders = true;

            [JsonProperty("How often should we check [seconds]")]
            public float check_time = 1;

            [JsonProperty("How close can the player get to the arena before they are warned")]
            public float dist_from_edge_restriction = 50f;

            [JsonProperty("How many seconds does the player have to leave the arena before they are killed?")]
            public float seconds_to_vacate = 10;

            [JsonProperty("Exclude players witht he IsAdmin flag from our checks?")]
            public bool ignore_admins = true;
        }

        public class NotificationsInfo
        {
            [JsonProperty("Notify plugin settings")]
            public NotifyInfo sendNotify = new NotifyInfo();

            [JsonProperty("GUIAnnouncements plugin settings")]
            public GUIAnnouncementsInfo GUIAnnouncements = new GUIAnnouncementsInfo();
        }

        public class NotifyInfo
        {
            [JsonProperty("Send notifications using the Notify plugin?")]
            public bool enabled = true;

            [JsonProperty("Notify profile/type")]
            public int notifyType = 0;
        }

        public class GUIAnnouncementsInfo
        {
            [JsonProperty("Send announcements using the GUIAnnouncements plugin?")]
            public bool enabled = true;

            [JsonProperty("Banner colour - see GUIAnnouncements for colours")]
            public string banner_colour = "Purple";

            [JsonProperty("Text colour - see GUIAnnouncements for colours")]
            public string text_colour = "Yellow";

            [JsonProperty("Position adjustment")]
            public float position_adjustment = 0;
        }

        public class LootSettings
        {
            [JsonProperty("Chance for this profile to be selected for the game (weighted system)")]
            public int profileWeight = 100;
            public int min_items;
            public int max_items;
            public List<LootInfo> items = new List<LootInfo>();

            public LootSettings(int min_items, int max_items, List<LootInfo> items, int profileWeight = 100)
            {
                this.min_items = min_items;
                this.max_items = max_items;
                this.items = items;
                this.profileWeight = profileWeight;
            }
        }

        public class LootInfo
        {
            public string shortname;
            public int min_amount;
            public int max_amount;
            public ulong skin;
            public string displayName;

            public LootInfo(string shortname, int min_amount, int max_amount, ulong skin = 0, string displayName = null)
            {
                this.shortname = shortname;
                this.min_amount = min_amount;
                this.max_amount = max_amount;
                this.skin = skin;
                this.displayName = displayName;
            }
        }

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
            SaveConfig();
        }

        protected override void SaveConfig()
        {
            PrintToConsole($"Configuration changes saved to {Name}.json");
            Config.WriteObject(config, true);
        }

        #endregion

        #region Defalt config

        Dictionary<BiomeType, List<string>> DefaultBiomeTrees
        {
            get
            {
                return new Dictionary<BiomeType, List<string>>()
                {
                    [BiomeType.Arid] = new List<string>()
                    {
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_forest/palm_tree_tall_a_entity.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_forest/palm_tree_tall_b_entity.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_beachside/palm_tree_short_a_entity.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_beachside/palm_tree_short_b_entity.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_beachside/palm_tree_short_c_entity.prefab"
                    },
                    [BiomeType.Temperate] = new List<string>()
                    {
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_forest_pine/pine_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_forest_pine/pine_c.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_field_large/oak_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_field_large/oak_c.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_field_large/oak_d.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_forest_pine/birch_big_temp.prefab"
                    },
                    [BiomeType.Tundra] = new List<string>()
                    {
                        "assets/bundled/prefabs/autospawn/resource/v3_tundra_field_pines/pine_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_tundra_field_pines/pine_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_tundra_field_pines/pine_d.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_tundra_forest/pine_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_tundra_forest/pine_c.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_tundra_forest/pine_dead_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_tundra_forest/pine_dead_b.prefab"
                    },
                    [BiomeType.Arctic] = new List<string>()
                    {
                        "assets/bundled/prefabs/autospawn/resource/v3_arctic_forest/pine_a_snow.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arctic_forest/pine_c_snow.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arctic_forest/pine_dead_snow_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arctic_forest/pine_dead_snow_c.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arctic_forestside/pine_a_snow.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arctic_forestside/pine_b snow.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arctic_forestside/pine_d_snow.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arctic_forestside/pine_dead_snow_e.prefab",
                    }
                };
            }
        }

        Dictionary<BiomeType, List<string>> DefaultBiomeBushes
        {
            get
            {
                return new Dictionary<BiomeType, List<string>>()
                {
                    [BiomeType.Arid] = new List<string>()
                    {
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arid_desert/creosote_bush_c.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arid_desert/creosote_bush_d.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arid_desert/mormon_tea_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arid_desert/mormon_tea_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arid_desert/mormon_tea_c.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arid_desert/mormon_tea_d.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arid_dry/creosote_bush_dry_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arid_dry/creosote_bush_dry_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arid_grass/creosote_bush_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arid_grass/creosote_bush_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arid_grass/creosote_bush_c.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arid_grass/creosote_bush_d.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_cactus/cactus-1.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_cactus/cactus-2.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_cactus/cactus-3.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_cactus/cactus-4.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_cactus/cactus-5.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_cactus/cactus-6.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_cactus/cactus-7.prefab"
                    },
                    [BiomeType.Temperate] = new List<string>()
                    {
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_temp/bush_spicebush_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_temp/bush_spicebush_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_temp/bush_spicebush_c.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_temp/bush_spicebush_d.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_temp/bush_willow_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_temp/bush_willow_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_temp/bush_willow_c.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_temp/bush_willow_d.prefab"
                    },
                    [BiomeType.Tundra] = new List<string>()
                    {
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_tundra/bush_spicebush_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_tundra/bush_spicebush_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_tundra/bush_spicebush_c.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_tundra/bush_spicebush_d.prefab"
                    },
                    [BiomeType.Arctic] = new List<string>()
                    {
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arctic/bush_willow_snow_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arctic/bush_willow_snow_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arctic/bush_willow_snow_c.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arctic/bush_willow_snow_d.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arctic/bush_willow_snow_small_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arctic_forest/bush_spicebush_a_snow.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arctic_forest/bush_spicebush_c_snow.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arctic_forest/bush_willow_snow_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arctic_forest/bush_willow_snow_b.prefab"
                    }
                };
            }
        }

        Dictionary<BiomeType, List<string>> DefaultBiomeLogs
        {
            get
            {
                return new Dictionary<BiomeType, List<string>>()
                {
                    [BiomeType.Arid] = new List<string>()
                    {
                        "assets/bundled/prefabs/autospawn/resource/logs_dry/dead_log_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/logs_dry/dead_log_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/logs_dry/dead_log_c.prefab"
                    },
                    [BiomeType.Temperate] = new List<string>()
                    {
                        "assets/bundled/prefabs/autospawn/resource/logs_dry/dead_log_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/logs_dry/dead_log_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/logs_dry/dead_log_c.prefab"
                    },
                    [BiomeType.Tundra] = new List<string>()
                    {
                        "assets/bundled/prefabs/autospawn/resource/logs_dry/dead_log_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/logs_dry/dead_log_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/logs_dry/dead_log_c.prefab"
                    },
                    [BiomeType.Arctic] = new List<string>()
                    {
                        "assets/bundled/prefabs/autospawn/resource/logs_snow/dead_log_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/logs_snow/dead_log_c.prefab",
                        "assets/bundled/prefabs/autospawn/resource/logs_snow/dead_log_a.prefab"
                    }
                };
            }
        }

        Dictionary<string, LootSettings> DefaultLootSettings
        {
            get
            {
                return new Dictionary<string, LootSettings>()
                {
                    ["PrimitiveLoot"] = new LootSettings(2, 4, new List<LootInfo>()
                    {
                        new LootInfo("attire.hide.pants", 1, 1),
                        new LootInfo("attire.hide.poncho", 1, 1),
                        new LootInfo("attire.hide.skirt", 1, 1),
                        new LootInfo("attire.hide.vest", 1, 1),
                        new LootInfo("attire.hide.helterneck", 1, 1),
                        new LootInfo("attire.hide.boots", 1, 1),
                        new LootInfo("grenade.beancan", 1, 1),
                        new LootInfo("hat.wolf", 1, 1),
                        new LootInfo("spear.stone", 1, 1),
                        new LootInfo("spear.wooden", 1, 1),
                        new LootInfo("arrow.bone", 10, 20),
                        new LootInfo("arrow.fire", 3, 6),
                        new LootInfo("arrow.hv", 5, 12),
                        new LootInfo("arrow.wooden", 5, 20),
                        new LootInfo("longsword", 1, 1),
                        new LootInfo("salvaged.sword", 1, 1),
                        new LootInfo("machete", 1, 1),
                        new LootInfo("bow.compound", 1, 1),
                        new LootInfo("crossbow", 1, 1),
                        new LootInfo("bow.hunting", 1, 1),
                        new LootInfo("grenade.f1", 1, 3),
                        new LootInfo("pistol.revolver", 1, 1),
                        new LootInfo("ammo.pistol", 5, 15),
                        new LootInfo("pistol.nailgun", 1, 1),
                        new LootInfo("bandage", 1, 6),
                        new LootInfo("syringe.medical", 1, 2),
                        new LootInfo("bone.armor.suit", 1, 1),
                        new LootInfo("bone.club", 1, 1),
                        new LootInfo("deer.skull.mask", 1, 1),
                        new LootInfo("knife.bone", 1, 1),
                        new LootInfo("wood.armor.helmet", 1, 1),
                        new LootInfo("wood.armor.pants", 1, 1),
                        new LootInfo("wood.armor.jacket", 1, 1),
                        new LootInfo("hat.boonie", 1, 1),
                        new LootInfo("bucket.helmet", 1, 1),
                        new LootInfo("riot.helmet", 1, 1),
                        new LootInfo("burlap.gloves.new", 1, 1),
                        new LootInfo("burlap.headwrap", 1, 1),
                        new LootInfo("burlap.shirt", 1, 1),
                        new LootInfo("burlap.shoes", 1, 1),
                        new LootInfo("burlap.trousers", 1, 1),
                        new LootInfo("knife.butcher", 1, 1),
                        new LootInfo("knife.combat", 1, 1),
                        new LootInfo("shotgun.waterpipe", 1, 1),
                        new LootInfo("ammo.handmade.shell", 1, 5),
                        new LootInfo("stonehatchet", 1, 1),
                        new LootInfo("mace", 1, 1),
                        new LootInfo("salvaged.cleaver", 1, 1),
                        new LootInfo("rock", 1, 1),
                        new LootInfo("sickle", 1, 1),
                    }),
                    ["GunLoot"] = new LootSettings(2, 4, new List<LootInfo>()
                    {
                        new LootInfo("weapon.mod.8x.scope", 1, 1),
                        new LootInfo("weapon.mod.small.scope", 1, 1),
                        new LootInfo("crossbow", 1, 1),
                        new LootInfo("weapon.mod.holosight", 1, 1),
                        new LootInfo("longsword", 1, 1),
                        new LootInfo("machete", 1, 1),
                        new LootInfo("weapon.mod.muzzleboost", 1, 1),
                        new LootInfo("weapon.mod.muzzlebrake", 1, 1),
                        new LootInfo("salvaged.cleaver", 1, 1),
                        new LootInfo("weapon.mod.silencer", 1, 1),
                        new LootInfo("weapon.mod.simplesight", 1, 1),
                        new LootInfo("tactical.gloves", 1, 1),
                        new LootInfo("weapon.mod.lasersight", 1, 1),
                        new LootInfo("ammo.shotgun", 5, 10),
                        new LootInfo("ammo.shotgun.fire", 5, 10),
                        new LootInfo("ammo.shotgun.slug", 2, 7),
                        new LootInfo("ammo.grenadelauncher.he", 2, 5),
                        new LootInfo("ammo.rifle", 20, 40),
                        new LootInfo("ammo.rifle.explosive", 5, 10),
                        new LootInfo("ammo.handmade.shell", 10, 20),
                        new LootInfo("ammo.rocket.hv", 1, 2),
                        new LootInfo("ammo.rifle.hv", 5, 10),
                        new LootInfo("ammo.pistol.fire", 5, 10),
                        new LootInfo("ammo.nailgun.nails", 20, 40),
                        new LootInfo("ammo.pistol", 20, 40),
                        new LootInfo("ammo.pistol.hv", 10, 20),
                        new LootInfo("ammo.rifle.incendiary", 10, 15),
                        new LootInfo("rifle.ak", 1, 1),
                        new LootInfo("rifle.bolt", 1, 1),
                        new LootInfo("rifle.l96", 1, 1),
                        new LootInfo("rifle.lr300", 1, 1),
                        new LootInfo("rifle.m39", 1, 1),
                        new LootInfo("rifle.semiauto", 1, 1),
                        new LootInfo("pistol.eoka", 1, 1),
                        new LootInfo("pistol.m92", 1, 1),
                        new LootInfo("pistol.nailgun", 1, 1),
                        new LootInfo("pistol.python", 1, 1),
                        new LootInfo("pistol.revolver", 1, 1),
                        new LootInfo("pistol.semiauto", 1, 1),
                        new LootInfo("arrow.bone", 10, 40),
                        new LootInfo("arrow.fire", 5, 10),
                        new LootInfo("arrow.hv", 10, 40),
                        new LootInfo("arrow.wooden", 20, 60),
                        new LootInfo("jumpsuit.suit.blue", 1, 1),
                        new LootInfo("bone.armor.suit", 1, 1),
                        new LootInfo("hazmatsuit", 1, 1),
                        new LootInfo("hazmatsuit.nomadsuit", 1, 1),
                        new LootInfo("hazmatsuit.spacesuit", 1, 1),
                        new LootInfo("roadsign.jacket", 1, 1),
                        new LootInfo("roadsign.kilt", 1, 1),
                        new LootInfo("wood.armor.helmet", 1, 1),
                        new LootInfo("wood.armor.pants", 1, 1),
                        new LootInfo("wood.armor.jacket", 1, 1),
                        new LootInfo("deer.skull.mask", 1, 1),
                        new LootInfo("bucket.helmet", 1, 1),
                        new LootInfo("coffeecan.helmet", 1, 1),
                        new LootInfo("heavy.plate.helmet", 1, 1),
                        new LootInfo("riot.helmet", 1, 1),
                        new LootInfo("shotgun.double", 1, 1),
                        new LootInfo("shotgun.pump", 1, 1),
                        new LootInfo("shotgun.spas12", 1, 1),
                        new LootInfo("shotgun.waterpipe", 1, 1),
                        new LootInfo("smg.2", 1, 1),
                        new LootInfo("smg.mp5", 1, 1),
                        new LootInfo("smg.thompson", 1, 1),
                        new LootInfo("grenade.f1", 2, 5),
                        new LootInfo("multiplegrenadelauncher", 1, 1),
                        new LootInfo("grenade.smoke", 1, 2),
                        new LootInfo("grenade.beancan", 1, 1),
                        new LootInfo("rocket.launcher", 1, 1),
                        new LootInfo("jacket", 1, 1),
                        new LootInfo("burlap.gloves.new", 1, 1),
                        new LootInfo("burlap.gloves", 1, 1),
                        new LootInfo("roadsign.gloves", 1, 1),
                        new LootInfo("shoes.boots", 1, 1),
                        new LootInfo("boots.frog", 1, 1),
                        new LootInfo("attire.hide.boots", 1, 1),
                        new LootInfo("attire.hide.pants", 1, 1),
                        new LootInfo("attire.hide.poncho", 1, 1),
                        new LootInfo("attire.hide.skirt", 1, 1),
                        new LootInfo("attire.hide.vest", 1, 1),
                        new LootInfo("pants", 1, 1),
                        new LootInfo("pants.shorts", 1, 1),
                        new LootInfo("hoodie", 1, 1),
                        new LootInfo("syringe.medical", 1, 3),
                        new LootInfo("bandage", 3, 6),
                        new LootInfo("largemedkit", 1, 2),
                        new LootInfo("metal.plate.torso", 1, 1),
                        new LootInfo("metal.facemask", 1, 1)

                    })
                };
            }
        }

        List<PrizeInfo> DefaultPrizes
        {
            get
            {
                return new List<PrizeInfo>()
                {
                    new PrizeInfo("scrap", 300, 600)
                };
            }
        }

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPerms"] = "You do not have permission to use this command.",
                ["EventRunning"] = "The event is already running.",
                ["SurvivalArenaStartingBeginIn"] = "Game will start in <color=#ffb600>{0}</color> seconds. Type <color=#00ff00>/{1}</color> to join.",
                ["Cancelled"] = "Survival Arena has been cancelled due to lack of players.",
                ["DoorsOpeningSoon"] = "Doors opening in <color=#ffb600>5</color> seconds.",
                ["Prefix"] = "[<color=#ffae00>SurvivalArena</color>] ",
                ["CircleClosingStarted"] = "<color=#0094c8>The circle has started to close!</color>",
                ["CircleClosingFinished"] = "<color=#0094c8>The final circle has been formed!</color>",
                ["AnnounceWinner"] = "{0} has won the event!",
                ["MessageWinner"] = "Type <color=#23f207>/sprize</color> to claim your prize. Prizes remaining: <color=#23f207>{0}</color>.\n",
                ["MsgToWinner"] = "You have won the event!\n",
                ["NobodyWon"] = "Nobody won the event.",
                ["EconomicsWon"] = "<color=#23f207>${0}</color> has been added to your account for winning.\n",
                ["ServerRewardsWon"] = "<color=#23f207>{0} points</color> were added to your account for winning.\n",
                ["SkillTreeXPWon"] = "You gained <color=#23f207>{0} xp</color> for winning.\n",

                ["FailedJoin_Closed"] = "Unable to join - Joining has closed.",
                ["FailedJoin_EventHelper"] = "Unable to join - Check the message from EventHelper.",
                ["FailedJoin_Enrolled"] = "Unable to join - You are already in this competition.",

                ["JoinAnnounce"] = "{0} joined the event [<color=#ffb600>{1}</color>/<color=#ffb600>{2}</color>]",
                ["LeftCompetition"] = "You left the arena.",
                ["NotAtEvent"] = "You are not at the event.",
                ["AddEntityEventRunning"] = "Event needs to be running to use this command.",
                ["AddEntityElevation"] = "Arena must not be above default elevation when adding spawns.",
                ["NullInvalidEntity"] = "Invalid entity targeted. Tried to target a rock, player or gates.",
                ["EntityRemoved"] = "Removed entity: {0} [POS: {1} - ROT: {2}]",
                ["EntityNotFoundInData"] = "Entity: {0} [POS: {1} - ROT: {2}] was not found in arena data.",
                ["EntityAddedTree"] = "Added new tree spawn at {0}",
                ["EntityAddedBush"] = "Added new bush spawn at {0}",
                ["EntityAddedLoot"] = "Added new loot spawn spawn at {0}",
                ["EntityAddedLog"] = "Added new log spawn at {0}",
                ["EntityAddInvalidType"] = "Invalid type selected.",
                ["InRadZone"] = "YOU ARE IN THE RADIATION ZONE!",
                ["UIPlayersRemaining"] = "Players remaining: <color=#ffae00>{0}</color>",
                ["UILootProfile"] = "Loot Profile",
                ["UIProfSelected"] = "<color=#ffae00>{0}</color>",
                ["UIHeightMod"] = "Height Mod",
                ["UILobbyTime"] = "Lobby Time",
                ["UISTOP"] = "<color=#f22407>STOP</color>",
                ["UISTART"] = "<color=#00be06>START</color>",
                ["UICLOSE"] = "CLOSE",
                ["UIEndedEvent"] = "Ended the event.",
                ["UIStartingEvent"] = "Starting event with the following parameters:\n- Lobby time: {0}\n- Height: {1}\n- Profile: {2}",
                ["_UILobbyTime"] = "GAME STARTING IN <color=#ffae00>{0}</color> SECONDS.\n<size=10>Type <color=#ffae00>/{1}</color> to leave the event.</size>",
                ["NoPrize"] = "You have no prizes left to claim.",
                ["PrizeGiven"] = "You received {0}x {1}.",
                ["JoinedTheEvent"] = "You joined the Survival Arena event!\nType <color=#00ff00>/{0}</color> if you wish to leave the event.",
                ["KillMessage1"] = "<color=#ffae00>{0}</color> stood no chance against <color=#ffae00>{1}</color>!",
                ["KillMessage2"] = "<color=#ffae00>{0}</color> was killed by magic...Or was it <color=#ffae00>{1}</color>?",
                ["KillMessage3"] = "<color=#ffae00>{0}</color> was escorted out of the arena by <color=#ffae00>{1}</color>.",
                ["KillMessage4"] = "<color=#ffae00>{0}</color> was slain by <color=#ffae00>{1}</color>.",
                ["KillMessage5"] = "<color=#ffae00>{0}</color> misplaced their weapon inside of <color=#ffae00>{1}</color>.",
                ["DeathMessage1"] = "<color=#ffae00>{0}</color> tripped on a branch and died.",
                ["DeathMessage2"] = "A cold breeze got the better of <color=#ffae00>{0}</color>!",
                ["DeathMessage3"] = "<color=#ffae00>{0}</color> decided that they no longer wish to play, so they died.",
                ["EventStillDespawning"] = "The arena is still being cleared from the last event. Please try again in a few seconds.",
                ["InvalidProfile"] = "You have specified an invalid loot profile [{0}]. Valid profiles:\n- {1}",
                ["PrizesDisabled"] = "This command is not enabled as there are no prizes specified in the config.",
                ["CircleStatusUpdate"] = "Circle status: {0}",
                ["CircleStatusMOVING"] = "<color=#fff700>MOVING</color>",
                ["CircleStatusSTOPPED"] = "<color=#00ff00>STOPPED</color>",
                ["CircleStatusINACTIVE"] = "<color=#ff0000>INACTIVE</color>",
                ["UILeaveButton"] = "<color=#ff0000>LEAVE</color>"
            }, this);
        }

        #endregion

        #region Data

        PlayerEntity pcdData;
        SpawnedEntities spawnData;

        private DynamicConfigFile PCDDATA;
        private DynamicConfigFile SPAWNDATA;

        Dictionary<string, DynamicConfigFile> ARENASDATA = new Dictionary<string, DynamicConfigFile>();
        Dictionary<string, ArenaData> Arenas = new Dictionary<string, ArenaData>();

        public static Vector3 CurrentCentrePoint;
        ArenaData CurrentArena;

        const string subDirectory = "survivalArena/";
        const string perm_admin = "survivalarena.admin";

        void Init()
        {
            UnsubHooks();            
            LoadData();

            permission.RegisterPermission(perm_admin, this);            

            cmd.AddChatCommand(config.command_settings.join_command, this, nameof(JoinEvent));
            cmd.AddChatCommand(config.command_settings.leave_command, this, nameof(LeaveEventCMD));
        }

        void Unload()
        {
            SaveData();
            try
            {
                EndEvent(true);
            }
            catch { }
            try
            {
                EventHelper.Call("EMRemoveEvent", this.Name);
            }
            catch { }
            
            cmd.RemoveChatCommand(config.command_settings.join_command, this);
            cmd.RemoveChatCommand(config.command_settings.leave_command, this);
            foreach (var player in BasePlayer.activePlayerList)
            {
                try
                {
                    DestroyMonitor(player);
                    DestroyOutsiderMonitor(player);
                }
                catch { }
            }

            if (Spawn_routine != null) ServerMgr.Instance.StopCoroutine(Spawn_routine);
            Spawn_routine = null;

            ServerMgr.Instance.StartCoroutine(DespawnEntities(0f, false, true));
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "GameStartUI");
                CuiHelper.DestroyUi(player, "MenuBackground");
                CuiHelper.DestroyUi(player, "STARTCountdownUI");
            }

            SaveData(SaveType.Spawn);
        }

        enum SaveType
        {
            Both,
            Player,
            Arena,
            Spawn
        }

        void SaveData(SaveType saveType = SaveType.Player)
        {
            if (saveType == SaveType.Both || saveType == SaveType.Player) PCDDATA.WriteObject(pcdData);
            if (saveType == SaveType.Spawn) SPAWNDATA.WriteObject(spawnData);
            if (saveType == SaveType.Both || saveType == SaveType.Arena)
            {
                foreach (var DATA in ARENASDATA)
                {
                    ArenaData arenaData;
                    if (Arenas.TryGetValue(DATA.Key, out arenaData)) DATA.Value.WriteObject(arenaData);
                }

                //ARENADATA.WriteObject(arenaData);
            }            
        }

        bool HaveArenaFile = false;
        void LoadData()
        {
            try
            {
                PCDDATA = Interface.Oxide.DataFileSystem.GetFile(subDirectory + "player_data");
                pcdData = Interface.Oxide.DataFileSystem.ReadObject<PlayerEntity>(subDirectory + "player_data");
            }
            catch
            {
                Puts("Couldn't load player data, creating new Playerfile");
                pcdData = new PlayerEntity();
            }

            try
            {
                SPAWNDATA = Interface.Oxide.DataFileSystem.GetFile(subDirectory + "spawn_data");
                spawnData = Interface.Oxide.DataFileSystem.ReadObject<SpawnedEntities>(subDirectory + "spawn_data");
            }
            catch
            {
                Puts("Could not load spawn_data file.");
                spawnData = new SpawnedEntities();
            }

            //ARENADATA = Interface.Oxide.DataFileSystem.GetFile(subDirectory + "Arena");
            foreach (var file in Interface.Oxide.DataFileSystem.GetFiles(subDirectory))
            {
                if (file.Contains("player_data") || file.Contains("spawn_data")) continue;
                var name = file.Split('/')?.Last();
                name = name.Replace(".json", "");
                if (!ARENASDATA.ContainsKey(name))
                {
                    var data = Interface.Oxide.DataFileSystem.GetFile(subDirectory + name);
                    if (data != null)
                    {
                        //ARENADATA
                        try
                        {
                            var arenaData = Interface.Oxide.DataFileSystem.ReadObject<ArenaData>(subDirectory + name);
                            if (arenaData != null)
                            {
                                ARENASDATA.Add(name, data);
                                Arenas.Add(name, arenaData);
                                Puts($"Found and added arena: {name}");
                                HaveArenaFile = true;
                                SaveData(SaveType.Arena);
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }
            if (Arenas.Count == 0)
            {
                Puts("Couldn't load arena data, creating new Arena data file");
                AddArenaFile();
            }
            //if (Interface.Oxide.DataFileSystem.ExistsDatafile(subDirectory + "Arena"))
            //{
            //    ARENADATA = Interface.Oxide.DataFileSystem.GetFile(subDirectory + "Arena");
            //    try
            //    {
            //        arenaData = Interface.Oxide.DataFileSystem.ReadObject<ArenaData>(subDirectory + "Arena");
            //        Puts($"Found and loaded arena data - Entities: {arenaData.entities?.Count}");
            //        HaveArenaFile = true;
            //    }
            //    catch
            //    {
            //        Puts("Couldn't load arena data, creating new Arena data file");
            //        AddArenaFile();
            //    }
            //}
        }

        class ArenaData
        {
            public float size = 210;
            public Vector3 CenterPoint;
            public List<EntityInfo> entities = new List<EntityInfo>();
        }

        public class EntityInfo
        {
            public string prefab;
            public Vector3 pos;
            public Vector3 rot;

            public EntityInfo(string prefab, Vector3 pos, Vector3 rot)
            {
                this.prefab = prefab;
                this.pos = pos;
                this.rot = rot;
            }
        }

        class SpawnedEntities
        {
            public List<ulong> spawnedEntities = new List<ulong>();
        }

        class PlayerEntity
        {
            public Dictionary<ulong, PCDInfo> pEntity = new Dictionary<ulong, PCDInfo>();

        }

        class PCDInfo
        {
            public int rewards_remaining;
        }


        #endregion;

        #region Get Arena

        //[ConsoleCommand("getarena")]
        //void GetArena(ConsoleSystem.Arg arg)
        //{
        //    var player = arg.Player();
        //    if (player != null && !IsAdmin(player))
        //    {
        //        PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("NoPerms", this, player.UserIDString));
        //        return;
        //    }

        //    if (arg.Args == null || arg.Args.Length == 0)
        //    {
        //        arg.ReplyWith("You must specify the arena name");
        //        return;
        //    }

        //    GetArena((BasePlayer)null);
        //}

        [ChatCommand("getarena")]
        void GetArena(BasePlayer player, string cmd, string[] args)
        {
            if (player != null && !IsAdmin(player))
            {
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("NoPerms", this, player.UserIDString));
                return;
            }
            if (string.IsNullOrEmpty(ConVar.Server.levelurl) || !ConVar.Server.levelurl.Contains("SurvivalArena"))
            {
                if (player != null) PrintToChat(player, "The map file URL on your server must contain the name SurvivalArena in order to run this command.");
                else Puts("The map file on your server must contain the name SurvivalArena in order to run this command.");
                return;
            }

            if (args == null || args.Length == 0)
            {
                if (player != null) PrintToChat(player, "You must specify a name for the arena");
                else Puts("You must specify a name for the arena");
                return;
            }

            var name = string.Join(" ", args);
            name = name.Replace(" ", "_");

            ArenaData arenaData = new ArenaData();

            foreach (var entity in spawned_entities)
            {
                entity?.KillMessage();
            }

            arenaData.entities.Clear();
            foreach (var entity in BaseNetworkable.serverEntities)
            {
                if (entity is BaseNpc || entity is BasePlayer) continue;
                if (!IsWhitelisted(entity.PrefabName)) continue;
                if (entity.PrefabName == "assets/prefabs/deployable/bbq/bbq.deployed.prefab")
                {
                    Puts("Found and stored center point.");
                    arenaData.CenterPoint = entity.transform.position;
                    continue;
                }

                // Replaces fire pit with wooden external gates
                if (entity.PrefabName == "assets/prefabs/misc/halloween/skull_fire_pit/skull_fire_pit.prefab")
                {
                    arenaData.entities.Add(new EntityInfo("assets/prefabs/building/gates.external.high/gates.external.high.wood/gates.external.high.wood.prefab", entity.transform.position, entity.transform.rotation.eulerAngles));
                    continue;
                }

                // Replaces snow machine with roof triangles
                if (entity.PrefabName == "assets/prefabs/misc/xmas/snow_machine/models/snowmachine.prefab")
                {
                    arenaData.entities.Add(new EntityInfo("assets/prefabs/building core/roof.triangle/roof.triangle.prefab", entity.transform.position, entity.transform.rotation.eulerAngles));
                    continue;
                }

                // Replaces carvable pumpkins with loot crates
                if (entity.PrefabName == "assets/prefabs/misc/halloween/carvablepumpkin/carvable.pumpkin.prefab")
                {
                    arenaData.entities.Add(new EntityInfo("assets/bundled/prefabs/radtown/crate_normal_2.prefab", entity.transform.position, entity.transform.rotation.eulerAngles));
                    continue;
                }

                arenaData.entities.Add(new EntityInfo(entity.PrefabName, entity.transform.position, entity.transform.rotation.eulerAngles));
            }

            if (Arenas.ContainsKey(name))
            {
                Arenas[name] = arenaData;
                PrintToChat(player, $"Overwrote old arena: {name}");
            }
            else
            {
                Arenas.Add(name, arenaData);
                PrintToChat(player, $"Added new arena: {name}");
            }

            SaveData(SaveType.Arena);
        }

        [ChatCommand("spawnarena")]
        void SpawnArena(BasePlayer player, string cmd, string[] args)
        {
            if (!IsAdmin(player))
            {
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("NoPerms", this, player.UserIDString));
                return;
            }
            if (IsDespawning)
            {
                PrintToChat(player, lang.GetMessage("Prefix", this, player?.UserIDString) + lang.GetMessage("EventStillDespawning", this, player.UserIDString));
                return;
            }

            if (args == null || args.Length == 0)
            {
                PrintToChat(player, $"You must specify the arena. Available arenas:\n{string.Join("\n", Arenas.Keys)}");
                return;
            }

            string arenaName = args[0];
            if (!Arenas.ContainsKey(arenaName))
            {
                PrintToChat(player, $"{arenaName} is not a valid arena! Valid arenas:\n{string.Join("\n", Arenas.Keys)}");
                return;
            }

            float heightMod = args != null && args.Length > 1 ? Convert.ToSingle(args[1]) : 0f;
            CurrentArena = Arenas[arenaName];
            StartSpawnSequence(heightMod, false);
            DevMode = true;
        }

        [ChatCommand("cleararena")]
        void ClearArena(BasePlayer player)
        {
            if (!IsAdmin(player))
            {
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("NoPerms", this, player.UserIDString));
                return;
            }
            EndEvent();
        }

        bool IsWhitelisted(string prefab)
        {
            foreach (var item in prefab_whitelist)
            {
                if (prefab.StartsWith(item)) return true;
            }
            return false;
        }

        List<string> prefab_whitelist = new List<string>()
        {
            "assets/bundled/prefabs/modding/admin/",
            "assets/bundled/prefabs/autospawn/resource/",
            "assets/prefabs/misc/xmas/",
            "assets/prefabs/deployable/",
            "assets/prefabs/building/",
            "assets/prefabs/building core/",
            "assets/prefabs/misc/halloween/"
        };

        [ChatCommand("setcentrepoint")]
        void SetCentrePoint(BasePlayer player, string cmd, string[] args)
        {
            if (!IsAdmin(player))
            {
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("NoPerms", this, player.UserIDString));
                return;
            }          

            if (CanJoin || IsRunning || DevMode)
            {
                PrintToChat(player, "The arena cannot be spawned when running this command. Please despawn the arena with /endarena.");
                return;
            }

            if (args == null || args.Length == 0)
            {
                PrintToChat(player, $"You must specify the arena name. Available arenas:\n{string.Join("\n", Arenas.Keys)}");
                return;
            }

            var name = string.Join(" ", args);

            MoveCentrePoint(player, player.transform.position, name);
        }
        
        void MoveCentrePoint(BasePlayer player, Vector3 new_pos, string arena)
        {
            ArenaData arenaData;
            if (!Arenas.TryGetValue(arena, out arenaData))
            {
                PrintToChat(player, $"Failed to find the arena: {arena}");
                return;
            }
            foreach (var entry in arenaData.entities)
            {
                var diff_x = Math.Abs(arenaData.CenterPoint.x - new_pos.x);
                if (arenaData.CenterPoint.x > new_pos.x) entry.pos.x -= diff_x;
                else entry.pos.x += diff_x;

                var diff_y = Math.Abs(arenaData.CenterPoint.y - new_pos.y);
                if (arenaData.CenterPoint.y > new_pos.y) entry.pos.y -= diff_y;
                else entry.pos.y += diff_y;

                var diff_z = Math.Abs(arenaData.CenterPoint.z - new_pos.z);
                if (arenaData.CenterPoint.z > new_pos.z) entry.pos.z -= diff_z;
                else entry.pos.z += diff_z;
            }
            arenaData.CenterPoint = new_pos;
            SaveData(SaveType.Arena);
            PrintToChat(player, $"Set new position: {new_pos} and adjusted points.");
        }

        #endregion

        #region Spawn Arena

        bool IsAdmin(BasePlayer player)
        {
            return permission.UserHasPermission(player.UserIDString, perm_admin);
        }

        public List<BaseEntity> spawned_entities = new List<BaseEntity>();
        public List<LootContainer> containerSpawns = new List<LootContainer>();
        public List<ulong> containerSpawnIDs = new List<ulong>();
        public List<Door> doors = new List<Door>();
        bool ManualStart = false;

        string GetRandomArena()
        {
            List<string> arenas = Pool.GetList<string>();
            arenas.AddRange(Arenas.Keys);
            var name = arenas.GetRandom();
            Pool.FreeList(ref arenas);
            return name;
        }

        [ConsoleCommand("startarena")]
        void StartArena(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && !IsAdmin(player))
            {
                arg.ReplyWith(lang.GetMessage("Prefix", this, player?.UserIDString) + lang.GetMessage("NoPerms", this, player?.UserIDString));
                return;
            }
            if (IsRunning || CanJoin)
            {
                arg.ReplyWith(lang.GetMessage("Prefix", this, player?.UserIDString) + lang.GetMessage("EventRunning", this, player?.UserIDString));
                return;
            }
            if (IsDespawning)
            {
                arg.ReplyWith(lang.GetMessage("Prefix", this, player?.UserIDString) + lang.GetMessage("EventStillDespawning", this, player?.UserIDString));
                return;
            }
            float heightMod = arg.Args != null && arg.Args.Length > 0 ? Convert.ToSingle(arg.Args[0]) : 0f;
            int timerOverride = arg.Args != null && arg.Args.Length > 1 ? Convert.ToInt32(arg.Args[1]) : 0;
            string arenaName = arg.Args != null && arg.Args.Length > 2 ? arg.Args[2] : GetRandomArena();
            string profile = arg.Args != null && arg.Args.Length > 3 ? string.Join(" ", arg.Args.Skip(3)) : null;
            if (profile != null)
            {
                bool validated_profile = false;
                foreach (var prof in config.lootSettings)
                {
                    if (prof.Key.Equals(profile, StringComparison.OrdinalIgnoreCase))
                    {
                        validated_profile = true;
                        profile = prof.Key;
                        break;
                    }
                }
                if (!validated_profile)
                {
                    arg.ReplyWith(lang.GetMessage("Prefix", this, player?.UserIDString) + string.Format(lang.GetMessage("InvalidProfile", this, player?.UserIDString), profile, string.Join("\n- ", config.lootSettings.Keys)));
                    return;
                }
            }
            ManualStart = true;
            if (timerOverride > 0) LobbyTime = timerOverride;
            StartEvent(arenaName, heightMod, profile);
        }

        [ChatCommand("startarena")]
        void SpawnChatCMD(BasePlayer player, string cmd, string[] args)
        {
            if (!IsAdmin(player))
            {
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("NoPerms", this, player.UserIDString));
                return;
            }
            if (IsRunning || CanJoin)
            {
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("EventRunning", this, player.UserIDString));
                return;
            }
            float heightMod = args != null && args.Length >= 1 ? Convert.ToSingle(args[0]) : 0f;
            int timerOverride = args != null && args.Length >= 2 ? Convert.ToInt32(args[1]) : 0;
            string arenaName = args != null && args.Length >= 3 ? Arenas.ContainsKey(args[2]) ? args[2] : GetRandomArena() : GetRandomArena();
            string profile = args != null && args.Length >= 3 ? string.Join(" ", args.Skip(3)) : null;

            if (profile != null)
            {
                bool validated_profile = false;
                foreach (var prof in config.lootSettings)
                {
                    if (prof.Key.Equals(profile, StringComparison.OrdinalIgnoreCase))
                    {
                        validated_profile = true;
                        profile = prof.Key;
                        break;
                    }
                }
                if (!validated_profile)
                {
                    PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + string.Format(lang.GetMessage("InvalidProfile", this, player.UserIDString), profile, string.Join("\n- ", config.lootSettings.Keys)));
                    return;
                }
            }            

            ManualStart = true;
            if (timerOverride > 0) LobbyTime = timerOverride;
            StartEvent(arenaName, heightMod, profile);
        }

        [ConsoleCommand("endarena")]
        void EndArenaConsole(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && !IsAdmin(player))
            {
                arg.ReplyWith(lang.GetMessage("Prefix", this, player?.UserIDString) + lang.GetMessage("NoPerms", this, player?.UserIDString));
                return;
            }

            EndEvent();
        }

        [ChatCommand("endarena")]
        void EndArenaCMD(BasePlayer player)
        {
            if (!IsAdmin(player))
            {
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("NoPerms", this, player.UserIDString));
                return;
            }
            EndEvent();
        }

        void StartSpawnSequence(float height = 0f, bool runEvent = true)
        {
            Puts("Attempting to build arena.");
            if (spawned_entities.Count > 0)
            {
                Puts("Stopping build attempt while we despawn the old arena.");
                ServerMgr.Instance.StartCoroutine(DespawnEntities(height, true));
                return;
            }
            Spawn_routine = ServerMgr.Instance.StartCoroutine(SpawnEntities(height, runEvent));
        }

        public IEnumerator DespawnEntities(float heightMod = 0f, bool startArena = false, bool unloaded = false)
        {
            IsDespawning = true;
            Puts("Starting despawn process.");
            Unsubscribe(nameof(OnEntityKill));
            foreach (var _timer in ContainerRespawnTimers)
            {
                if (_timer != null && !_timer.Destroyed) _timer.Destroy();
            }
            ContainerRespawnTimers?.Clear();
            int count = 0;
            if (spawned_entities.Count > 0)
            {
                Puts($"Starting despawn process. Entities to despawn: {spawned_entities.Count}");

                foreach (var entity in spawned_entities)
                {
                    try
                    {
                        if (entity != null && !entity.IsDestroyed)
                        {
                            count++;
                            entity.Kill();
                        }
                    }                    
                    catch { }

                    if (count >= config.max_procs_per_tick && !unloaded)
                    {
                        count = 0;
                        yield return CoroutineEx.waitForEndOfFrame;
                    }
                }
                Puts("Finished despawning arena entities.");
                spawned_entities.Clear();
            }

            count = 0;
            if (containerSpawns.Count > 0)
            {
                Puts("Starting despawn process for container spawns.");
                foreach (var crate in containerSpawns)
                {
                    try
                    {
                        if (crate != null && !crate.IsDestroyed)
                        {
                            crate.Kill();
                            count++;
                        }
                    }
                    catch { }

                    if (count >= config.max_procs_per_tick && !unloaded)
                    {
                        count = 0;
                        yield return CoroutineEx.waitForEndOfFrame;
                    }
                }
                Puts("Finished despawning container entities.");
                containerSpawns.Clear();
                containerSpawnIDs.Clear();
            }

            if (PlayerCorpses.Count > 0)
            {
                foreach(var corpse in PlayerCorpses)
                {
                    try
                    {
                        corpse.Kill();
                    }
                    catch { }
                }
            }
            PlayerCorpses?.Clear();
            CorpseMonitor?.Clear();

            if (!unloaded) yield return CoroutineEx.waitForEndOfFrame;

            Subscribe(nameof(OnEntityKill));
            Puts("Finished despawn process.");
            IsDespawning = false;            
            NextTick(() => Despawn_routine = null);
            if (startArena) StartSpawnSequence(heightMod);
        }

        Coroutine Spawn_routine;
        Coroutine Despawn_routine;
        static float FurthestEntity;

        public IEnumerator SpawnEntities(float height = 0f, bool runEvent = true)
        {
            int count = 0;
            EventElevationMod = height;
            CurrentCentrePoint = GetModifiedVector(CurrentArena.CenterPoint);

            foreach (var entry in CurrentArena.entities)
            {
                try
                {
                    var pos = new Vector3(entry.pos.x, entry.pos.y + height, entry.pos.z);
                    var eulerRotation = entry.rot;
                    BaseEntity entity;
                    if (entry.prefab == "assets/prefabs/deployable/playerioents/gates/branch/electrical.branch.deployed.prefab")
                    {
                        var rot = new Vector3(eulerRotation.x, eulerRotation.y + UnityEngine.Random.Range(-90f, 90f), eulerRotation.z);
                        entity = GameManager.server.CreateEntity(GetRandomBush(pos), pos, Quaternion.Euler(rot));
                    }
                    else if (entry.prefab == "assets/prefabs/misc/xmas/pookie/pookie_deployed.prefab")
                    {
                        var rot = new Vector3(eulerRotation.x, eulerRotation.y + UnityEngine.Random.Range(-90f, 90f), eulerRotation.z);
                        entity = GameManager.server.CreateEntity(GetRandomTree(pos), pos, Quaternion.Euler(rot));
                    }
                    else if (entry.prefab == "assets/bundled/prefabs/modding/admin/admin_rock_formation_medium_a.prefab")
                    {
                        var rot = new Vector3(eulerRotation.x, eulerRotation.y + UnityEngine.Random.Range(-90f, 90f), eulerRotation.z);
                        entity = GameManager.server.CreateEntity(entry.prefab, pos, Quaternion.Euler(rot));
                    }
                    else if (entry.prefab == "assets/prefabs/deployable/playerioents/lights/flasherlight/electric.flasherlight.deployed.prefab")
                    {
                        var rot = new Vector3(eulerRotation.x, eulerRotation.y + UnityEngine.Random.Range(-90f, 90f), eulerRotation.z);
                        entity = GameManager.server.CreateEntity(GetRandomLog(pos), pos, Quaternion.Euler(rot));
                    }
                    else if (entry.prefab == "assets/bundled/prefabs/radtown/crate_normal_2.prefab")
                    {
                        var rot = new Vector3(eulerRotation.x, eulerRotation.y + UnityEngine.Random.Range(-90f, 90f), eulerRotation.z);
                        entity = GameManager.server.CreateEntity(entry.prefab, pos, Quaternion.Euler(rot));
                        containerSpawns.Add(entity as LootContainer);
                    }
                    else entity = GameManager.server.CreateEntity(entry.prefab, pos, Quaternion.Euler(eulerRotation));
                    if (entity == null) continue;

                    entity.enableSaving = false;

                    var stabilityEntity = entity as StabilityEntity;

                    if (stabilityEntity != null)
                    {
                        stabilityEntity.grounded = true;
                    }
                    var buildingBlock = entity as BuildingBlock;

                    if (buildingBlock != null)
                    {
                        buildingBlock.blockDefinition = PrefabAttribute.server.Find<Construction>(buildingBlock.prefabID);
                        buildingBlock.SetGrade(BuildingGrade.Enum.Wood);
                        buildingBlock.grounded = true;
                    }

                    entity.Spawn();

                    if (entry.prefab == "assets/bundled/prefabs/radtown/crate_normal_2.prefab") containerSpawnIDs.Add(entity.net.ID.Value);

                    var baseCombat = entity as BaseCombatEntity;
                    if (baseCombat != null) baseCombat.SetHealth(baseCombat.MaxHealth());

                    var cp = CurrentArena.CenterPoint;
                    if (entity is Door)
                    {
                        NextTick(() =>
                        {
                            try
                            {
                                SetupDoor(entity as Door);
                            }
                            catch { }
                        });
                    }
                    spawned_entities.Add(entity);
                    spawnData.spawnedEntities.Add(entity.net.ID.Value);

                    var distFromCentre = Vector3.Distance(entity.transform.position, CurrentCentrePoint);
                    if (distFromCentre > FurthestEntity)
                    {
                        FurthestEntity = distFromCentre;
                    }
                }
                catch { }

                count++;
                if (count >= config.max_procs_per_tick)
                {
                    count = 0;
                    yield return CoroutineEx.waitForEndOfFrame;
                }
            }


            if (runEvent) NextTick(() => ArenaSpawned());
            Spawn_routine = null;
            FurthestEntity += 10f;
        }

        void SetupDoor(Door door)
        {
            if (door == null) return;
            if (!doors.Contains(door))
            {
                doors.Add(door);
            }
                
            if (door.IsLocked()) return;
            var key_lock = GameManager.server.CreateEntity("assets/prefabs/locks/keylock/lock.key.prefab") as KeyLock;
            key_lock.gameObject.Identity();
            key_lock.SetParent(door, door.GetSlotAnchorName(BaseEntity.Slot.Lock));
            key_lock.Spawn();
            door.SetSlot(BaseEntity.Slot.Lock, key_lock);
            key_lock.SetFlag(BaseEntity.Flags.Locked, true);
            spawned_entities.Add(key_lock);
        }

        // Add biome parameter later.
        string GetRandomBush(Vector3 pos)
        {
            switch (GetBiome(pos))
            {
                case BiomeType.Arid: return config.biome_bushes[BiomeType.Arid].GetRandom();
                case BiomeType.Temperate: return config.biome_bushes[BiomeType.Temperate].GetRandom();
                case BiomeType.Arctic: return config.biome_bushes[BiomeType.Arctic].GetRandom();
                case BiomeType.Tundra: return config.biome_bushes[BiomeType.Tundra].GetRandom();
            }

            return "assets/bundled/prefabs/autospawn/resource/v3_bushes_temp/bush_spicebush_b.prefab";
        }

        string GetRandomTree(Vector3 pos)
        {
            switch (GetBiome(pos))
            {
                case BiomeType.Arid: return config.biome_trees[BiomeType.Arid].GetRandom();
                case BiomeType.Temperate: return config.biome_trees[BiomeType.Temperate].GetRandom();
                case BiomeType.Arctic: return config.biome_trees[BiomeType.Arctic].GetRandom();
                case BiomeType.Tundra: return config.biome_trees[BiomeType.Tundra].GetRandom();
            }

            return "assets/bundled/prefabs/autospawn/resource/v3_temp_forest_pine/pine_a.prefab";
        }

        string GetRandomLog(Vector3 pos)
        {
            switch (GetBiome(pos))
            {
                case BiomeType.Arid: return config.biome_logs[BiomeType.Arid].GetRandom();
                case BiomeType.Temperate: return config.biome_logs[BiomeType.Temperate].GetRandom();
                case BiomeType.Arctic: return config.biome_logs[BiomeType.Arctic].GetRandom();
                case BiomeType.Tundra: return config.biome_logs[BiomeType.Tundra].GetRandom();
            }

            return "assets/bundled/prefabs/autospawn/resource/logs_dry/dead_log_a.prefab";
        }

        public enum BiomeType
        {
            Arid,
            Temperate,
            Tundra,
            Arctic
        }

        BiomeType GetBiome(Vector3 pos)
        {
            if (TerrainMeta.BiomeMap.GetBiome(pos, 1) > 0.5f) return BiomeType.Arid;
            if (TerrainMeta.BiomeMap.GetBiome(pos, 2) > 0.5f) return BiomeType.Temperate;
            if (TerrainMeta.BiomeMap.GetBiome(pos, 4) > 0.5f) return BiomeType.Tundra;
            if (TerrainMeta.BiomeMap.GetBiome(pos, 8) > 0.5f) return BiomeType.Arctic;
            return BiomeType.Temperate;
        }

        #endregion

        #region Loot handling

        void OnEntityKill(LootContainer container)
        {
            if (containerSpawns.Contains(container))
            {
                containerSpawns.Remove(container);
                containerSpawnIDs.Remove(container.net.ID.Value);
                var pos = container.transform.position;
                HandleRespawn(pos);
            }
        }

        List<Timer> ContainerRespawnTimers = new List<Timer>();

        void HandleRespawn(Vector3 pos)
        {
            ContainerRespawnTimers.Add(timer.Once(UnityEngine.Random.Range(config.gameSettings.min_crate_respawn_time, config.gameSettings.max_crate_respawn_time), () =>
            {
                var rot = new Vector3(0, UnityEngine.Random.Range(-90f, 90f), 0);
                var container = GameManager.server.CreateEntity("assets/bundled/prefabs/radtown/crate_normal_2.prefab", pos, Quaternion.Euler(rot)) as LootContainer;
                containerSpawns.Add(container);
                container.Spawn();
                containerSpawnIDs.Add(container.net.ID.Value);
            }));
        }

        #endregion

        #region Event

        List<BasePlayer> Participants = new List<BasePlayer>();
        bool CanJoin = false;
        bool IsRunning = false;
        bool IsDespawning = false;
        int LobbyTime = 300;
        bool DevMode = false;

        string LootProfile;
        static float EventElevationMod = 0f;

        static Vector3 GetModifiedVector(Vector3 pos)
        {
            if (EventElevationMod == 0) return pos;
            else return new Vector3(pos.x, pos.y + EventElevationMod, pos.z);
        }

        void StartEvent(string arenaName, float elevationMod = 0f, string profile = null)
        {
            if (config.lootSettings == null || config.lootSettings.Count == 0)
            {
                Puts("Failed to start event - no loot settings.");
                return;
            }
            if (string.IsNullOrEmpty(arenaName))
            {
                Puts("Failed to load the arena - no valid arena specified.");
                return;
            }
            CurrentArena = Arenas[arenaName];
            if (string.IsNullOrEmpty(profile))
            {
                LootProfile = GetRandomProfile();
            }
            else LootProfile = profile;
            if (!config.lootSettings.ContainsKey(LootProfile))
            {
                Puts($"Failed to start event - settings failure.");
                return;
            }
            Puts($"Set loot profile: {LootProfile}");
            IsRunning = true;

            Subscribe(nameof(OnLootSpawn));
            Subscribe(nameof(OnPlayerDeath));
            Subscribe(nameof(OnPlayerCorpseSpawn));
            Subscribe(nameof(CanLootEntity));
            Subscribe(nameof(CanLootPlayer));
            Subscribe(nameof(OnEntityKill));
            Subscribe(nameof(CanHelicopterTarget));
            Subscribe(nameof(CanHelicopterStrafeTarget));
            Subscribe(nameof(CanBuild));
            Subscribe(nameof(CanDeployItem));
            Subscribe(nameof(OnPlayerWound));
            Subscribe(nameof(OnItemDropped));
            SubscribeThirdpartyHooks(true);
            StartSpawnSequence(elevationMod);
        }

        Timer LobbyTimer;
        Timer GameStartTimer;
        Timer OutsiderCheckTimer;

        void DestroyTimer(Timer _timer)
        {
            if (_timer != null && !_timer.Destroyed) _timer.Destroy();
        }

        void ArenaSpawned()
        {
            Puts("Arena has finished spawning.");
            CircleDist = CurrentArena.size;
            if (config.gameSettings.interference_settings.check_for_outsiders)
            {
                DestroyTimer(OutsiderCheckTimer);
                OutsiderCheckTimer = timer.Every(config.gameSettings.interference_settings.check_time, () =>
                {
                    CheckForOutsiders();
                });
            }
            CanJoin = true;            
            EventHelper.Call("EMUpdateLobby", this.Name, CurrentArena.CenterPoint);
            foreach (var player in BasePlayer.activePlayerList)
            {
                string s = lang.GetMessage("Prefix", this, player.UserIDString) + string.Format(lang.GetMessage("SurvivalArenaStartingBeginIn", this, player.UserIDString), LobbyTime, config.command_settings.join_command);
                PrintToChat(player, s);
                SendGUIAnnouncement(player, s);
            }

            if (ManualStart) EventHelper.Call("EMManuallyStarted", this.Name);
            EventHelper.Call("EMStartEvent", this.Name);
            int timeElapsed = 0;

            if (doors.Count < 4)
            {
                foreach (var entity in spawned_entities.OfType<Door>())
                {
                    if (!entity.IsLocked())
                    {
                        SetupDoor(entity);
                    }
                }
            }
            else
            {
                foreach (var door in doors)
                {
                    if (!door.IsLocked())
                    {
                        SetupDoor(door);
                    }
                }
            }

            if (LobbyTimer != null && !LobbyTimer.Destroyed) LobbyTimer.Destroy();

            int nextNotify = LobbyTime > 60 ? LobbyTime - 60 : LobbyTime > 30 ? 30 : LobbyTime > 10 ? 10 : 0;

            LobbyTimer = timer.Every(1f, () =>
            {
                // Do lobby time reminders.

                timeElapsed++;
                if (timeElapsed > LobbyTime)
                {
                    CanJoin = false;
                    if (Participants.Count < config.gameSettings.min_player)
                    {
                        foreach (var player in BasePlayer.activePlayerList)
                        {
                            string s = lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("Cancelled", this, player.UserIDString);
                            PrintToChat(player, s);
                            SendGUIAnnouncement(player, s);
                        }
                        EndEvent();
                        return;
                    }
                    
                    MessageParticipants("DoorsOpeningSoon");
                    if (!string.IsNullOrEmpty(config.soundsSettings.sound_for_starting)) PlaySoundToPlayers(Participants, config.soundsSettings.sound_for_starting, CurrentCentrePoint);
                    if (GameStartTimer != null && !GameStartTimer.Destroyed) GameStartTimer.Destroy();

                    foreach (var p in Participants)
                    {
                        CuiHelper.DestroyUi(p, "STARTCountdownUI");
                    }

                    GameStartTimer = timer.Once(5f, () =>
                    {
                        StartPlay();
                    });

                    if (LobbyTimer != null && !LobbyTimer.Destroyed) LobbyTimer.Destroy();
                    return;
                }
                else
                {
                    var timeLeft = LobbyTime - timeElapsed;
                    if (timeLeft <= nextNotify && timeLeft > 0)
                    {
                        foreach (var player in BasePlayer.activePlayerList)
                        {
                            string s = string.Format(lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("SurvivalArenaStartingBeginIn", this, player.UserIDString), nextNotify, config.command_settings.join_command);
                            PrintToChat(player, s);
                            SendGUIAnnouncement(player, s);
                        }
                        
                        nextNotify = timeLeft > 60 ? timeLeft - 60 : timeLeft > 30 ? 30 : timeLeft > 10 ? 10 : 0;
                    }
                    foreach (var p in Participants)
                    {
                        STARTCountdownUI(p, timeLeft);
                    }
                }
            });
        }

        public List<BasePlayer> Intruders = new List<BasePlayer>();

        void CheckForOutsiders()
        {
            Intruders.Clear();
            FindPlayers(Intruders, CurrentCentrePoint, FurthestEntity + DistanceThreshold);
            if (Intruders == null) return;
            foreach (var player in Intruders)
            {
                if (player.HasPlayerFlag(BasePlayer.PlayerFlags.IsAdmin)) continue;
                if (!Participants.Contains(player) && !HasOutsiderMonitor(player) && player.transform.position.y > CurrentCentrePoint.y - 50)
                    AddOutsiderMonitor(player);
            }
        }

        private static void FindPlayers(List<BasePlayer> players, Vector3 a, float n, int m = Layers.Mask.Player_Server)
        {
            int hits = Physics.OverlapSphereNonAlloc(a, n, Vis.colBuffer, m, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hits; i++)
            {
                var entity = Vis.colBuffer[i]?.ToBaseEntity();
                if (entity is BasePlayer) players.Add(entity as BasePlayer);
                Vis.colBuffer[i] = null;
            }
            //Interface.Oxide.LogInfo($"Found {players.Count} players.");
        }

        void PlaySoundToPlayers(List<BasePlayer> players, string effect, Vector3 pos)
        {
            foreach (var player in players)
            {
                EffectNetwork.Send(new Effect(effect, pos, pos), player.net.connection);
            }
        }

        void PlaySound(BasePlayer player, string effect)
        {
            EffectNetwork.Send(new Effect(effect, player.transform.position, player.transform.position), player.net.connection);
        }

        void MessageParticipants(string lang_string)
        {
            foreach (var player in Participants)
            {
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage(lang_string, this, player.UserIDString));
            }
        }

        Timer CircleTimer;
        static float CircleDist;

        void StartPlay()
        {
            foreach (var player in Participants)
            {
                CircleStatusHUD(player, 0);
                if (config.gameSettings.commands_on_start.Count > 0)
                {
                    // Running commands for each player
                    foreach (var _command in config.gameSettings.commands_on_start)
                    {
                        try
                        {
                            string command_string = _command.Replace("{id}", player.UserIDString);
                            rust.RunServerCommand(command_string);
                        }
                        catch
                        {
                            Puts($"Exception: Failed to run command: {_command} for {player.displayName}");
                        }
                    }
                }
            }
            SendCounterUpdate();
            Subscribe(nameof(CanEntityTakeDamage));
            foreach (var door in doors)
            {
                door.SetOpen(true);
            }

            if (CircleTimer != null && !CircleTimer.Destroyed) CircleTimer.Destroy();

            var count = 0;
            bool triggered = false;            
            CircleTimer = timer.Every(1f, () =>
            {
                count++;
                if (count > config.radiationZoneSettings.circle_delay)
                {
                    if (!triggered)
                    {
                        triggered = true;
                        CreateSphere(CurrentCentrePoint, CircleDist, config.radiationZoneSettings.darkness, 0.5f);
                        MessageParticipants("CircleClosingStarted");
                        foreach (var player in Participants)
                        {
                            CircleStatusHUD(player, 1);
                        }
                    }
                    else
                    {
                        var shrinkDist = CircleDist - 0.5f;
                        if ((shrinkDist) > config.radiationZoneSettings.min_ring_size)
                        {
                            CircleDist = shrinkDist;
                        }
                        else
                        {                            
                            destroyspheres();
                            CreateSphere(CurrentCentrePoint, config.radiationZoneSettings.min_ring_size, config.radiationZoneSettings.darkness, 0);
                            MessageParticipants("CircleClosingFinished");
                            foreach (var player in Participants)
                            {
                                CircleStatusHUD(player, 2);
                            }
                            if (!CircleTimer.Destroyed) CircleTimer.Destroy();
                        }
                    }
                }
            });
        }

        bool IsEnding = false;

        BasePlayer WinnerWatch;

        void CheckWin()
        {
            if (CanJoin || IsEnding || Participants.Count > 1) return;

            if (Participants.Count == 1)
            {
                var winner = Participants[0];
                
                foreach (var player in BasePlayer.activePlayerList)
                {
                    string s = lang.GetMessage("Prefix", this, player.UserIDString) + string.Format(lang.GetMessage("AnnounceWinner", this, player.UserIDString), winner.displayName);
                    PrintToChat(player, s);
                    SendGUIAnnouncement(player, s);
                }
                string prize_str = lang.GetMessage("Prefix", this, winner.UserIDString) + lang.GetMessage("MsgToWinner", this, winner.UserIDString);
                // Handle prize.
                if (config.prize_settings.auto_award)
                {
                    // Add winners to a list, when the game finishes, give them the awards using the event finish hook.
                    WinnerWatch = winner;
                }
                else
                {
                    if (config.prize_settings.rolls_per_claim > 0)
                    {
                        PCDInfo pi;
                        if (!pcdData.pEntity.TryGetValue(winner.userID, out pi)) pcdData.pEntity.Add(winner.userID, pi = new PCDInfo());
                        pi.rewards_remaining++;
                        var s = string.Format(lang.GetMessage("MessageWinner", this, winner.UserIDString), pi.rewards_remaining);
                        prize_str += s;
                        if (config.notifications.sendNotify.enabled && Notify != null && Notify.IsLoaded) Notify.Call("SendNotify", winner.userID, config.notifications.sendNotify.notifyType, s);
                    }
                }

                string currency_out;
                if (config.prize_settings.economic_reward.max_amount > 0 && Economics != null && Economics.IsLoaded)
                {
                    GiveEconomicReward(winner, out currency_out);
                    var s = string.Format(lang.GetMessage("EconomicsWon", this, winner.UserIDString), currency_out);                    
                    prize_str += s;
                    if (config.notifications.sendNotify.enabled && Notify != null && Notify.IsLoaded) Notify.Call("SendNotify", winner.userID, config.notifications.sendNotify.notifyType, s);
                }
                if (config.prize_settings.srp_reward.max_amount > 0 && ServerRewards != null && ServerRewards.IsLoaded)
                {
                    GiveServerReward(winner, out currency_out);
                    var s = string.Format(lang.GetMessage("ServerRewardsWon", this, winner.UserIDString), currency_out);
                    prize_str += s;
                    if (config.notifications.sendNotify.enabled && Notify != null && Notify.IsLoaded) Notify.Call("SendNotify", winner.userID, config.notifications.sendNotify.notifyType, s);
                }

                if (config.prize_settings.SkillTree_XP_Reward > 0 && SkillTree != null && SkillTree.IsLoaded)
                {
                    GiveSkilTreeXP(winner, config.prize_settings.SkillTree_XP_Reward);
                    prize_str += string.Format(lang.GetMessage("SkillTreeXPWon", this, winner.UserIDString), config.prize_settings.SkillTree_XP_Reward);
                }

                PrintToChat(winner, prize_str);

                IsEnding = true;

                Interface.CallHook("OnSurvivalArenaWin", winner);

                timer.Once(3f, () =>
                {
                    EndEvent();
                });

                return;
            }
            else if (Participants.Count == 0 && !CanJoin)
            {                
                foreach (var player in BasePlayer.activePlayerList)
                {
                    string s = lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("NobodyWon", this, player.UserIDString);
                    PrintToChat(player, s);
                    SendGUIAnnouncement(player, s);
                }
                IsEnding = true;
                timer.Once(3f, () =>
                {
                    EndEvent();
                });
                return;
            }
            else SendCounterUpdate();
        }

        void SendGUIAnnouncement(BasePlayer player, string message)
        {
            if (!config.notifications.GUIAnnouncements.enabled || GUIAnnouncements == null || !GUIAnnouncements.IsLoaded) return;
            GUIAnnouncements.Call("CreateAnnouncement", message, config.notifications.GUIAnnouncements.banner_colour, config.notifications.GUIAnnouncements.text_colour, player, config.notifications.GUIAnnouncements.position_adjustment);
        }

        //WinnerWatch
        void EMOnEventLeft(BasePlayer player, string eventName)
        {
            if (config.prize_settings.rolls_per_claim == 0 || eventName != this.Name || WinnerWatch == null || player != WinnerWatch) return;
            // Handle prizes

            for (int i = 0; i < config.prize_settings.rolls_per_claim; i++)
            {
                RollReward(player);
            }
            PlaySound(player, config.soundsSettings.sound_for_prize);

            WinnerWatch = null;
        }

        void GiveEconomicReward(BasePlayer player, out string amount)
        {
            var am = (double)UnityEngine.Random.Range(Math.Max(config.prize_settings.economic_reward.min_amount, 1), Math.Max(config.prize_settings.economic_reward.max_amount, 1) + 1);
            amount = am.ToString();

            if (!Convert.ToBoolean(Economics.Call("Deposit", player.UserIDString, am)))
            {
                Puts($"Failed to give {player.displayName} their economics prize for some reason.");
            }
        }

        void GiveServerReward(BasePlayer player, out string amount)
        {
            var am = UnityEngine.Random.Range(Math.Max(config.prize_settings.srp_reward.min_amount, 1), Math.Max(config.prize_settings.srp_reward.max_amount, 1) + 1);
            amount = am.ToString();

            if (!Convert.ToBoolean(ServerRewards.Call("AddPoints", player.UserIDString, am)))
            {
                Puts($"Failed to give {player.displayName} their server rewards prize for some reason.");
            }
        }

        void GiveSkilTreeXP(BasePlayer player, double amount)
        {
            SkillTree.Call("AwardXP", player, amount, this.Name);
        }

        void LeaveEventCMD(BasePlayer player)
        {
            if (Participants.Contains(player))
            {
                if (NightVision != null && NightVision.IsLoaded && config.gameSettings.use_nightvision)
                {
                    NightVision.Call("UnlockPlayerTime", player);
                    permission.RevokeUserPermission(player.UserIDString, "nightvision.allowed");
                }
                LeaveEvent(player);
            }
        }
                
        void JoinEvent(BasePlayer player)
        {
            if (!CanJoin)
            {
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("FailedJoin_Closed", this, player.UserIDString));
                return;
            }
            if (!Convert.ToBoolean(EventHelper.Call("EMEnrollPlayer", player, this.Name)))
            {
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("FailedJoin_EventHelper", this, player.UserIDString));
                return;
            }
            if (Participants.Contains(player))
            {
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("FailedJoin_Enrolled", this, player.UserIDString));
                return;
            }
            else
            {
                Participants.Add(player);
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + string.Format(lang.GetMessage("JoinAnnounce", this, player.UserIDString), player.displayName, Participants.Count, config.gameSettings.min_player));
            }
            if (NightVision != null && NightVision.IsLoaded && config.gameSettings.use_nightvision)
            {
                permission.GrantUserPermission(player.UserIDString, "nightvision.allowed", NightVision);
                NightVision.Call("LockPlayerTime", player, 10f);
            }
            PrintToChat(player, string.Format(lang.GetMessage("JoinedTheEvent", this, player.UserIDString), config.command_settings.leave_command));
            AddMonitor(player);
            EventLeaveButton(player);
        }

        void LeaveEvent(BasePlayer player, bool died = false, bool event_ended = false)
        {
            CuiHelper.DestroyUi(player, "BleedingPanel");
            CuiHelper.DestroyUi(player, "PlayerCounter");
            CuiHelper.DestroyUi(player, "CircleStatusHUD");
            CuiHelper.DestroyUi(player, "EventLeaveButton");
            if (Participants.Contains(player))
            {
                player.inventory.Strip();

                DestroyMonitor(player);
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("LeftCompetition", this, player.UserIDString));

                Participants.Remove(player);
                CuiHelper.DestroyUi(player, "STARTCountdownUI");

                if (Participants.Count == 1) player.inventory.Strip();

                if (!died) EventHelper.Call("EMPlayerLeaveEvent", player, this.Name, true);

                if (NightVision != null && NightVision.IsLoaded && config.gameSettings.use_nightvision)
                {
                    NightVision.Call("UnlockPlayerTime", player);
                    permission.RevokeUserPermission(player.UserIDString, "nightvision.allowed");
                }

                if (!event_ended)
                {
                    CheckWin();
                }
            }
            else
            {
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("NotAtEvent", this, player.UserIDString));
                if (event_ended) DestroyMonitor(player);
            }
        }

        void EndEvent(bool unloaded = false)
        {
            Intruders.Clear();
            DestroyTimer(OutsiderCheckTimer);
            Puts("Ending SurvivalArena.");
            LootProfile = null;
            CanJoin = false;
            IsRunning = false;
            LobbyTime = config.gameSettings.defaultStartTime;
            ManualStart = false;
            DevMode = false;

            if (LobbyTimer != null && !LobbyTimer.Destroyed) LobbyTimer.Destroy();
            if (GameStartTimer != null && !GameStartTimer.Destroyed) GameStartTimer.Destroy();
            if (CircleTimer != null && !CircleTimer.Destroyed) CircleTimer.Destroy();
            if (CurrentArena != null) CircleDist = CurrentArena.size;
            destroyspheres();

            List<BasePlayer> _participants = Pool.GetList<BasePlayer>();
            try
            {
                _participants.AddRange(Participants);
                foreach (var player in _participants)
                {
                    player.inventory.Strip();
                    LeaveEvent(player, false, true);
                }
                
            }
            catch { }
            Pool.FreeList(ref _participants);

            try
            {
                // Removes corpses after event.
                foreach (var corpse in PlayerCorpses)
                {
                    if (corpse.inventory?.itemList != null)
                    {
                        try
                        {
                            foreach (var item in corpse.inventory.itemList)
                                item.Remove();
                        }
                        catch { }
                    }

                    try
                    {
                        corpse.Kill();
                    }
                    catch { }
                }
            }
            catch { }

            var droppedContainers = FindEntitiesOfType<DroppedItemContainer>(CurrentCentrePoint, CurrentArena.size);
            try
            {
                
                foreach (var container in droppedContainers)
                {
                    container.Kill();
                }
                
            }
            catch { }
            Pool.FreeList(ref droppedContainers);

            try
            {
                foreach (var item in EventItems)
                {
                    if (item != null) item.Remove();
                }
            }
            catch { }

            try
            {
                foreach (var p in BasePlayer.allPlayerList)
                    DestroyOutsiderMonitor(p);
            }
            catch { }

            EventHelper.Call("EMEndEvent", this.Name);
            if (!unloaded)
            {
                if (Spawn_routine != null) ServerMgr.Instance.StopCoroutine(Spawn_routine);
                Spawn_routine = null;
                Despawn_routine = ServerMgr.Instance.StartCoroutine(DespawnEntities());
            }

            UnsubHooks();

            EventItems.Clear();
            Participants.Clear();
            ContainerRespawnTimers.Clear();
            doors.Clear();

            EventElevationMod = 0f;

            CurrentArena = null;
            IsEnding = false;
        }

        #endregion

        #region Hooks

        void UnsubHooks()
        {
            Unsubscribe(nameof(OnLootSpawn));
            Unsubscribe(nameof(CanDropActiveItem));
            Unsubscribe(nameof(OnItemDropped));
            Unsubscribe(nameof(OnPlayerDeath));            
            Unsubscribe(nameof(CanLootEntity));
            Unsubscribe(nameof(CanLootPlayer));
            Unsubscribe(nameof(OnEntityKill));
            Unsubscribe(nameof(CanHelicopterTarget));
            Unsubscribe(nameof(CanHelicopterStrafeTarget));
            Unsubscribe(nameof(CanBuild));
            Unsubscribe(nameof(CanDeployItem));
            Unsubscribe(nameof(OnPlayerWound));
            Unsubscribe(nameof(CanEntityTakeDamage));

            SubscribeThirdpartyHooks(false);

            if (CorpseMonitor.Count == 0 && PlayerCorpses.Count == 0)
            {
                Unsubscribe(nameof(OnPlayerCorpseSpawn));
            }
        }

        object CanBuild(Planner planner, Construction prefab, Construction.Target target)
        {
            var player = planner?.GetOwnerPlayer();
            if (player != null && Participants.Contains(player) || (player.transform.position.y > CurrentCentrePoint.y - 50 && Vector3.Distance(player.transform.position, CurrentCentrePoint) < FurthestEntity)) return false;
            return null;
        }

        object CanDeployItem(BasePlayer player, Deployer deployer, ulong entityId)
        {
            if (player != null && Participants.Contains(player)) return false;
            return null;
        }

        object CanHelicopterTarget(PatrolHelicopterAI heli, BasePlayer player)
        {
            if (Participants.Contains(player)) return false;
            return null;
        }

        object CanHelicopterStrafeTarget(PatrolHelicopterAI entity, BasePlayer player)
        {
            if (Participants.Contains(player)) return false;
            return null;
        }

        void OnLootSpawn(LootContainer container)
        {
            if (container == null || !containerSpawns.Contains(container))
            {
                return;
            }

            timer.Once(0.1f, () =>
            {
                container.inventory.Clear();
                ItemManager.DoRemoves();

                var lootSettings = config.lootSettings[LootProfile];

                for (int i = 0; i < UnityEngine.Random.Range(lootSettings.min_items, lootSettings.max_items + 1); i++)
                {
                    var randProfile = lootSettings.items.GetRandom();
                    var item = ItemManager.CreateByName(randProfile.shortname, UnityEngine.Random.Range(randProfile.min_amount, randProfile.max_amount + 1), randProfile.skin);

                    if (item == null)
                    {
                        Puts($"Item: {randProfile.shortname} was invalid.");
                        continue;
                    }

                    if (randProfile.displayName != null)
                        item.name = randProfile.displayName;

                    EventItems.Add(item);

                    if (!item.MoveToContainer(container.inventory))
                        item.Remove();
                }
            });
        }

        List<Item> EventItems = new List<Item>();

        object CanDropActiveItem(BasePlayer player)
        {
            if (Participants.Contains(player)) return false;
            return null;
        }

        void OnItemDropped(Item item, BaseEntity entity)
        {
            if (item == null) return;
            if (EventItems.Contains(item)) NextTick(() =>
            {
                if (item != null) EventItems.Remove(item);
                entity?.KillMessage();
            });
            else if (entity != null && CurrentCentrePoint.y - 50 < entity.transform.position.y) NextTick(() => entity?.KillMessage());
        }

        [PluginReference]
        private Plugin EventHelper, NightVision, Economics, ServerRewards, Notify, GUIAnnouncements, SkillTree;

        private static SurvivalArena Instance { get; set; }

        void OnServerInitialized(bool initial)
        {
            if (EventHelper == null)
            {
                Puts("EventHelper is required to run this plugin.");
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }
            Instance = this;
            EventHelper.Call("EMCreateEvent", this.Name, config.eventHelperSettings.use_event_helper_timer, true, true, true, config.eventHelperSettings.give_items_back_on_death, true, Vector3.zero, true, config.eventHelperSettings.allow_teams);
            EventHelper.Call("EMExternalPluginSettings", this.Name);
            EventHelper.Call("EMBlackListCommands", this.Name, config.eventHelperSettings.prevent_commands);
            if (HaveArenaFile)
            {
                List<KeyValuePair<string, ArenaData>> arenas = Pool.GetList< KeyValuePair<string, ArenaData>> ();
                arenas.AddRange(Arenas);
                foreach (var arena in arenas)
                {
                    if (arena.Value.CenterPoint == Vector3.zero)
                    {
                        Puts($"Failed to load {arena.Key} due to an invalid Centre Point.");
                        Arenas.Remove(arena.Key);
                    }
                }
                Pool.FreeList(ref arenas);
                if (Arenas.Count == 0)
                {
                    Puts("No valid arenas. Unloading plugin.");
                    Interface.Oxide.UnloadPlugin(Name);
                    return;
                }
            }
            else
            {
                if (!RequestMade) AddArenaFile();
            }
                
            RadAccumulation = config.radiationZoneSettings.rad_increase_per_tick;
            CheckInterval = config.radiationZoneSettings.check_interval;
            bool DoSave = false;
            if (config.lootSettings == null || config.lootSettings.Count == 0)
            {
                config.lootSettings = DefaultLootSettings;
                DoSave = true;
            }
            else
            {
                foreach (var prof in config.lootSettings)
                {
                    if (prof.Value.profileWeight == 0)
                    {
                        prof.Value.profileWeight = 100;
                        DoSave = true;
                    }
                }
            }
            if (config.prize_settings.prizes.Count == 0)
            {
                config.prize_settings.prizes = DefaultPrizes;
                DoSave = true;
            }

            if (DoSave) SaveConfig();

            if (config.gameSettings.clear_arena_on_server_start && (string.IsNullOrEmpty(ConVar.Server.levelurl) || !ConVar.Server.levelurl.Contains("SurvivalArena")))
            {
                WipeOldArena();
            }
            LobbyTime = config.gameSettings.defaultStartTime;

            if (!config.prize_settings.auto_award) Unsubscribe(nameof(EMOnEventLeft));

            SecondsToVacate = config.gameSettings.interference_settings.seconds_to_vacate;
            DistanceThreshold = config.gameSettings.interference_settings.dist_from_edge_restriction;

            SubscribeThirdpartyHooks(false);
        }

        void WipeOldArena(BasePlayer player = null)
        {
            if (IsRunning || Spawn_routine != null || Despawn_routine != null)
            {
                if (player != null) PrintToChat(player, "Cannot wipe old arena data while a game is running, or if when the area is spawning or despawning.");
                else Puts("Cannot wipe old arena data while a game is running, or if when the area is spawning or despawning.");
                return;
            }
            if (WipingOldData)
            {
                if (player != null) PrintToChat(player, "Still wiping old data.");
                else Puts("Still wiping old data.");
                return;
            }

            if (spawnData.spawnedEntities.Count == 0)
            {
                if (player != null) PrintToChat(player, "There are no entities to wipe");
                else Puts("There are no entities to wipe");
                return;
            }

            foreach (var entity in BaseNetworkable.serverEntities)
            {
                if (spawnData.spawnedEntities.Contains(entity.net.ID.Value)) OldEntitiesWipeList.Add(entity);
            }

            if (player != null) PrintToChat(player, $"Found {OldEntitiesWipeList.Count} entities to wipe");
            else Puts($"Found {OldEntitiesWipeList.Count} entities to wipe");
            if (OldEntitiesWipeList.Count > 0) ServerMgr.Instance.StartCoroutine(DoArenaWipe());
            else spawnData.spawnedEntities.Clear();
        }

        [ChatCommand("forcewipeoldarena")]
        void ForceWipeOldArenaChat(BasePlayer player)
        {
            if (!IsAdmin(player))
            {
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("NoPerms", this, player.UserIDString));
                return;
            }

            ForceWipeArena(player);
        }

        void ForceWipeArena(BasePlayer player = null)
        {
            if (IsRunning || Spawn_routine != null || Despawn_routine != null)
            {
                if (player != null) PrintToChat(player, "Cannot wipe old arena data while a game is running, or if when the area is spawning or despawning.");
                else Puts("Cannot wipe old arena data while a game is running, or if when the area is spawning or despawning.");
                return;
            }
            if (WipingOldData)
            {
                if (player != null) PrintToChat(player, "Still wiping old data.");
                else Puts("Still wiping old data.");
                return;
            }
            List<BaseNetworkable> entities = Pool.GetList<BaseNetworkable>();
            entities.AddRange(BaseNetworkable.serverEntities);

            if (entities.Count == 0)
            {
                if (player != null) PrintToChat(player, "Could not find any entities to wipe.");
                else Puts("Could not find any entities to wipe.");
                Pool.FreeList(ref entities);
                return;
            }

            foreach (var arenaData in Arenas)
            {
                float heightTarget = arenaData.Value.CenterPoint.y - 40;
                foreach (var entity in entities)
                {
                    if (entity.transform.position.y > heightTarget && Vector3.Distance(entity.transform.position, arenaData.Value.CenterPoint) <= CurrentArena.size && !(entity is BaseVehicle) && !(entity is BasePlayer))
                    {
                        OldEntitiesWipeList.Add(entity);
                    }
                }
            }

            Pool.FreeList(ref entities);
            if (OldEntitiesWipeList.Count > 0) ServerMgr.Instance.StartCoroutine(DoArenaWipe());
        }

        [ConsoleCommand("forcewipevector")]
        void ForceWipeArenaVector3Console(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            ForceWipeOldArenaUsingVector3(player);
        }

        [ChatCommand("forcewipevector")]
        void ForceWipeArenaVector3Chat(BasePlayer player)
        {
            ForceWipeOldArenaUsingVector3(player);
        }
        
        void ForceWipeOldArenaUsingVector3(BasePlayer player)
        {
            if (IsRunning || Spawn_routine != null || Despawn_routine != null)
            {
                if (player != null) PrintToChat(player, "Cannot wipe old arena data while a game is running, or if when the area is spawning or despawning.");
                else Puts("Cannot wipe old arena data while a game is running, or if when the area is spawning or despawning.");
                return;
            }
            if (WipingOldData)
            {
                if (player != null) PrintToChat(player, "Still wiping old data.");
                else Puts("Still wiping old data.");
                return;
            }

            List<BaseEntity> entitiesToDestroy = Pool.GetList<BaseEntity>();
            foreach (var arena in Arenas)
            {
                var centrePos = arena.Value.CenterPoint;
                List<BaseEntity> entities = FindEntitiesOfType<BaseEntity>(centrePos, arena.Value.size);
                foreach (var entity in entities)
                {
                    if (entity is BasePlayer || entity is BaseVehicle) continue;
                    if (entity.transform.position.y < centrePos.y - 50) continue;
                    // Entity is above the arena threshold.
                    entitiesToDestroy.Add(entity);
                }
                Pool.FreeList(ref entities);
            }
            
            foreach (var entity in entitiesToDestroy)
            {
                try
                {
                    if (entity.IsFullySpawned()) entity.Kill();
                }
                catch
                {
                    Puts($"Failed to delete entity at pos: {entity?.transform.position} [Type: {entity?.GetType()}]");
                }
            }

            Pool.FreeList(ref entitiesToDestroy);
        }

        private static List<T> FindEntitiesOfType<T>(Vector3 a, float n, int m = -1) where T : BaseEntity
        {
            int hits = Physics.OverlapSphereNonAlloc(a, n, Vis.colBuffer, m, QueryTriggerInteraction.Collide);
            List<T> entities = Pool.GetList<T>();
            for (int i = 0; i < hits; i++)
            {
                var entity = Vis.colBuffer[i]?.ToBaseEntity() as T;
                if (entity != null && !entities.Contains(entity)) entities.Add(entity);
                Vis.colBuffer[i] = null;
            }
            return entities;
        }

        //void WipeOldArena(BasePlayer player = null)
        //{
        //    Puts($"IsRunning: {IsRunning}. Spawn_routine: {Spawn_routine != null}. Despawn_routine: {Despawn_routine != null}");
        //    if (IsRunning || Spawn_routine != null || Despawn_routine != null)
        //    {
        //        if (player != null) PrintToChat(player, "Cannot wipe old arena data while a game is running, or if when the area is spawning or despawning.");
        //        else Puts("Cannot wipe old arena data while a game is running, or if when the area is spawning or despawning.");
        //        return;
        //    }
        //    if (WipingOldData)
        //    {
        //        if (player != null) PrintToChat(player, "Still wiping old data.");
        //        else Puts("Still wiping old data.");
        //        return;
        //    }
        //    List<BaseNetworkable> entities = Pool.GetList<BaseNetworkable>();
        //    entities.AddRange(BaseNetworkable.serverEntities);

        //    if (entities.Count == 0)
        //    {
        //        if (player != null) PrintToChat(player, "Could not find any entities to wipe.");
        //        else Puts("Could not find any entities to wipe.");
        //        Pool.FreeList(ref entities);
        //        return;
        //    }

        //    foreach (var arenaData in Arenas)
        //    {
        //        float heightTarget = arenaData.Value.CenterPoint.y - 40;
        //        foreach (var entity in entities)
        //        {
        //            if (entity.transform.position.y > heightTarget && Vector3.Distance(entity.transform.position, arenaData.Value.CenterPoint) <= CurrentArena.size && !(entity is BaseVehicle) && !(entity is BasePlayer))
        //            {
        //                OldEntitiesWipeList.Add(entity);
        //            }
        //        }
        //    }

        //    Pool.FreeList(ref entities);
        //    if (OldEntitiesWipeList.Count > 0) ServerMgr.Instance.StartCoroutine(DoArenaWipe());            
        //}

        public bool WipingOldData = false;
        public IEnumerator DoArenaWipe()
        {
            WipingOldData = true;
            int count = 0;

            foreach (var entity in OldEntitiesWipeList)
            {
                try
                {
                    if (entity != null)
                    {
                        spawnData.spawnedEntities.Remove(entity.net.ID.Value);
                        count++;
                        entity.KillMessage();                        
                    }
                }
                catch { }
                if (count >= config.max_procs_per_tick)
                {
                    count = 0;
                    yield return CoroutineEx.waitForEndOfFrame;
                }
            }
            WipingOldData = false;
            OldEntitiesWipeList.Clear();
        }

        [ChatCommand("wipeoldarena")]
        void WipeOldArenaChat(BasePlayer player)
        {
            if (!IsAdmin(player))
            {
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("NoPerms", this, player.UserIDString));
                return;
            }

            WipeOldArena(player);
        }

        [ConsoleCommand("wipeoldarena")]
        void WipeOldArenaConsole(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null)
            {
                WipeOldArenaChat(player);
                return;
            }
            else WipeOldArena();
        }

        List<BaseNetworkable> OldEntitiesWipeList = new List<BaseNetworkable>();

        List<DroppedItemContainer> PlayerCorpses = new List<DroppedItemContainer>();
        List<ulong> CorpseMonitor = new List<ulong>();

        void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (Participants.Contains(player))
            {
                DestroyOutsiderMonitor(player);
                if (NightVision != null && NightVision.IsLoaded && config.gameSettings.use_nightvision)
                {
                    NightVision.Call("UnlockPlayerTime", player);
                    permission.RevokeUserPermission(player.UserIDString, "nightvision.allowed");
                }
                if (!string.IsNullOrWhiteSpace(config.soundsSettings.sound_for_killing) && info != null && info.InitiatorPlayer != null && Participants.Contains(info.InitiatorPlayer)) PlaySound(info.InitiatorPlayer, config.soundsSettings.sound_for_killing);

                var container = MovePlayerLootToDropContainer(player);
                if (container != null) PlayerCorpses.Add(container);
                CorpseMonitor.Add(player.userID);

                LeaveEvent(player, true);
                PlaySoundToPlayers(Participants, config.soundsSettings.sound_for_death, new Vector3(CurrentCentrePoint.x, CurrentCentrePoint.y - 10, CurrentCentrePoint.z));
                foreach (var p in Participants)
                {
                    if (info?.InitiatorPlayer != null) PrintToChat(p, string.Format(lang.GetMessage($"KillMessage{UnityEngine.Random.Range(1, 6)}", this, p.UserIDString), player.displayName, info.InitiatorPlayer.displayName));
                    else PrintToChat(p, string.Format(lang.GetMessage($"DeathMessage{UnityEngine.Random.Range(1, 4)}", this, p.UserIDString), player.displayName));
                }
            }
            else DestroyOutsiderMonitor(player);

        }

        DroppedItemContainer MovePlayerLootToDropContainer(BasePlayer player)
        {
            if (player.inventory == null) return null;
            var itemList = player.inventory.AllItems();
            if (itemList.Length == 0) return null;
            DroppedItemContainer container = GameManager.server.CreateEntity("assets/prefabs/misc/item drop/item_drop_backpack.prefab", player.transform.position, Quaternion.identity) as DroppedItemContainer;

            container.lootPanelName = "generic_resizable";
            container.playerName = $"{player.displayName}'s Backpack";
            container.playerSteamID = player.userID;

            container.inventory = new ItemContainer();
            container.inventory.ServerInitialize(null, itemList.Length);
            container.inventory.GiveUID();
            container.inventory.entityOwner = container;
            container.inventory.SetFlag(ItemContainer.Flag.NoItemInput, true);

            foreach (Item item in itemList)
            {
                if (!item.MoveToContainer(container.inventory))
                {
                    item.RemoveFromContainer();
                    item.Remove();
                }
            }

            container.Spawn();

            return container;
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (Participants.Contains(player))
            {
                player.Die();
            }
        }


        void OnPlayerRespawned(BasePlayer player)
        {
            if (config.gameSettings.use_nightvision && permission.UserHasPermission(player.UserIDString, "nightvision.allowed")) permission.RevokeUserPermission(player.UserIDString, "nightvision.allowed");
        }

        object OnPlayerCorpseSpawn(BasePlayer player)
        {
            if (CorpseMonitor.Contains(player.userID))
            {
                CorpseMonitor.Remove(player.userID);
                if (CorpseMonitor.Count == 0 && !IsRunning) Unsubscribe(nameof(OnPlayerCorpseSpawn)); 
                return true;
            }
            return null;
        }       

        // Checks when a corpse is looted, if it is outside of the arena.
        object CanLootEntity(BasePlayer player, LootableCorpse corpse)
        {
            if (CorpseMonitor.Contains(corpse.playerSteamID) && IsOutsideOfArena(corpse.transform.position))
            {
                NextTick(() =>
                {
                    corpse.KillMessage();
                });
                return false;
            }
            return null;
        }

        object CanLootPlayer(BasePlayer target, BasePlayer looter)
        {
            if (Participants.Contains(target) && !Participants.Contains(looter)) return false;
            return null;
        }

        static bool IsOutsideOfArena(Vector3 pos)
        {
            return pos.y < CurrentCentrePoint.y - 50;
        }

        object OnEntityTakeDamage(BasePlayer victim, HitInfo info)
        {
            if (victim == null || info == null || info.InitiatorPlayer == null || victim == info.InitiatorPlayer) return null;

            var damageType = info.damageTypes?.GetMajorityDamageType();
            if (damageType == Rust.DamageType.Radiation || damageType == Rust.DamageType.Fall) return null;

            if (Participants.Contains(victim))
            {
                // PRevents damage from external players or while lobby is active.
                if (!Participants.Contains(info.InitiatorPlayer) || CanJoin)
                {
                    info.damageTypes?.ScaleAll(0f);
                    return true;
                }
            }
            return null;
        }

        #endregion

        #region API

        void OnPlayerTeleported(BasePlayer player, Vector3 oldPos, Vector3 newPos)
        {
            if (!IsRunning) return;
            if (Participants.Contains(player)) return;
            if (Vector3.Distance(CurrentCentrePoint, newPos) < FurthestEntity + DistanceThreshold && player.transform.position.y > CurrentCentrePoint.y - 50)
            {
                PrintToChat(player, "You cannot teleport to an active event.");
                Player.Teleport(player, oldPos);
            }
        }

        object EMGetAnnounceJoinPrefix(BasePlayer player, string eventName)
        {
            if (string.IsNullOrEmpty(eventName) || !eventName.Equals(this.Name, StringComparison.OrdinalIgnoreCase)) return null;
            return lang.GetMessage("Prefix", this);
        }

        void SubscribeThirdpartyHooks(bool enable)
        {
            if (enable)
            {
                Subscribe(nameof(OnPopulateBetterLoot));
                Subscribe(nameof(OnAddRecipeCardToLootContainer));
                Subscribe(nameof(OnContainerPopulate));
                Subscribe(nameof(CanPopulateLoot));
                Subscribe(nameof(OnCustomLootContainer));
                Subscribe(nameof(STCanReceiveYield));
                Subscribe(nameof(STCanReceiveBonusLootFromContainer));
                Subscribe(nameof(CanReceiveEpicLootFromCrate));
                Subscribe(nameof(OnIngredientAddedToContainer));
                Subscribe(nameof(OnRecipeAddedToContainer));
                return;
            }
            Unsubscribe(nameof(OnPopulateBetterLoot));
            Unsubscribe(nameof(OnAddRecipeCardToLootContainer));
            Unsubscribe(nameof(OnContainerPopulate));
            Unsubscribe(nameof(CanPopulateLoot));
            Unsubscribe(nameof(OnCustomLootContainer));
            Unsubscribe(nameof(STCanReceiveYield));
            Unsubscribe(nameof(STCanReceiveBonusLootFromContainer));
            Unsubscribe(nameof(CanReceiveEpicLootFromCrate));
            Unsubscribe(nameof(OnIngredientAddedToContainer));
            Unsubscribe(nameof(OnRecipeAddedToContainer));
        }

        object OnPopulateBetterLoot(LootContainer container)
        {
            if (containerSpawns.Contains(container)) return true;
            return null;
        }

        object OnAddRecipeCardToLootContainer(BasePlayer player, LootContainer container)
        {
            if (containerSpawns.Contains(container))
            {
                return true;
            }
            return null;
        }

        object OnContainerPopulate(LootContainer container)
        {
            if (containerSpawns.Contains(container)) return true;
            return null;
        }

        object CanPopulateLoot(LootContainer container)
        {
            if (containerSpawns.Contains(container)) return false;
            return null;
        }

        object OnCustomLootContainer(NetworkableId containerID)
        {
            if (containerSpawnIDs.Contains(containerID.Value)) return true;
            return null;
        }

        object STCanReceiveYield(BasePlayer player, BaseEntity entity = null)
        {
            if (Participants.Contains(player)) return false;
            return null;
        }

        object STCanReceiveBonusLootFromContainer(BasePlayer player, LootContainer container)
        {
            if (containerSpawns.Contains(container)) return false;
            return null;
        }

        object CanReceiveEpicLootFromCrate(BasePlayer player, StorageContainer container)
        {
            if (containerSpawns.Contains(container))
            {
                return false;
            }
            return null;
        }

        object OnIngredientAddedToContainer(LootContainer container)
        {
            if (containerSpawns.Contains(container))
            {
                return true;
            }
            return null;
        }

        object OnRecipeAddedToContainer(LootContainer container)
        {
            if (containerSpawns.Contains(container))
            {
                return true;
            }
            return null;
        }

        // Handles truePVE
        object CanEntityTakeDamage(BasePlayer victim, HitInfo info)
        {
            if (victim != null && Participants.Contains(victim)) return true;
            return null;
        }

        bool IsSurivalArenaContainer(LootContainer container)
        {
            return containerSpawns.Contains(container);
        }

        void EMStartNextEvent(string eventName)
        {
            if (eventName == this.Name)
            {
                StartEvent(GetRandomArena());
            }
        }

        void EMEndGame(string eventName)
        {
            if (eventName == this.Name)
            {
                EndEvent();
            }
        }

        #endregion

        #region Monobehaviour 

        static int RadAccumulation = 3;
        static int CheckInterval = 1;

        void DestroyMonitor(BasePlayer player)
        {
            var gameObject = player.GetComponent<Monitor>();
            if (gameObject != null) GameObject.DestroyImmediate(gameObject);
        }

        void AddMonitor(BasePlayer player)
        {
            DestroyMonitor(player);
            player.gameObject.AddComponent<Monitor>();
        }

        public class Monitor : MonoBehaviour
        {
            private BasePlayer player;
            private float checkDelay;

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                checkDelay = Time.time + CheckInterval;
            }

            public void FixedUpdate()
            {
                if (player == null) return;
                if (checkDelay < Time.time)
                {
                    DoCheck();
                    checkDelay = Time.time + CheckInterval;
                }
            }

            private bool SentUI = false;

            public void DoCheck()
            {
                if (IsOutsideOfArena(player.transform.position))
                {
                    player.inventory.Strip();
                    player.Die();
                    return;
                }
                if (Vector3.Distance(player.transform.position, CurrentCentrePoint) > CircleDist)
                {
                    player.metabolism.radiation_level.SetValue(player.metabolism.radiation_level.value + RadAccumulation);
                    player.metabolism.radiation_poison.SetValue(player.metabolism.radiation_poison.value + RadAccumulation);
                    player.metabolism.SendChangesToClient();
                    if (!SentUI)
                    {
                        SentUI = true;
                        BleedingPanel(player);
                    }                    
                }
                else if (SentUI)
                {
                    SentUI = false;
                    CuiHelper.DestroyUi(player, "BleedingPanel");
                }
            }

            private void OnDestroy()
            {
                enabled = false;
                CancelInvoke();
                CuiHelper.DestroyUi(player, "BleedingPanel");
            }
        }

        object OnPlayerWound(BasePlayer player, HitInfo info)
        {
            if (Participants.Contains(player))
            {
                if (IsOutsideOfArena(player.transform.position)) return false;
            }
            return null;
        }


        #endregion

        #region Outsider behaviour

        static void DestroyOutsiderMonitor(BasePlayer player)
        {
            var gameObject = player.GetComponent<OutsiderBehaviour>();
            if (gameObject != null) GameObject.DestroyImmediate(gameObject);
        }

        void AddOutsiderMonitor(BasePlayer player)
        {
            DestroyOutsiderMonitor(player);
            player.gameObject.AddComponent<OutsiderBehaviour>();
        }

        bool HasOutsiderMonitor(BasePlayer player)
        {
            var gameObject = player.GetComponent<OutsiderBehaviour>();
            return gameObject != null;
        }

        static float SecondsToVacate;
        static float DistanceThreshold;

        public class OutsiderBehaviour : MonoBehaviour
        {
            private BasePlayer player;
            private float checkDelay;
            private float startedCheck;

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                checkDelay = Time.time + CheckInterval;
                player.ChatMessage("You have entered a restricted area. Vacate the area or you will be destroyed.");
                startedCheck = Time.time;
            }

            public void FixedUpdate()
            {
                if (player == null)
                {
                    DestroyOutsiderMonitor(player);
                    return;
                }
                if (checkDelay < Time.time)
                {
                    DoCheck();
                    checkDelay = Time.time + CheckInterval;
                }
            }

            public void DoCheck()
            {
                if (Vector3.Distance(CurrentCentrePoint, player.transform.position) < FurthestEntity + DistanceThreshold && player.transform.position.y > CurrentCentrePoint.y - 50)
                {
                    if (Time.time - startedCheck > SecondsToVacate)
                        DestroyPlayer();
                    else player.ChatMessage($"You have {Math.Round(startedCheck + SecondsToVacate - Time.time, 2)} seconds left to vacate the area");
                }
                else DestroyOutsiderMonitor(player);
            }

            private void DestroyPlayer()
            {
                if (player != null)
                {
                    player.Die();
                    player.ChatMessage("You were slain for getting too close to the Survival Arena event!");
                }
                DestroyOutsiderMonitor(player);
            }

            private void OnDestroy()
            {
                enabled = false;
                CancelInvoke();
            }
        }

        #endregion

        #region Adding/removing entities

        private const int LAYER_TARGET = ~(1 << 2 | 1 << 3 | 1 << 4 | 1 << 10 | 1 << 18 | 1 << 28 | 1 << 29);
        private BaseEntity GetTargetEntity(BasePlayer player)
        {
            RaycastHit raycastHit;
            bool flag = Physics.Raycast(player.eyes.HeadRay(), out raycastHit, 5, LAYER_TARGET);
            var targetEntity = flag ? raycastHit.GetEntity() : null;
            return targetEntity;
        }

        [ChatCommand("saremove")]
        void RemoveEntity(BasePlayer player)
        {
            if (!IsAdmin(player))
            {
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("NoPerms", this, player.UserIDString));
                return;
            }

            if (!IsRunning && !CanJoin)
            {
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("AddEntityEventRunning", this, player.UserIDString));
                return;
            }
            if (EventElevationMod > 0f)
            {
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("AddEntityElevation", this, player.UserIDString));
                return;
            }

            var target = GetTargetEntity(player);
            if (target == null || target.PrefabName.StartsWith("assets/bundled/prefabs/modding/admin/") || target.ShortPrefabName == "gates.external.high.wood")
            {
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("NullInvalidEntity", this, player.UserIDString));
                return;
            }                       

            foreach (var entry in CurrentArena.entities)
            {
                if (entry.pos == target.transform.position)
                {
                    PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + string.Format(lang.GetMessage("EntityRemoved", this, player.UserIDString), entry.prefab, entry.pos, entry.rot));
                    CurrentArena.entities.Remove(entry);
                    target.KillMessage();
                    return;
                }
            }
            PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + string.Format(lang.GetMessage("EntityNotFoundInData", this, player.UserIDString), target.PrefabName, target.transform.position, target.transform.rotation.eulerAngles));
        }

        [ChatCommand("addtree")]
        void AddTree(BasePlayer player)
        {
            if (!IsAdmin(player))
            {
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("NoPerms", this, player.UserIDString));
                return;
            }

            AddSpawn(player, "tree");
        }

        [ChatCommand("addbush")]
        void AddBush(BasePlayer player)
        {
            if (!IsAdmin(player))
            {
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("NoPerms", this, player.UserIDString));
                return;
            }

            AddSpawn(player, "bush");
        }

        [ChatCommand("addlog")]
        void AddLog(BasePlayer player)
        {
            if (!IsAdmin(player))
            {
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("NoPerms", this, player.UserIDString));
                return;
            }

            AddSpawn(player, "log");
        }

        [ChatCommand("addloot")]
        void AddLoot(BasePlayer player)
        {
            if (!IsAdmin(player))
            {
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("NoPerms", this, player.UserIDString));
                return;
            }

            AddSpawn(player, "loot");
        }

        void AddSpawn(BasePlayer player, string type)
        {
            if (!IsRunning && !CanJoin && !DevMode)
            {
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("AddEntityEventRunning", this, player.UserIDString)); 
                return;
            }
            if (EventElevationMod > 0f)
            {
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("AddEntityElevation", this, player.UserIDString));
                return;
            }
            Vector3 rot = new Vector3(0, UnityEngine.Random.Range(-90f, 90f), 0);
            BaseEntity entity;

            Vector3 pos = new Vector3(player.transform.position.x, player.transform.position.y - 0.1f, player.transform.position.z);

            switch (type)
            {
                case "tree":
                    CurrentArena.entities.Add(new EntityInfo("assets/prefabs/misc/xmas/pookie/pookie_deployed.prefab", pos, Vector3.zero));
                    PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + string.Format(lang.GetMessage("EntityAddedTree", this, player.UserIDString), pos));
                    entity = GameManager.server.CreateEntity(GetRandomTree(player.transform.position), pos, Quaternion.Euler(rot));
                    entity.Spawn();
                    spawned_entities.Add(entity);
                    SaveData(SaveType.Arena);
                    return;

                case "bush":
                    CurrentArena.entities.Add(new EntityInfo("assets/prefabs/deployable/playerioents/gates/branch/electrical.branch.deployed.prefab", pos, Vector3.zero));
                    PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + string.Format(lang.GetMessage("EntityAddedBush", this, player.UserIDString), pos));
                    entity = GameManager.server.CreateEntity(GetRandomBush(player.transform.position), pos, Quaternion.Euler(rot));
                    entity.Spawn();
                    spawned_entities.Add(entity);
                    SaveData(SaveType.Arena);
                    return;

                case "loot":
                    CurrentArena.entities.Add(new EntityInfo("assets/bundled/prefabs/radtown/crate_normal_2.prefab", pos, Vector3.zero));
                    PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + string.Format(lang.GetMessage("EntityAddedLoot", this, player.UserIDString), pos));
                    entity = GameManager.server.CreateEntity("assets/bundled/prefabs/radtown/crate_normal_2.prefab", pos, Quaternion.Euler(rot));
                    containerSpawns.Add((LootContainer)entity);
                    containerSpawnIDs.Add(entity.net.ID.Value);
                    entity.Spawn();
                    spawned_entities.Add(entity);
                    SaveData(SaveType.Arena);
                    return;

                case "log":
                    CurrentArena.entities.Add(new EntityInfo("assets/prefabs/deployable/playerioents/lights/flasherlight/electric.flasherlight.deployed.prefab", pos, Vector3.zero));
                    PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + string.Format(lang.GetMessage("EntityAddedLog", this, player.UserIDString), pos));
                    entity = GameManager.server.CreateEntity(GetRandomLog(player.transform.position), pos, Quaternion.Euler(rot));
                    entity.Spawn();
                    spawned_entities.Add(entity);
                    SaveData(SaveType.Arena);
                    return;

                default:
                    PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("EntityAddInvalidType", this, player.UserIDString));
                    return;
            }
        }

        #endregion

        #region Radiation Sphere

        List<BaseEntity> Spheres = new List<BaseEntity>();

        private void CreateSphere(Vector3 position, float radius, int darkness, float speed)
        {
            for (int i = 0; i < darkness; i++)
            {
                SphereEntity sphere = (SphereEntity)GameManager.server.CreateEntity("assets/prefabs/visualization/sphere.prefab", position, new Quaternion(), true);
                sphere.currentRadius = radius * 2;
                sphere.lerpSpeed = speed * 2;
                sphere.Spawn();
                Spheres.Add(sphere);
            }
        }

        private void destroyspheres()
        {
            foreach (var sphere in Spheres)
            {
                if (sphere != null)
                    sphere.KillMessage();
            }
            Spheres.Clear();
        }

        #endregion

        #region CUI

        #region HUDs

        static void BleedingPanel(BasePlayer player)
        {
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = Instance.config.radiationZoneSettings.panel_colour },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-0.01 0.01", OffsetMax = "0.1 0.1" }
            }, Instance.config.gameSettings.allow_looting_in_rad_zone ? "Hud" : "Overlay", "BleedingPanel");

            container.Add(new CuiElement
            {
                Name = "BleedingOutText",
                Parent = "BleedingPanel",
                FadeOut = 1,
                Components = {
                    new CuiTextComponent { Text = Instance.lang.GetMessage("InRadZone", Instance), Font = "robotocondensed-bold.ttf", FontSize = 50, Align = TextAnchor.MiddleCenter, Color = "1 0.7532371 0 1", FadeIn = 1 },
                    new CuiOutlineComponent { Color = "0 0 0 1", Distance = "1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-192.131 21.616", OffsetMax = "192.131 171.984" }
                }
            });

            CuiHelper.DestroyUi(player, "BleedingPanel");
            CuiHelper.AddUi(player, container);
        }

        private void PlayerCounter(BasePlayer player, int remaining)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiElement
            {
                Name = "PlayerCounter",
                Parent = "Hud",
                Components = {
                    new CuiTextComponent { Text = string.Format(lang.GetMessage("UIPlayersRemaining", this, player.UserIDString), remaining), Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = $"{-159.3 + config.anchors.player_count_anchor_adjustment.Key} {68.881 + config.anchors.player_count_anchor_adjustment.Value}", OffsetMax = $"{68.924 + config.anchors.player_count_anchor_adjustment.Key} {92.519 + config.anchors.player_count_anchor_adjustment.Value}" }
                }
            });

            CuiHelper.DestroyUi(player, "PlayerCounter");
            CuiHelper.AddUi(player, container);
        }

        void SendCounterUpdate()
        {
            int count = Participants.Count;
            foreach (var player in Participants)
            {
                PlayerCounter(player, count);
            }
        }

        private void CircleStatusHUD(BasePlayer player, int activity)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiElement
            {
                Name = "CircleStatusHUD",
                Parent = "Hud",
                Components = {
                    new CuiTextComponent { Text = string.Format(lang.GetMessage("CircleStatusUpdate", this, player.UserIDString), activity == 0 ? lang.GetMessage("CircleStatusINACTIVE", this, player.UserIDString) : activity == 1 ? lang.GetMessage("CircleStatusMOVING", this, player.UserIDString) : lang.GetMessage("CircleStatusSTOPPED", this, player.UserIDString)), Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.UpperCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-100.003 -63.9", OffsetMax = "99.997 -33.9" }
                }
            });

            CuiHelper.DestroyUi(player, "CircleStatusHUD");
            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Button UI

        private void EventLeaveButton(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.4036579 0.4223583 0.4433962 0.8509804" },
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-319.8 -34", OffsetMax = "-243.8 -2" }
            }, "Overlay", "EventLeaveButton");

            container.Add(new CuiElement
            {
                Name = "Label_8022",
                Parent = "EventLeaveButton",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UILeaveButton", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-38 -16", OffsetMax = "38 16" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = "survivalarenaleaveevent" },
                Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-38 -16", OffsetMax = "38 16" }
            }, "EventLeaveButton", "Button_63");

            CuiHelper.DestroyUi(player, "EventLeaveButton");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("survivalarenaleaveevent")]
        void SurvivalArenaLeaveEventButtonPressed(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            CuiHelper.DestroyUi(player, "EventLeaveButton");
            LeaveEventCMD(player);
        }

        #endregion

        #region Game start UI

        private void MenuBackground(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0.9803922" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 -3.265", OffsetMax = "0 -0.335" }
            }, "Overlay", "MenuBackground");

            CuiHelper.DestroyUi(player, "MenuBackground");
            CuiHelper.AddUi(player, container);
        }

        private Dictionary<string, string> DelayFieldInputText = new Dictionary<string, string>();
        private Dictionary<string, string> HeightFieldInputText = new Dictionary<string, string>();

        private void GameStartUI(BasePlayer player, string startDelay = "0", string heightMod = "0", string profile = "null", string arena = "null")
        {
            if (startDelay == "0") startDelay = config.gameSettings.defaultStartTime.ToString();
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 -3.265", OffsetMax = "0 -0.335" }
            }, "Overlay", "GameStartUI");

            container.Add(new CuiElement
            {
                Name = "LootProfileHeader",
                Parent = "GameStartUI",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UILootProfile", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.UpperCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-100 145.7", OffsetMax = "100 175.7" }
                }
            });            

            var count = 0;
            int finalCount;
            foreach (var prof in config.lootSettings.Keys)
            {
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1226415 0.1226415 0.1226415 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-100 {-63.4 - (count * 40)}", OffsetMax = $"100 {-33.4 - (count * 40)}" }
                }, "LootProfileHeader", "Profile");

                container.Add(new CuiElement
                {
                    Name = "text",
                    Parent = "Profile",
                    Components = {
                    new CuiTextComponent { Text = prof == profile ? string.Format(lang.GetMessage("UIProfSelected", this, player.UserIDString), prof.TitleCase()) : prof.TitleCase(), Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-100 -15", OffsetMax = "100 15" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "1 1 1 0", Command = $"survivalarenaselectprofile {prof} {arena}" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-100 -15", OffsetMax = "100 15" }
                }, "Profile", "button");

                count++;
            }
            finalCount = count;
            container.Add(new CuiElement
            {
                Name = "HeightModHeader",
                Parent = "GameStartUI",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIHeightMod", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.UpperCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-220 145.7", OffsetMax = "-120 175.7" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1226415 0.1226415 0.1226415 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -63.4", OffsetMax = "50 -33.4" }
            }, "HeightModHeader", "HeightMod");

            container.Add(new CuiElement
            {
                Name = "inputHeightText",
                Parent = "HeightMod",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Text = heightMod ?? string.Empty,
                        CharsLimit = 40,
                        Color = "1 1 1 1",
                        IsPassword = false,
                        Command = $"{"heightmod.heightyinputtextcb"} {startDelay} {profile} {arena}",
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 16,
                        NeedsKeyboard = true,
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-50 -15",
                        OffsetMax = "50 15"
                    }
                }
            });

            container.Add(new CuiElement
            {
                Name = "LobbyTimeHeader",
                Parent = "GameStartUI",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UILobbyTime", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.UpperCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "120 145.7", OffsetMax = "220 175.7" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1226415 0.1226415 0.1226415 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -63.4", OffsetMax = "50 -33.4" }
            }, "LobbyTimeHeader", "LobbyTime");

            container.Add(new CuiElement
            {
                Name = "inputText",
                Parent = "LobbyTime",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Text = startDelay ?? string.Empty,
                        CharsLimit = 40,
                        Color = "1 1 1 1",
                        IsPassword = false,
                        Command = $"{"delaytime.delayinputtextcb"} {heightMod} {profile} {arena}",
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 16,
                        NeedsKeyboard = true,
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-50 -15",
                        OffsetMax = "50 15"
                    }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1215686 0.1215686 0.1215686 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"20 {45 - (finalCount * 40)}", OffsetMax = $"100 {75 - (finalCount * 40)}" }
            }, "GameStartUI", "StopStartButton");

            container.Add(new CuiElement
            {
                Name = "text",
                Parent = "StopStartButton",
                Components = {
                    new CuiTextComponent { Text = IsRunning || CanJoin ? lang.GetMessage("UISTOP", this, player.UserIDString) : lang.GetMessage("UISTART", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-40 -15", OffsetMax = "40 15" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = $"stopstartsurvivalarena {heightMod} {startDelay} {arena} {profile}" },
                Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-40 -15", OffsetMax = "40 15" }
            }, "StopStartButton", "button");

            //
            count = 0;
            container.Add(new CuiElement
            {
                Name = "ArenaProfileHeader",
                Parent = "GameStartUI",
                Components = {
                    new CuiTextComponent { Text = "Arena", Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.UpperCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-440 145.7", OffsetMax = "-240 175.7" }
                }
            });
            foreach (var _arena in Arenas.Keys)
            {               
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1226415 0.1226415 0.1226415 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-100 {-63.4 - (count * 40)}", OffsetMax = $"100 {-33.4 - (count * 40)}" }
                }, "ArenaProfileHeader", "Profile");

                container.Add(new CuiElement
                {
                    Name = "text",
                    Parent = "Profile",
                    Components = {
                    new CuiTextComponent { Text = _arena == arena ? string.Format(lang.GetMessage("UIProfSelected", this, player.UserIDString), _arena.TitleCase()) : _arena.TitleCase(), Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-100 -15", OffsetMax = "100 15" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "1 1 1 0", Command = $"survivalarenaselectarena {profile} {_arena}" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-100 -15", OffsetMax = "100 15" }
                }, "Profile", "button");
                count++;
            }          

            //

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1215686 0.1215686 0.1215686 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-100 {45 - (finalCount * 40)}", OffsetMax = $"-20 {75 - (finalCount * 40)}" }
            }, "GameStartUI", "CloseButton");

            container.Add(new CuiElement
            {
                Name = "text",
                Parent = "CloseButton",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UICLOSE", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-40 -15", OffsetMax = "40 15" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = "survivalarenaclosestartmenu" },
                Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-40 -15", OffsetMax = "40 15" }
            }, "CloseButton", "button");

            CuiHelper.DestroyUi(player, "GameStartUI");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("survivalarenaclosestartmenu")]
        void CloseStartUI(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            CuiHelper.DestroyUi(player, "GameStartUI");
            CuiHelper.DestroyUi(player, "MenuBackground");
        }

        [ChatCommand("survivalarena")]
        void StartArenaUI(BasePlayer player)
        {
            if (!IsAdmin(player))
            {
                PrintToChat(player, lang.GetMessage("NoPerms", this, player.UserIDString));
                return;
            }
            MenuBackground(player);
            GameStartUI(player);            
        }

        [ConsoleCommand("delaytime.delayinputtextcb")]
        void DelayInputText(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            string heightMod = arg.Args.Length > 0 ? arg.Args[0] : "0";
            string profile = arg.Args.Length > 1 ? arg.Args[1] : "null";
            string arena = arg.Args.Length > 2 ? arg.Args[2] : "null";

            if (arg.Args.Length <= 0)
            {
                if (DelayFieldInputText.ContainsKey(player.UserIDString))
                    DelayFieldInputText.Remove(player.UserIDString);

                return;
            }

            if (DelayFieldInputText.ContainsKey(player.UserIDString))
            {
                DelayFieldInputText[player.UserIDString] = arg.Args[3];
            }
            else
            {
                DelayFieldInputText.Add(player.UserIDString, arg.Args[3]);
            }

            string timeDelay = arg.Args.Length > 3 ? arg.Args[3] : config.gameSettings.defaultStartTime.ToString();

            GameStartUI(player, timeDelay, heightMod, profile, arena);
        }

        [ConsoleCommand("heightmod.heightyinputtextcb")]
        void HeightInputText(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            string timeDelay = arg.Args.Length > 0 ? arg.Args[0] : config.gameSettings.defaultStartTime.ToString();
            string profile = arg.Args.Length > 1 ? arg.Args[1] : "null";
            string arena = arg.Args.Length > 2 ? arg.Args[2] : "null";
            if (arg.Args.Length <= 0)
            {
                if (HeightFieldInputText.ContainsKey(player.UserIDString))
                    HeightFieldInputText.Remove(player.UserIDString);

                return;
            }
            if (HeightFieldInputText.ContainsKey(player.UserIDString))
            {
                HeightFieldInputText[player.UserIDString] = arg.Args[3];
            }
            else
            {
                HeightFieldInputText.Add(player.UserIDString, arg.Args[3]);

            }
            string heightMod = arg.Args.Length > 3 ? arg.Args[3] : "0";
            GameStartUI(player, timeDelay, heightMod, profile, arena);
        }

        [ConsoleCommand("stopstartsurvivalarena")]
        void StartButtonPressed(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            CuiHelper.DestroyUi(player, "GameStartUI");
            CuiHelper.DestroyUi(player, "MenuBackground");

            var height = Convert.ToSingle(arg.Args[0]);
            var time = Convert.ToInt32(arg.Args[1]);
            var arenaName = arg.Args.Length > 2 ? Arenas.ContainsKey(arg.Args[2]) ? arg.Args[2] : GetRandomArena() : GetRandomArena();
            var profile = arg.Args.Length > 3 ? arg.Args[3] : null;            

            if (IsRunning || CanJoin)
            {
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("UIEndedEvent", this, player.UserIDString));
                EndEvent();
                return;
            }

            if (IsDespawning)
            {
                arg.ReplyWith(lang.GetMessage("Prefix", this, player?.UserIDString) + lang.GetMessage("EventStillDespawning", this, player?.UserIDString));
                return;
            }

            if (string.IsNullOrEmpty(profile) || profile == "null")
            {
                profile = GetRandomProfile();
            }

            LobbyTime = time;

            PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + string.Format(lang.GetMessage("UIStartingEvent", this, player.UserIDString), time, height, profile));

            StartEvent(arenaName, height, profile);
        }

        string GetRandomProfile()
        {
            var totalweight = 0;
            foreach (var kvp in config.lootSettings) 
                totalweight += kvp.Value.profileWeight;

            var roll = UnityEngine.Random.Range(0, totalweight + 1);
            var check = 0;
            foreach (var kvp in config.lootSettings)
            {
                check += kvp.Value.profileWeight;
                if (check <= roll) return kvp.Key;
            }

            string result;
            List<string> randProfile = Pool.GetList<string>();
            randProfile.AddRange(config.lootSettings.Keys);
            result = randProfile.GetRandom();
            Pool.FreeList(ref randProfile);

            return result;
        }

        [ConsoleCommand("survivalarenaselectprofile")]
        void SelectProfile(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            GameStartUI(player, DelayFieldInputText.ContainsKey(player.UserIDString) ? DelayFieldInputText[player.UserIDString] : config.gameSettings.defaultStartTime.ToString(), HeightFieldInputText.ContainsKey(player.UserIDString) ? HeightFieldInputText[player.UserIDString] : "0", arg.Args[0], arg.Args[1]);
        }

        [ConsoleCommand("survivalarenaselectarena")]
        void SelectArena(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            GameStartUI(player, DelayFieldInputText.ContainsKey(player.UserIDString) ? DelayFieldInputText[player.UserIDString] : config.gameSettings.defaultStartTime.ToString(), HeightFieldInputText.ContainsKey(player.UserIDString) ? HeightFieldInputText[player.UserIDString] : "0", arg.Args[0], arg.Args[1]);
        }

        #endregion

        #region Start timer UI

        private void STARTCountdownUI(BasePlayer player, int seconds)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiElement
            {
                Name = "STARTCountdownUI",
                Parent = "Overlay",
                Components = {
                    new CuiTextComponent { Text = string.Format(lang.GetMessage("_UILobbyTime", this, player.UserIDString), seconds, config.command_settings.leave_command), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-100.003 -163.9", OffsetMax = "99.997 -103.9" }
                }
            });

            CuiHelper.DestroyUi(player, "STARTCountdownUI");
            CuiHelper.AddUi(player, container);
        }

        #endregion

        #endregion

        #region Rewards

        [ChatCommand("sprize")]
        void ClaimPrize(BasePlayer player)
        {
            if (Participants.Contains(player)) return;
            if (config.prize_settings.rolls_per_claim < 0)
            {
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("PrizesDisabled", this, player.UserIDString));
                return;
            }
            PCDInfo pi;
            if (!pcdData.pEntity.TryGetValue(player.userID, out pi) || pi.rewards_remaining <= 0)
            {
                PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + lang.GetMessage("NoPrize", this, player.UserIDString));
                return;
            }

            for (int i = 0; i < config.prize_settings.rolls_per_claim; i++)
            {
                RollReward(player);
            }
            pi.rewards_remaining--;
            PlaySound(player, config.soundsSettings.sound_for_prize);
        }

        void RollReward(BasePlayer player)
        {
            var roll = UnityEngine.Random.Range(0, config.prize_settings.prizes.Sum(x => x.dropWeight) + 1);
            var count = 0;
            foreach (var prize in config.prize_settings.prizes)
            {
                count += prize.dropWeight;
                if (roll <= count)
                {
                    var item = ItemManager.CreateByName(prize.shortname, UnityEngine.Random.Range(Math.Max(prize.min_quantity, 1), Math.Max(prize.max_quantity, 1) + 1), prize.skin);
                    if (item == null) return;
                    if (!string.IsNullOrEmpty(prize.displayName)) item.name = prize.displayName;
                    
                    PrintToChat(player, lang.GetMessage("Prefix", this, player.UserIDString) + string.Format(lang.GetMessage("PrizeGiven", this, player.UserIDString), item.amount, item.name ?? item.info.displayName.english));
                    player.GiveItem(item);
                    break;
                    //found prize.
                }
            }
        }

        #endregion

        #region Handle web request
        // Credit Fetch by Wulf for a lot of the code
        bool RequestMade = false;
        void AddArenaFile()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile(subDirectory + "Arena")) return;
            Puts("Missing the Arena.json file in data. Attempting to obtain it.");
            Uri uriResult;
            string url = "https://www.dropbox.com/s/iylthpdq1witbl8/Arena.json?dl=1";
            bool uriTest = Uri.TryCreate(url, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);            
            if (uriTest)
            {
                try
                {
                    webrequest.Enqueue(url, null, (code, response) =>
                      GetCallback(code, response, url, "Arena"), this);
                    RequestMade = true;
                }
                catch
                {
                    Puts("Invalid request. Failed to fetch Arena file [1].");
                    Interface.Oxide.UnloadPlugin(Name);
                    return;
                }
            }
            else
            {
                Puts("Invalid request. Failed to fetch Arena file [2].");
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }
        }

        private void GetCallback(int code, string response, string uri, string savefilename)
        {
            if (response == null || code != 200)
            {
                Puts($"Error fetching file: {code}");
                return;
            }
            string SaveFilePath = String.Format($"{Interface.Oxide.DataDirectory}\\survivalArena\\{savefilename}");
            try
            {
                var json = JObject.Parse(response);
                Interface.Oxide.DataFileSystem.WriteObject(SaveFilePath, json);
                var arenaData = Interface.Oxide.DataFileSystem.ReadObject<ArenaData>(subDirectory + "Arena");
                if (Arenas.ContainsKey("Arena")) Arenas["Arena"] = arenaData;
                else Arenas.Add("Arena", arenaData);
                HaveArenaFile = true;
                if (HaveArenaFile && arenaData.CenterPoint != Vector3.zero)
                {
                    Puts("Saved Arena file to survivalArena/Arena.json");
                    RequestMade = false;
                }
                else
                {
                    Puts("Failed to acquire CentrePoint");
                    Interface.Oxide.UnloadPlugin(Name);
                    return;
                }
            }
            catch
            {                
                Puts($"Failed to save {savefilename} in {SaveFilePath}. Creating new one.");
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }
        }

        [ConsoleCommand("sadebug")]
        void GetPlayers(ConsoleSystem.Arg arg)
        {
            var user = arg.Player();
            if (user != null && !permission.UserHasPermission(user.UserIDString, perm_admin)) return;
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (Participants.Contains(player)) Puts($"{player.displayName} is still registered as a participant in SurvivalArena.");
                if (Convert.ToBoolean(EventHelper.Call("EMAtEvent", player.userID))) Puts($"{player.displayName} is still registered at an event in EventHelper.");
            }
        }

        #endregion
    }
}
