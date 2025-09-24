/* 

FORK OF https://umod.org/plugins/extra-gather-bonuses

MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

Original Author: Orange

*/

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Extra Gather Bonuses", "David", "1.0.7")]
    [Description("Get extra items on gathering resources")]
    public class ExtraGatherBonuses : RustPlugin
    {
        #region Vars

        private Dictionary<string, GatherInfo> bonuses = new Dictionary<string, GatherInfo>();

        #endregion

        #region Oxide Hooks

        private void Init()
        {
            foreach (var value in config.list)
            {
                bonuses.Add(value.resource, value);
                if (!permission.PermissionExists(value.perm, this))
                {
                    permission.RegisterPermission(value.perm, this);
                }
            }
        }

        private void OnCollectiblePickup(CollectibleEntity collectible, BasePlayer player)
        {
            try
            {
                foreach (ItemAmount item in collectible.itemList)
                {
                    OnGather(player, item.itemDef.shortname);
                }
            }
            catch
            {
                //
            }
        }

        private void OnCropGather(GrowableEntity plant, Item item, BasePlayer player)
        {
            try
            {
                OnGather(player, item.info.shortname);
            }
            catch
            {
                //
            }
        }

        private void OnGrowableGathered(GrowableEntity plant, Item item, BasePlayer player)
        {
            try
            {
                OnGather(player, item.info.shortname);
            }
            catch
            {
                //
            }
        }


        private void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            try
            {
                OnGather(player, item.info.shortname);
            }
            catch
            {
                //
            }
        }

        #endregion

        #region Core

        private void OnGather(BasePlayer player, string name)
        {
            var list = bonuses.Where(x => x.Key == name).ToList();
            if (list.Count == 0)
            {
                return;
            }

            foreach (var value in list)
            {
                CheckBonus(player, value.Value);
            }
        }

        private void CheckBonus(BasePlayer player, GatherInfo info)
        {
            if (permission.UserHasPermission(player.UserIDString, info.perm) == false)
            {
                return;
            }

            var amount = 0;
            var max = info.maxItems;
            foreach (var def in info.extra)
            {
                if (max != 0 && amount >= max)
                {
                    break;
                }

                var random = Core.Random.Range(0, 100);
                if (random > def.chance)
                {
                    continue;
                }

                var item = CreateItem(def);
                if (item != null)
                {
                    player.GiveItem(item);
                    Message(player, "Received", def.displayName);
                }

                amount++;
            }
        }

        private Item CreateItem(BaseItem def)
        {
            var amount = Core.Random.Range(def.amountMin, def.amountMax + 1);
            var item = ItemManager.CreateByName(def.shortname, amount, def.skinId);
            if (item == null)
            {
                PrintWarning($"Can't create item ({def.shortname})");
                return null;
            }

            item.name = def.displayName;
            return item;
        }

        #endregion

        #region Configuration 1.1.0

        private static ConfigData config;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Chat message when item received")]
            public bool chatMsg { get; set; }

            [JsonProperty(PropertyName = "Bonus list")]
            public List<GatherInfo> list { get; set; }
        }

        private class GatherInfo
        {
            [JsonProperty(PropertyName = "Item gathered to get bonus")]
            public string resource;

            [JsonProperty(PropertyName = "Permission")]
            public string perm;

            [JsonProperty(PropertyName = "Maximal items that player can get by once")]
            public int maxItems;

            [JsonProperty(PropertyName = "Bonus list")]
            public List<BaseItem> extra;
        }

        private class BaseItem
        {
            [JsonProperty(PropertyName = "Shortname")]
            public string shortname;

            [JsonProperty(PropertyName = "Amount min")]
            public int amountMin = 1;

            [JsonProperty(PropertyName = "Amount max")]
            public int amountMax = 1;

            [JsonProperty(PropertyName = "Skin")]
            public ulong skinId;

            [JsonProperty(PropertyName = "Display name")]
            public string displayName;

            [JsonProperty(PropertyName = "Chance")]
            public int chance;
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                chatMsg = true,
                list = new List<GatherInfo>
                {
                    new GatherInfo
                    {
                        resource = "cloth",
                        maxItems = 0,
                        perm = "extragatherbonuses.default",
                        extra = new List<BaseItem>
                        {
                            new BaseItem
                            {
                                shortname = "paper",
                                amountMin = 1,
                                amountMax = 3,
                                skinId = 2556285147,
                                displayName = "Sativa Hemp",
                                chance = 50
                            }
                        }
                    },
                    new GatherInfo
                    {
                        resource = "white.berry",
                        maxItems = 0,
                        perm = "extragatherbonuses.default",
                        extra = new List<BaseItem>
                        {
                            new BaseItem
                            {
                                shortname = "paper",
                                amountMin = 1,
                                amountMax = 5,
                                skinId = 2783018053,
                                displayName = "Coca Leaf",
                                chance = 75
                            }
                        }
                    }
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<ConfigData>();
                if (config == null)
                {
                    throw new JsonReaderException();
                }
            }
            catch
            {
                PrintError("Configuration file is corrupt! Loading defaults...");
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

        #region Localization 1.1.1

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Received", "You received {0} for gathering!"},
            }, this);
        }

        private void Message(BasePlayer player, string messageKey, params object[] args)
        {
            if (player == null || config.chatMsg == false)
            {
                return;
            }


            var message = GetMessage(messageKey, player.UserIDString, args);
            player.ChatMessage(message);
        }

        private string GetMessage(string messageKey, string playerID, params object[] args)
        {
            return string.Format(lang.GetMessage(messageKey, this, playerID), args);
        }

        #endregion
    }
}