using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/* Add NPC spawn option for shop access.
 * 
 * 
 */

/* 1.0.14 changes
 * Updated Imagur links to dropbox to prevent cap errors.
 */

namespace Oxide.Plugins
{
    [Info("DeployableNature", "imthenewguy", "1.0.14")]
    [Description("This plugin was fixed by Инкуб to order [Rust Plugin Sliv]: https://discord.gg/pFgKw6Dyyq")]
    class DeployableNature : RustPlugin
    {
        #region Config

        private Configuration config;
        public class Configuration
        {
            [JsonProperty("Maximum number of rocks that a player can deploy [0 = no limit]")]
            public int max_rocks = 0;

            [JsonProperty("Maximum number of trees that a player can deploy [0 = no limit]")]
            public int max_trees = 0;

            [JsonProperty("Maximum number of bushes that a player can deploy [0 = no limit]")]
            public int max_bushes = 0;

            [JsonProperty("Maximum number of animals that a player can deploy [0 = no limit]")]
            public int max_animals = 3;

            [JsonProperty("Mining node drop rate [%] - [0 = off]")]
            public int drop_rate_from_node = 1;

            [JsonProperty("Tree drop rate [%] - [0 = off]")]
            public int drop_rate_from_tree = 1;

            [JsonProperty("Animal drop rate [%] - [0 = off]")]
            public int drop_rate_from_animal = 1;

            [JsonProperty("Animals only drop from their corpse type [bear only drops from harvesting bear corpses]?")]
            public bool animal_specific_drops = true;

            [JsonProperty("Prevent wild animals from targeting and killing deployed animals?")]
            public bool prevent_animal_targeting = true;

            [JsonProperty("Collectable plant drop rate [%] - [0 = off]")]
            public int drop_rate_from_collectables = 5;

            [JsonProperty("Chance for loot to be added to a minecart when it spawns [%] - [0 = off]")]
            public int drop_rate_from_minecart = 10;

            [JsonProperty("Allow players to hold sprint while placing a rock to have it embed into the ground?")]
            public bool shift_place = true;

            [JsonProperty("How much deeper should the rock sink when shift-placing?")]
            public float depth_modifier = 0.5f;

            [JsonProperty("Currency to use [SCRAP, ECONOMICS, SR, CUSTOM]")]
            public string currency = "SCRAP";

            [JsonProperty("Custom currency details")]
            public CustomCurrency custom_currency = new CustomCurrency();

            [JsonProperty("Enable players to buy prfabs from the market?")]
            public bool market_enabled = true;

            [JsonProperty("Nature market command")]
            public string market_cmd = "naturemarket";

            [JsonProperty("How often should the chat message post telling players how to access the deployable nature market? (seconds) 0 = off")]
            public float chat_delay = 600f;

            [JsonProperty("Prevent deployable items from being recycled")]
            public bool prevent_recycling = true;

            [JsonProperty("Display a chat message when a player pulls out a hammer for the first time, reminding them that they can remove deployables")]
            public bool notify_player_with_hammer = false;

            [JsonProperty("Notify how to remove items after first deploy")]
            public bool notify_after_first_Deploy = true;

            [JsonProperty("Allow players to use their middle mouse button to remove an item with a hammer (they can still use chat command regardless if they have perms)")]
            public bool use_input_command = true;

            [JsonProperty("Automatically add new types to the config?")]
            public bool auto_update = false;

            [JsonProperty("Respawn delay when an entity is collected that isnt supposed to be")]
            public float collectible_respawn_delay = 1f;

            [JsonProperty("Kill all deployed nature when the associated cupboard is deployed?")]
            public bool cupboard_kill = false;

            [JsonProperty("Allow team mates to pickup their members items")]
            public bool team_pickup = true;

            [JsonProperty("Allow TC authed players to pickup items")]
            public bool auth_pickup = true;

            [JsonProperty("Allow DeployableNature to control stack sizes? [Set to false if using a stacks plugin]")]
            public bool manage_stacks = true;

            [JsonProperty("Names of HumanNPCs that will open the market")]
            public List<string> market_npcs = new List<string>() { "Nature Market" };

            [JsonProperty("IDs of HumanNPCs that will open the market")]
            public List<ulong> market_npc_ids = new List<ulong>();

            [JsonProperty("Permissions that will receive a discount on the store cost when purchasing [1.0 is full price]. Prefix with deployablenature.")]
            public Dictionary<string, float> discount_permissions = new Dictionary<string, float>();

            [JsonProperty("Prefab information", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<ulong, ItemInfo> Prefabs = new Dictionary<ulong, ItemInfo>();

            [JsonProperty("Tree prefabs", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<TreeType, TreeInfo> trees = new Dictionary<TreeType, TreeInfo>();

            [JsonProperty("Bush prefabs", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<BushType, BushInfo> bushes = new Dictionary<BushType, BushInfo>();

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration()
            {
                Prefabs = DefaultPrefabs,
                trees = DefaultTrees,
                bushes = DefaultBushes,
                custom_currency = new CustomCurrency()
                {
                    name = "cash",
                    skinID = 2661030582,
                    shortname = "blood"
                },
                discount_permissions = new Dictionary<string, float>()
                {
                    ["deployablenature.vip"] = 1.0f
                }
            };
        }

        private Dictionary<ulong, ItemInfo> DefaultPrefabs
        {
            get
            {
                return new Dictionary<ulong, ItemInfo>
                {
                    [2609145017] = new ItemInfo(true, "medium clutter rock", "electric.teslacoil", "assets/bundled/prefabs/modding/admin/admin_rock_clutter_medium_d.prefab", 10, true, true, false, 3, 20, PrefabType.Rock, true, 10, "https://www.dropbox.com/s/lit5qotrz3dzo5s/medium%20clutter%20rock.png?dl=1"),
                    [2668227876] = new ItemInfo(true, "small quarry rock", "electric.teslacoil", "assets/bundled/prefabs/modding/admin/admin_rock_quarry_small_a.prefab", 15, true, true, false, 3, 20, PrefabType.Rock, true, 10, "https://www.dropbox.com/s/hgd4lwzopde6zkv/small%20quarry%20rock.png?dl=1"),
                    [2668228341] = new ItemInfo(true, "small rock formation", "electric.teslacoil", "assets/bundled/prefabs/modding/admin/admin_rock_formation_small_c.prefab", 15, true, true, false, 3, 20, PrefabType.Rock, true, 10, "https://www.dropbox.com/s/uvggj6pu9g0liwm/small%20rock%20formation.png?dl=1"),
                    [2668228500] = new ItemInfo(true, "medium rock formation", "electric.teslacoil", "assets/bundled/prefabs/modding/admin/admin_rock_formation_medium_a.prefab", 15, true, true, false, 3, 20, PrefabType.Rock, true, 10, "https://www.dropbox.com/s/6dr34rizrmc1cyb/medium%20rock%20formation.png?dl=1"),
                    [2668228817] = new ItemInfo(true, "arid medium cliff", "electric.teslacoil", "assets/bundled/prefabs/modding/admin/admin_cliff_medium_arc_arid_small.prefab", 15, true, true, false, 3, 20, PrefabType.Rock, true, 10, "https://www.dropbox.com/s/b7pvwgzrlej5p3x/arid%20medium%20cliff.png?dl=1"),
                    [2668228981] = new ItemInfo(true, "large cliff", "electric.teslacoil", "assets/bundled/prefabs/modding/admin/admin_cliff_low_arc.prefab", 15, true, true, false, 3, 20, PrefabType.Rock, true, 10, "https://www.dropbox.com/s/ocbf43brdm9shio/large%20cliff.png?dl=1"),

                    [2806644689] = new ItemInfo(true, "polar bear", "telephone", "assets/rust.ai/agents/bear/polarbear.prefab", 15, true, true, false, 1, 20, PrefabType.Animal, true, 10, "https://www.dropbox.com/s/4btmzlc8nr2shpo/polar%20bear.png?dl=1"),
                    [2806645092] = new ItemInfo(true, "wolf", "telephone", "assets/rust.ai/agents/wolf/wolf.prefab", 15, true, true, false, 1, 20, PrefabType.Animal, true, 10, "https://www.dropbox.com/s/h7uagud141osqlc/wolf.png?dl=1"),
                    [2806645210] = new ItemInfo(true, "stag", "telephone", "assets/rust.ai/agents/stag/stag.prefab", 15, true, true, false, 1, 20, PrefabType.Animal, true, 10, "https://www.dropbox.com/s/2kgljzh1b0plx67/stag.png?dl=1"),
                    [2806645341] = new ItemInfo(true, "chicken", "telephone", "assets/rust.ai/agents/chicken/chicken.prefab", 15, true, true, false, 1, 20, PrefabType.Animal, true, 10, "https://www.dropbox.com/s/kwxi8x7knk83g27/chicken.png?dl=1"),
                    [2806645547] = new ItemInfo(true, "bear", "telephone", "assets/rust.ai/agents/bear/bear.prefab", 15, true, true, false, 1, 20, PrefabType.Animal, true, 10, "https://www.dropbox.com/s/04faw4m5yul2o1v/bear.png?dl=1"),
                    [2806645796] = new ItemInfo(true, "boar", "telephone", "assets/rust.ai/agents/boar/boar.prefab", 15, true, true, false, 1, 20, PrefabType.Animal, true, 10, "https://www.dropbox.com/s/s7bvbtctorqb1qo/boar.png?dl=1"),

                    [2668843731] = new ItemInfo(true, "oak tree", "electric.teslacoil", TreeType.Oak, 15, true, true, false, 3, 20, PrefabType.Tree, true, 10, "https://www.dropbox.com/s/7bn4ptx2g3v8jyk/oak%20tree.png?dl=1"),
                    [2668843336] = new ItemInfo(true, "beech tree", "electric.teslacoil", TreeType.Beech, 15, true, true, false, 3, 20, PrefabType.Tree, true, 10, "https://www.dropbox.com/s/py0m2m8i3yfocru/beech%20tree.png?dl=1"),
                    [2668843556] = new ItemInfo(true, "birch tree", "electric.teslacoil", TreeType.Birch, 15, true, true, false, 3, 20, PrefabType.Tree, true, 10, "https://www.dropbox.com/s/q3f6wyuopgwzgfa/birch%20tree.png?dl=1"),
                    [2668843841] = new ItemInfo(true, "palm tree", "electric.teslacoil", TreeType.Palm, 15, true, true, false, 3, 20, PrefabType.Tree, true, 10, "https://www.dropbox.com/s/5705n6xakmznd8o/palm%20tree.png?dl=1"),
                    [2668843981] = new ItemInfo(true, "pine tree", "electric.teslacoil", TreeType.Pine, 15, true, true, false, 3, 20, PrefabType.Tree, true, 10, "https://www.dropbox.com/s/zq62yzmia8h1cmc/pine%20tree.png?dl=1"),
                    [2668844123] = new ItemInfo(true, "swamp tree", "electric.teslacoil", TreeType.Swamp, 15, true, true, false, 3, 20, PrefabType.Tree, true, 10, "https://www.dropbox.com/s/r16q8nva7mvnvh1/swamp%20tree.png?dl=1"),
                    [2830312612] = new ItemInfo(true, "cactus", "electric.teslacoil", TreeType.Cacti, 5, true, true, false, 3, 20, PrefabType.Tree, true, 10, "https://www.dropbox.com/s/nx6arvwerzfts7h/cactus.png?dl=1"),

                    [2668860584] = new ItemInfo(true, "creosote bush", "electric.teslacoil", BushType.Creosote, 0, true, true, false, 3, 20, PrefabType.Bush, true, 10, "https://www.dropbox.com/s/4e651sbeodroqup/creosote%20bush.png?dl=1"),
                    [2668861030] = new ItemInfo(true, "snow willow bush", "electric.teslacoil", BushType.Willow_snow, 0, true, true, false, 3, 20, PrefabType.Bush, true, 10, "https://www.dropbox.com/s/41k3rwsxg6847vb/snow%20willow%20bush.png?dl=1"),
                    [2668861281] = new ItemInfo(true, "willow bush", "electric.teslacoil", BushType.Willow, 0, true, true, false, 3, 20, PrefabType.Bush, true, 10, "https://www.dropbox.com/s/xlu9mtasvov59g1/willow%20bush.png?dl=1"),
                    [2668861630] = new ItemInfo(true, "spice bush", "electric.teslacoil", BushType.Spice, 0, true, true, false, 3, 20, PrefabType.Bush, true, 10, "https://www.dropbox.com/s/rlr5wb59rhx1u5z/spice%20bush.png?dl=1"),
                    [2668861850] = new ItemInfo(true, "snow spice bush", "electric.teslacoil", BushType.Spice_snow, 0, true, true, false, 3, 20, PrefabType.Bush, true, 10, "https://www.dropbox.com/s/u8i0kihsqdhzcgw/snow%20spice%20bush.png?dl=1"),

                    [2668807382] = new ItemInfo(true, "decorative corn", "electric.teslacoil", "assets/bundled/prefabs/autospawn/collectable/corn/corn-collectable.prefab", 0, true, true, false, 3, 20, PrefabType.Bush, true, 10, "https://www.dropbox.com/s/36k8im1ifr573m7/decorative%20corn.png?dl=1"),
                    [2668906806] = new ItemInfo(true, "decorative pumpkin", "electric.teslacoil", "assets/bundled/prefabs/autospawn/collectable/pumpkin/pumpkin-collectable.prefab", 0, true, true, false, 3, 20, PrefabType.Bush, true, 10, "https://www.dropbox.com/s/pv45u1lmlg4e9y5/decorative%20pumpkin.png?dl=1"),
                    [2668914014] = new ItemInfo(true, "decorative potato", "electric.teslacoil", "assets/bundled/prefabs/autospawn/collectable/potato/potato-collectable.prefab", 0, true, true, false, 3, 20, PrefabType.Bush, true, 10, "https://www.dropbox.com/s/35fbck53pzqwaku/decorative%20potato.png?dl=1"),
                    [2668913894] = new ItemInfo(true, "decorative berries", "electric.teslacoil", BushType.Berries, 0, true, true, false, 3, 20, PrefabType.Bush, true, 10, "https://www.dropbox.com/s/wxgale7lp49205w/decorative%20berries.png?dl=1"),
                    [2668914545] = new ItemInfo(true, "dect' mushroom", "electric.teslacoil", BushType.Mushrooms, 0, true, true, false, 3, 20, PrefabType.Bush, true, 10, "https://www.dropbox.com/s/azcodsqthlkqaju/dect%27%20mushroom.png?dl=1"),
                    [2726178501] = new ItemInfo(true, "dect' hemp plant", "electric.teslacoil", "assets/bundled/prefabs/autospawn/collectable/hemp/hemp-collectable.prefab", 0, true, true, false, 3, 20, PrefabType.Bush, true, 10, "https://www.dropbox.com/s/e45sb8ocf6ymck2/dect%27%20hemp%20plant.png?dl=1")

                };
            }
        }

        private Dictionary<TreeType, TreeInfo> DefaultTrees
        {
            get
            {
                return new Dictionary<TreeType, TreeInfo>
                {
                    [TreeType.Beech] = new TreeInfo(new List<string>()
                    {
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_field/american_beech_d.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_field/american_beech_e.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_forest/american_beech_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_forest/american_beech_a_dead.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_forest/american_beech_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_forest/american_beech_c.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_forest_pine/american_beech_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_forest_pine/american_beech_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_forest_pine/american_beech_c.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_forestside/american_beech_d.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_forestside/american_beech_e.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_forestside/american_beech_e_dead.prefab"
                    }),
                    [TreeType.Birch] = new TreeInfo(new List<string>()
                    {
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_beachside/birch_small_temp.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_beachside/birch_tiny_temp.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_field/birch_small_temp.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_field/birch_tiny_temp.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_forest/birch_big_temp.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_forest/birch_large_temp.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_forest/birch_medium_temp.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_forest_pine/birch_big_temp.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_forest_pine/birch_large_temp.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_forestside/birch_medium_temp.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_forestside/birch_small_temp.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_forestside/birch_tiny_temp.prefab"
                    }),
                    [TreeType.Oak] = new TreeInfo(new List<string>()
                    {
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_field/oak_e.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_field/oak_f.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_field_large/oak_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_field_large/oak_c.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_field_large/oak_d.prefab"
                    }),
                    [TreeType.Palm] = new TreeInfo(new List<string>()
                    {
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_beachside/palm_tree_short_a_entity.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_beachside/palm_tree_short_b_entity.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_beachside/palm_tree_short_c_entity.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_beachside/palm_tree_small_a_entity.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_beachside/palm_tree_small_b_entity.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_beachside/palm_tree_small_c_entity.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_field/palm_tree_short_a_entity.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_field/palm_tree_short_b_entity.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_field/palm_tree_short_c_entity.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_field/palm_tree_small_b_entity.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_field/palm_tree_small_c_entity.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_forest/palm_tree_med_a_entity.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_forest/palm_tree_short_a_entity.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_forest/palm_tree_short_b_entity.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_forest/palm_tree_short_c_entity.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_forest/palm_tree_tall_a_entity.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_forest/palm_tree_tall_b_entity.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_forestside/palm_tree_short_a_entity.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_forestside/palm_tree_short_b_entity.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_forestside/palm_tree_short_c_entity.prefab"
                    }),
                    [TreeType.Pine] = new TreeInfo(new List<string>()
                    {
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_field/pine_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_field/pine_d.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_forest_pine/pine_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_forest_pine/pine_c.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_forestside_pine/pine_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_temp_forestside_pine/pine_d.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_tundra_field/pine_dead_d.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_tundra_field/pine_dead_e.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_tundra_field/pine_dead_f.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_tundra_field_pines/pine_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_tundra_field_pines/pine_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_tundra_field_pines/pine_d.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_tundra_forest/pine_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_tundra_forest/pine_c.prefab"
                    }),
                    [TreeType.Swamp] = new TreeInfo(new List<string>()
                    {
                        "assets/bundled/prefabs/autospawn/resource/swamp-trees/swamp_tree_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/swamp-trees/swamp_tree_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/swamp-trees/swamp_tree_c.prefab",
                        "assets/bundled/prefabs/autospawn/resource/swamp-trees/swamp_tree_d.prefab",
                        "assets/bundled/prefabs/autospawn/resource/swamp-trees/swamp_tree_e.prefab",
                        "assets/bundled/prefabs/autospawn/resource/swamp-trees/swamp_tree_f.prefab"
                    }),
                    [TreeType.Cacti] = new TreeInfo(new List<string>()
                    {
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_cactus/cactus-1.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_cactus/cactus-2.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_cactus/cactus-3.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_cactus/cactus-4.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_cactus/cactus-5.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_cactus/cactus-6.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_arid_cactus/cactus-7.prefab"
                    })
                };
            }
        }

        private Dictionary<BushType, BushInfo> DefaultBushes
        {
            get
            {
                return new Dictionary<BushType, BushInfo>
                {
                    [BushType.Creosote] = new BushInfo(new List<string>()
                    {
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arid_desert/creosote_bush_c.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arid_desert/creosote_bush_d.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arid_dry/creosote_bush_dry_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arid_dry/creosote_bush_dry_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arid_grass/creosote_bush_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arid_grass/creosote_bush_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arid_grass/creosote_bush_c.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arid_grass/creosote_bush_d.prefab"
                    }),
                    [BushType.Spice] = new BushInfo(new List<string>()
                    {
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_temp/bush_spicebush_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_temp/bush_spicebush_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_temp/bush_spicebush_c.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_temp/bush_spicebush_d.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_tundra/bush_spicebush_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_tundra/bush_spicebush_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_tundra/bush_spicebush_c.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_tundra/bush_spicebush_d.prefab"
                    }),
                    [BushType.Spice_snow] = new BushInfo(new List<string>()
                    {
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arctic_forest/bush_spicebush_a_snow.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arctic_forest/bush_spicebush_c_snow.prefab"
                    }),
                    [BushType.Willow] = new BushInfo(new List<string>()
                    {
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_temp/bush_willow_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_temp/bush_willow_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_temp/bush_willow_c.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_temp/bush_willow_d.prefab"
                    }),
                    [BushType.Willow_snow] = new BushInfo(new List<string>()
                    {
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arctic/bush_willow_snow_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arctic/bush_willow_snow_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arctic/bush_willow_snow_c.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arctic/bush_willow_snow_d.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arctic/bush_willow_snow_small_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arctic/bush_willow_snow_small_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arctic_forest/bush_willow_snow_a.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arctic_forest/bush_willow_snow_b.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arctic_forest/bush_willow_snow_c.prefab",
                        "assets/bundled/prefabs/autospawn/resource/v3_bushes_arctic_forest/bush_willow_snow_d.prefab"
                    }),
                    [BushType.Berries] = new BushInfo(new List<string>()
                    {
                        "assets/bundled/prefabs/autospawn/collectable/berry-black/berry-black-collectable.prefab",
                        "assets/bundled/prefabs/autospawn/collectable/berry-blue/berry-blue-collectable.prefab",
                        "assets/bundled/prefabs/autospawn/collectable/berry-green/berry-green-collectable.prefab",
                        "assets/bundled/prefabs/autospawn/collectable/berry-red/berry-red-collectable.prefab",
                        "assets/bundled/prefabs/autospawn/collectable/berry-white/berry-white-collectable.prefab",
                        "assets/bundled/prefabs/autospawn/collectable/berry-yellow/berry-yellow-collectable.prefab"
                    }),
                    [BushType.Mushrooms] = new BushInfo(new List<string>()
                    {
                        "assets/bundled/prefabs/autospawn/collectable/mushrooms/mushroom-cluster-5.prefab",
                        "assets/bundled/prefabs/autospawn/collectable/mushrooms/mushroom-cluster-6.prefab"
                    })
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
                {
                    PrintToConsole("Configuration appears to be outdated; updating and saving");
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
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

        #region Data

        PlayerEntity pcdData;
        private DynamicConfigFile PCDDATA;

        void Init()
        {
            PCDDATA = Interface.Oxide.DataFileSystem.GetFile(this.Name);
            permission.RegisterPermission("deployablenature.admin", this);
            permission.RegisterPermission("deployablenature.ignore.restrictions", this);
            permission.RegisterPermission("deployablenature.market.chat", this);
            permission.RegisterPermission("deployablenature.gather", this);
            permission.RegisterPermission("deployablenature.free", this);
            permission.RegisterPermission("deployablenature.use", this);

            if (!config.cupboard_kill) Unsubscribe("OnEntityDeath");

            var updated = false;
            if (config.auto_update)
            {
                if (UpdateBushes()) updated = true;
                if (UpdateTrees()) updated = true;
                if (UpdateItems()) updated = true;
            }

            if (updated) SaveConfig();

            if (!config.prevent_animal_targeting) Unsubscribe(nameof(OnNpcTarget));
        }

        bool UpdateBushes()
        {
            var result = false;
            foreach (var def in DefaultBushes)
            {
                if (!config.bushes.ContainsKey(def.Key))
                {
                    config.bushes.Add(def.Key, def.Value);
                    Puts($"Added new bush type: {def.Key}");
                    result = true;
                }
                else
                {
                    foreach (var path in def.Value.prefabs)
                    {
                        if (!config.bushes[def.Key].prefabs.Contains(path))
                        {
                            config.bushes[def.Key].prefabs.Add(path);
                            Puts($"Added new bush to {def.Key}: {path}");
                            result = true;
                        }
                    }
                }
            }
            return result;
        }

        bool UpdateTrees()
        {
            var result = false;
            foreach (var def in DefaultTrees)
            {
                if (!config.trees.ContainsKey(def.Key))
                {
                    config.trees.Add(def.Key, def.Value);
                    Puts($"Added new tree type: {def.Key}");
                    result = true;
                }
                else
                {
                    foreach (var path in def.Value.prefabs)
                    {
                        if (!config.trees[def.Key].prefabs.Contains(path))
                        {
                            config.trees[def.Key].prefabs.Add(path);
                            Puts($"Added new tree to {def.Key}: {path}");
                            result = true;
                        }
                    }
                }
            }
            return result;
        }

        bool UpdateItems()
        {
            var result = false;
            foreach (var def in DefaultPrefabs)
            {
                if (!config.Prefabs.ContainsKey(def.Key))
                {
                    config.Prefabs.Add(def.Key, def.Value);
                    result = true;
                    Puts($"Added new item: {def.Value.displayName}[{def.Key}]");
                }
            }
            return result;
        }

        void Unload()
        {
            SaveData();
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "NatureMarket");
                CuiHelper.DestroyUi(player, "MenuBackPanel");
            }
        }

        void Loaded()
        {
            LoadData();
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
                Puts("Couldn't load player data, creating new Playerfile");
                pcdData = new PlayerEntity();
            }
        }

        class PlayerEntity
        {
            public Dictionary<ulong, PCDInfo> pEntity = new Dictionary<ulong, PCDInfo>();
            public Dictionary<ulong, PrefabInfo> rEntity = new Dictionary<ulong, PrefabInfo>();
            public bool purgeEnabled = false;
        }

        public class CustomCurrency
        {
            public string name = null;
            public ulong skinID = 0;
            public string shortname = null;
        }

        class PCDInfo
        {
            public int deployed_rocks;
            public int deployed_trees;
            public int deployed_bushes;
            public int deployed_animals;
        }

        class PrefabInfo
        {
            public bool AuthDamageOnly;
            public int hits_taken;
            public int max_hits;
            public ulong skinID;
            public bool prevent_gather;
            public PrefabType type;
            public ulong ownerID;
            public Vector3 loc;
            public Vector3 rot;
            public string path;
            public ulong tool_cupboard_id;
        }

        public class ItemInfo
        {
            public bool enabled;
            public string displayName;
            public string item_shortname;
            public string prefab_path;
            public int prefab_durability;
            public bool TCAuthRequired;
            public bool canBePickedUp;
            public bool authDamageOnly;
            public int max_spawn_quantity;
            public int max_stack_size;
            public PrefabType prefab_type;
            public bool prevent_gather;
            public TreeType treeType = TreeType.None;
            public BushType bushType = BushType.None;
            public double market_price;
            public string img_url;

            [JsonConstructor]
            public ItemInfo() { }
            public ItemInfo(bool enabled, string displayName, string item_shortname, string prefab_path, int prefab_durability, bool TCAuthRequired, bool canBePickedUp, bool authDamageOnly, int max_spawn_quantity, int max_stack_size, PrefabType prefab_type, bool prevent_gather, float market_price, string img_url)
            {
                this.enabled = enabled;
                this.displayName = displayName;
                this.item_shortname = item_shortname;
                this.prefab_path = prefab_path;
                this.prefab_durability = prefab_durability;
                this.TCAuthRequired = TCAuthRequired;
                this.canBePickedUp = canBePickedUp;
                this.authDamageOnly = authDamageOnly;
                this.max_spawn_quantity = max_spawn_quantity;
                this.max_stack_size = max_stack_size;
                this.prefab_type = prefab_type;
                this.prevent_gather = prevent_gather;
                this.market_price = market_price;
                this.img_url = img_url;
            }

            public ItemInfo(bool enabled, string displayName, string item_shortname, TreeType treeType, int prefab_durability, bool TCAuthRequired, bool canBePickedUp, bool authDamageOnly, int max_spawn_quantity, int max_stack_size, PrefabType prefab_type, bool prevent_gather, float market_price, string img_url)
            {
                this.enabled = enabled;
                this.displayName = displayName;
                this.item_shortname = item_shortname;
                this.treeType = treeType;
                this.prefab_durability = prefab_durability;
                this.TCAuthRequired = TCAuthRequired;
                this.canBePickedUp = canBePickedUp;
                this.authDamageOnly = authDamageOnly;
                this.max_spawn_quantity = max_spawn_quantity;
                this.max_stack_size = max_stack_size;
                this.prefab_type = prefab_type;
                this.prevent_gather = prevent_gather;
                this.market_price = market_price;
                this.img_url = img_url;
            }

            public ItemInfo(bool enabled, string displayName, string item_shortname, BushType bushType, int prefab_durability, bool TCAuthRequired, bool canBePickedUp, bool authDamageOnly, int max_spawn_quantity, int max_stack_size, PrefabType prefab_type, bool prevent_gather, float market_price, string img_url)
            {
                this.enabled = enabled;
                this.displayName = displayName;
                this.item_shortname = item_shortname;
                this.bushType = bushType;
                this.prefab_durability = prefab_durability;
                this.TCAuthRequired = TCAuthRequired;
                this.canBePickedUp = canBePickedUp;
                this.authDamageOnly = authDamageOnly;
                this.max_spawn_quantity = max_spawn_quantity;
                this.max_stack_size = max_stack_size;
                this.prefab_type = prefab_type;
                this.prevent_gather = prevent_gather;
                this.market_price = market_price;
                this.img_url = img_url;
            }
        }

        public class TreeInfo
        {
            public List<string> prefabs = new List<string>();
            public TreeInfo(List<string> prefabs)
            {
                this.prefabs = prefabs;
            }
        }

        public class BushInfo
        {
            public List<string> prefabs = new List<string>();
            public BushInfo(List<string> prefabs)
            {
                this.prefabs = prefabs;
            }
        }

        public enum PrefabType
        {
            Rock,
            Tree,
            Bush,
            Animal
        }

        public enum TreeType
        {
            None,
            Palm,
            Oak,
            Swamp,
            Birch,
            Beech,
            Pine,
            Cacti
        }

        public enum BushType
        {
            None,
            Willow,
            Willow_snow,
            Spice,
            Spice_snow,
            Creosote,
            Berries,
            Mushrooms
        }

        public string Prefix;

        public Dictionary<ulong, ItemInfo> loot_table_rocks = new Dictionary<ulong, ItemInfo>();
        public Dictionary<ulong, ItemInfo> loot_table_animals = new Dictionary<ulong, ItemInfo>();
        public Dictionary<ulong, ItemInfo> loot_table_trees = new Dictionary<ulong, ItemInfo>();
        public Dictionary<ulong, ItemInfo> loot_table_bushes = new Dictionary<ulong, ItemInfo>();
        public Dictionary<TreeType, List<string>> trees = new Dictionary<TreeType, List<string>>();

        #endregion;

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Prefix"] = "<color=#DFF008>[DN]</color>",
                ["WarnMarketCooldown"] = "{0} Please wait a second before buying another item.",
                ["NoPickup"] = "{0} You cannot collect this entity as it is decorative.",
                ["NoGather"] = "{0} You cannot gather from this entity as it is decorative.",
                ["PlacementLimit"] = "{0} You have reached your placement limit for this item type.",
                ["BuildAuthReq"] = "{0} You must have building auth in order to deploy this item.",
                ["NP"] = "{0} You cannot pick this entity up.",
                ["WP"] = "{0} Please wait a second before picking up another entity.",
                ["GiveItem"] = "{0} You Received {1}x {2}.",
                ["NotifyConsoleInvalid"] = "{0} Invalid skin: {1}. A list of valid keys has been printed to console.",
                ["NotifyOfConsole"] = "{0} Usage: /giveprefab <skin id> <optional: quantity>\nA list of valid keys has been printed to console.",
                ["NotifyInConsole"] = "{0}",
                ["NEC"] = "{0} You do not have enough {1} to purchase {2}x {3}.",
                ["broadcastmsg"] = "{0} You can type <color=#51ff00>/{1}</color> to access the Nature Market and purchase deployable nature items.",
                ["itemDisabled"] = "{0} This item has been disabled.",
                ["dnkillentities"] = "Removed all deployable nature entities",
                ["dnkillentitiesdata"] = "Removed all deployable nature entities and cleared data.",
                ["NoUsePerms"] = "You do not have permission to use Deployable Nature items.",
                ["PickupNotify"] = "You can collect your deployable nature items by looking at them with your <color=#ff8300>{0}</color> active, and pressing your <color=#ff8300>middle mouse button</color>, or by using the <color=#ff8300>/dnpickup</color> command.\nYou can also adjust the deployment height of the entity by holding down <color=#ff8300>shift</color> while deploying.",
                ["PickupNotifyNoInput"] = "You can collect your deployable nature items by looking at them while using the <color=#ff8300>/dnpickup</color> command.\nYou can also adjust the deployment height of the entity by holding down <color=#ff8300>shift</color> while deploying.",
                ["dnkillentitiesplayer"] = "Deleted {0} entities for {1}.",
                ["PurgeEnabled"] = "Purge enabled: {0}.\nYou can enable/disable with dnpurge true/false.",
                ["PurgeEnabledAnnouncement"] = "Purge is enabled and all deployable nature items have been deleted.",
                ["PurgeDisabledAnnouncement"] = "Purge is no longer enabled. You can now deploy nature entities."
            }, this);
        }

        #endregion

        #region Hooks

        List<ulong> shown = new List<ulong>();

        void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (newItem != null && (newItem.info.shortname == "hammer" || newItem.info.shortname == "toolgun") && permission.UserHasPermission(player.UserIDString, "deployablenature.use") && !shown.Contains(player.userID))
            {
                PrintToChat(player, config.use_input_command ? string.Format(lang.GetMessage("PickupNotify", this, player.UserIDString), newItem.info.displayName.english) : lang.GetMessage("PickupNotifyNoInput", this, player.UserIDString));
                shown.Add(player.userID);
            }
        }

        object CanNpcEat(BaseNpc npc, BaseEntity target)
        {
            if (npc == null || target == null) return null;
            if (IsDeployableNature(target)) return false;
            return null;
        }

        object OnItemRecycle(Item item, Recycler recycler)
        {
            if (config.Prefabs.ContainsKey(item.skin)) return false;
            return null;
        }

        object OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            object result = null;
            var player = entity as BasePlayer;
            PrefabInfo prefabData;
            if (pcdData.rEntity.TryGetValue(dispenser.baseEntity.net.ID.Value, out prefabData) && prefabData.prevent_gather)
            {
                PrintToChat(player, string.Format(lang.GetMessage("NoGather", this, player.UserIDString), Prefix));
                result = false;
            }

            if (!permission.UserHasPermission(player.UserIDString, "deployablenature.gather")) return result;
            if (dispenser.gatherType == ResourceDispenser.GatherType.Ore && result == null && config.drop_rate_from_node > 0)
            {
                var roll = UnityEngine.Random.Range(1, 101);
                if (roll <= config.drop_rate_from_node)
                {                    
                    var randomRock = loot_table_rocks.ToList().GetRandom();
                    GiveItem(player, randomRock.Key, UnityEngine.Random.Range(1, randomRock.Value.max_spawn_quantity + 1));
                }
            }
            if (dispenser.gatherType == ResourceDispenser.GatherType.Tree && result == null && config.drop_rate_from_tree > 0)
            {
                var roll = UnityEngine.Random.Range(1, 101);
                if (roll <= config.drop_rate_from_tree)
                {
                    var randomTree = loot_table_trees.ToList().GetRandom();
                    GiveItem(player, randomTree.Key, UnityEngine.Random.Range(1, randomTree.Value.max_spawn_quantity + 1));
                }
            }

            if (dispenser.gatherType == ResourceDispenser.GatherType.Flesh && result == null && config.drop_rate_from_animal > 0)
            {
                var roll = UnityEngine.Random.Range(1, 101);
                if (roll <= config.drop_rate_from_animal)
                {
                    if (!config.animal_specific_drops)
                    {
                        var randomAnimal = loot_table_animals.ToList().GetRandom();
                        GiveItem(player, randomAnimal.Key, UnityEngine.Random.Range(1, randomAnimal.Value.max_spawn_quantity + 1));
                    }
                    else
                    {
                        var prefab = GetAnimalPrefab(dispenser.baseEntity.ShortPrefabName);
                        if (!string.IsNullOrEmpty(prefab))
                        {
                            foreach (var kvp in loot_table_animals)
                            {
                                if (kvp.Value.prefab_path == prefab)
                                {
                                    GiveItem(player, kvp.Key, UnityEngine.Random.Range(1, kvp.Value.max_spawn_quantity + 1));
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        string GetAnimalPrefab(string corpse_shortname)
        {
            switch (corpse_shortname)
            {
                case "bear.corpse": return "assets/rust.ai/agents/bear/polarbear.prefab";
                case "polarbear.corpse": return "assets/rust.ai/agents/bear/polarbear.prefab";
                case "chicken.corpse": return "assets/rust.ai/agents/chicken/chicken.prefab";
                case "stag.corpse": return "assets/rust.ai/agents/stag/stag.prefab";
                case "boar.corpse": return "assets/rust.ai/agents/boar/boar.prefab";
                case "wolf.corpse": return "assets/rust.ai/agents/wolf/wolf.prefab";
                default: return null;
            }
        }

        object OnDispenserBonus(ResourceDispenser dispenser, BaseEntity entity, Item item) => OnDispenserGather(dispenser, entity, item);

        List<ulong> old_net_IDs = new List<ulong>();

        class CreationInfo
        {
            public string PrefabName;
            public Vector3 pos;
            public Quaternion rot;
            public ulong skinID;
            public ulong ownerID;
            public CreationInfo(string PrefabName, Vector3 pos, Quaternion rot, ulong skinID, ulong ownerID)
            {
                this.PrefabName = PrefabName;
                this.pos = pos;
                this.rot = rot;
                this.skinID = skinID;
                this.ownerID = ownerID;
            }
        }

        object OnCollectiblePickup(CollectibleEntity collectible, BasePlayer player)
        {
            if (collectible == null || collectible.itemList == null || collectible.itemList.Length == 0 || player == null || player.IsNpc) return null;
            object result = null;
            PrefabInfo prefabData;
            if (pcdData.rEntity.TryGetValue(collectible.net.ID.Value, out prefabData) && prefabData.prevent_gather && collectible.itemList != null)
            {                
                PrintToChat(player, string.Format(lang.GetMessage("NoPickup", this, player.UserIDString), Prefix));
                result = false;
            }
            else
            {
                if (old_net_IDs.Contains(collectible.net.ID.Value))
                {
                    result = false;
                    old_net_IDs.Remove(collectible.net.ID.Value);
                }
            }
            if (!permission.UserHasPermission(player.UserIDString, "deployablenature.gather")) return result;
            if (result == null)
            {
                var roll = UnityEngine.Random.Range(1, 101);
                if (roll <= config.drop_rate_from_collectables && config.drop_rate_from_collectables > 0)
                {
                    List<KeyValuePair<ulong, ItemInfo>> temp_list = Pool.GetList<KeyValuePair<ulong, ItemInfo>>();
                    temp_list.AddRange(loot_table_bushes);
                    var randomPlant = temp_list.GetRandom();
                    GiveItem(player, randomPlant.Key, UnityEngine.Random.Range(1, randomPlant.Value.max_spawn_quantity + 1));
                    Pool.FreeList(ref temp_list);
                }
            }
            return result;
        }

        void OnLootSpawn(LootContainer container)
        {
            if (container == null || container.GetEntity() == null) return;
            try
            {
                if (container.GetEntity().ShortPrefabName == "minecart" && container.inventory.itemList.Count < 12)
                {
                    var roll = UnityEngine.Random.Range(1, 101);
                    if (roll <= config.drop_rate_from_minecart)
                    {
                        if (container.inventory.itemList.Count >= container.inventorySlots && container.inventorySlots < 12) container.inventorySlots += 1;
                        var randomRock = loot_table_rocks.ToList().GetRandom();
                        var item = ItemManager.CreateByName(randomRock.Value.item_shortname, UnityEngine.Random.Range(1, randomRock.Value.max_spawn_quantity + 1), randomRock.Key);
                        item.name = randomRock.Value.displayName;
                        NextTick(() =>
                        {
                            if (!item.MoveToContainer(container.inventory)) item.Remove();
                        });
                    }
                }
            }
            catch { }
        }

        void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            CuiHelper.DestroyUi(player, "NatureMarket");
            CuiHelper.DestroyUi(player, "MenuBackPanel");
        }


        object OnPlayerAttack(BasePlayer attacker, HitInfo info)
        {
            if (attacker == null || info == null || info.HitEntity == null) return null;
            PrefabInfo prefabData;
            ulong id = info.HitEntity.net.ID.Value;
            if (id == 0) return null;
            if (pcdData.rEntity.TryGetValue(id, out prefabData) && prefabData.prevent_gather)
            {
                return false;
            }
            return null;
        }

        object OnMaxStackable(Item item)
        {
            ItemInfo i;
            if (config.Prefabs.TryGetValue(item.skin, out i) && i.max_stack_size > 0)
                return i.max_stack_size;
            return null;
        }

        object CanCombineDroppedItem(DroppedItem item, DroppedItem targetItem)
        {
            if (item == null || targetItem == null) return null;
            if ((config.Prefabs.ContainsKey(item.item.skin) || config.Prefabs.ContainsKey(targetItem.item.skin)) && item.item.skin != targetItem.item.skin) return false;
            return null;
        }

        private Dictionary<string, string> loadOrder = new Dictionary<string, string>();

        void OnServerInitialized(bool initial)
        {
            Prefix = lang.GetMessage("Prefix", this);
            if (config.market_enabled && ImageLibrary == null)
            {
                Puts("ImageLibrary is required to run this plugin when the market is enabled.");
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }

            if (config.currency.ToUpper() == "SR" && ServerRewards == null)
            {
                Puts($"{config.currency.TitleCase()} has been set as the currency, but is not loaded. Please load the {config.currency.TitleCase()} plugin and try again or set the currency to something else.");
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }

            if (config.currency.ToUpper() == "ECONOMICS" && Economics == null)
            {
                Puts($"{config.currency.TitleCase()} has been set as the currency, but is not loaded. Please load the {config.currency.TitleCase()} plugin and try again or set the currency to something else.");
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }

            if (!config.manage_stacks)
            {
                Unsubscribe(nameof(OnMaxStackable));
            }

            if (!config.notify_player_with_hammer || pcdData.purgeEnabled) Unsubscribe("OnActiveItemChanged");
            if (!config.use_input_command || pcdData.purgeEnabled) Unsubscribe("OnPlayerInput");

            CheckForNewItems();

            if (!config.prevent_recycling) Unsubscribe("OnItemRecycle");

            if (config.currency.ToUpper() == "CUSTOM" && config.custom_currency == null)
            {
                config.custom_currency = new CustomCurrency() { name = "scrap", shortname = "scrap", skinID = 0 };
                SaveConfig();
            }

            cmd.AddChatCommand(config.market_cmd, this, "NatureMarketCMD");
            
            if (config.chat_delay > 0)
            {
                timer.Every(config.chat_delay, () =>
                {
                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        if (permission.UserHasPermission(player.UserIDString, "deployablenature.market.chat") || permission.UserHasPermission(player.UserIDString, "deployablenature.admin")) PrintToChat(player, string.Format(lang.GetMessage("broadcastmsg", this, player.UserIDString), Prefix, config.market_cmd));
                    }
                });
            }            

            if (config.drop_rate_from_minecart == 0) Unsubscribe("OnLootSpawn");

            var entities = BaseNetworkable.serverEntities.Where(x => pcdData.rEntity.ContainsKey(x.net.ID.Value)).ToList();
            var entities_id = entities.Select(x => x.net.ID.Value).ToList();

            foreach (var ent in pcdData.rEntity.ToList())
            {
                try
                {
                    if (!entities_id.Contains(ent.Key))
                    {
                        if (ent.Value.ownerID == 0 || ent.Value.loc == Vector3.zero || ent.Value.rot == Vector3.zero || ent.Value.path == null)
                        {
                            pcdData.rEntity.Remove(ent.Key);
                        }
                        else
                        {
                            ItemInfo info;
                            if (config.Prefabs.TryGetValue(ent.Value.skinID, out info))
                            {
                                CreateEntity(ent.Value.path, ent.Value.loc, Quaternion.Euler(ent.Value.rot), ent.Value.skinID, ent.Value.ownerID, info.prefab_durability, info.authDamageOnly, info.prevent_gather);
                            }
                            pcdData.rEntity.Remove(ent.Key);
                        }
                    }
                    else
                    {
                        var entity = entities.Where(x => ent.Key == x.net.ID.Value).FirstOrDefault() as BaseEntity;
                        if (entity == null) continue;
                        if (ent.Value.ownerID == 0) ent.Value.ownerID = entity.OwnerID;
                        if (ent.Value.loc == Vector3.zero) ent.Value.loc = entity.transform.position;
                        if (ent.Value.rot == Vector3.zero) ent.Value.rot = entity.transform.rotation.eulerAngles;
                        if (ent.Value.path == null) ent.Value.path = entity.PrefabName;
                    }
                }
                catch { }
            }
            

            foreach (KeyValuePair<ulong, PrefabInfo> kvp in pcdData.rEntity)
            {
                ItemInfo itemData;
                if (config.Prefabs.TryGetValue(kvp.Value.skinID, out itemData))
                {
                    kvp.Value.max_hits = itemData.prefab_durability;
                    kvp.Value.AuthDamageOnly = itemData.authDamageOnly;
                    kvp.Value.prevent_gather = itemData.prevent_gather;
                    kvp.Value.type = itemData.prefab_type;
                }                
            }

            var doSave = false;
            foreach (KeyValuePair<ulong, ItemInfo> kvp in config.Prefabs)
            {
                if (!kvp.Value.enabled) continue;
                if (!string.IsNullOrEmpty(kvp.Value.img_url))
                {
                    if (kvp.Value.img_url.StartsWith("https://imgur"))
                    {
                        ItemInfo defaultData;
                        if (DefaultPrefabs.TryGetValue(kvp.Key, out defaultData))
                        {
                            kvp.Value.img_url = defaultData.img_url;
                            Puts($"Updated URL for {kvp.Value.displayName}.");
                            doSave = true;
                        }
                    }
                    loadOrder.Add(kvp.Key.ToString(), kvp.Value.img_url);
                }
                //ImageLibrary?.Call("AddImage", kvp.Value.img_url, kvp.Key.ToString());
                
                for (int i = 0; i < 99; i++)
                {
                    if (!market_pages.ContainsKey(i))
                    {
                        market_pages.Add(i, new List<ulong> { kvp.Key });
                        break;
                    }
                    else if (market_pages[i].Count < 6)
                    {
                        market_pages[i].Add(kvp.Key);
                        break;
                    }
                }
            }

            if (doSave) SaveConfig();

            if (loadOrder.Count > 0)
            {
                Puts($"Loading {loadOrder.Count} images into ImageLibrary.");
                ImageLibrary?.Call("ImportImageList", this.Name, loadOrder, 0ul, doSave, new Action(ImagesReady));
            }
            else ImagesReady();

            // Cleans the data file of removed entities.
            List<ulong> found = new List<ulong>();
            Dictionary<ulong, PCDInfo> ent_count = new Dictionary<ulong, PCDInfo>();
            foreach (KeyValuePair<ulong, PCDInfo> kvp in pcdData.pEntity)
            {
                ent_count.Add(kvp.Key, new PCDInfo());
            }

            foreach (var entity in BaseNetworkable.serverEntities)
            {
                if (pcdData.rEntity.ContainsKey(entity.net.ID.Value))
                {
                    found.Add(entity.net.ID.Value);
                    var ent = entity as BaseEntity;
                    if (ent == null || ent.OwnerID == 0) continue;
                    PCDInfo playerData;
                    if (!ent_count.TryGetValue(ent.OwnerID, out playerData)) ent_count.Add(ent.OwnerID, playerData = new PCDInfo());
                    switch (pcdData.rEntity[entity.net.ID.Value].type)
                    {
                        case PrefabType.Bush:
                            playerData.deployed_bushes++;
                            break;
                        case PrefabType.Rock:
                            playerData.deployed_rocks++;
                            break;
                        case PrefabType.Tree:
                            playerData.deployed_trees++;
                            break;
                        case PrefabType.Animal:
                            playerData.deployed_animals++;
                            break;
                    }
                }
            }

            foreach (var key in pcdData.rEntity.Keys.Except(found).ToList())
            {
                pcdData.rEntity.Remove(key);
            }

            if (ent_count != null)
            {
                foreach (var kvp in ent_count)
                {
                    if (!pcdData.pEntity.ContainsKey(kvp.Key)) pcdData.pEntity.Add(kvp.Key, new PCDInfo());
                    pcdData.pEntity[kvp.Key] = kvp.Value;
                }
            }           


            foreach (KeyValuePair<ulong, ItemInfo> kvp in config.Prefabs)
            {
                if (kvp.Value.enabled)
                {
                    if (kvp.Value.prefab_type == PrefabType.Bush && !loot_table_bushes.ContainsKey(kvp.Key)) loot_table_bushes.Add(kvp.Key, kvp.Value);
                    if (kvp.Value.prefab_type == PrefabType.Tree && !loot_table_trees.ContainsKey(kvp.Key)) loot_table_trees.Add(kvp.Key, kvp.Value);
                    if (kvp.Value.prefab_type == PrefabType.Rock && !loot_table_rocks.ContainsKey(kvp.Key)) loot_table_rocks.Add(kvp.Key, kvp.Value);
                    if (kvp.Value.prefab_type == PrefabType.Animal && !loot_table_animals.ContainsKey(kvp.Key)) loot_table_animals.Add(kvp.Key, kvp.Value);
                }                               
            }      
            
            foreach (var perm in config.discount_permissions.Keys)
            {
                if (!permission.PermissionExists(perm, this))
                {
                    permission.RegisterPermission(perm, this);
                    Puts($"Registered permission: {perm}");
                }
            }

            if (pcdData.purgeEnabled)
            {
                PrintToChat(lang.GetMessage("PurgeEnabledAnnouncement", this));
                KillAllEntities(false);
            }
        }

        private void ImagesReady()
        {
            loadOrder.Clear();
            loadOrder = null;
            Puts($"Loaded all images for DeployableNature.");
        }

        void OnMeleeAttack(BasePlayer player, HitInfo info)
        {
            if (player != null && !player.IsNpc && info != null && info.HitEntity != null)
            {
                var entity = info.HitEntity;
                if (!IsDestroyableRock(entity)) return;
                PrefabInfo ri;
                if (!pcdData.rEntity.TryGetValue(entity.net.ID.Value, out ri)) pcdData.rEntity.Add(entity.net.ID.Value, ri = new PrefabInfo());
                if (ri.AuthDamageOnly && !player.IsBuildingAuthed() && entity.GetBuildingPrivilege() != null) return;
                ri.hits_taken++;
                if (ri.max_hits > 0 && ri.hits_taken >= ri.max_hits)
                {
                    //Break rock
                    PCDInfo pi;
                    if (pcdData.pEntity.TryGetValue(entity.net.ID.Value, out pi))
                    {
                        if (ri.type == PrefabType.Rock && pi.deployed_rocks > 0) pi.deployed_rocks--;
                        if (ri.type == PrefabType.Animal && pi.deployed_animals > 0) pi.deployed_animals--;
                        if (ri.type == PrefabType.Tree && pi.deployed_trees > 0) pi.deployed_trees--;
                        if (ri.type == PrefabType.Bush && pi.deployed_bushes > 0) pi.deployed_bushes--;
                    }
                    ulong id = entity.net.ID.Value;
                    entity.Invoke(entity.KillMessage, 0.01f);
                    timer.Once(0.05f, () => pcdData.rEntity.Remove(id));
                }
            }            
        }        

        void OnNewSave(string filename)
        {
            Puts("New map wipe - clearing data.");
            pcdData.pEntity.Clear();
            pcdData.rEntity.Clear();
            pcdData.purgeEnabled = false;
            SaveData();
        }

        public void CreateEntity(string prefab, Vector3 pos, Quaternion rot, ulong skin, ulong ownerID, int max_hits, bool authDamageOnly, bool prevent_gather)
        {
            
            var result = GameManager.server.CreateEntity(prefab, pos, rot, true);
            result.skinID = skin;
            result.OwnerID = ownerID;
            result.Spawn();            
            if (!pcdData.rEntity.ContainsKey(result.net.ID.Value))
            {
                pcdData.rEntity.Add(result.net.ID.Value, new PrefabInfo()
                {
                    max_hits = max_hits,
                    AuthDamageOnly = authDamageOnly,
                    skinID = result.skinID,
                    prevent_gather = prevent_gather,
                    loc = pos,
                    rot = rot.eulerAngles,
                    ownerID = ownerID,
                    path = result.PrefabName,
                    tool_cupboard_id = result.GetBuildingPrivilege()?.net.ID.Value ?? 0
                });
            }
            if (result is BaseAnimalNPC)
            {
                var animal = result as BaseAnimalNPC;
                animal.brain = null;
                var rotation = rot;
                NextTick(() =>
                {
                    animal.transform.rotation = rotation;
                    animal.SendNetworkUpdateImmediate();
                });
            }
        }

        object OnNpcTarget(BaseEntity npc, BaseEntity entity)
        {
            if (pcdData.rEntity.ContainsKey(entity.net.ID.Value)) return true;
            return null;
        }


        void OnEntityKill(BaseNetworkable entity)
        {
            if (entity == null || entity.net == null) return;
            if (old_net_IDs.Contains(entity.net.ID.Value))
            {
                old_net_IDs.Remove(entity.net.ID.Value);
            }
        }

        object OnEntityTakeDamage(BaseAnimalNPC entity, HitInfo info)
        {            
            PrefabInfo ri;
            if (entity.skinID > 0 && pcdData.rEntity.TryGetValue(entity.net.ID.Value, out ri))
            {
                info?.damageTypes?.ScaleAll(0);
                var player = info?.InitiatorPlayer;
                if (player == null || player.IsNpc || !player.userID.IsSteamId()) return null;
                if (ri.AuthDamageOnly && !player.IsBuildingAuthed() && entity.GetBuildingPrivilege() != null) return null;
                ri.hits_taken++;
                if (ri.max_hits > 0 && ri.hits_taken >= ri.max_hits)
                {
                    PCDInfo pi;
                    if (pcdData.pEntity.TryGetValue(entity.net.ID.Value, out pi))
                    {
                        if (ri.type == PrefabType.Rock && pi.deployed_rocks > 0) pi.deployed_rocks--;
                        if (ri.type == PrefabType.Animal && pi.deployed_animals > 0) pi.deployed_animals--;
                        if (ri.type == PrefabType.Tree && pi.deployed_trees > 0) pi.deployed_trees--;
                        if (ri.type == PrefabType.Bush && pi.deployed_bushes > 0) pi.deployed_bushes--;
                    }
                    ulong id = entity.net.ID.Value;
                    entity.Invoke(entity.KillMessage, 0.01f);
                    timer.Once(0.05f, () => pcdData.rEntity.Remove(id));
                }
            }
            return null;
        }


        void OnEntityDeath(BuildingPrivlidge cupboard, HitInfo info)
        {
            if (cupboard == null) return;
            Dictionary<ulong, PrefabInfo> delete = new Dictionary<ulong, PrefabInfo>();
            foreach (KeyValuePair<ulong, PrefabInfo> kvp in pcdData.rEntity.Where(x => x.Value.tool_cupboard_id == cupboard.net.ID.Value).Select(y => y))
            {
                if (delete.ContainsKey(kvp.Key)) continue;
                delete.Add(kvp.Key, kvp.Value);
            }

            if (delete.Count == 0) return;

            List<BaseNetworkable> entities = Pool.GetList<BaseNetworkable>();
            entities.AddRange(BaseNetworkable.serverEntities.Where(x => delete.ContainsKey(x.net.ID.Value)));

            if (entities == null || entities.Count == 0)
            {
                Pool.FreeList(ref entities);
                return;
            }
                
            PCDInfo pi;
            ItemInfo itemData;
            foreach (var entry in delete)
            {
                if (!pcdData.pEntity.TryGetValue(entry.Value.ownerID, out pi)) continue;
                if (!config.Prefabs.TryGetValue(entry.Value.skinID, out itemData)) continue;
                switch (itemData.prefab_type)
                {
                    case PrefabType.Bush:
                        pi.deployed_bushes--;
                        break;
                    case PrefabType.Tree:
                        pi.deployed_trees--;
                        break;
                    case PrefabType.Rock:
                        pi.deployed_rocks--;
                        break;
                    case PrefabType.Animal:
                        pi.deployed_animals--;
                        break;
                }
                pcdData.rEntity.Remove(entry.Key);
            }

            foreach (var entity in entities)
            {
                // May need to test for Issues with killing entities in a list.
                entity.KillMessage();
            }
            Pool.FreeList(ref entities);
        }

        void OnEntityBuilt(Planner plan, GameObject go)
        {
            var entity = go?.ToBaseEntity();
            if (entity == null) return;
            var player = plan?.GetOwnerPlayer();
            if (player == null) return;

            ItemInfo itemData;
            if (!config.Prefabs.TryGetValue(entity.skinID, out itemData)) return;
            PCDInfo pi;
            if (!pcdData.pEntity.TryGetValue(player.userID, out pi)) pcdData.pEntity.Add(player.userID, pi = new PCDInfo());

            var pos = entity.transform.position;
            
            if (config.shift_place && player.serverInput.IsDown(BUTTON.SPRINT))
            {
                pos.y -= config.depth_modifier;
            }

            var prefabPath = itemData.prefab_path;
            if (prefabPath == null)
            {
                if (itemData.treeType != TreeType.None)
                {
                    TreeInfo treeData;
                    if (config.trees.TryGetValue(itemData.treeType, out treeData)) prefabPath = treeData.prefabs.GetRandom();
                    else prefabPath = "assets/bundled/prefabs/autospawn/resource/v3_temp_field/pine_b.prefab";
                }
                else if (itemData.bushType != BushType.None)
                {
                    BushInfo bushData;
                    if (config.bushes.TryGetValue(itemData.bushType, out bushData)) prefabPath = bushData.prefabs.GetRandom();
                    else prefabPath = "assets/bundled/prefabs/autospawn/clutter/v3_temp_bushes/bush_willow_a.prefab";
                }
            }

            CreateEntity(prefabPath, pos, entity.transform.rotation, entity.skinID, player.userID, itemData.prefab_durability, itemData.authDamageOnly, itemData.prevent_gather);
            entity.Invoke(entity.KillMessage, 0.01f);
            switch (itemData.prefab_type)
            {
                case PrefabType.Bush:
                    pi.deployed_bushes++;
                    break;
                case PrefabType.Tree:
                    pi.deployed_trees++;
                    break;
                case PrefabType.Rock:
                    pi.deployed_rocks++;
                    break;
                case PrefabType.Animal:
                    pi.deployed_animals++;
                    break;
            }

            if (!shown.Contains(player.userID))
            {
                PrintToChat(player, config.use_input_command ? string.Format(lang.GetMessage("PickupNotify", this, player.UserIDString), "Hammer") : lang.GetMessage("PickupNotifyNoInput", this, player.UserIDString));
                shown.Add(player.userID);
            }
        }

        object CanBuild(Planner planner, Construction prefab, Construction.Target target)
        {
            var player = planner?.GetOwnerPlayer();
            if (player == null) return null;
            if (permission.UserHasPermission(player.UserIDString, "deployablenature.ignore.restrictions")) return null;
            var item = planner?.GetItem();
            ItemInfo itemData;
            if (item == null || !config.Prefabs.TryGetValue(item.skin, out itemData)) return null;
            if (pcdData.purgeEnabled)
            {
                PrintToChat(player, "You cannot deploy nature entities while purge is enabled.");
                return false;
            }
            if (!permission.UserHasPermission(player.UserIDString, "deployablenature.use"))
            {
                PrintToChat(player, lang.GetMessage("NoUsePerms", this, player.UserIDString));
                return false;
            }
            PCDInfo pi;
            if (!pcdData.pEntity.TryGetValue(player.userID, out pi)) pcdData.pEntity.Add(player.userID, pi = new PCDInfo());
            if (!itemData.enabled)
            {
                PrintToChat(player, string.Format(lang.GetMessage("itemDisabled", this, player.UserIDString), Prefix));
                return false;
            }
            if ((config.max_trees > 0 && itemData.prefab_type == PrefabType.Tree && pi.deployed_trees >= config.max_trees) || (config.max_rocks > 0 && itemData.prefab_type == PrefabType.Rock && pi.deployed_rocks >= config.max_rocks) || (config.max_bushes > 0 && itemData.prefab_type == PrefabType.Bush && pi.deployed_bushes >= config.max_bushes) || (config.max_animals > 0 && itemData.prefab_type == PrefabType.Animal && pi.deployed_animals >= config.max_animals))
            {
                PrintToChat(player, string.Format(lang.GetMessage("PlacementLimit", this, player.UserIDString), Prefix));
                return false;
            }
            if (itemData.TCAuthRequired && !player.IsBuildingAuthed())
            {
                PrintToChat(player, string.Format(lang.GetMessage("BuildAuthReq", this, player.UserIDString), Prefix));
                return false;
            }            
            return null;
        }

        List<ulong> pickup_cooldown = new List<ulong>();        

        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (!input.WasJustReleased(BUTTON.FIRE_THIRD) || player == null) return;
            if (player.GetActiveItem() == null) return;
            var tool = player.GetActiveItem();
            if (tool == null || (!tool.info.shortname.Equals("toolgun") && !tool.info.shortname.Equals("hammer"))) return;
            PickupEntity(player);
        }

        void OnServerSave()
        {
            SaveData();
        }

        #endregion

        #region Helpers

        void CheckForNewItems()
        {
            try
            {
                var newItems = false;
                var missing_items = DefaultPrefabs.Where(x => !config.Prefabs.ContainsKey(x.Key)).ToList();
                if (missing_items != null && missing_items.Count > 0)
                {
                    newItems = true;
                    foreach (var item in missing_items)
                    {
                        config.Prefabs.Add(item.Key, item.Value);
                        Puts($"Added new bush - {item.Value.displayName} [{item.Key}]");
                    }
                    if (newItems) SaveConfig();
                }
            }
            catch { }
        }

        float GetModifier(string id)
        {
            if (permission.UserHasPermission(id, "deployablenature.free")) return 0f;
            var lowest = 1.0f;
            foreach (var perm in config.discount_permissions)
            {
                if (permission.UserHasPermission(id, perm.Key) && perm.Value < lowest) lowest = perm.Value;
            }
            return lowest;
        }

        bool isTeam(BasePlayer player, ulong owner)
        {
            if (player.Team == null || player.Team.members == null) return false;
            return player.Team.members.Contains(owner);
        }

        void PickupEntity(BasePlayer player)
        {            
            var target = GetTargetEntity(player);
            if (target == null || !pcdData.rEntity.ContainsKey(target.net.ID.Value)) return;            
            ItemInfo itemData;
            if ((target.OwnerID == player.userID || (config.team_pickup && isTeam(player, target.OwnerID)) || permission.UserHasPermission(player.UserIDString, "deployablenature.admin") || (config.auth_pickup && player.IsBuildingAuthed())) && config.Prefabs.TryGetValue(target.skinID, out itemData))
            {
                if (!itemData.canBePickedUp)
                {
                    PrintToChat(player, string.Format(lang.GetMessage("NP", this, player.UserIDString), Prefix));
                    return;
                }
                if (pickup_cooldown.Contains(player.userID))
                {
                    PrintToChat(player, string.Format(lang.GetMessage("WP", this, player.UserIDString), Prefix));
                    return;
                }
                ulong playerID = player.userID;
                pickup_cooldown.Add(playerID);
                timer.Once(0.5f, () => pickup_cooldown.Remove(playerID));
                PCDInfo pi;
                if (pcdData.pEntity.TryGetValue(target.OwnerID, out pi))
                {
                    switch (itemData.prefab_type)
                    {
                        case PrefabType.Bush:
                            pi.deployed_bushes--;
                            break;
                        case PrefabType.Tree:
                            pi.deployed_trees--;
                            break;
                        case PrefabType.Rock:
                            pi.deployed_rocks--;
                            break;
                        case PrefabType.Animal:
                            pi.deployed_animals--;
                            break;
                    }
                }
                pcdData.rEntity.Remove(target.net.ID.Value);
                GiveItem(player, target.skinID);
                target.Invoke(target.KillMessage, 0.01f);
            }
        }

        private const int LAYER_TARGET = ~(1 << 2 | 1 << 3 | 1 << 4 | 1 << 10 | 1 << 18 | 1 << 28 | 1 << 29);
        private BaseEntity GetTargetEntity(BasePlayer player, float dist = 5f)
        {
            RaycastHit raycastHit;
            bool flag = Physics.Raycast(player.eyes.HeadRay(), out raycastHit, 100f, LAYER_TARGET);
            var targetEntity = flag ? raycastHit.GetEntity() : null;
            return targetEntity;
        }

        bool IsDestroyableRock(BaseEntity entity)
        {
            return config.Prefabs.ContainsKey(entity.skinID) && entity.OwnerID > 0;
        }

        void GiveItem(BasePlayer player, ulong skin, int quantity = 1)
        {
            ItemInfo itemData;
            if (!config.Prefabs.TryGetValue(skin, out itemData)) return;
            var item = ItemManager.CreateByName(itemData.item_shortname, quantity, skin);
            item.name = itemData.displayName;
            foreach (var _item in player.inventory.AllItems())
            {
                if (_item.skin == skin && _item.amount < itemData.max_stack_size)
                {
                    if (!item.MoveToContainer(_item.GetRootContainer(), _item.position, true, true)) item.Remove();
                    PrintToChat(player, string.Format(lang.GetMessage("GiveItem", this, player.UserIDString), Prefix, quantity, itemData.displayName));
                    return;
                }
            }            
            player.GiveItem(item);
            PrintToChat(player, string.Format(lang.GetMessage("GiveItem", this, player.UserIDString), Prefix, quantity, itemData.displayName));
        }

        #endregion

        #region ChatCommands

        void NatureMarketCMD(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "deployablenature.market.chat") && !permission.UserHasPermission(player.UserIDString, "deployablenature.admin")) return;
            MenuBackPanel(player);
            SendNatureMenu(player);
        }

        [ChatCommand("dnpickup")]
        void DNPickupCMD(BasePlayer player)
        {
            PickupEntity(player);
        }

        BasePlayer FindOnlinePlayerByID(ulong id)
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.userID == id) return player;
            }
            return null;
        }

        private BasePlayer FindPlayerByName(string Playername, BasePlayer SearchingPlayer = null)
        {
            var targetList = BasePlayer.allPlayerList.Where(x => x.displayName.ToLower().Contains(Playername.ToLower())).OrderBy(x => x.displayName.Length);
            if (targetList.Count() == 1)
            {
                return targetList.First();
            }
            if (targetList.Count() > 1)
            {
                if (targetList.First().displayName.ToLower() == Playername.ToLower())
                {
                    return targetList.First();
                }
                if (SearchingPlayer != null)
                {
                    PrintToChat(SearchingPlayer, $"More than one player found: {String.Join(",", targetList.Select(x => x.displayName))}");
                }
                else Puts($"More than one player found: {String.Join(",", targetList.Select(x => x.displayName))}");
                return null;
            }
            if (targetList.Count() == 0)
            {
                if (SearchingPlayer != null)
                {
                    PrintToChat(SearchingPlayer, $"No player was found that matched: {Playername}");
                }
                else Puts($"No player was found that matched: {Playername}");
                return null;
            }
            return null;
        }

        [ConsoleCommand("giveprefab")]
        void ConsoleGivePrefab(ConsoleSystem.Arg arg)
        {            
            var player = arg.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, "deployablenature.admin")) return;
            BasePlayer foundPlayer;
            if (arg.Args == null || arg.Args.Length < 2)
            {
                if (player != null) PrintToConsole(player, "Invalid parameters: giveprefab <player ID/name> <skin ID> <optional: quantity>");
                else Puts("Invalid parameters: giveprefab <player ID/name> <skin ID> <optional: quantity>");
                return;
            }
            if (arg.Args[0].IsNumeric()) foundPlayer = FindOnlinePlayerByID(Convert.ToUInt64(arg.Args[0]));
            else
            {
                if (player != null) foundPlayer = FindPlayerByName(arg.Args[0], player);
                else foundPlayer = FindPlayerByName(arg.Args[0]);
            }

            if (foundPlayer == null || foundPlayer.IsDead() || !foundPlayer.IsConnected)
            {
                if (player != null) PrintToConsole(player, $"No player matched: {arg.Args[0]} was found, or they are dead.");
                else Puts($"No player matched: {arg.Args[0]} was found, or they are dead.");
                return;
            }
            if (!arg.Args[1].IsNumeric())
            {
                if (player != null) PrintToConsole(player, $"{arg.Args[1]} is not a valid skin ID.\n{GetValidSkins()}");
                else Puts($"{arg.Args[1]} is not a valid skin ID.\n{GetValidSkinsNoCol()}");
                return;
            }
            var skinID = Convert.ToUInt64(arg.Args[1]);
            if (!config.Prefabs.ContainsKey(skinID))
            {
                if (player != null) PrintToConsole(player, $"{arg.Args[1]} is not a valid skin ID.\n{GetValidSkins()}");
                else Puts($"{arg.Args[1]} is not a valid skin ID.\n{GetValidSkinsNoCol()}");
                return;
            }
            var quantity = 1;
            if (arg.Args.Length == 3 && arg.Args[2].IsNumeric()) quantity = Convert.ToInt32(arg.Args[2]);
            if (player != null) PrintToConsole(player, $"Gave {foundPlayer.displayName} {quantity}x {config.Prefabs[skinID].displayName}");
            else Puts($"Gave {foundPlayer.displayName} {quantity}x {config.Prefabs[skinID].displayName}");
            GiveItem(foundPlayer, skinID, quantity);
        }

        string GetValidSkins()
        {
            StringBuilder sb = new StringBuilder("Valid Skins:\n");
            foreach (KeyValuePair<ulong, ItemInfo> kvp in config.Prefabs)
            {
                sb.AppendFormat("\n- Key: <color=#ffff00>{0}(</color><color=#ffaa00>{1}</color><color=#ffff00>)</color>", kvp.Key, kvp.Value.displayName);
            }
            return sb.ToString();
        }

        string GetValidSkinsNoCol()
        {
            StringBuilder sb = new StringBuilder("Valid Skins:\n");
            foreach (KeyValuePair<ulong, ItemInfo> kvp in config.Prefabs)
            {
                sb.AppendFormat("\n- Key: {0}({1})", kvp.Key, kvp.Value.displayName);
            }
            return sb.ToString();
        }

        [ChatCommand("giveprefab")]
        void GivePrefab(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "deployablenature.admin")) return;
            if (args.Length == 0 || args.Length > 2)
            {
                
                PrintToChat(player, string.Format(lang.GetMessage("NotifyOfConsole", this), Prefix));
                PrintToConsole(player, string.Format(lang.GetMessage("NotifyInConsole", this), GetValidSkins()));
                return;
            }
            var quantity = 1;
            var skin = Convert.ToUInt64(args[0]);
            if (!config.Prefabs.ContainsKey(skin))
            {
                PrintToChat(player, string.Format(lang.GetMessage("NotifyConsoleInvalid", this), Prefix, GetValidSkins()));
                PrintToConsole(player, string.Format(lang.GetMessage("NotifyInConsole", this), GetValidSkins()));
                return;
            }
            if (args.Length == 2 && args[1].IsNumeric()) quantity = Convert.ToInt32(args[1]);
            GiveItem(player, skin, quantity);
        }

        #endregion

        #region Console commands

        [ConsoleCommand("dnpurge")]
        void PurgeCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, "deployablenature.admin")) return;

            if (arg.Args == null || arg.Args.Length == 0 || (!arg.Args[0].Equals("true", StringComparison.OrdinalIgnoreCase) && !arg.Args[0].Equals("false", StringComparison.OrdinalIgnoreCase)))
            {
                arg.ReplyWith(string.Format(lang.GetMessage("PurgeEnabled", this, player != null? player.UserIDString : null), pcdData.purgeEnabled));
                return;
            }
            if (arg.Args[0].Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                pcdData.purgeEnabled = true;                
                KillAllEntities(false);
                PrintToChat(lang.GetMessage("PurgeEnabledAnnouncement", this));
                
            }
            else
            {
                pcdData.purgeEnabled = false;
                PrintToChat(lang.GetMessage("PurgeDisabledAnnouncement", this));
                if (config.notify_player_with_hammer) Subscribe("OnActiveItemChanged");
                if (config.use_input_command) Subscribe("OnPlayerInput");
            }
            SaveData();
        }

        [ConsoleCommand("dnkillentities")]
        void KillEntities(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, "deployablenature.admin")) return;

            if (arg.Args != null && arg.Args.Length > 0 && arg.Args[0].Equals("true", StringComparison.OrdinalIgnoreCase)) KillAllEntities(true);
            else KillAllEntities(false);
            arg.ReplyWith(pcdData.rEntity.Count > 0 ? lang.GetMessage("dnkillentities", this) : lang.GetMessage("dnkillentitiesdata", this));
        }

        void KillAllEntities(bool deleteFromData)
        {
            var entityList = BaseNetworkable.serverEntities.Where(x => pcdData.rEntity.ContainsKey(x.net.ID.Value)).ToList();

            Puts($"Deleting {entityList.Count} entities.");
            foreach (var entity in entityList.ToList())
            {
                entity.KillMessage();
            }

            if (deleteFromData)
            {
                pcdData.rEntity.Clear();
                pcdData.pEntity.Clear();
                SaveData();
            }
        }

        [ConsoleCommand("dnkillentitiesforplayer")]
        void KillEntitiesForPlayer(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, "deployablenature.admin")) return;

            if (arg.Args == null || arg.Args.Length == 0)
            {
                arg.ReplyWith("Usage: /dnkillentitiesforplayer <player name/ID>");
                return;
            }
            var name = string.Join(" ", arg.Args);

            var target = name.IsNumeric() ? FindOnlinePlayerByID(Convert.ToUInt64(name)) : FindPlayerByName(name, player ?? null);
            if (target == null)
            {
                arg.ReplyWith($"Could not find player matching: {name}");
                return;
            }

            var entityList = BaseNetworkable.serverEntities.Where(x => pcdData.rEntity.ContainsKey(x.net.ID.Value) && pcdData.rEntity[x.net.ID.Value].ownerID == target.userID).ToList();

            Puts($"Deleting {entityList.Count} entities.");
            foreach (var entity in entityList.ToList())
            {
                pcdData.rEntity.Remove(entity.net.ID.Value);
                entity.KillMessage();
            }
            pcdData.pEntity.Remove(target.userID);            
            arg.ReplyWith(string.Format(lang.GetMessage("dnkillentitiesplayer", this), entityList?.Count, target.displayName));
        }

        #endregion

        #region API Stuff

        void OnUseNPC(BasePlayer npc, BasePlayer player)
        {
            if (config.market_npcs.Contains(npc.displayName) || config.market_npc_ids.Contains(npc.userID))
            {
                MenuBackPanel(player);
                SendNatureMenu(player);
            }
        }

        object CanGatherIngredient(BasePlayer player, uint source)
        {
            if (old_net_IDs.Contains(source) || pcdData.rEntity.ContainsKey(source)) return false;
            return null;
        }

        object CanGatherRune(BasePlayer player, ulong source)
        {
            if (old_net_IDs.Contains(source) || pcdData.rEntity.ContainsKey(source)) return false;
            return null;
        }

        object CanGainXP(BasePlayer player, BaseEntity source)
        {
            if (old_net_IDs.Contains(source.net.ID.Value) || pcdData.rEntity.ContainsKey(source.net.ID.Value)) return false;
            return null;
        }

        void OnZLevelDispenserGather(ResourceDispenser dispenser, BasePlayer player, Item item, int prevAmount, int newAmount)
        {
            if (old_net_IDs.Contains(dispenser.baseEntity.net.ID.Value) || pcdData.rEntity.ContainsKey(dispenser.baseEntity.net.ID.Value)) item.amount = prevAmount;
        }

        void OnZLevelCollectiblePickup(Item item, BasePlayer player, CollectibleEntity collectible, int prevAmount, int newAmount)
        {
            PrefabInfo pd;
            if (pcdData.rEntity.TryGetValue(collectible.net.ID.Value, out pd))
            {
                if (pd.prevent_gather) item.amount = prevAmount;
            }
        }

        object STCanGainXP(BasePlayer player, BaseEntity source)
        {
            PrefabInfo pi;
            if (source != null)
            {
                if (old_net_IDs.Contains(source.net.ID.Value)) return false;
                if (pcdData.rEntity.TryGetValue(source.net.ID.Value, out pi) && pi.prevent_gather) return false;
            }
            return null;
        }

        public bool IsDeployableNature(BaseEntity entity)
        {
            PrefabInfo pi;
            if (entity != null)
            {
                if (old_net_IDs.Contains(entity.net.ID.Value)) return true;
                if (pcdData.rEntity.TryGetValue(entity.net.ID.Value, out pi) && pi.prevent_gather) return true;
            }
            return false;
        }

        object STCanReceiveYield(BasePlayer player, BaseEntity source) => STCanGainXP(player, source);

        #endregion

        #region Nature Market

        Dictionary<int, List<ulong>> market_pages = new Dictionary<int, List<ulong>>();

        private void MenuBackPanel(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0.99" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" }
            }, "Overlay", "MenuBackPanel");

            CuiHelper.DestroyUi(player, "MenuBackPanel");
            CuiHelper.AddUi(player, container);
        }

        void SendNatureMenu(BasePlayer player, int page_number = 0)
        {
            if (market_pages.Count == 0) return;
            var keys = market_pages[page_number];

            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" }
            }, "Overlay", "NatureMarket");

            container.Add(new CuiElement
            {
                Name = "NatureMarket_Title",
                Parent = "NatureMarket",
                Components = {
                    new CuiTextComponent { Text = "Nature Shop", Font = "robotocondensed-bold.ttf", FontSize = 32, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-122.996 115.986", OffsetMax = "122.996 181.014" }
                }
            });

            ItemInfo itemData;

            var modifier = GetModifier(player.UserIDString);

            if (keys.Count > 0)
            {
                itemData = config.Prefabs[keys[0]];
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1333333 0.1333333 0.1333333 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-284.319 -2", OffsetMax = "-216.319 66" }
                }, "NatureMarket", "NatureMarket_img_panel_back_1");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.282353 0.282353 0.282353 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-32 -32", OffsetMax = "32 32" }
                }, "NatureMarket_img_panel_back_1", "NatureMarket_img_panel_front_1");

                container.Add(new CuiElement
                {
                    Name = "NatureMarket_img_1",
                    Parent = "NatureMarket_img_panel_back_1",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = (string)ImageLibrary?.Call("GetImage", keys[0].ToString()) },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-32 -32", OffsetMax = "32 32" }
                }
                });

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1320755 0.1320755 0.1320755 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "34 -34", OffsetMax = "184 -6" }
                }, "NatureMarket_img_panel_back_1", "NatureMarket_button_panel_back_1");

                container.Add(new CuiElement
                {
                    Name = "NatureMarket_item_title_1",
                    Parent = "NatureMarket_img_panel_back_1",
                    Components = {
                    new CuiTextComponent { Text = itemData.displayName.TitleCase(), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "40.84 -6", OffsetMax = "184 32" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "0.2830189 0.2830189 0.2830189 1", Command = $"naturemarketbuyitem 1 {keys[0]}" },
                    Text = { Text = "1", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "34 -32", OffsetMax = "82 -8" }
                }, "NatureMarket_img_panel_back_1", "NatureMarket_buy_1_button_1");

                container.Add(new CuiButton
                {
                    Button = { Color = "0.2830189 0.2830189 0.2830189 1", Command = $"naturemarketbuyitem 10 {keys[0]}" },
                    Text = { Text = "10", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "84 -32", OffsetMax = "132 -8" }
                }, "NatureMarket_img_panel_back_1", "NatureMarket_buy_2_button_1");

                container.Add(new CuiButton
                {
                    Button = { Color = "0.2830189 0.2830189 0.2830189 1", Command = $"naturemarketbuyitem 20 {keys[0]}" },
                    Text = { Text = "20", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "134 -32", OffsetMax = "182 -8" }
                }, "NatureMarket_img_panel_back_1", "NatureMarket_buy_3_button_1");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1333333 0.1333333 0.1333333 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "184 -34", OffsetMax = "234 -6" }
                }, "NatureMarket_img_panel_back_1", "NatureMarket_price_panel_1");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.6320754 0.4442004 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25 -12", OffsetMax = "23 12" }
                }, "NatureMarket_price_panel_1", "NatureMarket_price_panel_front_1");

                container.Add(new CuiElement
                {
                    Name = "NatureMarket_price_1",
                    Parent = "NatureMarket_price_panel_1",
                    Components = {
                    new CuiTextComponent { Text = $"${Math.Round(itemData.market_price * modifier, 1)}", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25 -12", OffsetMax = "23 12" }
                }
                });
            }

            if (keys.Count > 1)
            {
                itemData = config.Prefabs[keys[1]];
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1333333 0.1333333 0.1333333 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "66.381 0", OffsetMax = "134.381 68" }
                }, "NatureMarket", "NatureMarket_img_panel_back_2");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.282353 0.282353 0.282353 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-32 -32", OffsetMax = "32 32" }
                }, "NatureMarket_img_panel_back_2", "NatureMarket_img_panel_front_2");

                container.Add(new CuiElement
                {
                    Name = "NatureMarket_img_2",
                    Parent = "NatureMarket_img_panel_back_2",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = (string)ImageLibrary?.Call("GetImage", keys[1].ToString()) },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-32 -32", OffsetMax = "32 32" }
                }
                });

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1320755 0.1320755 0.1320755 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "34 -34", OffsetMax = "184 -6" }
                }, "NatureMarket_img_panel_back_2", "NatureMarket_button_panel_back_2");

                container.Add(new CuiElement
                {
                    Name = "NatureMarket_item_title_2",
                    Parent = "NatureMarket_img_panel_back_2",
                    Components = {
                    new CuiTextComponent { Text = itemData.displayName.TitleCase(), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "40.84 -6", OffsetMax = "184 32" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "0.2830189 0.2830189 0.2830189 1", Command = $"naturemarketbuyitem 1 {keys[1]}" },
                    Text = { Text = "1", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "34 -32", OffsetMax = "82 -8" }
                }, "NatureMarket_img_panel_back_2", "NatureMarket_buy_1_button_2");

                container.Add(new CuiButton
                {
                    Button = { Color = "0.2830189 0.2830189 0.2830189 1", Command = $"naturemarketbuyitem 10 {keys[1]}" },
                    Text = { Text = "10", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "84 -32", OffsetMax = "132 -8" }
                }, "NatureMarket_img_panel_back_2", "NatureMarket_buy_2_button_2");

                container.Add(new CuiButton
                {
                    Button = { Color = "0.2830189 0.2830189 0.2830189 1", Command = $"naturemarketbuyitem 20 {keys[1]}" },
                    Text = { Text = "20", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "134 -32", OffsetMax = "182 -8" }
                }, "NatureMarket_img_panel_back_2", "NatureMarket_buy_3_button_2");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1333333 0.1333333 0.1333333 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "184 -34", OffsetMax = "234 -6" }
                }, "NatureMarket_img_panel_back_2", "NatureMarket_price_panel_2");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.6320754 0.4442004 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25 -12", OffsetMax = "23 12" }
                }, "NatureMarket_price_panel_2", "NatureMarket_price_panel_front_2");

                container.Add(new CuiElement
                {
                    Name = "NatureMarket_price_2",
                    Parent = "NatureMarket_price_panel_2",
                    Components = {
                    new CuiTextComponent { Text = $"${Math.Round(itemData.market_price * modifier, 1)}", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25 -12", OffsetMax = "23 12" }
                }
                });
            }

            if (keys.Count > 2)
            {
                itemData = config.Prefabs[keys[2]];
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1333333 0.1333333 0.1333333 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-284.319 -90", OffsetMax = "-216.319 -22" }
                }, "NatureMarket", "NatureMarket_img_panel_back_3");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.282353 0.282353 0.282353 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-32 -32", OffsetMax = "32 32" }
                }, "NatureMarket_img_panel_back_3", "NatureMarket_img_panel_front_3");

                container.Add(new CuiElement
                {
                    Name = "NatureMarket_img_3",
                    Parent = "NatureMarket_img_panel_back_3",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = (string)ImageLibrary?.Call("GetImage", keys[2].ToString()) },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-32 -32", OffsetMax = "32 32" }
                }
                });

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1320755 0.1320755 0.1320755 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "34 -34", OffsetMax = "184 -6" }
                }, "NatureMarket_img_panel_back_3", "NatureMarket_button_panel_back_3");

                container.Add(new CuiElement
                {
                    Name = "NatureMarket_item_title_3",
                    Parent = "NatureMarket_img_panel_back_3",
                    Components = {
                    new CuiTextComponent { Text = itemData.displayName.TitleCase(), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "40.84 -6", OffsetMax = "184 32" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "0.2830189 0.2830189 0.2830189 1", Command = $"naturemarketbuyitem 1 {keys[2]}" },
                    Text = { Text = "1", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "34 -32", OffsetMax = "82 -8" }
                }, "NatureMarket_img_panel_back_3", "NatureMarket_buy_1_button_3");

                container.Add(new CuiButton
                {
                    Button = { Color = "0.2830189 0.2830189 0.2830189 1", Command = $"naturemarketbuyitem 10 {keys[2]}" },
                    Text = { Text = "10", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "84 -32", OffsetMax = "132 -8" }
                }, "NatureMarket_img_panel_back_3", "NatureMarket_buy_2_button_3");

                container.Add(new CuiButton
                {
                    Button = { Color = "0.2830189 0.2830189 0.2830189 1", Command = $"naturemarketbuyitem 20 {keys[2]}" },
                    Text = { Text = "20", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "134 -32", OffsetMax = "182 -8" }
                }, "NatureMarket_img_panel_back_3", "NatureMarket_buy_3_button_3");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1333333 0.1333333 0.1333333 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "184 -34", OffsetMax = "234 -6" }
                }, "NatureMarket_img_panel_back_3", "NatureMarket_price_panel_3");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.6320754 0.4442004 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25 -12", OffsetMax = "23 12" }
                }, "NatureMarket_price_panel_3", "NatureMarket_price_panel_front_3");

                container.Add(new CuiElement
                {
                    Name = "NatureMarket_price_3",
                    Parent = "NatureMarket_price_panel_3",
                    Components = {
                    new CuiTextComponent { Text = $"${Math.Round(itemData.market_price * modifier, 1)}", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25 -12", OffsetMax = "23 12" }
                }
                });
            }

            if (keys.Count > 3)
            {
                itemData = config.Prefabs[keys[3]];
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1333333 0.1333333 0.1333333 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "66.381 -90", OffsetMax = "134.381 -22" }
                }, "NatureMarket", "NatureMarket_img_panel_back_4");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.282353 0.282353 0.282353 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-32 -32", OffsetMax = "32 32" }
                }, "NatureMarket_img_panel_back_4", "NatureMarket_img_panel_front_4");

                container.Add(new CuiElement
                {
                    Name = "NatureMarket_img_4",
                    Parent = "NatureMarket_img_panel_back_4",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1",  Png = (string)ImageLibrary?.Call("GetImage", keys[3].ToString()) },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-32 -32", OffsetMax = "32 32" }
                }
                });

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1320755 0.1320755 0.1320755 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "34 -34", OffsetMax = "184 -6" }
                }, "NatureMarket_img_panel_back_4", "NatureMarket_button_panel_back_4");

                container.Add(new CuiElement
                {
                    Name = "NatureMarket_item_title_4",
                    Parent = "NatureMarket_img_panel_back_4",
                    Components = {
                    new CuiTextComponent { Text = itemData.displayName.TitleCase(), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "40.84 -6", OffsetMax = "184 32" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "0.2830189 0.2830189 0.2830189 1", Command = $"naturemarketbuyitem 1 {keys[3]}" },
                    Text = { Text = "1", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "34 -32", OffsetMax = "82 -8" }
                }, "NatureMarket_img_panel_back_4", "NatureMarket_buy_1_button_4");

                container.Add(new CuiButton
                {
                    Button = { Color = "0.2830189 0.2830189 0.2830189 1", Command = $"naturemarketbuyitem 10 {keys[3]}" },
                    Text = { Text = "10", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "84 -32", OffsetMax = "132 -8" }
                }, "NatureMarket_img_panel_back_4", "NatureMarket_buy_2_button_4");

                container.Add(new CuiButton
                {
                    Button = { Color = "0.2830189 0.2830189 0.2830189 1", Command = $"naturemarketbuyitem 20 {keys[3]}" },
                    Text = { Text = "20", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "134 -32", OffsetMax = "182 -8" }
                }, "NatureMarket_img_panel_back_4", "NatureMarket_buy_3_button_4");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1333333 0.1333333 0.1333333 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "184 -34", OffsetMax = "234 -6" }
                }, "NatureMarket_img_panel_back_4", "NatureMarket_price_panel_4");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.6320754 0.4442004 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25 -12", OffsetMax = "23 12" }
                }, "NatureMarket_price_panel_4", "NatureMarket_price_panel_front_4");

                container.Add(new CuiElement
                {
                    Name = "NatureMarket_price_4",
                    Parent = "NatureMarket_price_panel_4",
                    Components = {
                    new CuiTextComponent { Text = $"${Math.Round(itemData.market_price * modifier, 1)}", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25 -12", OffsetMax = "23 12" }
                }
                });
            }

            if (keys.Count > 4)
            {
                itemData = config.Prefabs[keys[4]];
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1333333 0.1333333 0.1333333 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-284.319 -178", OffsetMax = "-216.319 -110" }
                }, "NatureMarket", "NatureMarket_img_panel_back_5");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.282353 0.282353 0.282353 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-32 -32", OffsetMax = "32 32" }
                }, "NatureMarket_img_panel_back_5", "NatureMarket_img_panel_front_5");

                container.Add(new CuiElement
                {
                    Name = "NatureMarket_img_5",
                    Parent = "NatureMarket_img_panel_back_5",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = (string)ImageLibrary?.Call("GetImage", keys[4].ToString()) },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-32 -32", OffsetMax = "32 32" }
                }
                });

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1320755 0.1320755 0.1320755 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "34 -34", OffsetMax = "184 -6" }
                }, "NatureMarket_img_panel_back_5", "NatureMarket_button_panel_back_5");

                container.Add(new CuiElement
                {
                    Name = "NatureMarket_item_title_5",
                    Parent = "NatureMarket_img_panel_back_5",
                    Components = {
                    new CuiTextComponent { Text = itemData.displayName.TitleCase(), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "40.84 -6", OffsetMax = "184 32" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "0.2830189 0.2830189 0.2830189 1", Command = $"naturemarketbuyitem 1 {keys[4]}" },
                    Text = { Text = "1", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "34 -32", OffsetMax = "82 -8" }
                }, "NatureMarket_img_panel_back_5", "NatureMarket_buy_1_button_5");

                container.Add(new CuiButton
                {
                    Button = { Color = "0.2830189 0.2830189 0.2830189 1", Command = $"naturemarketbuyitem 10 {keys[4]}" },
                    Text = { Text = "10", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "84 -32", OffsetMax = "132 -8" }
                }, "NatureMarket_img_panel_back_5", "NatureMarket_buy_2_button_5");

                container.Add(new CuiButton
                {
                    Button = { Color = "0.2830189 0.2830189 0.2830189 1", Command = $"naturemarketbuyitem 20 {keys[4]}" },
                    Text = { Text = "20", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "134 -32", OffsetMax = "182 -8" }
                }, "NatureMarket_img_panel_back_5", "NatureMarket_buy_3_button_5");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1333333 0.1333333 0.1333333 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "184 -34", OffsetMax = "234 -6" }
                }, "NatureMarket_img_panel_back_5", "NatureMarket_price_panel_5");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.6320754 0.4442004 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25 -12", OffsetMax = "23 12" }
                }, "NatureMarket_price_panel_5", "NatureMarket_price_panel_front_5");

                container.Add(new CuiElement
                {
                    Name = "NatureMarket_price_5",
                    Parent = "NatureMarket_price_panel_5",
                    Components = {
                    new CuiTextComponent { Text = $"${Math.Round(itemData.market_price * modifier, 1)}", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25 -12", OffsetMax = "23 12" }
                }
                });
            }

            if (keys.Count > 5)
            {
                itemData = config.Prefabs[keys[5]];
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1333333 0.1333333 0.1333333 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "66.381 -178", OffsetMax = "134.381 -110" }
                }, "NatureMarket", "NatureMarket_img_panel_back_6");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.282353 0.282353 0.282353 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-32 -32", OffsetMax = "32 32" }
                }, "NatureMarket_img_panel_back_6", "NatureMarket_img_panel_front_6");

                container.Add(new CuiElement
                {
                    Name = "NatureMarket_img_6",
                    Parent = "NatureMarket_img_panel_back_6",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = (string)ImageLibrary?.Call("GetImage", keys[5].ToString()) },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-32 -32", OffsetMax = "32 32" }
                }
                });

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1320755 0.1320755 0.1320755 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "34 -34", OffsetMax = "184 -6" }
                }, "NatureMarket_img_panel_back_6", "NatureMarket_button_panel_back_6");

                container.Add(new CuiElement
                {
                    Name = "NatureMarket_item_title_6",
                    Parent = "NatureMarket_img_panel_back_6",
                    Components = {
                    new CuiTextComponent { Text = itemData.displayName.TitleCase(), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "40.84 -6", OffsetMax = "184 32" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "0.2830189 0.2830189 0.2830189 1", Command = $"naturemarketbuyitem 1 {keys[5]}" },
                    Text = { Text = "1", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "34 -32", OffsetMax = "82 -8" }
                }, "NatureMarket_img_panel_back_6", "NatureMarket_buy_1_button_6");

                container.Add(new CuiButton
                {
                    Button = { Color = "0.2830189 0.2830189 0.2830189 1", Command = $"naturemarketbuyitem 10 {keys[5]}" },
                    Text = { Text = "10", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "84 -32", OffsetMax = "132 -8" }
                }, "NatureMarket_img_panel_back_6", "NatureMarket_buy_2_button_6");

                container.Add(new CuiButton
                {
                    Button = { Color = "0.2830189 0.2830189 0.2830189 1", Command = $"naturemarketbuyitem 20 {keys[5]}" },
                    Text = { Text = "20", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "134 -32", OffsetMax = "182 -8" }
                }, "NatureMarket_img_panel_back_6", "NatureMarket_buy_3_button_6");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1333333 0.1333333 0.1333333 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "184 -34", OffsetMax = "234 -6" }
                }, "NatureMarket_img_panel_back_6", "NatureMarket_price_panel_6");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.6320754 0.4442004 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25 -12", OffsetMax = "23 12" }
                }, "NatureMarket_price_panel_6", "NatureMarket_price_panel_front_6");

                container.Add(new CuiElement
                {
                    Name = "NatureMarket_price_6",
                    Parent = "NatureMarket_price_panel_6",
                    Components = {
                    new CuiTextComponent { Text = $"${Math.Round(itemData.market_price * modifier, 1)}", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25 -12", OffsetMax = "23 12" }
                }
                });
            }               
            
            if (market_pages.ContainsKey(page_number + 1))
            {
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1333333 0.1333333 0.1333333 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "66.381 -218.8", OffsetMax = "114.381 -194.8" }
                }, "NatureMarket", "NatureMarket_next_panel");

                container.Add(new CuiButton
                {
                    Button = { Color = "0.282353 0.282353 0.282353 1", Command = $"naturemarketpage {page_number + 1}" },
                    Text = { Text = "NEXT", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-22 -10", OffsetMax = "22 10" }
                }, "NatureMarket_next_panel", "NatureMarket_next_button");                
            }

            if (market_pages.ContainsKey(page_number - 1))
            {
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1333333 0.1333333 0.1333333 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-115.2 -218.8", OffsetMax = "-67.2 -194.8" }
                }, "NatureMarket", "NatureMarket_back_panel");

                container.Add(new CuiButton
                {
                    Button = { Color = "0.282353 0.282353 0.282353 1", Command = $"naturemarketpage {page_number - 1}" },
                    Text = { Text = "BACK", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-22 -10", OffsetMax = "22 10" }
                }, "NatureMarket_back_panel", "NatureMarket_back_button");
            }

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1333333 0.1333333 0.1333333 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-24 -218.8", OffsetMax = "24 -194.8" }
            }, "NatureMarket", "NatureMarket_close_panel");

            container.Add(new CuiButton
            {
                Button = { Color = "0.282353 0.282353 0.282353 1", Command = $"naturemarketclose" },
                Text = { Text = "CLOSE", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-22 -10", OffsetMax = "22 10" }
            }, "NatureMarket_close_panel", "NatureMarket_close_button");

            CuiHelper.DestroyUi(player, "NatureMarket");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("naturemarketclose")]
        void CloseNatureMarket(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, "NatureMarket");
            CuiHelper.DestroyUi(player, "MenuBackPanel");
        }

        [ConsoleCommand("naturemarketpage")]
        void SendMarketPage(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            SendNatureMenu(player, Convert.ToInt32(arg.Args[0]));
        }

        [ConsoleCommand("naturemarketbuyitem")]
        void BuyMarketItem(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            if (market_cooldown.TryGetValue(player.userID, out markettimerData))
            {
                if (!markettimerData.warned)
                {
                    PrintToChat(player, string.Format(lang.GetMessage("WarnMarketCooldown", this, player.UserIDString), Prefix));
                    markettimerData.warned = true;
                }
                return;
            }
            
            var quantity = Convert.ToInt32(arg.Args[0]);
            var key = Convert.ToUInt64(arg.Args[1]);
            ItemInfo itemData;
            if (!config.Prefabs.TryGetValue(key, out itemData)) return;

            var cost = itemData.market_price;
            var modifier = GetModifier(player.UserIDString);

            cost = cost * modifier;

            if (!permission.UserHasPermission(player.UserIDString, "deployablenature.free") && modifier > 0)
            {
                switch (config.currency.ToUpper())
                {                  
                    case "ECONOMICS":
                        var playerBalance = Convert.ToDouble(Economics?.Call("Balance", player.userID));
                        if (playerBalance < Math.Round(cost * quantity, 1))
                        {
                            PrintToChat(player, string.Format(lang.GetMessage("NEC", this), Prefix, config.currency.TitleCase(), quantity, itemData.displayName));
                            return;
                        }
                        if (!Convert.ToBoolean(Economics?.Call("Withdraw", player.userID, Math.Round(cost * quantity, 1)))) return;
                        break;

                    case "SR":
                        var balance = Convert.ToInt32(ServerRewards?.Call("CheckPoints", player.userID));
                        if (balance < itemData.market_price * quantity)
                        {
                            PrintToChat(player, string.Format(lang.GetMessage("NEC", this), Prefix, config.currency.TitleCase(), quantity, itemData.displayName));
                            return;
                        }
                        if (!Convert.ToBoolean(ServerRewards?.Call("TakePoints", player.userID, Convert.ToInt32(cost) * quantity))) return;
                        break;

                    case "CUSTOM":
                        if (!PaidWithItem(player, quantity, Convert.ToInt32(cost), config.custom_currency.shortname, config.custom_currency.skinID))
                        {
                            PrintToChat(player, string.Format(lang.GetMessage("NEC", this), Prefix, config.custom_currency.name.TitleCase(), quantity, itemData.displayName));
                            return;
                        }
                        break;
                    default:
                        if (!PaidWithItem(player, quantity, Convert.ToInt32(cost), "scrap"))
                        {
                            PrintToChat(player, string.Format(lang.GetMessage("NEC", this), Prefix, config.currency.TitleCase(), quantity, itemData.displayName));
                            return;
                        }
                        break;
                }
            }            
            AddCooldown(player);
            GiveItem(player, key, quantity);
        }

        [PluginReference]
        private Plugin Economics, ServerRewards, ImageLibrary, VendingUI;

        bool PaidWithItem(BasePlayer player, int quantity, int cost, string shortname, ulong skin = 0)
        {
            var found = 0;
            var totalCost = quantity * cost;
            foreach (var item in player.inventory.AllItems())
            {
                if (item.info.shortname == shortname && item.skin == skin) found += item.amount;
                if (found >= totalCost) break;
            }
            if (found < totalCost) return false;
            found = 0;
            foreach (var item in player.inventory.AllItems())
            {
                if (item.info.shortname == shortname && item.skin == skin)
                {
                    if (found >= totalCost) break;
                    if (item.amount > totalCost - found)
                    {
                        item.UseItem(totalCost - found);
                        break;
                    }
                    else
                    {
                        found += item.amount;
                        item.UseItem(item.amount);
                    }                        
                }
            }
            return true;
        }

        Dictionary<ulong, MarketTimerInfo> market_cooldown = new Dictionary<ulong, MarketTimerInfo>();

        MarketTimerInfo markettimerData;

        void AddCooldown(BasePlayer player)
        {
            if (market_cooldown.TryGetValue(player.userID, out markettimerData))
            {
                if (markettimerData._timer != null && !markettimerData._timer.Destroyed) markettimerData._timer.Destroy();
                market_cooldown.Remove(player.userID);
            }
            ulong id = player.userID;
            market_cooldown.Add(id, new MarketTimerInfo()
            {
                _timer = timer.Once(1f, () =>
                {
                    if (market_cooldown.TryGetValue(id, out markettimerData))
                    {
                        if (markettimerData._timer != null && !markettimerData._timer.Destroyed) markettimerData._timer.Destroy();
                        market_cooldown.Remove(id);
                    }
                })
            });

        }

        public class MarketTimerInfo
        {
            public Timer _timer;
            public bool warned;
        }

        #endregion

        #region Vending UI Integration

        void OnVendingUILoaded()
        {
            List<object[]> objects = Pool.GetList<object[]>();
            foreach (var deployable in config.Prefabs)
            {
                objects.Add(new object[] { deployable.Key, deployable.Value.img_url, deployable.Key.ToString() });
            }
            VendingUI?.Call("LoadExternalImages", objects);
            Pool.FreeList(ref objects);
        }

        #endregion
    }
}
