// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Custom Store Items", "supreme", "1.0.0")]
    [Description("Creates and gives custom items that can be used in any stores")]
    public class CustomStoreItems : RustPlugin
    {
        #region Class Fields
        
        private PluginConfig _pluginConfig;

        private const string IgniterShortname = "electric.igniter";
        private const string LargeLootBagShortname = "halloween.lootbag.large";
        
        private const string DefaultDeployEffect = "assets/prefabs/misc/halloween/lootbag/effects/loot_bag_upgrade.prefab";
        private const string DefaultUnwrapEffect = "assets/prefabs/misc/halloween/lootbag/effects/gold_open.prefab";
        
        private const string UnwrapAction = "unwrap";

        private readonly object _returnableObject = new object();

        private enum CustomItemType : byte
        {
            Usable = 0,
            Deployable = 1
        }

        private enum CustomItemRewardType : byte
        {
            Default = 0,
            Spawn = 1
        }

        #endregion

        #region Hooks

        private object CanBuild(Planner planner, Construction prefab, Construction.Target target)
        {
            Item item = planner.GetItem();
            if (item == null || item.info.shortname != IgniterShortname)
            {
                return null;
            }

            BasePlayer player = item.GetOwnerPlayer();
            if (player == null)
            {
                return null;
            }
            
            if (_pluginConfig.CustomItems.ContainsKey(item.skin))
            {
                ProcessCustomItem(item, CustomItemType.Deployable, player, target.position);
                player.ChatMessage(Lang(LangKeys.Deploy, player, item.name));
                return _returnableObject;
            }
            
            return null;
        }
        
        private object OnItemAction(Item item, string action, BasePlayer player)
        {
            if (item.info.shortname != LargeLootBagShortname)
            {
                return null;
            }

            if (action != UnwrapAction)
            {
                return null;
            }

            if (_pluginConfig.CustomItems.ContainsKey(item.skin))
            {
                ProcessCustomItem(item, CustomItemType.Usable, player, Vector3.zero);
                player.ChatMessage(Lang(LangKeys.Unwrap, player, item.name));
                return _returnableObject;
            }
            
            return null;
        }

        #endregion

        #region Core Methods

        private void CreateCustomItem(BasePlayer player, CustomItemType customItemType, ulong skinId, int amount, string newName)
        {
            switch (customItemType)
            {
                case CustomItemType.Usable:
                {
                    Item item = ItemManager.CreateByName(LargeLootBagShortname, amount, skinId);
                    item.name = newName;
                    player.GiveItem(item);
                    break;
                }
                case CustomItemType.Deployable:
                {
                    Item item = ItemManager.CreateByName(IgniterShortname, amount, skinId);
                    item.name = newName;
                    player.GiveItem(item);
                    break;
                }
            }
            
            player.ChatMessage(Lang(LangKeys.ReceivedItem, player, newName));
        }

        private void ProcessCustomItem(Item item, CustomItemType customItemType, BasePlayer player, Vector3 position)
        {
            RemoveItem(item);
            CustomItem customItem = _pluginConfig.CustomItems[item.skin];
            string command = customItem.Command.Replace("{playerId}", player.UserIDString);
            if (!string.IsNullOrEmpty(command))
            {
                rust.RunServerCommand(command);
            }
            
            Effect.server.Run(customItemType == CustomItemType.Usable ? _pluginConfig.UnwrapEffect : _pluginConfig.DeployEffect, player.transform.position + new Vector3(0f, 1f));
            
            switch (customItem.CustomItemRewardType)
            {
                case CustomItemRewardType.Spawn:
                {
                    BaseEntity entity = GameManager.server.CreateEntity(customItem.ItemSpawn, position);
                    if (entity == null)
                    {
                        return;
                    }
                    
                    entity.Spawn();
                    break;
                }
            }
        }

        #endregion

        #region Helper Methods

        private void RemoveItem(Item item)
        {
            if (item.amount == 1) 
            {
                item.Remove();
                ItemManager.DoRemoves();
            } 
            else 
            {
                item.amount = --item.amount;
                item.MarkDirty();
            }
        }
        
        private BasePlayer FindPlayer(string arg)
        {
            return BasePlayer.activePlayerList.FirstOrDefault(p => p.displayName.Contains(arg, CompareOptions.OrdinalIgnoreCase) || p.UserIDString.Contains(arg)) ?? BasePlayer.sleepingPlayerList.FirstOrDefault(p => p.displayName.Contains(arg, CompareOptions.OrdinalIgnoreCase) || p.UserIDString.Contains(arg));
        }

        #endregion

        #region Commands

        [ConsoleCommand("customstoreitems.give")]
        private void GiveCustomItemCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Args.Length < 5)
            {
                Puts("Syntax error, please use customstoreitems.give playerName/playerId Usable/Deployable skinId amount name\nExample: customstoreitems.give supreme Usable 25312 10 CustomItem");
                return;
            }
            
            BasePlayer player = FindPlayer(arg.Args[0]);
            if (player == null)
            {
                Puts("Syntax error, please use customstoreitems.give playerName/playerId Usable/Deployable skinId amount name\nExample: customstoreitems.give supreme Usable 25312 10 CustomItem");
                return;
            }

            CustomItemType customItemType;
            if (!Enum.TryParse(arg.Args[1], out customItemType))
            {
                Puts("Syntax error, cannot parse the custom item type, please use Usable or Deployable");
                return;
            }

            ulong skinId = Convert.ToUInt64(arg.Args[2]);
            if (skinId == 0)
            {
                Puts("Syntax error, please choose a correct skin id");
                return;
            }

            int amount = Convert.ToInt32(arg.Args[3]);
            if (amount == 0)
            {
                Puts("Syntax error, please choose a correct amount");
                return;
            }
            
            string newName = arg.Args[4];
            if (string.IsNullOrEmpty(newName))
            {
                Puts("Syntax error, please choose a correct name");
                return;
            }
            
            CreateCustomItem(player, customItemType, skinId, amount, newName);
        }

        #endregion

        #region Configuration

        private class PluginConfig
        {
            [DefaultValue(DefaultDeployEffect)]
            [JsonProperty(PropertyName = "Deploy effect when deploying the custom item")]
            public string DeployEffect { get; set; }
            
            [DefaultValue(DefaultUnwrapEffect)]
            [JsonProperty(PropertyName = "Unwrap effect when unwrapping the custom item")]
            public string UnwrapEffect { get; set; }
            
            [JsonProperty(PropertyName = "Custom Items (Skin Id and their settings)")]
            public Dictionary<ulong, CustomItem> CustomItems { get; set; }
        }

        private class CustomItem
        {
            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty(PropertyName = "Custom Item Reward Type (Default/Spawn)")]
            public CustomItemRewardType CustomItemRewardType { get; set; }
            
            [JsonProperty(PropertyName = "Command to run after using the custom item (Leave empty if not needed)")]
            public string Command { get; set; }
            
            [JsonProperty(PropertyName = "Item to spawn (Prefab) (Only works if the Item Reward Type is set to Spawn)")]
            public string ItemSpawn { get; set; }
        }
        
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Loading Default Config");
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            Config.Settings.DefaultValueHandling = DefaultValueHandling.Populate;
            _pluginConfig = AdditionalConfig(Config.ReadObject<PluginConfig>());
            Config.WriteObject(_pluginConfig);
        }

        private PluginConfig AdditionalConfig(PluginConfig pluginConfig)
        {
            pluginConfig.CustomItems = pluginConfig.CustomItems ?? new Dictionary<ulong, CustomItem>
            {
                [1] = new CustomItem
                {
                    CustomItemRewardType = CustomItemRewardType.Default,
                    Command = "sr add {playerId} 100",
                    ItemSpawn = ""
                },
                [2] = new CustomItem
                {
                    CustomItemRewardType = CustomItemRewardType.Spawn,
                    Command = "",
                    ItemSpawn = "assets/prefabs/deployable/bbq/bbq.deployed.prefab"
                }
            };
            
            return pluginConfig;
        }

        #endregion
        
        #region Language
        
        private class LangKeys
        {
            public const string Unwrap = nameof(Unwrap);
            public const string Deploy = nameof(Deploy);
            public const string ReceivedItem = nameof(ReceivedItem);
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [LangKeys.Unwrap] = "You have unwrapped <color=#acfa58>{0}</color>!",
                [LangKeys.Deploy] = "You have successfully deployed <color=#acfa58>{0}</color>!",
                [LangKeys.ReceivedItem] = "You have received <color=#acfa58>{0}</color>!",
            }, this);
        }
        
        private string Lang(string key, BasePlayer player = null, params object[] args)
        {
            try
            {
                return string.Format(lang.GetMessage(key, this, player?.UserIDString), args);
            }
            catch (Exception ex)
            {
                PrintError($"Lang Key '{key}' threw exception:\n{ex}");
                throw;
            }
        }

        #endregion
    }
}