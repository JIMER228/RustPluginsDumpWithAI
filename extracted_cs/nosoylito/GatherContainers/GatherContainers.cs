using System;
using Oxide.Core.Configuration;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Gather Containers", "NoSoyLito", "0.0.1")]
    [Description("Server PVP Gamemode that randomly divides the server population in two factions")]
    public class GatherContainers : RustPlugin
    {
        #region "Fields"

        private PluginConfig config;
        private DynamicConfigFile dataFile;

        #endregion

        #region "Oxide Hooks"

        void Init()
        {
            config = Config.ReadObject<PluginConfig>();
        }

        void OnLootEntityEnd(BasePlayer player, BaseCombatEntity entity)
        {
            if (player != null)
            {
                LootContainer container = entity as LootContainer;
                if (container.inventory.itemList.Count == 0)
                {
                    GiveMaterialsForCrate(player, container);
                }
            }
        }

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (IsPlayer(info))
            {
                if (IsLootContainer(entity))
                {
                    if (IsBreakableContainer(entity))
                    {
                        GiveMaterialsForBarrel(entity, info);
                    }
                }
            }
            return null;
        }

        #endregion

        #region "Plugin guts"

        private bool IsPlayer(HitInfo info)
        {
            if (info != null && info.Initiator != null && info.InitiatorPlayer != null && !info.InitiatorPlayer.IsNpc)
            {
                return true;
            }
            return false;
        }

        private bool IsLootContainer(BaseCombatEntity entity)
        {
            if (entity.GetType().Name.Equals("LootContainer"))
            {
                return true;
            }
            return false;
        }

        private bool IsBreakableContainer(BaseCombatEntity entity)
        {
            if (entity.ShortPrefabName.Contains("barrel"))
            {
                return true;
            }
            return false;
        }

        private void GiveMaterialsForBarrel(BaseCombatEntity entity, HitInfo info)
        {
            //Puts(info.damageProperties.name);
            //Puts(info.damageTypes.Total().ToString());
            float amount = config.ContainerGatheringMultiplier * info.damageTypes.Total();
            int maxAmount = (int)Math.Round(amount / 4.0);
            int minAmount = (int)Math.Round(maxAmount / 2.0);
            if (maxAmount < 1) { maxAmount = 1; minAmount = 1; }
            if (minAmount < 1) { minAmount = 1; }
            info.InitiatorPlayer.GiveItem(ItemManager.CreateByName("metal.fragments", Core.Random.Range(minAmount, maxAmount)));
        }

        private void GiveMaterialsForCrate(BasePlayer player, LootContainer container)
        {
            switch (container.ShortPrefabName)
            {
                case "crate_basic":
                    player.GiveItem(ItemManager.CreateByName("wood", 15 * config.ContainerGatheringMultiplier));
                    break;
                case "crate_tools":
                    player.GiveItem(ItemManager.CreateByName("metal.fragments", 25 * config.ContainerGatheringMultiplier));
                    player.GiveItem(ItemManager.CreateByName("metal.refined", 1 * config.ContainerGatheringMultiplier));
                    break;
                case "crate_mine":
                    player.GiveItem(ItemManager.CreateByName("wood", 50 * config.ContainerGatheringMultiplier));
                    break;
                case "crate_normal_2":
                    player.GiveItem(ItemManager.CreateByName("wood", 50 * config.ContainerGatheringMultiplier));
                    break;
                case "crate_normal_2_food":
                    player.GiveItem(ItemManager.CreateByName("wood", 50 * config.ContainerGatheringMultiplier));
                    break;
                case "crate_normal_2_medical":
                    player.GiveItem(ItemManager.CreateByName("wood", 50 * config.ContainerGatheringMultiplier));
                    break;
                case "crate_normal":
                    player.GiveItem(ItemManager.CreateByName("wood", 50 * config.ContainerGatheringMultiplier));
                    player.GiveItem(ItemManager.CreateByName("metal.fragments", 5 * config.ContainerGatheringMultiplier));
                    break;
                case "crate_elite":
                    player.GiveItem(ItemManager.CreateByName("metal.fragments", 100 * config.ContainerGatheringMultiplier));
                    player.GiveItem(ItemManager.CreateByName("metal.refined", 5 * config.ContainerGatheringMultiplier));
                    break;
            }
        }

        #endregion

        #region "Config logic"

        private class PluginConfig
        {
            public int ContainerGatheringMultiplier;
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(GetDefaultConfig(), true);
        }

        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                ContainerGatheringMultiplier = 1
            };
        }

        #endregion

        #region "Helpers"

        string ColorString(string text, string color)
        {
            return "<color=" + color + ">" + text + "</color>";
        }

        #endregion
    }
}

