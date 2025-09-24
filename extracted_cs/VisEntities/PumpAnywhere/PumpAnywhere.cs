// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Pump Anywhere", "VisEntities", "1.0.0")]
    [Description("Forces water pumps to produce clean water even in salty areas like beaches or the sea.")]
    public class PumpAnywhere : RustPlugin
    {
        #region Fields

        private static PumpAnywhere _plugin;
        private static Configuration _config;

        private ItemDefinition _freshWaterDef;
        private ItemDefinition _saltWaterDef;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Convert Salt Output To Fresh")]
            public bool ConvertSaltOutputToFresh { get; set; }

            [JsonProperty("Convert Existing Salt Stacks In Pump")]
            public bool ConvertExistingSaltStacksInPump { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Config changes detected! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                ConvertSaltOutputToFresh = true,
                ConvertExistingSaltStacksInPump = true
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
        }

        private void Unload()
        {
            _config = null;
            _plugin = null;
        }

        private void OnServerInitialized(bool isStartup)
        {
            _freshWaterDef = ItemManager.FindItemDefinition("water");
            _saltWaterDef = ItemManager.FindItemDefinition("water.salt");

            if (_freshWaterDef == null)
                PrintError("Failed to find ItemDefinition for 'water'. Plugin cannot function correctly.");

            if (_saltWaterDef == null)
                PrintError("Failed to find ItemDefinition for 'water.salt'. Plugin cannot function correctly.");
        }

        private object OnWaterCollect(WaterPump waterPump, ItemDefinition wouldCollect)
        {
            if (waterPump == null)
                return null;

            if (!_config.ConvertSaltOutputToFresh)
                return null;

            if (_freshWaterDef == null || _saltWaterDef == null)
                return null;

            bool wasSaltOutput = IsSaltDefinition(wouldCollect);

            if (_config.ConvertExistingSaltStacksInPump)
                EnsureContainerIsFresh(waterPump);

            if (wasSaltOutput)
            {
                AddFreshToPump(waterPump, waterPump.AmountPerPump);
                return true;
            }

            return null;
        }

        #endregion Oxide Hooks

        #region Helper Functions

        private bool IsSaltDefinition(ItemDefinition itemDefinition)
        {
            if (itemDefinition == null)
                return false;

            if (_saltWaterDef == null)
                return false;

            return itemDefinition.itemid == _saltWaterDef.itemid;
        }

        private bool IsFreshDefinition(ItemDefinition itemDefinition)
        {
            if (itemDefinition == null)
                return false;

            if (_freshWaterDef == null)
                return false;

            return itemDefinition.itemid == _freshWaterDef.itemid;
        }

        private void AddFreshToPump(WaterPump waterPump, int waterAmountToAdd)
        {
            if (waterPump == null)
                return;

            if (_freshWaterDef == null)
                return;

            if (waterAmountToAdd <= 0)
                return;

            if (waterPump.inventory == null)
                return;

            waterPump.inventory.AddItem(_freshWaterDef, waterAmountToAdd, 0UL, ItemContainer.LimitStack.Existing);
        }

        private void EnsureContainerIsFresh(WaterPump waterPump)
        {
            if (waterPump == null)
                return;

            if (waterPump.inventory == null)
                return;

            List<Item> pumpInventoryItems = waterPump.inventory.itemList;
            if (pumpInventoryItems == null || pumpInventoryItems.Count == 0)
                return;

            Item containerLiquidItem = pumpInventoryItems[0];
            if (containerLiquidItem == null)
                return;

            ItemDefinition containerLiquidDefinition = containerLiquidItem.info;
            bool containsSaltWater = IsSaltDefinition(containerLiquidDefinition);
            if (!containsSaltWater)
                return;

            int containedLiquidAmount = containerLiquidItem.amount;

            containerLiquidItem.Remove(0f);
            AddFreshToPump(waterPump, containedLiquidAmount);
        }

        #endregion Helper Functions
    }
}