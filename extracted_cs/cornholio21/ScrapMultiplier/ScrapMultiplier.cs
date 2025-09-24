using Newtonsoft.Json;
using Oxide.Core;
using Rust;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ScrapMultiplier", "Cornholio21", "1.0.5")]
    [Description("Multiplies the amount of scrap found in crates.")]
    public class ScrapMultiplier : RustPlugin
    {
        private ConfigurationFile _configuration;
        private const int ScrapItemId = -932201673;

        public class ConfigurationFile
        {
            [JsonProperty(PropertyName = "Scrap Multiplier")]
            public float ScrapMultiplier = 1f;
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file");
            _configuration = new ConfigurationFile { ScrapMultiplier = 1f };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _configuration = Config.ReadObject<ConfigurationFile>();
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(_configuration);

        private void OnServerInitialized()
        {
            RepopulateContainers();
        }

        private void OnLootSpawn(LootContainer container)
        {
            if (container?.inventory?.itemList == null)
            {
                return;
            }

            NextFrame(() =>
            {
                if (container?.inventory?.itemList == null)
                {
                    return;
                }

                foreach (Item item in container.inventory.itemList)
                {
                    if (item.info.itemid == ScrapItemId)
                    {
                        item.amount = (int)(item.amount * _configuration.ScrapMultiplier);
                        item.MarkDirty();
                    }
                }
            });
        }

        private void RepopulateContainers()
        {
            int affectedContainers = 0;

            foreach (var container in UnityEngine.Object.FindObjectsOfType<LootContainer>())
            {
                if (container?.inventory != null)
                {
                    container.inventory.Clear();
                    container.SpawnLoot();
                    affectedContainers++;
                }
            }

            PrintWarning($"Repopulated loot in {affectedContainers} containers.");
        }

        private void Unload()
        {
            RepopulateContainers();
        }
    }
}
