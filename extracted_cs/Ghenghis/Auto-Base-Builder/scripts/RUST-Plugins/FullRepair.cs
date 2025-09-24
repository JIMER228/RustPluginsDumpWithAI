using UnityEngine;
using System.Linq;
using Oxide.Core;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Full Repair", "Death", "1.0.2")]
    class FullRepair : RustPlugin
    {
        #region Declarations
        const string perm = "fullrepair.use";
        #endregion

        #region Hooks
        // Disable condition loss for each item definition globally at startup.
        void OnServerInitialized()
        {
            LoadConfig();
            permission.RegisterPermission(perm, this);

            if (options.Settings.UsePermission)
            {
                return;
            }

            List<ItemDefinition> Definitions = new List<ItemDefinition>(ItemManager.GetItemDefinitions());

            foreach (var definition in Definitions)
            {
                definition.condition.maintainMaxCondition = true;
            }
        }

        void Unload()
        {
            List<ItemDefinition> Definitions = new List<ItemDefinition>(ItemManager.GetItemDefinitions());

            foreach (var definition in Definitions)
            {
                definition.condition.maintainMaxCondition = false;
            }
        }

        void OnItemRepair(BasePlayer player, Item item)
        {
            if (!permission.UserHasPermission(player.UserIDString, perm))
            {
                return;
            }

            NextTick(() =>
            {
                if (item.conditionNormalized != 1f)
                {
                    return;
                }

                item.maxCondition = item.info.condition.max;
                item.condition = item.maxCondition;

                item.GetHeldEntity()?.SetFlag(BaseEntity.Flags.Broken, false);
                item.MarkDirty();
            });
        }
        #endregion

        #region Functions

        #region Config
        ConfigFile options;

        class ConfigFile
        {
            public PluginSettings Settings = new PluginSettings();
        }

        class PluginSettings
        {
            public bool UsePermission = false;
        }

        void LoadDefaultConfig()
        {
            var config = new ConfigFile();

            SaveConfig(config);
        }

        void LoadConfig()
        {
            options = Config.ReadObject<ConfigFile>();
            SaveConfig(options);
        }

        void SaveConfig(ConfigFile config)
        {
            Config.WriteObject(config, true);
        }
        #endregion

        #endregion
    }
} 