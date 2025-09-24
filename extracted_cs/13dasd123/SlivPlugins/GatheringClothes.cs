// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using Newtonsoft.Json;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;

/* 1.0.2
 * Changed the unsubscribe function to Init (from OSI).
 * Added HeldEntity skin application.
 */

namespace Oxide.Plugins
{
    [Info("GatheringClothes", "imthenewguy", "1.0.3")]
    [Description("Adds gathering clothing sets to the game with spawning options.")]
    class GatheringClothes : RustPlugin
    {
        #region Config       

        private Configuration config;
        public class Configuration
        {
            [JsonProperty("Enable clothing to be dropped from loot containers")]
            public bool enable_loot_drops = true;

            [JsonProperty("Minimum additional scrap to be added if the scavenger perk triggers")]
            public int min_scrap = 1;

            [JsonProperty("Maximum additional scrap to be added if the scavenger perk triggers")]
            public int max_scrap = 3;

            [JsonProperty("Chat commands that a player can use to see their current gather bonuses")]
            public List<string> cmd_bonus_printout = new List<string>() { "bonuses", "bonus" };

            [JsonProperty("Print a chat message whenever a player equips a piece showing them their current bonus")]
            public bool printBonusOnEquip = true;

            [JsonProperty("Black list the skins from SkinBox")]
            public bool item_black_list = true;

            [JsonProperty("Clothing set information")]
            public Dictionary<Bonus, SetInfo> clothing_sets = new Dictionary<Bonus, SetInfo>();

            [JsonProperty("Loot information")]
            public Dictionary<string, float> loot = new Dictionary<string, float>();

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        public class SetInfo
        {
            public Dictionary<ulong, ClothingInfo> clothing_items = new Dictionary<ulong, ClothingInfo>();
            public int set_bonus_required_amount;
            public float set_bonus;
        }

        public class ClothingInfo
        {
            public string displayName;
            public float yield_increase;
            public string shortname;
            public int drop_weight;
            public List<string> drop_containers = new List<string>();
            
            public ClothingInfo(string displayName, float yield_increase, string shortname, int drop_weight, List<string> drop_containers)
            {
                this.displayName = displayName;
                this.yield_increase = yield_increase;
                this.shortname = shortname;
                this.drop_weight = drop_weight;
                this.drop_containers = drop_containers;
            }
        }

        public enum Bonus
        {
            Mining,
            Woodcutting,
            Farming,
            Fishing,
            Hunter,
            Scavenging
        }

        Bonus[] all_bonuses = new Bonus[] { Bonus.Mining, Bonus.Farming, Bonus.Woodcutting, Bonus.Fishing, Bonus.Hunter, Bonus.Scavenging };

        Dictionary<ulong, PlayerInfo> pcdata = new Dictionary<ulong, PlayerInfo>();

        public class PlayerInfo
        {
            public Dictionary<Bonus, PlayerBonusInfo> bonuses = new Dictionary<Bonus, PlayerBonusInfo>();
        }

        public class PlayerBonusInfo
        {
            public int items_worn = 0;
            public float bonus_value = 0f;
            public bool set_bonus_given = false;
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
            config.clothing_sets = new Dictionary<Bonus, SetInfo>()
            {
                [Bonus.Mining] = new SetInfo()
                {
                    clothing_items = new Dictionary<ulong, ClothingInfo>()
                    {
                        [1087075684] = new ClothingInfo("Mining hat", 0.10f, "hat.miner", 100, default_loot_containers),
                        [1220577283] = new ClothingInfo("Mining chest", 0.25f, "hoodie", 100, default_loot_containers),
                        [1220582391] = new ClothingInfo("Mining pants", 0.15f, "pants", 100, default_loot_containers)
                    },
                    set_bonus = 0.50f,
                    set_bonus_required_amount = 3
                },
                [Bonus.Farming] = new SetInfo()
                {
                    clothing_items = new Dictionary<ulong, ClothingInfo>()
                    {
                        [2423620138] = new ClothingInfo("Farmers hat", 0.05f, "hat.boonie", 100, default_loot_containers),
                        [925111592] = new ClothingInfo("Farmers chest", 0.15f, "hoodie", 100, default_loot_containers),
                        [1563936319] = new ClothingInfo("Farmers pants", 0.10f, "pants", 100, default_loot_containers)
                    },
                    set_bonus = 0.30f,
                    set_bonus_required_amount = 3
                },
                [Bonus.Fishing] = new SetInfo()
                {
                    clothing_items = new Dictionary<ulong, ClothingInfo>()
                    {
                        [2558653957] = new ClothingInfo("Anglers hat", 0.20f, "hat.boonie", 100, default_loot_containers),
                        [2558655669] = new ClothingInfo("Anglers chest", 0.50f, "hoodie", 100, default_loot_containers),
                        [2558657371] = new ClothingInfo("Anglers pants", 0.30f, "pants", 100, default_loot_containers)
                    },
                    set_bonus = 1.0f,
                    set_bonus_required_amount = 3
                },
                [Bonus.Hunter] = new SetInfo()
                {
                    clothing_items = new Dictionary<ulong, ClothingInfo>()
                    {
                        [841998387] = new ClothingInfo("Hunters hat", 0.10f, "hat.boonie", 100, default_loot_containers),
                        [852449747] = new ClothingInfo("Hunters chest", 0.25f, "hoodie", 100, default_loot_containers),
                        [582569231] = new ClothingInfo("Hunters pants", 0.15f, "pants", 100, default_loot_containers)
                    },
                    set_bonus = 0.50f,
                    set_bonus_required_amount = 3
                },
                [Bonus.Woodcutting] = new SetInfo()
                {
                    clothing_items = new Dictionary<ulong, ClothingInfo>()
                    {
                        [849682373] = new ClothingInfo("Lumberjacks hat", 0.10f, "hat.beenie", 100, default_loot_containers),
                        [2418623281] = new ClothingInfo("Lumberjacks chest", 0.25f, "hoodie", 100, default_loot_containers),
                        [2418624977] = new ClothingInfo("Lumberjacks pants", 0.15f, "pants", 100, default_loot_containers)
                    },
                    set_bonus = 0.50f,
                    set_bonus_required_amount = 3
                },
                [Bonus.Scavenging] = new SetInfo()
                {
                    clothing_items = new Dictionary<ulong, ClothingInfo>()
                    {
                        [1334831212] = new ClothingInfo("Scavengers hat", 0.05f, "hat.boonie", 100, default_loot_containers),
                        [1356372731] = new ClothingInfo("Scavengers chest", 0.15f, "hoodie", 100, default_loot_containers),
                        [1356379591] = new ClothingInfo("Scavengers pants", 0.10f, "pants", 100, default_loot_containers)
                    },
                    set_bonus = 0.30f,
                    set_bonus_required_amount = 3
                }
            };

            config.loot = new Dictionary<string, float>()
            {
                ["assets/bundled/prefabs/radtown/crate_normal_2.prefab"] = 50f,
                ["assets/bundled/prefabs/radtown/crate_normal.prefab"] = 100f,
                ["assets/bundled/prefabs/radtown/crate_elite.prefab"] = 300f
            };
        }

        List<string> default_loot_containers = new List<string>()
        {
            "assets/bundled/prefabs/radtown/crate_normal_2.prefab",
            "assets/bundled/prefabs/radtown/crate_normal.prefab",
            "assets/bundled/prefabs/radtown/crate_elite.prefab"
        };

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
        }

        protected override void SaveConfig()
        {
            PrintToConsole($"Configuration changes saved to {Name}.json");
            Config.WriteObject(config, true);
        }

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["MorePlayersFound"] = "More than one player found: {0}",
                ["NoMatch"] = "No player was found that matched: {0}",
                ["ReceivedClothes"] = "You received a set of {0} clothes.",
                ["GiveItemUsage"] = "usage: /giveitem <skin ID>",
                ["GiveSetUsage"] = "Usage: /giveset <Optional: player> <bonus type>\n\nAvailable types:\n{0}",
                ["InvalidSet"] = "{0} is not a valid set. Valid sets: {1}",
                ["NotWearingGC"] = "You are not wearing any gathering clothes.",
                ["WCYield"] = "Woodcutting yield: <color=#51ff00>+{0}%</color>",
                ["FishYield"] = "Fishing yield: <color=#51ff00>+{0}%</color>",
                ["HunterYield"] = "Hunting yield: <color=#51ff00>+{0}%</color>",
                ["MiningYield"] = "Mining yield: <color=#51ff00>+{0}%</color>",
                ["FarmingYield"] = "Farming yield: <color=#51ff00>+{0}%</color>",
                ["ScavengerChance"] = "Scavenging chance: <color=#51ff00>+{0}%</color>"
            }, this);
        }

        #endregion

        #region Dictionaries

        [PluginReference]
        private Plugin SkinBox;

        Dictionary<ulong, float> woodcutter_modifier = new Dictionary<ulong, float>();
        Dictionary<ulong, float> miner_modifier = new Dictionary<ulong, float>();
        Dictionary<ulong, float> hunter_modifier = new Dictionary<ulong, float>();
        Dictionary<ulong, float> farmer_modifier = new Dictionary<ulong, float>();
        Dictionary<ulong, float> fisher_modifier = new Dictionary<ulong, float>();
        Dictionary<ulong, float> scavenger_modifier = new Dictionary<ulong, float>();

        Dictionary<ulong, Bonus> item_skins = new Dictionary<ulong, Bonus>();

        #endregion

        #region Hooks

        void Unload()
        {
            foreach (var c in config.cmd_bonus_printout)
            {
                cmd.RemoveChatCommand(c, this);
            }
        }

        const string perm_admin = "gatheringclothes.admin";
        void Init()
        {
            permission.RegisterPermission(perm_admin, this);
            foreach (var sub in subscriptions)
            {
                Unsubscribe(sub.Key);
            }
        }        

        void OnServerInitialized(bool initial)
        {
            foreach (var c in config.cmd_bonus_printout)
            {
                cmd.AddChatCommand(c, this, "PrintOutBonus");
            }            
            foreach (var set in config.clothing_sets)
            {
                foreach (var item in set.Value.clothing_items)
                {
                    if (!item_skins.ContainsKey(item.Key)) item_skins.Add(item.Key, set.Key);
                    foreach (var container in item.Value.drop_containers)
                    {
                        if (!LootInfo.ContainsKey(container)) LootInfo.Add(container, new Dictionary<ulong, ClothingInfo>());
                        if (!LootInfo[container].ContainsKey(item.Key)) LootInfo[container].Add(item.Key, item.Value);
                    }

                }
            }
            if (BasePlayer.activePlayerList.Count > 0)
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    CheckBonuses(player);       
                }
            }
            if (config.item_black_list)
            {
                timer.Once(20f, () =>
                {                    
                    BlackListSkins();
                });
            }
        }
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            RemoveBonuses(player.userID);
        }
        void OnPlayerSleepEnded(BasePlayer player)
        {
            CheckBonuses(player);
        }
        void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            RemoveBonuses(player.userID);
        }

        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            var player = container.GetOwnerPlayer();
            if (player == null || player.IsNpc || !player.userID.IsSteamId()) return;
            
            if (player.inventory.containerWear == container)
            {
                CheckBonuses(player);                
            }
        }

        void BlackListSkins()
        {
            if (SkinBox == null) return;
            Puts("Black listing skins.");
            foreach (var set in config.clothing_sets)
            {
                foreach (var item in set.Value.clothing_items)
                {
                    Server.Command("skinbox.addexcluded", item.Key);
                }
            }
        }

        void OnItemRemovedFromContainer(ItemContainer container, Item item) => OnItemAddedToContainer(container, item);

        void CheckBonuses(BasePlayer player)
        {
            if (!player.IsAlive()) return;
            var items = IsWearingSpecialItems(player);            
            if (items == null || items.Count == 0)
            {
                RemoveBonuses(player.userID);
                return;
            }
            List<Bonus> bonuses = new List<Bonus>();
            PlayerInfo pi;
            if (!pcdata.TryGetValue(player.userID, out pi)) pcdata.Add(player.userID, pi = new PlayerInfo());
            Dictionary<Bonus, PlayerBonusInfo> pbData = new Dictionary<Bonus, PlayerBonusInfo>();
            
            foreach (var bonusItem in items)
            {
                if (!bonuses.Contains(bonusItem.Value)) bonuses.Add(bonusItem.Value);
                PlayerBonusInfo bi;
                if (!pbData.TryGetValue(bonusItem.Value, out bi)) pbData.Add(bonusItem.Value, bi = new PlayerBonusInfo());

                var bonusData = config.clothing_sets[bonusItem.Value].clothing_items[bonusItem.Key];
                var configData = config.clothing_sets[bonusItem.Value];
                bi.bonus_value += bonusData.yield_increase;
                bi.items_worn++;
                if (bi.items_worn == configData.set_bonus_required_amount && !bi.set_bonus_given)
                {
                    bi.bonus_value += configData.set_bonus;
                    bi.set_bonus_given = true;
                }                
            }
            foreach (var key in pi.bonuses.Keys.Except(bonuses).ToList())
            {
                ManageGroups(player.userID, key, 0, false);
            }            
            pi.bonuses = pbData;
            foreach (var key in pi.bonuses)
            {
                ManageGroups(player.userID, key.Key, key.Value.bonus_value, true);
            }
            if (config.printBonusOnEquip) PrintOutBonus(player);
        }
        void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item) => OnDispenserGather(dispenser, player, item);
        void OnDispenserGather(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            float bonus;
            if (dispenser.gatherType == ResourceDispenser.GatherType.Tree && woodcutter_modifier.TryGetValue(player.userID, out bonus)) item.amount += Convert.ToInt32(Math.Round(item.amount * bonus, 0, MidpointRounding.AwayFromZero));
            if (dispenser.gatherType == ResourceDispenser.GatherType.Flesh && hunter_modifier.TryGetValue(player.userID, out bonus)) item.amount += Convert.ToInt32(Math.Round(item.amount * bonus, 0, MidpointRounding.AwayFromZero));
            if (dispenser.gatherType == ResourceDispenser.GatherType.Ore && miner_modifier.TryGetValue(player.userID, out bonus)) item.amount += Convert.ToInt32(Math.Round(item.amount * bonus, 0, MidpointRounding.AwayFromZero));
        }

        void CanCatchFish(BasePlayer player, BaseFishingRod fishingRod, Item item)
        {
            float bonus;
            if (fisher_modifier.TryGetValue(player.userID, out bonus)) item.amount += Convert.ToInt32(Math.Round(item.amount * bonus, 0, MidpointRounding.AwayFromZero));
        }

        void OnGrowableGathered(GrowableEntity plant, Item item, BasePlayer player)
        {
            float bonus;
            if (farmer_modifier.TryGetValue(player.userID, out bonus)) item.amount += Convert.ToInt32(Math.Round(item.amount * bonus, 0, MidpointRounding.AwayFromZero));
        }

        List<LootContainer> looted_crates = new List<LootContainer>();
        void CanLootEntity(BasePlayer player, StorageContainer container)
        {
            if (container?.GetEntity() is LootContainer && !looted_crates.Contains(container as LootContainer))
            {
                looted_crates.Add(container as LootContainer);
                float bonus;
                if (scavenger_modifier.TryGetValue(player.userID, out bonus) && UnityEngine.Random.Range(0f, 1f) < bonus)
                {
                    container.inventory.capacity++;
                    container.inventorySlots++;
                    var scrap = ItemManager.CreateByName("scrap", UnityEngine.Random.Range(config.min_scrap, config.max_scrap + 1));
                    if (!scrap.MoveToContainer(container.inventory)) scrap.Remove();
                }
            }
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (info == null || info.InitiatorPlayer == null || entity == null) return;
            var player = info.InitiatorPlayer;
            if (player.IsNpc || !player.userID.IsSteamId()) return;
            switch (entity.ShortPrefabName)
            {
                case "loot-barrel-1":
                case "loot-barrel-2":
                case "loot_barrel_1":
                case "loot_barrel_2":
                    float bonus;
                    if (scavenger_modifier.TryGetValue(player.userID, out bonus) && UnityEngine.Random.Range(0f, 1f) < bonus)
                    {
                        ItemManager.CreateByName("scrap", UnityEngine.Random.Range(config.min_scrap, config.max_scrap + 1)).DropAndTossUpwards(entity.transform.position);
                    }
                    break;
            }
        }

        void OnEntityKill(BaseNetworkable entity)
        {
            var container = entity as LootContainer;
            if (container == null) return;
            looted_crates.Remove(container);
        }

        #endregion

        #region Methods

        void GiveSet(BasePlayer player, Bonus set)
        {
            SetInfo si;
            if (config.clothing_sets.TryGetValue(set, out si))
            {
                foreach (var clothes in si.clothing_items)
                {
                    var item = ItemManager.CreateByName(clothes.Value.shortname, 1, clothes.Key);
                    if (item == null) continue;
                    ApplySkinToItem(item, clothes.Key);
                    item.name = clothes.Value.displayName;
                    player.GiveItem(item);
                }
                PrintToChat(player, string.Format(lang.GetMessage("ReceivedClothes", this, player.UserIDString), set.ToString()));
            }
        }

        private void ApplySkinToItem(Item item, ulong Skin)
        {
            item.skin = Skin;
            item.MarkDirty();
            BaseEntity heldEntity = item.GetHeldEntity();
            if ((UnityEngine.Object)heldEntity != (UnityEngine.Object)null)
            {
                heldEntity.skinID = Skin;
                heldEntity.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            }
        }

        Dictionary<ulong, Bonus> IsWearingSpecialItems(BasePlayer player)
        {
            if (player.inventory.containerWear.itemList.Count == 0) return null;
            var items = new Dictionary<ulong, Bonus>();
            foreach (var item in player.inventory.containerWear.itemList)
            {
                Bonus bonus;
                if (item_skins.TryGetValue(item.skin, out bonus) && !items.ContainsKey(item.skin)) items.Add(item.skin, bonus);
            }
            return items;
        }

        void RemoveBonuses(ulong id)
        {
            PlayerInfo pi;
            if (pcdata.TryGetValue(id, out pi))
            {
                foreach (var bonus in pi.bonuses)
                {
                    ManageGroups(id, bonus.Key, 0, false);
                }
                pcdata.Remove(id);
            }
        }

        private BasePlayer FindPlayerByName(string Playername, BasePlayer SearchingPlayer = null)
        {
            var lowered = Playername.ToLower();
            var targetList = BasePlayer.allPlayerList.Where(x => x.displayName.ToLower().Contains(lowered)).OrderBy(x => x.displayName.Length);
            if (targetList.Count() == 1)
            {
                return targetList.First();
            }
            if (targetList.Count() > 1)
            {
                if (targetList.First().displayName.Equals(Playername, StringComparison.OrdinalIgnoreCase))
                {
                    return targetList.First();
                }
                if (SearchingPlayer != null)
                {
                    PrintToChat(SearchingPlayer, string.Format(lang.GetMessage("MorePlayersFound", this, SearchingPlayer.UserIDString), String.Join(",", targetList.Select(x => x.displayName))));
                }
                else Puts(string.Format(lang.GetMessage("MorePlayersFound", this), String.Join(",", targetList.Select(x => x.displayName))));
                return null;
            }
            if (targetList.Count() == 0)
            {
                if (SearchingPlayer != null)
                {
                    PrintToChat(SearchingPlayer, string.Format(lang.GetMessage("NoMatch", this, SearchingPlayer.UserIDString), Playername));
                }
                else Puts(string.Format(lang.GetMessage("NoMatch", this), Playername));
                return null;
            }
            return null;
        }

        #endregion

        #region Chat commands

        [ChatCommand("giveitem")]
        void GiveItem(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, perm_admin)) return;
            if (args.Length == 0)
            {
                PrintToChat(player, lang.GetMessage("GiveItemUsage", this, player.UserIDString));
                return;
            }
            var skin = Convert.ToUInt64(args[0]);
            foreach (var cfg in config.clothing_sets)
            {
                ClothingInfo ci;
                if (cfg.Value.clothing_items.TryGetValue(skin, out ci))
                {
                    var item = ItemManager.CreateByName(ci.shortname, 1, skin);
                    ApplySkinToItem(item, skin);
                    item.name = ci.displayName;
                    player.GiveItem(item);
                    return;
                }
            }
            PrintToChat(player, lang.GetMessage("GiveItemUsage", this, player.UserIDString));
        }
        
        [ChatCommand("giveset")]
        void GiveSetCMD(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, perm_admin)) return;
            if (args.Length == 0)
            {
                PrintToChat(player, string.Format(lang.GetMessage("GiveSetUsage", this, player.UserIDString), string.Join(", ", all_bonuses.Select(x => x.ToString()).ToArray())));
                return;
            }
            var set = (Bonus)Enum.Parse(typeof(Bonus), args.Last(), true);
            if (!config.clothing_sets.ContainsKey(set))
            {
                PrintToChat(player, string.Format(lang.GetMessage("InvalidSet", this, player.UserIDString), args[0], string.Join(", ", all_bonuses.Select(x => x.ToString()).ToArray())));
                return;
            }
            if (args.Length > 1)
            {
                var target = FindPlayerByName(String.Join(" ", args.Take(args.Length - 1)), player);
                GiveSet(target, set);
            }
            else GiveSet(player, set);
        }

        void PrintOutBonus(BasePlayer player)
        {
            var title = "<color=#ffec00>Bonuses:</color>\n";
            var s = "";
            if (woodcutter_modifier.ContainsKey(player.userID)) s = $"{s}- {string.Format(lang.GetMessage("WCYield", this, player.UserIDString), woodcutter_modifier[player.userID] * 100)}\n";
            if (fisher_modifier.ContainsKey(player.userID)) s = $"{s}- {string.Format(lang.GetMessage("FishYield", this, player.UserIDString), fisher_modifier[player.userID] * 100)}\n";
            if (hunter_modifier.ContainsKey(player.userID)) s = $"{s}- {string.Format(lang.GetMessage("HunterYield", this, player.UserIDString), hunter_modifier[player.userID] * 100)}\n";
            if (miner_modifier.ContainsKey(player.userID)) s = $"{s}- {string.Format(lang.GetMessage("MiningYield", this, player.UserIDString), miner_modifier[player.userID] * 100)}\n";
            if (farmer_modifier.ContainsKey(player.userID)) s = $"{s}- {string.Format(lang.GetMessage("FarmingYield", this, player.UserIDString), farmer_modifier[player.userID] * 100)}\n";
            if (scavenger_modifier.ContainsKey(player.userID)) s = $"{s}- {string.Format(lang.GetMessage("ScavengerChance", this, player.UserIDString), scavenger_modifier[player.userID] * 100)}\n";
            if (string.IsNullOrEmpty(s))
            {
                PrintToChat(player, lang.GetMessage("NotWearingGC", this, player.UserIDString));
                return;
            }
            PrintToChat(player, $"{title}{s}");
        }

        #endregion

        #region Subscriptions

        Dictionary<string, List<Bonus>> subscriptions = new Dictionary<string, List<Bonus>>()
        {
            ["OnDispenserBonus"] = new List<Bonus>() 
            {
                Bonus.Hunter,
                Bonus.Mining,
                Bonus.Woodcutting
            },
            ["OnDispenserGather"] = new List<Bonus>()
            {
                Bonus.Hunter,
                Bonus.Mining,
                Bonus.Woodcutting
            },
            ["OnGrowableGather"] = new List<Bonus>() { Bonus.Farming },
            ["CanCatchFish"] = new List<Bonus>() { Bonus.Fishing }
        };

        void ManageGroups(ulong id, Bonus bonus, float value = 0, bool adding = true)
        {
            switch (bonus) 
            {
                case Bonus.Farming:
                    if (!adding) farmer_modifier.Remove(id);
                    else
                    {
                        if (!farmer_modifier.ContainsKey(id)) farmer_modifier.Add(id, value);
                        else farmer_modifier[id] = value;
                    }
                    break;
                case Bonus.Fishing:
                    if (!adding) fisher_modifier.Remove(id);
                    else
                    {
                        if (!fisher_modifier.ContainsKey(id)) fisher_modifier.Add(id, value);
                        else fisher_modifier[id] = value;
                    }
                    break;
                case Bonus.Hunter:
                    if (!adding) hunter_modifier.Remove(id);
                    else
                    {
                        if (!hunter_modifier.ContainsKey(id)) hunter_modifier.Add(id, value);
                        else hunter_modifier[id] = value;
                    }
                    break;
                case Bonus.Mining:
                    if (!adding) miner_modifier.Remove(id);
                    else
                    {
                        if (!miner_modifier.ContainsKey(id)) miner_modifier.Add(id, value);
                        else miner_modifier[id] = value;
                    }
                    break;
                case Bonus.Woodcutting:
                    if (!adding) woodcutter_modifier.Remove(id);
                    else
                    {
                        if (!woodcutter_modifier.ContainsKey(id)) woodcutter_modifier.Add(id, value);
                        else woodcutter_modifier[id] = value;
                    }
                    break;
                case Bonus.Scavenging:
                    if (!adding) scavenger_modifier.Remove(id);
                    else
                    {
                        if (!scavenger_modifier.ContainsKey(id)) scavenger_modifier.Add(id, value);
                        else scavenger_modifier[id] = value;
                    }
                    break;
            }
            ManageSubscription(bonus);
        }

        void ManageSubscription(Bonus bonus)
        {
            foreach (var sub in subscriptions)
            {
                if (sub.Value.Contains(bonus))
                {
                    if (KeepSubscribed(sub.Key)) Subscribe(sub.Key);
                    else Unsubscribe(sub.Key);
                }
            }
        }

        bool KeepSubscribed(string hook)
        {
            switch (hook)
            {
                case "OnDispenserBonus":
                case "OnDispenserGather":
                    if (woodcutter_modifier.Count > 0 || miner_modifier.Count > 0 || hunter_modifier.Count > 0) return true;
                    else return false;
                case "OnGrowableGather":
                    if (farmer_modifier.Count > 0) return true;
                    else return false;
                case "CanCatchFish":
                    if (fisher_modifier.Count > 0) return true;
                    else return false;
                case "OnEntityDeath":
                case "CanLootEntity":
                    if (scavenger_modifier.Count > 0) return true;
                    else return false;
                
            }
            return true;
        }

        #endregion

        #region Loot
        Dictionary<string, Dictionary<ulong, ClothingInfo>> LootInfo = new Dictionary<string, Dictionary<ulong, ClothingInfo>>();
        void RollLoot(LootContainer container)
        {
            if (container.inventorySlots < 12)
            {
                container.inventory.capacity++;
                container.inventorySlots++;
            }
            float target_chance;
            if (config.loot.TryGetValue(container.PrefabName, out target_chance))
            {
                var chance = UnityEngine.Random.Range(0, 1001);
                if (chance < 1000 - target_chance) return;

                Dictionary<ulong, ClothingInfo> clothes;
                if (!LootInfo.TryGetValue(container.PrefabName, out clothes)) return;
                
                var rollMax = 0;
                foreach (var c in clothes)
                {
                    rollMax += c.Value.drop_weight;
                }
                var roll = UnityEngine.Random.Range(0, rollMax + 1);
                rollMax = 0;
                foreach (var c in clothes)
                {
                    if (c.Value.drop_weight == 0) continue;
                    rollMax += c.Value.drop_weight;
                    if (roll > rollMax) continue;
                    else
                    {
                        var item = ItemManager.CreateByName(c.Value.shortname, 1, c.Key);
                        if (item == null) return;
                        ApplySkinToItem(item, c.Key);
                        item.name = c.Value.displayName;
                        item.MoveToContainer(container.inventory);
                        return;
                    }
                }
            }
        }

        void OnEntitySpawned(LootContainer container)
        {
            RollLoot(container);
        }

        #endregion
    }
}
