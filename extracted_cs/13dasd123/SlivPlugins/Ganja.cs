// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

/**
 * VERSION HISTORY
 * 
 * V 1.0.7
 * - fixed gahter permission being ignored for collectable hemp
 * - added smoke effect
 * - color effects rework
 * 
 * V 1.0.8
 * - fixed gathering not working
 * - improved ui, image library not needed anymore
 * - added weed skin ids to config
 * - added custom item support for loottable
 * 
 * V 1.0.9
 * - fixed drop chance when collecting hemp
 * 
 * V 2.0.0
 * - full rewrite
 * 
 * V 2.0.1
 * - add support for deployable nature
 * - brought back gene support
 * - amount of produced items can now be configured
 * 
 * V 2.0.2
 * - fix hook error with deployable nature
 * - modify internal crafting system
 * - change joint break effect
 * - add option to disable gathering from growable hemp
 * - add page navigation to crafting ui
 * 
 * V 2.0.3
 * - added the ability to extinguish joints
 * - joint burn time and loss per hit can now be configured
 * 
 * V 2.0.4
 * - apply stacking logic only to joints and weed
 * - prevent joints from being repaired
 * - burn time can now be configured per joint
 * - add support for AutoFarm
 * 
 * V 2.0.5
 * - display numbers with up to 4 digits
 * - drop overflow items when inventory is full
 * - fix bug when crafting a recipe with 5 ingredients
 * - adjust ui spacing
 * - support for PlanterboxDefender
 * 
 **/

namespace Oxide.Plugins
{
    [Info(nameof(Ganja), "The_Kiiiing", "2.0.5")]
    public class Ganja : RustPlugin
    {
        #region Fields

        #region Constants

        private const float JOINT_USE_COOLDOWN = 2f;

        private const string PERM_GATHER = "ganja.gather";
        private const string PERM_CRAFT = "ganja.craft";

        private const string CMD_TOGGLE_UI = "ganja.toggleui";
        private const string CMD_CRAFT = "ganja.craft";
        private const string CMD_CRAFT_PAGE = "ganja.craft.page";

        private const string SHAKE_EFFECT = "assets/bundled/prefabs/fx/screen_land.prefab";
        private const string SHAKE2_EFECT = "assets/bundled/prefabs/fx/takedamage_generic.prefab";
        private const string VOMIT_EFFECT = "assets/bundled/prefabs/fx/gestures/drink_vomit.prefab";
        private const string LICK_EFFECT = "assets/bundled/prefabs/fx/gestures/lick.prefab";
        private const string BREATHE_EFFECT = "assets/prefabs/npc/bear/sound/breathe.prefab";
        private const string SMOKE_EFFECT = "assets/bundled/prefabs/fx/door/barricade_spawn.prefab";

        #endregion

        [PluginReference]
        private Plugin CustomSkinsStacksFix, StackModifier, Loottable, DeployableNature, PlanterboxDefender;

        private static Ganja _instance;

        private readonly Dictionary<ulong, float> lastUsed = new Dictionary<ulong, float>();
        private readonly Dictionary<Item, Timer> jointTimers = new Dictionary<Item, Timer>();

        private readonly List<ulong> openUis = new List<ulong>();

        #endregion

        #region Configuration

        private static Configuration _config;

        private class Configuration
        {
            [JsonProperty("Weed configuration", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<WeedConfig> weedConfig = new List<WeedConfig>
            {
                new WeedConfig
                {
                    shortName = "sticks",
                    skinId = 2661029427,
                    displayName = "Low Quality Weed",
                    dropChance = 0.4f,
                    dropAmount = new MinMaxInt(1, 3),
                    biomeMask = 6,
                    minHGenesChance = 1,
                    minHGenesGuaranteed = 3,
                    disableCollGathering = false
                },
                new WeedConfig
                {
                    shortName = "sticks",
                    skinId = 2661031542,
                    displayName = "Medium Quality Weed",
                    dropChance = 0.3f,
                    dropAmount = new MinMaxInt(1, 3),
                    biomeMask = 1,
                    minHGenesChance = 1,
                    minHGenesGuaranteed = 3,
                    disableCollGathering = false
                },
                new WeedConfig
                {
                    shortName = "sticks",
                    skinId = 2660588149,
                    displayName = "High Quality Weed",
                    dropChance = 0.1f,
                    dropAmount = new MinMaxInt(1, 2),
                    biomeMask = 8,
                    minHGenesChance = 1,
                    minHGenesGuaranteed = 3,
                    disableCollGathering = false
                }
            };

#if COCA
            [JsonProperty("Berry configuration", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<WeedConfig> berryConfig = new List<WeedConfig>
            {
                new WeedConfig
                {
                    shortName = "sticks",
                    skinId = 2946165766,
                    displayName = "Coca Leaves",
                    dropChance = 0.8f,
                    dropAmount = new MinMaxInt(1, 3),
                    biomeMask = 6,
                    minHGenesChance = 1,
                    minHGenesGuaranteed = 3,
                    disableCollGathering = false,
                    disableGrowableGathering = false
                }
            };
#endif

            [JsonProperty("Crafting Recipes", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<CustomRecipe> recipes = new List<CustomRecipe>
            {
                new CustomRecipe
                {
                    ingredientSlots = new Dictionary<int, Ingredient>
                    {
                        [0] = new Ingredient
                        {
                            shortName = "note",
                            skinId = 0,
                            amount = 1
                        },
                        [1] = new Ingredient
                        {
                            shortName = "sticks",
                            skinId = 2661029427,
                            amount = 1
                        },
                        [2] = new Ingredient
                        {
                            shortName = "sticks",
                            skinId = 2661029427,
                            amount = 1
                        }
                    },

                    producedItem = new ProducedItem
                    {
                        shortName = "horse.shoes.basic",
                        skinId = 2894101592,
                        displayName = "Low Quality Joint",
                        amount = 1
                    },
                    
                    isJoint = true,
                    boosts = new BoostCollection
                    {
                        woodPercentage = 0.4f,
                        woodDuration = 20f,
                        orePercentage = 0f,
                        oreDuration = 0f,
                        scrapPercentage = 0f,
                        scrapDuration = 0f,
                        maxHealthPercentage = 0f,
                        maxHealthDuration = 0f,
                        healingPerUse = 1f
                    }
                },

                new CustomRecipe
                {
                    ingredientSlots = new Dictionary<int, Ingredient>
                    {
                        [0] = new Ingredient
                        {
                            shortName = "note",
                            skinId = 0,
                            amount = 1
                        },
                        [1] = new Ingredient
                        {
                            shortName = "sticks",
                            skinId = 2661031542,
                            amount = 1
                        },
                        [2] = new Ingredient
                        {
                            shortName = "sticks",
                            skinId = 2661031542,
                            amount = 1
                        }
                    },

                    producedItem = new ProducedItem
                    {
                        shortName = "horse.shoes.basic",
                        skinId = 2894101290,
                        displayName = "Medium Quality Joint",
                        amount = 1
                    },

                    isJoint = true,
                    boosts = new BoostCollection
                    {
                        woodPercentage = 0f,
                        woodDuration = 0f,
                        orePercentage = 0.8f,
                        oreDuration = 20f,
                        scrapPercentage = 0f,
                        scrapDuration = 0f,
                        maxHealthPercentage = 0f,
                        maxHealthDuration = 0f,
                        healingPerUse = 4f
                    }
                },

                new CustomRecipe
                {
                    ingredientSlots = new Dictionary<int, Ingredient>
                    {
                        [0] = new Ingredient
                        {
                            shortName = "note",
                            skinId = 0,
                            amount = 1
                        },
                        [1] = new Ingredient
                        {
                            shortName = "sticks",
                            skinId = 2660588149,
                            amount = 1
                        },
                        [2] = new Ingredient
                        {
                            shortName = "sticks",
                            skinId = 2660588149,
                            amount = 1
                        }
                    },

                    producedItem = new ProducedItem
                    {
                        shortName = "horse.shoes.basic",
                        skinId = 2893700325,
                        displayName = "High Quality Joint",
                        amount = 1
                    },

                    isJoint = true,
                    boosts = new BoostCollection
                    {
                        woodPercentage = 0f,
                        woodDuration = 0f,
                        orePercentage = 0f,
                        oreDuration = 0f,
                        scrapPercentage = 1f,
                        scrapDuration = 30f,
                        maxHealthPercentage = 0.3f,
                        maxHealthDuration = 30f,
                        healingPerUse = 8f
                    }
                },
            };

            [JsonProperty("Require permission for crafting")]
            public bool enableCraftPerm = true;

            [JsonProperty("Require permission for gathering")]
            public bool enableGatherPerm = true;

            [JsonProperty("Disable built-in stack fix (set to true if you have problems with item stacking/splitting)")]
            public bool disableStackFix = false;

            [JsonProperty("Automatically extinguish joint when unequiping it")]
            public bool extinguishOnUnequip = true;

            [JsonIgnore]
            private HashSet<ulong> jointSkins;

            [JsonIgnore]
            private HashSet<ulong> weedSkins;

            public CustomRecipe GetJointRecipe(Item joint)
            {
                foreach (var recipe in recipes)
                {
                    if (!recipe.isJoint)
                    {
                        continue;
                    }

                    if (recipe.producedItem.skinId == joint.skin &&
                        recipe.producedItem.shortName == joint.info.shortname)
                    {
                        return recipe;
                    }
                }

                return null;
            }

            public bool IsJointSkin(ulong skin)
            {
                if (jointSkins == null)
                {
                    jointSkins = new HashSet<ulong>(recipes.Where(x => x.isJoint).Select(x => x.producedItem.skinId));
                }

                return jointSkins.Contains(skin);
            }

            public bool IsWeedSkin(ulong skin)
            {
                if (weedSkins == null)
                {
                    weedSkins = new HashSet<ulong>(weedConfig.Select(x => x.skinId));
                }

                return weedSkins.Contains(skin);
            }

        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();
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

        #region Config Classes

        private class CustomRecipe
        {
            [JsonProperty("Ingredient Slots")]
            public Dictionary<int, Ingredient> ingredientSlots;

            [JsonProperty("Produced Item")]
            public ProducedItem producedItem;

            [JsonProperty("Is joint")]
            public bool isJoint;
            [JsonProperty("Boosts (only works for joints)")]
            public BoostCollection boosts;

            /// <summary>
            /// Attempt to craft the current recipe
            /// </summary>
            /// <param name="overflow">Contains excess items when crafting was successful</param>
            /// <returns>true when crafting was successful</returns>
            public bool TryCraft(MixingTable mixingTable, List<Item> overflow)
            {
                if (ingredientSlots.Count < 1)
                {
                    _instance.PrintError("Invalid Recipe! Recipe needs to have at least 1 ingredient");
                    return false;
                }

                List<Item> collect = Pool.GetList<Item>();
                List<int> collectAmount = new List<int>();

                // Check if mixing table contains correct ingredients
                Ingredient ing;
                for (int slot = 0; slot < mixingTable.inventory.capacity; slot++)
                {
                    try
                    {
                        ing = ingredientSlots[slot];
                    }
                    catch (KeyNotFoundException)
                    {
                        continue;
                    }

                    Item item = mixingTable.inventory.GetSlot(slot);

                    if (item == null || 
                        item.info.shortname != ing.shortName || 
                        item.skin != ing.skinId ||  
                        item.amount < ing.amount)
                    {
                        Pool.FreeList(ref collect);
                        return false;
                    }

                    collect.Add(item);
                    collectAmount.Add(ing.amount);

                    ing = null;
                }

                // Collect ingredients
                for (int i = 0; i < collect.Count; i++)
                {
                    var item = collect[i];
                    int amount = collectAmount[i];

                    if (item.amount == amount)
                    {
                        item.Remove();
                    }
                    else
                    {
                        item.amount -= amount;
                        item.RemoveFromContainer();
                        overflow.Add(item);
                    }
                }

                // Free resources
                Pool.FreeList(ref collect);
                ItemManager.DoRemoves();

                // Create new item
                Item result = producedItem.CreateItem();
                if (result == null)
                {
                    _instance.PrintError($"Crafting failed: failed to create item {producedItem.shortName}[{producedItem.skinId}] x{producedItem.amount} - make sure the item you specified in the config exists");
                    return false;
                }

                if (!result.MoveToContainer(mixingTable.inventory))
                {
                    _instance.PrintError("Crafting failed: failed to move item");
                }

                return true;
            }
        }

        private class WeedConfig : CustomItem
        {
            [JsonProperty("Drop chance when harvesting (1 = 100%)")]
            public float dropChance;
            [JsonProperty("Drop amount when harvesting")]
            public MinMaxInt dropAmount;
            [JsonProperty("Biome mask (see description for details)")]
            public ushort biomeMask;

            [JsonProperty("Minimum amount of H genes for a chance to yield weed")]
            public int minHGenesChance;
            [JsonProperty("Minimum amount of H genes for guaranteed yield")]
            public int minHGenesGuaranteed;

            [JsonProperty("Disable gathering from collectable hemp")]
            public bool disableCollGathering;
            [JsonProperty("Disable gathering from growable hemp")]
            public bool disableGrowableGathering;

            public bool IsValidGatherLocation(ushort locationMask)
            {
                //BitArray maskBits = new BitArray(BitConverter.GetBytes(biomeMask));
                //BitArray locationBits = new BitArray(BitConverter.GetBytes(locationMask));

                //return maskBits.And(locationBits).Cast<bool>().Contains(true);

                return (biomeMask & locationMask) > 0;
            }

            public bool MaybeGather(ushort locationMask, bool isCollectable, int hGenes, out Item weed)
            {
                weed = null;

                if (!IsValidGatherLocation(locationMask) || (isCollectable && disableCollGathering) || (!isCollectable && disableGrowableGathering))
                {
                    return false;
                }

                float chance = dropChance;

                if (!isCollectable)
                {
                    if (hGenes < minHGenesChance)
                    {
                        // Not enough h genes
                        return false;
                    }
                    if (hGenes >= minHGenesGuaranteed)
                    {
                        // enough h genes for 100% drop chance
                        chance = 1f;
                    }
                }

                if (Random.Range(0f, 1f) > chance)
                {
                    return false;
                }

                int amount = dropAmount.Random();
                if (amount > 0)
                {
                    weed = CreateItem(amount);
                    if (weed == null)
                    {
                        _instance.PrintError("Failed to create weed item, check if your config is correct");
                    }
                }
                
                return weed != null;
            }
        }

        private class BoostCollection
        {
            [JsonProperty("Wood boost percentage (1 = 100%)")]
            public float woodPercentage;
            [JsonProperty("Wood boost duration (seconds)")]
            public float woodDuration;

            [JsonProperty("Ore boost percentage (1 = 100%)")]
            public float orePercentage;
            [JsonProperty("Ore boost duration (seconds)")]
            public float oreDuration;

            [JsonProperty("Scrap boost percentage (1 = 100%)")]
            public float scrapPercentage;
            [JsonProperty("Scrap boost duration (seconds)")]
            public float scrapDuration;

            [JsonProperty("Max Health percentage (1 = 100%)")]
            public float maxHealthPercentage;
            [JsonProperty("Max Health duration (seconds)")]
            public float maxHealthDuration;

            [JsonProperty("Healing per use")]
            public float healingPerUse;

            [JsonProperty("Joint durability (seconds)")]
            public float jointDurability = 120f;
            [JsonProperty("Joint durability loss per hit (seconds)")]
            public float jointDurabilityLossPerHit = 10f;
            [JsonIgnore]
            public float JointDurabilityLossPerSecond => 100f / jointDurability;

            public void ApplyToPlayer(BasePlayer player)
            {
                List<ModifierDefintion> mods = Pool.GetList<ModifierDefintion>();

                if (scrapDuration > 0 && scrapPercentage > 0)
                {
                    mods.Add(new ModifierDefintion
                    {
                        source = Modifier.ModifierSource.Tea,
                        type = Modifier.ModifierType.Scrap_Yield,
                        duration = scrapDuration,
                        value = scrapPercentage
                    });
                }

                if (woodDuration > 0 && woodPercentage > 0)
                {
                    mods.Add(new ModifierDefintion
                    {
                        source = Modifier.ModifierSource.Tea,
                        type = Modifier.ModifierType.Wood_Yield,
                        duration = woodDuration,
                        value = woodPercentage
                    });
                }

                if (oreDuration > 0 && orePercentage > 0)
                {
                    mods.Add(new ModifierDefintion
                    {
                        source = Modifier.ModifierSource.Tea,
                        type = Modifier.ModifierType.Ore_Yield,
                        duration = oreDuration,
                        value = orePercentage
                    });
                }

                if (maxHealthDuration > 0 && maxHealthPercentage > 0)
                {
                    mods.Add(new ModifierDefintion
                    {
                        source = Modifier.ModifierSource.Tea,
                        type = Modifier.ModifierType.Max_Health,
                        duration = maxHealthDuration,
                        value = maxHealthPercentage
                    });
                }

                player.modifiers.Add(mods);
                player.Heal(healingPerUse);
                Pool.FreeList(ref mods);
            }

            public override string ToString()
            {
                return $"wood {woodPercentage:N2} {woodDuration}s; scrap  {scrapPercentage:N2} {scrapDuration}s; ore {orePercentage:N2} {oreDuration}s; maxHealth {maxHealthPercentage:N2} {maxHealthDuration}s; heal {healingPerUse}";
            }
        }

        private class Ingredient : SkinnedItem
        {
            [JsonProperty("Amount")]
            public int amount;
        }

        private class ProducedItem : CustomItem
        {
            [JsonProperty("Amount")]
            public int amount = 1;

            public Item CreateItem()
            {
                return base.CreateItem(amount);
            }
        }

        private class CustomItem : SkinnedItem
        {
            [JsonProperty("Custom item name (null = default name)")]
            public string displayName;

            [JsonIgnore]
            public string UiDisplayName => GetDisplayName(false);

            public string GetDisplayName(bool nullIfNotCustom)
            {
                if (displayName == null || displayName == string.Empty)
                {
                    if (nullIfNotCustom)
                    {
                        return null;
                    }
                    else
                    {
                        return ItemDefinition.displayName.english;
                    }
                }

                return displayName;
            }

            public override Item CreateItem(int amount)
            {
                var itm = ItemManager.Create(ItemDefinition, amount, skinId);
                string name = GetDisplayName(true);
                if (itm != null && name != null)
                {
                    itm.name = name;
                }

                return itm;
            }
        }

        private class SkinnedItem
        {
            [JsonProperty("Item short name")]
            public string shortName;
            [JsonProperty("Item skin id")]
            public ulong skinId;

            [JsonIgnore]
            public ItemDefinition ItemDefinition => ItemManager.FindItemDefinition(shortName);

            public virtual Item CreateItem(int amount)
            {
                return ItemManager.Create(ItemDefinition, amount, skinId);
            }
        }

        private struct MinMaxInt
        {
            public int min;
            public int max;

            public MinMaxInt(int min, int max)
            {
                this.min = min;
                this.max = max;
            }

            public int Random()
            {
                return UnityEngine.Random.Range(min, max + 1);
            }
        }

        #endregion

        #region Data

        private class PlayerData
        {
            [JsonProperty]
            private Dictionary<ulong, bool> craftingUiState = new Dictionary<ulong, bool>();

            private static PlayerData _ins;

            public static void Load()
            {
                _ins = Interface.Oxide.DataFileSystem.ReadObject<PlayerData>(_instance.Name);
            }

            public static void Save()
            {
                if (_ins != null)
                {
                    Interface.Oxide.DataFileSystem.WriteObject(_instance.Name, _ins);
                }
            }

            public static bool IsCraftingUiOpen(BasePlayer player)
            {
                if (!_ins.craftingUiState.ContainsKey(player.userID))
                {
                    _ins.craftingUiState[player.userID] = false;
                    return false;
                }

                return _ins.craftingUiState[player.userID];
            }

            public static bool ToggleCraftingUi(BasePlayer player)
            {
                if (!_ins.craftingUiState.ContainsKey(player.userID))
                {
                    _ins.craftingUiState[player.userID] = true;
                    return true;
                }

                return _ins.craftingUiState[player.userID] = !_ins.craftingUiState[player.userID];
            }
        }

        #endregion

        #region Helpers

        private bool DisableStackFix()
        {
            if (_config.disableStackFix)
            {
                return true;
            }

            return StackModifier != null || Loottable != null || CustomSkinsStacksFix != null;
        }

        private bool AllowedToCraft(BasePlayer player)
        {
            bool permEnabled = _config.enableCraftPerm;
            bool hasPermission = permission.UserHasPermission(player.UserIDString, PERM_CRAFT);
            bool result = !permEnabled || hasPermission;

            return result;
        }

        private bool AllowedToGather(BasePlayer player) => AllowedToGather(player.UserIDString);
        private bool AllowedToGather(string playerId)
        {
            bool permEnabled = _config.enableGatherPerm;
            bool hasPermission = permission.UserHasPermission(playerId, PERM_GATHER);
            bool result = !permEnabled || hasPermission;

            return result;
        }

        private void SendNote(BasePlayer player, Item item, int amount = 0)
        {
            player.Command("note.inv", item.info.itemid, amount == 0 ? item.amount.ToString() : amount.ToString(), item.name);
        }

        private bool IsDeployableNature(BaseEntity entity)
        {
            if (DeployableNature == null)
            {
                return false;
            }

            var b = DeployableNature.Call("STCanGainXP", null, entity) as Boolean?;
            if (b == false)
            {
                return true;
            }
            
            return false;
        }

        #endregion

        #region Functions

        private bool IsJointOrWeed(Item item)
        {
            if (item == null)
            {
                return false;
            }

            return _config.IsWeedSkin(item.skin) || _config.IsJointSkin(item.skin);
        }

        private bool IsJoint(Item item)
        {
            if (item == null)
            {
                return false;
            }

            return _config.IsJointSkin(item.skin);
        }

        private void UseJoint(BasePlayer player, Item joint)
        {
            if (!joint.HasFlag(global::Item.Flag.OnFire) || joint.isBroken)
            {
                return;
            }

            var def = _config.GetJointRecipe(joint);
            if (def == null)
            {
                _instance.PrintError($"Failed to find joint definition for joint with skin id '{joint.skin}'");
                return;
            }

            def.boosts.ApplyToPlayer(player);
            RunEffects(player, 2.5f);
            JointLoseCondition(joint, def.boosts.JointDurabilityLossPerSecond * def.boosts.jointDurabilityLossPerHit);
        }

        private void IgniteJoint(BasePlayer player, Item joint)
        {
            if (joint.HasFlag(global::Item.Flag.OnFire))
            {
                return;
            }

            var def = _config.GetJointRecipe(joint);
            if (def == null)
            {
                _instance.PrintError($"Failed to find joint definition for joint with skin id '{joint.skin}'");
                return;
            }

            joint.SetFlag(global::Item.Flag.OnFire, true);

            RunEffect("assets/prefabs/weapons/torch/effects/ignite.prefab", player);

            // Start decay timer
            jointTimers[joint] = timer.Every(1, () => JointLoseCondition(joint, def.boosts.JointDurabilityLossPerSecond));
        }

        private void ExtinguishJoint(BasePlayer player, Item joint)
        {
            if (!joint.HasFlag(global::Item.Flag.OnFire))
            {
                return;
            }

            joint.SetFlag(global::Item.Flag.OnFire, false);
            joint.MarkDirty();

            if (player != null)
            {
                RunEffect("assets/prefabs/weapons/torch/effects/extinguish.prefab", player);
            }

            if (jointTimers.ContainsKey(joint))
            {
                jointTimers[joint].Destroy();
                jointTimers.Remove(joint);
            }
        }

        private void JointLoseCondition(Item joint, float loss)
        {
            if (joint.condition - loss < 1f)
            {
                jointTimers[joint].Destroy();
                jointTimers.Remove(joint);
                joint.Remove();

                var player = joint.GetOwnerPlayer();
                if (player != null)
                {
                    RunEffect("assets/bundled/prefabs/fx/impacts/additive/fire.prefab", player);
                }
            }
            else
            {
                joint.condition -= loss;
            }
        }

        private void OnHempGather(BasePlayer player, Vector3 plantPositon, bool isCollectable, int hGenes = 0)
        {
            ushort locationMask = (ushort)TerrainMeta.BiomeMap.GetBiomeMaxType(plantPositon);

            foreach(var cfg in _config.weedConfig)
            {
                if (cfg.MaybeGather(locationMask, isCollectable, hGenes, out var weed))
                {
                    if (player.inventory.containerMain.IsFull() &&
                        player.inventory.containerBelt.IsFull())
                    {
                        weed.Drop(plantPositon, Vector3.up * 3f);
                        SendNote(player, weed);
                        SendNote(player, weed, -weed.amount);
                    }
                    else
                    {
                        int amt = weed.amount;
                        player.inventory.GiveItem(weed);
                        SendNote(player, weed, amt);
                    }
                }
            }
        }

#if COCA
        private void OnBerryGather(BasePlayer player, Vector3 plantPositon, bool isCollectable, int hGenes = 0)
        {
            ushort locationMask = (ushort)TerrainMeta.BiomeMap.GetBiomeMaxType(plantPositon);

            foreach (var cfg in _config.berryConfig)
            {
                if (cfg.MaybeGather(locationMask, isCollectable, hGenes, out var coca))
                {
                    if (player.inventory.containerMain.IsFull() &&
                        player.inventory.containerBelt.IsFull())
                    {
                        coca.Drop(plantPositon, Vector3.up * 3f);
                        SendNote(player, coca);
                        SendNote(player, coca, -coca.amount);
                    }
                    else
                    {
                        int amt = coca.amount;
                        player.inventory.GiveItem(coca);
                        SendNote(player, coca, amt);
                    }
                }
            }
        }
#endif

        private bool OnCooldown(BasePlayer player)
        {
            ulong userId = player.userID;
            if (!lastUsed.ContainsKey(userId) || Time.time - lastUsed[userId] > JOINT_USE_COOLDOWN)
            {
                lastUsed[player.userID] = Time.time;
                return false;
            }

            return true;
        }

#endregion

        #region Default Hooks

        private void Init()
        {
            _instance = this;

            permission.RegisterPermission(PERM_CRAFT, this);
            permission.RegisterPermission(PERM_GATHER, this);

            AddCovalenceCommand(CMD_CRAFT, nameof(CmdCraft));
            AddCovalenceCommand(CMD_TOGGLE_UI, nameof(CmdToggleUi));
            AddCovalenceCommand(CMD_CRAFT_PAGE, nameof(CmdChangePage));

            PlayerData.Load();

            timer.In(1f, () =>
            {
                if (DisableStackFix())
                {
                    Unsubscribe(nameof(CanStackItem));
                    Unsubscribe(nameof(CanCombineDroppedItem));
                    Unsubscribe(nameof(OnItemSplit));
                }
                else
                {
                    Subscribe(nameof(CanStackItem));
                    Subscribe(nameof(CanCombineDroppedItem));
                    Subscribe(nameof(OnItemSplit));
                }

                if (!_config.extinguishOnUnequip)
                {
                    Unsubscribe(nameof(OnActiveItemChanged));
                }
                else
                {
                    Subscribe(nameof(OnActiveItemChanged));
                }

                foreach (var item in _config.weedConfig)
                {
                    Loottable?.Call("AddCustomItem", this, item.ItemDefinition.itemid, item.skinId, item.displayName);
                }
            });
        }

        private void Unload()
        {
            RemoveEffectsGlobal();

            foreach (var id in openUis.ToArray())
            {
                BasePlayer player = BasePlayer.FindByID(id);
                DestroyUi(player);
            }

            foreach (var joint in jointTimers.Keys.ToList())
            {
                ExtinguishJoint(null, joint);
            }

            PlayerData.Save();

            _config = null;
            _instance = null;
        }

        #endregion

        #region Hooks

        #region Stack Fix

        object CanStackItem(Item item, Item target)
        {
            if (DisableStackFix()) return null;
            if (!IsJointOrWeed(item)) return null;

            if (item.info.itemid != target.info.itemid) return false;
            if (item.skin != target.skin) return false;
            if (item.name != target.name) return false;

            return null;
        }

        Item OnItemSplit(Item item, int amount)
        {
            if (DisableStackFix()) return null;
            if (!IsJointOrWeed(item)) return null;

            item.amount -= amount;
            Item split = ItemManager.Create(item.info, amount, item.skin);
            split.name = item.name;
            split.MarkDirty();
            item.MarkDirty();

            return split;
        }

        object CanCombineDroppedItem(WorldItem witem1, WorldItem witem2) => CanStackItem(witem1.item, witem2.item);

        #endregion

        #region Joint controls

        void OnPlayerInput(BasePlayer player, InputState input)
        {
            Item joint = player.GetActiveItem();

            if (!IsJoint(joint))
            {
                return;
            }

            // Ignite or extinguish
            if (input.IsDown(BUTTON.FIRE_SECONDARY))
            {
                if (!OnCooldown(player))
                {
                    if (joint.HasFlag(global::Item.Flag.OnFire))
                    {
                        ExtinguishJoint(player, joint);
                    }
                    else
                    {
                        IgniteJoint(player, joint);
                    }
                }
            }

            // Use
            if (input.IsDown(BUTTON.FIRE_PRIMARY))
            {
                if (!OnCooldown(player))
                {
                    UseJoint(player, joint);
                }
            }
        }

        void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (IsJoint(oldItem) && _config.extinguishOnUnequip)
            {
                ExtinguishJoint(player, oldItem);
            }
        }

        #endregion

        #region Gathering

        void OnGrowableGather(GrowableEntity plant, BasePlayer player)
        {
            if (PlanterboxDefender != null && PlanterboxDefender.Call("CanLootGrowableEntity", plant, player) != null)
            {
                return;
            }

            if (plant.prefabID == 3587624038 && plant.State == PlantProperties.State.Ripe && AllowedToGather(player))
            {
                OnHempGather(player, plant.transform.position, false, plant.Genes.GetGeneTypeCount(GrowableGenetics.GeneType.Hardiness));
            }

#if COCA
            if (plant.prefabID == 4038822397 && plant.State == PlantProperties.State.Ripe && AllowedToGather(player))
            {
                OnBerryGather(player, plant.transform.position, false, plant.Genes.GetGeneTypeCount(GrowableGenetics.GeneType.Hardiness));
            }
#endif
        }

        void OnCollectiblePickup(CollectibleEntity entity, BasePlayer player)
        {
            if (player == null)
            {
                return;
            }

            if (entity.prefabID == 3006540952 && AllowedToGather(player) && !IsDeployableNature(entity) && !entity.IsDestroyed)
            {
                OnHempGather(player, entity.transform.position, true);
            }

#if COCA
            if (entity.prefabID == 1989241797 && AllowedToGather(player) && !IsDeployableNature(entity) && !entity.IsDestroyed)
            {
                OnBerryGather(player, entity.transform.position, true);
            }
#endif
        }

        // Support AutoFarm
        void OnAutoFarmGather(string userID, StorageContainer container, Vector3 plantPositon, bool isCollectable, int hGenes = 0)
        {
            if (!AllowedToGather(userID))
            {
                return;
            }

            ushort locationMask = (ushort)TerrainMeta.BiomeMap.GetBiomeMaxType(plantPositon);
            foreach (var cfg in _config.weedConfig)
            {
                if (cfg.MaybeGather(locationMask, isCollectable, hGenes, out var weed))
                {
                    if (container.inventory.IsFull() || !weed.MoveToContainer(container.inventory))
                    {
                        weed.Drop(plantPositon, Vector3.up * 3f);
                    }
                }
            }
        }

        #endregion

        #region Crafting Ui

        private void OnLootEntity(BasePlayer player, MixingTable entity)
        {
            if (entity == null) return;

            if (AllowedToCraft(player))
            {
                entity.OnlyAcceptValidIngredients = false;
                CreateToggleUi(player);
                if (PlayerData.IsCraftingUiOpen(player))
                {
                    CreateRecipeUi(player);
                    CreateCraftButtonUi(player);
                }
            }
        }

        private void OnLootEntityEnd(BasePlayer player, MixingTable entity)
        {
            if (entity == null) return;

            entity.OnlyAcceptValidIngredients = true;
            DestroyUi(player);
        }

        #endregion

        #region Misc

        ItemContainer.CanAcceptResult? CanAcceptItem(ItemContainer container, Item item, int targetPos)
        {
            // Prevent joints form bein put in repair bench
            if (container.entityOwner?.prefabID == 3846783416u && IsJoint(item))
            {
                return ItemContainer.CanAcceptResult.CannotAccept;
            }

            return null;
        }

        void OnPlayerDeath(BasePlayer p, HitInfo hitInfo) => OnPlayerDisconnected(p, null);

        void OnPlayerDisconnected(BasePlayer p, string reason)
        {
            RemoveEffects(p);
            DestroyUi(p);
        }

        #endregion

        #endregion

        #region UI

        private static class UI
        {
            public const string LAYER_RECIPE = "ganja.ui.craft";
            public const string LAYER_TOGGLE = "ganja.ui.toggle";
            public const string LAYER_CRAFT_BUTTON = "ganja.ui.craftbutton";

            public const string LAYER_BLUR = "ganja.ui.effects.blur";
            public const string LAYER_COLOR = "ganja.ui.effects.color";

            public const string greenButtonColor = "0.415 0.5 0.258 0.7";
            public const string redButtonColor = "0.8 0.28 0.2 1";
            public const string blueButtonColor = "0.13 0.52 0.82 0.8";
            public const string greyButtonColor = "0.4 0.4 0.4 0.4";

            public const string buttonColor = "0.75 0.75 0.75 0.3";
            public const string buttonTextColor2 = "0.68 0.68 0.68 1";

            public const string textColor = "0.745 0.709 0.674 1";

            public const string itemTileColor = "0.2 0.2 0.19 1";

            public const int RECIPES_PER_PAGE = 4;
        }

        private void CreateToggleUi(BasePlayer player)
        {
            CuiElementContainer result = new CuiElementContainer();

            string rootPanel = result.Add(new CuiPanel
            {
                Image = new CuiImageComponent
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = "0.5 0",
                    AnchorMax = "0.5 0",
                    OffsetMin = "430 476",
                    OffsetMax = "572 501"
                }
            }, "Hud.Menu", UI.LAYER_TOGGLE);

            result.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1",
                },
                Button =
                {
                    Command = CMD_TOGGLE_UI,
                    Color = UI.buttonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = !PlayerData.IsCraftingUiOpen(player) ? "SHOW CUSTOM RECIPES" : "HIDE CUSTOM RECIPES",
                    Color = UI.buttonTextColor2,
                    FontSize = 12
                }
            }, rootPanel);

            openUis.Add(player.userID);
            CuiHelper.AddUi(player, result);
        }

        private void CreateRecipeUi(BasePlayer player, int page = 0)
        {
            
            int itemPanelSize = 14;
            CuiElementContainer result = new CuiElementContainer();

            #region Root Panel

            string rootPanelName = UI.LAYER_RECIPE;
                
            result.Add(new CuiElement
            {
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0.33 0.314 0.29 1",
                        //Color = "0.32 0.31 0.30 1",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0",
                        AnchorMax = "0.5 0",
                        OffsetMin = "192 302",
                        OffsetMax = "572 451"
                    }
                },
                Parent = "Hud.Menu",
                Name = rootPanelName
            });

            #endregion

            #region Page Nav

            if (_config.recipes.Count > UI.RECIPES_PER_PAGE)
            {
                result.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.88 0.01",
                        AnchorMax = "0.98 0.08",
                    },
                    Button =
                    {
                        Command = $"{CMD_CRAFT_PAGE} {page+1}",
                        Color = _config.recipes.Count > (page+1) * UI.RECIPES_PER_PAGE ? UI.blueButtonColor : UI.greyButtonColor
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Text = "->",
                        Color = UI.buttonTextColor2,
                        FontSize = 9
                    }
                }, rootPanelName);

                result.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.76 0.01",
                        AnchorMax = "0.86 0.08",
                    },
                    Button =
                    {
                        Command = $"{CMD_CRAFT_PAGE} {page-1}",
                        Color = page-1 >= 0 ? UI.blueButtonColor : UI.greyButtonColor
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Text = "<-",
                        Color = UI.buttonTextColor2,
                        FontSize = 9
                    }
                }, rootPanelName);
            }

            #endregion

            #region Recipe List

            int offset = page * UI.RECIPES_PER_PAGE;

            for(int i = 0; i < UI.RECIPES_PER_PAGE; i++)
            {
                CustomRecipe recipe;
                try
                {
                    recipe = _config.recipes[i + offset];
                }
                catch (ArgumentOutOfRangeException)
                {
                    break;
                }

                string recipePanel = result.Add(new CuiPanel
                {
                    Image = new CuiImageComponent
                    {
                        Color = "0.5 0.5 0.15 0"
                    },
                    RectTransform =
                    {
                        AnchorMin = $"0.02 {0.79f-i*0.23f}",
                        AnchorMax = $"0.98 {0.97f-i*0.23f}",
                    }
                }, rootPanelName);

                // Recipe preview
                result.Add(new CuiElement
                {
                    Components =
                    {
                        new CuiImageComponent
                        {
                            SkinId = recipe.producedItem.skinId,
                            ItemId = recipe.producedItem.ItemDefinition.itemid
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.05 0.5",
                            AnchorMax = "0.05 0.5",
                            OffsetMin = $"{-itemPanelSize} {-itemPanelSize}",
                            OffsetMax = $"{itemPanelSize} {itemPanelSize}"
                        }
                    },
                    Parent = recipePanel
                });

                // Recipe name
                result.Add(new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = "0.11 0",
                        AnchorMax = "0.4 1"
                    },
                    Text =
                    {
                        Text = recipe.producedItem.amount > 1 ? $"{recipe.producedItem.UiDisplayName} x{recipe.producedItem.amount}" : recipe.producedItem.UiDisplayName,
                        Align = TextAnchor.MiddleLeft,
                        Color = UI.textColor,
                        FontSize = 12
                    }
                }, recipePanel);

                // Display ingredients
                for (int slot = 0; slot < 5; slot++)
                {
                    Ingredient ingredient;
                    string anchor = $"{0.58f + slot * 0.095f} 0.5";

                    string ingredientPanel = result.Add(new CuiPanel
                    {
                        Image = new CuiImageComponent
                        {
                            Color = UI.itemTileColor
                        },
                        RectTransform =
                        {
                            AnchorMin = anchor,
                            AnchorMax = anchor,
                            OffsetMin = $"{-itemPanelSize} {-itemPanelSize}",
                            OffsetMax = $"{itemPanelSize} {itemPanelSize}"
                        }
                    }, recipePanel);

                    try
                    {
                        ingredient = recipe.ingredientSlots[slot];
                    }
                    catch (KeyNotFoundException)
                    {
                        continue;
                    }

                    // Ingredient image
                    result.Add(new CuiElement
                    {
                        Components =
                        {
                            new CuiImageComponent
                            {
                                SkinId = ingredient.skinId,
                                ItemId = ingredient.ItemDefinition.itemid
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = anchor,
                                AnchorMax = anchor,
                                OffsetMin = $"{-itemPanelSize} {-itemPanelSize}",
                                OffsetMax = $"{itemPanelSize} {itemPanelSize}"
                            }
                        },
                        Parent = recipePanel
                    });

                    // Ingredient amount
                    result.Add(new CuiLabel
                    {
                        RectTransform =
                        {
                            AnchorMin = anchor,
                            AnchorMax = anchor,
                            OffsetMin = $"{-itemPanelSize} {-itemPanelSize-2}",
                            OffsetMax = $"{itemPanelSize-2} 0"
                        },
                        Text =
                        {
                            Text = ingredient.amount.ToString(),
                            Align = TextAnchor.MiddleRight,
                            Color = UI.textColor,
                            FontSize = 12
                        }
                    }, recipePanel);

                }
            }

            #endregion

            CuiHelper.AddUi(player, result);
        }

        private void CreateCraftButtonUi(BasePlayer player)
        {
            CuiElementContainer craftButton = new CuiElementContainer();

            #region Craft Button

            craftButton.Add(new CuiElement
            {
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0.33 0.314 0.29 1",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0",
                        AnchorMax = "0.5 0",
                        OffsetMin = "193 40",
                        OffsetMax = "420 98"
                    }
                },
                Parent = "Hud.Menu",
                Name = UI.LAYER_CRAFT_BUTTON
            });

            craftButton.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = "0.03 0.1",
                    AnchorMax = "0.48 0.9",
                },
                Button =
                {
                    Command = CMD_CRAFT,
                    Color = UI.greenButtonColor
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Text = "START CRAFTING",
                    Color = UI.buttonTextColor2,
                    FontSize = 12
                }
            }, UI.LAYER_CRAFT_BUTTON);

            craftButton.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = "0.52 0",
                    AnchorMax = "0.95 1"
                },
                Text =
                {
                    Text = "Use this button to craft custom recipes",
                    Align = TextAnchor.MiddleLeft,
                    Color = UI.textColor,
                    FontSize = 12,
                    Font = "robotocondensed-regular.ttf"
                }
            }, UI.LAYER_CRAFT_BUTTON);

            #endregion

            CuiHelper.AddUi(player, craftButton);
        }

        private void DestroyUi(BasePlayer player, string layer = null)
        {
            if (layer != null)
            {
                CuiHelper.DestroyUi(player, layer);
                return;
            }

            if (openUis.Remove(player.userID))
            {
                CuiHelper.DestroyUi(player, UI.LAYER_RECIPE);
                CuiHelper.DestroyUi(player, UI.LAYER_TOGGLE);
                CuiHelper.DestroyUi(player, UI.LAYER_CRAFT_BUTTON);
            }
        }

        #endregion

        #region Commands

        private void CmdToggleUi(IPlayer iPlayer, string command, string[] args)
        {
            BasePlayer player = iPlayer.Object as BasePlayer;
            if (iPlayer.IsServer || player == null)
            {
                return;
            }

            DestroyUi(player);

            if (PlayerData.ToggleCraftingUi(player))
            {
                CreateRecipeUi(player);
                CreateCraftButtonUi(player);
            }

            CreateToggleUi(player);
        }

        private void CmdCraft(IPlayer iPlayer, string command, string[] args)
        {
            var player = iPlayer.Object as BasePlayer;
            var mixingTable = player?.inventory.loot?.entitySource as MixingTable;

            if (iPlayer.IsServer || player == null || mixingTable == null || !AllowedToCraft(player))
            {
                return;
            }

            List<Item> overflow = Pool.GetList<Item>();
            foreach(var recipe in _config.recipes)
            {
                if (recipe.TryCraft(mixingTable, overflow))
                {
                    foreach(var item in overflow)
                    {
                        if (!item.MoveToContainer(player.inventory.containerMain))
                        {
                            item.DropAndTossUpwards(mixingTable.transform.position + mixingTable.transform.up * 1.2f);
                        }
                    }
                }
            }

            Pool.FreeList(ref overflow);
        }

        private void CmdChangePage(IPlayer iPlayer, string command, string[] args)
        {
            BasePlayer player = iPlayer.Object as BasePlayer;
            if (iPlayer.IsServer || player == null || args.Length < 1)
            {
                return;
            }

            DestroyUi(player, UI.LAYER_RECIPE);

            int page = Mathf.Clamp(Int32.Parse(args[0]), 0, Mathf.CeilToInt((float)_config.recipes.Count / UI.RECIPES_PER_PAGE) - 1);

            CreateRecipeUi(player, page);
        }

        #endregion

        #region Effects

        private void RunEffects(BasePlayer player, float time)
        {
            float repeatInterval = 0.25f;

            timer.In(time, () => CuiHelper.DestroyUi(player, UI.LAYER_BLUR));
            timer.In(time, () => CuiHelper.DestroyUi(player, UI.LAYER_COLOR));

            timer.Repeat(time / 3f, 2, () =>
            {
                if (Random.Range(0, 2) == 1)
                    RunEffect(SHAKE_EFFECT, player);
                else
                    RunEffect(SHAKE2_EFECT, player);
            });

            RunEffect(BREATHE_EFFECT, player);
            RunEffect(LICK_EFFECT, player);

            timer.Repeat(0.25f, 4, () =>
            {
                RunEffect(SMOKE_EFFECT, player, false);
            });

            timer.In(time / 2f, () =>
            {
                if (Random.Range(0, 11) == 1)
                    RunEffect(VOMIT_EFFECT, player);  
            });

            CreateBlur(player);
            timer.Repeat(repeatInterval, Mathf.FloorToInt(time / repeatInterval), () => CreateColor(player));
        }

        private void RunEffect(string effect, BasePlayer player, bool defaultPos = true, bool broadcast = true)
        {
            if (player == null) return;

            if (defaultPos)
                Effect.server.Run(effect, player, 0, Vector3.zero, Vector3.zero, null, broadcast);
            else
                Effect.server.Run(effect, player, 0, Vector3.up*1.7f, new Vector3(1, 0, 0), null, broadcast);
        }

        void CreateBlur(BasePlayer player)
        {
            if (player == null) return;
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.5", Material = "assets/content/ui/uibackgroundblur.mat" },
                FadeOut = 1f
            }, "Overlay", UI.LAYER_BLUR);
            CuiHelper.DestroyUi(player, UI.LAYER_BLUR);
            CuiHelper.AddUi(player, container);
        }

        void CreateColor(BasePlayer player)
        {
            if (player == null) return;
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = RandomColor() },
                FadeOut = 0.5f
            }, UI.LAYER_BLUR, UI.LAYER_COLOR);
            CuiHelper.DestroyUi(player, UI.LAYER_COLOR);
            CuiHelper.AddUi(player, container);
        }

        private void RemoveEffectsGlobal()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                RemoveEffects(player);
            }
        }

        private void RemoveEffects(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UI.LAYER_BLUR);
        }

        private string RandomColor(float opacity = 0.3f)
        {
            var random = new System.Random();
            return $"{random.NextDouble()} {random.NextDouble()} {random.NextDouble()} {opacity}";
        }

        #endregion
    }
}