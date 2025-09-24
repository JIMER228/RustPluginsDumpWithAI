using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BaseFixer", "Pitu", "1.0.4")]
    [Description("A plugin to repair entire bases using a command.")]
    public class BaseFixer : RustPlugin
    {
        #region Configuration

        private Configuration _config;

        private sealed class Configuration
        {
            [JsonProperty(PropertyName = "Entities Per Second")]
            public int EntitiesPerSecond { get; set; } = 10;

            [JsonProperty(PropertyName = "Damage Repair Cooldown")]
            public float DamageRepairCooldown { get; set; } = 60f;

            [JsonProperty(PropertyName = "Repair Cost Multiplier")]
            public int RepairCostMultiplier { get; set; } = 1;

            [JsonProperty(PropertyName = "Global Repair Limit")]
            public int GlobalRepairLimit { get; set; } = 100;

            [JsonProperty(PropertyName = "Command")]
            public string Command { get; set; } = "br";

            [JsonProperty(PropertyName = "Messages")]
            public MessagesConfig Messages { get; set; } = new MessagesConfig();

            [JsonProperty(PropertyName = "Repair Materials")]
            public RepairMaterialsConfig RepairMaterials { get; set; } = new RepairMaterialsConfig();
        }

        private sealed class MessagesConfig
        {
            public string NoPermission { get; set; } = "<color=#FF4500>You don't have permission to use this command.</color>";
            public string NotInRange { get; set; } = "<color=#FF4500>You are not in a Tool Cupboard range.</color>";
            public string Cooldown { get; set; } = "<color=#FF4500>You must wait before using this command again. Cooldown: {0} seconds.</color>";
            public string NoDamagedEntities { get; set; } = "<color=#FF4500>No damaged entities found to repair.</color>";
            public string Repairing { get; set; } = "<color=#32CD32>Repairing base...</color>";
            public string RepairComplete { get; set; } = "<color=#32CD32>Repair complete. Repaired {0} out of {1} entities.</color>";
            public string MissingMaterials { get; set; } = "<color=#FF4500>Missing: {0}.</color>";
        }

        private sealed class RepairMaterialsConfig
        {
            public bool Wood { get; set; } = false;
            public bool Stone { get; set; } = false;
            public bool Metal { get; set; } = true;
            public bool HqMetal { get; set; } = true;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>() ?? new Configuration();
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = new Configuration();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        #endregion

        #region Permissions

        private const string UsePermission = "basefixer.use";
        private const string NoPlayerTaxPermission = "basefixer.noplayertax";

        #endregion

        #region Variables

        private Dictionary<string, float> lastRepairTime = new Dictionary<string, float>();

        #endregion

        #region Hooks

        private void Init()
        {
            permission.RegisterPermission(UsePermission, this);
            permission.RegisterPermission(NoPlayerTaxPermission, this);

            AddCovalenceCommand(_config.Command, nameof(ToggleBaseFixer));
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, UsePermission))
            {
                permission.GrantUserPermission(player.UserIDString, UsePermission, this);
            }
        }

        [Command("br")]
        private void ToggleBaseFixer(IPlayer player, string command, string[] args)
        {
            var basePlayer = player.Object as BasePlayer;
            if (basePlayer == null) return;

            if (!HasPermission(basePlayer, UsePermission))
            {
                player.Message(_config.Messages.NoPermission);
                return;
            }

            RepairBase(basePlayer);
        }

        #endregion

        #region Base Repair

        private void RepairBase(BasePlayer player)
        {
            var tc = player.GetBuildingPrivilege();
            if (tc == null)
            {
                player.ChatMessage(_config.Messages.NotInRange);
                return;
            }

            if (lastRepairTime.ContainsKey(player.UserIDString) && Time.realtimeSinceStartup - lastRepairTime[player.UserIDString] < _config.DamageRepairCooldown)
            {
                player.ChatMessage(string.Format(_config.Messages.Cooldown, _config.DamageRepairCooldown));
                return;
            }

            var entities = new List<BaseEntity>();
            Vis.Entities(tc.transform.position, 50f, entities);

            // Filter damaged entities
            var damagedEntities = new List<BaseEntity>();
            foreach (var entity in entities)
            {
                if (entity is BaseCombatEntity combatEntity && combatEntity.health < combatEntity.MaxHealth())
                {
                    damagedEntities.Add(entity);
                }
            }

            if (damagedEntities.Count == 0)
            {
                player.ChatMessage(_config.Messages.NoDamagedEntities);
                return;
            }

            if (damagedEntities.Count > _config.GlobalRepairLimit)
            {
                player.ChatMessage($"<color=#FF4500>Cannot repair more than {_config.GlobalRepairLimit} entities at once.</color>");
                return;
            }

            int entitiesPerSecond = GetDynamicEntitiesPerSecond();
            var totalEntities = damagedEntities.Count;
            var repairedEntities = 0;

            // Calculate repair costs
            int totalWoodCost = 0;
            int totalStoneCost = 0;
            int totalMetalCost = 0;
            int totalHqMetalCost = 0;

            foreach (var entity in damagedEntities)
            {
                if (entity is BuildingBlock buildingBlock)
                {
                    switch (buildingBlock.grade)
                    {
                        case BuildingGrade.Enum.Wood:
                            totalWoodCost += (int)(buildingBlock.MaxHealth() - buildingBlock.health) / 10 / _config.RepairCostMultiplier;
                            break;
                        case BuildingGrade.Enum.Stone:
                            totalStoneCost += (int)(buildingBlock.MaxHealth() - buildingBlock.health) / 10 / _config.RepairCostMultiplier;
                            break;
                        case BuildingGrade.Enum.Metal:
                            totalMetalCost += (int)(buildingBlock.MaxHealth() - buildingBlock.health) / 10 / _config.RepairCostMultiplier;
                            break;
                        case BuildingGrade.Enum.TopTier:
                            totalHqMetalCost += (int)(buildingBlock.MaxHealth() - buildingBlock.health) / 10 / _config.RepairCostMultiplier;
                            break;
                    }
                }
            }

            // Check player inventory for materials
            var playerInventory = player.inventory;

            if (!permission.UserHasPermission(player.UserIDString, NoPlayerTaxPermission))
            {
                int playerWoodAmount = playerInventory.GetAmount(-151838493);
                int playerStoneAmount = playerInventory.GetAmount(-2099697608);
                int playerMetalAmount = playerInventory.GetAmount(69511070);
                int playerHqMetalAmount = playerInventory.GetAmount(317398316);

                bool hasEnoughMaterials = true;
                List<string> missingMaterials = new List<string>();

                if (_config.RepairMaterials.Wood && playerWoodAmount < totalWoodCost)
                {
                    missingMaterials.Add($"{totalWoodCost - playerWoodAmount} wood");
                    hasEnoughMaterials = false;
                }
                if (_config.RepairMaterials.Stone && playerStoneAmount < totalStoneCost)
                {
                    missingMaterials.Add($"{totalStoneCost - playerStoneAmount} stone");
                    hasEnoughMaterials = false;
                }
                if (_config.RepairMaterials.Metal && playerMetalAmount < totalMetalCost)
                {
                    missingMaterials.Add($"{totalMetalCost - playerMetalAmount} metal fragments");
                    hasEnoughMaterials = false;
                }
                if (_config.RepairMaterials.HqMetal && playerHqMetalAmount < totalHqMetalCost)
                {
                    missingMaterials.Add($"{totalHqMetalCost - playerHqMetalAmount} high-quality metal");
                    hasEnoughMaterials = false;
                }

                if (!hasEnoughMaterials)
                {
                    player.ChatMessage(string.Format(_config.Messages.MissingMaterials, string.Join(", ", missingMaterials)));
                    return;
                }

                // Deduct materials from player inventory
                if (_config.RepairMaterials.Wood && totalWoodCost > 0)
                    playerInventory.Take(null, -151838493, totalWoodCost);
                if (_config.RepairMaterials.Stone && totalStoneCost > 0)
                    playerInventory.Take(null, -2099697608, totalStoneCost);
                if (_config.RepairMaterials.Metal && totalMetalCost > 0)
                    playerInventory.Take(null, 69511070, totalMetalCost);
                if (_config.RepairMaterials.HqMetal && totalHqMetalCost > 0)
                    playerInventory.Take(null, 317398316, totalHqMetalCost);
            }

            lastRepairTime[player.UserIDString] = Time.realtimeSinceStartup;

            timer.Repeat(1f / entitiesPerSecond, totalEntities, () =>
            {
                if (damagedEntities.Count == 0) return;

                var entity = damagedEntities[0];
                damagedEntities.RemoveAt(0);

                // Check if the entity is a construction entity
                if (entity is BuildingBlock buildingBlock)
                {
                    buildingBlock.health = buildingBlock.MaxHealth();
                    buildingBlock.SendNetworkUpdate();
                    repairedEntities++;
                }
                // Check if the entity is a deployable (like doors, boxes, etc.)
                else if (entity is BaseCombatEntity combatEntity)
                {
                    combatEntity.health = combatEntity.MaxHealth();
                    combatEntity.SendNetworkUpdate();
                    repairedEntities++;
                }
            });

            player.ChatMessage(_config.Messages.Repairing);
            timer.Once(totalEntities / (float)entitiesPerSecond, () =>
            {
                player.ChatMessage(string.Format(_config.Messages.RepairComplete, repairedEntities, totalEntities));
            });
        }

        private int GetDynamicEntitiesPerSecond()
        {
            // Placeholder logic: return a fixed value for now
            float currentLoad = UnityEngine.SystemInfo.processorCount; // Placeholder: Get actual server load metrics
            int adjustedEntitiesPerSecond = Mathf.Clamp((int)(_config.EntitiesPerSecond * (1.0f / currentLoad)), 1, _config.EntitiesPerSecond);
            return adjustedEntitiesPerSecond;
        }

        #endregion

        #region Helpers

        private bool HasPermission(BasePlayer player, string perm)
        {
            return permission.UserHasPermission(player.UserIDString, perm);
        }

        #endregion
    }
}
