// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Nerfed Raid Building", "VisEntities", "1.0.0")]
    [Description("Prevents entities placed during raids from starting at full health, gradually healing them afterward.")]
    public class NerfedRaidBuilding : RustPlugin
    {
        #region 3rd Party Dependencies

        [PluginReference]
        private readonly Plugin BetterNoEscape;

        #endregion 3rd Party Dependencies

        #region Fields

        private static NerfedRaidBuilding _plugin;
        private static Configuration _config;
        private static readonly HashSet<NerfComponent> _nerfedEntities = new HashSet<NerfComponent>();

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Initial Health Percent On Placement (0-100)")]
            public float InitialHealthPercentOnPlacement { get; set; }

            [JsonProperty("Upgrade Health Percent (0-100)")]
            public float UpgradeHealthPercent { get; set; }

            [JsonProperty("Seconds Since Last Damage Before Healing Starts")]
            public float SecondsSinceLastDamageBeforeHealingStarts { get; set; }

            [JsonProperty("Heal Frequency Seconds")]
            public float HealFrequencySeconds { get; set; }

            [JsonProperty("Percent Healed Per Tick (0-100)")]
            public float PercentHealedPerTick { get; set; }

            [JsonProperty("Entity Keyword Whitelist (prefab or type substring)")]
            public List<string> EntityKeywordWhitelist { get; set; }

            [JsonProperty("Entity Keyword Blacklist (prefab or type substring)")]
            public List<string> EntityKeywordBlacklist { get; set; }
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
                InitialHealthPercentOnPlacement = 25f,
                UpgradeHealthPercent = 50f,
                SecondsSinceLastDamageBeforeHealingStarts = 90f,
                HealFrequencySeconds = 3f,
                PercentHealedPerTick = 2f,
                EntityKeywordWhitelist = new List<string>
                {
                    "building",
                    "door",
                    "wall",
                    "gate"
                },
                EntityKeywordBlacklist = new List<string>()
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
            PermissionUtil.RegisterPermissions();
        }

        private void Unload()
        {
            foreach (NerfComponent nerfedEntity in _nerfedEntities.ToArray())
            {
                if (nerfedEntity != null)
                    nerfedEntity.DestroySelf();
            }
            _nerfedEntities.Clear();

            _config = null;
            _plugin = null;
        }

        private void OnServerInitialized(bool isStartup)
        {
            if (!CheckDependencies())
            {
                timer.Once(1f, () => Interface.Oxide.UnloadPlugin(Name));
                return;
            }
        }

        private void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            if (planner == null || gameObject == null)
                return;

            BasePlayer player = planner.GetOwnerPlayer();
            if (player == null)
                return;

            if (PermissionUtil.HasPermission(player, PermissionUtil.BYPASS))
                return;

            BaseEntity entity = gameObject.ToBaseEntity();
            if (entity == null)
                return;

            if (!BetterNoEscapeUtil.IsRaidBlocked(player))
                return;

            TryNerf(entity, isNew: true);
        }

        private void OnStructureUpgrade(BuildingBlock buildingBlock, BasePlayer player, BuildingGrade.Enum grade)
        {
            if (buildingBlock == null || player == null)
                return;

            if (PermissionUtil.HasPermission(player, PermissionUtil.BYPASS))
                return;

            if (!BetterNoEscapeUtil.IsRaidBlocked(player))
                return;

            TryNerf(buildingBlock, isNew: false);
        }

        #endregion Oxide Hooks

        #region Core

        private void TryNerf(BaseEntity entity, bool isNew)
        {
            if (entity == null)
                return;

            if (!DoesEntityPassKeywordFilters(entity, _config.EntityKeywordWhitelist, _config.EntityKeywordBlacklist))
                return;

            NextTick(() =>
            {
                if (entity != null)
                    NerfComponent.Install(entity, isNew);
            });
        }

        #endregion Core

        #region Entity Filter

        private static bool DoesEntityPassKeywordFilters(BaseEntity entity, IReadOnlyCollection<string> whitelist, IReadOnlyCollection<string> blacklist)
        {
            if (entity == null)
                return false;

            string prefabName = string.Empty;
            if (entity.ShortPrefabName != null)
                prefabName = entity.ShortPrefabName.ToLowerInvariant();

            string typeName = entity.GetType().Name.ToLowerInvariant();

            if (whitelist != null && whitelist.Count > 0)
            {
                bool matchedWhitelist = false;

                foreach (string rawKeyword in whitelist)
                {
                    if (string.IsNullOrEmpty(rawKeyword))
                        continue;

                    string keyword = rawKeyword.ToLowerInvariant();

                    if (prefabName.Contains(keyword) || typeName.Contains(keyword))
                    {
                        matchedWhitelist = true;
                        break;
                    }
                }

                if (!matchedWhitelist)
                    return false;
            }

            if (blacklist != null && blacklist.Count > 0)
            {
                foreach (string rawKeyword in blacklist)
                {
                    if (string.IsNullOrEmpty(rawKeyword))
                        continue;

                    string keyword = rawKeyword.ToLowerInvariant();

                    if (prefabName.Contains(keyword) || typeName.Contains(keyword))
                        return false;
                }
            }

            return true;
        }

        #endregion Entity Filter

        #region Nerf Component

        public class NerfComponent : FacepunchBehaviour
        {
            private BaseCombatEntity _entity;

            public static NerfComponent Install(BaseEntity entity, bool isPlacement)
            {
                NerfComponent nerfedEntity = entity.gameObject.GetComponent<NerfComponent>();
                if (nerfedEntity == null)
                {
                    nerfedEntity = entity.gameObject.AddComponent<NerfComponent>();
                    nerfedEntity.Initialize(isPlacement);
                    _nerfedEntities.Add(nerfedEntity);
                }
                else
                {
                    nerfedEntity.Refresh(isPlacement);
                }
                return nerfedEntity;
            }

            public static NerfComponent GetComponent(BaseEntity entity)
            {
                return entity.gameObject.GetComponent<NerfComponent>();
            }

            private void Initialize(bool isPlacement)
            {
                _entity = GetComponent<BaseCombatEntity>();
                ApplyInitialHealth(isPlacement);
                RestartHealTimer();
            }

            public void DestroySelf()
            {
                DestroyImmediate(this);
            }

            private void OnDestroy()
            {
                _nerfedEntities.Remove(this);
                CancelInvoke();
            }

            private void Refresh(bool isPlacement)
            {
                ApplyInitialHealth(isPlacement);
                RestartHealTimer();
            }

            private void HealTick()
            {
                if (_entity == null || !_entity.IsValid())
                {
                    DestroySelf();
                    return;
                }

                if (_entity.SecondsSinceAttacked < _config.SecondsSinceLastDamageBeforeHealingStarts)
                    return;

                if (Mathf.Approximately(_entity.Health(), _entity.MaxHealth()))
                {
                    DestroySelf();
                    return;
                }

                float amount = _entity.MaxHealth() * _config.PercentHealedPerTick / 100f;
                _entity.Heal(amount);
            }

            private void ApplyInitialHealth(bool isPlacement)
            {
                float percentage = _config.UpgradeHealthPercent;
                if (isPlacement)
                {
                    percentage = _config.InitialHealthPercentOnPlacement;
                }

                float hp = Mathf.Clamp(_entity.MaxHealth() * percentage / 100f, 1f, _entity.MaxHealth());
                _entity.health = hp;
            }

            private void RestartHealTimer()
            {
                CancelInvoke();
                InvokeRepeating(nameof(HealTick), _config.SecondsSinceLastDamageBeforeHealingStarts, _config.HealFrequencySeconds);
            }
        }

        #endregion Nerf Component

        #region Helper Functions

        private bool CheckDependencies()
        {
            if (!PluginLoaded(BetterNoEscape))
            {
                Puts("Better No Escape is not loaded. Download it from https://game4freak.io.");
                return false;
            }
            return true;
        }

        private static bool PluginLoaded(Plugin plugin)
        {
            if (plugin != null && plugin.IsLoaded)
                return true;
            else
                return false;
        }

        #endregion Helper Functions

        #region 3rd Party Integration

        public static class BetterNoEscapeUtil
        {
            private static bool Loaded
            {
                get
                {
                    return _plugin != null &&
                           _plugin.BetterNoEscape != null &&
                           _plugin.BetterNoEscape.IsLoaded;
                }
            }

            public static bool IsRaidBlocked(BasePlayer player)
            {
                if (!Loaded || player == null)
                    return false;

                return _plugin.BetterNoEscape.Call<bool>("API_IsRaidBlocked", player);
            }
        }

        #endregion 3rd Party Integration

        #region Permissions

        private static class PermissionUtil
        {
            public const string BYPASS = "nerfedraidbuilding.bypass";
            private static readonly List<string> _permissions = new List<string>
            {
                BYPASS,
            };

            public static void RegisterPermissions()
            {
                foreach (var permission in _permissions)
                {
                    _plugin.permission.RegisterPermission(permission, _plugin);
                }
            }

            public static bool HasPermission(BasePlayer player, string permissionName)
            {
                return _plugin.permission.UserHasPermission(player.UserIDString, permissionName);
            }
        }

        #endregion Permissions
    }
}