using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("FishingRewards", "headtapper", "1.0.0")]
    [Description("Grant additional items to players for catching fish with configurable loot table.")]

    public class FishingRewards : RustPlugin
    {
        #region Configuration

        private PluginConfig config;

        private class PluginConfig
        {
            public int PercentChanceOfReward;

            [JsonProperty("Drop Rates")]
            public Dictionary<string, int> Drops { get; set; }
        }

        private PluginConfig GetDefaultConfigValues()
        {
            return new PluginConfig
            {
                PercentChanceOfReward = 75,
                Drops = new Dictionary<string, int>
                {
                    ["rifle.ak"] = 1,
                    ["rifle.bolt"] = 2,
                    ["rifle.sks"] = 2,
                    ["rifle.m39"] = 4,
                    ["rifle.lr300"] = 2,
                    ["rifle.semiauto"] = 11,
                    ["rifle.l96"] = 1,
                    ["weapon.mod.8x.scope"] = 2,
                    ["lmg.m249"] = 1,
                    ["shotgun.spas12"] = 2,
                    ["shotgun.m4"] = 1,
                    ["t1_smg"] = 11,
                    ["smg.2"] = 7,
                    ["smg.thompson"] = 5,
                    ["smg.mp5"] = 2,
                    ["pistol.revolver"] = 20,
                    ["pistol.semiauto"] = 10,
                    ["pistol.python"] = 5,
                    ["pistol.m92"] = 5,
                    ["pistol.prototype17"] = 3,
                    
                }
            };
        }

        protected override void LoadDefaultConfig()
        {
            config = GetDefaultConfigValues();
            SaveConfig();
        }

        protected override void LoadConfig()
        {
            try
            {
                base.LoadConfig();
                config = Config.ReadObject<PluginConfig>();
                SaveConfig();
            }
            catch (Exception ex)
            {
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(config, true);

        #endregion

        #region Initialization

        System.Random rnd = new System.Random();

        private const string UsePluginPermission = "fishingrewards.use";

        private void Init()
        {
            permission.RegisterPermission(UsePluginPermission, this);
        }

        #endregion

        #region Hooks

        private void OnFishCaught(ItemDefinition definition, BaseFishingRod rod, BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, UsePluginPermission))
                return;

            Item reward = GetFishingReward();
            if (reward != null)
                player.inventory.GiveItem(reward);
        }

        #endregion

        #region Functions

        private Item GetFishingReward()
        {
            if (rnd.Next(100) > config.PercentChanceOfReward)
                return null;

            var rewardsTable = (from x in config.Drops orderby x.Value ascending select x);
            int nWeights = config.Drops.Values.Sum();
            int selected = rnd.Next(1, nWeights + 1);
            int current = 0;

            foreach (var itemEntry in rewardsTable)
            {
                current += itemEntry.Value;
                if (current >= selected)
                    return ItemManager.CreateByName(itemEntry.Key, 1);
            }

            return null;
        }

        #endregion
    }
}
