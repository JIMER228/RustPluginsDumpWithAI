// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("Professions", "imthenewguy", "1.0.3")]
    [Description("This plugin was fixed by Инкуб to order [Rust Plugin Sliv]: https://discord.gg/pFgKw6Dyyq")]
	class Professions : RustPlugin
	{
        #region Config       

        private Configuration config;
        public class Configuration
        {
            [JsonProperty("Prime meat default chance [%]")]
            public float prime_meat_default = 1f;

            [JsonProperty("Prime meat professional chance [%]")]
            public float prime_meat_professional = 4f;

            [JsonProperty("Prime meat chance increase per level [%]")]
            public float prime_meat_chance_per_level = 0.1f;

            [JsonProperty("Skinning experience per hit")]
            public float skinner_xp_per_hit = 8f;

            [JsonProperty("Skinning experience animal modifier", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, float> skinner_modifier = new Dictionary<string, float>();

            [JsonProperty("Skinner tools", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> skinner_tools;

            [JsonProperty("Enable animal tracking for skinners using the /track command")]
            public bool tracking_enabled = true;

            [JsonProperty("Time delay between tracking attempts")]
            public float tracking_delay = 5;

            [JsonProperty("Gold nugget default chance [%]")]
            public float gold_nugget_default = 1f;

            [JsonProperty("Gold nugget professional chance [%]")]
            public float gold_nugget_professional = 4f;

            [JsonProperty("Gold nugget chance increase per level [%]")]
            public float gold_nugget_chance_per_level = 0.1f;

            [JsonProperty("Mining experience per hit")]
            public float miner_xp_per_hit = 7f;

            [JsonProperty("Miner tools", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> miner_tools;

            [JsonProperty("Jackhammer xp modifier [1.0 = full xp per hit]")]
            public double jackhammer_xp_modifier = 0.4;

            [JsonProperty("Allow jackhammer to obtain gold nuggets?")]
            public bool allow_jackhammer_drops = false;

            [JsonProperty("Pinecone default chance [%]")]
            public float pinecone_default = 1f;

            [JsonProperty("Pinecone professional chance [%]")]
            public float pinecone_professional = 4f;

            [JsonProperty("Pinecone chance increase per level [%]")]
            public float pinecone_chance_per_level = 0.1f;

            [JsonProperty("Logging experience per hit")]
            public float logger_xp_per_hit = 5f;

            [JsonProperty("Logger tools", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> logger_tools;

            [JsonProperty("Chainsaw xp modifier [1.0 = full xp per hit]")]
            public double chainsaw_xp_modifier = 0.05;

            [JsonProperty("Allow chainsaws to obtain pinecones?")]
            public bool allow_chainsaw_drops = false;

            [JsonProperty("Experience increase per level for gathering skills")]
            public double xp_increase = 100f;
            public double xp_additive = 20f;
            public int max_level = 50;

            [JsonProperty("Additional resources per level [%]")]
            public double gather_yield_per_level = 2;

            [JsonProperty("Item information", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, Iteminfo> item_info = new Dictionary<string, Iteminfo>();

            [JsonProperty("Experience required for crafting level 1")]
            public double crafting_level_1 = 100;

            [JsonProperty("Experience required for crafting level 2")]
            public double crafting_level_2 = 1100;

            [JsonProperty("Experience required for crafting level 3")]
            public double crafting_level_3 = 4100;

            [JsonProperty("Cost to change class?")]
            public float ClassChangeCost = 0;

            [JsonProperty("Job information", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, JobInfo> job_info = new Dictionary<string, JobInfo>();

            [JsonProperty("Job info font size")]
            public int JobInfoFontSize = 12;

            [JsonProperty("Add a warning at the bottom of the job info page telling players that their xp will be reset if they change roles?")]
            public bool job_change_warning_text = true;

            [JsonProperty("Component xp", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, float> component = new Dictionary<string, float>();

            [JsonProperty("Research xp multiplier")]
            public double research_xp_multiplier = 3;          

            [JsonProperty("Crafting level is required to craft items (setting to false will allow crafters of any level to craft all tiers of items)")]
            public bool crafting_level_required = true;

            [JsonProperty("Allow the use of resource bags")]
            public bool allow_resource_bags = true;

            [JsonProperty("Allow users to access the resource bag via chat command [requires professions.chat.bag permissions]")]
            public bool bag_chat_command_allow = true;

            [JsonProperty("Chat command to open the resource bag")]
            public string bag_chat_cmd = "rbag";

            [JsonProperty("Max stack size when the item is placed in the bag from gathering?")]
            public int max_stack_size_in_bag = 10;

            [JsonProperty("What method should we use to handle crafting - [0 = Prevent craft, 1 = Tax economics, 2 = Tax scrap")]
            public CraftMethod craft_method = CraftMethod.TaxEcon;

            [JsonProperty("Currency to use for market and quitting [economics, scrap]")]
            public string currency = "economics";

            [JsonProperty("Chat command to open the market [requires professions.chat.market permissions]")]
            public string market_chat_cmd = "pmarket";

            [JsonProperty("Name of the human NPC that will open the store.")]
            public string market_npc_name = "resource market";

            [JsonProperty("IDs of NPCs that will open the market", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<ulong> market_npc_ids = new List<ulong>();

            [JsonProperty("Chat command to open the job menu [requires professions.chat.jobmenu permissions]")]
            public string jobmenu_chat_cmd = "jobmenu";            

            [JsonProperty("Name of the human NPC that will open the job menu.")]
            public string job_npc_name = "centrelink";

            [JsonProperty("Send a message showing what % of players are the selected profession when using the menu")]
            public bool send_profession_msg = true;

            [JsonProperty("IDs of NPCs that will open the job menu", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<ulong> job_npc_ids = new List<ulong>();

            [JsonProperty("Tier 1 default items tax")]
            public int default_tax_items_tier_1 = 50;

            [JsonProperty("Tier 2 default items tax")]
            public int default_tax_items_tier_2 = 125;

            [JsonProperty("Tier 3 default items tax")]
            public int default_tax_items_tier_3 = 500;

            [JsonProperty("Delete the NPC vending machines that sell car parts on wipe?")]
            public bool delete_carpart_vendors = false;

            [JsonProperty("Disable crafting professions?")]
            public bool disable_crafting_professions = false;

            [JsonProperty("Better chat titles", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, TitleInfo> titles = new Dictionary<string, TitleInfo>()
            {
                {"logger", new TitleInfo()
                    { title = "Logger", colour = "#6D3D23" }
                },
                {"miner", new TitleInfo()
                    { title = "Miner", colour = "#556F80" }
                },
                {"skinner", new TitleInfo()
                    { title = "Skinner", colour = "#A92727" }
                },
                {"weaponsmith", new TitleInfo()
                    { title = "Weaponsmith", colour = "#1A2C41" }
                },
                {"tailor", new TitleInfo()
                    { title = "Tailor", colour = "#B1D94E" }
                },
                {"electrician", new TitleInfo()
                    { title = "Electrician", colour = "#F3C920" }
                },
                {"mechanic", new TitleInfo()
                    { title = "Mechanic", colour = "#5979DE" }
                },

            };


            public class JobInfo
            {
                public string description;
                public string img;
            }

            public class Iteminfo
            {
                public string shortname;
                public ulong skin;
                public string displayName;
                public int quantity = 1;
                public int sell_price = 10;
            }

            public class TitleInfo
            {
                public string title;
                public string colour;
            }

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }
        PCDInfo playerData;

        public enum CraftMethod
        {
            PreventCraft,
            TaxEcon,
            TaxScrap
        }

        private void LevelCrafting(BasePlayer player, double xp)
        {
            if (config.disable_crafting_professions) return;
            if (!pcdData.pEntity.TryGetValue(player.userID, out playerData))
            {
                SetupPlayer(player);
                playerData = pcdData.pEntity[player.userID];
            }
            if (playerData.level == 3) return;
            playerData.xp += xp;
            if (playerData.xp >= config.crafting_level_1)
            {
                if (playerData.xp >= config.crafting_level_2)
                {
                    if (playerData.xp >= config.crafting_level_3)
                    {
                        if (playerData.level == 2) PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("CanMakeT", this, player.UserIDString), 3)}");
                        playerData.level = 3;
                        SaveData();
                        return;
                    }
                    if (playerData.level == 1) PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("CanMakeT", this, player.UserIDString), 2)}");
                    playerData.level = 2;
                    SaveData();
                    return;
                }
                if (playerData.level == 0) PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("CanMakeT", this, player.UserIDString), 1)}");
                playerData.level = 1;
                SaveData();
                return;
            }
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
            foreach (var animal in new string[] {"chicken", "bear", "boar", "horse", "stag", "wolf", "shark"})
            {
                config.skinner_modifier.Add(animal, 1.0f);
            }
            config.skinner_tools = new List<string>(){ "knife.bone", "knife.butcher", "knife.combat", "rock", "axe.salvaged", "stonehatchet", "hatchet", "hammer.salvaged" };
            config.logger_tools = new List<string>() { "rock", "axe.salvaged", "stonehatchet", "hatchet", "hammer.salvaged" };
            config.miner_tools = new List<string>() { "rock", "hammer.salvaged", "pickaxe", "stone.pickaxe", "icepick.salvaged"};
            config.item_info = new Dictionary<string, Configuration.Iteminfo>();
            config.item_info.Add("skinner", new Configuration.Iteminfo()
            {
                shortname = "bearmeat.cooked",
                skin = 2403118688,
                displayName = "Prime Meat"
            });
            config.item_info.Add("miner", new Configuration.Iteminfo()
            {
                shortname = "battery.small",
                skin = 2405961582,
                displayName = "Gold Nugget"
            });
            config.item_info.Add("logger", new Configuration.Iteminfo()
            {
                shortname = "battery.small",
                skin = 2405963120,
                displayName = "Pinecone"
            });
            foreach (var profession in professions)
            {
                config.job_info.Add(profession, new Configuration.JobInfo()
                {
                    description = GetDefaultDescription(profession),
                    img = GetDefaultImage(profession)
                });
            }
            config.component.Add("targeting.computer", 20f);
            config.component.Add("cctv.camera", 15f);
            config.component.Add("propanetank", 0.5f);
            config.component.Add("metalblade", 0.4f);
            config.component.Add("metalpipe", 1f);
            config.component.Add("metalspring", 4f);
            config.component.Add("riflebody", 10f);
            config.component.Add("roadsigns", 1f);
            config.component.Add("rope", 0.35f);
            config.component.Add("gears", 2f);
            config.component.Add("semibody", 5f);
            config.component.Add("sewingkit", 1.5f);
            config.component.Add("sheetmetal", 4f);
            config.component.Add("smgbody", 5f);
            config.component.Add("tarp", 1.5f);
            config.component.Add("stones", 0.001f);
            config.component.Add("lowgradefuel", 0.005f);
            config.component.Add("cloth", 0.001f);
            config.component.Add("leather", 0.01f);
            config.component.Add("metal.refined", 0.2f);
            config.component.Add("metal.fragments", 0.005f);
            config.component.Add("techparts", 5f);

        }

        string GetDefaultDescription(string job)
        {
            switch(job)
            {
                case "miner": return "Miners make money mining resources.\n\nThey have a higher chance to obtain Gold Nuggets when mining nodes, which can be sold to the Resouce Market.\n\nLevel up your mining to increase the amount of resources you gather, as well as your chance to obtain Gold Nuggets.";
                case "logger": return "Loggers make money chopping trees.\n\nThey have a higher chance to obtain Pinecones when woodcutting, which can be sold to the Resouce Market.\n\nLevel up your woodcutting to increase the amount of wood you gather, as well as your chance to obtain Pinecones.";
                case "skinner": return "Skinners make money skinning animals.\nThey have a higher chance to obtain Prime Meat when skinning, which can be sold to the Resouce Market.\nSkinners can also track animals down using the /track command.\nLevel up your skinning to increase the amount of resources you gather, as well as your chance to obtain Prime Meat.";
                case "electrician": return "Electricians specialize in the production of electrical equipment, from branches and switches, to solar panels and batteries.\nThey can exclusively make, or receive tax breaks when producing the wares that they specialize in.";
                case "weaponsmith": return "Weaponsmiths specialize in creating weapons from knives and swords, to firearms and explosives.\nThey can exclusively make, or receive tax breaks when producing the wares that they specialize in.";
                case "tailor": return "Tailors specialize in the creation of clothing and armour.\nThey can exclusively make, or receive tax breaks when producing the wares that they specialize in.";
                case "mechanic": return "Mechanics specialize in the creation of vehicle modules and engine parts.\nThey can exclusively make, or receive tax breaks when producing the wares that they specialize in.";
            }
            return "Add description here.";
        }

        string GetDefaultImage(string job)
        {
            switch (job)
            {
                case "miner": return "https://i.imgur.com/lm71k74.png";
                case "logger": return "https://imgur.com/e27vfnW.png";
                case "skinner": return "https://imgur.com/q0slnz9.png";
                case "electrician": return "https://imgur.com/oHxhaMO.png";
                case "weaponsmith": return "https://imgur.com/DpDCQMR.png";
                case "tailor": return "https://imgur.com/UeLlUhh.png";
                case "mechanic": return "https://i.imgur.com/6rQI7x6.png";
            }
            return "";
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();

                if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
                {
                    PrintToConsole("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            PrintToConsole($"Configuration changes saved to {Name}.json");
            Config.WriteObject(config, true);
        }

        #endregion

        #region Data

        const string default_job = "unemployed";
        public string[] professions = { "miner", "logger", "skinner", "electrician", "weaponsmith", "tailor", "mechanic" };        

        PlayerEntity pcdData;
        Crafting crafting_info;
        private DynamicConfigFile PCDDATA;
        private DynamicConfigFile CRAFTING;

        void Init()
        {
            PCDDATA = Interface.Oxide.DataFileSystem.GetFile("Professions/players");
            CRAFTING = Interface.Oxide.DataFileSystem.GetFile("Professions/crafting_info");
            LoadData();
            foreach (var profession in professions)
            {
                if (!permission.GroupExists(profession)) permission.CreateGroup(profession, "", 0);
            }
            Levels.Add(0, 0);
            for (int i = 1; i < config.max_level + 1; i++)
            {
                Levels.Add(i, (config.xp_increase * i) + (config.xp_additive * i));
            }
            permission.RegisterPermission("professions.admin", this);
            permission.RegisterPermission("professions.chat.bag", this);
            permission.RegisterPermission("professions.chat.market", this);
            permission.RegisterPermission("professions.chat.jobmenu", this);
        }

        void Unload()
        {
            SaveData();
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "Professions_Menu");
                CuiHelper.DestroyUi(player, "Rust Quit UI");
            }
            foreach (var container in containers)
            {
                if (container.inventory.itemList.Count > 0)
                {
                    if (container.OwnerID == 0)
                    {
                        Puts("Error - OwnerID is 0");
                        continue;
                    }
                    if (!pcdData.resource_bag.TryGetValue(container.OwnerID, out bagData))
                    {
                        pcdData.resource_bag.Add(container.OwnerID, new ResourceBag());
                        bagData = pcdData.resource_bag[container.OwnerID];
                    }
                    bagData._storage.Clear();
                    foreach (var item in container.inventory.itemList)
                    {
                        var displayName = item.info.displayName.english;
                        if (item.name != null) displayName = item.name;
                        bagData._storage.Add(new BagInfo()
                        {
                            shortname = item.info.shortname,
                            displayName = displayName,
                            amount = item.amount,
                            slot = item.position,
                            skin = item.skin
                        });
                    }
                }
                container.Kill();
            }
        }

        public enum SaveType
        {
            PCDDATA,
            CRAFTING
        }

        void SaveData(SaveType type = SaveType.PCDDATA)
        {
            if (type == SaveType.PCDDATA) PCDDATA.WriteObject(pcdData);
            else if (type == SaveType.CRAFTING) CRAFTING.WriteObject(crafting_info);
        }     

        void LoadData()
        {
            try
            {
                pcdData = Interface.Oxide.DataFileSystem.ReadObject<PlayerEntity>("Professions/players");
            }
            catch
            {
                Puts("Couldn't load player data, creating new Playerfile");
                pcdData = new PlayerEntity();
            }

            try
            {
                Puts("Loaded Crafting Info");
                crafting_info = Interface.Oxide.DataFileSystem.ReadObject<Crafting>("Professions/crafting_info");
            }
            catch
            {
                Puts("Couldn't load crafting data, creating new Playerfile");
                crafting_info = new Crafting();
            }
        }

        class PlayerEntity
        {
            public Dictionary<ulong, PCDInfo> pEntity = new Dictionary<ulong, PCDInfo>();
            public Dictionary<ulong, ResourceBag> resource_bag = new Dictionary<ulong, ResourceBag>();
        }

        class Crafting
        {
            public Dictionary<string, CraftingInfo> blueprints = new Dictionary<string, CraftingInfo>();
        }

        class PCDInfo
        {
            public string name;
            public string profession = default_job;
            public string job_type;
            public int level;
            public double xp;
            public double bonus_chance;
            public int gold_nuggets_acquired;
            public int prime_meat_acquired;
            public int pinecones_acquired;
        }

        class CraftingInfo 
        {
            public List<string> classes = new List<string>();
            public double xp;
            public double xp_multiplier = 1f;
            public double research_xp = 0;
            public int tax;
        }

        List<ulong> skins = new List<ulong>();

        Dictionary<string, double> weaponsmith = new Dictionary<string, double>();
        Dictionary<string, double> electrician = new Dictionary<string, double>();
        Dictionary<string, double> tailor = new Dictionary<string, double>();
        Dictionary<string, double> mechanic = new Dictionary<string, double>();
        string quitText;
        void OnServerInitialized(bool initial)
        {
            if (ImageLibrary == null)
            {
                Puts("ImageLibrary is required to run this plugin.");
                Unsubscribe("Unload");
                Interface.Oxide.UnloadPlugin(Name);                
                return;
            }
            if (config.craft_method == CraftMethod.PreventCraft)
            {
                Unsubscribe("OnItemCraftCancelled");
            }
            if (config.disable_crafting_professions)
            {
                Unsubscribe("CanCraft");
                Unsubscribe("OnItemCraftFinished");
                Unsubscribe("OnItemCraftCancelled");
            }
            if (config.currency.ToLower() == "economics") quitText = string.Format(lang.GetMessage("QuitJob1", this), config.ClassChangeCost);
            else if (config.currency.ToLower() == "scrap") quitText = string.Format(lang.GetMessage("QuitJob2", this), config.ClassChangeCost);
            cmd.AddChatCommand(config.bag_chat_cmd, this, "ResourceBagCommand");
            cmd.AddChatCommand(config.market_chat_cmd, this, "OpenMarket");
            cmd.AddChatCommand(config.jobmenu_chat_cmd, this, "JobMenuCMD");
            if (crafting_info.blueprints.Count == 0)
            {
                foreach (var bp in ItemManager.GetBlueprints())
                {
                    if (bp.isResearchable || bp.defaultBlueprint)
                    {
                        var item = bp.GetComponent<ItemDefinition>();
                        if (item == null) continue;
                        var exp = CalculateXP(bp);
                        crafting_info.blueprints.Add(item.shortname, new CraftingInfo()
                        {
                            classes = { GetItemCategory(item) },
                            xp = exp
                        });
                    }
                }
                SaveData(SaveType.CRAFTING);
                Puts("Loaded xp values");
            }
            else
            {
                Dictionary<string, int> bp_counts = new Dictionary<string, int>();
                foreach (KeyValuePair<string, CraftingInfo> kvp in crafting_info.blueprints)
                {
                    if (kvp.Value.classes != null)
                    {
                        if (!bp_counts.ContainsKey(kvp.Value.classes.First())) bp_counts.Add(kvp.Value.classes.First(), 1);
                        else bp_counts[kvp.Value.classes.First()]++;
                    }
                }
                foreach (KeyValuePair<string, int> kvp in bp_counts)
                {
                    Puts($"Loaded {kvp.Value} blueprints for {kvp.Key.TitleCase()}");
                }
            }

            foreach (var item in config.item_info)
            {
                skins.Add(item.Value.skin);
            }

            if (!config.allow_resource_bags)
            {
                Unsubscribe("CanMoveItem");
                Unsubscribe("OnLootEntityEnd");
            }

            foreach (KeyValuePair<string, CraftingInfo> kvp in crafting_info.blueprints)
            {
                if (kvp.Value.classes.Contains("weaponsmith") && !weaponsmith.ContainsKey(kvp.Key)) weaponsmith.Add(kvp.Key, kvp.Value.xp * kvp.Value.xp_multiplier);
                if (kvp.Value.classes.Contains("tailor") && !tailor.ContainsKey(kvp.Key)) tailor.Add(kvp.Key, kvp.Value.xp * kvp.Value.xp_multiplier);
                if (kvp.Value.classes.Contains("mechanic") && !mechanic.ContainsKey(kvp.Key)) mechanic.Add(kvp.Key, kvp.Value.xp * kvp.Value.xp_multiplier);
                if (kvp.Value.classes.Contains("electrician") && !electrician.ContainsKey(kvp.Key)) electrician.Add(kvp.Key, kvp.Value.xp * kvp.Value.xp_multiplier);
            }

            foreach (KeyValuePair<string, Configuration.JobInfo> kvp in config.job_info)
            {
                ImageLibrary?.Call("AddImage", kvp.Value.img, kvp.Key);
                if (!permission.GroupExists(kvp.Key)) permission.CreateGroup(kvp.Key, "", 0);
            }
            ImageLibrary?.Call("AddImage", "https://i.imgur.com/arIzmlS.png", "pinecone");
            ImageLibrary?.Call("AddImage", "https://i.imgur.com/3ySNtDo.png", "primemeat");
            ImageLibrary?.Call("AddImage", "https://i.imgur.com/Xyotu52.png", "goldnugget");
            ImageLibrary?.Call("AddImage", "https://i.imgur.com/TGHA3yK.png", "quitui");            
        }

        #endregion;

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["prefix"] = "<color=#FF0000>[Jobs]</color>",
                ["CanMakeT"] = "You can now make Tier {0} items.",
                ["ResearchXP"] = "You gained {0} xp for researching {1}",
                ["PTC1"] = "You must be crafting level {0} to craft this item. Type /class to see your current level.",
                ["PTC2"] = "You must be a {0} to craft this item. Type /class to see your current profession.",
                ["PTC3"] = "You paid ${0} to craft {1}x {2} as you are not a {3}",
                ["PTC4"] = "You do not have enough cash to craft this item. Requires ${0} per item unless you are a {1}.",
                ["PTC5"] = "You need {0} scrap per item in order to craft this item.",
                ["PTC6"] = "You paid {0} scrap to craft {1}x {2} as you are not a {3}",
                ["PTC7"] = "Refunded ${0}.",
                ["PTC8"] = "Refunded {0} scrap.",
                ["PTC9"] = "Your {0} is now level {1}",
                ["PTC10"] = "You were removed from your job.",
                ["PTC11"] = "You have been employed as a {0}",
                ["PTC12"] = "You can access the gathering bag by typing <color=#FF229A>/{0}</color>",
                ["PTC13"] = "You acquired a <color=#B600FF>{0}</color>. It was placed into your bag.",
                ["PTC14"] = "You acquired a <color=#B600FF>{0}</color>. It was placed into a new slot in your bag.",
                ["PTC15"] = "You acquired a <color=#B600FF>{0}</color>.",
                ["PTC16"] = "More than one player found: {0}",
                ["PTC17"] = "No player was found that matched: {0}",
                ["PTC18"] = "You were retro-actively awarded {0} xp for items researched in your current profession.",
                ["PTC19"] = "Added {0} new items.",
                ["PTC20"] = "Updated xp values.",
                ["PTC21"] = "Usage: /setjob <name> <job>",
                ["PTC22"] = "{0} is not valid. Valid jobs are: {1}",
                ["PTC23"] = "Unemployed. Go to town and get a job you bum.",
                ["PTC24"] = "<color=#FF7C00>Job information</color>\n<color=#FF7C00>- Job:</color> {0}\n<color=#FF7C00>- Level:</color> {1}\n<color=#FF7C00>- XP:</color> <color=#B827FC>{2}/{3}</color>",
                ["PTC25"] = "usage: /updatemultiplier <item shortname> <value>",
                ["PTC26"] = "Invalid shortname specified.",
                ["PTC27"] = "You need a crafting job to print a list of items and their xp values.",
                ["PTC28"] = "A list of items for your class has been printed to console.",
                ["PTC29"] = "You are already unemployed.",
                ["PTC30"] = "You do not have enough cash to quit your job.",
                ["PTC31"] = "You paid ${0} to quit your job.",
                ["PTC32"] = "You do not have enough scrap to quit your job.",
                ["PTC33"] = "You paid {0} scrap to quit your job.",
                ["PTC34"] = "You are already employed as a {0}",
                ["PTC35"] = "You you ate the {0} but do not feel wiser, as you are not employed!",
                ["PTC36"] = "You consume the {0} and are rewarded with 200 xp.",
                ["PTC37"] = "Please wait a moment before opening the bag again.",
                ["PTC38"] = "You can only move gold nuggets, prime meat and pinecones into this bag.",
                ["PTC39"] = "You sold 1x {0}.",
                ["PTC40"] = "You sold {0}x {1}.",
                ["PTC41"] = "You do not have any {0}.",
                ["QuitJob1"] = "It costs ${0} to quit your job, are you sure?",
                ["QuitJob2"] = "It costs {0} scrap to quit your job, are you sure?",
                ["TrackerClose"] = "You see fresh tracks to the",
                ["TrackerMid"] = "You see slightly old tracks to the",
                ["TrackerFar"] = "You see very old tracks to the",
                ["WarnSkinner"] = "You must be a skinner to use this command.",
                ["NoAnimals"] = "No animals were found.",
                ["TrackWait"] = "You need to wait a few seconds before attempting to track the animal again.",
                ["EmploymentStats"] = "There are <color=#E8FF00>{0}</color> [<color=#00F3FF>{1}%</color>] players who are employed as a <color=#00F3FF>{2}</color>.",
                ["JobPickWarn"] = "<color=#FF0C00>All levels and xp will be lost if you change your job.</color>"
            }, this);
        }

        #endregion

        #region Levels

        const string Prefix = "<color=#FF0000>[Jobs]</color>";

        public Dictionary<int, double> Levels = new Dictionary<int, double>();
        #endregion

        #region Hooks

        object OnItemAction(Item bp, string action, BasePlayer player)
        {
            if (action == "study")
            {
                if (pcdData.pEntity.TryGetValue(player.userID, out playerData))
                {
                    if (!bp.IsBlueprint()) return null;

                    var itemDef = bp.blueprintTargetDef;
                    if (!player.blueprints.HasUnlocked(itemDef))
                    {
                        if (crafting_info.blueprints.TryGetValue(itemDef.shortname, out bpinfo))
                        {
                            if (!bpinfo.classes.Contains(playerData.profession)) return null;
                            PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("ResearchXP", this, player.UserIDString), Math.Round(bpinfo.research_xp, 2), itemDef.displayName.english)}"); 
                            LevelCrafting(player, bpinfo.research_xp);
                        }
                    }
                }                
            }
            else if (action.Equals("unwrap") && bp.skin == 2582646034)
            {
                OpenBag(player);
                return false;
            }
            else if (action.Equals("upgrade")) return false;
            return null;
        }

        void OnNewSave(string filename)
        {
            pcdData.pEntity.Clear();
            pcdData.resource_bag.Clear();
            UpdateXP();
            var newItems = 0;
            CheckForNewItems(out newItems);
            if (newItems > 0) Puts($"Added {newItems} new items.");
            ClearJobGroups();
            SaveData();
            if (config.delete_carpart_vendors) RemoveVendingMachines();
        }

        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            var player = entity as BasePlayer;
            var roll = UnityEngine.Random.Range(0f, 100f);
            var tool = player.GetActiveItem()?.info?.shortname;
            if (!pcdData.pEntity.TryGetValue(player.userID, out playerData))
            {
                SetupPlayer(player);
                playerData = pcdData.pEntity[player.userID];
            }
            if (tool == null) return;
            
            if (dispenser.gatherType == ResourceDispenser.GatherType.Flesh)
            {
                if (!config.skinner_tools.Contains(tool)) return;
                var animal = GetAnimal(dispenser.baseEntity.ShortPrefabName);
                if (animal == null) return;
                if (playerData.profession.Equals("skinner"))
                {
                    object hookResult = Interface.CallHook("CanLevelSkinner", player, dispenser.baseEntity);
                    if (hookResult is bool && (bool)hookResult == false) return;
                    UpdateLevel(player, config.skinner_xp_per_hit, "skinner");
                    if (playerData.level > 0)
                    {
                        var amount = Convert.ToInt32(((playerData.level * config.gather_yield_per_level) / 100) * item.amount);
                        if (amount < 1 && amount > 0.5) amount = 1;
                        if (amount >= 1)
                        {
                            NextTick(() =>
                            {
                                var extra_items = ItemManager.CreateByItemID(item.info.itemid, amount);
                                player.GiveItem(extra_items);
                            });
                        }

                    }
                }
                if ((permission.UserHasGroup(player.UserIDString, "skinner") && roll >= 100 - ((config.prime_meat_professional * config.skinner_modifier[animal]) + playerData.bonus_chance)) || roll >= 100 - (config.prime_meat_default * config.skinner_modifier[animal]))
                {
                    var itemInfo = config.item_info["skinner"];
                    GiveItem(player, itemInfo.shortname, itemInfo.skin, itemInfo.displayName, itemInfo.quantity);
                    playerData.prime_meat_acquired++;
                    SaveData();
                }
            }
            else if (dispenser.gatherType == ResourceDispenser.GatherType.Ore)
            {
                if (config.miner_tools.Contains(tool) || (tool == "jackhammer" && config.jackhammer_xp_modifier > 0))
                {
                    if (playerData.profession.Equals("miner"))
                    {
                        object hookResult = Interface.CallHook("CanLevelMiner", player, dispenser.baseEntity);
                        if (hookResult is bool && (bool)hookResult == false) return;
                        if (tool == "jackhammer") UpdateLevel(player, config.miner_xp_per_hit * config.jackhammer_xp_modifier, "logger");
                        else UpdateLevel(player, config.miner_xp_per_hit, "miner");
                        if (playerData.level > 0)
                        {
                            var amount = Convert.ToInt32(((playerData.level * config.gather_yield_per_level) / 100) * item.amount);
                            if (amount < 1 && amount > 0.5 && tool != "jackhammer") amount = 1;
                            if (amount >= 1)
                            {
                                NextTick(() =>
                                {
                                    var extra_items = ItemManager.CreateByItemID(item.info.itemid, amount);
                                    player.GiveItem(extra_items);
                                });
                            }

                        }
                    }
                    if (tool == "jackhammer" && !config.allow_jackhammer_drops) return;
                    if ((permission.UserHasGroup(player.UserIDString, "miner") && roll >= 100 - (config.gold_nugget_professional + playerData.bonus_chance)) || roll >= 100 - config.gold_nugget_default)
                    {
                        var itemInfo = config.item_info["miner"];
                        GiveItem(player, itemInfo.shortname, itemInfo.skin, itemInfo.displayName, itemInfo.quantity);
                        playerData.gold_nuggets_acquired++;
                        SaveData();
                    }
                }                
            }
            else if (dispenser.gatherType == ResourceDispenser.GatherType.Tree)
            {
                if (config.logger_tools.Contains(tool) || (tool == "chainsaw" && config.chainsaw_xp_modifier > 0))
                {
                    if (playerData.profession.Equals("logger"))
                    {
                        object hookResult = Interface.CallHook("CanLevelLogger", player, dispenser.baseEntity);
                        if (hookResult is bool && (bool)hookResult == false) return;
                        if (tool == "chainsaw") UpdateLevel(player, config.logger_xp_per_hit * config.chainsaw_xp_modifier, "logger");
                        else UpdateLevel(player, config.logger_xp_per_hit, "logger");
                        SaveData();
                        if (playerData.level > 0)
                        {
                            var amount = Convert.ToInt32(((playerData.level * config.gather_yield_per_level) / 100) * item.amount);
                            if (amount < 1 && amount > 0.5 && tool != "chainsaw") amount = 1;
                            if (amount >= 1)
                            {
                                NextTick(() =>
                                {
                                    var extra_items = ItemManager.CreateByItemID(item.info.itemid, amount);
                                    player.GiveItem(extra_items);
                                });
                            }
                        }
                    }
                    if (tool == "chainsaw" && !config.allow_chainsaw_drops) return;
                    if ((permission.UserHasGroup(player.UserIDString, "logger") && roll >= 100 - (config.pinecone_professional + playerData.bonus_chance)) || roll >= 100 - config.pinecone_default)
                    {
                        var itemInfo = config.item_info["logger"];
                        GiveItem(player, itemInfo.shortname, itemInfo.skin, itemInfo.displayName, itemInfo.quantity);                        
                        playerData.pinecones_acquired++;
                        SaveData();
                    }
                }
            }
        }

        void OnDispenserBonus(ResourceDispenser dispenser, BaseEntity entity, Item item) => OnDispenserGather(dispenser, entity, item);

        private object CanCraft(ItemCrafter itemCrafter, ItemBlueprint bp, int amount)
        {
            var player = itemCrafter.GetComponent<BasePlayer>();
            var item = bp.GetComponent<ItemDefinition>();
            var playerData = pcdData.pEntity[player.userID];
            if (!crafting_info.blueprints.TryGetValue(item.shortname, out bpinfo)) return null;
            if (bpinfo.classes.Contains("free_craft")) return null;
            else
            {
                foreach (var profession in bpinfo.classes)
                {
                    if (permission.UserHasGroup(player.UserIDString, profession))
                    {                        
                        if (playerData.level >= bp.workbenchLevelRequired || !config.crafting_level_required) return null;
                        PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("PTC1", this, player.UserIDString), bp.workbenchLevelRequired)}");
                        return false;
                    }
                        
                }

                if (config.craft_method == CraftMethod.PreventCraft)
                {
                    PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("PTC2", this, player.UserIDString), String.Join(" or ", bpinfo.classes))}");
                    return false;
                }
                else if (config.craft_method == CraftMethod.TaxEcon)
                {
                    var  balance = (double)Economics?.Call("Balance", player.userID);
                    var cost = (double)(bpinfo.tax * amount); 
                    if (balance >= cost && Convert.ToBoolean(Economics?.Call("Withdraw", player.userID, cost)))
                    {
                        PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("PTC3", this, player.UserIDString), cost, amount, item.displayName.english, String.Join(" or ", bpinfo.classes))}");
                        return null;
                    }
                    else
                    {
                        PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("PTC4", this, player.UserIDString), bpinfo.tax, String.Join(" or ", bpinfo.classes))}");
                        return false;
                    }
                }
                else
                {
                    var found = 0;
                    var cost = bpinfo.tax * amount;
                    foreach (var scrap in player.inventory.AllItems())
                    {
                        if (scrap.info.shortname == "scrap") found += scrap.amount;
                        if (found >= cost) break;
                    }
                    if (found < cost)
                    {
                        PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("PTC5", this, player.UserIDString), bpinfo.tax)}");
                        return false;
                    }
                    foreach (var scrap in player.inventory.AllItems())
                    {
                        if (scrap.info.shortname == "scrap")
                        {
                            if (cost <= 0)
                            {
                                break;
                            }

                            if (scrap.amount >= cost)
                            {
                                scrap.UseItem(cost);
                                break;
                            }
                            else
                            {
                                cost -= scrap.amount;
                                scrap.UseItem(scrap.amount);
                            }
                        }
                    }
                    PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("PTC6", this, player.UserIDString), bpinfo.tax * amount, amount, item.displayName.english, String.Join(" or ", bpinfo.classes))}");
                    return null;
                }
            }
        }

        void OnItemCraftCancelled(BasePlayer player, ItemCraftTask task)
        {
            if (config.craft_method == CraftMethod.PreventCraft) return;
            if (crafting_info.blueprints.TryGetValue(task.blueprint.targetItem.shortname, out bpinfo))
            {
                if (bpinfo.classes.Contains("free_craft")) return;
                if (player != null)
                    foreach (var profession in bpinfo.classes)
                {
                    if (permission.UserHasGroup(player.UserIDString, profession))
                    {
                        return;
                    }
                }
                if (config.craft_method == CraftMethod.TaxEcon)
                {
                    var cost = (double)(task.amount * bpinfo.tax);
                    if (Convert.ToBoolean(Economics?.Call("Deposit", player.userID, cost)))
                    {
                        PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("PTC7", this, player.UserIDString), cost)}");
                        return;
                    }                    
                }
                else
                {
                    var cost = task.amount * bpinfo.tax;
                    var item = ItemManager.CreateByName("scrap", cost);
                    player.GiveItem(item);
                    PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("PTC8", this, player.UserIDString), cost)}");
                }
            }
        }

        void OnItemCraftFinished(BasePlayer player, ItemCraftTask task, Item item)
        {

            if (player == null) return;
            if (!pcdData.pEntity.TryGetValue(player.userID, out playerData)) return;
            if (!crafting_info.blueprints.TryGetValue(item.info.shortname, out bpinfo)) return;
            foreach (var c in bpinfo.classes)
            {
                if (permission.UserHasGroup(player.UserIDString, c) || (playerData.level == 0 && playerData.job_type == "crafting"))
                {
                    LevelCrafting(player, bpinfo.xp * bpinfo.xp_multiplier);
                    return;
                }
            }            
        }

        #endregion

        #region Helper        

        CraftingInfo bpinfo;
        void ClearJobGroups()
        {
            foreach (var job in professions)
            {
                foreach (var jobUser in permission.GetUsersInGroup(job))
                {
                    permission.RemoveUserGroup(jobUser.Split(' ')[0], job);
                }
            }
        }

        void UpdateXP()
        {
            foreach (var bp in ItemManager.GetBlueprints())
            {
                if (bp.isResearchable || bp.defaultBlueprint)
                {
                    var item = bp.GetComponent<ItemDefinition>();
                    if (item == null) continue;                    
                    var exp = CalculateXP(bp);
                    if (crafting_info.blueprints.TryGetValue(item.shortname, out bpinfo))
                    {
                        bpinfo.xp = exp;
                        bpinfo.research_xp = exp * bpinfo.xp_multiplier * config.research_xp_multiplier;
                    }
                    else
                    {
                        crafting_info.blueprints.Add(item.shortname, new CraftingInfo()
                        {
                            classes = { GetItemCategory(item) },
                            xp = exp,
                            xp_multiplier = 1,
                            research_xp = exp * config.research_xp_multiplier
                        });
                    }
                }
            }
            SaveData(SaveType.CRAFTING);
            Puts("Loaded new xp values");
        }
        
        double CalculateXP(ItemBlueprint bp)
        {
            var xp = 0d;
            float ingredient;
            foreach (var material in bp.ingredients)
            {                
                if (config.component.TryGetValue(material.itemDef.shortname, out ingredient)) xp = xp + (ingredient * material.amount);
            }
            return Math.Round(xp, 2);
        }

        bool ItemIsMechanical(ItemDefinition item)
        {
            for (int i = 1; i < 4; i++)
            {
                if (item.shortname.StartsWith("carburetor")) return true;
                if (item.shortname.StartsWith("piston")) return true;
                if (item.shortname.StartsWith("sparkplug")) return true;
                if (item.shortname.StartsWith("crankshaft")) return true;
                if (item.shortname.StartsWith("valve")) return true;            }
                if (item.shortname.Contains("vehicle.")) return true;
            return false;
        }

        string GetItemCategory(ItemDefinition item)
        {
            if (item == null) return "free_craft";
            var bp = item.GetComponent<ItemBlueprint>();
            if (bp.workbenchLevelRequired == 0) return "free_craft";
            if (item.category == ItemCategory.Weapon) return "weaponsmith";
            else if (item.category == ItemCategory.Attire) return "tailor";
            else if (item.category == ItemCategory.Electrical) return "electrician";
            else if (ItemIsMechanical(item)) return "mechanic";
            return "free_craft";
        }               

        void CheckForNewItems(out int newitems)
        {
            var count = 0;
            foreach (var bp in ItemManager.GetBlueprints())
            {
                var item = bp.GetComponent<ItemDefinition>();
                if (item == null) continue;
                double exp;
                if (!crafting_info.blueprints.ContainsKey(item.shortname))
                {
                    if (!bp.isResearchable && !bp.defaultBlueprint) continue;
                    exp = CalculateXP(bp);
                    var tax = config.default_tax_items_tier_1;
                    if (bp.workbenchLevelRequired == 2) tax = config.default_tax_items_tier_2;
                    else if (bp.workbenchLevelRequired == 3) tax = config.default_tax_items_tier_3;
                    crafting_info.blueprints.Add(item.shortname, new CraftingInfo()
                    {
                        classes = { GetItemCategory(item) },
                        xp = exp,
                        tax = tax
                    });
                    Puts($"Added new item: {item.shortname} - xp: {exp}");
                    count++;
                }
            }
            SaveData(SaveType.CRAFTING);
            newitems = count;
        }

        void SetupPlayer(BasePlayer player)
        {
            pcdData.pEntity.Add(player.userID, new PCDInfo() { name = player.displayName });
            SaveData();
        }

        void UpdateLevel(BasePlayer player, double xp, string job)
        {            
            if (!pcdData.pEntity.TryGetValue(player.userID, out playerData))
            {
                SetupPlayer(player);
                playerData = pcdData.pEntity[player.userID];
            }            
            playerData.xp = playerData.xp + xp;
            foreach (KeyValuePair<int, double> kvp in Levels)
            {
                if (kvp.Key > playerData.level)
                {
                    if (playerData.xp >= kvp.Value)
                    {
                        var bonusxp = 0f;                        
                        playerData.level = kvp.Key;
                        if (job == "skinner") bonusxp = config.prime_meat_chance_per_level * config.prime_meat_chance_per_level;
                        if (job == "miner") bonusxp = config.gold_nugget_chance_per_level * config.gold_nugget_chance_per_level;
                        if (job == "logger") bonusxp = config.pinecone_chance_per_level * config.pinecone_chance_per_level;
                        playerData.bonus_chance = bonusxp;
                        PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("PTC9", this, player.UserIDString), playerData.profession, playerData.level)}");
                    }                    
                }
            }
            SaveData();
        }

        void ClearUsersGroups(string playerid)
        {
            foreach (var perm in professions)
            {
                if (permission.UserHasGroup(playerid, perm)) permission.RemoveUserGroup(playerid, perm);
            }
        }

        void Unemploy(BasePlayer player)
        {
            if (pcdData.pEntity.ContainsKey(player.userID))
            {                
                var playerData = pcdData.pEntity[player.userID];
                if (permission.UserHasGroup(player.UserIDString, playerData.profession)) permission.RemoveUserGroup(player.UserIDString, playerData.profession);
                Interface.CallHook("OnPlayerUnemployed", player, playerData.profession);
                playerData.level = 0;
                playerData.xp = 0;
                permission.RemoveUserGroup(player.UserIDString, playerData.profession);
                playerData.profession = default_job;
                playerData.job_type = null;
                playerData.bonus_chance = 0.0;
                SaveData();
                
            }
            ClearUsersGroups(player.UserIDString);
            PrintToChat(player, $"{Prefix} {lang.GetMessage("PTC10", this, player.UserIDString)}");
        }

        string GetJobType(string job)
        {
            List<string> gathering_jobs = new List<string>(){"skinner", "miner", "logger"};
            if (gathering_jobs.Contains(job)) return "gathering";
            else return "crafting";
        }

        void Employ(BasePlayer player, string job)
        {
            ClearUsersGroups(player.UserIDString);
            permission.AddUserGroup(player.UserIDString, job);
            if (!pcdData.pEntity.ContainsKey(player.userID)) SetupPlayer(player);
            var playerData = pcdData.pEntity[player.userID];
            var jobType = GetJobType(job);
            if (config.disable_crafting_professions && jobType == "crafting")
            {
                PrintToChat(player, $"Cannot become a {job} as crafting jobs are disabled.");
                return;
            }
            playerData.profession = job;
            playerData.level = 0;
            playerData.xp = 0;
            playerData.job_type = jobType;
            permission.AddUserGroup(player.UserIDString, job);
            SaveData();
            PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("PTC11", this, player.UserIDString), job.TitleCase())}");
            Interface.CallHook("OnPlayerEmployed", player, job);

            if (permission.UserHasPermission(player.UserIDString, "professions.chat.bag") && config.allow_resource_bags && config.bag_chat_command_allow && playerData.job_type == "gathering")
            {
                PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("PTC12", this, player.UserIDString), config.bag_chat_cmd)}");
            }
        }

        string GetAnimal(string target)
        {
            foreach (var animal in config.skinner_modifier)
            {
                if (target.Contains(animal.Key)) return animal.Key;
            }
            return null;
        }

        bool BagInInventory(BasePlayer player)
        {
            foreach (var item in player.inventory.AllItems())
            {
                if (item.skin == 2582646034) return true;
            }
            return false;
        }

        void GiveItem(BasePlayer player, string shortname, ulong skin, string displayName, int quantity = 1)
        {
            var item = ItemManager.CreateByName(shortname, quantity, skin);
            item.name = displayName;
            var leftOver = 0;            
            if (config.allow_resource_bags && (BagInInventory(player) || permission.UserHasPermission(player.UserIDString, "professions.chat.bag")))
            {
                if (pcdData.resource_bag.TryGetValue(player.userID, out bagData))
                {                    
                    foreach (var itemRef in bagData._storage)
                    {
                        if (itemRef.skin == item.skin)
                        {
                            if (itemRef.amount >= config.max_stack_size_in_bag) continue;
                            if (itemRef.amount + item.amount > config.max_stack_size_in_bag)
                            {
                                leftOver = (itemRef.amount + item.amount) - config.max_stack_size_in_bag;
                            }
                            itemRef.amount += item.amount - leftOver;
                            SaveData();
                            PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("PTC13", this, player.UserIDString), displayName)}");
                            if (leftOver > 0) break;
                            else
                            {
                                item.Remove();
                                return;
                            }                                
                        }
                    }
                    var slot = GetFreeSlot(bagData);
                    if (slot > -1)                    
                    {
                        var qty = quantity;
                        if (leftOver > 0) qty = leftOver;
                        bagData._storage.Add(new BagInfo()
                        {
                            amount = qty,
                            displayName = displayName,
                            shortname = shortname,
                            skin = skin,
                            slot = slot
                        });
                        SaveData();
                        PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("PTC14", this, player.UserIDString), displayName)}");
                        item.Remove();
                        return;
                    }
                }
                else
                {
                    pcdData.resource_bag.Add(player.userID, new ResourceBag());
                    pcdData.resource_bag[player.userID]._storage.Add(new BagInfo()
                    {
                        amount = quantity,
                        displayName = displayName,
                        shortname = shortname,
                        skin = skin,
                        slot = 0
                    });
                    SaveData();
                    PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("PTC14", this, player.UserIDString), displayName)}");
                    item.Remove();
                    return;
                }
            }
            if (leftOver > 0)
            {
                item.amount = leftOver;
            }
            player.GiveItem(item);
            PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("PTC15", this, player.UserIDString), displayName)}");
        }

        int GetFreeSlot(ResourceBag bag)
        {
            var result = -1;
            for (int i = 0; i < 12; i++)
            {
                result = i;
                foreach (var ingredient in bag._storage)
                {
                    if (ingredient.slot == i)
                    {
                        result = -1;
                        break;
                    }
                }
                if (result == -1) continue;
                else return result;
            }
            return -1;
        }

        private BasePlayer FindPlayerByName(string Playername, BasePlayer SearchingPlayer = null)
        {
            var targetList = BasePlayer.allPlayerList.Where(x => x.displayName.ToLower().Contains(Playername.ToLower())).OrderBy(x => x.displayName.Length);
            if (targetList.Count() == 1) return targetList.First();
            if (targetList.Count() > 1)
            {
                if (targetList.First().displayName.ToLower() == Playername.ToLower()) return targetList.First();
                if (SearchingPlayer != null) PrintToChat(SearchingPlayer, $"{Prefix} {string.Format(lang.GetMessage("PTC16", this, SearchingPlayer.UserIDString), String.Join(",", targetList.Select(x => x.displayName)))}");
                return null;
            }
            if (targetList.Count() == 0)
            {                
                if (SearchingPlayer != null) PrintToChat(SearchingPlayer, $"{Prefix} {string.Format(lang.GetMessage("PTC17", this, SearchingPlayer.UserIDString), Playername)}");
                return null;
            }
            return null;
        }

        public double XPUntilLevel(ulong id)
        {            
            if (!pcdData.pEntity.ContainsKey(id)) return 0;            
            var playerData = pcdData.pEntity[id];            
            if (playerData.profession.Equals(default_job)) return 0;
            if (playerData.job_type.Equals("crafting"))
            {                
                if (playerData.level == 0) return config.crafting_level_1;
                if (playerData.level == 1) return config.crafting_level_2;
                if (playerData.level == 2) return config.crafting_level_3;
                else return 0;
            }
            if (playerData.job_type.Equals("gathering"))
            {
                foreach (KeyValuePair<int, double> kvp in Levels)
                {
                    if (kvp.Key > playerData.level)
                    {
                        return kvp.Value;
                    }
                }
            }
            return 0;
        }

        #endregion

        #region Chat Commands

        void JobMenuCMD(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, "professions.chat.jobmenu")) CentrelinkMenu(player);
        }

        void ResourceBagCommand(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "professions.chat.bag") || !config.allow_resource_bags || !config.bag_chat_command_allow) return;
            OpenBag(player);
        }

        void OpenMarket(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, "professions.chat.market")) SendProfessionsMarket(player);
        }

        [ChatCommand("updateplayerxp")]
        void UpdatePlayerXP(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "professions.admin")) return;
            List<ItemDefinition> bps = new List<ItemDefinition>();
            ItemDefinition def;
            foreach (var bp in ItemManager.GetBlueprints())
            {
                if (bp.defaultBlueprint) continue;
                if (!bp.isResearchable) continue;       
                def = bp.GetComponent<ItemDefinition>();
                if (crafting_info.blueprints.ContainsKey(def.shortname))
                {
                    bps.Add(def);
                }
            }
            Puts($"Added {bps.Count} bps.");
            var found = 0;
            foreach (var user in BasePlayer.allPlayerList)
            {
                if (pcdData.pEntity.TryGetValue(user.userID, out playerData))
                {
                    found++;
                    var oldXP = playerData.xp;
                    if (playerData.job_type == null || playerData.job_type == "gathering") continue;
                    foreach (var bp in bps)
                    {
                        if (player.blueprints.HasUnlocked(bp) && crafting_info.blueprints[bp.shortname].classes.Contains(playerData.profession))
                        {
                            playerData.xp += crafting_info.blueprints[bp.shortname].research_xp;
                        }
                    }
                    SaveData();                    
                    if (playerData.xp > oldXP && user.IsConnected) PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("PTC18", this, player.UserIDString), playerData.xp - oldXP)}");
                }
            }
            Puts($"Found: {found} players.");
        }

        [ChatCommand("clearjobs")]
        void ClearJobs(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "professions.admin")) return;
            ClearJobGroups();
        }

        [ChatCommand("updateitems")]
        void UpdateItems(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "professions.admin")) return;
            var count = 0;
            CheckForNewItems(out count);
            PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("PTC19", this, player.UserIDString), count)}");
        }

        [ChatCommand("updatexp")]
        void Updatexp(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "professions.admin")) return;
            UpdateXP();
            PrintToChat(player, $"{Prefix} {lang.GetMessage("PTC20", this, player.UserIDString)}");
        }

        [ChatCommand("setjob")]
        void SetJob(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "professions.admin")) return;
            if (args.Length != 2)
            {
                PrintToChat(player, $"{Prefix} {lang.GetMessage("PTC21", this, player.UserIDString)}");
                return;
            }
            var target = FindPlayerByName(args[0]);
            if (target == null) return;
            var job = args[1].ToLower();
            if (!professions.Contains(job) && !job.Equals("unemployed"))
            {
                PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("PTC22", this, player.UserIDString), job, String.Join(", ", professions))}");
                return;
            }
            Unemploy(target);
            if (!job.Equals("unemployed")) Employ(target, job);

        }

        [ChatCommand("class")]
        void GetClass(BasePlayer player)
        {
            if (!pcdData.pEntity.TryGetValue(player.userID, out playerData) || playerData.job_type == null)
            {
                PrintToChat(player, $"{Prefix} {lang.GetMessage("PTC23", this, player.UserIDString)}");
                return;
            }
            PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("PTC24", this, player.UserIDString), playerData.profession.TitleCase(), playerData.level, Math.Round(playerData.xp, 2), XPUntilLevel(player.userID))}");
        }

        [ChatCommand("printlevels")]
        void PrintLevels(BasePlayer player)
        {
            foreach (var level in Levels)
            {
                PrintToConsole(player, $"{Prefix} {level.Key} - {level.Value}");
            }
        }

        [ChatCommand("updatemultiplier")]
        void UpdateMultiplier(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "professions.admin")) return;
            if (args.Length != 2 || !args[1].IsNumeric())
            {
                PrintToChat(player, $"{Prefix} {lang.GetMessage("PTC25", this, player.UserIDString)}");
                return;
            }
            if (crafting_info.blueprints.TryGetValue(args[0], out bpinfo))
            {
                bpinfo.xp_multiplier = Convert.ToDouble(args[1]);
                SaveData();
            }
            else PrintToChat(player, $"{Prefix} {lang.GetMessage("PTC26", this, player.UserIDString)}");
        }

        [ChatCommand("showitems")]
        void ShowItems(BasePlayer player)
        {
            if (!pcdData.pEntity.TryGetValue(player.userID, out playerData) || playerData.job_type != "crafting")
            {
                PrintToChat(player, $"{Prefix} {lang.GetMessage("PTC27", this, player.UserIDString)}");
                return;
            }
            if (playerData.profession == "weaponsmith") PrintItems(player, weaponsmith);
            else if (playerData.profession == "tailor") PrintItems(player, tailor);
            else if (playerData.profession == "mechanic") PrintItems(player, mechanic);
            else if (playerData.profession == "electrician") PrintItems(player, electrician);
        }

        [ChatCommand("clearstoragebags")]
        void ClearRecipeBags(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "professions.admin")) return;
            pcdData.resource_bag.Clear();
            SaveData();
            PrintToChat(player, $"{Prefix} Cleared storage bags data.");
        }

        [ChatCommand("clearprofessions")]
        void ClearProfessions(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "professions.admin")) return;
            pcdData.pEntity.Clear();
            SaveData();
            ClearJobGroups();
            PrintToChat(player, $"{Prefix} Cleared professions data.");
        }

        [ChatCommand("resettax")]
        void UpdateTax(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "professions.admin")) return;
            foreach (var bp in ItemManager.GetBlueprints())
            {
                var item = bp.GetComponent<ItemDefinition>();
                if (crafting_info.blueprints.TryGetValue(item.shortname, out bpinfo))
                {
                    var tax = config.default_tax_items_tier_1;
                    if (bp.workbenchLevelRequired == 2) tax = config.default_tax_items_tier_2;
                    else if (bp.workbenchLevelRequired == 3) tax = config.default_tax_items_tier_3;
                    bpinfo.tax = tax;
                }
            }
            SaveData(SaveType.CRAFTING);
            PrintToChat(player, $"{Prefix} Reset taxes to default.");
        }

        void PrintItems(BasePlayer player, Dictionary<string, double> items)
        {
            var str = "<color=#FF9E00>__________\n\nCrafting table:</color>\n\n";
            foreach (KeyValuePair<string, double> kvp in items)
            {
                str = $"{str} <color=#FF229A>Item:</color> {kvp.Key} - <color=#57EB02>XP:</color> {kvp.Value}\n";
            }
            str = $"{str}\n<color=#FF9E00>__________</color>";
            PrintToConsole(player, str);
            PrintToChat(player, $"{Prefix} {lang.GetMessage("PTC28", this, player.UserIDString)}");
        }

        #endregion

        #region Menus

        void QuitUI(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0.854902 0.854902 0.854902 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.3721935 0.3724469",
                    AnchorMax = "0.6343787 0.7437127"
                },

                CursorEnabled = true
            }, "Overlay", "Rust Quit UI");

            container.Add(new CuiElement
            {
                Name = "QuitImage",
                Parent = "Rust Quit UI",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Color = "1 1 1 1",
                        Png = (string)ImageLibrary?.Call("GetImage", "quitui")
                    },

                    new CuiRectTransformComponent
                    {
                        AnchorMin = "-7.312374E-07 0.2746351",
                        AnchorMax = "0.9999933 0.9999782"
                    }
                }
            });

            container.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0.8679245 0.7336845 0.257921 1"
                },

                RectTransform =
                {
                    AnchorMin = "5.624903E-08 0.1987483",
                    AnchorMax = "0.9999999 0.2746394"
                },

                CursorEnabled = false
            }, "Rust Quit UI", "Top Text Panel");

            container.Add(new CuiLabel
            {
                Text =
                {
                    Text = "Quit your Job?",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                    Font = "robotocondensed-bold.ttf",
                    Color = "0 0 0 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.3379522 0.1987483",
                    AnchorMax = "0.6620479 0.2746338"
                },
            }, "Rust Quit UI", "QuitHeader");

            container.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0.0471698 0.04561231 0.04561231 1"
                },

                RectTransform =
                {
                    AnchorMin = "-7.593619E-06 -7.023726E-06",
                    AnchorMax = "1 0.1987369"
                },

                CursorEnabled = false
            }, "Rust Quit UI", "Bottom text panel");

            container.Add(new CuiButton
            {
                Button =
                {
                    Command = $"acceptquit",
                    Color = "0.8666667 0.7333333 0.2588235 1"
                },

                Text =
                {
                    Text = "Confirm",
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = "0.1350125 0.7735849 0.2878551 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.03437706 0.2306984",
                    AnchorMax = "0.2442124 0.7693016"
                },
            }, "Bottom text panel", "Confirm button");

            container.Add(new CuiButton
            {
                Button =
                {
                    Command = $"closeHRmenu",
                    Color = "0.8666667 0.7333333 0.2588235 1"
                },

                Text =
                {
                    Text = "Deny",
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = "0.772549 0.2376549 0.1333333 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.7535758 0.2306984",
                    AnchorMax = "0.9634112 0.7693016"
                },
            }, "Bottom text panel", "Deny button");

            container.Add(new CuiLabel
            {
                Text =
                {
                    Text = quitText,
                    FontSize = 12,
                    Align = TextAnchor.UpperCenter,
                    Font = "robotocondensed-regular.ttf",
                    Color = "1 1 1 1"
                },

                RectTransform =
                {
                    AnchorMin = "0.2763187 0.05099668",
                    AnchorMax = "0.7236738 0.1987501"
                },
            }, "Rust Quit UI", "button text label");


            CuiHelper.AddUi(player, container);
        }

        [PluginReference]
        private Plugin Economics, ImageLibrary;

        [ChatCommand("quitjob")]
        private void QuitJob(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "Rust Quit UI");
            QuitUI(player);
        }

        [ConsoleCommand("closeHRmenu")]
        private void closeHRmenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            CuiHelper.DestroyUi(player, "Rust Quit UI");

            CentrelinkMenu(player);
        }

        [ConsoleCommand("acceptquit")]
        private void acceptquit(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, "Rust Quit UI");
            if (config.ClassChangeCost < 0)  return;
            
            if (!pcdData.pEntity.ContainsKey(player.userID) || pcdData.pEntity[player.userID].profession == default_job)
            {
                PrintToChat(player, $"{Prefix} {lang.GetMessage("PTC29", this, player.UserIDString)}");
                return;
            }

            if (config.currency.ToLower() == "economics" && Economics != null)
            {
                var playerBalance = (double)Economics?.Call("Balance", player.userID);
                if (playerBalance < config.ClassChangeCost)
                {
                    PrintToChat(player, $"{Prefix} {lang.GetMessage("PTC30", this, player.UserIDString)}");
                    return;
                }
                if (!Convert.ToBoolean(Economics?.Call("Withdraw", player.userID, (double)config.ClassChangeCost))) return;
                PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("PTC31", this, player.UserIDString), config.ClassChangeCost)}");
                Unemploy(player);
                return;
            }
            if (config.currency.ToLower() == "scrap")
            {
                var found = 0;
                foreach (var item in player.inventory.AllItems())
                {
                    if (item.info.shortname == "scrap") found += item.amount;
                    if (found >= config.ClassChangeCost) break;
                }
                if (found < config.ClassChangeCost)
                {
                    PrintToChat(player, $"{Prefix} {lang.GetMessage("PTC32", this, player.UserIDString)}");
                    return;
                }
                found = 0;
                foreach (var item in player.inventory.AllItems())
                {
                    if (item.info.shortname == "scrap")
                    {
                        if (item.amount <= config.ClassChangeCost - found)
                        {
                            found += item.amount;
                            item.UseItem(item.amount);
                        }
                        else
                        {
                            item.UseItem((int)config.ClassChangeCost - found);
                            break;
                        }
                        if (found >= config.ClassChangeCost) break;
                    }
                    PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("PTC33", this, player.UserIDString), config.ClassChangeCost)}");
                    Unemploy(player);
                    return;
                }
            }            
        }

        private double CalculateJobPercentage(string Job)
        {
            int Jobs = 0;
            int CurrentJob = 0;
            var allClasses = professions;
            foreach (var c in allClasses)
            {
                int i = permission.GetUsersInGroup(c).Length;
                if (c.Equals(Job)) CurrentJob = (CurrentJob + i);

                Jobs = (Jobs + i);
            }
            if (Jobs < 1) return 0;
            return ((CurrentJob * 100) / Jobs);
        }

        #endregion

        #region Job Menu

        string FormatDescription(string description, string userid)
        {
            if (config.job_change_warning_text)
            {
                return string.Format("{0}\n{1}", description, lang.GetMessage("JobPickWarn", this, userid));
            }
            else return description;
        }

        void SendJobMenu(BasePlayer player, string job)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0.8823529" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-0.353 -0.328", OffsetMax = "0.347 0.332" }
            }, "Overlay", "Professions_Menu");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.002091496 0.2565822 0.4433962 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-175 20.16", OffsetMax = "175 195.16" }
            }, "Professions_Menu", "pm_img_backpanel");

            container.Add(new CuiElement
            {
                Name = "pm_img",
                Parent = "pm_img_backpanel",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = (string)ImageLibrary?.Call("GetImage", job) },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 -82.5", OffsetMax = "170 82.5" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.003921569 0.254902 0.4431373 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-87.5 -50.079", OffsetMax = "87.5 10.08" }
            }, "Professions_Menu", "pm_title_backpanel");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.03529412 0.3921569 0.6588235 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-82.5 -25.079", OffsetMax = "82.5 25.08" }
            }, "pm_title_backpanel", "Panel_8824");

            container.Add(new CuiElement
            {
                Name = "pm_title",
                Parent = "pm_title_backpanel",
                Components = {
                    new CuiTextComponent { Text = job.ToUpper(), Font = "robotocondensed-bold.ttf", FontSize = 26, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-82.5 -25.079", OffsetMax = "82.5 25.08" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.003921569 0.254902 0.4431373 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-175 -200.677", OffsetMax = "175 -60.163" }
            }, "Professions_Menu", "pm_description_backpanel");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.03426487 0.3920433 0.6603774 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 -65.259", OffsetMax = "170 65.261" }
            }, "pm_description_backpanel", "Panel_5263");

            container.Add(new CuiElement
            {
                Name = "pm_description",
                Parent = "Panel_5263",
                Components = {
                    new CuiTextComponent { Text = FormatDescription(config.job_info[job].description, player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = config.JobInfoFontSize, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-165 -60.26", OffsetMax = "165 60.26" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.003921569 0.254902 0.4431373 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-175 -50.08", OffsetMax = "-97.5 10.079" }
            }, "Professions_Menu", "pm_accept_panel");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.03529412 0.3921569 0.6588235 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-33.75 -25.08", OffsetMax = "33.75 25.079" }
            }, "pm_accept_panel", "pm_accept_frontpanel");

            container.Add(new CuiElement
            {
                Name = "pm_tick_img",
                Parent = "pm_accept_frontpanel",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Sprite = "assets/icons/check.png" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30 -30", OffsetMax = "30 30" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = $"acceptprofession {job}" },
                Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-33.75 -25.079", OffsetMax = "33.75 25.08" }
            }, "pm_accept_frontpanel", "pm_accept_button");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.003921569 0.254902 0.4431373 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "97.5 -50.08", OffsetMax = "175 10.079" }
            }, "Professions_Menu", "pm_decline_panel");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.03529412 0.3921569 0.6588235 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-33.75 -25.079", OffsetMax = "33.75 25.08" }
            }, "pm_decline_panel", "pm_decline_frontpanel");

            container.Add(new CuiElement
            {
                Name = "pm_x_img",
                Parent = "pm_decline_panel",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Sprite = "assets/icons/vote_down.png" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-20 -20", OffsetMax = "20 20" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = "closepmbutton" },
                Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-33.75 -25.079", OffsetMax = "33.75 25.08" }
            }, "pm_decline_panel", "pm_decline_button");

            CuiHelper.DestroyUi(player, "Professions_Menu");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("closepmbutton")]
        private void ClosePMMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            CuiHelper.DestroyUi(player, "Professions_Menu");

            CentrelinkMenu(player);
        }

        [ConsoleCommand("acceptprofession")]
        private void AcceptJob(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, "Professions_Menu");

            if (arg.Args.Length != 1) return;
            var job = arg.Args[0].ToLower();
            if (!professions.Contains(job)) return;

            if (!pcdData.pEntity.ContainsKey(player.userID)) SetupPlayer(player);
            var playerData = pcdData.pEntity[player.userID];
            if (playerData.profession != default_job)
            {
                PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("PTC34", this, player.UserIDString), playerData.profession.TitleCase())}");
                return;
            }
            Employ(player, job);
        }

        #endregion

        #region Centrelink Menu        

        void CentrelinkMenu(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0.8823529" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-0.353 -0.328", OffsetMax = "0.347 0.332" }
            }, "Overlay", "Professions_Menu");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.002091496 0.2565822 0.4433962 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-175.32 170.241", OffsetMax = "174.68 239.759" }
            }, "Professions_Menu", "pm_img_backpanel_header");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.03529412 0.3921569 0.6588235 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 -30", OffsetMax = "170 30" }
            }, "pm_img_backpanel_header", "pm_img_frontpanel_header");

            container.Add(new CuiElement
            {
                Name = "pm_header_text",
                Parent = "pm_img_frontpanel_header",
                Components = {
                    new CuiTextComponent { Text = "Job Services", Font = "robotocondensed-bold.ttf", FontSize = 32, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 -30", OffsetMax = "170 30" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.002091496 0.2565822 0.4433962 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-175 110.7", OffsetMax = "175 160.7" }
            }, "Professions_Menu", "pm_img_backpanel_1");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.03426487 0.3920433 0.6603774 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 -20", OffsetMax = "170 20" }
            }, "pm_img_backpanel_1", "pm_img_frontpanel_1");

            container.Add(new CuiElement
            {
                Name = "pm_img_header_1",
                Parent = "pm_img_frontpanel_1",
                Components = {
                    new CuiTextComponent { Text = "Miner", Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 -20", OffsetMax = "170 20" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = "sendjobinfo miner" },
                Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 -20", OffsetMax = "170 20" }
            }, "pm_img_frontpanel_1", "pm_img_button_1");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.002091496 0.2565822 0.4433962 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-175 55.7", OffsetMax = "175 105.7" }
            }, "Professions_Menu", "pm_img_backpanel_2");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.03426487 0.3920433 0.6603774 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 -20", OffsetMax = "170 20" }
            }, "pm_img_backpanel_2", "pm_img_frontpanel_2");

            container.Add(new CuiElement
            {
                Name = "pm_img_header_2",
                Parent = "pm_img_frontpanel_2",
                Components = {
                    new CuiTextComponent { Text = "Logger", Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 -20", OffsetMax = "170 20" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = "sendjobinfo logger" },
                Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 -20", OffsetMax = "170 20" }
            }, "pm_img_frontpanel_2", "pm_img_button_2");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.002091496 0.2565822 0.4433962 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-175 0.7", OffsetMax = "175 50.7" }
            }, "Professions_Menu", "pm_img_backpanel_3");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.03426487 0.3920433 0.6603774 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 -20", OffsetMax = "170 20" }
            }, "pm_img_backpanel_3", "pm_img_frontpanel_3");

            container.Add(new CuiElement
            {
                Name = "pm_img_header_3",
                Parent = "pm_img_frontpanel_3",
                Components = {
                    new CuiTextComponent { Text = "Skinner", Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 -20", OffsetMax = "170 20" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = "sendjobinfo skinner" },
                Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 -20", OffsetMax = "170 20" }
            }, "pm_img_frontpanel_3", "pm_img_button_3");            

            if (!config.disable_crafting_professions)
            {
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.002091496 0.2565822 0.4433962 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-175 -54.3", OffsetMax = "175 -4.3" }
                }, "Professions_Menu", "pm_img_backpanel_4");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.03426487 0.3920433 0.6603774 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 -20", OffsetMax = "170 20" }
                }, "pm_img_backpanel_4", "pm_img_frontpanel_4");

                container.Add(new CuiElement
                {
                    Name = "pm_img_header_4",
                    Parent = "pm_img_frontpanel_4",
                    Components = {
                    new CuiTextComponent { Text = "Weaponsmith", Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 -20", OffsetMax = "170 20" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "1 1 1 0", Command = "sendjobinfo weaponsmith" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 -20", OffsetMax = "170 20" }
                }, "pm_img_frontpanel_4", "pm_img_button_4");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.002091496 0.2565822 0.4433962 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-175 -109.3", OffsetMax = "175 -59.3" }
                }, "Professions_Menu", "pm_img_backpanel_5");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.03426487 0.3920433 0.6603774 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 -20", OffsetMax = "170 20" }
                }, "pm_img_backpanel_5", "pm_img_frontpanel_5");

                container.Add(new CuiElement
                {
                    Name = "pm_img_header_5",
                    Parent = "pm_img_frontpanel_5",
                    Components = {
                    new CuiTextComponent { Text = "Mechanic", Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 -20", OffsetMax = "170 20" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "1 1 1 0", Command = "sendjobinfo mechanic" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 -20", OffsetMax = "170 20" }
                }, "pm_img_frontpanel_5", "pm_img_button_5");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.002091496 0.2565822 0.4433962 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-175 -164.3", OffsetMax = "175 -114.3" }
                }, "Professions_Menu", "pm_img_backpanel_6");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.03426487 0.3920433 0.6603774 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 -20", OffsetMax = "170 20" }
                }, "pm_img_backpanel_6", "pm_img_frontpanel_6");

                container.Add(new CuiElement
                {
                    Name = "pm_img_header_6",
                    Parent = "pm_img_frontpanel_6",
                    Components = {
                    new CuiTextComponent { Text = "Electrician", Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 -20", OffsetMax = "170 20" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "1 1 1 0", Command = "sendjobinfo electrician" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 -20", OffsetMax = "170 20" }
                }, "pm_img_frontpanel_6", "pm_img_button_6");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.002091496 0.2565822 0.4433962 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-175 -219.3", OffsetMax = "175 -169.3" }
                }, "Professions_Menu", "pm_img_backpanel_7");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.03426487 0.3920433 0.6603774 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 -20", OffsetMax = "170 20" }
                }, "pm_img_backpanel_7", "pm_img_frontpanel_7");

                container.Add(new CuiElement
                {
                    Name = "pm_img_header_7",
                    Parent = "pm_img_frontpanel_7",
                    Components = {
                    new CuiTextComponent { Text = "Tailor", Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 -20", OffsetMax = "170 20" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "1 1 1 0", Command = "sendjobinfo tailor" },
                    Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 -20", OffsetMax = "170 20" }
                }, "pm_img_frontpanel_7", "pm_img_button_7");
            }           

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.002091496 0.2565822 0.4433962 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "198.2 189", OffsetMax = "230.2 221" }
            }, "Professions_Menu", "pm_close_backpanel");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.03529412 0.3921569 0.6588235 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-14 -14", OffsetMax = "14 14" }
            }, "pm_close_backpanel", "pm_close_frontpanel");

            container.Add(new CuiElement
            {
                Name = "pm_close_text",
                Parent = "pm_close_frontpanel",
                Components = {
                    new CuiTextComponent { Text = "X", Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-14 -14", OffsetMax = "14 14" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = "pmclosemenu" },
                Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-14 -14", OffsetMax = "14 14" }
            }, "pm_close_frontpanel", "pm_close_button");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.002091496 0.2565822 0.4433962 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-232.32 189", OffsetMax = "-200.32 221" }
            }, "Professions_Menu", "pm_quit_backpanel");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.03529412 0.3921569 0.6588235 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-14 -14", OffsetMax = "14 14" }
            }, "pm_quit_backpanel", "pm_quit_frontpanel");

            container.Add(new CuiElement
            {
                Name = "pm_quit_text",
                Parent = "pm_quit_frontpanel",
                Components = {
                    new CuiTextComponent { Text = "Q", Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-14 -14", OffsetMax = "14 14" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = "pmquitmenu" },
                Text = { Text = " ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-14 -14", OffsetMax = "14 14" }
            }, "pm_quit_frontpanel", "pm_quit_button");

            CuiHelper.DestroyUi(player, "Professions_Menu");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("pmclosemenu")]
        void CloseCLMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, "Professions_Menu");
        }

        [ConsoleCommand("pmquitmenu")]
        void SendQuitMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, "Professions_Menu");
            QuitJob(player);
        }

        [ConsoleCommand("sendjobinfo")]
        void SendJobPage(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            var job = arg.Args[0];
            double JobsPercentage = CalculateJobPercentage(job);
            if (config.send_profession_msg) player.ChatMessage($"{Prefix} {string.Format(lang.GetMessage("EmploymentStats", this, player.UserIDString), permission.GetUsersInGroup(job).Length, JobsPercentage, job.TitleCase())}");
            SendJobMenu(player, job);
        }

        #endregion

        #region Cooking API

        object BuffRequiresTimer(BasePlayer player, string name, string shortname, ulong skin)
        {
            if (skin == 2578965785)
            {
                return true;
            }
            return null;
        }

        object GetBuffDescription(ulong skin)
        {
            if (skin == 2578965785) return "Consuming this will provide you with 200 xp in your profession.";
            else return null;
        }

        void RecipeConsumed(BasePlayer player, string name, string shortname, ulong skin, int duration)
        {
            if (skin == 2578965785)
            {
                if (!pcdData.pEntity.TryGetValue(player.userID, out playerData) || playerData.job_type == null)
                {
                    PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("PTC35", this, player.UserIDString), name)}");
                    return;
                }
                if (playerData.job_type == "crafting")
                {                    
                    LevelCrafting(player, 200);
                }
                else if (playerData.job_type == "gathering")
                {
                    UpdateLevel(player, 200, playerData.profession);
                }
                PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("PTC36", this, player.UserIDString), name)}");
            }
        }

        #endregion

        #region Carry bag

        class ResourceBag
        {
            public List<BagInfo> _storage = new List<BagInfo>();
        }

        class BagInfo
        {
            public string shortname;
            public string displayName;
            public ulong skin;
            public int amount;
            public int slot;
        }

        List<StorageContainer> containers = new List<StorageContainer>();

        Dictionary<ulong, Timer> bagCooldownTimer = new Dictionary<ulong, Timer>();

        private void OpenBag(BasePlayer player)
        {
            if (bagCooldownTimer.ContainsKey(player.userID) && bagCooldownTimer[player.userID] != null)
            {
                if (!bagCooldownTimer[player.userID].Destroyed)
                {
                    PrintToChat(player, $"{Prefix} {lang.GetMessage("PTC37", this, player.UserIDString)}");
                    return;
                }
            }
            if (!bagCooldownTimer.ContainsKey(player.userID))
            {
                bagCooldownTimer.Add(player.userID, timer.Once(3f, () =>
                {
                    bagCooldownTimer[player.userID].Destroy();
                }));
            }
            else bagCooldownTimer[player.userID] = timer.Once(3f, () =>
            {
                bagCooldownTimer[player.userID].Destroy();
            });
            player.EndLooting();
            var pos = new Vector3(player.transform.position.x, player.transform.position.y - 1000, player.transform.position.z);
            var storage = GameManager.server.CreateEntity("assets/prefabs/deployable/woodenbox/woodbox_deployed.prefab", pos) as StorageContainer;
            storage.Spawn();

            if (pcdData.resource_bag.TryGetValue(player.userID, out bagData) && bagData._storage.Count > 0)
            {
                foreach (var itemDef in bagData._storage)
                {
                    var item = ItemManager.CreateByName(itemDef.shortname, itemDef.amount, itemDef.skin);
                    item.name = itemDef.displayName;
                    item.MoveToContainer(storage.inventory, itemDef.slot, true, true);
                }
            }

            storage.OwnerID = player.userID;
            containers.Add(storage);

            timer.Once(0.1f, () =>
            {
                if (storage != null) storage.PlayerOpenLoot(player, "", false);
            });
        }

        ResourceBag bagData;
        void OnLootEntityEnd(BasePlayer player, BaseCombatEntity entity)
        {
            var container = entity as StorageContainer;
            if (containers.Contains(container))
            {
                if (!pcdData.resource_bag.TryGetValue(player.userID, out bagData))
                {
                    pcdData.resource_bag.Add(player.userID, new ResourceBag());
                    bagData = pcdData.resource_bag[player.userID];
                }
                if (bagData._storage.Count != 0) bagData._storage.Clear();
                foreach (var item in container.inventory.itemList)
                {
                    var displayName = item.info.displayName.english;
                    if (item.name != null) displayName = item.name;
                    bagData._storage.Add(new BagInfo()
                    {
                        shortname = item.info.shortname,
                        displayName = displayName,
                        amount = item.amount,
                        slot = item.position,
                        skin = item.skin
                    });
                }
                SaveData();
                containers.Remove(container);
                container.Kill();
            }
        }

        object CanMoveItem(Item item, PlayerInventory playerLoot, uint targetContainer, int targetSlot, int amount)
        {
            if (config.allow_resource_bags)
            {
                if (containers.Count > 0)
                {
                    foreach (var container in containers)
                    {
                        if (container.inventory != null && container.inventory.uid.Value == targetContainer)
                        {
                            if (skins.Contains(item.skin))
                            {
                                return null;
                            }
                            var player = item.GetOwnerPlayer();
                            PrintToChat(player, $"{Prefix} {lang.GetMessage("PTC38", this, player.UserIDString)}");
                            return false;
                        }
                    }
                }
            }            

            return null;
        }    
        
        [ChatCommand("giverbag")]
        void GiveResourceBag(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "professions.admin")) return;
            var item = ItemManager.CreateByName("halloween.lootbag.medium", 1, 2582646034);
            item.name = "Resource Bag";
            player.GiveItem(item);
        }

        #endregion

        #region Professions Market

        void SendProfessionsMarket(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0.9607843" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-0.353 -0.328", OffsetMax = "0.348 0.332" }
            }, "Overlay", "Professions_Market");

            container.Add(new CuiElement
            {
                Name = "pinecone_img",
                Parent = "Professions_Market",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = (string)ImageLibrary?.Call("GetImage", "pinecone") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-36 -23.495", OffsetMax = "36 48.505" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "pinecone_price_label",
                Parent = "pinecone_img",
                Components = {
                    new CuiTextComponent { Text = "PRICE", Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -62.7", OffsetMax = "50 -30.7" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "pinecone_price",
                Parent = "pinecone_price_label",
                Components = {
                    new CuiTextComponent { Text = config.item_info["logger"].sell_price.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-16 -48.6", OffsetMax = "16 -16.6" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.5471698 0.5471698 0.5471698 0.5019608" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-38 -112.1", OffsetMax = "-14 -88.1" }
            }, "pinecone_img", "pinecone_bp_sellall_button");

            container.Add(new CuiButton
            {
                Button = { Color = "0.254717 0.254717 0.254717 0", Command = $"sellresource logger all" },
                Text = { Text = "ALL", Font = "robotocondensed-bold.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-10 -10", OffsetMax = "10 10" }
            }, "pinecone_bp_sellall_button", "pinecone_sellall_button");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.5471698 0.5471698 0.5471698 0.5019608" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "14 -112.1", OffsetMax = "38 -88.1" }
            }, "pinecone_img", "pinecone_bp_sell1_button");

            container.Add(new CuiButton
            {
                Button = { Color = "0.254717 0.254717 0.254717 0", Command = $"sellresource logger one" },
                Text = { Text = "1", Font = "robotocondensed-bold.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-10 -10", OffsetMax = "10 10" }
            }, "pinecone_bp_sell1_button", "pinecone_sell1_button");

            container.Add(new CuiElement
            {
                Name = "pinecone_title",
                Parent = "pinecone_img",
                Components = {
                    new CuiTextComponent { Text = config.item_info["logger"].displayName, Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-75.973 43.254", OffsetMax = "75.973 87.346" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "pinecone_img",
                Parent = "Professions_Market",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = (string)ImageLibrary?.Call("GetImage", "goldnugget") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-157.995 -23.495", OffsetMax = "-85.995 48.505" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "goldnugget_price_label",
                Parent = "pinecone_img",
                Components = {
                    new CuiTextComponent { Text = "PRICE", Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -62.7", OffsetMax = "50 -30.7" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "goldnugget_price",
                Parent = "goldnugget_price_label",
                Components = {
                    new CuiTextComponent { Text = config.item_info["miner"].sell_price.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-16 -48.6", OffsetMax = "16 -16.6" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.5471698 0.5471698 0.5471698 0.5019608" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-38 -112.1", OffsetMax = "-14 -88.1" }
            }, "pinecone_img", "goldnugget_bp_sellall_button");

            container.Add(new CuiButton
            {
                Button = { Color = "0.254717 0.254717 0.254717 0", Command = $"sellresource miner all" },
                Text = { Text = "ALL", Font = "robotocondensed-bold.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-10 -10", OffsetMax = "10 10" }
            }, "goldnugget_bp_sellall_button", "goldnugget_sellall_button");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.5471698 0.5471698 0.5471698 0.5019608" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "14 -112.1", OffsetMax = "38 -88.1" }
            }, "pinecone_img", "goldnugget_bp_sell1_button");

            container.Add(new CuiButton
            {
                Button = { Color = "0.254717 0.254717 0.254717 0", Command = $"sellresource miner one" },
                Text = { Text = "1", Font = "robotocondensed-bold.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-10 -10", OffsetMax = "10 10" }
            }, "goldnugget_bp_sell1_button", "goldnugget_sell1_button");

            container.Add(new CuiElement
            {
                Name = "pinecone_title",
                Parent = "pinecone_img",
                Components = {
                    new CuiTextComponent { Text = config.item_info["miner"].displayName, Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-75.973 43.254", OffsetMax = "75.973 87.346" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "primemeat_img",
                Parent = "Professions_Market",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = (string)ImageLibrary?.Call("GetImage", "primemeat") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "85.995 -23.495", OffsetMax = "157.995 48.505" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "primemeat_price_label",
                Parent = "primemeat_img",
                Components = {
                    new CuiTextComponent { Text = "PRICE", Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -62.7", OffsetMax = "50 -30.7" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "primemeat_price",
                Parent = "primemeat_price_label",
                Components = {
                    new CuiTextComponent { Text = config.item_info["skinner"].sell_price.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-16 -48.6", OffsetMax = "16 -16.6" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.5471698 0.5471698 0.5471698 0.5019608" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-38 -112.1", OffsetMax = "-14 -88.1" }
            }, "primemeat_img", "primemeat_bp_sellall_button");

            container.Add(new CuiButton
            {
                Button = { Color = "0.254717 0.254717 0.254717 0", Command = $"sellresource skinner all" },
                Text = { Text = "ALL", Font = "robotocondensed-bold.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-10 -10", OffsetMax = "10 10" }
            }, "primemeat_bp_sellall_button", "primemeat_sellall_button");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.5471698 0.5471698 0.5471698 0.5019608" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "14 -112.1", OffsetMax = "38 -88.1" }
            }, "primemeat_img", "primemeat_bp_sell1_button");

            container.Add(new CuiButton
            {
                Button = { Color = "0.254717 0.254717 0.254717 0", Command = $"sellresource skinner one" },
                Text = { Text = "1", Font = "robotocondensed-bold.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-10 -10", OffsetMax = "10 10" }
            }, "primemeat_bp_sell1_button", "primemeat_sell1_button");

            container.Add(new CuiElement
            {
                Name = "primemeat_title",
                Parent = "primemeat_img",
                Components = {
                    new CuiTextComponent { Text = config.item_info["skinner"].displayName, Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-75.973 43.254", OffsetMax = "75.973 87.346" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Professions_Market_Title",
                Parent = "Professions_Market",
                Components = {
                    new CuiTextComponent { Text = "Resource Exchange", Font = "robotocondensed-bold.ttf", FontSize = 24, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-157.995 125.216", OffsetMax = "157.995 176.784" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.5471698 0.5471698 0.5471698 0.5019608" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-24 -164.7", OffsetMax = "24 -140.7" }
            }, "Professions_Market", "Professions_Market_close_button_panel");

            container.Add(new CuiButton
            {
                Button = { Color = "0.254717 0.254717 0.254717 0", Command = "closeprofessionsmarketmenu" },
                Text = { Text = "CLOSE", Font = "robotocondensed-bold.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-24 -12", OffsetMax = "24 12" }
            }, "Professions_Market_close_button_panel", "Professions_Market_close_button");

            CuiHelper.DestroyUi(player, "Professions_Market");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("closeprofessionsmarketmenu")]
        void CloseMarketMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, "Professions_Market");
        }

        [ConsoleCommand("sellresource")]
        void SellResources(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, "Professions_Market");
            if (!config.item_info.ContainsKey(arg.Args[0])) return;
            var skin = config.item_info[arg.Args[0]].skin;
            if (arg.Args[1] == "one")
            {
                foreach (var item in player.inventory.AllItems())
                {
                    if (item.skin == skin)
                    {
                        item.UseItem(1);
                        if (config.currency.ToLower() == "economics") Economics?.Call("Deposit", player.userID, Convert.ToDouble(config.item_info[arg.Args[0]].sell_price));
                        else if (config.currency.ToLower() == "scrap")
                        {
                            var scrap = ItemManager.CreateByName("scrap", config.item_info[arg.Args[0]].sell_price);
                            player.GiveItem(scrap);
                        }
                        PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("PTC39", this, player.UserIDString), config.item_info[arg.Args[0]].displayName)}");
                    }                        
                }
            }
            else
            {
                var removed = 0;
                foreach (var item in player.inventory.AllItems())
                {
                    if (item.skin == skin)
                    {
                        removed += item.amount;
                        item.UseItem(item.amount);
                    }
                                    
                }
                if (removed > 0)
                {
                    if (config.currency.ToLower() == "economics") Economics?.Call("Deposit", player.userID, Convert.ToDouble(removed * config.item_info[arg.Args[0]].sell_price));
                    else if (config.currency.ToLower() == "scrap")
                    {
                        var scrap = ItemManager.CreateByName("scrap", removed * config.item_info[arg.Args[0]].sell_price);
                        player.GiveItem(scrap);
                    }
                    PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("PTC40", this, player.UserIDString), removed, config.item_info[arg.Args[0]].displayName)}");
                }
                else PrintToChat(player, $"{Prefix} {string.Format(lang.GetMessage("PTC41", this, player.UserIDString), config.item_info[arg.Args[0]].displayName)}");
            }
        }

        #endregion

        #region HumanNPC

        void OnUseNPC(BasePlayer npc, BasePlayer player)
        {
            if (npc.displayName.Equals(config.job_npc_name, StringComparison.OrdinalIgnoreCase) || config.job_npc_ids.Contains(npc.userID))
            {
                CentrelinkMenu(player);
            }
            else if (npc.displayName.Equals(config.market_npc_name, StringComparison.OrdinalIgnoreCase) || config.market_npc_ids.Contains(npc.userID))
            {
                SendProfessionsMarket(player);
            }
        }

        #endregion

        #region Animal Tracking

        Dictionary<ulong, Timer> track_timer = new Dictionary<ulong, Timer>();
        Dictionary<ulong, BaseNetworkable> animal = new Dictionary<ulong, BaseNetworkable>();

        [ChatCommand("track")]
        void TrackAnimal(BasePlayer player)
        {
            if (!config.tracking_enabled || !pcdData.pEntity.TryGetValue(player.userID, out playerData) || playerData.profession != "skinner")
            {
                PrintToChat(player, $"{Prefix} {lang.GetMessage("WarnSkinner", this, player.UserIDString)}");
                return;
            }
            Timer timerData;
            if (track_timer.TryGetValue(player.userID, out timerData) && !timerData.Destroyed)
            {
                PrintToChat(player, $"{Prefix} {lang.GetMessage("TrackWait", this, player.UserIDString)}");
                return;
            }
            else track_timer.Remove(player.userID);
            track_timer.Add(player.userID, timer.Once(config.tracking_delay, () =>
            {
                track_timer.Remove(player.userID);
            }));
            BaseNetworkable animalData;
            if (!animal.TryGetValue(player.userID, out animalData) || animalData == null || animalData.IsDestroyed)
            {
                var Newanimal = BaseEntity.serverEntities.entityList.Where(x => x.Value is BaseAnimalNPC)?.Select(x => x.Value)?.OrderBy(x => Vector3.Distance(x.transform.position, player.transform.position))?.First();
                animal.Remove(player.userID);
                animal.Add(player.userID, animalData = Newanimal);
            }            
            if (animalData == null)
            {
                PrintToChat(player, $"{Prefix} {lang.GetMessage("NoAnimals", this, player.UserIDString)}");
                return;
            }
            var distText = "";
            var distance = Vector3.Distance(player.transform.position, animalData.transform.position);
            if (distance < 50) distText = lang.GetMessage("TrackerClose", this, player.UserIDString);
            else if (distance >= 50 && distance <= 100) distText = lang.GetMessage("TrackerMid", this);
            else if (distance > 100) distText = lang.GetMessage("TrackerFar", this, player.UserIDString);
            var direction = player.transform.position - animalData.transform.position;
            direction.Normalize();
            PrintToChat(player, $"{Prefix} {distText} {Direction(direction.ZX2D())}");
        }

        string Direction(Vector2 dir)
        {
            if (dir.x >= -1.0 && dir.x <= -0.8 && dir.y >= -0.5 && dir.y <= 0.5) return "North";
            if (dir.x >= -1.0 && dir.x <= -0.5 && dir.y >= 0.5 && dir.y <= 1.0) return "North-West";
            if (dir.x >= -0.5 && dir.x <= 0.5 && dir.y >= 0.8 && dir.y <= 1.0) return "West";
            if (dir.x >= 0.5 && dir.x <= 1.0 && dir.y >= 0.5 && dir.y <= 1.0) return "South-West";
            if (dir.x >= -0.5 && dir.x <= 0.5 && dir.y >= -1.0 && dir.y <= -0.8) return "East";
            if (dir.x >= -1.0 && dir.x <= -0.5 && dir.y >= -1.0 && dir.y <= -0.5) return "North-East";
            if (dir.x >= 0.5 && dir.x <= 1.0 && dir.y >= -1.0 && dir.y <= -0.5) return "South-East";
            if (dir.x >= 0.8 && dir.x <= 1.0 && dir.y >= -0.5 && dir.y <= 0.5) return "South";
            return null;
        }


        #endregion

        #region Delete vending machines
        void RemoveVendingMachines()
        {
            var npcv = BaseEntity.serverEntities.entityList.Where(x => x.Value is NPCVendingMachine);
            foreach (var vm in npcv)
            {
                var vending = vm.Value as NPCVendingMachine;
                if (vending.shopName == "Vehicle Parts" || vending.shopName == "Vehicles" || vending.shopName == "Vehicles Extra")
                {
                    Puts($"Deleting: {vending.shopName}");
                    vending.KillMessage();
                }                
            }
        }
        #endregion

        #region BetterChat

        Dictionary<string, string> GetJobTitleInfo(string job)
        {
            foreach (var title in config.titles)
            {
                if (title.Key == job) return new Dictionary<string, string>()
                {
                    {title.Value.title, title.Value.colour }
                };
            }
            return null;
        }

        private object OnBetterChat(Dictionary<string, object> data)
        {
            var player = (IPlayer)data["Player"];

            if (!pcdData.pEntity.TryGetValue(Convert.ToUInt64(player.Id), out playerData) || playerData.profession == default_job) return null;

            var job_title = GetJobTitleInfo(playerData.profession);
            if (job_title == null) return null;

            var title = string.Format("<color={0}>[{1}]</color>", job_title.First().Value, job_title.First().Key);

            var titles = (List<string>)data["Titles"];

            titles.Add(title);
            data["Titles"] = titles;
            return data;
        }

        #endregion
    }
}
