using ConVar;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Rust.Ai;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using VLB;

namespace Oxide.Plugins
{
    [Info("Simple Gather","Jezz","0.0.2")]
    [Description("A simple gather rate changer for the game rust")]
    public class SimpleGather : CovalencePlugin
    {
        
        
        #region Config and Dictionary Initialization
        private class PluginConfig
        {
            public bool EnableGatherRate;
            public int GatherRateStone;
            public int GatherRateMetal;
            public int GatherRateSulfur;
            public int GatherRateWood;
            public int GatherRateFlesh;
            public int GatherRateCloth;
            public int GatherRatePickups;
            public int GatherRateFarm;
            public int GatherRateQuarry;
        }


        private PluginConfig config;

        private void Init()
        {
            config = Config.ReadObject<PluginConfig>();

        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(GetDefaultConfig(), true);
        }

        public void LoadConfig()
        {
            config = Config.ReadObject();
        }

        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                EnableGatherRate = true,
                GatherRateStone = 1,
                GatherRateMetal = 1,
                GatherRateSulfur = 1,
                GatherRateWood = 1,
                GatherRateFlesh = 1,
                GatherRateCloth = 1,
                GatherRatePickups = 1,
                GatherRateFarm = 1,
                GatherRateQuarry = 1,
    };
        }



        IDictionary gathermultiplier = new Dictionary
        {
            ["stones"] = config.GatherRateStone,
            ["metal.ore"] = config.GatherRateMetal,
            ["sulfur.ore"] = config.GatherRateSulfur,
            ["wood"] = config.GatherRateWood,
            ["flesh"] = config.GatherRateFlesh,
            ["hemp"] = config.GatherRateCloth
        };

        #endregion

        #region Utility Methods
        int ChangeGather(int itemamount, int multiplier)
        {
            if (multiplier == 0)
            {
                return 0;
            }
            else if (multiplier < 0)
            {
                PrintError("[SimpleGather] Multiplier is less than 0, Please Check config file");
                return 0;
            }
            else return itemamount * multiplier;
        }
        #endregion

        #region Gather Rate Changing
        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (config.EnableGatherRate)
            {
                if (dispenser.gatherType.ToString() == "Flesh")
                    item.amount = ChangeGather(item.amount, gathermultiplier["flesh"]);
                else
                    item.amount = ChangeGather(item.amount, gathermultiplier[item.info.shortname]);
            }
        }


        void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (config.EnableGatherRate)
                item.amount = ChangeGather(item.amount, gathermultiplier[item.info.shortname]);
        }

        void OnGrowableGathered(GrowableEntity plant, Item item, BasePlayer player)
        {
            if (config.EnableGatherRate)
            {
                item.amount = ChangeGather(item.amount, config.GatherRateFarm);
            }
        }


        void OnCollectiblePickup(Item item, BasePlayer player, CollectibleEntity entity)
        {
            
            if (config.EnableGatherRate)
            {
                if (item.info.shortname == "hemp")
                    item.amount = ChangeGather(item.amount, gathermultiplier[item.info.shortname]);
                else
                    item.amount = ChangeGather(item.amount, config.GatherRatePickups);
            }
        }

        void OnQuarryGather(MiningQuarry quarry, Item item)
        {
            if (config.EnableGatherRate)
            {
                item.amount = ChangeGather(item.amount, config.GatherRateQuarry);
            }
        }
        #endregion
    }
}

