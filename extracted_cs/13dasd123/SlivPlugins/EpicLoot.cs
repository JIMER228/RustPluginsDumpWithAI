// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿// Reference: 0Harmony
using CompanionServer.Handlers;
using Facepunch;
using Facepunch.Models;
#if CARBON
using HarmonyLib;
#else
using Harmony;
#endif
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;


/* 1.1.14
 * Fixed an issue with CanRecycle throwing an error.
 * Fixed an exception caused by a null item when generating new random items.
 * Updated the GenerateItem method to edit the existing item if using EpicScrap to modify an item.
 */

namespace Oxide.Plugins
{
    [Info("Epic Loot", "imthenewguy", "1.1.14")]
    [Description("Man this loot is epic!")]
    class EpicLoot : RustPlugin
    {
        #region Config

        private Configuration config;
        public class Configuration
        {
            [JsonProperty("General Settings")]
            public GeneralSettings general_settings = new GeneralSettings();

            [JsonProperty("Crafting info")]
            public CraftingChance crafting_info = new CraftingChance();

            [JsonProperty("Button information")]
            public ButtonInfo button_info = new ButtonInfo();

            [JsonProperty("PVP critical damage information")]
            public PVPCrit pvp_crit_damage = new PVPCrit();

            [JsonProperty("Horse information")]
            public HorseStats horse_info = new HorseStats();

            [JsonProperty("Scrap information for Scavangers buff")]
            public Scavengers scavanger_info = new Scavengers();

            [JsonProperty("Raiders information")]
            public Raiders raiders_info = new Raiders();

            [JsonProperty("Tier information")]
            public TierSettings tier_information = new TierSettings();

            [JsonProperty("Loot crate info")]
            public LootInfo containers = new LootInfo();

            [JsonProperty("Corpse loot info")]
            public CorpseLootInfo corpses = new CorpseLootInfo();

            [JsonProperty("List of items that can not be enhanced")]
            public List<string> global_blacklist = new List<string>();

            [JsonProperty("List of skin IDs that cannot be enhanced")]
            public List<ulong> skin_blacklist = new List<ulong>();

            [JsonProperty("A black list of items that will not be refunded when using the assembler buff", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> craft_refund_blacklist = new List<string>() { "gunpowder", "explosives", "sulfur" };

            [JsonProperty("A black list of items that will not be duplicated when using the fabricators buff", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> fabricator_blacklist = new List<string>() { "gunpowder", "explosives", "sulfur" };

            [JsonProperty("A black list of items that will be ignored by the reinforced buff", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> reinforced_blacklist = new List<string>() { "keycard_green", "keycard_blue", "keycard_red" };

            [JsonProperty("Healthshot weapon modifiers. Modifies the set bonus value for selected weapons. [1.0 is full dmage]")]
            public Dictionary<string, float> healthshot_override = new Dictionary<string, float>();

            [JsonProperty("Attractive set bonus settings")]
            public LootPickup attractive_settings = new LootPickup();

            [JsonProperty("Mission Rewards")]
            public MissionConfig mission_rewards;

            [JsonProperty("Drop tables for MinersLuck, WoodcuttersLuck and SkinnersLuck set bonuses")]
            public Dictionary<SetBonus, DropTableLuck> luck_drop_table = new Dictionary<SetBonus, DropTableLuck>();

            [JsonProperty("Enhancement information")]
            public Dictionary<Buff, EnhancementInfo> enhancements = new Dictionary<Buff, EnhancementInfo>();

            [JsonProperty("List of skins for item types. If more than 1 skin is added, a random one will be selected when the item is created.")]
            public Dictionary<string, List<ulong>> item_skins = new Dictionary<string, List<ulong>>();

            [JsonProperty("RandomTrader Integration")]
            public RandomTraderAPI random_trader_api = new RandomTraderAPI();
            
            [JsonProperty("FishingContest Integration")]
            public FishingContestLoot fishing_contest_api = new FishingContestLoot();
            
            [JsonProperty("Scrapper settings")]
            public ScrapperSettings scrapper_settings = new ScrapperSettings();

            [JsonProperty("TruePVE settings")]
            public TruePVESettings truepve_settings = new TruePVESettings();

            [JsonProperty("RaidableBases settings")]
            public RaidableBaseSettings raidablebases_settings = new RaidableBaseSettings();

            [JsonProperty("Effect settings")]
            public EffectSettings effect_settings = new EffectSettings();

            [JsonProperty("BossMonster settings")]
            public BossMonsterSettings bossmonster_settings = new BossMonsterSettings();

            [JsonProperty("Notification settings")]
            public NotificationSettings notificationSettings = new NotificationSettings();

            [JsonProperty("SkillTree settings")]
            public SkillTreeInfo skilltree_settings = new SkillTreeInfo();

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }
        
        public class NotificationSettings
        {
            [JsonProperty("UINotify plugin settings")]
            public NotifySettings notify = new NotifySettings();
        }

        //UINotify
        public class NotifySettings
        {
            [JsonProperty("Send player specific messages using the notify plugin")]
            public bool enabled = true;

            [JsonProperty("Message type")]
            public int messageType = 0;
        }

        public class EffectSettings
        {
            [JsonProperty("Effect when a player gets a successful dodge. Set to null to disable.")]
            public string UncannyDodge_effect = "assets/prefabs/building/wall.frame.fence/effects/chain-link-impact.prefab";
        }

        public class FishingContestLoot
        {
            [JsonProperty("A list of item shortnames that will be added to the fishing contest as prizes")]
            public List<string> shortnames = new List<string>();

            [JsonProperty("A list of buff types that will be added to the fishing contest items as prizes")]
            public List<Buff> buffs = new List<Buff>();

            [JsonProperty("Send payload? (set to true if you want more EpicLoot items added to the FishingContest loot table)")]
            public bool send_payload = true;
        }

        public class LootInfo
        {
            [JsonProperty("Should we use the default item loot table for the below containers?")]
            public bool use_default_loot_table = true;

            [JsonProperty("Containers and their chance for an enhanced item to spawn")]
            public Dictionary<string, float> containers = new Dictionary<string, float>();

            [JsonProperty("Barrels and their chance for an enhanced item to spawn")]
            public Dictionary<string, float> barrels = new Dictionary<string, float>();
        }

        public class CorpseLootInfo
        {
            [JsonProperty("Corpse types and their chance for an enhanced item to spawn")]
            public Dictionary<string, float> corpses = new Dictionary<string, float>();
        }

        public class TruePVESettings
        {
            [JsonProperty("Allow the healthshot set bonus to work in pve [requires TruePVE]?")]
            public bool allow_healthshot_in_pve = true;
        }

        public class ScrapperSettings
        {
            [JsonProperty("Enable scrapping of equipment for a special currency that can be used to enhanced weapons?")]
            public bool enabled = true;

            [JsonProperty("Allow players to recycle EpicLoot items in the recycler (no components will be received, only EpicScrap)?")]
            public bool allow_recyler = true;

            [JsonProperty("Name of the scrapper currency")]
            public string currency_name = "epic scrap";

            [JsonProperty("Shortname of the item the currency will be based off of")]
            public string currency_shortname = "blood";

            [JsonProperty("Currency skin")]
            public ulong currency_skin = 2834920066;

            [JsonProperty("Currency received for scrapping items based on tier")]
            public Dictionary<string, int> scrapper_value = new Dictionary<string, int>();

            [JsonProperty("Cost to enhance an item based on the buff type")]
            public Dictionary<Buff, int> enhancement_cost = new Dictionary<Buff, int>();
        }

        public class ButtonInfo
        {
            [JsonProperty("Enable use of the HUD button? Players can still disable it client side via the player settings")]
            public bool enable_button = true;

            [JsonProperty("Path of the icon button. You can use a SkinID, URL or assets/icons path")]
            public string icon_path = "assets/icons/inventory.png";

            [JsonProperty("Offset Min")]
            public string offsetMin = "188.6 15.7";

            [JsonProperty("Offset Max")]
            public string offsetMax = "224.6 51.69";            
        }

        public class MissionConfig
        {
            public bool enabled = true;
            public Dictionary<string, MissionInfo> mission_rewards;

            public MissionConfig(bool enabled, Dictionary<string, MissionInfo> mission_rewards)
            {
                this.enabled = enabled;
                this.mission_rewards = mission_rewards;
            }

            [JsonConstructor]
            public MissionConfig()
            {

            }
        }

        public class MissionInfo
        {
            [JsonProperty("Chance after completing a mission to obtain a reward [%]")]
            public float reward_chance = 25f;

            [JsonProperty("Set types that can be given as a reward, and their respective chance weights")]
            public Dictionary<string, int> rewards = new Dictionary<string, int>();

            public MissionInfo(float reward_chance, Dictionary<string, int> rewards)
            {
                this.reward_chance = reward_chance;
                this.rewards = rewards;
            }
        }

        public class RandomTraderAPI
        {
            [JsonProperty("CopyPaste file name to use")]
            public string copypaste_file_name = "EnhancedStore";

            [JsonProperty("Shop name")]
            public string shop_name = "Enhanced Item Shop";

            [JsonProperty("Shop purchase limit")]
            public int shop_purchase_limit = 2;

            [JsonProperty("How many random items from the list will be picked?")]
            public int shop_display_amount = 8;

            [JsonProperty("Minimum quantity of an item that the shop will stock")]
            public int min_stock_quantity = 1;

            [JsonProperty("Maximum quantity of an item that the shop will stock")]
            public int max_stock_quantity = 5;

            [JsonProperty("Minimum cost of an item")]
            public int min_cost = 100;

            [JsonProperty("Maximum cost of an item")]
            public int max_cost = 1000;

            [JsonProperty("Skin the items?")]
            public bool skin_item = true;

            [JsonProperty("How many rolls of each enhancement should we add?")]
            public int quantity_of_enhancements = 10;
        }


        public class CraftingChance
        {
            [JsonProperty("Should players have a chance of their crafted items becoming enhanced?")]
            public bool enabled = true;

            [JsonProperty("What % chance each craft should they have for their item to be enhanced?")]
            public float chance = 1.0f;

            [JsonProperty("List of craftable items that cannot be enhanced")]
            public List<string> black_list = new List<string>();

        }

        public class LootPickup
        {
            [JsonProperty("Should the Attractive set bonus only trigger with melee weapons?")]
            public bool MeleeOnly = false;

            [JsonProperty("Maximum distance that the player can be for Attractive set bonus to trigger. 0 = no distance limit.")]
            public float MaxDistance = 0f;
        }

        public class PVPCrit
        {
            [JsonProperty("Minimum crit damage multiplier")]
            public float min_crit_modifier = 0.1f;

            [JsonProperty("Maximum crit damage multiplier")]
            public float max_crit_modifier = 0.3f;
        }

        public class GeneralSettings
        {
            [JsonProperty("Automatically add missing sets from the default sets?")]
            public bool auto_update_sets = true;

            [JsonProperty("Automatically add missing set bonuses from the default sets?")]
            public bool auto_update_set_bonuses = true;

            [JsonProperty("Maximum distance that the TeamHeal setbonus will share healing with")]
            public float TeamHealRadius = 5f;

            [JsonProperty("Message players about how EpicLoot works when they find their first piece (resets on plugin restart)?")]
            public bool message_on_first_loot = true;

            [JsonProperty("Prevent EpicLoot items from being reskinned?")]
            public bool prevent_reskin = false;

            [JsonProperty("Prevent EpicLoot items from losing max condition?")]
            public bool prevent_max_condition_loss = false;

            [JsonProperty("Chat commands to open the Inspector")]
            public List<string> ui_commands = new List<string>();

            [JsonProperty("Permission based chance multiplier for finding EpicLoot in crates [1.0 = normal chance. 2.0 = twice the chance]")]
            public Dictionary<string, float> perm_chance_modifier_loot = new Dictionary<string, float>();

            [JsonProperty("Permission based chance multiplier for creating an EpicLoot item while crafting [1.0 = normal chance. 2.0 = twice the chance]")]
            public Dictionary<string, float> perm_chance_modifier_craft = new Dictionary<string, float>();
        }

        public class TierSettings
        {
            [JsonProperty("Tier display names that show up in the menu")]
            public Dictionary<string, string> tier_display_names = new Dictionary<string, string>()
            {
                ["s"] = "Legendary",
                ["a"] = "Epic",
                ["b"] = "Rare",
                ["c"] = "Uncommon"
            };

            [JsonProperty("Tier colours")]
            public Dictionary<string, string> tier_colours = new Dictionary<string, string>()
            {
                ["s"] = "#FFF900",
                ["a"] = "#9500BA",
                ["b"] = "#077E93",
                ["c"] = "#AE5403"
            };

        }

        public class HorseStats
        {
            [JsonProperty("Adjust run speed?")]
            public bool Adjust_Run_Speed = true;

            [JsonProperty("Adjust trot speed?")]
            public bool Adjust_Trot_Speed = true;

            [JsonProperty("Adjust walk speed?")]
            public bool Adjust_Walk_Speed = true;

            [JsonProperty("Adjust turn speed?")]
            public bool Adjust_Turn_Speed = true;
        }

        public class Scavengers
        {
            [JsonProperty("Minimum extra scrap that a player should receive when the Scavengers buff triggers")]
            public int Min_extra_scrap = 1;

            [JsonProperty("Maximum extra scrap that a player should receive when the Scavengers buff triggers")]
            public int Max_extra_scrap = 3;
        }

        public class Raiders
        {
            [JsonProperty("Blacklist of items that will not trigger the raiders buff")]
            public List<string> blacklist = new List<string>();
        }

        public class DropTableLuck
        {
            [JsonProperty("Drop table")]
            public List<DropTable> dropTable;

            public DropTableLuck(List<DropTable> dropTable)
            {
                this.dropTable = dropTable;
            }
        }

        public enum GatherType
        {
            Mine,
            Chop,
            Skin,
            Fish
        }

        public class DropTable
        {
            public string shortname;
            public ulong skin;
            public int min_quantity;
            public int max_quantity;
            public string name;
            public int drop_weight;
            public DropTable(string shortname, int min_quantity, int max_quantity, int drop_weight, ulong skin = 0, string name = null)
            {
                this.shortname = shortname;
                this.min_quantity = min_quantity;
                this.max_quantity = max_quantity;
                this.skin = skin;
                this.name = name;
                this.drop_weight = drop_weight;
            }
        }

        public class SkillTreeInfo
        {
            [JsonProperty("Set to true if you want to add the Epic Loot buff to the skill tree")]
            public bool enabled = false;

            [JsonProperty("Maximum level for the loot chance buff")]
            public int max_level = 5;

            [JsonProperty("Chance increase per level for the loot chance buff [0.05 = 5%]")]
            public float increase_per_level = 0.05f;

            [JsonProperty("Allow the MaxRepair buff from SkillTree to repair EpicLoot items to max condition?")]
            public bool allow_MaxRepair_buff_on_EL_items = false;
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
            config.enhancements = Default_Tier_Values;
            config.global_blacklist = new List<string>() { "geiger.counter", "flare", "grenade.smoke", "explosive.timed", "rf.detonator", "tool.camera", "skull" };
            config.containers = DefaultProfile;
            config.luck_drop_table = Default_tables;
            config.mission_rewards = Default_Mission_Rewards;
            config.corpses = DefaultCorpseProfile;
            config.fishing_contest_api.shortnames = DefaultFishingContestShortnames;
            config.fishing_contest_api.buffs = DefaultFishingContestBuffs;
            config.scrapper_settings.scrapper_value = DefaultScrapperTierValue;
            config.scrapper_settings.enhancement_cost = DefaultEnhancementCost();
            config.general_settings.ui_commands = new List<string>() { "el", "elinspect" };
            config.raidablebases_settings = new RaidableBaseSettings()
            {
                enabled = true,
                base_settings = DefaultRaidables,
                container_whitelist = DefaultRaidContainerWhitelist
            };
            config.healthshot_override = DefaultHealthShot;

            config.general_settings.perm_chance_modifier_craft.Add("epicloot.vip", 1);
            config.general_settings.perm_chance_modifier_loot.Add("epicloot.vip", 1);
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

        #region Default settings

        Dictionary<string, float> DefaultHealthShot
        {
            get
            {
                return new Dictionary<string, float>()
                {
                    ["lmg.m249"] = 0.2f,
                    ["hmlmg"] = 0.2f
                };
            }
        }

        Dictionary<int, RaidableBaseLootSettings> DefaultRaidables
        {
            get
            {
                return new Dictionary<int, RaidableBaseLootSettings>()
                {
                    [0] = new RaidableBaseLootSettings(1, 2),
                    [1] = new RaidableBaseLootSettings(2, 4),
                    [2] = new RaidableBaseLootSettings(3, 6),
                    [3] = new RaidableBaseLootSettings(5, 10),
                    [4] = new RaidableBaseLootSettings(8, 14)
                };
            }
        }

        public List<string> DefaultRaidContainerWhitelist
        {
            get
            {
                return new List<string>()
                {
                    "coffinstorage",
                    "box.wooden.large",
                    "dropbox.deployed",
                    "woodbox_deployed"
                };
            }
        }

        List<Buff> DefaultFishingContestBuffs
        {
            get
            {
                return new List<Buff>()
                {
                    Buff.Fishermans
                };
            }
        }

        List<string> DefaultFishingContestShortnames
        {
            get
            {
                return new List<string>()
                {
                    "hoodie",
                    "pants",
                    "attire.hide.boots",
                    "shoes.boots",
                    "hat.beenie",
                    "twitchsunglasses",
                    "sunglasses02black",
                    "santabeard",
                    "burlap.gloves.new",
                    "burlap.gloves",
                    "tactical.gloves",
                    "wood.armor.jacket",
                    "jacket",
                    "wood.armor.pants",
                    "fishingrod.handmade"
                };
            }
        }

        LootInfo DefaultProfile = new LootInfo()
        {
            containers = Default_Crate_Spawns,
            use_default_loot_table = true,
            barrels = Default_Barrel_Spawns
        };        

        CorpseLootInfo DefaultCorpseProfile = new CorpseLootInfo()
        {
            corpses = Default_Corpse_Spawns
        };

        Dictionary<Buff, int> DefaultEnhancementCost()
        {
            var result = new Dictionary<Buff, int>();

            List<Buff> buffs = Pool.GetList<Buff>();
            buffs.AddRange(Enum.GetValues(typeof(Buff)).Cast<Buff>().Intersect(config.enhancements.Keys));

            foreach (var buff in buffs)
            {
                if (!result.ContainsKey(buff)) result.Add(buff, 100);
            }

            Pool.FreeList(ref buffs);

            return result;
        }

        Dictionary<string, int> DefaultScrapperTierValue
        {
            get
            {
                return new Dictionary<string, int>()
                {
                    ["s"] = 10,
                    ["a"] = 8,
                    ["b"] = 6,
                    ["c"] = 4
                };
            }
        }

        public MissionConfig Default_Mission_Rewards
        {
            get
            {
                return new MissionConfig(true, new Dictionary<string, MissionInfo>()
                {
                    ["missionprovider_fishing_a"] = new MissionInfo(25f, new Dictionary<string, int>()
                    {
                        ["Fishermans"] = 100,
                        ["Scavengers"] = 25,
                        ["Foragers"] = 25
                    }),
                    ["missionprovider_fishing_b"] = new MissionInfo(25f, new Dictionary<string, int>()
                    {
                        ["Fishermans"] = 100,
                        ["Scavengers"] = 25,
                        ["Foragers"] = 25
                    }),
                    ["missionprovider_bandit_a"] = new MissionInfo(25f, new Dictionary<string, int>()
                    {
                        ["Lumberjacks"] = 100,
                        ["Operators"] = 25,
                        ["Transporters"] = 25
                    }),
                    ["missionprovider_bandit_b"] = new MissionInfo(25f, new Dictionary<string, int>()
                    {
                        ["Lumberjacks"] = 100,
                        ["Operators"] = 25,
                        ["Transporters"] = 25
                    }),
                    ["missionprovider_outpost_a"] = new MissionInfo(25f, new Dictionary<string, int>()
                    {
                        ["Miners"] = 100,
                        ["Assassins"] = 25,
                        ["Reinforced"] = 25
                    }),
                    ["missionprovider_outpost_b"] = new MissionInfo(25f, new Dictionary<string, int>()
                    {
                        ["Miners"] = 100,
                        ["Assassins"] = 25,
                        ["Reinforced"] = 25
                    }),
                    ["missionprovider_stables_a"] = new MissionInfo(25f, new Dictionary<string, int>()
                    {
                        ["Jockeys"] = 100,
                        ["Builders"] = 25,
                        ["Skinners"] = 25
                    })
                });
            }
        }

        public static Dictionary<string, float> Default_Crate_Spawns
        {
            get
            {
                return new Dictionary<string, float>()
                {
                    ["crate_normal_2"] = 1f,
                    ["crate_normal"] = 3f,
                    ["crate_elite"] = 15f,
                    ["crate_underwater_basic"] = 6f,
                    ["crate_underwater_advanced"] = 12f,
                    ["heli_crate"] = 10f,
                    ["bradley_crate"] = 10f,
                    ["codelockedhackablecrate"] = 10f,
                    ["codelockedhackablecrate_oilrig"] = 10f,
                    ["crate_tools"] = 1.5f
                };
            }
        }

        public static Dictionary<string, float> Default_Barrel_Spawns
        {
            get
            {
                return new Dictionary<string, float>()
                {
                    ["loot-barrel-1"] = 1f,
                    ["loot_barrel_1"] = 1f,
                    ["loot-barrel-2"] = 2f,
                    ["loot_barrel_2"] = 2f,
                };
            }
        }

        public static Dictionary<string, float> Default_Corpse_Spawns
        {
            get
            {
                return new Dictionary<string, float>()
                {
                    ["scientistnpc_heavy"] = 3f,
                    ["scientistnpc_cargo"] = 1f,
                    ["scientistnpc_cargo_turret_any"] = 1f,
                    ["scientistnpc_cargo_turret_lr300"] = 1f,
                    ["scientistnpc_ch47_gunner"] = 1f,
                    ["scientistnpc_excavator"] = 1f,
                    ["scientistnpc_full_any"] = 1f,
                    ["scientistnpc_full_lr300"] = 1f,
                    ["scientistnpc_full_mp5"] = 1f,
                    ["scientistnpc_full_pistol"] = 1f,
                    ["scientistnpc_full_shotgun"] = 1f,
                    ["scientistnpc_junkpile_pistol"] = 1f,
                    ["scientistnpc_oilrig"] = 1f,
                    ["scientistnpc_patrol"] = 1f,
                    ["scientistnpc_peacekeeper"] = 1f,
                    ["scientistnpc_roam"] = 1f,
                    ["scientistnpc_roamtethered"] = 1f
                };
            }
        }

        Dictionary<string, List<ulong>> DefaultItemSkins()
        {
            var result = new Dictionary<string, List<ulong>>();
            System.Random rnd = new System.Random();

            foreach (var skin in Rust.Workshop.Approved.All.Values)
            {
                if (EnhanceableItems.Contains(skin.Skinnable.ItemName))
                {
                    if (!result.ContainsKey(skin.Skinnable.ItemName)) result.Add(skin.Skinnable.ItemName, new List<ulong>());
                    result[skin.Skinnable.ItemName].Add(skin.WorkshopdId);
                }
            }

            // Picks 5 random skins from each result
            foreach (var entry in result)
            {
                if (entry.Value.Count > 5)
                {
                    List<ulong> skins = Pool.GetList<ulong>();
                    skins.AddRange(entry.Value.OrderBy(x => rnd.Next()).Take(5));
                    entry.Value.Clear();
                    entry.Value.AddRange(skins);
                    Pool.FreeList(ref skins);
                }
                else if (entry.Value.Count == 0) entry.Value.Add(0);
            }
            return result;
        }

        Dictionary<string, string> DefaultTierColours
        {
            get
            {
                return new Dictionary<string, string>()
                {
                    ["s"] = "#FFF900",
                    ["a"] = "#9500BA",
                    ["b"] = "#077E93",
                    ["c"] = "#AE5403",
                };
            }
        }

        Dictionary<Buff, EnhancementInfo> Default_Tier_Values
        {
            get
            {
                return new Dictionary<Buff, EnhancementInfo>()
                {
                    [Buff.Miners] = new EnhancementInfo(true, new Dictionary<string, EnhancementInfo.TierInfo>()
                    {
                        ["s"] = new EnhancementInfo.TierInfo(10, 0.15f, 0.25f),
                        ["a"] = new EnhancementInfo.TierInfo(20, 0.10f, 0.17f),
                        ["b"] = new EnhancementInfo.TierInfo(50, 0.05f, 0.12f),
                        ["c"] = new EnhancementInfo.TierInfo(100, 0.03f, 0.08f)
                    },
                    new Dictionary<int, SetBonusEffect>()
                    {
                        [2] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.25f) }),
                        [4] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.InstantMining] = new SetBonusValues(0.10f), [SetBonus.Smelting] = new SetBonusValues(0.20f), [SetBonus.BonusMultiplier] = new SetBonusValues(0.35f) }),
                        [6] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.InstantMining] = new SetBonusValues(0.20f), [SetBonus.Smelting] = new SetBonusValues(0.30f), [SetBonus.BonusMultiplier] = new SetBonusValues(0.65f), [SetBonus.MinersLuck] = new SetBonusValues(0.05f) })
                    }),

                    [Buff.Lumberjacks] = new EnhancementInfo(true, new Dictionary<string, EnhancementInfo.TierInfo>()
                    {
                        ["s"] = new EnhancementInfo.TierInfo(10, 0.15f, 0.25f),
                        ["a"] = new EnhancementInfo.TierInfo(20, 0.10f, 0.17f),
                        ["b"] = new EnhancementInfo.TierInfo(50, 0.05f, 0.12f),
                        ["c"] = new EnhancementInfo.TierInfo(100, 0.03f, 0.08f)
                    },
                    new Dictionary<int, SetBonusEffect>()
                    {
                        [2] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.25f) }),
                        [4] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.InstantWoodcutting] = new SetBonusValues(0.10f), [SetBonus.Regrowth] = new SetBonusValues(0.10f), [SetBonus.BonusMultiplier] = new SetBonusValues(0.35f) }),
                        [6] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.InstantWoodcutting] = new SetBonusValues(0.20f), [SetBonus.Regrowth] = new SetBonusValues(0.20f), [SetBonus.BonusMultiplier] = new SetBonusValues(0.65f), [SetBonus.WoodcuttersLuck] = new SetBonusValues(0.05f) })
                    }),

                    [Buff.Skinners] = new EnhancementInfo(true, new Dictionary<string, EnhancementInfo.TierInfo>()
                    {
                        ["s"] = new EnhancementInfo.TierInfo(10, 0.15f, 0.25f),
                        ["a"] = new EnhancementInfo.TierInfo(20, 0.10f, 0.17f),
                        ["b"] = new EnhancementInfo.TierInfo(50, 0.05f, 0.12f),
                        ["c"] = new EnhancementInfo.TierInfo(100, 0.03f, 0.08f)
                    },
                    new Dictionary<int, SetBonusEffect>()
                    {
                        [2] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.25f) }),
                        [4] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.InstantSkinning] = new SetBonusValues(0.10f), [SetBonus.InstantCook] = new SetBonusValues(0.50f), [SetBonus.BonusMultiplier] = new SetBonusValues(0.35f) }),
                        [6] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.InstantSkinning] = new SetBonusValues(0.20f), [SetBonus.InstantCook] = new SetBonusValues(0.80f), [SetBonus.BonusMultiplier] = new SetBonusValues(0.65f), [SetBonus.SkinnersLuck] = new SetBonusValues(0.05f) })
                    }),

                    [Buff.Farmers] = new EnhancementInfo(true, new Dictionary<string, EnhancementInfo.TierInfo>()
                    {
                        ["s"] = new EnhancementInfo.TierInfo(10, 0.10f, 0.15f),
                        ["a"] = new EnhancementInfo.TierInfo(20, 0.06f, 0.11f),
                        ["b"] = new EnhancementInfo.TierInfo(50, 0.04f, 0.07f),
                        ["c"] = new EnhancementInfo.TierInfo(100, 0.01f, 0.05f)
                    },
                    new Dictionary<int, SetBonusEffect>()
                    {
                        [2] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.10f) }),
                        [4] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.25f) }),
                        [6] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.50f) })
                    }),

                    [Buff.Foragers] = new EnhancementInfo(true, new Dictionary<string, EnhancementInfo.TierInfo>()
                    {
                        ["s"] = new EnhancementInfo.TierInfo(10, 0.15f, 0.25f),
                        ["a"] = new EnhancementInfo.TierInfo(20, 0.10f, 0.17f),
                        ["b"] = new EnhancementInfo.TierInfo(50, 0.05f, 0.12f),
                        ["c"] = new EnhancementInfo.TierInfo(100, 0.03f, 0.08f)
                    },
                    new Dictionary<int, SetBonusEffect>()
                    {
                        [2] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.25f) }),
                        [4] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.35f) }),
                        [6] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.65f) })
                    }),

                    [Buff.Fishermans] = new EnhancementInfo(true, new Dictionary<string, EnhancementInfo.TierInfo>()
                    {
                        ["s"] = new EnhancementInfo.TierInfo(10, 0.15f, 0.25f),
                        ["a"] = new EnhancementInfo.TierInfo(20, 0.10f, 0.17f),
                        ["b"] = new EnhancementInfo.TierInfo(50, 0.05f, 0.12f),
                        ["c"] = new EnhancementInfo.TierInfo(100, 0.03f, 0.08f)
                    },
                    new Dictionary<int, SetBonusEffect>()
                    {
                        [2] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.25f) }),
                        [4] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.35f), [SetBonus.Gilled] = new SetBonusValues(0.50f), [SetBonus.FishingRodModifier] = new SetBonusValues(0.25f) }),
                        [6] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.75f), [SetBonus.Gilled] = new SetBonusValues(1.0f), [SetBonus.FishersLuck] = new SetBonusValues(0.05f), [SetBonus.FishingRodModifier] = new SetBonusValues(0.50f) })
                    }),

                    [Buff.Demo] = new EnhancementInfo(true, new Dictionary<string, EnhancementInfo.TierInfo>()
                    {
                        ["s"] = new EnhancementInfo.TierInfo(10, 0.10f, 0.15f),
                        ["a"] = new EnhancementInfo.TierInfo(20, 0.06f, 0.11f),
                        ["b"] = new EnhancementInfo.TierInfo(50, 0.04f, 0.07f),
                        ["c"] = new EnhancementInfo.TierInfo(100, 0.01f, 0.05f)
                    },
                    new Dictionary<int, SetBonusEffect>()
                    {
                        [2] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.20f) }),
                        [4] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.50f) })
                    }),

                    [Buff.Elemental] = new EnhancementInfo(true, new Dictionary<string, EnhancementInfo.TierInfo>()
                    {
                        ["s"] = new EnhancementInfo.TierInfo(10, 0.15f, 0.25f),
                        ["a"] = new EnhancementInfo.TierInfo(20, 0.10f, 0.17f),
                        ["b"] = new EnhancementInfo.TierInfo(50, 0.05f, 0.12f),
                        ["c"] = new EnhancementInfo.TierInfo(100, 0.03f, 0.08f)
                    },
                    new Dictionary<int, SetBonusEffect>()
                    {
                        [2] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.20f) }),
                        [4] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.50f) })
                    }),

                    [Buff.Scavengers] = new EnhancementInfo(true, new Dictionary<string, EnhancementInfo.TierInfo>()
                    {
                        ["s"] = new EnhancementInfo.TierInfo(10, 0.10f, 0.15f),
                        ["a"] = new EnhancementInfo.TierInfo(20, 0.06f, 0.11f),
                        ["b"] = new EnhancementInfo.TierInfo(50, 0.04f, 0.07f),
                        ["c"] = new EnhancementInfo.TierInfo(100, 0.01f, 0.05f)
                    },
                    new Dictionary<int, SetBonusEffect>()
                    {
                        [2] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.10f) }),
                        [4] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.20f), [SetBonus.Smasher] = new SetBonusValues(0.30f) }),
                        [6] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.50f), [SetBonus.Smasher] = new SetBonusValues(0.75f), [SetBonus.Attractive] = new SetBonusValues(0.50f) })
                    }),

                    [Buff.Transporters] = new EnhancementInfo(true, new Dictionary<string, EnhancementInfo.TierInfo>()
                    {
                        ["s"] = new EnhancementInfo.TierInfo(10, 0.10f, 0.15f),
                        ["a"] = new EnhancementInfo.TierInfo(20, 0.06f, 0.11f),
                        ["b"] = new EnhancementInfo.TierInfo(50, 0.04f, 0.07f),
                        ["c"] = new EnhancementInfo.TierInfo(100, 0.01f, 0.05f)
                    },
                    new Dictionary<int, SetBonusEffect>()
                    {
                        [2] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.10f) }),
                        [4] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.20f), [SetBonus.IncreasedBoatSpeed] = new SetBonusValues(0.50f) }),
                        [6] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.50f), [SetBonus.IncreasedBoatSpeed] = new SetBonusValues(1f), [SetBonus.FreeVehicleRepair] = new SetBonusValues(1f) })
                    }),

                    [Buff.Crafters] = new EnhancementInfo(true, new Dictionary<string, EnhancementInfo.TierInfo>()
                    {
                        ["s"] = new EnhancementInfo.TierInfo(10, 0.10f, 0.15f),
                        ["a"] = new EnhancementInfo.TierInfo(20, 0.06f, 0.11f),
                        ["b"] = new EnhancementInfo.TierInfo(50, 0.04f, 0.07f),
                        ["c"] = new EnhancementInfo.TierInfo(100, 0.01f, 0.05f)
                    },
                    new Dictionary<int, SetBonusEffect>()
                    {
                        [2] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.10f) }),
                        [4] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.20f), [SetBonus.Researcher] = new SetBonusValues(0.10f) }),
                        [6] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.50f), [SetBonus.Researcher] = new SetBonusValues(0.3f) })
                    }),

                    [Buff.Reinforced] = new EnhancementInfo(true, new Dictionary<string, EnhancementInfo.TierInfo>()
                    {
                        ["s"] = new EnhancementInfo.TierInfo(10, 0.10f, 0.15f),
                        ["a"] = new EnhancementInfo.TierInfo(20, 0.06f, 0.11f),
                        ["b"] = new EnhancementInfo.TierInfo(50, 0.04f, 0.07f),
                        ["c"] = new EnhancementInfo.TierInfo(100, 0.01f, 0.05f)
                    },
                    new Dictionary<int, SetBonusEffect>()
                    {
                        [2] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.10f) }),
                        [4] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.20f), [SetBonus.Feline] = new SetBonusValues(0.50f, new Dictionary<string, string>() { ["DoubleJump.use"] = "Double Jump" }), [SetBonus.Lead] = new SetBonusValues(0.50f) }),
                        [6] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.50f), [SetBonus.Feline] = new SetBonusValues(1f, new Dictionary<string, string>() { ["DoubleJump.use"] = "Double Jump" }), [SetBonus.Lead] = new SetBonusValues(1f) })
                    }),

                    [Buff.Tamers] = new EnhancementInfo(true, new Dictionary<string, EnhancementInfo.TierInfo>()
                    {
                        ["s"] = new EnhancementInfo.TierInfo(10, 0.10f, 0.15f),
                        ["a"] = new EnhancementInfo.TierInfo(20, 0.06f, 0.11f),
                        ["b"] = new EnhancementInfo.TierInfo(50, 0.04f, 0.07f),
                        ["c"] = new EnhancementInfo.TierInfo(100, 0.01f, 0.05f)
                    },
                    new Dictionary<int, SetBonusEffect>()
                    {
                        [2] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.10f) }),
                        [4] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.20f), [SetBonus.Reflexes] = new SetBonusValues(0.10f) }),
                        [6] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.50f), [SetBonus.Reflexes] = new SetBonusValues(0.2f) })
                    }),

                    [Buff.Hunters] = new EnhancementInfo(true, new Dictionary<string, EnhancementInfo.TierInfo>()
                    {
                        ["s"] = new EnhancementInfo.TierInfo(10, 0.10f, 0.15f),
                        ["a"] = new EnhancementInfo.TierInfo(20, 0.06f, 0.11f),
                        ["b"] = new EnhancementInfo.TierInfo(50, 0.04f, 0.07f),
                        ["c"] = new EnhancementInfo.TierInfo(100, 0.01f, 0.05f)
                    },
                    new Dictionary<int, SetBonusEffect>()
                    {
                        [2] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.10f) }),
                        [4] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.20f), [SetBonus.Survivalist] = new SetBonusValues(0.20f) }),
                        [6] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.50f), [SetBonus.Survivalist] = new SetBonusValues(0.5f) })
                    }),

                    [Buff.Operators] = new EnhancementInfo(true, new Dictionary<string, EnhancementInfo.TierInfo>()
                    {
                        ["s"] = new EnhancementInfo.TierInfo(10, 0.10f, 0.15f),
                        ["a"] = new EnhancementInfo.TierInfo(20, 0.06f, 0.11f),
                        ["b"] = new EnhancementInfo.TierInfo(50, 0.04f, 0.07f),
                        ["c"] = new EnhancementInfo.TierInfo(100, 0.01f, 0.05f)
                    },
                    new Dictionary<int, SetBonusEffect>()
                    {
                        [2] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.20f) }),
                        [4] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.20f) }),
                        [6] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.50f) })
                    }),

                    [Buff.Jockeys] = new EnhancementInfo(true, new Dictionary<string, EnhancementInfo.TierInfo>()
                    {
                        ["s"] = new EnhancementInfo.TierInfo(10, 0.15f, 0.25f),
                        ["a"] = new EnhancementInfo.TierInfo(20, 0.10f, 0.17f),
                        ["b"] = new EnhancementInfo.TierInfo(50, 0.05f, 0.12f),
                        ["c"] = new EnhancementInfo.TierInfo(100, 0.03f, 0.08f)
                    },
                    new Dictionary<int, SetBonusEffect>()
                    {
                        [2] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.25f) }),
                        [4] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.35f), [SetBonus.PVPCrit] = new SetBonusValues(0.05f) }),
                        [6] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.75f), [SetBonus.PVPCrit] = new SetBonusValues(0.10f) })
                    }),

                    [Buff.Raiders] = new EnhancementInfo(true, new Dictionary<string, EnhancementInfo.TierInfo>()
                    {
                        ["s"] = new EnhancementInfo.TierInfo(10, 0.03f, 0.05f),
                        ["a"] = new EnhancementInfo.TierInfo(20, 0.02f, 0.04f),
                        ["b"] = new EnhancementInfo.TierInfo(50, 0.01f, 0.03f),
                        ["c"] = new EnhancementInfo.TierInfo(100, 0.01f, 0.02f)
                    },
                    new Dictionary<int, SetBonusEffect>()
                    {
                        [4] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.05f) }),
                        [6] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.10f) })
                    }),

                    [Buff.Builders] = new EnhancementInfo(true, new Dictionary<string, EnhancementInfo.TierInfo>()
                    {
                        ["s"] = new EnhancementInfo.TierInfo(10, 0.10f, 0.15f),
                        ["a"] = new EnhancementInfo.TierInfo(20, 0.06f, 0.11f),
                        ["b"] = new EnhancementInfo.TierInfo(50, 0.04f, 0.07f),
                        ["c"] = new EnhancementInfo.TierInfo(100, 0.01f, 0.05f)
                    },
                    new Dictionary<int, SetBonusEffect>()
                    {
                        [2] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.10f) }),
                        [4] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.20f) }),
                        [6] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.30f) })
                    }),

                    [Buff.Assemblers] = new EnhancementInfo(true, new Dictionary<string, EnhancementInfo.TierInfo>()
                    {
                        ["s"] = new EnhancementInfo.TierInfo(10, 0.08f, 0.10f),
                        ["a"] = new EnhancementInfo.TierInfo(20, 0.05f, 0.07f),
                        ["b"] = new EnhancementInfo.TierInfo(50, 0.03f, 0.06f),
                        ["c"] = new EnhancementInfo.TierInfo(100, 0.01f, 0.04f)
                    },
                    new Dictionary<int, SetBonusEffect>()
                    {
                        [2] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.10f) }),
                        [4] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.15f) }),
                        [6] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.20f) })
                    }),

                    [Buff.Assassins] = new EnhancementInfo(true, new Dictionary<string, EnhancementInfo.TierInfo>()
                    {
                        ["s"] = new EnhancementInfo.TierInfo(10, 0.01f, 0.03f),
                        ["a"] = new EnhancementInfo.TierInfo(20, 0.01f, 0.02f),
                        ["b"] = new EnhancementInfo.TierInfo(50, 0.01f, 0.01f),
                        ["c"] = new EnhancementInfo.TierInfo(100, 0.01f, 0.01f)
                    },
                    new Dictionary<int, SetBonusEffect>()
                    {
                        [4] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.05f), [SetBonus.UncannyDodge] = new SetBonusValues(0.05f) })
                    }),

                    [Buff.Fabricators] = new EnhancementInfo(true, new Dictionary<string, EnhancementInfo.TierInfo>()
                    {
                        ["s"] = new EnhancementInfo.TierInfo(10, 0.04f, 0.05f),
                        ["a"] = new EnhancementInfo.TierInfo(20, 0.03f, 0.04f),
                        ["b"] = new EnhancementInfo.TierInfo(50, 0.01f, 0.03f),
                        ["c"] = new EnhancementInfo.TierInfo(100, 0.01f, 0.02f)
                    },
                    new Dictionary<int, SetBonusEffect>()
                    {
                        [4] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.05f) })
                    }),

                    [Buff.Medics] = new EnhancementInfo(true, new Dictionary<string, EnhancementInfo.TierInfo>()
                    {
                        ["s"] = new EnhancementInfo.TierInfo(10, 0.03f, 0.05f),
                        ["a"] = new EnhancementInfo.TierInfo(20, 0.02f, 0.04f),
                        ["b"] = new EnhancementInfo.TierInfo(50, 0.01f, 0.03f),
                        ["c"] = new EnhancementInfo.TierInfo(100, 0.01f, 0.02f)
                    },
                    new Dictionary<int, SetBonusEffect>()
                    {
                        [3] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.25f) }),
                        [6] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.50f), [SetBonus.TeamHeal] = new SetBonusValues(0.30f), [SetBonus.HealthShot] = new SetBonusValues(0.50f) })
                    }),

                    [Buff.Knights] = new EnhancementInfo(true, new Dictionary<string, EnhancementInfo.TierInfo>()
                    {
                        ["s"] = new EnhancementInfo.TierInfo(10, 0.03f, 0.05f),
                        ["a"] = new EnhancementInfo.TierInfo(20, 0.02f, 0.04f),
                        ["b"] = new EnhancementInfo.TierInfo(50, 0.01f, 0.03f),
                        ["c"] = new EnhancementInfo.TierInfo(100, 0.01f, 0.02f)
                    },
                    new Dictionary<int, SetBonusEffect>()
                    {
                        [3] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.25f) }),
                        [6] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.50f) })
                    }),

                    [Buff.Barbarians] = new EnhancementInfo(true, new Dictionary<string, EnhancementInfo.TierInfo>()
                    {
                        ["s"] = new EnhancementInfo.TierInfo(10, 0.03f, 0.05f),
                        ["a"] = new EnhancementInfo.TierInfo(20, 0.02f, 0.04f),
                        ["b"] = new EnhancementInfo.TierInfo(50, 0.01f, 0.03f),
                        ["c"] = new EnhancementInfo.TierInfo(100, 0.01f, 0.02f)
                    },
                    new Dictionary<int, SetBonusEffect>()
                    {
                        [3] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.25f) }),
                        [6] = new SetBonusEffect(new Dictionary<SetBonus, SetBonusValues>() { [SetBonus.BonusMultiplier] = new SetBonusValues(0.50f), [SetBonus.MaxHealth] = new SetBonusValues(0.2f) })
                    }),
                };
            }
        }

        // Table is food based.
        List<DropTable> GetDefaultTreeTable()
        {
            List<DropTable> result = new List<DropTable>();
            var itemDefs = ItemManager.GetItemDefinitions();
            foreach (var def in itemDefs)
            {
                if (def.category == ItemCategory.Food && def.isUsable)
                {
                    result.Add(new DropTable(def.shortname, 1, 3, 100));
                }
            }
            return result;
        }

        List<DropTable> GetDefaultMineTable
        {
            get
            {
                return new List<DropTable>()
                {
                    new DropTable("stones", 1, 250, 100),
                    new DropTable("metal.refined", 1, 10, 100),
                    new DropTable("hq.metal.ore", 1, 12, 100),
                    new DropTable("metal.fragments", 1, 250, 100),
                    new DropTable("metal.ore", 1, 250, 100),
                    new DropTable("fat.animal", 1, 100, 100),
                    new DropTable("cloth", 1, 250, 100),
                    new DropTable("sulfur", 1, 250, 100),
                    new DropTable("sulfur.ore", 1, 300, 100),
                    new DropTable("bone.fragments", 1, 300, 100),
                    new DropTable("horsedung", 1, 10, 100),
                    new DropTable("leather", 1, 100, 100),
                    new DropTable("plantfiber", 1, 25, 100),
                    new DropTable("diesel_barrel", 1, 5, 100),
                    new DropTable("lowgradefuel", 1, 50, 100)
                };
            }
        }

        List<DropTable> GetDefaultSkinTable
        {
            get
            {
                return new List<DropTable>()
                {
                    new DropTable("riflebody", 1, 2, 100),
                    new DropTable("semibody", 1, 3, 100),
                    new DropTable("smgbody", 1, 3, 100),
                    new DropTable("metalpipe", 1, 3, 100),
                    new DropTable("roadsigns", 1, 3, 100),
                    new DropTable("techparts", 1, 2, 100),
                    new DropTable("cctv.camera", 1, 1, 100),
                    new DropTable("targeting.computer", 1, 1, 100),
                    new DropTable("rope", 1, 6, 100),
                    new DropTable("tarp", 1, 3, 100),
                    new DropTable("metalblade", 1, 3, 100),
                    new DropTable("charcoal", 1, 5, 100),
                    new DropTable("bone.fragments", 1, 5, 100)
                };
            }
        }

        List<DropTable> GetDefaultFishingTable
        {
            get
            {
                return new List<DropTable>()
                {
                    new DropTable("easter.silveregg", 1, 2, 10),
                    new DropTable("riflebody", 1, 2, 100),
                    new DropTable("semibody", 1, 3, 100),
                    new DropTable("smgbody", 1, 3, 100),
                    new DropTable("metalpipe", 1, 3, 100),
                    new DropTable("roadsigns", 1, 3, 100),
                    new DropTable("techparts", 1, 2, 100),
                    new DropTable("cctv.camera", 1, 1, 100),
                    new DropTable("targeting.computer", 1, 1, 100),
                    new DropTable("rope", 1, 6, 100),
                    new DropTable("tarp", 1, 3, 100),
                    new DropTable("metalblade", 1, 3, 100),
                    new DropTable("charcoal", 1, 5, 100),
                    new DropTable("bone.fragments", 1, 5, 100)
                };
            }
        }

        Dictionary<SetBonus, DropTableLuck> Default_tables
        {
            get
            {
                return new Dictionary<SetBonus, DropTableLuck>()
                {
                    [SetBonus.MinersLuck] = new DropTableLuck(GetDefaultMineTable),
                    [SetBonus.SkinnersLuck] = new DropTableLuck(GetDefaultSkinTable),
                    [SetBonus.WoodcuttersLuck] = new DropTableLuck(GetDefaultTreeTable()),
                    [SetBonus.FishersLuck] = new DropTableLuck(GetDefaultSkinTable),
                };
            }
        }

        #endregion

        #region Data

        [PluginReference]
        private Plugin Cooking, RandomTrader, FishingContest, ImageLibrary, TruePVE, SkillTree, UINotify;

        PlayerEntity pcdData;
        private DynamicConfigFile PCDDATA;
        public Dictionary<string, int> ItemIDs = new Dictionary<string, int>();
        public Dictionary<int, List<Buff>> Buff_Info_Pages = new Dictionary<int, List<Buff>>();

        List<ulong> NotifyPlayers = new List<ulong>();

        const string perm_use = "epicloot.use";
        const string perm_admin = "epicloot.admin";
        const string perm_drop = "epicloot.drop";
        const string perm_salvage = "epicloot.salvage";
        const string perm_enhance = "epicloot.enhance";
        const string perm_enhance_free = "epicloot.enhance.free";
        const string perm_craft = "epicloot.craft";
        private static EpicLoot Instance { get; set; }

        void Init()
        {
            Instance = this;
            permission.RegisterPermission(perm_use, this);
            permission.RegisterPermission(perm_drop, this);
            permission.RegisterPermission(perm_admin, this);
            permission.RegisterPermission(perm_salvage, this);
            permission.RegisterPermission(perm_enhance, this);
            permission.RegisterPermission(perm_enhance_free, this);
            permission.RegisterPermission(perm_craft, this);

            if (config.skilltree_settings.allow_MaxRepair_buff_on_EL_items) Unsubscribe(nameof(STOnItemRepairWithMaxRepair));

            PCDDATA = Interface.Oxide.DataFileSystem.GetFile(this.Name);
            LoadData();

            if (!config.scrapper_settings.enabled || !config.scrapper_settings.allow_recyler) Unsubscribe(nameof(CanRecycle));

            if (config.skilltree_settings.enabled)
            {
                for (int i = 1; i < config.skilltree_settings.max_level + 1; i++)
                {
                    permission.RegisterPermission($"epicloot.skilltree.{i}", this);
                    SkillTreeChancePerms.Add($"epicloot.skilltree.{i}", config.skilltree_settings.increase_per_level * i);
                }
            }
        }

        void DestroyUIs(BasePlayer player, bool includingHud = true)
        {
            CuiHelper.DestroyUi(player, "InspectMenu");
            if (includingHud) CuiHelper.DestroyUi(player, "InspectButtonImg");
            CuiHelper.DestroyUi(player, "ItemDetails_Header_outer");
            CuiHelper.DestroyUi(player, "EL_Button_Repos");
            CuiHelper.DestroyUi(player, "EL_Info");
            CuiHelper.DestroyUi(player, "EL_Settings");
            CuiHelper.DestroyUi(player, "EpicSalvager_SalvagerMenu");
            CuiHelper.DestroyUi(player, "SalvagerBackground");
            CuiHelper.DestroyUi(player, "Salvager_Market_item_select");
            CuiHelper.DestroyUi(player, "Salvager_Market_buff_select");
            CuiHelper.DestroyUi(player, "Salvager_Market_confirmation");
        }

        void Unload()
        {
            _harmony.UnpatchAll(Name + "Patch");
            SaveData();
            foreach (var player in BasePlayer.activePlayerList)
            {
                RemovePerms(player.userID);
                DestroyUIs(player);
                if (player.isMounted)
                {
                    var vehicle = player.GetMountedVehicle();
                    if (vehicle != null && vehicle is MotorRowboat)
                    {
                        ResetBoatSpeed(vehicle as MotorRowboat);
                    }
                }                
                foreach (var mod in player.modifiers.All)
                {
                    if (mod.Type == Modifier.ModifierType.Max_Health && mod.Duration > 1200f)
                    {
                        RemoveMaxHealth(player);
                        break;
                    }
                }
            }

            foreach (var boat in Tracked_Boats)
            {
                ResetFuelRate(boat.Value);
            }
            foreach (var boat in Tracked_Boat_Speeds)
            {
                ResetBoatSpeed(boat.Value);
            }
            foreach (var mini in Tracked_Helis)
            {
                ResetFuelRate(mini.Value);
            }
            foreach (var horse in Tracked_Horses)
            {
                ResetHorse(horse.Value.horse);
            }                       
            ClearData();

            foreach (var _command in config.general_settings.ui_commands)
            {
                cmd.RemoveChatCommand(_command, this);
                cmd.RemoveConsoleCommand(_command, this);
            }

            NotifyPlayers.Clear();
            NotifyPlayers = null;

            StaticBuffs.Clear();
        }

        Dictionary<ulong, List<string>> Perms_tracker = new Dictionary<ulong, List<string>>();

        void AddPerms(BasePlayer player, List<string> valid_perms)
        {
            List<string> perms;
            if (!Perms_tracker.TryGetValue(player.userID, out perms)) Perms_tracker.Add(player.userID, perms = new List<string>());
            foreach (var perm in valid_perms)
            {
                if (!perms.Contains(perm)) perms.Add(perm);
                if (!permission.UserHasPermission(player.UserIDString, perm)) permission.GrantUserPermission(player.UserIDString, perm, null);
            }
        }

        void ClearData()
        {
            looted_containers.Clear();
            looted_corpses.Clear();
            npc_corpses.Clear();
            parsedEnums.Clear();
            player_attributes.Clear();
            EnhanceableItems.Clear();
            Tracked_Helis.Clear();
            Tracked_Boats.Clear();
            Tracked_Boat_Speeds.Clear();
            Tracked_Horses.Clear();
            item_BPs.Clear();
            Perms_tracker.Clear();
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
            public Dictionary<ulong, PlayerSettings> pEntity = new Dictionary<ulong, PlayerSettings>();
        }

        class PlayerSettings
        {
            public float offsetMin_x;
            public float offsetMax_x;
            public float offsetMin_y;
            public float offsetMax_y;

            public bool notifications = true;
            public bool showHud = true;

            public PlayerSettings(float offsetMin_x, float offsetMin_y, float offsetMax_x, float offsetMax_y)
            {
                this.offsetMin_x = offsetMin_x;
                this.offsetMin_y = offsetMin_y;
                this.offsetMax_x = offsetMax_x;
                this.offsetMax_y = offsetMax_y;
            }
        }

        public class EnhancementInfo
        {
            public bool enabled;
            // Black list will prevent matching items from being enchanted with this buff type.
            public List<string> item_blacklist;

            // If Whitelist is not null, it will only allow these types of items.
            public List<string> item_whitelist;

            public Dictionary<string, TierInfo> tierInfo;

            public Dictionary<string, ulong> static_skins = new Dictionary<string, ulong>();

            public class TierInfo
            {
                public int chance_weight;
                public float min_value;
                public float max_value;
                public TierInfo(int chance_weight, float min_value, float max_value)
                {
                    this.chance_weight = chance_weight;
                    this.min_value = min_value;
                    this.max_value = max_value;
                }
            }

            public Dictionary<int, SetBonusEffect> setInfo;

            /* int is the amount of pieces. SetBonusEffect is the list of bonuses we apply for meeting the number of pieces.
                 * Bonus will choose the last qualifying bonus. IE if they achieved 4/4, it will only look at the effects for 4/4 instead of adding all of the previous buffs.
                 */
            public EnhancementInfo(bool enabled, Dictionary<string, TierInfo> tierInfo, Dictionary<int, SetBonusEffect> setInfo, List<string> item_blacklist = null, List<string> item_whitelist = null)
            {
                this.enabled = enabled;
                this.tierInfo = tierInfo;
                this.setInfo = setInfo;
                this.item_blacklist = item_blacklist;
                this.item_whitelist = item_whitelist;
            }
        }

        public class SetBonusEffect
        {
            public Dictionary<SetBonus, SetBonusValues> setBonus;
            public SetBonusEffect(Dictionary<SetBonus, SetBonusValues> setBonus)
            {
                this.setBonus = setBonus;
            }
        }

        public class SetBonusValues
        {
            public float modifier;

            [JsonProperty("Permissions [permission / title]")]
            public Dictionary<string, string> perms = new Dictionary<string, string>();

            public SetBonusValues(float modifier, Dictionary<string, string> perms = null)
            {
                this.modifier = modifier;
                this.perms = perms;
            }
        }

        public Dictionary<string, Buff> parsedEnums = new Dictionary<string, Buff>(StringComparer.InvariantCultureIgnoreCase);

        // A dictionary that we use to track the 
        public Dictionary<ulong, BuffSetInfo> player_attributes = new Dictionary<ulong, BuffSetInfo>();

        /* Will be named based identifier [<Buff> <item display name>] Tier and modifier will be stored in the description.english field [<tier letter (S,A,B,C)> <value>].
            Example: Miners Roadsign Gloves
         */
        //public Dictionary<string, EnchantInfo> enchants = new Dictionary<string, EnchantInfo>();        

        ///*Can store the tier and modifier into the item description, allowing for the info to be retained in the background for worn items only*/
        //public class EnchantInfo
        //{
        //    public Buff buffType;
        //    public float modifier;
        //}

        public class BuffSetInfo
        {
            public Dictionary<Buff, BuffAttributes> buffs = new Dictionary<Buff, BuffAttributes>();
            public Dictionary<SetBonus, SetBonusValues> setBonuses = new Dictionary<SetBonus, SetBonusValues>();
            public Dictionary<SetBonus, List<string>> setBonusPerms = new Dictionary<SetBonus, List<string>>();
            public Dictionary<Buff, float> setBonusMultiplier = new Dictionary<Buff, float>();
        }

        public class BuffAttributes
        {
            public void AddPiece(float modifier)
            {
                this.totalModifier += modifier;
                this.piecesWorn++;
            }

            public void RemovePiece(float modifier)
            {
                this.totalModifier -= modifier;
                this.piecesWorn--;
                if (this.piecesWorn == 0) totalModifier = 0;
            }
            public float totalModifier;
            public int piecesWorn;
            public BuffAttributes(float totalModifier, int piecesWorn)
            {
                this.totalModifier = totalModifier;
                this.piecesWorn = piecesWorn;
            }
        }

        List<string> EnhanceableItems = new List<string>();

        #endregion;

        #region Enums

        public enum SetBonus
        {
            BonusMultiplier, // Multiple the bonus of the item.
            Smelting, // Instantly smelt resources as you mine them
            InstantMining, // INstantly mine a node out
            InstantWoodcutting, // Instantly cut a tree down
            Regrowth, // Instantly respawn a tree at the same location
            InstantSkinning, // Instantly butcher an animal
            InstantCook, // Instantly cooks meat
            PVPCrit, // Chance to do critical damage in PVP
            Reflexes, // Reduced damage in PVP
            IncreasedBoatSpeed, // Increase boat speed
            FreeVehicleRepair, // Repair vehicles for free
            Survivalist, // Increases cal/hyd from food/water
            Researcher, // Refund or partial refund of scrap when researching
            Feline, // Reduces fall damage
            Lead, // Reduces radiation damage
            Gilled, // Underwater breathing
            Smasher, // % chance to destroy barells and roadsigns instantly
            WoodcuttersLuck, // Access to a loot table for woodcutting.
            MinersLuck, //access to a loot table for mining.
            SkinnersLuck, // access to a loot table for skinning.
            RockCycle, // chance to spawn a new rock once mined out.
            Attractive, // chance for loot to be instantly moved to your inventory.
            FishersLuck, // acces to a loot table for fishing.
            TeamHeal, // Shares heals with nearby team mates
            HealthShot, // Heals team mates for damage that would have been done when shot
            BulletProof, // Reduces damage from bullets.
            FishingRodModifier, // Adjusts the tensile strenght of the cast rod
            UncannyDodge, // Chance to dodge projectiles and receive no damage 
            MaxHealth // Increases the max health of the player.
        }

        public enum Buff
        {
            None,
            Miners, // Mining yield
            Lumberjacks, // woodcutting yield
            Skinners, // Skinning yield
            Farmers, // Grown harvesting yield
            Foragers, // Map generated harvesting yield
            Fishermans, // Fishing yield
            Assassins, // Increased PVP damage
            Demo, // Decreased damage from explosives
            Elemental, // Decreased damage from fire/cold/radiation
            Scavengers, // Chance to obtain scrap from barrels/chests.
            Transporters, // Reduced fuel consumption in helis and boats.
            Crafters, // Increased crafting speed
            Reinforced, // Reduces durability loss
            Tamers, // Reduced damage from animals
            Hunters, // Increased damage to animals
            Operators, // Increased damage to scientists
            Jockeys, // Increases speed of horse
            Raiders, // Chance for thrown explosive or rocket to replaced on use.
            Builders, // Chance for build and upgrade costs to be refunded.
            Assemblers, // Chance for components to be refunded.
            Fabricators, // Chance to create an additional item on craft.
            Medics, // Increase healing
            Knights, // Melee damage reduction
            Barbarians, // Increased damage with melee weapons
        }

        public static List<string> StaticBuffs = new List<string>();

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            Dictionary<string, string> language = new Dictionary<string, string>
            {
                ["ScrapRefund"] = "Вы получили возврат металлолома за свои исследования.",
                ["CraftRefund"] = "Вы получили возврат за свое последнее изготовление.",
                ["FreeUpgrade"] = "Вам предоставлена бесплатная модернизация.",
                ["NoPerms"] = "У вас нет разрешения на использование этой команды.",
                ["GenItemInvalidParameters"] = "Некорректные параметры. Использование:\n- genitem <target> <optional: item shortname> <optional: enhancement type> <optional: tier>",
                ["UISetTypes"] = "ЗАДАТЬ ТИПЫ",
                ["InfoMiners"] = "<color=#ffb600><b>Miners: </b></color>Увеличивает вашу добычу руды на x% за каждый надетый предмет",
                ["InfoLumberjacks"] = "<color=#ffb600><b>Lumberjacks: </b></color>Увеличивает вашу добычу древесины на x%, за каждый экипированный предмет.",
                ["InfoSkinners"] = "<color=#ffb600><b>Skinners: </b></color>Увеличивает вашу добычу при снятии шкур на x%, за каждый экипированный предмет.",
                ["InfoFarmers"] = "<color=#ffb600><b>Farmers: </b></color>Увеличивает ваш урожай при сельском хозяйстве на x%, за каждый экипированный предмет.",
                ["InfoForagers"] = "<color=#ffb600><b>Foragers: </b></color>Увеличивает ваш сбор урожая дикорастущего на x%, за каждый экипированный предмет.",
                ["InfoFishermans"] = "<color=#ffb600><b>Fishermans: </b></color>Увеличивает ваш улов при рыбалке на x%, за каждый экипированный предмет.",
                ["InfoAssassins"] = "<color=#ffb600><b>Assassins: </b></color>Увеличивает урон по игрокам в PVP на x% за каждый экипированный предмет.",
				["InfoDemo"] = "<color=#ffb600><b>Demo: </b></color>Уменьшает получаемый урон от взрывчатки на x% за каждый экипированный предмет.",
				["InfoElemental"] = "<color=#ffb600><b>Elemental: </b></color>Уменьшает получаемый урон от огня и холода на x% за каждый экипированный предмет.",
				["InfoScavengers"] = "<color=#ffb600><b>Scavengers: </b></color>Предоставляет x% шанс получить дополнительный лом за каждый экипированный предмет.",
				["InfoTransporters"] = "<color=#ffb600><b>Transporters: </b></color>Снижает расход топлива на коптере/лодке на x% за каждый экипированный предмет.",
				["InfoCrafters"] = "<color=#ffb600><b>Crafters: </b></color>Увеличивает скорость создания на x% за каждый экипированный предмет.",
				["InfoReinforced"] = "<color=#ffb600><b>Reinforced: </b></color>Снижает потерю прочности на всех предметах на x% за каждый экипированный предмет.",
				["InfoTamers"] = "<color=#ffb600><b>Tamers: </b></color>Уменьшает урон от животных на x% за каждый экипированный предмет.",
				["InfoHunters"] = "<color=#ffb600><b>Hunters: </b></color>Увеличивает урон по животным на x% за каждый экипированный предмет.",
				["InfoOperators"] = "<color=#ffb600><b>Operators: </b></color>Увеличивает урон по NPC на x% за каждый экипированный предмет.",
				["InfoJockeys"] = "<color=#ffb600><b>Jockeys: </b></color>Увеличивает скорость лошадей на x% за каждый экипированный предмет.",
				["InfoRaiders"] = "<color=#ffb600><b>Raiders: </b></color>Предоставляет x% шанс замены метательных взрывчаток или ракет за каждый экипированный предмет.",
				["InfoBuilders"] = "<color=#ffb600><b>Builders: </b></color>Предоставляет x% шанс возврата затрат на обновление и строительство за каждый экипированный предмет.",
				["InfoAssemblers"] = "<color=#ffb600><b>Assemblers: </b></color>Предоставляет x% шанс возврата компонентов при создании за каждый экипированный предмет.",
				["InfoFabricators"] = "<color=#ffb600><b>Fabricators: </b></color>Предоставляет x% шанс создания дополнительного предмета за каждый экипированный предмет.",
				["InfoKnights"] = "<color=#ffb600><b>Knights: </b></color>Уменьшает урон в ближнем бою на x%.",
				["InfoBarbarians"] = "<color=#ffb600><b>Barbarians: </b></color>Увеличивает урон в ближнем бою на x%.",
				["InfoMedics"] = "<color=#ffb600><b>Medics: </b></color>Увеличивает получаемое исцеление на x% за каждый экипированный предмет.",
                ["UIPlayerSettingsTitle"] = "НАСТРОЙКИ ИГРОКА",
                ["UIReposButtonDescription"] = "Переместить значок HUD",
                ["UIToggle"] = "Кнопка переключения HUD",
                ["UINotificationsToggle"] = "Включить/выключить уведомления о баффах",
                ["UIReposButtonText"] = "ИЗМЕНИТЬ",
                ["RegrowProc"] = "Срубленное вами дерево быстро отрастает.",
                ["RockcycleProc"] = "Камень, который вы добыли, прошел рок-цикл.",
                ["FabricateProc"] = "Вам удалось изготовить дополнительный {0}.",
                ["MultipleMatches"] = "Найдено несколько игроков, соответствующих {0}.\n{1}",
                ["NoMatch"] = "Не найдено соответствий для {0}.",
                ["GenerateItemHookResponse"] = "Вы получили: {0}.\n- Уровень: {1}.\n- Значение: {2}",
                ["MissionItemReceived"] = "Вы получили {0} за выполнение мисии.",
                ["UIMoveL"] = "L",
                ["UIMoveU"] = "U",
                ["UIMoveR"] = "R",
                ["UIMoveD"] = "D",
                ["UIMainTitle"] = "ПРОВЕРКА СНАРЯЖЕНИЯ",
                ["UIMainBuff"] = "БАФФ",
                ["UIMainBonus"] = "БОНУС",
                ["UIMainSetBonus"] = "СЕТОВЫЕ БОНУСЫ",
                ["UIMainSetBonusLine"] = "<color=#42f105>+</color> Бонус {0}: <color=#42f105>+{1}%</color>. Это показано в бонусе выше.\n",
                ["UIMainSetBonusDefault"] = "Нет информации о бонусах. Экипируйте больше предметов с тем же типом сета, чтобы получить бонусы.",
                ["UILeftPanelBuff"] = "БАФФ: ",
                ["UILeftPanelTier"] = "УРОВЕНЬ: ",
                ["UILeftPanelModifier"] = "МОДИФИКАТОР: ",
                ["DispayName.Buff.Miners"] = "Добыча руды",
                ["DispayName.Buff.Lumberjacks"] = "Добыча дерева",
                ["DispayName.Buff.Skinners"] = "Добыча шкур",
                ["DispayName.Buff.Farmers"] = "Сельское хозяйство",
                ["DispayName.Buff.Foragers"] = "Сбор урожая",
                ["DispayName.Buff.Fishermans"] = "Рыболовство",
                ["DispayName.Buff.Assassins"] = "Урон в PVP",
                ["DispayName.Buff.Demo"] = "Сопротивление взрывам",
                ["DispayName.Buff.Elemental"] = "Сопротивление стихиям",
                ["DispayName.Buff.Scavengers"] = "Шанс на дополнительный лом",
                ["DispayName.Buff.Transporters"] = "Экономия топлива",
                ["DispayName.Buff.Crafters"] = "Скорость создания",
                ["DispayName.Buff.Reinforced"] = "Потеря прочности",
                ["DispayName.Buff.Tamers"] = "Уменьшение урона от животных",
                ["DispayName.Buff.Hunters"] = "Бонус урона по животным",
                ["DispayName.Buff.Operators"] = "Бонус урона по ученым NPC",
                ["DispayName.Buff.Jockeys"] = "Скорость лошади",
                ["DispayName.Buff.Raiders"] = "Шанс возврата взрывчаток",
                ["DispayName.Buff.Assemblers"] = "Шанс возврата при создании",
                ["DispayName.Buff.Knights"] = "Сопротивление урону в ближнем бою",
                ["DispayName.Buff.Barbarians"] = "Бонус урона в ближнем бою",
                ["DispayName.Buff.Fabricators"] = "Шанс создания",
                ["DispayName.Buff.Builders"] = "Шанс возврата при улучшении строений",
                ["DispayName.Buff.Medics"] = "Бонус исцеления",
                ["Description.SetBonus.FreeVehicleRepair"] = "Позволяет вам бесплатно починить свое транспортное средство.",
                ["Description.SetBonus.IncreasedBoatSpeed"] = "Увеличивает скорость вашей лодки на <color=#65d615>{0}%</color>.",
                ["Description.SetBonus.InstantCook"] = "У вас есть шанс в <color=#65d615>{0}%</color> получить приготовленное мясо при снятии шкуры.",
                ["Description.SetBonus.InstantMining"] = "У вас есть шанс в <color=#65d615>{0}%</color> мгновенно добыть руду из узла.",
                ["Description.SetBonus.InstantSkinning"] = "У вас есть шанс в <color=#65d615>{0}%</color> мгновенно снять шкуру с животного.",
                ["Description.SetBonus.InstantWoodcutting"] = "У вас есть шанс в <color=#65d615>{0}%</color> мгновенно срубить дерево.",
                ["Description.SetBonus.PVPCrit"] = "У вас есть шанс в <color=#65d615>{0}%</color> нанести дополнительный критический урон против игроков.",
                ["Description.SetBonus.Reflexes"] = "Вы получаете <color=#65d615>{0}%</color> меньше урона от игроков.",
                ["Description.SetBonus.Regrowth"] = "Деревья, которые вы срубили, имеют шанс в <color=#65d615>{0}%</color> мгновенно восстановиться.",
                ["Description.SetBonus.Researcher"] = "У вас есть шанс в <color=#65d615>{0}%</color> вернуть свой металлолом во время исследования.",
                ["Description.SetBonus.Smelting"] = "У вас есть шанс в <color=#65d615>{0}%</color> расплавить добываемую руду во время добычи.",
                ["Description.SetBonus.Survivalist"] = "Вы получаете <color=#65d615>{0}%</color> больше калорий и гидратации при приеме пищи и напитков.",
                ["Description.SetBonus.Feline"] = "Вы получаете <color=#65d615>{0}%</color> меньше урона при падении.",
                ["Description.SetBonus.Lead"] = "Вы получаете на <color=#65d615>{0}%</color> меньше урона от радиации.",
                ["Description.SetBonus.Gilled"] = "Вы получаете на <color=#65d615>{0}%</color> меньше урона от утопления.",
                ["Description.SetBonus.BonusMultiplier"] = "Увеличивает общий бонус на <color=#65d615>{0}%</color>.",
                ["Description.SetBonus.Smasher"] = "У вас есть шанс в <color=#65d615>{0}%</color> мгновенно уничтожить бочку или знак.",
                ["Description.SetBonus.WoodcuttersLuck"] = "У вас есть шанс в <color=#65d615>{0}%</color> получить случайный предмет при срубании дерева.",
                ["Description.SetBonus.MinersLuck"] = "У вас есть шанс в <color=#65d615>{0}%</color> получить случайный предмет при добыче узла.",
                ["Description.SetBonus.SkinnersLuck"] = "У вас есть шанс в <color=#65d615>{0}%</color> обнаружить случайный предмет в животе животного.",
                ["Description.SetBonus.FishersLuck"] = "У вас есть шанс в <color=#65d615>{0}%</color> обнаружить случайный предмет в животе рыбы.",
                ["Description.SetBonus.RockCycle"] = "У вас есть шанс в <color=#65d615>{0}%</color> мгновенно возродиться добываемый узел.",
                ["Description.SetBonus.Attractive"] = "У вас есть шанс в <color=#65d615>{0}%</color> того, что содержимое бочки появится в вашем инвентаре при уничтожении.",
                ["Description.SetBonus.FishingRodModifier"] = "Увеличивает прочность вашей удочки на <color=#65d615>{0}%</color>.",
                ["Description.SetBonus.TeamHeal"] = "Делится <color=#65d615>{0}%</color> лечения, которое вы получаете, с ближайшими членами команды.",
                ["Description.SetBonus.HealthShot"] = "Лечит союзников, поврежденных вами, на <color=#65d615>{0}%</color> от базового урона вашего оружия.",
                ["Description.SetBonus.BulletProof"] = "Уменьшает получаемый вами урон от пуль на <color=#65d615>{0}%</color>.",
                ["Description.SetBonus.UncannyDodge"] = "У вас есть шанс в <color=#65d615>{0}%</color> уклониться от снаряда и не получить урона.",
                ["Description.SetBonus.MaxHealth"] = "Ваш максимальный запас здоровья увеличивается на <color=#65d615>{0}%</color>.",
                ["Description.Buff.Miners"] = "Этот предмет увеличит вашу добычу при добыче на <color=#65d615>{0}%</color>.",
                ["Description.Buff.Lumberjacks"] = "Этот предмет увеличит вашу добычу древесины при рубке деревьев на <color=#65d615>{0}%</color>.",
                ["Description.Buff.Skinners"] = "Этот предмет увеличит вашу добычу при снятии шкурки на <color=#65d615>{0}%</color>.",
                ["Description.Buff.Farmers"] = "Этот предмет увеличит ваш урожай при сельском хозяйстве на <color=#65d615>{0}%</color>.",
                ["Description.Buff.Foragers"] = "Этот предмет увеличит ваш сбор несозревших ресурсов на <color=#65d615>{0}%</color>.",
                ["Description.Buff.Fishermans"] = "Этот предмет увеличит ваш улов при рыбалке на <color=#65d615>{0}%</color>.",
                ["Description.Buff.Assassins"] = "Этот предмет увеличит ваш урон в PvP на <color=#65d615>{0}%</color>.",
                ["Description.Buff.Demo"] = "Этот предмет уменьшит урон от взрывов на <color=#65d615>{0}%</color>.",
                ["Description.Buff.Elemental"] = "Этот предмет уменьшит урон от огня и холода на <color=#65d615>{0}%</color>.",
                ["Description.Buff.Scavengers"] = "Этот предмет даст вам шанс получить дополнительный лом из бочек и ящиков в <color=#65d615>{0}%</color> случаев.",
                ["Description.Buff.Transporters"] = "Этот предмет улучшит вашу экономию топлива на <color=#65d615>{0}%</color>.",
                ["Description.Buff.Crafters"] = "Этот предмет увеличит скорость вашего крафта на <color=#65d615>{0}%</color>.",
                ["Description.Buff.Reinforced"] = "Этот предмет уменьшит потерю прочности на <color=#65d615>{0}%</color>.",
                ["Description.Buff.Tamers"] = "Этот предмет уменьшит урон от животных на <color=#65d615>{0}%</color>.",
                ["Description.Buff.Hunters"] = "Этот предмет увеличит урон, наносимый животным, на <color=#65d615>{0}%</color>.",
                ["Description.Buff.Operators"] = "Этот предмет увеличит урон, наносимый ученым, на <color=#65d615>{0}%</color>.",
                ["Description.Buff.Jockeys"] = "Этот предмет увеличит вашу скорость верхом на <color=#65d615>{0}%</color>.",
                ["Description.Buff.Raiders"] = "У этого предмета есть шанс в <color=#65d615>{0}%</color> вернуть ваши взрывные предметы.",
                ["Description.Buff.Builders"] = "У этого предмета есть шанс в <color=#65d615>{0}%</color> вернуть ваши расходы на улучшение строений.",
                ["Description.Buff.Assemblers"] = "У этого предмета есть шанс в <color=#65d615>{0}%</color> вернуть ваши материалы при крафте.",
                ["Description.Buff.Knights"] = "Этот предмет уменьшит урон от ударов в ближнем бою на <color=#65d615>{0}%</color>.",
                ["Description.Buff.Barbarians"] = "Этот предмет увеличит урон от ваших атак в ближнем бою на <color=#65d615>{0}%</color>.",
                ["Description.Buff.Fabricators"] = "У этого предмета есть шанс в <color=#65d615>{0}%</color> изготовить дополнительный предмет при крафте.",
                ["Description.Buff.Medics"] = "Этот предмет увеличит лечение на <color=#65d615>{0}%</color>.",
                ["UILeftSideTierString"] = "\n \u0020 <size=12>- <color=#B71BA9>{0}</color>:</size> <size=8>{1}</size>",
                ["UILeftSidePerm"] = "\n \u0020 <size=12>- <color=#B71BA9>Perm</color>:</size> <size=8>{0}</size>",
                ["CraftedProc"] = "Вы смогли создать улучшенный предмет: <color={0}>{1}</color>.",
                ["eladdskinusage"] = "Использование: /eladdskin <тип комплекта> <краткое имя предмета> <ID скина>\nПример: /eladdskin Transporters штаны 2533474346",
                ["eladdskininvalidbuff"] = "{0} не является допустимым баффом.",
                ["eladdskininvalidshortname"] = "Недопустимое краткое имя предмета {0}",
                ["eladdskininvalidskinid"] = "Идентификаторы скинов должны содержать только числа.",
                ["eladdskinsuccess"] = "Настроена статическая кожа для {0} [{1}] в разделе {2}.",
                ["UIOn"] = "<color=#00ab08>ВКЛ</color>",
                ["UIOff"] = "<color=#bf2405>ВЫКЛ</color>",
                ["elsettingsnotificationsoff"] = "Теперь вы не будете получать уведомления, касающиеся Epic Loot.",
                ["elsettingsnotificationson"] = "Теперь вы будете получать уведомления, касающиеся Epic Loot.",
                ["elhudoff"] = "Ваша кнопка HUD была отключена. Вы должны использовать <color=#65d615>/{0}</color>",
                ["elhudon"] = "Ваша кнопка HUD была включена.",
                ["BoatSpeedBoosted"] = "Скорость лодки увеличена благодаря вашему бонусу комплекта [Скорость: <color=#ffae00>{0}</color>].",
                ["FirstFindLoot"] = "Вы нашли предмет <color=#ffae00>EpicLoot</color>! Когда он будет экипирован или удерживаем, этот предмет предоставит вам бафф <color=#ffae00>{0}</color>. Бафф станет более мощным и разблокирует бонусы комплекта, когда вы экипируете несколько предметов с тем же типом баффа.\n\n{1}\n\nНажмите на значок или введите <color=#ffae00>/{2}</color> для доступа к меню.",
                ["Notify_FirstFindLoot"] = "Вы нашли предмет EpicLoot!\nПроверьте свой чат для получения дополнительной информации.",
                ["UIEquipmentEnhancerTitle"] = "Улучшение снаряжения",
                ["UIEquipmentEnhancerDescription"] = "Щелкните по предмету, который вы хотите улучшить.",
                ["UISalvagerTitleConfirm"] = "УТИЛИЗАЦИЯ УЛУЧШЕННОГО СНАРЯЖЕНИЯ",
                ["UISalvagerClickAnItem"] = "Щелкните по предмету, чтобы проверить его стоимость утилизации.",
                ["UISalvageDescription"] = "Утилизация этого предмета принесет вам <color=#42f105>{0} {1}</color>.",
                ["UISalvageButton"] = "<color=#ffb600>УТИЛИЗИРОВАТЬ</color>",
                ["UIEnhancerSelectEnhancement"] = "Выберите желаемое улучшение для вашего {0}.",
                ["UIEnhancerTitle"] = "УЛУЧШЕНИЕ СНАРЯЖЕНИЯ",
                ["UIEnhanceConfirmation"] = "Вы улучшаете свой <color=#ffec00>{0}</color> с улучшением <color=#ffec00>{1}</color>.",
                ["UIItem"] = "ПРЕДМЕТ:",
                ["UICost"] = "СТОИМОСТЬ:",
                ["UIEnhancementConfirmationDescription"] = "Улучшение этого предмета придаст ему улучшение <color=#ffd800>{0}</color> с случайным уровнем и модификатором.",
                ["ItemMissing"] = "Предмет больше не существует!",
                ["UIEnhance"] = "УЛУЧШИТЬ",
                ["NoDrinkTea"] = "Вы не можете пить чай с максимальным бонусом здоровья!",
                ["eladdskinusagemessage"] = "Использование: /eladdskin <тип комплекта> <краткое имя предмета> <ID скина>\nПример: /eladdskin Transporters штаны 2533474346",
                ["ScrapperReceivedCurrency"] = "Вы получили {0} {1}.",
                ["CantAffordEnhancement"] = "У вас недостаточно {0} для применения этого улучшения."
            };

            List<Buff> buffs = Pool.GetList<Buff>();
            buffs.AddRange(Enum.GetValues(typeof(Buff)).Cast<Buff>());
            foreach (var buff in buffs)
            {
                var buffString = buff.ToString();
                if (!language.ContainsKey(buffString)) language.Add(buffString, buffString);
            }
            Pool.FreeList(ref buffs);

            lang.RegisterMessages(language, this);
        }

        #endregion

        #region Hooks

        object OnItemSkinChange(int num, Item item, RepairBench bench, BasePlayer player)
        {
            if (item.name != null && GetBuff(item.name) != Buff.None)
            {
                if (config.general_settings.prevent_reskin) return true;
                string name = item.name;
                NextTick(() =>
                {
                    if (bench != null && bench.inventory != null && bench.inventory.itemList != null && bench.inventory.itemList.Count > 0)
                    {
                        var _item = bench.inventory.itemList.First();
                        _item.name = name;
                        _item.MarkDirty();
                    }
                });
            }
            return null;
        }

        object CanRecycle(Recycler recycler, Item item)
        {
            if (item.name != null && GetBuff(item.name) != Buff.None)
            {
                string tier = GetBuffTier(item.name);
                if (string.IsNullOrEmpty(tier)) return null;
                int amount;
                if (!config.scrapper_settings.scrapper_value.TryGetValue(tier, out amount)) return null;
                item.RemoveFromContainer();
                var payment = ItemManager.CreateByName(config.scrapper_settings.currency_shortname, Math.Max(amount, 1), config.scrapper_settings.currency_skin);
                if (payment == null) return null;
                if (!string.IsNullOrEmpty(config.scrapper_settings.currency_name)) payment.name = config.scrapper_settings.currency_name;
                recycler.MoveItemToOutput(payment);
                NextTick(() => item.Remove());
                return null;
            }
            return null;
        }

        static bool IsEpicScrap(Item item)
        {
            // We check to see if the item.name field is null or empty. If it contains something, we check it against the currency name. If they don't match, we return false because the item is not Epic Scrap.
            if (!string.IsNullOrEmpty(item.name) && !item.name.Equals(Instance.config.scrapper_settings.currency_name, StringComparison.OrdinalIgnoreCase)) return false;
            
            // We check the item's skin against the value set for Epic scrap. If they don't match, we return false because the item is not epic scrap.
            if (item.skin != Instance.config.scrapper_settings.currency_skin) return false;

            // We check the item's shortname against the value set for Epic scrap. If they don't match, we return false because the item is not epic scrap.
            if (!item.info.shortname.Equals(Instance.config.scrapper_settings.currency_shortname, StringComparison.OrdinalIgnoreCase)) return false;

            // We confirm the item is Epic scrap because the shortname, skin and name fields all match our configured values.
            return true;
        }

        public string CurrencyName;

        string icon_img;
        ulong icon_id;
        string icon_path;

        void OnServerInitialized(bool initial)
        {
            if (!string.IsNullOrEmpty(config.button_info.icon_path))
            {
                if (config.button_info.icon_path.StartsWith("http"))
                {
                    if (ImageLibrary == null || !ImageLibrary.IsLoaded)
                    {
                        Puts("You must have ImageLibrary installed if you wish to use http images for the button icon.");
                        Interface.Oxide.UnloadPlugin(Name);
                    }
                    else
                    {
                        Puts($"Caching {config.button_info.icon_path}");
                        icon_img = "url_icon";
                        ImageLibrary?.Call("AddImage", config.button_info.icon_path, icon_img);
                    }
                }
                else if (config.button_info.icon_path.IsNumeric()) icon_id = Convert.ToUInt64(config.button_info.icon_path);
                else if (config.button_info.icon_path.StartsWith("assets/icons/")) icon_path = config.button_info.icon_path;
                else Puts("Failed to get the button icon - using default.");
            }
            else Puts("No path specified for the button - using default.");

            GetEnhanceableItems();
            bool updated = false;
            if (config.general_settings.auto_update_sets)
            {
                foreach (var set in Default_Tier_Values)
                {
                    if (!config.enhancements.ContainsKey(set.Key))
                    {
                        Puts($"Adding new set: {set.Key}");
                        config.enhancements.Add(set.Key, set.Value);
                        updated = true;
                    }
                    else if (config.general_settings.auto_update_set_bonuses)
                    {
                        foreach (var entry in set.Value.setInfo)
                        {
                            foreach (var bonus in entry.Value.setBonus)
                            {
                                EnhancementInfo ei;
                                if (!config.enhancements.TryGetValue(set.Key, out ei) || !ei.setInfo.ContainsKey(entry.Key)) continue;
                                var value = config.enhancements[set.Key].setInfo[entry.Key];
                                if (!value.setBonus.ContainsKey(bonus.Key))
                                {
                                    updated = true;
                                    value.setBonus.Add(bonus.Key, bonus.Value);
                                    Puts($"Added new SetBonus to: {set.Key}/{entry.Key}/Key: {bonus.Key} - Value: {bonus.Value.modifier}");
                                }
                            }
                        }
                    }
                }
            }

            if (config.item_skins == null || config.item_skins.Count == 0)
            {
                config.item_skins = DefaultItemSkins();
                updated = true;
            }

            if (config.scrapper_settings.scrapper_value.Count == 0)
            {
                config.scrapper_settings.scrapper_value = DefaultScrapperTierValue;
                updated = true;
            }

            if (config.scrapper_settings.enhancement_cost.Count == 0)
            {
                config.scrapper_settings.enhancement_cost = DefaultEnhancementCost();
                updated = true;
            }

            if (config.containers.barrels.Count == 0)
            {
                config.containers.barrels = Default_Barrel_Spawns;
                updated = true;
            }

            if (config.raidablebases_settings.base_settings.Count == 0)
            {
                config.raidablebases_settings.base_settings = DefaultRaidables;
                updated = true;
            }

            if (config.raidablebases_settings.container_whitelist.Count == 0)
            {
                config.raidablebases_settings.container_whitelist = DefaultRaidContainerWhitelist;
                updated = true;
            }

            if (!config.raidablebases_settings.enabled) Unsubscribe(nameof(OnRaidableBaseStarted));

            List<Buff> buffs = Pool.GetList<Buff>();
            buffs.AddRange(Enum.GetValues(typeof(Buff)).Cast<Buff>().Intersect(config.enhancements.Keys));

            if (buffs != null)
            {
                var page = 0;
                foreach (var buff in buffs)
                {
                    StaticBuffs.Add(buff.ToString());
                    if (!parsedEnums.ContainsKey(buff.ToString())) parsedEnums.Add(buff.ToString(), buff);
                    if (buff == Buff.None) continue;
                    // Handle pages
                    if (!Buff_Info_Pages.ContainsKey(page)) Buff_Info_Pages.Add(page, new List<Buff>() { buff });
                    else
                    {
                        if (Buff_Info_Pages[page].Count < 6) Buff_Info_Pages[page].Add(buff);
                        else
                        {
                            page++;
                            Buff_Info_Pages.Add(page, new List<Buff>() { buff });
                        }
                    }
                }
                Puts($"Setup {Buff_Info_Pages.Count} pages of buffs.");
            }
            Pool.FreeList(ref buffs);
            
            if (config.mission_rewards == null)
            {
                config.mission_rewards = Default_Mission_Rewards;
                updated = true;
            }

            if (config.corpses.corpses.Count == 0)
            {
                config.corpses = DefaultCorpseProfile;
                updated = true;
            }

            if (config.general_settings.perm_chance_modifier_loot.Count == 0)
            {
                config.general_settings.perm_chance_modifier_loot.Add("epicloot.vip", 1);
                updated = true;
            }

            if (config.general_settings.perm_chance_modifier_craft.Count == 0)
            {
                config.general_settings.perm_chance_modifier_craft.Add("epicloot.vip", 1);
                updated = true;
            }

            foreach (var perm in config.general_settings.perm_chance_modifier_loot)
            {
                if (!permission.PermissionExists(perm.Key, this))
                    permission.RegisterPermission(perm.Key, this);
            }

            foreach (var perm in config.general_settings.perm_chance_modifier_craft)
            {
                if (!permission.PermissionExists(perm.Key, this))
                    permission.RegisterPermission(perm.Key, this);
            }

            if (!config.mission_rewards.enabled) Unsubscribe("OnMissionSucceeded");
            Unsubscribe("OnBonusItemDropped");

            if (config.containers.containers.Count == 0)
            {
                config.containers = DefaultProfile;
                updated = true;
            }

            if (config.fishing_contest_api.shortnames.Count == 0)
            {
                config.fishing_contest_api.shortnames = DefaultFishingContestShortnames;
                updated = true;
            }

            if (config.fishing_contest_api.buffs.Count == 0)
            {
                config.fishing_contest_api.buffs = DefaultFishingContestBuffs;
                updated = true;
            }

            if (config.general_settings.ui_commands == null || config.general_settings.ui_commands.Count == 0)
            {
                config.general_settings.ui_commands = new List<string>() { "el", "elinspect" };
                updated = true;
            }

            if (updated) SaveConfig();

            foreach (var _command in config.general_settings.ui_commands)
            {
                cmd.AddChatCommand(_command, this, "OpenInspectMenuChatCMD");
                cmd.AddConsoleCommand(_command, this, "OpenInspectMenu");
            }

            foreach (var type in config.corpses.corpses)
            {
                if (!npc_corpses.ContainsKey(type.Key)) npc_corpses.Add(type.Key, new List<LootableCorpse>());
            }

            // Cycle through and add bonuses to players based on what they are wearing.
            if (BasePlayer.activePlayerList != null)
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    SetupBonuses(player);
                    PlayerSettings pi;
                    if (!pcdData.pEntity.TryGetValue(player.userID, out pi) || pi.showHud) SendInspectMenuButton(player);
                    UpdateModifiers(player);
                }
            }

            if (string.IsNullOrEmpty(config.scrapper_settings.currency_name))
            {
                CurrencyName = ItemManager.FindItemDefinition(config.scrapper_settings.currency_shortname)?.displayName.english?.TitleCase() ?? config.scrapper_settings.currency_shortname;
            }
            else CurrencyName = config.scrapper_settings.currency_name.TitleCase();

            if (!config.truepve_settings.allow_healthshot_in_pve || TruePVE == null || !TruePVE.IsLoaded)
            {
                // Unsubs CanEntityTakeDamage if healthshot isn't allowed in truepve.
                Unsubscribe("CanEntityTakeDamage");
            }

            if (config.skilltree_settings.enabled) NextTick(() => AddNodeToSkillTree());

            if (!config.bossmonster_settings.enabled) Unsubscribe(nameof(OnBossKilled));
        }

        void OnServerSave()
        {
            SaveData();
        }

        // Triggered when clothing is equipped.
        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (container == null || item == null) return;
            var player = container.GetOwnerPlayer();
            if (player == null || player.IsNpc || !player.userID.IsSteamId() || !permission.UserHasPermission(player.UserIDString, perm_use) || !player.IsConnected) return;
            if (player.inventory.containerWear == container)
            {
                UpdateBonuses(player, item, true);
                UpdateModifiers(player);
            }
        }

        // Triggered when clothing is removed.
        void OnItemRemovedFromContainer(ItemContainer container, Item item)
        {
            if (container == null) return;
            var player = container.GetOwnerPlayer();
            if (player == null || player.IsNpc || !player.userID.IsSteamId() || !permission.UserHasPermission(player.UserIDString, perm_use) || !player.IsConnected) return;

            if (player.inventory?.containerWear == container)
            {
                UpdateBonuses(player, item, false);
                UpdateModifiers(player);
            }
        }

        object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (player == null || player.IsNpc || !player.userID.IsSteamId()) return null;
            if (!permission.UserHasPermission(player.UserIDString, perm_use)) return null;
            DestroyUIs(player);
            RemoveBonuses(player);
            return null;
        }

        void OnPlayerRespawned(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, perm_use)) return;
            PlayerSettings pi;
            if (!pcdData.pEntity.TryGetValue(player.userID, out pi) || pi.showHud) SendInspectMenuButton(player);
        }

        void OnUserPermissionGranted(string id, string permName) => UpdatePlayerPerms(id, permName);
        void OnUserPermissionRevoked(string id, string permName) => UpdatePlayerPerms(id, permName);

        void OnGroupPermissionGranted(string name, string perm) => UpdateGroupPlayerPerms(name, perm);
        void OnGroupPermissionRevoked(string name, string perm) => UpdateGroupPlayerPerms(name, perm);

        void UpdatePlayerPerms(string id, string permName)
        {
            if (permName.Equals(perm_use, StringComparison.OrdinalIgnoreCase))
            {
                var player = BasePlayer.activePlayerList?.FirstOrDefault(x => x.UserIDString == id);
                if (permission.UserHasPermission(id, permName))
                {
                    if (player != null)
                    {
                        PlayerSettings pi;
                        if (!pcdData.pEntity.TryGetValue(player.userID, out pi) || pi.showHud) SendInspectMenuButton(player);
                    }
                }
                else
                {
                    if (player != null)
                    {
                        player_attributes.Remove(player.userID);
                        RemovePerms(player.userID);
                        DestroyUIs(player);
                    }
                }
            }
        }

        void UpdateGroupPlayerPerms(string group, string permName)
        {
            if (permName.Equals(perm_use, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var player in permission.GetUsersInGroup(group))
                {
                    UpdatePlayerPerms(player.Split(' ')[0], permName);
                }
            }
        }

        void RemovePerms(ulong id)
        {
            List<string> perms;
            if (Perms_tracker.TryGetValue(id, out perms))
            {
                List<string> perms_to_remove = Pool.GetList<string>();
                foreach (var perm in perms)
                {
                    perms_to_remove.Add(perm);
                    permission.RevokeUserPermission(id.ToString(), perm);
                }
                foreach (var perm in perms_to_remove)
                {
                    perms.Remove(perm);
                }
                Pool.FreeList(ref perms_to_remove);
            }
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            player_attributes.Remove(player.userID);
            RemovePerms(player.userID);
            foreach (var mod in player.modifiers.All)
            {
                if (mod.Type == Modifier.ModifierType.Max_Health && mod.Duration > 1200f)
                {
                    RemoveMaxHealth(player);
                    break;
                }
            }
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (!player.IsDead())
            {
                SetupBonuses(player);
                PlayerSettings pi;
                if (!pcdData.pEntity.TryGetValue(player.userID, out pi) || pi.showHud) SendInspectMenuButton(player);
            }
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (player == null || player.IsDead() || player.IsNpc || !player.userID.IsSteamId()) return;
            try
            {
                SetupBonuses(player);
            }
            catch { }
            PlayerSettings pi;
            if (!pcdData.pEntity.TryGetValue(player.userID, out pi) || pi.showHud) SendInspectMenuButton(player);
        }

        object OnPlayerHealthChange(BasePlayer player, float oldValue, float newValue)
        {            
            if (player == null || player.IsNpc || !player.userID.IsSteamId()) return null;
            if (oldValue > newValue) return null;
            // player has healed

            try
            {
                var bonus = GetTotalBuffValue(player, Buff.Medics);
                if (bonus > 0)
                {
                    Unsubscribe("OnPlayerHealthChange");
                    var additionalHealth = (newValue - oldValue) * bonus;
                    player.health += additionalHealth;
                    //player.Heal(additionalHealth);
                    Subscribe("OnPlayerHealthChange");
                }
            }
            catch
            {
                LogToFile("OnPlayerHealthChangeFailure", $"[{DateTime.Now}] Encountered an exception when using the Medics buff.", this);
            }

            string DebugString = string.Empty;
            List<BasePlayer> hitPlayers = null;
            try
            {
                var setBonuses = GetSetBonuses(player);
                SetBonusValues values;
                if (setBonuses != null && setBonuses.TryGetValue(SetBonus.TeamHeal, out values) && player.Team != null)
                {
                    DebugString += $"Found values for TeamHeal. [Team is null: {player.Team == null}]";
                    hitPlayers = FindEntitiesOfType<BasePlayer>(player.transform.position, config.general_settings.TeamHealRadius, 1 << 17);
                    DebugString += "Found players around our player. ";
                    foreach (var hit in hitPlayers)
                    {
                        if (hit == player) continue;
                        if (hit.Team == null) continue;
                        DebugString += $"Checking player: {hit?.displayName} (Team is null: {hit.Team == null}). ";
                        //if (hit.Team.teamID == player.Team.teamID) hit.Heal((newValue - oldValue) * setBonuses[SetBonus.TeamHeal].modifier);
                        if (hit.Team?.teamID == player.Team.teamID) hit.health += (newValue - oldValue) * values.modifier;
                        DebugString += $"Finished check on hit.";
                    }
                    Pool.FreeList(ref hitPlayers);
                }
            }
            catch
            {
                if (hitPlayers != null) Pool.FreeList(ref hitPlayers);
                LogToFile("OnPlayerHealthChangeFailure", $"[{DateTime.Now}] Encountered an exception when using the TeamHeal set bonus. Error string: {DebugString}", this);
            }

            return null;
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

        object OnDispenserGather(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            return HandleDispenser(dispenser, player, item, false);
        }

        object OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            return HandleDispenser(dispenser, player, item, true);
        }

        object HandleDispenser(ResourceDispenser dispenser, BasePlayer player, Item item, bool bonus)
        {
            if (dispenser == null || player == null || item == null || dispenser.containedItems == null) return null;
            var setBonuses = GetSetBonuses(player);
            SetBonusValues setbonusValues;
            if (dispenser.gatherType == ResourceDispenser.GatherType.Tree)
            {
                var modifier = GetTotalBuffValue(player, Buff.Lumberjacks);
                var cutOut = false;
                item.amount += Convert.ToInt32(item.amount * modifier);
                if (!bonus && setBonuses != null && setBonuses.TryGetValue(SetBonus.InstantWoodcutting, out setbonusValues) && RollSuccessful(setbonusValues.modifier))
                {
                    foreach (var r in dispenser.containedItems)
                    {
                        if (r.amount < 1) continue;
                        var amount_to_give = Convert.ToInt32(Math.Round(r.amount + (r.amount * modifier), 0, MidpointRounding.AwayFromZero));
                        if (r.itemDef.shortname == item.info.shortname) item.amount += amount_to_give;
                        else if (amount_to_give >= 1) player.GiveItem(ItemManager.CreateByName(r.itemDef.shortname, amount_to_give));
                        r.amount = 0;
                    }
                    cutOut = true;
                }

                if ((bonus || cutOut) && setBonuses != null && setBonuses.TryGetValue(SetBonus.Regrowth, out setbonusValues) && RollSuccessful(setbonusValues.modifier))
                {
                    var newTree = GameManager.server.CreateEntity(dispenser.baseEntity.PrefabName, dispenser.baseEntity.transform.position, dispenser.baseEntity.transform.rotation);
                    newTree.Spawn();
                    if (MessagesOn(player)) PrintToChat(player, lang.GetMessage("RegrowProc", this, player.UserIDString));
                }
                if ((bonus || cutOut) && setBonuses != null && setBonuses.TryGetValue(SetBonus.WoodcuttersLuck, out setbonusValues) && RollSuccessful(setbonusValues.modifier))
                {
                    DropTableLuck dt;
                    if (config.luck_drop_table.TryGetValue(SetBonus.WoodcuttersLuck, out dt))
                    {
                        var def = GetRandomDrop(dt.dropTable);
                        var droppedItem = ItemManager.CreateByName(def.shortname, UnityEngine.Random.Range(Math.Max(def.min_quantity, 1), Math.Max(def.max_quantity, 1) + 1), def.skin);
                        if (def.name != null)
                        {
                            droppedItem.name = def.name;
                            droppedItem.MarkDirty();
                        }
                        player.GiveItem(droppedItem);
                    }
                }
            }
            else if (dispenser.gatherType == ResourceDispenser.GatherType.Ore)
            {
                // Bonus modifier is handled by GetTotalBuffValue.
                var modifier = GetTotalBuffValue(player, Buff.Miners);
                item.amount += Convert.ToInt32(item.amount * modifier);
                bool minedOut = false;
                if (!bonus && setBonuses != null && setBonuses.TryGetValue(SetBonus.InstantMining, out setbonusValues) && RollSuccessful(setbonusValues.modifier))
                {
                    foreach (var r in dispenser.containedItems)
                    {
                        if (r.amount < 1) continue;
                        var amount_to_give = Convert.ToInt32(Math.Round(r.amount + (r.amount * modifier), 0, MidpointRounding.AwayFromZero));
                        if (r.itemDef.shortname == item.info.shortname) item.amount += amount_to_give;
                        else if (amount_to_give >= 1) player.GiveItem(ItemManager.CreateByName(r.itemDef.shortname, amount_to_give));
                        r.amount = 0;
                    }
                    minedOut = true;
                }
                //var leftOver = dispenser.containedItems.Sum(x => x.amount);
                if ((bonus || minedOut) && setBonuses != null && setBonuses.TryGetValue(SetBonus.RockCycle, out setbonusValues) && RollSuccessful(setbonusValues.modifier))
                {
                    var newRock = GameManager.server.CreateEntity(dispenser.baseEntity.PrefabName, dispenser.baseEntity.transform.position, dispenser.baseEntity.transform.rotation);
                    newRock.Spawn();
                    if (MessagesOn(player)) PrintToChat(player, lang.GetMessage("RockcycleProc", this, player.UserIDString));
                }

                // Needs to be last as it removes the item.
                if (setBonuses != null && setBonuses.ContainsKey(SetBonus.Smelting) && RollSuccessful(setBonuses[SetBonus.Smelting].modifier))
                {
                    var refined = GetRefinedMaterial(item.info.shortname);
                    if (!string.IsNullOrEmpty(refined))
                    {
                        player.GiveItem(ItemManager.CreateByName(refined, Math.Max(item.amount, 1)));
                        item.amount = 0;
                        item.Remove();
                    }
                }
                if ((bonus || minedOut) && setBonuses != null && setBonuses.TryGetValue(SetBonus.MinersLuck, out setbonusValues) && RollSuccessful(setbonusValues.modifier))
                {
                    DropTableLuck dt;
                    if (config.luck_drop_table.TryGetValue(SetBonus.MinersLuck, out dt))
                    {                        
                        var def = GetRandomDrop(dt.dropTable);
                        var amount = UnityEngine.Random.Range(Math.Max(def.min_quantity, 1), Math.Max(def.max_quantity, 1) + 1);
                        var droppedItem = ItemManager.CreateByName(def.shortname, amount, def.skin);
                        if (def.name != null)
                        {
                            droppedItem.name = def.name;
                            droppedItem.MarkDirty();
                        }
                        player.GiveItem(droppedItem);
                    }
                }
            }
            else if (dispenser.gatherType == ResourceDispenser.GatherType.Flesh)
            {
                bool isFinalHit = dispenser.containedItems.FirstOrDefault(x => x.amount > 0) == null;
                var modifier = GetTotalBuffValue(player, Buff.Skinners);
                item.amount += Convert.ToInt32(item.amount * modifier);
                if (setBonuses != null && setBonuses.TryGetValue(SetBonus.InstantSkinning, out setbonusValues) && RollSuccessful(setbonusValues.modifier))
                {
                    foreach (var r in dispenser.containedItems)
                    {
                        if (r.amount < 1) continue;
                        var amount_to_give = Convert.ToInt32(Math.Round(r.amount + (r.amount * modifier), 0, MidpointRounding.AwayFromZero));
                        if (r.itemDef.shortname == item.info.shortname) item.amount += amount_to_give;
                        else player.GiveItem(ItemManager.CreateByName(r.itemDef.shortname, Math.Max(amount_to_give, 1)));
                        r.amount = 0;
                        isFinalHit = true;
                    }
                }
                // Needs to be last as it removes and replaces the item.
                if (setBonuses != null && setBonuses.TryGetValue(SetBonus.InstantCook, out setbonusValues) && RollSuccessful(setbonusValues.modifier))
                {
                    var cooked = GetCookedMeat(item.info.shortname);
                    if (!string.IsNullOrEmpty(cooked))
                    {
                        player.GiveItem(ItemManager.CreateByName(cooked, Math.Max(item.amount, 1)));
                        item.amount = 0;
                        item.Remove();
                    }
                }
                if (isFinalHit && setBonuses != null && setBonuses.TryGetValue(SetBonus.SkinnersLuck, out setbonusValues) && RollSuccessful(setbonusValues.modifier))
                {
                    DropTableLuck dt;
                    if (config.luck_drop_table.TryGetValue(SetBonus.SkinnersLuck, out dt))
                    {
                        var def = GetRandomDrop(dt.dropTable);
                        var droppedItem = ItemManager.CreateByName(def.shortname, UnityEngine.Random.Range(Math.Max(def.min_quantity, 1), Math.Max(def.max_quantity, 1) + 1), def.skin);
                        if (def.name != null)
                        {
                            droppedItem.name = def.name;
                            droppedItem.MarkDirty();
                        }
                        player.GiveItem(droppedItem);
                    }
                }
            }

            return null;
        }

        DropTable GetRandomDrop(List<DropTable> drops)
        {
            var roll = UnityEngine.Random.Range(0, drops.Sum(x => x.drop_weight) + 1);
            var count = 0;

            foreach (var drop in drops)
            {
                count += drop.drop_weight;
                if (roll <= count)
                {
                    return drop;
                }
            }
            // Failed to find the item using drop weight so we return a random one.
            return drops.GetRandom();
        }

        void OnGrowableGathered(GrowableEntity plant, Item item, BasePlayer player)
        {
            var modifier = GetTotalBuffValue(player, Buff.Farmers); // Handles set bonus buff modifier as well.
            item.amount += Convert.ToInt32(item.amount * modifier); // May need to change it so it spawns new items. 

        }

        void OnCollectiblePickup(CollectibleEntity entity, BasePlayer player, bool eat)
        {
            if (player == null || entity == null || player.IsNpc || !player.userID.IsSteamId() || entity.itemList == null) return;
            var modifier = GetTotalBuffValue(player, Buff.Foragers);
            
            foreach (var item in entity.itemList)
            {
                item.amount += Convert.ToInt32(item.amount * modifier);
            }
        }

        void OnFishingStopped(BaseFishingRod fishingRod, BaseFishingRod.FailReason reason)
        {
            if (fishingRod.skinID == 0) fishingRod.GlobalStrainSpeedMultiplier = 0.75f;
        }

        object CanCastFishingRod(BasePlayer player, BaseFishingRod fishingRod, Item lure)
        {
            if (player == null || fishingRod == null) return null;
            if (fishingRod.skinID == 0)
            {
                var setBonuses = GetSetBonuses(player);
                SetBonusValues sbv;
                if (setBonuses != null && setBonuses.TryGetValue(SetBonus.FishingRodModifier, out sbv))
                {
                    fishingRod.GlobalStrainSpeedMultiplier = 0.75f - (0.75f * sbv.modifier);
                }
            }
            return null;
        }

        object CanCatchFish(BasePlayer player, BaseFishingRod rod, Item item)
        {
            if (rod.skinID == 0) rod.GlobalStrainSpeedMultiplier = 0.75f;

            var modifier = GetTotalBuffValue(player, Buff.Fishermans); // Handles set bonus buff modifier as well.

            if (modifier > 0)
            {
                var count = 0;
                var additionalFish = 0;
                for (int i = 0; i < 999; i++)
                {
                    count++;
                    if (modifier >= count) additionalFish++;
                    else
                    {
                        if (UnityEngine.Random.Range(0, 100 + 1) >= 100 - modifier) additionalFish++;
                        break;
                    }
                }

                item.amount += additionalFish;
            }                

            var setBonuses = GetSetBonuses(player);
            DropTableLuck dt;
            SetBonusValues setbonusValues;
            if (setBonuses != null && setBonuses.TryGetValue(SetBonus.FishersLuck, out setbonusValues) && RollSuccessful(setbonusValues.modifier) && config.luck_drop_table.TryGetValue(SetBonus.FishersLuck, out dt))
            {
                var def = GetRandomDrop(dt.dropTable);
                var droppedItem = ItemManager.CreateByName(def.shortname, UnityEngine.Random.Range(Math.Max(def.min_quantity, 1), Math.Max(def.max_quantity, 1) + 1), def.skin);
                if (def.name != null)
                {
                    droppedItem.name = def.name;
                    droppedItem.MarkDirty();
                }
                player.GiveItem(droppedItem);
            }

            return null;
        }

        object OnEntityTakeDamage(BasePlayer victim, HitInfo info)
        {
            if (victim == null || info == null || info.damageTypes == null) return null;
            float modifiedDamageScale = 1f;
            var damageType = info.damageTypes.GetMajorityDamageType();
            if (damageType != Rust.DamageType.Generic)
            {
                float damageModifier = 0f;
                switch (damageType)
                {
                    case Rust.DamageType.Heat:
                    case Rust.DamageType.ColdExposure:
                    case Rust.DamageType.Cold:
                    case Rust.DamageType.Drowned:
                        HandleDamageType(victim, info, damageType, out damageModifier);
                        info.damageTypes.ScaleAll(modifiedDamageScale - damageModifier < 0 ? 0 : modifiedDamageScale - damageModifier);
                        return null;
                    case Rust.DamageType.Fall:
                    case Rust.DamageType.Explosion:
                    case Rust.DamageType.Blunt:
                    case Rust.DamageType.Radiation:
                    case Rust.DamageType.RadiationExposure:
                        HandleDamageType(victim, info, damageType, out damageModifier);
                        break;
                    case Rust.DamageType.Poison:
                    case Rust.DamageType.Hunger:
                    case Rust.DamageType.Suicide:
                        return null;
                }
                modifiedDamageScale -= damageModifier;
            }
            var attacker = info.Initiator;
            if (attacker != null)
            {
                if (attacker is BasePlayer && attacker != victim)
                {
                    var attackerPlayer = attacker as BasePlayer;
                    if (attackerPlayer.IsNpc || !attackerPlayer.userID.IsSteamId()) modifiedDamageScale = HandleHumanNPCHitPlayer(victim, attackerPlayer, info, modifiedDamageScale);
                        
                    else if (victim.IsNpc || !victim.userID.IsSteamId())
                    {
                        modifiedDamageScale = HandlePlayerHitNPC(victim, attackerPlayer, info, modifiedDamageScale);
                    }
                    else
                    {
                        bool IsHealthShot;
                        modifiedDamageScale = HandlePlayerHitPlayer(victim, attackerPlayer, info, modifiedDamageScale, out IsHealthShot);
                        if (IsHealthShot)
                        {
                            info.damageTypes.ScaleAll(modifiedDamageScale);
                            return null;
                        }
                    }
                }
                else if (attacker is BaseAnimalNPC)
                {
                    if (!victim.IsNpc && victim.userID.IsSteamId()) modifiedDamageScale = HandleAnimalHitPlayer(victim, attacker as BaseAnimalNPC, info, modifiedDamageScale);
                }
            }
            if (modifiedDamageScale < 0) modifiedDamageScale = 0;
            info.damageTypes.ScaleAll(modifiedDamageScale);

            return null;
        }

        object OnEntityTakeDamage(BaseAnimalNPC animal, HitInfo info)
        {
            if (animal == null || info == null) return null;
            var attacker = info.InitiatorPlayer;
            if (attacker != null && !attacker.IsNpc && attacker.userID.IsSteamId())
            {
                return HandlePlayerHitAnimal(animal, attacker, info);
            }
            return null;
        }

        object OnEntityTakeDamage(LootContainer entity, HitInfo info)
        {
            if (entity == null || info == null || info.damageTypes == null) return null;
            var player = info.InitiatorPlayer;
            BuffSetInfo bsi;
            if (player == null || player.IsNpc || !player.userID.IsSteamId() || !player_attributes.TryGetValue(player.userID, out bsi)) return null;

            SetBonusValues values;
            if (bsi.setBonuses.TryGetValue(SetBonus.Smasher, out values) && RollSuccessful(values.modifier)) info.damageTypes.ScaleAll(200);

            if (info.damageTypes.Total() >= entity.health && permission.UserHasPermission(player.UserIDString, perm_drop))
            {
                // Handle barrel roll
                float chance;
                if (config.containers.barrels.TryGetValue(entity.ShortPrefabName, out chance))
                {
                    if (config.containers.use_default_loot_table)
                    {
                        if (UnityEngine.Random.Range(0f, 100f) >= 100f - chance)
                        {
                            var shortnames = GetLootTable(entity);
                            if (shortnames != null)
                            {
                                var item = GenerateRandomItem(null, shortnames);
                                if (item != null)
                                {
                                    entity.inventory.capacity++;
                                    entity.inventorySlots++;

                                    if (!item.MoveToContainer(entity.inventory)) item.Remove();
                                }
                            }
                        }
                    }
                    else
                    {
                        if (UnityEngine.Random.Range(0f, 100f) >= 100f - chance)
                        {
                            var item = GenerateRandomItem();
                            if (item != null)
                            {
                                entity.inventory.capacity++;
                                entity.inventorySlots++;
                                if (!item.MoveToContainer(entity.inventory)) item.Remove();
                            }
                        }
                    }
                }

                BuffAttributes buffmods;
                if (bsi.buffs.TryGetValue(Buff.Scavengers, out buffmods) && RollSuccessful(buffmods.totalModifier))
                {
                    var scrap = ItemManager.CreateByName("scrap", UnityEngine.Random.Range(Math.Max(config.scavanger_info.Min_extra_scrap, 1), Math.Max(config.scavanger_info.Max_extra_scrap, 1) + 1));
                    if (!scrap.MoveToContainer(entity.inventory)) scrap.Drop(entity.transform.position, Vector3.zero);
                }

                if (bsi.setBonuses.TryGetValue(SetBonus.Attractive, out values) && RollSuccessful(values.modifier))
                {
                    var activeItem = player.GetHeldEntity();
                    if ((!config.attractive_settings.MeleeOnly || (activeItem != null && activeItem is BaseMelee)) && (config.attractive_settings.MaxDistance == 0 || (Vector3.Distance(player.transform.position, entity.transform.position) < config.attractive_settings.MaxDistance)))
                    {
                        List<Item> item_drops = Pool.GetList<Item>();
                        item_drops.AddRange(entity.inventory.itemList);
                        Subscribe("OnBonusItemDropped");
                        NextTick(() =>
                        {
                            foreach (var item in item_drops)
                            {
                                if (item == null) continue;
                                var parent = item.GetOwnerPlayer();
                                //item.RemoveFromWorld();
                                if (parent == null)
                                {
                                    player.GiveItem(item);
                                }
                            }
                            Pool.FreeList(ref item_drops);
                            Unsubscribe("OnBonusItemDropped");
                        });
                    }
                }
            }
            return null;
        }

        void OnBonusItemDropped(Item item, BasePlayer player)
        {
            // Used by Buff.Loot_Pickup to handle loot magnet with bonus scrap. We subscribe to the hook for a tick and then unsubscribe once the scrap has been generated.
            player.GiveItem(item);
        }

        List<LootContainer> looted_containers = new List<LootContainer>();

        void CanLootEntity(BasePlayer player, StorageContainer container)
        {
            if (container == null) return;
            LootContainer lootContainer = null;
            if ((lootContainer = container.GetEntity() as LootContainer) && lootContainer != null && !looted_containers.Contains(lootContainer))
            {
                looted_containers.Add(lootContainer);
                if (RollSuccessful(GetTotalBuffValue(player, Buff.Scavengers)))
                {
                    var scrap = ItemManager.CreateByName("scrap", UnityEngine.Random.Range(Math.Max(config.scavanger_info.Min_extra_scrap, 1), Math.Max(config.scavanger_info.Max_extra_scrap, 1) + 1));
                    if (!scrap.MoveToContainer(container.inventory))
                    {
                        container.inventory.capacity++;
                        container.inventorySlots++;
                        if (!scrap.MoveToContainer(container.inventory)) scrap.Remove();
                    }
                }
            }
        }

        object HandleDamageType(BasePlayer victim, HitInfo info, Rust.DamageType damageType, out float damageScaleModifier)
        {
            BuffSetInfo bsi;
            damageScaleModifier = 0f;
            if (!player_attributes.TryGetValue(victim.userID, out bsi)) return null;
            SetBonusValues values;
            switch (damageType)
            {
                case Rust.DamageType.Cold:
                case Rust.DamageType.ColdExposure:
                case Rust.DamageType.Heat:
                    damageScaleModifier += GetTotalBuffValue(victim, Buff.Elemental);
                    if (damageScaleModifier >= 1f) victim.metabolism.temperature.SetValue(25f);
                    break;

                case Rust.DamageType.Drowned:
                    if (bsi.setBonuses.TryGetValue(SetBonus.Gilled, out values)) damageScaleModifier += values.modifier;
                    //if (damageScaleModifier >= 1f) victim.metabolism.oxygen.SetValue(victim.metabolism.oxygen.max);
                    break;

                case Rust.DamageType.Fall:
                    if (bsi.setBonuses.TryGetValue(SetBonus.Feline, out values)) damageScaleModifier += values.modifier;
                    break;
                case Rust.DamageType.Blunt:
                    if (info.WeaponPrefab?.ShortPrefabName != null && (info.WeaponPrefab?.ShortPrefabName == "grenade.f1.deployed" || info.WeaponPrefab?.ShortPrefabName == "grenade.beancan.deployed"))
                    {
                        damageScaleModifier += GetTotalBuffValue(victim, Buff.Demo);
                    }
                    break;
                case Rust.DamageType.Explosion:
                    damageScaleModifier += GetTotalBuffValue(victim, Buff.Demo);
                    break;
                case Rust.DamageType.Radiation:
                case Rust.DamageType.RadiationExposure:
                    if (bsi.setBonuses.TryGetValue(SetBonus.Lead, out values)) damageScaleModifier += values.modifier;
                    if (damageScaleModifier >= 1f)
                    {
                        victim.metabolism.radiation_poison.SetValue(0f);
                        victim.metabolism.radiation_level.SetValue(0f);
                    }
                    break;
            }
            return null;
        }


        object HandlePlayerHitAnimal(BaseAnimalNPC victim, BasePlayer attacker, HitInfo info)
        {
            if (attacker == null) return null;
            
            var modifiedValue = GetTotalBuffValue(attacker, Buff.Hunters);
            if (info.WeaponPrefab != null && info.WeaponPrefab is BaseMelee) modifiedValue += GetTotalBuffValue(attacker, Buff.Barbarians);
            info.damageTypes.ScaleAll(1 + modifiedValue);
            return null;
        }

        float HandleAnimalHitPlayer(BasePlayer victim, BaseAnimalNPC attacker, HitInfo info, float modifiedDamageScale)
        {
            if (attacker == null) return modifiedDamageScale;
            modifiedDamageScale -= GetTotalBuffValue(victim, Buff.Tamers);
            if (modifiedDamageScale < 0) modifiedDamageScale = 0;
            return modifiedDamageScale;
        }

        void AddSetBonusEntries(Dictionary<SetBonus, float> dict, Dictionary<SetBonus, float> newValues)
        {
            foreach (var kvp in newValues)
            {
                if (dict.ContainsKey(kvp.Key)) dict[kvp.Key] += kvp.Value;
                else dict.Add(kvp.Key, kvp.Value);
            }
        }

        object CanEntityTakeDamage(BasePlayer player, HitInfo hitinfo)
        {
            if (player == null || player.IsNpc || !player.userID.IsSteamId() || player.currentTeam == 0 || player.Team == null || player.Team.members == null || hitinfo == null || hitinfo.InitiatorPlayer == null || !player.Team.members.Contains(hitinfo.InitiatorPlayer.userID)) return null;
            var setBonuses = GetSetBonuses(hitinfo.InitiatorPlayer);
            if (setBonuses != null && setBonuses.ContainsKey(SetBonus.HealthShot))
            {
                var damageType = hitinfo.damageTypes.GetMajorityDamageType();
                if (damageType == Rust.DamageType.Bullet || damageType == Rust.DamageType.Arrow || damageType == Rust.DamageType.Blunt || damageType == Rust.DamageType.Slash || damageType == Rust.DamageType.Stab)
                {
                    return true;
                }
            }
            return null;
        }

        float HandlePlayerHitPlayer(BasePlayer victim, BasePlayer attacker, HitInfo info, float modifiedDamageScale, out bool IsHealthShot)
        {
            //cumulativeAttackerSetBonuses is to handle when checking different bonuses.
            //Checks attacker for their buffs and set bonuses in PVP.
            IsHealthShot = false;
            modifiedDamageScale += GetTotalBuffValue(attacker, Buff.Assassins); // Handles set bonus buff modifier as well.);
            var setBonuses = GetSetBonuses(attacker);
            SetBonusValues values;
            if (setBonuses != null)
            {
                var damageType = info.damageTypes.GetMajorityDamageType();
                if ((damageType == Rust.DamageType.Bullet || damageType == Rust.DamageType.Arrow) && setBonuses.TryGetValue(SetBonus.UncannyDodge, out values) && RollSuccessful(values.modifier))
                {                    
                    if (!string.IsNullOrEmpty(config.effect_settings.UncannyDodge_effect)) EffectNetwork.Send(new Effect(config.effect_settings.UncannyDodge_effect, victim.transform.position, victim.transform.position), victim.net.connection);
                    return 0f;
                }
                if (setBonuses.TryGetValue(SetBonus.PVPCrit, out values) && RollSuccessful(values.modifier))
                {
                    modifiedDamageScale += UnityEngine.Random.Range(config.pvp_crit_damage.min_crit_modifier, config.pvp_crit_damage.max_crit_modifier);
                }
                if (setBonuses.TryGetValue(SetBonus.HealthShot, out values) && victim.Team?.teamID == attacker.Team?.teamID)
                {                    
                    if (damageType == Rust.DamageType.Bullet || damageType == Rust.DamageType.Arrow || damageType == Rust.DamageType.Blunt || damageType == Rust.DamageType.Slash || damageType == Rust.DamageType.Stab)
                    {
                        var damage = info.damageTypes.Total();
                        var weapon = attacker.GetActiveItem();
                        float mod;
                        if (weapon == null || !config.healthshot_override.TryGetValue(weapon.info.shortname, out mod)) mod = 1f;
                        victim.Heal((damage * values.modifier) * mod);
                        victim.metabolism.bleeding.SetValue(0);
                        modifiedDamageScale = 0;
                        IsHealthShot = true;
                        return modifiedDamageScale;
                    }
                }                
            }
            // Checks if the victim has the Reflex setbonus
            var victimSetBonuses = GetSetBonuses(victim);
            if (victimSetBonuses != null)
            {
                if (victimSetBonuses.TryGetValue(SetBonus.Reflexes, out values)) modifiedDamageScale -= values.modifier;
                if (victimSetBonuses.TryGetValue(SetBonus.BulletProof, out values))
                {
                    var damageType = info.damageTypes.GetMajorityDamageType();
                    if (damageType == Rust.DamageType.Bullet) modifiedDamageScale -= values.modifier;
                }                
            }
            if (info.WeaponPrefab != null && info.WeaponPrefab is BaseMelee)
            {
                modifiedDamageScale += GetTotalBuffValue(attacker, Buff.Barbarians);
                modifiedDamageScale -= GetTotalBuffValue(victim, Buff.Knights);
            }

            if (modifiedDamageScale < 0) modifiedDamageScale = 0;
            return modifiedDamageScale;
        }

        float HandleHumanNPCHitPlayer(BasePlayer victim, BasePlayer attacker, HitInfo info, float modifiedDamageScale)
        {
            var victimSetBonuses = GetSetBonuses(victim);
            if (victimSetBonuses != null)
            {
                SetBonusValues values;
                var damageType = info.damageTypes.GetMajorityDamageType();
                if ((damageType == Rust.DamageType.Bullet || damageType == Rust.DamageType.Arrow) && victimSetBonuses.TryGetValue(SetBonus.UncannyDodge, out values) && RollSuccessful(values.modifier))
                {
                    if (!string.IsNullOrEmpty(config.effect_settings.UncannyDodge_effect)) EffectNetwork.Send(new Effect(config.effect_settings.UncannyDodge_effect, victim.transform.position, victim.transform.position), victim.net.connection);
                    return 0f;
                }
                if (victimSetBonuses.TryGetValue(SetBonus.BulletProof, out values))
                {                   
                    if (damageType == Rust.DamageType.Bullet) modifiedDamageScale -= values.modifier;
                }
            }

            if (info.WeaponPrefab != null && info.WeaponPrefab is BaseMelee)
            {                
                modifiedDamageScale -= GetTotalBuffValue(victim, Buff.Knights);
            }

            return modifiedDamageScale;
        }

        float HandlePlayerHitNPC(BasePlayer victim, BasePlayer attacker, HitInfo info, float modifiedDamageScale)
        {
            if (attacker == null || victim == null) return modifiedDamageScale;
            modifiedDamageScale += GetTotalBuffValue(attacker, Buff.Operators);
            if (info.WeaponPrefab != null && info.WeaponPrefab is BaseMelee) modifiedDamageScale += GetTotalBuffValue(attacker, Buff.Barbarians);
            return modifiedDamageScale;
        }

        Dictionary<ulong, Minicopter> Tracked_Helis = new Dictionary<ulong, Minicopter>();
        Dictionary<ulong, MotorRowboat> Tracked_Boats = new Dictionary<ulong, MotorRowboat>();
        Dictionary<ulong, MotorRowboat> Tracked_Boat_Speeds = new Dictionary<ulong, MotorRowboat>();
        public float default_heli_fuel_rate = 0.25f;
        public float default_rowboat_fuel_rate = 0.1f;
        public float default_rhib_fuel_rate = 0.25f;
        Dictionary<ulong, HorseInfo> Tracked_Horses = new Dictionary<ulong, HorseInfo>();

        public class HorseInfo
        {
            public RidableHorse horse;
            public float current_maxSpeed;
            public float current_runSpeed;
            public float current_walkSpeed;
            public float current_trotSpeed;
            public float current_turnSpeed;
            public HorseInfo(RidableHorse horse, float current_maxSpeed, float current_runSpeed, float current_walkSpeed, float current_trotSpeed, float current_turnSpeed)
            {
                this.horse = horse;
                this.current_maxSpeed = current_maxSpeed;
                this.current_runSpeed = current_runSpeed;
                this.current_walkSpeed = current_walkSpeed;
                this.current_trotSpeed = current_trotSpeed;
                this.current_turnSpeed = current_turnSpeed;
            }
        }

        void OnEntityMounted(BaseMountable entity, BasePlayer player)
        {
            if (player.IsNpc || !player.userID.IsSteamId() || entity == null || entity.GetParentEntity() == null) return;
            var vehicle = entity.GetParentEntity();
            if (vehicle == null) return;
            if (vehicle is Minicopter)
            {
                SetMiniFuel(vehicle as Minicopter, GetTotalBuffValue(player, Buff.Transporters));
            }
            else if (vehicle is MotorRowboat)
            {
                var boat = vehicle as MotorRowboat;
                SetBoatFuel(boat, GetTotalBuffValue(player, Buff.Transporters));
                var setBonuses = GetSetBonuses(player);
                SetBonusValues values;
                if (setBonuses != null && setBonuses.TryGetValue(SetBonus.IncreasedBoatSpeed, out values) && !Tracked_Boat_Speeds.ContainsKey(boat.net.ID.Value))
                {
                    if (boat.ShortPrefabName == "rhib" && boat.engineThrust == 1500f && Interface.CallHook("ELOnModifyBoatSpeed", player, boat) == null)
                    {
                        boat.engineThrust += values.modifier * 1500f;
                        if (MessagesOn(player)) PrintToChat(player, string.Format(lang.GetMessage("BoatSpeedBoosted", this, player.UserIDString), boat.engineThrust));
                        Tracked_Boat_Speeds.Add(boat.net.ID.Value, boat);
                    }
                    if (boat.ShortPrefabName == "tugboat" && boat.engineThrust == 200000f)
                    {
                        boat.engineThrust += values.modifier * 200000f;
                        if (MessagesOn(player)) PrintToChat(player, string.Format(lang.GetMessage("BoatSpeedBoosted", this, player.UserIDString), boat.engineThrust));
                        Tracked_Boat_Speeds.Add(boat.net.ID.Value, boat);
                    }
                    else if (boat.engineThrust == 600f)
                    {
                        boat.engineThrust += values.modifier * 600f;
                        if (MessagesOn(player)) PrintToChat(player, string.Format(lang.GetMessage("BoatSpeedBoosted", this, player.UserIDString), boat.engineThrust));
                        Tracked_Boat_Speeds.Add(boat.net.ID.Value, boat);
                    }
                }
            }
            else if (vehicle is RidableHorse)
            {
                SetupHorse(vehicle as RidableHorse, GetTotalBuffValue(player, Buff.Jockeys));
            }
        }

        void OnEntityDismounted(BaseMountable entity, BasePlayer player)
        {
            if (player.IsNpc || !player.userID.IsSteamId() || entity == null || entity.GetParentEntity() == null) return;
            var vehicle = entity.GetParentEntity();
            if (vehicle is Minicopter && Tracked_Helis.ContainsKey(vehicle.net.ID.Value))
            {
                ResetFuelRate(vehicle as Minicopter);
                Tracked_Helis.Remove(vehicle.net.ID.Value);
            }
            else if (vehicle is MotorRowboat)
            {
                var boat = vehicle as MotorRowboat;
                if (Tracked_Boats.ContainsKey(boat.net.ID.Value))
                {
                    ResetFuelRate(boat);
                    Tracked_Boats.Remove(boat.net.ID.Value);
                }
                if (!boat.AnyMounted()) ResetBoatSpeed(boat);
            }
            else if (vehicle is RidableHorse && Tracked_Horses.ContainsKey(vehicle.net.ID.Value))
            {
                ResetHorse(vehicle as RidableHorse);
                Tracked_Horses.Remove(vehicle.net.ID.Value);
            }
        }

        void ResetBoatSpeed(MotorRowboat boat)
        {
            if (!Tracked_Boat_Speeds.ContainsKey(boat.net.ID.Value)) return;
            Tracked_Boat_Speeds.Remove(boat.net.ID.Value);
            if (boat == null || !boat.IsAlive()) return;
            if (boat.ShortPrefabName == "rhib") boat.engineThrust = 1500f;
            else if (boat.ShortPrefabName == "tugboat") boat.engineThrust = 200000f;
            else boat.engineThrust = 600f;
        }

        void OnLoseCondition(Item item, ref float amount)
        {
            var player = item.GetOwnerPlayer();
            if (player == null || player.IsNpc || !player.userID.IsSteamId()) return;
            if (config.reinforced_blacklist.Contains(item.info.shortname)) return;
            var bonus = GetTotalBuffValue(player, Buff.Reinforced) * amount;
            if (bonus > amount) bonus = amount;
            item.condition += bonus;
        }

        void OnEntityDeath(BaseVehicle entity, HitInfo info)
        {
            if (entity == null) return;
            if (entity is Minicopter && Tracked_Helis.ContainsKey(entity.net.ID.Value)) Tracked_Helis.Remove(entity.net.ID.Value);
            else if (entity is MotorRowboat)
            {
                Tracked_Boats.Remove(entity.net.ID.Value);
                Tracked_Boat_Speeds.Remove(entity.net.ID.Value);
            }                
        }

        object OnPlayerAddModifiers(BasePlayer player, Item item, ItemModConsumable consumable)
        {
            BuffSetInfo bsi;
            if (!player_attributes.TryGetValue(player.userID, out bsi)) return null;
            SetBonusValues values;
            if (bsi.setBonuses.TryGetValue(SetBonus.Survivalist, out values))
            {
                var gain = consumable.GetIfType(MetabolismAttribute.Type.Calories);
                if (gain > 0) player.metabolism.calories.value += values.modifier * gain;
                gain = consumable.GetIfType(MetabolismAttribute.Type.Hydration);
                if (gain > 0) player.metabolism.hydration.value += values.modifier * gain;
            }
            if (bsi.setBonuses.TryGetValue(SetBonus.MaxHealth, out values) && item != null && item.info.shortname.StartsWith("maxhealthtea"))
            {
                foreach (var mod in consumable.modifiers)
                {
                    if (mod.type == Modifier.ModifierType.Max_Health)
                    {
                        PrintToChat(player, lang.GetMessage("NoDrinkTea", this, player.UserIDString));
                        return true;
                    }
                }
            }
            return null;
        }

        object OnResearchCostDetermine(Item item)
        {
            ResearchTable researchTable = item.GetEntityOwner() as ResearchTable;
            var player = researchTable.user;
            BuffSetInfo bsi;
            SetBonusValues values;
            if (player != null && player_attributes.TryGetValue(player.userID, out bsi) && bsi.setBonuses.TryGetValue(SetBonus.Researcher, out values) && RollSuccessful(values.modifier))
            {
                if (MessagesOn(player)) PrintToChat(player, lang.GetMessage("ScrapRefund", this, player.UserIDString));
                return 0;
            }
            return null;
        }

        object OnHammerHit(BasePlayer player, HitInfo info)
        {
            BuffSetInfo bsi;
            if (player_attributes.TryGetValue(player.userID, out bsi) && bsi.setBonuses.ContainsKey(SetBonus.FreeVehicleRepair) && info?.HitEntity is BaseVehicle)
            {
                var vehicle = info.HitEntity as BaseVehicle;
                var amount = vehicle.MaxHealth() - vehicle.health;
                if (amount <= 0) return null;
                else
                {
                    vehicle.Heal(amount);
                    EffectNetwork.Send(new Effect("assets/bundled/prefabs/fx/build/repair_full_metal.prefab", player.transform.position, player.transform.position), player.net.connection);
                    return false;
                }
            }

            return null;
        }

        object OnItemCraft(ItemCraftTask task, BasePlayer player, Item fromTempBlueprint)
        {
            BuffSetInfo bsi;
            if (player_attributes.TryGetValue(player.userID, out bsi) && bsi.buffs.ContainsKey(Buff.Crafters))
            {
                var craftingTime = task.blueprint.time;
                var reducedTime = craftingTime - (craftingTime * GetTotalBuffValue(player, Buff.Crafters));
                if (reducedTime <= 0) reducedTime = 0.1f;
                if (!task.blueprint.name.Contains("(Clone)"))
                    task.blueprint = UnityEngine.Object.Instantiate(task.blueprint);
                task.blueprint.time = reducedTime;
            }
            return null;
        }

        Dictionary<string, ItemBlueprint> item_BPs = new Dictionary<string, ItemBlueprint>();

        float GetCraftChance(string userIDString, float luck)
        {
            var highest = 1f;
            foreach (var perm in config.general_settings.perm_chance_modifier_craft)
            {
                if (permission.UserHasPermission(userIDString, perm.Key))
                {
                    if (highest == 1) highest = perm.Value;
                    else if (perm.Value > highest) highest = perm.Value;
                }
            }
            return luck * highest;
        }

        void OnItemCraftFinished(ItemCraftTask task, Item item, ItemCrafter crafter)
        {
            if (crafter == null) return;
            var player = crafter.owner;
            if (player == null) return;
            if (task.blueprint == null)
            {
                return;
            }
            BuffSetInfo bsi;
            if (player_attributes.TryGetValue(player.userID, out bsi) && bsi.buffs.ContainsKey(Buff.Assemblers) && RollSuccessful(GetTotalBuffValue(player, Buff.Assemblers)))
            {
                var refunded = 0;
                ItemBlueprint bp;
                if (!item_BPs.TryGetValue(item.info.shortname, out bp)) return;
                foreach (var component in bp.ingredients)
                {
                    if (config.craft_refund_blacklist.Contains(component.itemDef.shortname)) continue;
                    var nitem = ItemManager.CreateByName(component.itemDef.shortname, Math.Max(Convert.ToInt32(component.amount), 1));
                    if (!player.inventory.containerBelt.IsFull() || !player.inventory.containerMain.IsFull())
                    {
                        player.inventory.GiveItem(nitem);
                    }
                    else nitem.DropAndTossUpwards(player.transform.position);
                    refunded++;
                }
                if (refunded > 0 && MessagesOn(player)) PrintToChat(player, lang.GetMessage("CraftRefund", this, player.UserIDString));
            }

            if (config.crafting_info.enabled && permission.UserHasPermission(player.UserIDString, perm_craft) && !config.crafting_info.black_list.Contains(item.info.shortname) && EnhanceableItems.Contains(item.info.shortname) && RollSuccessful(GetCraftChance(player.UserIDString, config.crafting_info.chance), false))
            {
                ReplaceItem(player, item);
            }

            if (bsi != null && bsi.buffs.ContainsKey(Buff.Fabricators) && RollSuccessful(GetTotalBuffValue(player, Buff.Fabricators)) && !config.fabricator_blacklist.Contains(item.info.shortname))
            {
                if (MessagesOn(player)) PrintToChat(player, string.Format(lang.GetMessage("FabricateProc", this, player.UserIDString), item.info.displayName.english));
                player.GiveItem(ItemManager.CreateByName(item.info.shortname, Math.Max(item.amount, 1), item.skin));
            }

            if (task.amount > 0) return;

            if (task.blueprint != null && task.blueprint.name.Contains("(Clone)"))
            {
                var behaviours = task.blueprint.GetComponents<MonoBehaviour>();
                foreach (var behaviour in behaviours)
                {
                    if (behaviour.name.Contains("(Clone)")) UnityEngine.Object.Destroy(behaviour);
                }
            }
        }

        void ReplaceItem(BasePlayer player, Item oldItem)
        {
            List<Buff> potentialBuffs = Pool.GetList<Buff>();
            potentialBuffs.AddRange(config.enhancements.Where(x => (x.Value.item_whitelist != null && x.Value.item_whitelist.Count > 0 && x.Value.item_whitelist.Contains(oldItem.info.shortname)) || (x.Value.item_blacklist == null || !x.Value.item_blacklist.Contains(oldItem.info.shortname))).Select(y => y.Key));
            if (potentialBuffs.Count > 0)
            {
                var buff = potentialBuffs.GetRandom();
                EnhancementInfo ei;
                if (config.enhancements.TryGetValue(buff, out ei))
                {
                    var tier = GetTier(ei);
                    if (tier.Key != null)
                    {
                        var skin = oldItem.skin > 0 ? oldItem.skin : ei.static_skins.ContainsKey(oldItem.info.shortname) ? ei.static_skins[oldItem.info.shortname] : config.item_skins.ContainsKey(oldItem.info.shortname) ? config.item_skins[oldItem.info.shortname].GetRandom() : 0ul;                        
                        var newItem = ItemManager.CreateByName(oldItem.info.shortname, Math.Max(oldItem.amount, 1), skin);
                        if (newItem.GetHeldEntity() != null)
                            newItem.GetHeldEntity().skinID = skin;
                        if (newItem == null) return;
                        newItem.name = $"{buff} {newItem.info.displayName.english} [{tier.Key} {tier.Value}]";
                        newItem.MarkDirty();
                        var heldEntity = newItem.GetHeldEntity();
                        if (heldEntity != null)
                        {
                            heldEntity.skinID = oldItem.skin;
                        }
                        oldItem.RemoveFromContainer();
                        player.GiveItem(newItem);
                        oldItem.Remove();
                        if (MessagesOn(player)) PrintToChat(player, string.Format(lang.GetMessage("CraftedProc", this, player.UserIDString), config.tier_information.tier_colours[tier.Key], newItem.name));
                    }
                }
            }
            Pool.FreeList(ref potentialBuffs);
        }

        KeyValuePair<string, float> GetTier(EnhancementInfo ei)
        {
            string tier = null;
            var totalWeight = ei.tierInfo.Sum(x => x.Value.chance_weight);
            var randomNumber = UnityEngine.Random.Range(0, totalWeight + 1);
            var count = 0;
            var modifier = 0f;
            foreach (var t in ei.tierInfo)
            {
                count += t.Value.chance_weight;
                if (randomNumber <= count)
                {
                    tier = t.Key;
                    modifier = (float)Math.Round(UnityEngine.Random.Range(t.Value.min_value, t.Value.max_value), 2);
                    break;
                }
            }
            return new KeyValuePair<string, float>(tier, modifier);
        }

        object OnPayForUpgrade(BasePlayer player, BuildingBlock block, ConstructionGrade gradeTarget)
        {
            BuffSetInfo bsi;
            BuffAttributes buffmods;
            if (player_attributes.TryGetValue(player.userID, out bsi) && bsi.buffs.TryGetValue(Buff.Builders, out buffmods) && RollSuccessful(buffmods.totalModifier))
            {
                if (MessagesOn(player)) PrintToChat(player, lang.GetMessage("FreeUpgrade", this, player.UserIDString));
                return 0;
            }
            return null;
        }

        void OnWeaponFired(BaseProjectile projectile, BasePlayer player, ItemModProjectile mod, ProtoBuf.ProjectileShoot projectiles)
        {
            if (projectile == null || projectile.primaryMagazine == null) return;
            BuffSetInfo bsi;
            BuffAttributes buffmods;
            if (IsRaidItem(projectile.primaryMagazine.ammoType) && player_attributes.TryGetValue(player.userID, out bsi) && bsi.buffs.TryGetValue(Buff.Raiders, out buffmods) && RollSuccessful(buffmods.totalModifier))
            {
                var heldEntity = projectile.GetItem();
                if (heldEntity == null) return;
                if (config.raiders_info.blacklist.Contains(heldEntity.info.shortname)) return;
                projectile.primaryMagazine.contents++;
                projectile.SendNetworkUpdateImmediate();
            }
        }

        void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            BuffSetInfo bsi;
            BuffAttributes buffmods;
            if (player_attributes.TryGetValue(player.userID, out bsi) && bsi.buffs.TryGetValue(Buff.Raiders, out buffmods) && RollSuccessful(buffmods.totalModifier))
            {
                var shortname = IsRocket(entity.ShortPrefabName);
                if (string.IsNullOrEmpty(shortname)) return;
                var heldEntity = player.GetHeldEntity() as BaseProjectile;
                if (heldEntity != null)
                {
                    if (config.raiders_info.blacklist.Contains(shortname)) return;
                    NextTick(() =>
                    {
                        heldEntity.primaryMagazine.contents++;
                        heldEntity.SendNetworkUpdateImmediate();
                    });
                }
            }
        }

        void OnExplosiveThrown(BasePlayer player, BaseEntity entity, ThrownWeapon thrown)
        {
            var item = thrown.GetItem();
            if (item == null) return;            
            BuffSetInfo bsi;
            BuffAttributes buffmods;
            if (IsRaidItem(item) && player_attributes.TryGetValue(player.userID, out bsi) && bsi.buffs.TryGetValue(Buff.Raiders, out buffmods) && RollSuccessful(buffmods.totalModifier))
            {
                var rfExplosive = entity as RFTimedExplosive;
                if (rfExplosive != null && rfExplosive.GetFrequency() >= 0) return;
                if (config.raiders_info.blacklist.Contains(item.info.shortname)) return;
                var replaceItem = ItemManager.CreateByName(item.info.shortname);
                player.GiveItem(replaceItem);
            }
        }

        string IsRocket(string prefab)
        {
            switch (prefab)
            {
                case "rocket_basic": return "ammo.rocket.basic";
                case "rocket_hv": return "ammo.rocket.fire";
                case "rocket_fire": return "ammo.rocket.hv";
                default: return null;
            }
        }

        bool IsRaidItem(ItemDefinition def)
        {
            return def.shortname == "ammo.rifle.explosive";
        }

        bool IsRaidItem(Item def)
        {
            switch (def.info.shortname)
            {
                case "explosive.timed":
                case "explosive.satchel":
                case "grenade.f1":
                case "grenade.beancan":
                    return true;
                default: return false;
            }
        }

        #endregion

        #region methods

        [ConsoleCommand("elprintitems")]
        void PrintEnhanceableItems(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && permission.UserHasPermission(player.UserIDString, perm_admin))
            {
                PrintToChat(player, "Item list was printed to your console.");
                PrintToConsole(player, string.Join("\n- ", EnhanceableItems));
            }
            else Puts(string.Join("\n- ", EnhanceableItems));
        }

        bool MessagesOn(BasePlayer player, PlayerSettings pi = null)
        {
            if (pi == null) pi = SetupDefaultPlayerUI(player);
            return pi.notifications;
        }

        [ChatCommand("eladdskin")]
        void AddSkin(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, perm_admin)) return;
            if (args.Length < 3)
            {
                PrintToChat(player, lang.GetMessage("eladdskinusagemessage", this, player.UserIDString));
                return;
            }
            var buff = parsedEnums.ContainsKey(args[0]) ? parsedEnums[args[0]] : Buff.None;
            if (buff == Buff.None || !config.enhancements.ContainsKey(buff))
            {
                PrintToChat(player, string.Format(lang.GetMessage("eladdskininvalidbuff", this, player.UserIDString), args[0]));
                return;
            }
            var itemDef = ItemManager.FindItemDefinition(args[1]);
            if (itemDef == null)
            {
                PrintToChat(player, string.Format(lang.GetMessage("eladdskininvalidshortname", this, player.UserIDString), args[1]));
                return;
            }
            if (!args[2].IsNumeric())
            {
                PrintToChat(player, lang.GetMessage("eladdskininvalidskinid", this, player.UserIDString));
                return;
            }
            var skin = Convert.ToUInt64(args[2]);
            if (config.enhancements[buff].static_skins.ContainsKey(itemDef.shortname)) config.enhancements[buff].static_skins[itemDef.shortname] = skin;
            else config.enhancements[buff].static_skins.Add(itemDef.shortname, skin);
            SaveConfig();
            PrintToChat(player, string.Format(lang.GetMessage("eladdskinsuccess", this, player.UserIDString), itemDef.shortname, skin, buff));
        }

        private BasePlayer FindPlayer(string IDName, bool fromConsole, BasePlayer SearchingPlayer = null)
        {
            IDName = IDName.ToLower();
            List<BasePlayer> foundPlayers = Pool.GetList<BasePlayer>();
            foreach (var player in BasePlayer.allPlayerList)
            {
                if (player.displayName.Equals(IDName, StringComparison.OrdinalIgnoreCase) || player.UserIDString == IDName) return player;
                if (player.displayName.ToLower().Contains(IDName)) foundPlayers.Add(player);
            }

            if (foundPlayers.Count == 0)
            {
                Pool.FreeList(ref foundPlayers);
                Respond(string.Format(lang.GetMessage("NoMatch", this, SearchingPlayer?.UserIDString ?? null), IDName), fromConsole, SearchingPlayer);
                return null;
            }

            if (foundPlayers.Count == 1)
            {
                var result = foundPlayers[0];
                Pool.FreeList(ref foundPlayers);
                return result;
            }

            Respond(string.Format(lang.GetMessage("MultipleMatches", this, SearchingPlayer?.UserIDString ?? null), IDName, string.Join(", ", foundPlayers.Select(x => x.displayName))), fromConsole, SearchingPlayer);
            Pool.FreeList(ref foundPlayers);
            return null;
        }

        void Respond(string message, bool console, BasePlayer player = null)
        {
            if (console)
            {
                if (player != null)
                    PrintToConsole(player, message);
                else Puts(message);
            }
            PrintToChat(player, message);             
        }

        void SetupHorse(RidableHorse horse, float value)
        {
            if (value == 0) return;

            // Prevents this plugin from overriding horse stats if another plugin says we cannot.
            if (Interface.CallHook("ELCanModifyHorse", horse, value) != null) return;
            if (Cooking != null && Cooking.IsLoaded && Convert.ToBoolean(Cooking.Call("IsHorseBuffed", horse))) return;

            if (Tracked_Horses.ContainsKey(horse.net.ID.Value))
            {
                ResetHorse(horse);
                Tracked_Horses.Remove(horse.net.ID.Value);
            }
            Tracked_Horses.Add(horse.net.ID.Value, new HorseInfo(horse, horse.maxSpeed, horse.runSpeed, horse.walkSpeed, horse.trotSpeed, horse.turnSpeed));
            horse.maxSpeed += horse.maxSpeed * value;
            if (config.horse_info.Adjust_Run_Speed) horse.runSpeed += horse.runSpeed * value;
            if (config.horse_info.Adjust_Trot_Speed) horse.trotSpeed += horse.trotSpeed * value;
            if (config.horse_info.Adjust_Turn_Speed) horse.turnSpeed += horse.turnSpeed * value;
            if (config.horse_info.Adjust_Walk_Speed) horse.walkSpeed += horse.walkSpeed * value;
        }

        void SetBoatFuel(MotorRowboat boat, float value)
        {
            if (value == 0) return;

            var fuelSystem = boat.GetFuelSystem();
            if (fuelSystem == null || fuelSystem.nextFuelCheckTime == float.MaxValue || fuelSystem.GetFuelContainer().HasFlag(BaseEntity.Flags.Locked)) return;

            if (Tracked_Boats.ContainsKey(boat.net.ID.Value)) return;
            else Tracked_Boats.Add(boat.net.ID.Value, boat);
            if (boat is RHIB && boat.fuelPerSec >= 0.25f) boat.fuelPerSec = default_rhib_fuel_rate - (default_rhib_fuel_rate * value);
            else if (boat.fuelPerSec >= 0.1f) boat.fuelPerSec = default_rowboat_fuel_rate - (default_rowboat_fuel_rate * value);
        }

        void SetMiniFuel(Minicopter mini, float value)
        {
            if (value == 0) return;

            var fuelSystem = mini.GetFuelSystem();
            if (fuelSystem == null || fuelSystem.nextFuelCheckTime == float.MaxValue || fuelSystem.GetFuelContainer().HasFlag(BaseEntity.Flags.Locked)) return;

            if (mini.fuelPerSec < 0.5f) return;
            if (Tracked_Helis.ContainsKey(mini.net.ID.Value) || mini.fuelPerSec < default_heli_fuel_rate) return;
            else Tracked_Helis.Add(mini.net.ID.Value, mini);            

            var fuelRate = default_heli_fuel_rate - (default_heli_fuel_rate * value);
            mini.fuelPerSec = fuelRate;
        }

        void ResetHorse(RidableHorse horse)
        {
            if (horse == null) return;
            HorseInfo hi;
            if (Tracked_Horses.TryGetValue(horse.net.ID.Value, out hi))
            {
                horse.maxSpeed = hi.current_maxSpeed;
                horse.turnSpeed = hi.current_turnSpeed;
                horse.trotSpeed = hi.current_trotSpeed;
                horse.walkSpeed = hi.current_walkSpeed;
                horse.runSpeed = hi.current_runSpeed;
            }
        }

        void ResetFuelRate(MotorRowboat boat)
        {
            if (boat == null) return;
            boat.fuelPerSec = (boat is RHIB) ? default_rhib_fuel_rate : default_rowboat_fuel_rate;
        }

        void ResetFuelRate(Minicopter mini)
        {
            if (mini == null) return;
            mini.fuelPerSec = default_heli_fuel_rate;
        }

        string GetBuffTitle(BasePlayer player, Buff buff)
        {
            switch (buff)
            {
                case Buff.Miners: return lang.GetMessage("DispayName.Buff.Miners", this, player.UserIDString);
                case Buff.Lumberjacks: return lang.GetMessage("DispayName.Buff.Lumberjacks", this, player.UserIDString);
                case Buff.Skinners: return lang.GetMessage("DispayName.Buff.Skinners", this, player.UserIDString);
                case Buff.Farmers: return lang.GetMessage("DispayName.Buff.Farmers", this, player.UserIDString);
                case Buff.Foragers: return lang.GetMessage("DispayName.Buff.Foragers", this, player.UserIDString);
                case Buff.Fishermans: return lang.GetMessage("DispayName.Buff.Fishermans", this, player.UserIDString);
                case Buff.Assassins: return lang.GetMessage("DispayName.Buff.Assassins", this, player.UserIDString);
                case Buff.Demo: return lang.GetMessage("DispayName.Buff.Demo", this, player.UserIDString);
                case Buff.Elemental: return lang.GetMessage("DispayName.Buff.Elemental", this, player.UserIDString);
                case Buff.Scavengers: return lang.GetMessage("DispayName.Buff.Scavengers", this, player.UserIDString);
                case Buff.Transporters: return lang.GetMessage("DispayName.Buff.Transporters", this, player.UserIDString);
                case Buff.Crafters: return lang.GetMessage("DispayName.Buff.Crafters", this, player.UserIDString);
                case Buff.Reinforced: return lang.GetMessage("DispayName.Buff.Reinforced", this, player.UserIDString);
                case Buff.Tamers: return lang.GetMessage("DispayName.Buff.Tamers", this, player.UserIDString);
                case Buff.Hunters: return lang.GetMessage("DispayName.Buff.Hunters", this, player.UserIDString);
                case Buff.Operators: return lang.GetMessage("DispayName.Buff.Operators", this, player.UserIDString);
                case Buff.Jockeys: return lang.GetMessage("DispayName.Buff.Jockeys", this, player.UserIDString);
                case Buff.Raiders: return lang.GetMessage("DispayName.Buff.Raiders", this, player.UserIDString);
                case Buff.Assemblers: return lang.GetMessage("DispayName.Buff.Assemblers", this, player.UserIDString);
                case Buff.Fabricators: return lang.GetMessage("DispayName.Buff.Fabricators", this, player.UserIDString);
                case Buff.Builders: return lang.GetMessage("DispayName.Buff.Builders", this, player.UserIDString);
                case Buff.Medics: return lang.GetMessage("DispayName.Buff.Medics", this, player.UserIDString);
                case Buff.Knights: return lang.GetMessage("DispayName.Buff.Knights", this, player.UserIDString);
                case Buff.Barbarians: return lang.GetMessage("DispayName.Buff.Barbarians", this, player.UserIDString);

                default: return null;
            }
        }

        string GetBuffDescription(BasePlayer player, Buff buff)
        {
            switch (buff)
            {
                case Buff.Miners: return lang.GetMessage("Description.Buff.Miners", this, player.UserIDString);
                case Buff.Lumberjacks: return lang.GetMessage("Description.Buff.Lumberjacks", this, player.UserIDString);
                case Buff.Skinners: return lang.GetMessage("Description.Buff.Skinners", this, player.UserIDString);
                case Buff.Farmers: return lang.GetMessage("Description.Buff.Farmers", this, player.UserIDString);
                case Buff.Foragers: return lang.GetMessage("Description.Buff.Foragers", this, player.UserIDString);
                case Buff.Fishermans: return lang.GetMessage("Description.Buff.Fishermans", this, player.UserIDString);
                case Buff.Assassins: return lang.GetMessage("Description.Buff.Assassins", this, player.UserIDString);
                case Buff.Demo: return lang.GetMessage("Description.Buff.Demo", this, player.UserIDString);
                case Buff.Elemental: return lang.GetMessage("Description.Buff.Elemental", this, player.UserIDString);
                case Buff.Scavengers: return lang.GetMessage("Description.Buff.Scavengers", this, player.UserIDString);
                case Buff.Transporters: return lang.GetMessage("Description.Buff.Transporters", this, player.UserIDString);
                case Buff.Crafters: return lang.GetMessage("Description.Buff.Crafters", this, player.UserIDString);
                case Buff.Reinforced: return lang.GetMessage("Description.Buff.Reinforced", this, player.UserIDString);
                case Buff.Tamers: return lang.GetMessage("Description.Buff.Tamers", this, player.UserIDString);
                case Buff.Hunters: return lang.GetMessage("Description.Buff.Hunters", this, player.UserIDString);
                case Buff.Operators: return lang.GetMessage("Description.Buff.Operators", this, player.UserIDString);
                case Buff.Jockeys: return lang.GetMessage("Description.Buff.Jockeys", this, player.UserIDString);
                case Buff.Raiders: return lang.GetMessage("Description.Buff.Raiders", this, player.UserIDString);
                case Buff.Builders: return lang.GetMessage("Description.Buff.Builders", this, player.UserIDString);
                case Buff.Assemblers: return lang.GetMessage("Description.Buff.Assemblers", this, player.UserIDString);
                case Buff.Fabricators: return lang.GetMessage("Description.Buff.Fabricators", this, player.UserIDString);
                case Buff.Medics: return lang.GetMessage("Description.Buff.Medics", this, player.UserIDString);
                case Buff.Barbarians: return lang.GetMessage("Description.Buff.Barbarians", this, player.UserIDString);
                case Buff.Knights: return lang.GetMessage("Description.Buff.Knights", this, player.UserIDString);
                default: return "";
            }
        }

        string GetSetBonusDescription(BasePlayer player, SetBonus set)
        {
            switch (set)
            {
                case SetBonus.FreeVehicleRepair: return lang.GetMessage("Description.SetBonus.FreeVehicleRepair", this, player.UserIDString);
                case SetBonus.IncreasedBoatSpeed: return lang.GetMessage("Description.SetBonus.IncreasedBoatSpeed", this, player.UserIDString);
                case SetBonus.InstantCook: return lang.GetMessage("Description.SetBonus.InstantCook", this, player.UserIDString);
                case SetBonus.InstantMining: return lang.GetMessage("Description.SetBonus.InstantMining", this, player.UserIDString);
                case SetBonus.InstantSkinning: return lang.GetMessage("Description.SetBonus.InstantSkinning", this, player.UserIDString);
                case SetBonus.InstantWoodcutting: return lang.GetMessage("Description.SetBonus.InstantWoodcutting", this, player.UserIDString);
                case SetBonus.PVPCrit: return lang.GetMessage("Description.SetBonus.PVPCrit", this, player.UserIDString);
                case SetBonus.Reflexes: return lang.GetMessage("Description.SetBonus.Reflexes", this, player.UserIDString);
                case SetBonus.Regrowth: return lang.GetMessage("Description.SetBonus.Regrowth", this, player.UserIDString);
                case SetBonus.Researcher: return lang.GetMessage("Description.SetBonus.Researcher", this, player.UserIDString);
                case SetBonus.Smelting: return lang.GetMessage("Description.SetBonus.Smelting", this, player.UserIDString);
                case SetBonus.Survivalist: return lang.GetMessage("Description.SetBonus.Survivalist", this, player.UserIDString);
                case SetBonus.Feline: return lang.GetMessage("Description.SetBonus.Feline", this, player.UserIDString);
                case SetBonus.Lead: return lang.GetMessage("Description.SetBonus.Lead", this, player.UserIDString);
                case SetBonus.Gilled: return lang.GetMessage("Description.SetBonus.Gilled", this, player.UserIDString);
                case SetBonus.BonusMultiplier: return lang.GetMessage("Description.SetBonus.BonusMultiplier", this, player.UserIDString);
                case SetBonus.Smasher: return lang.GetMessage("Description.SetBonus.Smasher", this, player.UserIDString);
                case SetBonus.WoodcuttersLuck: return lang.GetMessage("Description.SetBonus.WoodcuttersLuck", this, player.UserIDString);
                case SetBonus.MinersLuck: return lang.GetMessage("Description.SetBonus.MinersLuck", this, player.UserIDString);
                case SetBonus.SkinnersLuck: return lang.GetMessage("Description.SetBonus.SkinnersLuck", this, player.UserIDString);
                case SetBonus.FishersLuck: return lang.GetMessage("Description.SetBonus.FishersLuck", this, player.UserIDString);
                case SetBonus.RockCycle: return lang.GetMessage("Description.SetBonus.RockCycle", this, player.UserIDString);
                case SetBonus.Attractive: return lang.GetMessage("Description.SetBonus.Attractive", this, player.UserIDString);
                case SetBonus.FishingRodModifier: return lang.GetMessage("Description.SetBonus.FishingRodModifier", this, player.UserIDString);
                case SetBonus.TeamHeal: return lang.GetMessage("Description.SetBonus.TeamHeal", this, player.UserIDString);
                case SetBonus.HealthShot: return lang.GetMessage("Description.SetBonus.HealthShot", this, player.UserIDString);
                case SetBonus.BulletProof: return lang.GetMessage("Description.SetBonus.BulletProof", this, player.UserIDString);
                case SetBonus.UncannyDodge: return lang.GetMessage("Description.SetBonus.UncannyDodge", this, player.UserIDString);
                case SetBonus.MaxHealth: return lang.GetMessage("Description.SetBonus.MaxHealth", this, player.UserIDString);
                default: return "";
            }
        }

        string GetRefinedMaterial(string shortname)
        {
            switch (shortname)
            {
                case "hq.metal.ore": return "metal.refined";
                case "metal.ore": return "metal.fragments";
                case "sulfur.ore": return "sulfur";
                default: return null;
            }
        }

        string GetCookedMeat(string shortname)
        {
            switch (shortname)
            {
                case "bearmeat": return "bearmeat.cooked";
                case "chicken.raw": return "chicken.cooked";
                case "deermeat.raw": return "deermeat.cooked";
                case "fish.raw": return "fish.cooked";
                case "horsemeat.raw": return "horsemeat.cooked";
                case "humanmeat.raw": return "humanmeat.cooked";
                case "meat.boar": return "meat.pork.cooked";
                case "wolfmeat.raw": return "wolfmeat.cooked";
                default: return null;
            }
        }        

        bool RollSuccessful(float luck, bool isDecimal = true, string type = "loot")
        {            
            var roll = UnityEngine.Random.Range(0f, 100f);
            if (isDecimal) return roll >= 100f - (luck / 1 * 100);
            else return roll >= 100f - luck;
        }

        //Gets all items that we can enchant based on isWearable, isHoldable and categories weapon and tool.
        void GetEnhanceableItems()
        {
            foreach (var itemDef in ItemManager.GetItemDefinitions())
            {
                //This handles the item defs for blueprints (crafting refund).
                if (itemDef.Blueprint != null && itemDef.Blueprint.userCraftable)
                {
                    if (!item_BPs.ContainsKey(itemDef.shortname)) item_BPs.Add(itemDef.shortname, itemDef.Blueprint);
                }

                //This handles enchantables.
                if (config.global_blacklist.Contains(itemDef.shortname)) continue;
                if (itemDef.category == ItemCategory.Attire && itemDef.isWearable && !itemDef.shortname.StartsWith("frankensteins.monster") && !EnhanceableItems.Contains(itemDef.shortname))
                {
                    ItemIDs.Add(itemDef.shortname, itemDef.itemid);
                    EnhanceableItems.Add(itemDef.shortname);
                }

                if ((itemDef.category == ItemCategory.Weapon || itemDef.category == ItemCategory.Tool) && itemDef.isHoldable && (itemDef.occupySlots == ItemSlot.None || itemDef.occupySlots == 0) && !EnhanceableItems.Contains(itemDef.shortname))
                {
                    ItemIDs.Add(itemDef.shortname, itemDef.itemid);
                    EnhanceableItems.Add(itemDef.shortname);
                }
            }
            Puts($"Added {EnhanceableItems.Count} items to loot table.");
        }

        #endregion

        #region Generate Item
        /* We should generate items in this order: Buff > Tier > Whitelist ?? Blacklist (global blacklist is handled from GeEnchantableItems) > find equipable item. */

        [ChatCommand("genitem")]
        void GenerateItem(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, perm_admin)) return;

            List<string> shortName = Pool.GetList<string>();
            string type = null;
            string tier = null;
            if (args.Length > 0)
            {
                var str = args[0].ToLower();
                if (EnhanceableItems.Contains(str))
                {
                    shortName.Add(str);
                    if (args.Length > 1)
                    {
                        if (args[1].Length > 1) type = args[1];
                        else tier = args[1].ToLower();
                        if (args.Length > 2) tier = args[2];
                    }
                }
                else if (args[0].Length > 1)
                {
                    type = args[0];
                    if (args.Length > 1)
                    {
                        tier = args[1].ToLower();
                    }
                }
                else tier = args[0].ToLower();
            }

            GenerateItem(player, type, shortName.Count > 0 ? shortName : null, tier);
            Pool.FreeList(ref shortName);
        }

        [ConsoleCommand("genitem")]
        void GenerateItem(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, perm_admin))
            {
                arg.ReplyWith(lang.GetMessage("NoPerms", this, player.UserIDString));
                return;
            }
            if (arg.Args == null || arg.Args.Length < 1)
            {
                arg.ReplyWith(lang.GetMessage("GenItemInvalidParameters", this, player?.UserIDString));
                return;
            }
            var target = FindPlayer(arg.Args[0], true, player ?? null);
            if (target == null || target.IsDead() || !target.IsConnected) return;
            List<string> shortName = Pool.GetList<string>();
            string type = null;
            string tier = null;
            if (arg.Args.Length > 1)
            {
                var str = arg.Args[1].ToLower();
                if (EnhanceableItems.Contains(str))
                {
                    shortName.Add(str);
                    if (arg.Args.Length > 2)
                    {
                        if (arg.Args[2].Length > 1) type = arg.Args[2];
                        else tier = arg.Args[2].ToLower();
                        if (arg.Args.Length > 3) tier = arg.Args[3];
                    }
                }
                else if (str.Length > 1)
                {
                    type = str;
                    if (arg.Args.Length > 1) tier = arg.Args[2].ToLower();
                }
                else tier = str;

            }

            GenerateItem(target, type, shortName.Count > 0 ? shortName : null, tier);
            Pool.FreeList(ref shortName);
        }

        [HookMethod("GenerateItem")]    
        public void GenerateItem(BasePlayer player, string type = null, List<string> item_shortname = null, string tier = null, bool msg = false, Item itemToReplace = null)
        {
            if (player == null || player.IsDead() || !player.IsConnected) return;
            var randomItem = GenerateRandomItem(type, item_shortname, tier);
            if (randomItem != null)
            {
                if (itemToReplace != null)
                {
                    itemToReplace.name = randomItem.name;
                    itemToReplace.MarkDirty();
                    randomItem.Remove();
                    if (!player.inventory.AllItems().Contains(itemToReplace)) player.GiveItem(itemToReplace);
                    //if (itemToReplace.contents?.itemList != null)
                    //{
                    //    List<Item> attachments = Pool.GetList<Item>();
                    //    attachments.AddRange(itemToReplace.contents.itemList);

                    //    if (randomItem.contents == null)
                    //    {
                    //        randomItem.contents = new ItemContainer();
                    //        randomItem.contents.ServerInitialize(null, attachments.Count);
                    //        randomItem.contents.GiveUID();
                    //        randomItem.contents.parent = randomItem;
                    //    }

                    //    foreach (var _item in attachments)
                    //    {
                    //        if (!_item.MoveToContainer(randomItem.contents)) player.GiveItem(_item);
                    //    }
                    //    Pool.FreeList(ref attachments);
                    //}

                    //var ammo = itemToReplace.GetHeldEntity() is BaseProjectile ? (itemToReplace.GetHeldEntity() as BaseProjectile).primaryMagazine.contents : itemToReplace.GetHeldEntity() is FlameThrower ? (itemToReplace.GetHeldEntity() as FlameThrower).ammo : 0;
                    //var ammotype = (itemToReplace.GetHeldEntity() as BaseProjectile)?.primaryMagazine.ammoType.shortname ?? null;

                    //BaseProjectile weapon = randomItem.GetHeldEntity() as BaseProjectile;
                    //if (weapon != null)
                    //{
                    //    if (!string.IsNullOrEmpty(ammotype))
                    //        weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(ammotype);
                    //    weapon.primaryMagazine.contents = ammo;
                    //}
                    //FlameThrower flameThrower = randomItem.GetHeldEntity() as FlameThrower;
                    //if (flameThrower != null) flameThrower.ammo = ammo;

                    //if (!string.IsNullOrEmpty(itemToReplace.text)) randomItem.text = itemToReplace.text;

                    //var container = itemToReplace.GetRootContainer();
                    //var slot = itemToReplace.position;
                    //itemToReplace.RemoveFromContainer();
                    //if (!randomItem.MoveToContainer(container, slot)) player.GiveItem(randomItem);
                }
                else player.GiveItem(randomItem);
                if (msg) PrintToChat(player, string.Format(lang.GetMessage("GenerateItemHookResponse", this, player.UserIDString), randomItem?.name ?? itemToReplace?.name, GetBuffTier(randomItem?.name ?? itemToReplace?.name)?.TitleCase(), GetBuffValue(randomItem?.name ?? itemToReplace?.name)));
            }
        }

        public Item GenerateRandomItem(string type = null, List<string> item_shortnames = null, string tier = null)
        {
            List<Buff> buffs = Pool.GetList<Buff>();

            buffs.AddRange(Enum.GetValues(typeof(Buff)).Cast<Buff>().Intersect(config.enhancements.Keys));
            // Filters out buffs that have been disabled.
            List<Buff> disabledBuffs = Pool.GetList<Buff>();
            disabledBuffs.AddRange(config.enhancements.Where(x => !x.Value.enabled).Select(y => y.Key));
            buffs.RemoveAll(x => disabledBuffs.Contains(x));
            Pool.FreeList(ref disabledBuffs);
            if (buffs == null)
            {
                Pool.FreeList(ref buffs);
                return null;
            }
            var buff = type != null ? parsedEnums.ContainsKey(type) ? parsedEnums[type] : buffs.GetRandom() : buffs.GetRandom();

            Pool.FreeList(ref buffs);
            EnhancementInfo ei;
            if (config.enhancements.TryGetValue(buff, out ei))
            {
                float modifier = 0f;
                string shortname = null;
                ulong skin;
                var totalWeight = ei.tierInfo.Sum(x => x.Value.chance_weight);

                var count = 0;

                if (tier != null && ei.tierInfo.ContainsKey(tier))
                {
                    modifier = (float)Math.Round(UnityEngine.Random.Range(ei.tierInfo[tier].min_value, ei.tierInfo[tier].max_value), 2);
                }
                else
                {
                    var randomNumber = UnityEngine.Random.Range(0, totalWeight + 1);
                    foreach (var t in ei.tierInfo)
                    {
                        count += t.Value.chance_weight;
                        if (randomNumber <= count)
                        {
                            tier = t.Key;
                            modifier = (float)Math.Round(UnityEngine.Random.Range(t.Value.min_value, t.Value.max_value), 2);
                            break;
                        }
                    }
                }
                // We see if a list of items has been given first
                if (item_shortnames != null)
                {
                    List<string> items = Pool.GetList<string>();
                    if (ei.item_whitelist != null && ei.item_whitelist.Count > 0) items.AddRange(item_shortnames.Where(x => ei.item_whitelist.Contains(x)));
                    else if (ei.item_blacklist != null) items.AddRange(item_shortnames.Where(x => !ei.item_blacklist.Contains(x)));
                    else items.AddRange(EnhanceableItems.Where(x => item_shortnames.Contains(x)));
                    shortname = items.GetRandom();
                    Pool.FreeList(ref items);
                }
                //if true, we pick a random from whitelist.
                else if (ei.item_whitelist != null && ei.item_whitelist.Count > 0) shortname = ei.item_whitelist.GetRandom();
                else
                {
                    // otherwise we pick a random from our itemdefs.
                    List<string> items = Pool.GetList<string>();
                    if (ei.item_blacklist != null) items.AddRange(EnhanceableItems.Where(x => !ei.item_blacklist.Contains(x)));
                    else items.AddRange(EnhanceableItems);
                    shortname = items.GetRandom();
                    Pool.FreeList(ref items);
                }
                if (shortname == null) return null;
                if (ei.static_skins.ContainsKey(shortname)) skin = ei.static_skins[shortname];
                else if (config.item_skins.ContainsKey(shortname)) skin = config.item_skins[shortname].GetRandom();
                else skin = 0;
                var enhanced_item = ItemManager.CreateByName(shortname, 1, skin);
                if (enhanced_item == null) return null;
                else
                {
                    enhanced_item.name = $"{buff} {enhanced_item.info.displayName.english} [{tier} {modifier}]";
                    enhanced_item.MarkDirty();
                }
                if (enhanced_item.GetHeldEntity() != null) enhanced_item.GetHeldEntity().skinID = skin;
                enhanced_item.MarkDirty();
                var heldEntity = enhanced_item.GetHeldEntity();
                if (heldEntity != null)
                {
                    heldEntity.skinID = skin;
                }
                return enhanced_item;
            }

            return null;
        }

        #endregion

        #region Bonuses

        int GetIndex(string name)
        {
            var indexStr = name.Split(' ')?.Last() ?? null;
            if (string.IsNullOrEmpty(indexStr) || !indexStr.IsNumeric()) return 0;
            return Convert.ToInt32(indexStr);
        }

        // For some reason the decimal values don't seem to round when messing around with multiple items.
        // Checks the item for bonuses and adds or removes the modifier value depending if equip is true or false.
        void UpdateBonuses(BasePlayer player, Item item, bool equip)
        {
            if (!permission.UserHasPermission(player.UserIDString, perm_use)) return;
            if (item == null || item.name == null) return;
            var buff = GetBuff(item.name);
            
            if (buff != Buff.None)
            {
                var value = Convert.ToSingle(Math.Round((decimal)GetBuffValue(item.name), 2));
                BuffSetInfo buffSets;
                if (!player_attributes.TryGetValue(player.userID, out buffSets)) player_attributes.Add(player.userID, buffSets = new BuffSetInfo());
                if (equip)
                {
                    BuffAttributes buffAttributes;
                    if (!buffSets.buffs.TryGetValue(buff, out buffAttributes)) buffSets.buffs.Add(buff, buffAttributes = new BuffAttributes(value, 1));
                    else buffAttributes.AddPiece(value);
                    // When adding items to our buff attributes, we check them against their sets to see if any new items have been added.
                    var setInfo = GetSetBonuses(buffAttributes.piecesWorn, config.enhancements[buff].setInfo);
                    if (setInfo != null)
                    {
                        List<string> valid_perms = Pool.GetList<string>();
                        foreach (var set in setInfo)
                        {
                            if (set.Value.perms != null)
                            {
                                foreach (var perm in set.Value.perms)
                                {
                                    if (!valid_perms.Contains(perm.Key)) valid_perms.Add(perm.Key);                                    
                                }
                            }
                            SetBonusValues values;
                            if (set.Key == SetBonus.BonusMultiplier)
                            {
                                if (!buffSets.setBonusMultiplier.ContainsKey(buff)) buffSets.setBonusMultiplier.Add(buff, set.Value.modifier);
                                else buffSets.setBonusMultiplier[buff] = set.Value.modifier;
                            }
                            else if (!buffSets.setBonuses.TryGetValue(set.Key, out values)) buffSets.setBonuses.Add(set.Key, values = set.Value);
                            else
                            {
                                if (values.modifier < set.Value.modifier)
                                {                                    
                                    buffSets.setBonuses[set.Key] = set.Value;
                                }
                            }
                        }
                        RevokeInvalidPerms(player, valid_perms);
                        AddPerms(player, valid_perms);
                        Pool.FreeList(ref valid_perms);

                        
                    }
                }
                //else if (player_attributes.ContainsKey(player.userID) && player_attributes[player.userID].buffs.ContainsKey(buff))
                else
                {
                    SetupBonuses(player);
                    //player_attributes[player.userID].buffs[buff].RemovePiece(value);

                    // We unsub from the buff if out value is <= 0.
                    //if (player_attributes[player.userID].buffs[buff].totalModifier <= 0)
                    //{
                    //    player_attributes[player.userID].buffs.Remove(buff);
                    //    List<Buff> buffs = Pool.GetList<Buff>();
                    //    buffs.AddRange(player_attributes[player.userID].buffs.Keys);                        
                    //    //RemoveBuffs(player.userID, buff, buffs);
                    //    Pool.FreeList(ref buffs);
                    //}
                    // When removing a piece of clothing, we check the set bonuses.
                    //var setBonuses = UpdateSetBonuses(player);
                    //if (setBonuses != null) player_attributes[player.userID].setBonuses = setBonuses;
                    //else player_attributes[player.userID].setBonuses.Clear();
                    //if (player_attributes[player.userID].setBonusMultiplier.ContainsKey(buff))
                    //{
                    //    var bonusMultiplier = GetBonusMultiplier(player, buff, player_attributes[player.userID].buffs.ContainsKey(buff) ? player_attributes[player.userID].buffs[buff].piecesWorn : 0);
                    //    if (bonusMultiplier <= 0.001) player_attributes[player.userID].setBonusMultiplier.Remove(buff);
                    //    else player_attributes[player.userID].setBonusMultiplier[buff] = bonusMultiplier;
                    //}
                }
            }
        }

        void RevokeInvalidPerms(BasePlayer player, List<string> valid_perms)
        {
            List<string> perms;
            if (!Perms_tracker.TryGetValue(player.userID, out perms)) return;

            foreach (var perm in perms)
                if (!valid_perms.Contains(perm))
                    permission.RevokeUserPermission(player.UserIDString, perm);

            perms.RemoveAll(x => !valid_perms.Contains(x));

            if (perms.Count == 0)
            {
                perms.Clear();
                Perms_tracker.Remove(player.userID);
            }
        }

        void RemoveBonuses(BasePlayer player)
        {
            BuffSetInfo bsi;
            if (player_attributes.TryGetValue(player.userID, out bsi))
            {
                bsi.buffs?.Clear();
                bsi.setBonuses?.Clear();
                bsi.setBonusMultiplier?.Clear();
                //RemoveFromAllBuffs(player.userID);
            }
            RemovePerms(player.userID);
        }       

        void UpdateModifiers(BasePlayer player)
        {
            var bonuses = GetSetBonuses(player);
            SetBonusValues values;
            if (bonuses != null && bonuses.TryGetValue(SetBonus.MaxHealth, out values))
            {
                var currentMod = HasHealthModifier(player);
                if (currentMod > values.modifier || Interface.CallHook("ELOnModifyHealth", player, values.modifier) != null) return;
                player.modifiers.Add(new List<ModifierDefintion>
                {
                    new ModifierDefintion
                    {
                        type = Modifier.ModifierType.Max_Health,
                        value = values.modifier,
                        duration = 999999f,
                        source = Modifier.ModifierSource.Tea
                    }
                });
                player.modifiers.SendChangesToClient();
            }
            else if (player.modifiers.All != null && player.modifiers.All.Count > 0)
            {
                foreach (var mod in player.modifiers.All)
                {
                    if (mod.Type == Modifier.ModifierType.Max_Health && mod.Duration > 1200f)
                    {
                        if (Interface.CallHook("ELOnModifyHealth", player, mod.Value) != null) return;
                        RemoveMaxHealth(player);
                        break;
                    }                
                }
            }
        }

        float HasHealthModifier(BasePlayer player)
        {
            foreach (var mod in player.modifiers.All)
                if (mod.Type == Modifier.ModifierType.Max_Health) return mod.Value;

            return 0;
        }

        void RemoveMaxHealth(BasePlayer player)
        {
            player.modifiers.Add(new List<ModifierDefintion>
            {
                new ModifierDefintion
                {
                    type = Modifier.ModifierType.Max_Health,
                    value = 0.01f,
                    duration = 0.2f,
                    source = Modifier.ModifierSource.Tea
                }
            });
            player.modifiers.SendChangesToClient();
        }

        // Gets all bonuses that a player has on them.
        void SetupBonuses(BasePlayer player)
        {
            if (player.IsNpc || !player.userID.IsSteamId()) return;
            if (!permission.UserHasPermission(player.UserIDString, perm_use)) return;
            if (player.inventory.containerWear.itemList == null) return;
            if (!player_attributes.ContainsKey(player.userID)) player_attributes.Add(player.userID, new BuffSetInfo());
            else RemoveBonuses(player);
            if (player.inventory == null || player.inventory.containerWear.itemList == null) return;
            foreach (var item in player.inventory.containerWear.itemList)
            {
                if (item.name != null)
                {
                    UpdateBonuses(player, item, true);
                }
            }
        }

        // Call GetBuff method from UpdateBonuses.

        // Used to get the buff value based on the description of the item.

        public float GetBuffValue(string name)
        {
            if (string.IsNullOrEmpty(name)) return 0f;
            var splitString = name.Split('[', ']');
            if (splitString.Length < 2) return 0f;
            var splitResult = splitString[1].Split(' ');
            if (splitResult.Length < 2) return 0f;
            var result = Convert.ToSingle(splitResult[1]);
            return result;
        }

        string GetBuffTier(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var splitString = name.Split('[', ']');
            if (splitString.Length < 2) return null;
            var result = splitString[1].Split(' ')[0];
            return result;
        }

        // Get the buff from the item name
        public Buff GetBuff(string name)
        {
            if (string.IsNullOrEmpty(name)) return Buff.None;
            var buffStr = name.Split(' ')?.First() ?? null;
            if (string.IsNullOrEmpty(buffStr)) return Buff.None;
            //Buff value;
            if (parsedEnums.ContainsKey(buffStr)) return parsedEnums[buffStr];
            //if (Enum.TryParse(buffStr, out value)) return value;
            return Buff.None;
        }

        // Returns the total buff value for the specified buff (including worn and held).
        // Weapons will contribute to the active buff while being used, but will not count towards set bonus.
        public float GetTotalBuffValue(BasePlayer player, Buff buff)
        {
            var modifier = 0f;

            modifier += GetWornModifierValue(player.userID, buff);
            var item = player.GetActiveItem();
            if (item != null) modifier += GetItemModifierValue(item, buff);

            // Handles the BonusMultiplier set bonus

            // Adds the modifier based on the stored value in player_attributes
            modifier += GetBonusMultiplier(player, buff);

            return modifier;
        }


        public Dictionary<SetBonus, SetBonusValues> GetSetBonuses(int wornPieces, Dictionary<int, SetBonusEffect> setInfo)
        {
            var highest = 0;
            foreach (var setinfo in setInfo)
            {
                if (wornPieces >= setinfo.Key && setinfo.Key > highest)
                {
                    highest = setinfo.Key;
                }
            }
            SetBonusEffect sbe;
            return setInfo.TryGetValue(highest, out sbe) ? sbe.setBonus : null;
        }

        public float GetBonusMultiplier(BasePlayer player, Buff buff, int piecesWorn)
        {
            var setBonuses = GetSetBonuses(piecesWorn, config.enhancements[buff].setInfo);
            SetBonusValues values;
            if (setBonuses != null && setBonuses.TryGetValue(SetBonus.BonusMultiplier, out values)) return values.modifier;
            else return 0f;
        }

        public float GetBonusMultiplier(BasePlayer player, Buff buff)
        {
            BuffSetInfo bsi;
            if (player_attributes.TryGetValue(player.userID, out bsi) && bsi.setBonusMultiplier.ContainsKey(buff)) return bsi.setBonusMultiplier[buff];
            return 0f;
        }        

        void RemoveFromAllGroups(string id, List<string> groups)
        {
            if (groups == null) return;
            foreach (var group in groups)
            {
                permission.RemoveUserGroup(id, group);
            }
        }

        public Dictionary<SetBonus, SetBonusValues> GetAllSetBonuses(BasePlayer player)
        {
            BuffSetInfo bsi;
            if (!player_attributes.TryGetValue(player.userID, out bsi) || bsi.buffs.Count == 0) return null;
            Dictionary<SetBonus, SetBonusValues> bonuses = null;
            foreach (var attribute in bsi.buffs)
            {
                var wornPieces = GetWornPiecesCount(player, attribute.Key);
                var setbonuses = GetSetBonuses(wornPieces, config.enhancements[attribute.Key].setInfo);
                foreach (var set in setbonuses)
                {
                    if (bonuses == null) bonuses = new Dictionary<SetBonus, SetBonusValues>();
                    // If the buff exists, it will add the value onto the entry.
                    if (bonuses.ContainsKey(set.Key)) bonuses[set.Key].modifier += set.Value.modifier;
                    else bonuses.Add(set.Key, set.Value);
                }
            }
            return bonuses;
        }

        public int GetWornPiecesCount(BasePlayer player, Buff buff)
        {
            BuffSetInfo bsi;
            if (!player_attributes.TryGetValue(player.userID, out bsi) || !bsi.buffs.ContainsKey(buff)) return 0;
            return bsi.buffs[buff].piecesWorn;
        }

        // Checks to see if the held entity has a buff value, and if it matches our queried value, and if so, returns the modifier.
        public float GetItemModifierValue(Item item, Buff buff)
        {
            if (buff != Buff.None && item.name != null && GetBuff(item.name) == buff) return GetBuffValue(item.name);
            return 0f;
        }

        // Gets the total buff value for all items worn.
        public float GetWornModifierValue(ulong id, Buff buff)
        {
            BuffSetInfo bsi;
            BuffAttributes ba;
            if (!player_attributes.TryGetValue(id, out bsi) || !bsi.buffs.TryGetValue(buff, out ba)) return 0f;
            return ba.totalModifier;
        }

        public Dictionary<SetBonus, SetBonusValues> GetSetBonuses(BasePlayer player)
        {
            BuffSetInfo bsi;
            if (!player_attributes.TryGetValue(player.userID, out bsi) || bsi.setBonuses.Count == 0) return null;
            return bsi.setBonuses;
        }

        #endregion

        #region LootSpawn

        List<LootableCorpse> looted_corpses = new List<LootableCorpse>();

        Dictionary<string, List<LootableCorpse>> npc_corpses = new Dictionary<string, List<LootableCorpse>>();

        Dictionary<string, float> SkillTreeChancePerms = new Dictionary<string, float>();

        LootableCorpse OnCorpsePopulate(BasePlayer npcPlayer, LootableCorpse corpse)
        {
            if (corpse == null || npcPlayer == null || !npc_corpses.ContainsKey(npcPlayer.ShortPrefabName)) return null;
            if (!npc_corpses[npcPlayer.ShortPrefabName].Contains(corpse)) npc_corpses[npcPlayer.ShortPrefabName].Add(corpse);
            return null;
        }

        float GetLootChance(string userIDString, float luck)
        {
            float highest = 1;
            foreach (var perm in config.general_settings.perm_chance_modifier_loot)
            {
                if (permission.UserHasPermission(userIDString, perm.Key))
                {
                    if (highest == 1) highest = perm.Value;
                    else if (perm.Value > highest) highest = perm.Value;
                }                
            }
            var skillTreeModifier = 0f;
            foreach (var perm in SkillTreeChancePerms)
            {
                if (permission.UserHasPermission(userIDString, perm.Key))
                {
                    if (skillTreeModifier == 0) skillTreeModifier = perm.Value;
                    else if (perm.Value > skillTreeModifier) skillTreeModifier = perm.Value;
                }
            }
            return luck * (highest + skillTreeModifier);
        }

        void CanLootEntity(BasePlayer player, LootableCorpse corpse)
        {
            if (player == null || corpse == null || !corpse.ShortPrefabName.Equals("scientist_corpse", StringComparison.OrdinalIgnoreCase) || looted_corpses.Contains(corpse)) return;
            if (!permission.UserHasPermission(player.UserIDString, perm_drop)) return;
            looted_corpses.Add(corpse);
            foreach (var type in npc_corpses)
            {
                if (type.Value.Contains(corpse))
                {
                    type.Value.Remove(corpse);
                    if (!config.corpses.corpses.ContainsKey(type.Key)) return;
                    var chance = GetLootChance(player.UserIDString, config.corpses.corpses[type.Key]);
                    if (UnityEngine.Random.Range(0f, 100f) >= 100f - chance)
                    {
                        var item = GenerateRandomItem();
                        if (item == null) return;
                        foreach (var container in corpse.containers)
                        {
                            container.capacity++;
                            if (item.MoveToContainer(container)) return;
                        }
                        item.DropAndTossUpwards(corpse.transform.position);
                        if (config.general_settings.message_on_first_loot && !NotifyPlayers.Contains(player.userID))
                        {
                            var buff = GetBuff(item.name).ToString();
                            NotifyPlayers.Add(player.userID);
                            string notification = string.Format(lang.GetMessage("FirstFindLoot", this, player.UserIDString), buff, lang.GetMessage("Info" + buff, this, player.UserIDString), config.general_settings.ui_commands.First());
                            PrintToChat(player, notification);
                            if (config.notificationSettings.notify.enabled && UINotify != null && UINotify.IsLoaded && permission.UserHasPermission(player.UserIDString, "uinotify.see")) UINotify.Call("SendNotify", player.userID, config.notificationSettings.notify.messageType, lang.GetMessage("Notify_FirstFindLoot", this, player.UserIDString));
                        }
                        return;
                    }
                    return;
                }
            }
        }

        void OnEntityKill(LootableCorpse corpse)
        {
            foreach (var type in npc_corpses)
            {
                if (type.Value.Contains(corpse))
                {
                    type.Value.Remove(corpse);
                    break;
                }                    
            }
            looted_corpses.Remove(corpse);
        }

        List<LootContainer> CheckedContainers = new List<LootContainer>();

        void CanLootEntity(BasePlayer player, LootContainer container)
        {
            if (container == null || CheckedContainers.Contains(container) || !config.containers.containers.ContainsKey(container.ShortPrefabName)) return;
            if (!permission.UserHasPermission(player.UserIDString, perm_drop)) return;
            CheckedContainers.Add(container);
            var chance = config.containers.containers[container.ShortPrefabName];

            if (chance == 0) return;

            if (config.containers.use_default_loot_table)
            {
                if (UnityEngine.Random.Range(0f, 100f) >= 100f - GetLootChance(player.UserIDString, chance) && Interface.CallHook("CanReceiveEpicLootFromCrate", player, container) == null)
                {
                    var shortnames = GetLootTable(container);
                    if (shortnames == null) return;
                    var item = GenerateRandomItem(null, shortnames);
                    if (item == null) return;

                    container.inventory.capacity++;
                    container.inventorySlots++;
                    if (!item.MoveToContainer(container.inventory)) item.Remove();
                    else
                    {
                        if (config.general_settings.message_on_first_loot && !NotifyPlayers.Contains(player.userID))
                        {
                            var buff = GetBuff(item.name).ToString();
                            NotifyPlayers.Add(player.userID);
                            string notification = string.Format(lang.GetMessage("FirstFindLoot", this, player.UserIDString), buff, lang.GetMessage("Info" + buff, this, player.UserIDString), config.general_settings.ui_commands.First());
                            PrintToChat(player, notification);
                            if (config.notificationSettings.notify.enabled && UINotify != null && UINotify.IsLoaded && permission.UserHasPermission(player.UserIDString, "uinotify.see")) UINotify.Call("SendNotify", player.userID, config.notificationSettings.notify.messageType, lang.GetMessage("Notify_FirstFindLoot", this, player.UserIDString));
                        }
                    }
                }                
            }
            else
            {
                if (UnityEngine.Random.Range(0f, 100f) >= 100f - GetLootChance(player.UserIDString, chance) && Interface.CallHook("CanReceiveEpicLootFromCrate", player, container) == null)
                {
                    var item = GenerateRandomItem();
                    if (item == null) return;

                    container.inventory.capacity++;
                    container.inventorySlots++;
                    if (!item.MoveToContainer(container.inventory)) item.Remove();
                }
            }
            
            return;
        }

        //test
        private const int LAYER_TARGET = ~(1 << 2 | 1 << 3 | 1 << 4 | 1 << 10 | 1 << 18 | 1 << 28 | 1 << 29);
        private BaseEntity GetTargetEntity(BasePlayer player)
        {
            RaycastHit raycastHit;
            bool flag = Physics.Raycast(player.eyes.HeadRay(), out raycastHit, 5, LAYER_TARGET);
            var targetEntity = flag ? raycastHit.GetEntity() : null;
            return targetEntity;
        }

        List<string> GetLootTable(LootContainer container)
        {
            if (container == null) return null;
            var loot = GameManager.server.FindPrefab(container.PrefabName)?.GetComponent<LootContainer>();
            if (loot == null)
                return null;
            List<string> table = new List<string>();
            if (loot.lootDefinition != null) table.AddRange(GetLootFromTable(loot.lootDefinition));
            else
            {
                if (loot.LootSpawnSlots.Length > 0)
                {
                    LootContainer.LootSpawnSlot[] lootSpawnSlots = loot.LootSpawnSlots;
                    foreach (var slot in lootSpawnSlots)
                    {
                        table.AddRange(GetLootFromTable(slot.definition));
                    }
                }
            }
            return table;
        }

        //endtest

        List<string> GetLootFromTable(LootSpawn profile)
        {
            if (profile == null) return null;
            var result = new List<string>();
            if (profile.subSpawn != null && profile.subSpawn.Length > 0)
            {
                foreach (var cat in profile.subSpawn)
                {
                    var list = GetLootFromTable(cat.category);
                    if (list != null) result.AddRange(list);
                }
                return result;
            }
            if (profile.items != null && profile.items.Length > 0)
            {
                result.AddRange(profile.items.Select(x => x.itemDef.shortname));                
            }
            return result;
        }
        
        void OnEntityKill(LootContainer container)
        {
            CheckedContainers.Remove(container);
        }

        #endregion

        #region CUI

        #region Icon_mover_menu

        [ChatCommand("elmove")] 
        void MoveButtonCMD(BasePlayer player)
        {
            SendButtonRepositionMenu(player);
        }

        [ChatCommand("reseticonpositions")]
        void ResetIconPositions(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, perm_admin)) return;

            var offsetMin_x = Convert.ToSingle(config.button_info.offsetMin[0]);
            var offsetMin_y = Convert.ToSingle(config.button_info.offsetMin[1]);
            var offsetMax_x = Convert.ToSingle(config.button_info.offsetMax[0]);
            var offsetMax_y = Convert.ToSingle(config.button_info.offsetMax[1]);
            foreach (var p in BasePlayer.activePlayerList)
            {
                PlayerSettings ps;
                if (pcdData.pEntity.TryGetValue(p.userID, out ps)) 
                {
                    ps.offsetMin_x = offsetMin_x;
                    ps.offsetMin_y = offsetMin_y;                    
                    ps.offsetMax_x = offsetMax_x;
                    ps.offsetMax_y = offsetMax_y;
                    if (p.IsConnected) SendInspectMenuButton(p);
                }
            }
        }

        private void SendButtonRepositionMenu(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "1 1 1 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-37.953 -38.007", OffsetMax = "37.952 38.007" }
            }, "Overlay", "EL_Button_Repos");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2641509 0.2641509 0.2641509 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-37.96 -11.992", OffsetMax = "-13.96 12.008" }
            }, "EL_Button_Repos", "EL_Button_Repos_Left");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1132075 0.1132075 0.1132075 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-10 -10", OffsetMax = "10 10" }
            }, "EL_Button_Repos_Left", "EL_Button_Repos_Inner_Left");

            container.Add(new CuiElement
            {
                Name = "EL_Button_Repos_Text_Left",
                Parent = "EL_Button_Repos_Left",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIMoveL", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-10 -10", OffsetMax = "10 10" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = $"repositionelicon {"left"}" },
                Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-12 -12", OffsetMax = "12 12" }
            }, "EL_Button_Repos_Left", "EL_Button_Repos_Button_Left");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2641509 0.2641509 0.2641509 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-12.05 14.008", OffsetMax = "11.95 38.008" }
            }, "EL_Button_Repos", "EL_Button_Repos_Up");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1132075 0.1132075 0.1132075 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-10 -10", OffsetMax = "10 10" }
            }, "EL_Button_Repos_Up", "EL_Button_Repos_Inner_Up");

            container.Add(new CuiElement
            {
                Name = "EL_Button_Repos_Text_Up",
                Parent = "EL_Button_Repos_Up",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIMoveU", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-10 -10", OffsetMax = "10 10" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = $"repositionelicon {"up"}" },
                Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-12 -12", OffsetMax = "12 12" }
            }, "EL_Button_Repos_Up", "EL_Button_Repos_Button_Up");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2641509 0.2641509 0.2641509 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "13.95 -12", OffsetMax = "37.95 12" }
            }, "EL_Button_Repos", "EL_Button_Repos_Right");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1132075 0.1132075 0.1132075 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-10 -10", OffsetMax = "10 10" }
            }, "EL_Button_Repos_Right", "EL_Button_Repos_Inner_Right");

            container.Add(new CuiElement
            {
                Name = "EL_Button_Repos_Text_Right",
                Parent = "EL_Button_Repos_Right",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIMoveR", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-10 -10", OffsetMax = "10 10" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = $"repositionelicon {"right"}" },
                Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-12 -12", OffsetMax = "12 12" }
            }, "EL_Button_Repos_Right", "EL_Button_Repos_Button_Right");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2641509 0.2641509 0.2641509 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-12.05 -38.007", OffsetMax = "11.95 -14.007" }
            }, "EL_Button_Repos", "EL_Button_Repos_Down");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1132075 0.1132075 0.1132075 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-10 -10", OffsetMax = "10 10" }
            }, "EL_Button_Repos_Down", "EL_Button_Repos_Inner_Down");

            container.Add(new CuiElement
            {
                Name = "EL_Button_Repos_Text_Down",
                Parent = "EL_Button_Repos_Down",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIMoveD", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-10 -10", OffsetMax = "10 10" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = $"repositionelicon {"down"}" },
                Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-12 -12", OffsetMax = "12 12" }
            }, "EL_Button_Repos_Down", "EL_Button_Repos_Button_Down");

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = "closerepositioniconmenu" },
                Text = { Text = "X", Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-8.005 -8", OffsetMax = "7.995 8" }
            }, "EL_Button_Repos", "EL_Button_Repos_close");

            CuiHelper.DestroyUi(player, "EL_Button_Repos");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("closerepositioniconmenu")]
        void CloseReposMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, "EL_Button_Repos");
        }

        [ConsoleCommand("repositionelicon")]
        void RepositionIcon(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            PlayerSettings pi = SetupDefaultPlayerUI(player);

            if (!pi.showHud) pi.showHud = true;

            var direction = arg.Args?.First();
            switch (direction)
            {
                case "left":
                    pi.offsetMin_x -= 5;
                    pi.offsetMax_x -= 5;
                    break;
                case "right":
                    pi.offsetMin_x += 5;
                    pi.offsetMax_x += 5;
                    break;
                case "up":
                    pi.offsetMin_y += 5;
                    pi.offsetMax_y += 5;
                    break;
                case "down":
                    pi.offsetMin_y -= 5;
                    pi.offsetMax_y -= 5;
                    break;
            }
            SendInspectMenuButton(player);
        }

        PlayerSettings SetupDefaultPlayerUI(BasePlayer player)
        {
            PlayerSettings pi;
            if (pcdData.pEntity.TryGetValue(player.userID, out pi)) return pi;
            var x_min = Convert.ToSingle(config.button_info.offsetMin.Split(' ').First());
            var y_min = Convert.ToSingle(config.button_info.offsetMin.Split(' ').Last());

            var x_max = Convert.ToSingle(config.button_info.offsetMax.Split(' ').First());
            var y_max = Convert.ToSingle(config.button_info.offsetMax.Split(' ').Last());
            pcdData.pEntity.Add(player.userID, pi = new PlayerSettings(x_min, y_min, x_max, y_max));
            return pi;
        }

        #endregion

        #region Inspection Menu Button

        private void SendInspectMenuButton(BasePlayer player)
        {
            if (!config.button_info.enable_button) return;
            if (player.IsNpc || !player.userID.IsSteamId()) return;
            if (!permission.UserHasPermission(player.UserIDString, perm_use)) return;
            if (player.IsDead()) return;
            var container = new CuiElementContainer();
            var pi = SetupDefaultPlayerUI(player);
            if (string.IsNullOrEmpty(icon_img))
            {
                CuiImageComponent img;
                if (!string.IsNullOrEmpty(icon_path)) img = new CuiImageComponent { Color = "1 1 1 1", Sprite = config.button_info.icon_path };
                else if (icon_id > 0) img = new CuiImageComponent { Color = "1 1 1 1", ItemId = 1776460938, SkinId = icon_id };
                else img = new CuiImageComponent { Color = "1 1 1 1", Sprite = "assets/icons/inventory.png" };
                container.Add(new CuiElement
                {
                    Name = "InspectButtonImg",
                    Parent = "Overlay",
                    Components = {
                    img,
                    new CuiRectTransformComponent { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = $"{pi.offsetMin_x} {pi.offsetMin_y}", OffsetMax = $"{pi.offsetMax_x} {pi.offsetMax_y}" }
                }
                });
            }
            else
            {
                container.Add(new CuiElement
                {
                    Name = "InspectButtonImg",
                    Parent = "Overlay",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = (string)ImageLibrary?.Call("GetImage", icon_img) },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = $"{pi.offsetMin_x} {pi.offsetMin_y}", OffsetMax = $"{pi.offsetMax_x} {pi.offsetMax_y}" }
                }
                });
            }
            
            
            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = "openinspectmenu" },
                Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-18 -18", OffsetMax = "18 18" }
            }, "InspectButtonImg", "InspectButton");

            CuiHelper.DestroyUi(player, "InspectButtonImg");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("openinspectmenu")]
        void OpenInspectMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || player.IsDead()) return;

            SendInspectionMenu(player);
        }

        void OpenInspectMenuChatCMD(BasePlayer player)
        {
            if (player.IsDead()) return;
            SendInspectionMenu(player);
        }

        #endregion

        #region Inspection Menu Main

        private void SendInspectionMenu(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, perm_use)) return;
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0.9568627" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-0.351 0.337", OffsetMax = "0.349 -0.323" }
            }, "Overlay", "InspectMenu");
            List<Item> items = Pool.GetList<Item>();
            if (player.inventory.containerWear.itemList != null)
            {
                foreach (var item in player.inventory.containerWear.itemList)
                {
                    if (item.name != null)
                    {
                        items.Add(item);
                    }
                }
            }
            var activeItem = player.GetActiveItem();
            if (activeItem != null && activeItem.name != null && GetBuff(activeItem.name) != Buff.None) items.Add(activeItem);
            var count = 0;
            var yposmin = -148.3f;
            var yposmax = -80.3;

            for (int i = 0; i < items.Count; i++)
            {
                var tier = GetBuffTier(items[i].name);
                if (tier == null) continue;
                Color col;
                string ColString = config.tier_information.tier_colours.ContainsKey(tier) ? ColorUtility.TryParseHtmlString(config.tier_information.tier_colours[tier], out col) ? $"{col.r} {col.g} {col.b} {col.a}" : "0.2641509 0.2641509 0.2641509 1" : "0.2641509 0.2641509 0.2641509 1";
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = ColString },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-151 + (count * 78)} {yposmin}", OffsetMax = $"{-83 + (count * 78)} {yposmax}" }
                }, "InspectMenu", $"InspectMenu_Slot_{i}");
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1132075 0.1132075 0.1132075 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-32 -32", OffsetMax = "32 32" }
                }, $"InspectMenu_Slot_{i}", "InspectMenu_InnerPanel");

                container.Add(new CuiElement
                {
                    Name = $"InspectMenu_Img_{i}",
                    Parent = $"InspectMenu_Slot_{i}",
                    Components = {
                    new CuiImageComponent { Color = "1 1 1 1", ItemId = items[i].info.itemid, SkinId = items[i].skin },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-32 -32", OffsetMax = "32 32" }
                }
                });
                container.Add(new CuiButton
                {
                    Button = { Color = "1 1 1 0", Command = $"inspectiteminformation {items[i].uid.Value}" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-32 -32", OffsetMax = "32 32" }
                }, $"InspectMenu_Img_{i}", "InspectMenu_Button");
                count++;
                if (count == 4)
                {
                    count = 0;
                    yposmin -= 78;
                    yposmax -= 78;
                }
            }
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2641509 0.2641509 0.2641509 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-151 -68.33", OffsetMax = "151 157.67" }
            }, "InspectMenu", "InspectMenu_Body_Outer");
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1132075 0.1132075 0.1132075 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-149 -111", OffsetMax = "149 111" }
            }, "InspectMenu_Body_Outer", "InspectMenu_Body_Inner");
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2641509 0.2641509 0.2641509 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-151 161.67", OffsetMax = "151 203.67" }
            }, "InspectMenu", "InspectMenu_Header_outer");
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1132075 0.1132075 0.1132075 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-149 -19.001", OffsetMax = "149 19" }
            }, "InspectMenu_Header_outer", "InspectMenu_Header_inner");
            container.Add(new CuiElement
            {
                Name = "EquipMenuTitle",
                Parent = "InspectMenu_Header_outer",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIMainTitle", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-149 -19.001", OffsetMax = "149 19" }
                }
            });
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1981132 0.1981132 0.1981132 0.5019608" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-143 128.667", OffsetMax = "143 150.673" }
            }, "InspectMenu", "Buff_info_panel");
            container.Add(new CuiElement
            {
                Name = "buff_info_bonus_buff_title",
                Parent = "Buff_info_panel",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIMainBuff", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 15, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-80 -10", OffsetMax = "0 10" }
                }
            });
            container.Add(new CuiElement
            {
                Name = "buff_info_bonus_buff_value",
                Parent = "Buff_info_panel",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIMainBonus", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 15, Align = TextAnchor.MiddleRight, Color = "0.3955572 0.8396226 0.08317016 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "0 -9.998", OffsetMax = "80 10.002" }
                }
            });
            BuffSetInfo bsi;
            if (player_attributes.TryGetValue(player.userID, out bsi) && bsi.buffs != null)
            {                
                var heldModifier = 0f;
                Buff heldBuff = Buff.None;
                if (activeItem != null && activeItem.name != null)
                {
                    heldBuff = GetBuff(activeItem.name);
                    if (heldBuff != Buff.None) heldModifier = GetBuffValue(activeItem.name);
                }
                count = 0;
                var fontSize = bsi.buffs.Count < 4 ? 12 : bsi.buffs.Count > 5 ? 6 : 8;
                foreach (var buff in bsi.buffs)
                {
                    var description = GetBuffTitle(player, buff.Key) ?? buff.Key.ToString();

                    var modifier = bsi.setBonusMultiplier.ContainsKey(buff.Key) ? buff.Value.totalModifier + bsi.setBonusMultiplier[buff.Key] : buff.Value.totalModifier;
                    if (heldBuff != Buff.None && buff.Key == heldBuff) modifier += heldModifier;

                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "0.1981132 0.1981132 0.1981132 0" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-143 {108.658 - (count * (fontSize == 12 ? 16 : fontSize == 8 ? 11 : 8))}", OffsetMax = $"143 {128.663 - (count * (fontSize == 12 ? 16 : fontSize == 8 ? 11 : 8))}" }
                    }, "InspectMenu", "buff_info_values");
                    container.Add(new CuiElement
                    {
                        Name = "bufftitle",
                        Parent = "buff_info_values",
                        Components = {
                        new CuiTextComponent { Text = description, Font = "robotocondensed-regular.ttf", FontSize = fontSize, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-143 -10", OffsetMax = "0 10" }
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Name = "buffvalue",
                        Parent = "buff_info_values",
                        Components = {
                        new CuiTextComponent { Text = $"+{modifier * 100}%", Font = "robotocondensed-regular.ttf", FontSize = fontSize, Align = TextAnchor.MiddleLeft, Color = "0.3955572 0.8396226 0.08317016 1" },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "45.497 -9.998", OffsetMax = "143 10.002" }
                        }
                    });
                    count++;
                }
                if (heldBuff != Buff.None && !bsi.buffs.ContainsKey(heldBuff))
                {
                    var description = GetBuffTitle(player, heldBuff);
                    var modifier = heldModifier;
                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "0.1981132 0.1981132 0.1981132 0" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-143 {102.658 - (count * (fontSize == 12 ? 16 : fontSize == 8 ? 11 : 8))}", OffsetMax = $"143 {122.663 - (count * (fontSize == 12 ? 16 : fontSize == 8 ? 11 : 8))}" }
                    }, "InspectMenu", "buff_info_values");
                    container.Add(new CuiElement
                    {
                        Name = "bufftitle",
                        Parent = "buff_info_values",
                        Components = {
                        new CuiTextComponent { Text = description, Font = "robotocondensed-regular.ttf", FontSize = fontSize, Align = TextAnchor.UpperCenter, Color = "1 1 1 1" },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-143 -10", OffsetMax = "0 10" }
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Name = "buffvalue",
                        Parent = "buff_info_values",
                        Components = {
                        new CuiTextComponent { Text = $"+{modifier * 100}%", Font = "robotocondensed-regular.ttf", FontSize = fontSize, Align = TextAnchor.UpperLeft, Color = "0.3955572 0.8396226 0.08317016 1" },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "45.497 -9.998", OffsetMax = "143 10.002" }
                        }
                    });
                }
            }
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1981132 0.1981132 0.1981132 0.5019608" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-143 34.668", OffsetMax = "143 54.673" }
            }, "InspectMenu", "buff_setbonus_panel");
            container.Add(new CuiElement
            {
                Name = "buff_setbonus_info",
                Parent = "buff_setbonus_panel",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIMainSetBonus", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 15, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-143 -10", OffsetMax = "143 10" }
                }
            });

            if (bsi != null && bsi.setBonuses != null)
            {
                string formattedText = null;
                foreach (var set in bsi.setBonuses)
                {
                    formattedText += $"<color=#42f105>+</color> {set.Key}: {string.Format(GetSetBonusDescription(player, set.Key), set.Value.modifier * 100)}\n";
                    var groupName = set.Key.ToString().ToLower();
                    if (set.Value.perms != null && set.Value.perms.Count > 0)
                    {
                        foreach (var perm in set.Value.perms)
                        {
                            formattedText += $"<color=#42f105>+</color> {perm.Value}\n";
                        }                        
                    }
                }
                if (bsi.setBonusMultiplier != null)
                {
                    foreach (var set in bsi.setBonusMultiplier)
                    {
                        if (set.Value <= 0) continue;
                        formattedText += string.Format(lang.GetMessage("UIMainSetBonusLine", this, player.UserIDString), set.Key, set.Value * 100);
                    }
                }
                if (string.IsNullOrEmpty(formattedText))
                {
                    formattedText = lang.GetMessage("UIMainSetBonusDefault", this, player.UserIDString);
                }
                container.Add(new CuiElement
                {
                    Name = "InspectMenu_SetBonus_Description",
                    Parent = "buff_setbonus_panel",
                    Components = {
                    new CuiTextComponent { Text = formattedText, Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-143 -107", OffsetMax = "143 -10.002" }
                    }
                });
            }

            container.Add(new CuiElement
            {
                Name = "inspectmenuclose",
                Parent = "InspectMenu",
                Components = {
                    new CuiTextComponent { Text = "X", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "151 209.67", OffsetMax = "175 233.67" }
                }
            });
            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = "closeinspectionmenu" },
                Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-12 -12", OffsetMax = "12 12" }
            }, "inspectmenuclose", "inspectmenuclose_bttn");

            // Inspect menu player settings button

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2641509 0.2641509 0.2641509 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "161.001 115.67", OffsetMax = "203.001 157.67" }
            }, "InspectMenu", "InspectMenu_Settings");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1132075 0.1132075 0.1132075 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-19 -19", OffsetMax = "19 19" }
            }, "InspectMenu_Settings", "InspectMenu_Header_Settings_Inner");

            container.Add(new CuiElement
            {
                Name = "InspectMenu_Header_Settings_Img",
                Parent = "InspectMenu_Header_Settings_Inner",
                Components = {
                    new CuiImageComponent { Color = "1 1 1 1", Sprite = "assets/icons/gear.png" },
                    new CuiOutlineComponent { Color = "0 0 0 0", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-16 -16", OffsetMax = "16 16" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = "epiclootplayersettingsmenu" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-19 -19", OffsetMax = "19 19" }
            }, "InspectMenu_Settings", "InspectMenu_Header_Settings_Button");

            // Info button

            var min_y = 63.67f;
            var max_y = 105.67f;

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2641509 0.2641509 0.2641509 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "161.001 63.67", OffsetMax = "203.001 105.67" }
            }, "InspectMenu", "InspectMenu_Info");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1132075 0.1132075 0.1132075 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-19 -19", OffsetMax = "19 19" }
            }, "InspectMenu_Info", "InspectMenu_Header_Info_Inner");

            container.Add(new CuiElement
            {
                Name = "InspectMenu_Header_Info_Img",
                Parent = "InspectMenu_Header_Info_Inner",
                Components = {
                    new CuiImageComponent { Color = "1 1 1 1", Sprite = "assets/icons/info.png" },
                    new CuiOutlineComponent { Color = "0 0 0 0", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-16 -16", OffsetMax = "16 16" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = "epiclootinfomenu" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-19 -19", OffsetMax = "19 19" }
            }, "InspectMenu_Info", "InspectMenu_Header_Info_Button");

            // Add any new buttons above these and assign the min_y and max_y values to its offset values

            // Item scrapper button

            if (config.scrapper_settings.enabled)
            {
                if (permission.UserHasPermission(player.UserIDString, perm_salvage))
                {
                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "0.2641509 0.2641509 0.2641509 1" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"161.001 {min_y -= 52}", OffsetMax = $"203.001 {max_y -= 52}" }
                    }, "InspectMenu", "InspectMenu_Info");

                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "0.1132075 0.1132075 0.1132075 1" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-19 -19", OffsetMax = "19 19" }
                    }, "InspectMenu_Info", "InspectMenu_Header_Info_Inner");

                    container.Add(new CuiElement
                    {
                        Name = "InspectMenu_Header_Info_Img",
                        Parent = "InspectMenu_Header_Info_Inner",
                        Components = {
                    new CuiImageComponent { Color = "1 1 1 1", Sprite = "assets/icons/refresh.png" },
                    new CuiOutlineComponent { Color = "0 0 0 0", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-16 -16", OffsetMax = "16 16" }
                }
                    });

                    container.Add(new CuiButton
                    {
                        Button = { Color = "1 1 1 0", Command = "epiclootsendsalvagermenu" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-19 -19", OffsetMax = "19 19" }
                    }, "InspectMenu_Info", "InspectMenu_Header_Info_Button");
                }               

                // Enhancement button
                if (permission.UserHasPermission(player.UserIDString, perm_enhance))
                {
                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "0.2641509 0.2641509 0.2641509 1" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"161.001 {min_y -= 52}", OffsetMax = $"203.001 {max_y -= 52}" }
                    }, "InspectMenu", "InspectMenu_enhancements");

                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "0.1132075 0.1132075 0.1132075 1" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-19 -19", OffsetMax = "19 19" }
                    }, "InspectMenu_enhancements", "InspectMenu_Header_Info_Inner");

                    container.Add(new CuiElement
                    {
                        Name = "InspectMenu_Header_Info_Img",
                        Parent = "InspectMenu_Header_Info_Inner",
                        Components = {
                    new CuiImageComponent { Color = "1 1 1 1", Sprite = "assets/icons/level_top.png" },
                    new CuiOutlineComponent { Color = "0 0 0 0", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-16 -16", OffsetMax = "16 16" }
                }
                    });

                    container.Add(new CuiButton
                    {
                        Button = { Color = "1 1 1 0", Command = "epiclootsendenhancementmenu" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-19 -19", OffsetMax = "19 19" }
                    }, "InspectMenu_enhancements", "InspectMenu_Header_Info_Button");
                }                
            }

            CuiHelper.DestroyUi(player, "InspectMenu");
            CuiHelper.AddUi(player, container);
            Pool.FreeList(ref items);
        }

        void CloseInspector(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "InspectMenu");
            CuiHelper.DestroyUi(player, "ItemDetails_Header_outer");
            CuiHelper.DestroyUi(player, "EL_Info");
            CuiHelper.DestroyUi(player, "EL_Settings");
        }

        [ConsoleCommand("epiclootsendenhancementmenu")]
        void SendEpicLootEnhancementMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            CloseInspector(player);
            SalvagerBackground(player);
            Enhancer_Market_item_select(player, true);
        }

        [ConsoleCommand("epiclootsendsalvagermenu")]
        void SendEpicLootSalvagerMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            CloseInspector(player);

            SendSalvagerMenu(player);
        }

        [ConsoleCommand("epiclootinfomenu")]
        void SendInfoMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            CuiHelper.DestroyUi(player, "EL_Settings");
            SendInspectionInfo(player);
        }

        [ConsoleCommand("epiclootplayersettingsmenu")]
        void SendSettingsPage(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            CuiHelper.DestroyUi(player, "EL_Info");
            SendSettingsMenu(player);
        }

        [ConsoleCommand("closeinspectionmenu")]
        void CloseMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            CloseInspector(player);
        }

        [ConsoleCommand("inspectiteminformation")]
        void InspectItem(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            var id = Convert.ToUInt32(arg.Args.First());
            SendItemDetailsMenu(player, id);
        }

        #endregion

        #region Inspection Menu detailed

        private void SendItemDetailsMenu(BasePlayer player, ulong uid)
        {
            if (!permission.UserHasPermission(player.UserIDString, perm_use)) return;
            Item focused_Item = null;
            if (player.inventory.containerWear.itemList != null)
            {
                foreach (var item in player.inventory.containerWear.itemList)
                {
                    if (item.uid.Value == uid)
                    {
                        focused_Item = item;
                        break;
                    }
                }
            }
            if (focused_Item == null)
            {
                var item = player.GetActiveItem();
                if (item != null && item.uid.Value == uid) focused_Item = item;
            }

            if (focused_Item == null || focused_Item.name == null) return;

            var buff = GetBuff(focused_Item.name);
            var tier = GetBuffTier(focused_Item.name);
            var modifier = GetBuffValue(focused_Item.name);
            
            var uiCol = config.tier_information.tier_colours.ContainsKey(tier) ? config.tier_information.tier_colours[tier] : "#FFFFFF";            

            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2641509 0.2641509 0.2641509 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-474.8 161.5", OffsetMax = "-172.8 203.5" }
            }, "Overlay", "ItemDetails_Header_outer");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1132075 0.1132075 0.1132075 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-149 -19.001", OffsetMax = "149 19" }
            }, "ItemDetails_Header_outer", "ItemDetails_Header_inner");
            
            var title = focused_Item.name.Split('[');
            container.Add(new CuiElement
            {
                Name = "ItemDetailsTitle",
                Parent = "ItemDetails_Header_outer",
                Components = {
                    new CuiTextComponent { Text = $"<color={uiCol}>{string.Join(" ", title.Take(title.Length - 1))}</color>", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-149 -19.001", OffsetMax = "149 19" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2641509 0.2641509 0.2641509 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-151 -97", OffsetMax = "-79 -25" }
            }, "ItemDetails_Header_outer", "ItemDetails_IMG_panel_outer");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1132075 0.1132075 0.1132075 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-34 -34", OffsetMax = "34 34" }
            }, "ItemDetails_IMG_panel_outer", "ItemDetails_IMG_panel_inner");

            container.Add(new CuiElement
            {
                Name = "Image_4040",
                Parent = "ItemDetails_IMG_panel_outer",
                Components = {
                    new CuiImageComponent { Color = "1 1 1 1", ItemId = focused_Item.info.itemid, SkinId = focused_Item.skin },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-34 -34", OffsetMax = "34 34" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2641509 0.2641509 0.2641509 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-75 -97", OffsetMax = "151 -25" }
            }, "ItemDetails_Header_outer", "ItemDetails_Info_panel_outer");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1132075 0.1132075 0.1132075 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-111 -34", OffsetMax = "111 34" }
            }, "ItemDetails_Info_panel_outer", "ItemDetails_Info_panel_inner");

            container.Add(new CuiElement
            {
                Name = "ItemDetailsBuffType",
                Parent = "ItemDetails_Info_panel_outer",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UILeftPanelBuff", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-107.4 10", OffsetMax = "-32.4 34" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "ItemDetailsTierTitle",
                Parent = "ItemDetails_Info_panel_outer",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UILeftPanelTier", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-107.4 -12", OffsetMax = "-32.4 12" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "ItemDetailsModifier",
                Parent = "ItemDetails_Info_panel_outer",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UILeftPanelModifier", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-107.4 -34", OffsetMax = "-32.4 -10" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "ItemDetailsBuffTypeVALUE",
                Parent = "ItemDetails_Info_panel_outer",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage(buff.ToString(), this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.UpperLeft, Color = "0.3955572 0.8396226 0.08317016 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-24.869 10", OffsetMax = "112.999 34" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "ItemDetailsTierTitleVALUE",
                Parent = "ItemDetails_Info_panel_outer",
                Components = {
                    new CuiTextComponent { Text = $"<color={uiCol}>{config.tier_information.tier_display_names[tier]}</color>", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.UpperLeft, Color = "1 0.8771619 0 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-24.869 -12.57", OffsetMax = "75.131 11.43" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "ItemDetailsModifierVALUE",
                Parent = "ItemDetails_Info_panel_outer",
                Components = {
                    new CuiTextComponent { Text = $"+{Math.Round(modifier * 100, 2)}%", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.UpperLeft, Color = "0.2916635 1 0 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-24.87 -34", OffsetMax = "111 -10" }
                }
            });

            string SetBonusDescription = "";

            if (config.enhancements[buff].setInfo != null)
            {
                foreach (var set in config.enhancements[buff].setInfo)
                {
                    SetBonusDescription += $"<size=8>[<color=#B71BA9>{set.Key}</color>]</size>";
                    foreach (var s in set.Value.setBonus)
                    {
                        SetBonusDescription += string.Format(lang.GetMessage("UILeftSideTierString", this, player.UserIDString), s.Key, string.Format(GetSetBonusDescription(player, s.Key), s.Value.modifier * 100));
                        if (s.Value.perms != null && s.Value.perms.Count > 0)
                        {
                            foreach (var perm in s.Value.perms)
                            {
                                SetBonusDescription += string.Format(lang.GetMessage("UILeftSidePerm", this, player.UserIDString), perm.Value);
                            }
                        }
                    }
                    SetBonusDescription += "\n\n";
                    
                }
            }

            container.Add(new CuiElement
            {
                Name = "ItemDetails_Buffdescription",
                Parent = "ItemDetails_Header_outer",
                Components = {
                    new CuiTextComponent { Text = string.Format(GetBuffDescription(player, buff) + "\n\n<size=12><color=#B71BA9>Set Bonuses - {1}</color></size>\n\n" + SetBonusDescription, Math.Round(modifier * 100, 2), lang.GetMessage(buff.ToString(), this, player.UserIDString)), Font = "robotocondensed-regular.ttf", FontSize = 8, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-151 -399.958", OffsetMax = "151 -101" }
                }
            });

            CuiHelper.DestroyUi(player, "ItemDetails_Header_outer");
            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Inspection Menu Info        

        private void SendInspectionInfo(BasePlayer player, int page = 0)
        {
            if (!Buff_Info_Pages.ContainsKey(page)) page = 0;

            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2641509 0.2641509 0.2641509 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "212.96 161.5", OffsetMax = "502.96 203.5" }
            }, "Overlay", "EL_Info");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1132075 0.1132075 0.1132075 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-143 -19.001", OffsetMax = "143 19" }
            }, "EL_Info", "EL_Info_Inner");

            container.Add(new CuiElement
            {
                Name = "EL_Info_Title",
                Parent = "EL_Info",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UISetTypes", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-143 -19.001", OffsetMax = "143 19" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2641509 0.2641509 0.2641509 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-145 -250.4", OffsetMax = "145 -24.4" }
            }, "EL_Info", "EL_Info_Body");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1132075 0.1132075 0.1132075 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-143 -111", OffsetMax = "143 111" }
            }, "EL_Info_Body", "EL_Info_Body_Inner");

            var count = 0;
            foreach (var entry in Buff_Info_Pages[page])
            {
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "1 1 1 0" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-143 {73 - (count * 37)}", OffsetMax = $"143 {110 - (count * 37)}" }
                }, "EL_Info_Body", "EL_Info_Description");

                container.Add(new CuiElement
                {
                    Name = "EL_Info_Description_Text",
                    Parent = "EL_Info_Description",
                    Components = {
                    new CuiTextComponent { Text = lang.GetMessage("Info"+entry.ToString(), this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-139 -18.5", OffsetMax = "139 18.5" }
                }
                });
                count++;
            }

            if (Buff_Info_Pages.ContainsKey(page - 1))
            {
                container.Add(new CuiButton
                {
                    Button = { Color = "1 1 1 0", Command = $"elsendinfopage {page - 1}" },
                    Text = { Text = "<<", Font = "robotocondensed-regular.ttf", FontSize = 24, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-51.672 -291.341", OffsetMax = "0 -256.398" }
                }, "EL_Info", "EL_Info_Button_Back");
            }

            if (Buff_Info_Pages.ContainsKey(page + 1))
            {
                container.Add(new CuiButton
                {
                    Button = { Color = "1 1 1 0", Command = $"elsendinfopage {page + 1}" },
                    Text = { Text = ">>", Font = "robotocondensed-regular.ttf", FontSize = 24, Align = TextAnchor.UpperRight, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "0 -291.341", OffsetMax = "51.672 -256.398" }
                }, "EL_Info", "EL_Info_Button_Forward");
            }

            CuiHelper.DestroyUi(player, "EL_Info");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("elsendinfopage")]
        void SendInfoPage(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            // Remove settings page
            SendInspectionInfo(player, Convert.ToInt32(arg.Args[0]));
        }

        #endregion

        #region Inspection Menu Settings

        private void SendSettingsMenu(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2641509 0.2641509 0.2641509 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "212.96 161.5", OffsetMax = "502.96 203.5" }
            }, "Overlay", "EL_Settings");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1132075 0.1132075 0.1132075 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-143 -19.001", OffsetMax = "143 19" }
            }, "EL_Settings", "EL_Settings_Inner");

            container.Add(new CuiElement
            {
                Name = "EL_Settings_Title",
                Parent = "EL_Settings",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIPlayerSettingsTitle", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-143 -19.001", OffsetMax = "143 19" }
                }
            });
            // Repos button menu
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2641509 0.2641509 0.2641509 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-145 -69.001", OffsetMax = "145 -25.001" }
            }, "EL_Settings", "EL_Settings_Body_1");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1132075 0.1132075 0.1132075 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-143 -20", OffsetMax = "143 20" }
            }, "EL_Settings_Body_1", "EL_Settings_Body_Inner");

            container.Add(new CuiElement
            {
                Name = "EL_Settings_Label",
                Parent = "EL_Settings_Body_1",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIReposButtonDescription", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-138.615 -20", OffsetMax = "63 20" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2627451 0.2627451 0.2627451 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "68 -15", OffsetMax = "138 15" }
            }, "EL_Settings_Body_1", "EL_Settings_Button_Panel");

            // Buttons that use player settings will need to pull data from player.

            container.Add(new CuiButton
            {
                Button = { Color = "0.1886792 0.1842292 0.1842292 1", Command = "elsendhudreposmenu" },
                Text = { Text = lang.GetMessage("UIReposButtonText", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 0.8144701 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-33 -13", OffsetMax = "33 13" }
            }, "EL_Settings_Button_Panel", "EL_Settings_Button");

            //"-145 -69.001", OffsetMax = "145 -25.001"
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2641509 0.2641509 0.2641509 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-145 -117.001", OffsetMax = "145 -73.001" }
            }, "EL_Settings", "EL_Settings_Body_2");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1132075 0.1132075 0.1132075 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-143 -20", OffsetMax = "143 20" }
            }, "EL_Settings_Body_2", "EL_Settings_Body_Inner");

            container.Add(new CuiElement
            {
                Name = "EL_Settings_Label",
                Parent = "EL_Settings_Body_2",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UINotificationsToggle", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-138.615 -20", OffsetMax = "63 20" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2627451 0.2627451 0.2627451 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "68 -15", OffsetMax = "138 15" }
            }, "EL_Settings_Body_2", "EL_Settings_Button_Panel");

            // Buttons that use player settings will need to pull data from player.

            PlayerSettings ps;
            var button = pcdData.pEntity.TryGetValue(player.userID, out ps) && ps.notifications ? lang.GetMessage("UIOn", this, player.UserIDString) : lang.GetMessage("UIOff", this, player.UserIDString);

            container.Add(new CuiButton
            {
                Button = { Color = "0.1886792 0.1842292 0.1842292 1", Command = "eltogglenotificationsmenu" },
                Text = { Text = button, Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 0.8144701 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-33 -13", OffsetMax = "33 13" }
            }, "EL_Settings_Button_Panel", "EL_Settings_Button");

            // 48 difference
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2641509 0.2641509 0.2641509 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-145 -165.001", OffsetMax = "145 -121.001" }
            }, "EL_Settings", "EL_Settings_Body_2");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1132075 0.1132075 0.1132075 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-143 -20", OffsetMax = "143 20" }
            }, "EL_Settings_Body_2", "EL_Settings_Body_Inner");

            container.Add(new CuiElement
            {
                Name = "EL_Settings_Label",
                Parent = "EL_Settings_Body_2",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIToggle", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-138.615 -20", OffsetMax = "63 20" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2627451 0.2627451 0.2627451 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "68 -15", OffsetMax = "138 15" }
            }, "EL_Settings_Body_2", "EL_Settings_Button_Panel");

            // Buttons that use player settings will need to pull data from player.

            button = ps != null && ps.showHud ? lang.GetMessage("UIOn", this, player.UserIDString) : lang.GetMessage("UIOff", this, player.UserIDString);

            container.Add(new CuiButton
            {
                Button = { Color = "0.1886792 0.1842292 0.1842292 1", Command = "eltoggleuibutton" },
                Text = { Text = button, Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 0.8144701 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-33 -13", OffsetMax = "33 13" }
            }, "EL_Settings_Button_Panel", "EL_Settings_Button");

            CuiHelper.DestroyUi(player, "EL_Settings");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("eltogglenotificationsmenu")]
        void ToggleNotifications(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            PlayerSettings pi = SetupDefaultPlayerUI(player);

            if (pi.notifications)
            {
                pi.notifications = false;
                PrintToChat(player, lang.GetMessage("elsettingsnotificationsoff", this, player.UserIDString));
            }
            else
            {
                PrintToChat(player, lang.GetMessage("elsettingsnotificationson", this, player.UserIDString));
                pi.notifications = true;
            }

            SendSettingsMenu(player);
        }

        [ConsoleCommand("eltoggleuibutton")]
        void ToggleHUDIcon(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            PlayerSettings pi = SetupDefaultPlayerUI(player);
            if (pi.showHud)
            {
                pi.showHud = false;
                PrintToChat(player, string.Format(lang.GetMessage("elhudoff", this, player.UserIDString), config.general_settings.ui_commands.First()));
                CuiHelper.DestroyUi(player, "InspectButtonImg");
            }
            else
            {
                PrintToChat(player, lang.GetMessage("elhudon", this, player.UserIDString));
                pi.showHud = true;
                SendInspectMenuButton(player);
            }

            SendSettingsMenu(player);
        }

        [ConsoleCommand("elsendhudreposmenu")]
        void SendReposMenu(ConsoleSystem.Arg arg)
        {

            var player = arg.Player();
            if (player == null) return;

            DestroyUIs(player, false);
            SendButtonRepositionMenu(player);
        }

        #endregion

        #region Salvager menu        

        void SendSalvagerMenu(BasePlayer player)
        {
            SalvagerBackground(player);
            EpicSalvager_SalvagerMenu(player);
        }

        #region Salvager CUI

        private void SalvagerBackground(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "1 1 1 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "471 195", OffsetMax = "-471 -195" }
            }, "Overlay", "SalvagerBackground");

            CuiHelper.DestroyUi(player, "SalvagerBackground");
            CuiHelper.AddUi(player, container);
        }

        private void EpicSalvager_SalvagerMenu(BasePlayer player, Item selected_item = null)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0.9803922" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-0.326 -0.332", OffsetMax = "0.674 0.338" }
            }, "Overlay", "EpicSalvager_SalvagerMenu");

            container.Add(new CuiElement
            {
                Name = "title",
                Parent = "EpicSalvager_SalvagerMenu",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UISalvagerTitleConfirm", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 24, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-206.99 147.9", OffsetMax = "206.99 183.9" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "title_instructions",
                Parent = "title",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UISalvagerClickAnItem", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-206.99 -51.4", OffsetMax = "206.99 -19.4" }
                }
            });
            List<Item> items = Pool.GetList<Item>();
            items.AddRange(player.inventory.AllItems());
            float min_y = 70;
            float max_y = 110;
            var foundAnItem = false;
            if (items != null)
            {                
                var menu_count = 0;
                var count = 0;
                var rows = 0;
                foreach (var item in items)
                {
                    if (item.amount > 1 || !EnhanceableItems.Contains(item.info.shortname) || item.name == null || GetBuff(item.name) == Buff.None) continue;
                    var _tier = GetBuffTier(item.name);
                    if (_tier == null) continue;
                    Color col;
                    string ColString = config.tier_information.tier_colours.ContainsKey(_tier) ? ColorUtility.TryParseHtmlString(config.tier_information.tier_colours[_tier], out col) ? $"{col.r} {col.g} {col.b} {col.a}" : "0.2641509 0.2641509 0.2641509 1" : "0.2641509 0.2641509 0.2641509 1";
                    foundAnItem = true;
                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = ColString },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-125 + (count * 42)} {70 - (rows * 42)}", OffsetMax = $"{-85 + (count * 42)} {110 - (rows * 42)}" }
                    }, "EpicSalvager_SalvagerMenu", $"item_{menu_count}");
                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "0.1803922 0.1803922 0.1803922 1" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-19 -19", OffsetMax = "19 19" }
                    }, $"item_{menu_count}", "item_frontpanel");
                    container.Add(new CuiElement
                    {
                        Name = "item_img",
                        Parent = $"item_{menu_count}",
                        Components = {
                        new CuiImageComponent { Color = "1 1 1 1", ItemId = item.info.itemid, SkinId = item.skin },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-19 -19", OffsetMax = "19 19" }
                    }
                    });
                    container.Add(new CuiButton
                    {
                        Button = { Color = "1 1 1 0", Command = $"esscrappersendmenu {item.uid.Value}" },
                        Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-19 -19", OffsetMax = "19 19" }
                    }, $"item_{menu_count}", "item_button");
                    menu_count++;
                    count++;
                    if (count >= 6)
                    {
                        count = 0;
                        rows++;
                    }
                    min_y = 72 - (rows * 42);
                    max_y = 100 - (rows * 42);
                }
            }
            Pool.FreeList(ref items);
            if (selected_item != null)
            {                
                var tier = GetBuffTier(selected_item.name);
                var modifier = Convert.ToInt32(GetBuffValue(selected_item.name) * 100);
                var uiCol = config.tier_information.tier_colours.ContainsKey(tier) ? config.tier_information.tier_colours[tier] : "#FFFFFF";
                var title = selected_item.name.Split('[');
                Color col;
                string ColString = config.tier_information.tier_colours.ContainsKey(tier) ? ColorUtility.TryParseHtmlString(config.tier_information.tier_colours[tier], out col) ? $"{col.r} {col.g} {col.b} {col.a}" : "0.2641509 0.2641509 0.2641509 1" : "0.2641509 0.2641509 0.2641509 1";
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = ColString },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-125 {min_y - 82}", OffsetMax = $"-61 {max_y - 46}" }
                }, "EpicSalvager_SalvagerMenu", "Selected_item");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1803922 0.1803922 0.1803922 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30 -30", OffsetMax = "30 30" }
                }, "Selected_item", "selected_item_frontpanel");

                container.Add(new CuiElement
                {
                    Name = "selected_item_img",
                    Parent = "Selected_item",
                    Components = {
                    new CuiImageComponent { Color = "1 1 1 1", ItemId = selected_item.info.itemid, SkinId = selected_item.skin },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30 -30", OffsetMax = "30 30" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "selected_item_name",
                    Parent = "Selected_item",
                    Components = {
                    new CuiTextComponent { Text = "NAME:", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "41.286 12", OffsetMax = "100.362 32" }
                }
                });
                
                container.Add(new CuiElement
                {
                    Name = "selected_item_name_entry",
                    Parent = "Selected_item",
                    Components = {
                    new CuiTextComponent { Text = $"<color={uiCol}>{string.Join(" ", title.Take(title.Length - 1))}</color>", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "104.525 11.999", OffsetMax = "459.495 32.001" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "selected_item_tier",
                    Parent = "Selected_item",
                    Components = {
                    new CuiTextComponent { Text = "TIER:", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "41.286 -8", OffsetMax = "100.362 12" }
                }
                });

                string tier_name;
                container.Add(new CuiElement
                {
                    Name = "selected_item_tier_entry",
                    Parent = "Selected_item",
                    Components = {
                    new CuiTextComponent { Text = $"<color={uiCol}>{(config.tier_information.tier_display_names.TryGetValue(tier, out tier_name) ? tier_name.TitleCase() : tier.ToUpper())}</color>", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "104.525 -8", OffsetMax = "459.495 12" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "selected_item_modifier",
                    Parent = "Selected_item",
                    Components = {
                    new CuiTextComponent { Text = "MODIFIER:", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "41.284 -28", OffsetMax = "100.362 -8" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "selected_item_modifier_entry",
                    Parent = "Selected_item",
                    Components = {
                    new CuiTextComponent { Text = $"<color={uiCol}>{modifier}%</color>", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "104.522 -28", OffsetMax = "459.495 -8" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "selected_item_value",
                    Parent = "Selected_item",
                    Components = {
                    new CuiTextComponent { Text = string.Format(lang.GetMessage("UISalvageDescription", this, player.UserIDString), config.scrapper_settings.scrapper_value[tier], CurrencyName), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-31.997 -68", OffsetMax = "459.493 -36" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "0.1803922 0.1803922 0.1803922 0.6666667", Command = $"scrapperdosalvage {selected_item.uid.Value}" },
                    Text = { Text = lang.GetMessage("UISalvageButton", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-32 -92", OffsetMax = "32 -68" }
                }, "Selected_item", "selected_item1_salvage_button");
            }
            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = "closesalvagermenu" },
                Text = { Text = "X", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "206.99 147.9", OffsetMax = "242.99 183.9" }
            }, "EpicSalvager_SalvagerMenu", "close_menu_button");

            if (!foundAnItem)
            {
                container.Add(new CuiElement
                {
                    Name = "no_equip_found",
                    Parent = "EpicSalvager_SalvagerMenu",
                    Components = {
                    new CuiTextComponent { Text = "<color=#ffd800>У вас нет улучшенного снаряжения для обработки. Добавьте улучшенное снаряжение в основной инвентарь и откройте это меню снова.</color>", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-150 62", OffsetMax = "150 110" }
                }
                });
            }

            CuiHelper.DestroyUi(player, "EpicSalvager_SalvagerMenu");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("scrapperdosalvage")]
        void ConfirmSalvage(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            var id = Convert.ToUInt32(arg.Args[0]);
            var item = player.inventory.AllItems()?.FirstOrDefault(x => x.uid.Value == id);
            if (item != null)
            {
                CuiHelper.DestroyUi(player, "EpicSalvager_SalvagerMenu");

                var tier = GetBuffTier(item.name);
                int amount;
                if (!config.scrapper_settings.scrapper_value.TryGetValue(tier, out amount)) return;
                item.RemoveFromContainer();
                var payment = ItemManager.CreateByName(config.scrapper_settings.currency_shortname, Math.Max(amount, 1), config.scrapper_settings.currency_skin);
                if (payment == null) return;
                if (!string.IsNullOrEmpty(config.scrapper_settings.currency_name)) payment.name = config.scrapper_settings.currency_name;
                player.GiveItem(payment);
                NextTick(() =>
                {
                    item.Remove();
                });
                EpicSalvager_SalvagerMenu(player);
                PrintToChat(player, string.Format(lang.GetMessage("ScrapperReceivedCurrency", this, player.UserIDString), amount, CurrencyName));
            }
            else PrintToChat(player, "Invalid item.");
        }

        [ConsoleCommand("closesalvagermenu")]
        void CloseSalvagerMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, "EpicSalvager_SalvagerMenu");
            CuiHelper.DestroyUi(player, "SalvagerBackground");

            SendInspectionMenu(player);
        }

        [ConsoleCommand("esscrappersendmenu")]
        void SendItemInfo(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            var id = Convert.ToUInt32(arg.Args[0]);
            var item = player.inventory.AllItems()?.FirstOrDefault(x => x.uid.Value == id);
            if (item != null) EpicSalvager_SalvagerMenu(player, item);
            else PrintToChat(player, "Invalid item.");
        }

        #endregion        

        #region Enhancer market item select 

        private void Enhancer_Market_item_select(BasePlayer player, bool from_menu = false)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0.9803922" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-0.326 -0.332", OffsetMax = "0.674 0.338" }
            }, "Overlay", "Salvager_Market_item_select");

            container.Add(new CuiElement
            {
                Name = "title",
                Parent = "Salvager_Market_item_select",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIEquipmentEnhancerTitle", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 24, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-150.162 147.566", OffsetMax = "149.838 183.566" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "title_instructions",
                Parent = "title",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIEquipmentEnhancerDescription", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-206.99 -51.4", OffsetMax = "206.99 -19.4" }
                }
            });

            List<Item> items = Pool.GetList<Item>();
            items.AddRange(player.inventory.AllItems());

            if (items != null)
            {
                var menu_count = 0;
                var count = 0;
                var rows = 0;
                foreach (var item in items)
                {
                    if (item.amount > 1 || !EnhanceableItems.Contains(item.info.shortname) || (item.name != null && GetBuff(item.name) != Buff.None) || config.skin_blacklist.Contains(item.skin)) continue;
                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "0.1792453 0.1792453 0.1792453 1" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-125 + (count * 42)} {70 - (rows * 42)}", OffsetMax = $"{-85 + (count * 42)} {110 - (rows * 42)}" }
                    }, "Salvager_Market_item_select", "item_");
                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "0.1803922 0.1803922 0.1803922 1" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-19 -19", OffsetMax = "19 19" }
                    }, "item_", "item_frontpanel");
                    container.Add(new CuiElement
                    {
                        Name = "item_img",
                        Parent = "item_",
                        Components = {
                        new CuiImageComponent { Color = "1 1 1 1", ItemId = item.info.itemid, SkinId = item.skin },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-19 -19", OffsetMax = "19 19" }
                    }
                    });
                    container.Add(new CuiButton
                    {
                        Button = { Color = "1 1 1 0", Command = $"marketselectitem {item.uid.Value}" },
                        Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-19 -19", OffsetMax = "19 19" }
                    }, "item_", "item_button");
                    menu_count++;
                    count++;
                    if (count >= 6)
                    {
                        count = 0;
                        rows++;
                    }
                }
            }
            Pool.FreeList(ref items);
            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = $"closesalvagerenhancer {(from_menu ? "back" : "")}" },
                Text = { Text = "X", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "149.84 147.57", OffsetMax = "185.84 183.57" }
            }, "Salvager_Market_item_select", "closebutton");

            CuiHelper.DestroyUi(player, "Salvager_Market_item_select");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("closesalvagerenhancer")]
        void CLoseSalvagerEnhancer_ItemSelect(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            CuiHelper.DestroyUi(player, "Salvager_Market_item_select");
            CuiHelper.DestroyUi(player, "SalvagerBackground");

            if (arg.Args.Length > 0 && !string.IsNullOrEmpty(arg.Args[0])) SendInspectionMenu(player);
        }

        [ConsoleCommand("marketselectitem")]
        void SelectItemForEnhancement(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            CuiHelper.DestroyUi(player, "Salvager_Market_item_select");
            var id = Convert.ToUInt32(arg.Args[0]);
            var item = player.inventory.AllItems()?.FirstOrDefault(x => x.uid.Value == id);
            if (item == null)
            {
                PrintToChat(player, lang.GetMessage("ItemMissing", this, player.UserIDString));
                return;
            }
            Salvager_Market_buff_select(player, item);
        }

        #endregion

        #region Enhancer Market buff select

        private void Salvager_Market_buff_select(BasePlayer player, Item item)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0.9490196" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-0.686 0.003", OffsetMax = "1.013 0.673" }
            }, "Overlay", "Salvager_Market_buff_select");
            container.Add(new CuiElement
            {
                Name = "title",
                Parent = "Salvager_Market_buff_select",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIEnhancerTitle", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 24, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-149.99 147.566", OffsetMax = "150.01 183.566" }
                }
            });
            container.Add(new CuiElement
            {
                Name = "title_instructions",
                Parent = "title",
                Components = {
                    new CuiTextComponent { Text = string.Format(lang.GetMessage("UIEnhancerSelectEnhancement", this, player.UserIDString), item.info.displayName.english), Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-206.99 -51.4", OffsetMax = "206.99 -19.4" }
                }
            });
            var count = 0;
            var rows = 0;
            foreach (var buff in parsedEnums)
            {
                EnhancementInfo ei;
                if (!config.enhancements.TryGetValue(buff.Value, out ei) || !ei.enabled || (ei.item_whitelist != null && ei.item_whitelist.Count > 0 && !ei.item_whitelist.Contains(item.info.shortname)) || (ei.item_blacklist != null && ei.item_blacklist.Contains(item.info.shortname))) continue;
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.05660379 0.05660379 0.05660379 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-322 + (count * 108) } {64.8 - (rows * 32)}", OffsetMax = $"{-218 + (count * 108)} {92.8 - (rows * 32)}" }
                }, "Salvager_Market_buff_select", "Salvager_Buff_backpanel");

                container.Add(new CuiButton
                {
                    Button = { Color = "0.1803922 0.1803922 0.1803922 1", Command = $"confirmcostwithbuff {item.uid.Value} {buff.Key}" },
                    Text = { Text = lang.GetMessage(buff.Key, this, player.UserIDString).ToUpper(), Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 0.9397966 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -12", OffsetMax = "50 12" }
                }, "Salvager_Buff_backpanel", "Salvager_Buff_button");
                count++;
                if (count >= 6)
                {
                    count = 0;
                    rows++;
                }
            }
            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = "closesalvagerenhancerbuffmenu" },
                Text = { Text = "X", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "150.01 147.566", OffsetMax = "186.01 183.566" }
            }, "Salvager_Market_buff_select", "close_menu_button");

            CuiHelper.DestroyUi(player, "Salvager_Market_buff_select");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("closesalvagerenhancerbuffmenu")]
        void CloseEnhancementBuffMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, "Salvager_Market_buff_select");

            Enhancer_Market_item_select(player);
        }

        [ConsoleCommand("confirmcostwithbuff")]
        void ConfirmEnhancementCost(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            CuiHelper.DestroyUi(player, "Salvager_Market_buff_select");
            var id = Convert.ToUInt32(arg.Args[0]);
            var item = player.inventory.AllItems()?.FirstOrDefault(x => x.uid.Value == id);
            if (item == null)
            {
                PrintToChat(player, lang.GetMessage("ItemMissing", this, player.UserIDString));
                Enhancer_Market_item_select(player);
                return;
            }
            var buff = parsedEnums.ContainsKey(arg.Args[1]) ? parsedEnums[arg.Args[1]] : Buff.None;
            if (buff == Buff.None)
            {
                PrintToChat(player, "Invalid buff.");
                Enhancer_Market_item_select(player);
                return;
            }
            // From here I need to send a menu that confirms the cost of the enhancement and specify that the tier will be random.

            Salvager_Market_confirmation(player, item, arg.Args[1]);

        }

        #endregion

        #region Enhancer Market buff confirm

        private void Salvager_Market_confirmation(BasePlayer player, Item item, string buff)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0.9490196" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-0.675 -0.664", OffsetMax = "0.325 1.007" }
            }, "Overlay", "Salvager_Market_confirmation");

            container.Add(new CuiElement
            {
                Name = "title",
                Parent = "Salvager_Market_confirmation",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIEnhancerTitle", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 24, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-149.651 147.732", OffsetMax = "150.349 183.732" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "title_instructions",
                Parent = "title",
                Components = {
                    new CuiTextComponent { Text = string.Format(lang.GetMessage("UIEnhanceConfirmation", this, player.UserIDString), item.info.displayName.english.TitleCase(), lang.GetMessage(buff, this, player.UserIDString)), Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-300 -51.4", OffsetMax = "300 -19.4" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = $"closesalvagerenhancerconfirmationmenu {item.uid.Value}" },
                Text = { Text = "X", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "150.348 147.732", OffsetMax = "186.348 183.732" }
            }, "Salvager_Market_confirmation", "close_menu_button");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1803922 0.1803922 0.1803922 0.6666667" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-149.65 31.9", OffsetMax = "-85.65 95.9" }
            }, "Salvager_Market_confirmation", "Selected_item");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.1803922 0.1803922 0.1803922 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30 -30", OffsetMax = "30 30" }
            }, "Selected_item", "selected_item_frontpanel");

            container.Add(new CuiElement
            {
                Name = "selected_item_img",
                Parent = "Selected_item",
                Components = {
                    new CuiImageComponent { Color = "1 1 1 1", ItemId = item.info.itemid, SkinId = item.skin },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30 -30", OffsetMax = "30 30" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "selected_item_buff",
                Parent = "Selected_item",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UILeftPanelBuff", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "41.286 12", OffsetMax = "85.481 32" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "selected_item_buff_entry",
                Parent = "Selected_item",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage(buff, this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "85.485 12", OffsetMax = "440.455 32.002" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "selected_item_name",
                Parent = "Selected_item",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIItem", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "41.286 -8", OffsetMax = "85.48 12" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "selected_item_name_entry",
                Parent = "Selected_item",
                Components = {
                    new CuiTextComponent { Text = item.info.displayName.english.TitleCase(), Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "85.484 -8", OffsetMax = "440.456 12" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "selected_item_modifier",
                Parent = "Selected_item",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UICost", this, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "41.286 -28", OffsetMax = "85.48 -8" }
                }
            });
            int cost = (config.scrapper_settings.enhancement_cost.TryGetValue((parsedEnums.ContainsKey(buff) ? parsedEnums[buff] : Buff.None), out cost) ? cost : 100);
            container.Add(new CuiElement
            {
                Name = "selected_item_modifier_entry",
                Parent = "Selected_item",
                Components = {
                    new CuiTextComponent { Text = $"{cost} {CurrencyName}", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "85.484 -28", OffsetMax = "440.456 -8" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "selected_enhancement_confirmation",
                Parent = "Selected_item",
                Components = {
                    new CuiTextComponent { Text = string.Format(lang.GetMessage("UIEnhancementConfirmationDescription", this, player.UserIDString), lang.GetMessage(buff, this, player.UserIDString)), Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-31.997 -68", OffsetMax = "318.075 -36" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "0.1803922 0.1803922 0.1803922 0.950", Command = $"enhancementsconfirm {item.uid.Value} {buff} {cost}" },
                Text = { Text = lang.GetMessage("UIEnhance", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-32 -102.5", OffsetMax = "32 -78.5" }
            }, "Selected_item", "selected_item_enhance_button");

            CuiHelper.DestroyUi(player, "Salvager_Market_confirmation");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("closesalvagerenhancerconfirmationmenu")]
        void BackToBuffSelection(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            CuiHelper.DestroyUi(player, "Salvager_Market_confirmation");

            var id = Convert.ToUInt32(arg.Args[0]);
            var item = player.inventory.AllItems()?.FirstOrDefault(x => x.uid.Value == id);
            if (item == null)
            {
                Enhancer_Market_item_select(player);
                return;
            }

            Salvager_Market_buff_select(player, item);
        }

        [ConsoleCommand("enhancementsconfirm")]
        void ProcessEnhancement(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            CuiHelper.DestroyUi(player, "Salvager_Market_confirmation");

            var id = Convert.ToUInt32(arg.Args[0]);
            var item = player.inventory.AllItems()?.FirstOrDefault(x => x.uid.Value == id);
            if (item == null)
            {
                PrintToChat(player, lang.GetMessage("ItemMissing", this, player.UserIDString));
                Enhancer_Market_item_select(player);
                return;
            }

            var cost = Convert.ToInt32(arg.Args[2]);

            var found = 0;
            List<Item> found_currency = Pool.GetList<Item>();
            if (permission.UserHasPermission(player.UserIDString, perm_enhance_free)) found = cost;
            else
            {
                foreach (var currency in player.inventory.AllItems())
                {
                    if (config.scrapper_settings.currency_skin > 0)
                    {
                        if (currency.skin != config.scrapper_settings.currency_skin) continue;
                        if (found + currency.amount > cost)
                        {
                            found_currency.Add(currency);
                            found = cost;
                        }
                        else if (found + currency.amount <= cost)
                        {
                            found_currency.Add(currency);
                            found += currency.amount;
                        }
                        if (found >= cost) break;
                    }
                    else if (!string.IsNullOrEmpty(config.scrapper_settings.currency_name))
                    {
                        if (currency.name == null || currency.name != config.scrapper_settings.currency_name) continue;
                        if (found + currency.amount > cost)
                        {
                            found_currency.Add(currency);
                            found = cost;
                        }
                        else if (found + currency.amount <= cost)
                        {
                            found_currency.Add(currency);
                            found += currency.amount;
                        }
                        if (found >= cost) break;
                    }
                    else
                    {
                        if (currency.info.shortname == config.scrapper_settings.currency_shortname)
                        {
                            if (found + currency.amount > cost)
                            {
                                found_currency.Add(currency);
                                found = cost;
                            }
                            else if (found + currency.amount <= cost)
                            {
                                found_currency.Add(currency);
                                found += currency.amount;
                            }
                            if (found >= cost) break;
                        }
                    }
                }
            }            
            if (found < cost)
            {
                PrintToChat(player, string.Format(lang.GetMessage("CantAffordEnhancement", this, player.UserIDString), CurrencyName));
                Pool.FreeList(ref found_currency);
                CuiHelper.DestroyUi(player, "SalvagerBackground");
                return;
            }

            if (!permission.UserHasPermission(player.UserIDString, perm_enhance_free))
            {
                found = 0;
                foreach (var currency in found_currency)
                {
                    if (found + currency.amount > cost)
                    {
                        currency.UseItem(cost - found);
                        found = cost;
                    }
                    else if (found + currency.amount <= cost)
                    {
                        found += currency.amount;
                        currency.Remove();
                    }
                    if (found >= cost) break;
                }
            }

            Pool.FreeList(ref found_currency);
            List<string> shortname = new List<string>();
            shortname.Add(item.info.shortname);
            GenerateItem(player, arg.Args[1], shortname, null, true, item);
            shortname.Clear();
            CuiHelper.DestroyUi(player, "SalvagerBackground");
            //item.Remove();
            // Handle generating new item.
        }

        [ChatCommand("givescrappercurrency")]
        void GiveScrapperCurrency(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, perm_admin)) return;
            var amount = args.Length > 0 ? Convert.ToInt32(args[0]) : 1;
            var item = ItemManager.CreateByName(config.scrapper_settings.currency_shortname, Math.Max(amount, 1), config.scrapper_settings.currency_skin);
            if (!string.IsNullOrEmpty(config.scrapper_settings.currency_name))
            {
                item.name = config.scrapper_settings.currency_name;
                item.MarkDirty();
            }
            player.GiveItem(item);
        }

        [ConsoleCommand("givescrappercurrency")]
        void GiveScrapperCurrencyConsole(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, perm_admin)) return;
            if (arg.Args == null || arg.Args.Length < 2)
            {
                arg.ReplyWith("Usage: givescrappercurrency <target ID/name> <amount>");
                return;
            }
            var target = FindPlayer(arg.Args[0], true, player ?? null);
            if (target == null)
            {
                return;
            }
            var amount = Math.Max(Convert.ToInt32(arg.Args[1]), 1);
            var item = ItemManager.CreateByName(config.scrapper_settings.currency_shortname, amount, config.scrapper_settings.currency_skin);
            if (!string.IsNullOrEmpty(config.scrapper_settings.currency_name)) item.name = config.scrapper_settings.currency_name;
            target.GiveItem(item);
            arg.ReplyWith($"Gave {target.displayName} {amount}x {config.scrapper_settings.currency_name ?? item.info.displayName.english}.");
        }

        //public void GenerateItem(BasePlayer player, string type = null, List<string> item_shortname = null, string tier = null, bool msg = false)

        #endregion

        #endregion

        #endregion

        #region API

        object STOnModifyBoatSpeed(BasePlayer player, MotorRowboat boat)
        {
            if (Tracked_Boat_Speeds.ContainsKey(boat.net.ID.Value)) return true;
            return null;
        }

        object STOnItemRepairWithMaxRepair(Item item)
        {
            if (item == null || string.IsNullOrEmpty(item.name)) return null;
            var buff = GetBuff(item.name);
            if (buff != Buff.None) return true;
            return null;
        }

        void STOnInstantMineTrigger(BasePlayer player, ResourceDispenser dispenser, Item item)
        {
            float value;
            switch (dispenser.gatherType)
            {
                case ResourceDispenser.GatherType.Ore:
                    value = GetTotalBuffValue(player, Buff.Miners);
                    break;
                case ResourceDispenser.GatherType.Tree:
                    value = GetTotalBuffValue(player, Buff.Lumberjacks);
                    break;
                case ResourceDispenser.GatherType.Flesh:
                    value = GetTotalBuffValue(player, Buff.Skinners);
                    break;

                default: 
                    value = 0;
                    break;
            }
            
            if (value == 0) return;            
            foreach (var r in dispenser.containedItems)
            {
                if (r.amount < 1) continue;
                //var amount_to_give = Convert.ToInt32(Math.Round((item != null && r.itemDef != null && item.info.shortname == r.itemDef.shortname ? r.amount - item.amount : r.amount) * value, 0, MidpointRounding.AwayFromZero));
                int amountToGive = 0;
                if (item != null && r.itemDef != null && r.itemDef.shortname == item.info.shortname) amountToGive = Convert.ToInt32(Math.Round((r.amount - item.amount) * value, 0, MidpointRounding.AwayFromZero));
                else amountToGive = Convert.ToInt32(Math.Round(r.amount * value, 0, MidpointRounding.AwayFromZero));
                //if (amountToGive >= 1) item.amount += amountToGive;
                if (amountToGive >= 1) player.GiveItem(ItemManager.CreateByName(r.itemDef.shortname, amountToGive));                
            }
        }

        object STCanModifyHorse(BasePlayer player, RidableHorse horse, float value)
        {
            var weapon = player.GetActiveItem();
            var weaponMod = weapon != null && weapon.name != null ? GetBuffValue(weapon.name) : 0f;
            if (player_attributes.ContainsKey(player.userID) && player_attributes[player.userID].buffs.ContainsKey(Buff.Jockeys))
            {
                var clothesMod = player_attributes[player.userID].buffs[Buff.Jockeys].totalModifier;
                var multiplier = player_attributes[player.userID].setBonusMultiplier.ContainsKey(Buff.Jockeys) ? player_attributes[player.userID].setBonusMultiplier[Buff.Jockeys] : 0f;                
                if (clothesMod + multiplier + weaponMod > value) return false;
                else
                {
                    ResetHorse(horse);
                    return null;
                }
                    
            }
            if (weaponMod > value) return false;
            ResetHorse(horse);
            return null;
        }

        Dictionary<ulong, string> Skinbox_Item_Tracking = new Dictionary<ulong, string>();

        object RecipeCanModifyHorse(RidableHorse horse)
        {
            if (Tracked_Horses.ContainsKey(horse.net.ID.Value)) return true;
            else return null;
        }

        string SB_CanReskinItem(BasePlayer player, Item item, ulong newSkinID)
        {
            if (Skinbox_Item_Tracking.ContainsKey(player.userID))
            {
                NextFrame(() =>
                {
                    item.name = Skinbox_Item_Tracking[player.userID];
                    player.SendNetworkUpdateImmediate();
                    Skinbox_Item_Tracking.Remove(player.userID);
                });                
            }
            
            return null;
        }

        string SB_CanAcceptItem(BasePlayer player, Item item)
        {
            if (item.name != null && GetBuff(item.name) != Buff.None)
            {
                if (config.general_settings.prevent_reskin) return "You cannot reskin an enhanced item.";
                if (!Skinbox_Item_Tracking.ContainsKey(player.userID)) Skinbox_Item_Tracking.Add(player.userID, item.name);
                else Skinbox_Item_Tracking[player.userID] = item.name;
            } 
            
            //if (item.name != null && GetBuff(item.name) != Buff.None)
            //{
            //    return "You cannot reskin this item or it will lose it's buff!";
            //}
            return null;
        }

        

        #endregion

        #region Mission Rewards

        void OnMissionSucceeded(BaseMission mission, BaseMission.MissionInstance missionInstance, BasePlayer player)
        {
            foreach (var info in config.mission_rewards.mission_rewards)
            {
                if (info.Key.Equals(missionInstance.ProviderEntity()?.ShortPrefabName, StringComparison.OrdinalIgnoreCase))
                {
                    var roll = UnityEngine.Random.Range(0f, 100f);
                    if (roll >= 100f - info.Value.reward_chance)
                    {
                        var totalWeight = 0;

                        foreach (var type in info.Value.rewards) 
                            totalWeight += type.Value;

                        var typeRoll = UnityEngine.Random.Range(0, totalWeight);
                        totalWeight = 0;
                        foreach (var type in info.Value.rewards)
                        {
                            totalWeight += type.Value;
                            if (typeRoll <= totalWeight)
                            {
                                var item = GenerateRandomItem(type.Key);
                                if (item != null)
                                {
                                    player.GiveItem(item);
                                    if (MessagesOn(player)) PrintToChat(player, string.Format(lang.GetMessage("MissionItemReceived", this, player.UserIDString), item.name));
                                    break;
                                }
                            }
                        }
                    }
                    break;
                }
            }
        }

        #endregion

        #region RandomTrader integration

        public class RTInfo
        {
            public string fileName;
            public string shopName;
            public int purchase_limit;
            public int quantity_picked;
            public List<RTItemInfo> items = new List<RTItemInfo>();

            public class RTItemInfo
            {
                public string shortname;
                public ulong skin;
                public int min_quantity;
                public int max_quantity;
                public int min_cost;
                public int max_cost;
                public string name;
                public string store_display_name;
                public KeyValuePair<string, string> url;
                public RTItemInfo(string shortname, ulong skin, int min_quantity, int max_quantity, int min_cost, int max_cost, string name, string store_display_name, KeyValuePair<string, string> url)
                {
                    this.shortname = shortname;
                    this.skin = skin;
                    this.min_quantity = min_quantity;
                    this.max_quantity = max_quantity;
                    this.min_cost = min_cost;
                    this.max_cost = max_cost;
                    this.name = name;
                    this.store_display_name = store_display_name;
                    this.url = url;
                }
            }
        }


        RTInfo.RTItemInfo GetRandomItem(Buff buff)
        {
            EnhancementInfo ei;
            if (config.enhancements.TryGetValue(buff, out ei))
            {
                float modifier = 0f;
                string shortname = null;
                ulong skin;
                var totalWeight = ei.tierInfo.Sum(x => x.Value.chance_weight);
                var randomNumber = UnityEngine.Random.Range(0, totalWeight + 1);
                var count = 0;
                string tier = null;
                foreach (var t in ei.tierInfo)
                {
                    count += t.Value.chance_weight;
                    if (randomNumber <= count)
                    {
                        tier = t.Key;
                        modifier = (float)Math.Round(UnityEngine.Random.Range(t.Value.min_value, t.Value.max_value), 2);
                        break;
                    }
                }
                // We see if a list of items has been given first
                if (ei.item_whitelist != null && ei.item_whitelist.Count > 0) shortname = ei.item_whitelist.GetRandom();
                else
                {
                    // otherwise we pick a random from our itemdefs.
                    List<string> items = Pool.GetList<string>();
                    if (ei.item_blacklist != null) items.AddRange(EnhanceableItems.Where(x => !ei.item_blacklist.Contains(x)));
                    else items.AddRange(EnhanceableItems);
                    shortname = items.GetRandom();
                    Pool.FreeList(ref items);
                }
                if (config.item_skins.ContainsKey(shortname)) skin = config.item_skins[shortname].GetRandom();
                else skin = 0;
                var uiCol = config.tier_information.tier_colours.ContainsKey(tier) ? config.tier_information.tier_colours[tier] : "#FFFFFF";
                var itemDef = ItemManager.FindItemDefinition(shortname);
                return new RTInfo.RTItemInfo(shortname, skin, config.random_trader_api.min_stock_quantity, config.random_trader_api.max_stock_quantity, config.random_trader_api.min_cost, config.random_trader_api.max_cost, $"{buff} {itemDef?.displayName.english ?? shortname} [{tier} {modifier}]", $"{buff} {itemDef?.displayName.english ?? shortname} <color={uiCol}>[{config.tier_information.tier_display_names[tier]}]</color>", new KeyValuePair<string, string>());
            }
            return null;
        }

        void RandomTraderReady(List<string> current_stores)
        {
            if (current_stores == null || !current_stores.Contains(this.Name)) AddEpicLoot();
        }

        void AddEpicLoot()
        {
            Puts("Attempting to add a new RandomTrader list for EpicLoot.");
            object[] generlSettingsOjb = new object[] { config.random_trader_api.copypaste_file_name, config.random_trader_api.shop_name, config.random_trader_api.shop_purchase_limit, config.random_trader_api.shop_display_amount };

            List<RTInfo.RTItemInfo> items = Pool.GetList<RTInfo.RTItemInfo>();
            foreach (var entry in config.enhancements)
            {
                for (int i = 0; i < config.random_trader_api.quantity_of_enhancements; i++)
                {
                    items.Add(GetRandomItem(entry.Key));
                }
            }
            List<object[]> item_objects = Pool.GetList<object[]>();
            foreach (var item in items)
            {
                item_objects.Add(new object[] { item.shortname, item.skin, item.min_quantity, item.max_quantity, item.min_cost, item.max_cost, item.name, item.store_display_name, item.url });
            }
            Pool.FreeList(ref items);

            RandomTrader.Call("RTAddStore", this.Name, generlSettingsOjb, item_objects);
        }

        #endregion

        #region FishingContest Integration

        void FishingContestLoaded()
        {
            if (!config.fishing_contest_api.send_payload) return;
            List<object[]> hookObjects = Pool.GetList<object[]>();
            Puts("Preparing items to send to FishingContest.");
            foreach (var shortname in config.fishing_contest_api.shortnames)
            {
                var itemDef = ItemManager.FindItemDefinition(shortname);
                foreach (var buff in config.fishing_contest_api.buffs)
                {
                    string displayName;
                    ulong skin = 0;
                    int itemWeight = 20;
                    string tier;
                    float modifier;

                    EnhancementInfo ei;
                    if (config.enhancements.TryGetValue(buff, out ei))
                    {
                        foreach (var t in ei.tierInfo)
                        {
                            tier = t.Key;
                            modifier = (float)Math.Round(UnityEngine.Random.Range(t.Value.min_value, t.Value.max_value), 2);

                            if (config.item_skins.ContainsKey(shortname)) skin = config.item_skins[shortname].GetRandom();


                            displayName = $"{buff} {itemDef.displayName.english ?? itemDef.shortname} [{tier} {modifier}]";

                            hookObjects.Add(new object[] { shortname, (int)1, (int)1, skin, displayName, itemWeight });
                        }
                    }                  
                }              
            }
            Puts($"We have {hookObjects.Count} items to send over to FishingContest.");
            FishingContest.Call("FCAddRewards", hookObjects);
            config.fishing_contest_api.send_payload = false;
            SaveConfig();
            Pool.FreeList(ref hookObjects);
        }

        #endregion

        #region RaidableBases Integration        

        public class RaidableBaseSettings
        {
            [JsonProperty("Allow EpicLoot to add items to RaidableBases?")]
            public bool enabled = true;

            [JsonProperty("Difficulty settings")]
            public Dictionary<int, RaidableBaseLootSettings> base_settings = new Dictionary<int, RaidableBaseLootSettings>();

            [JsonProperty("Containers that you want EpicLoot items to spawn into")]
            public List<string> container_whitelist = new List<string>();
        }

        public class RaidableBaseLootSettings
        {
            public int min_drops;
            public int max_drops;
            

            public RaidableBaseLootSettings(int min_drops, int max_drops)
            {
                this.min_drops = min_drops;
                this.max_drops = max_drops;
            }
        }

        private void OnRaidableBaseStarted(Vector3 location, int mode, bool allowPVP, string id, float spawnTime, float despawnTime, float loadTime, ulong ownerId, BasePlayer owner, List<BasePlayer> players, List<BasePlayer> intruders, List<BaseEntity> entities)
        {
            RaidableBaseLootSettings lootSettings;
            if (config.raidablebases_settings.base_settings.TryGetValue(mode, out lootSettings))
            {
                NextTick(() =>
                {
                    List<StorageContainer> containers = Pool.GetList<StorageContainer>();
                    foreach (var entity in entities)
                    {
                        if (entity is StorageContainer && config.raidablebases_settings.container_whitelist.Contains(entity.ShortPrefabName))
                        {
                            var container = entity as StorageContainer;
                            if (!container.inventory.IsFull()) containers.Add(container);

                        }
                    }

                    if (containers.Count > 0)
                    {
                        var randomAmount = UnityEngine.Random.Range(lootSettings.min_drops, lootSettings.max_drops + 1);
                        if (randomAmount > 0)
                        {
                            for (int i = 0; i < randomAmount; i++)
                            {
                                var randomContainer = containers.GetRandom();
                                randomContainer.inventory.capacity++;
                                randomContainer.inventorySlots++;

                                var item = GenerateRandomItem();
                                if (item != null)
                                {
                                    if (!item.MoveToContainer(randomContainer.inventory)) item.Remove();
                                    else Puts($"Added {item.name} to RaidableBase[{id}] - Container: {randomContainer.ShortPrefabName}[{randomContainer.net.ID.Value}].");
                                }
                            }
                        }
                    }
                    Pool.FreeList(ref containers);
                });                
            }
        }

        #endregion

        #region SkillTree integration

        void AddNodeToSkillTree()
        {
            if (config.skilltree_settings.enabled && SkillTree != null && SkillTree.IsLoaded)
            {
                Dictionary<int, Dictionary<string, string>> _perms = new Dictionary<int, Dictionary<string, string>>();


                for (int i = 1; i < config.skilltree_settings.max_level + 1; i++)
                {
                    _perms.Add(i, new Dictionary<string, string>() { [$"epicloot.skilltree.{i}"] = $"Epic Loot Chance + {(config.skilltree_settings.increase_per_level * i) * 100}%" });
                }

                string Description = $"Increases your chance of finding <color=#d800ff>Epic Loot</color> in crates and barrels by <color=#00ff00>{config.skilltree_settings.increase_per_level * 100}%</color> per level.";

                object[] perms = new object[]
                {
                    Description,
                    _perms
                };

                string Tree = "Scavenging";
                string Node = "Epic Loot Chance";
                bool StartOn = true;
                int Max_Level = 5;
                int Tier = 3;
                float Value_per_Level = config.skilltree_settings.increase_per_level;
                string Buff = "Permission";
                string BuffType = "Permission";
                string URL = "https://www.dropbox.com/s/7oecz6cy1oy0nia/Epic_Loot_Chance.png?dl=1";
                ulong skin = 2874788669;

                SkillTree.Call("AddNode", Tree, Node, StartOn, Max_Level, Tier, Value_per_Level, Buff, BuffType, URL, perms, skin, true);
            }
        }

        #endregion

        #region BossMonster Integration

        public class BossMonsterSettings
        {
            [JsonProperty("Create an epic loot cache where the boss dies?")]
            public bool enabled = true;

            [JsonProperty("Minimum quantity of items generated on a boss kill")]
            public int min_drops = 1;

            [JsonProperty("Maximum quantity of items generated on a boss kill")]
            public int max_drops = 3;
        }

        void OnBossKilled(ScientistNPC boss, BasePlayer attacker)
        {
            var container = PopulateLoot(boss);

            if (container != null && container.inventory.itemList != null && container.inventory.itemList.Count > 0)
            {
                var pos = new Vector3(boss.transform.position.x, boss.transform.position.y + 0.5f, boss.transform.position.z);
                var rot = boss.transform.rotation;
                timer.Once(0.1f, () =>
                {
                    container.inventory.Drop("assets/prefabs/misc/item drop/item_drop.prefab", pos, rot, 0);
                    container.KillMessage();
                });
            }
        }

        StorageContainer PopulateLoot(BaseEntity entity)
        {
            var pos = new Vector3(entity.transform.position.x, entity.transform.position.y - 1000, entity.transform.position.z);
            var storage = GameManager.server.CreateEntity("assets/prefabs/deployable/large wood storage/box.wooden.large.prefab", pos) as StorageContainer;
            storage.Spawn();

            UnityEngine.Object.DestroyImmediate(storage.GetComponent<GroundWatch>());
            UnityEngine.Object.DestroyImmediate(storage.GetComponent<DestroyOnGroundMissing>());            

            var rolls = Math.Max(config.bossmonster_settings.min_drops != config.bossmonster_settings.max_drops ? UnityEngine.Random.Range(config.bossmonster_settings.min_drops, config.bossmonster_settings.max_drops + 1) : config.bossmonster_settings.max_drops, 1);

            storage.inventory.capacity = rolls + 1;
            storage.inventorySlots = rolls + 1;

            for (int i = 0; i < rolls; i++)
            {
                var item = GenerateRandomItem();
                if (item != null) item.MoveToContainer(storage.inventory);
            }

            return storage;
        }

        #endregion

        #region Harmony patch

#if CARBON
        private Harmony _harmony;
#else
        private HarmonyInstance _harmony;
#endif

        private void Loaded()
        {
            // Name can be anything.
#if CARBON
            _harmony = new Harmony(Name + "Patch");
            
            _harmony.PatchAll();
#else
            _harmony = HarmonyInstance.Create(Name + "Patch");
            System.Type[] patchType =
            {
                AccessTools.Inner(typeof(EpicLoot), "Item_MaxStackable_Patch"),
                AccessTools.Inner(typeof(EpicLoot), "Item_SplitItem_Patch"),
                AccessTools.Inner(typeof(EpicLoot), "Item_CanStack_Patch")
            };
            if (config.general_settings.prevent_max_condition_loss) patchType.Append(AccessTools.Inner(typeof(EpicLoot), "Item_DoRepair_Patch"));
            foreach (var t in patchType) 
            {
                new PatchProcessor(_harmony, t, HarmonyMethod.Merge(t.GetHarmonyMethods())).Patch();
            }
#endif
        }

        [HarmonyPatch(typeof(Item), "MaxStackable")]
        internal class Item_MaxStackable_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(Item __instance, ref int __result)
            {
                if (__instance == null) return true;

                if (__instance.name != null && StaticBuffs.Contains(__instance.name.Split(' ')[0]))
                {
                    __result = 1;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Item), "SplitItem", typeof(int))]
        internal class Item_SplitItem_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(Item __instance, int split_Amount, ref Item __result)
            {
                if (__instance == null || !IsEpicScrap(__instance)) return true;

                Assert.IsTrue(split_Amount > 0, "split_Amount <= 0");
                if (split_Amount <= 0)
                {
                    return true;
                }
                if (split_Amount >= __instance.amount)
                {
                    return true;
                }
                __instance.amount -= split_Amount;
                var splitItem = ItemManager.CreateByItemID(__instance.info.itemid, 1, 0ul);
                splitItem.amount = split_Amount;
                splitItem.skin = __instance.skin;
                splitItem.name = __instance.name;
                splitItem.MarkDirty();
                __result = splitItem;

                return false;
            }
        }

        [HarmonyPatch(typeof(Item), "CanStack", typeof(Item))]
        internal class Item_CanStack_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(Item __instance, Item item, ref bool __result)
            {
                if (__instance == null || item == null || (!IsEpicScrap(__instance) && !IsEpicScrap(item))) return true;
                if (__instance.name != item.name || __instance.skin != item.skin || __instance.info.shortname != item.info.shortname) return false;
                return true;
            }
        }

        [HarmonyPatch(typeof(Item), "DoRepair", typeof(float))]
        internal class Item_DoRepair_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Item __instance, float maxLossFraction)
            {
                if (__instance == null || string.IsNullOrEmpty(__instance.name) || !__instance.hasCondition || !StaticBuffs.Contains(__instance.name.Split(' ')[0])) return;

                __instance.maxCondition = __instance.info.Blueprint.targetItem.condition.max;
                __instance.condition = __instance.info.Blueprint.targetItem.condition.max;
            }
        }

        #endregion
    }
}
