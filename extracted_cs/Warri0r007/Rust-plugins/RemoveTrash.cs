using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("RemoveTrash", "Инкуб/WOLF SPIRIT", "1.3.3")]
    class RemoveTrash : RustPlugin
    {
        private Configuration config;

        class Configuration
        {
            public string Version { get; set; } = "1.3.2";
            public float DistanceRadius { get; set; } = 10f;
            public float IntervalCheck { get; set; } = 60f;
            public Position PositionDeletions { get; set; } = new Position();
            public string[] Exclude { get; set; } = new string[] { "player_corpse", "stash.small", "box.wooden", "box.wooden.large", "rowboat_storage", "workbench1.deployed", "workbench2.deployed", "workbench3.deployed", "rocket_mlrs" };
            public bool DeleteNPC { get; set; } = false;
            public bool DeletePlayers { get; set; } = false;
        }

        class Position
        {
            public float x { get; set; } = 0f;
            public float y { get; set; } = 0f;
            public float z { get; set; } = 0f;
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file");
            config = new Configuration();
            SaveConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null || config.Version != Version.ToString())
                {
                    PrintWarning("The configuration file does not match the current version, update...");
                    LoadDefaultConfig();
                }
            }
            catch
            {
                PrintWarning("Configuration file is corrupted, create a new one");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(config, true);

        private void OnServerInitialized()
        {
            PrintWarning("\n-----------------------------------------------------------------------------------------\n" +
            "     Loading plugin...\n" +
            "     oxide-russia.ru...\n" +
            "     Enjoy your use!....\n" +
            "-----------------------------------------------------------------------------------------");
            timer.Every(config.IntervalCheck, RemoveObjects);
        }

        private void RemoveObjects()
        {
            var colliders = Physics.OverlapSphere(new Vector3(config.PositionDeletions.x, config.PositionDeletions.y, config.PositionDeletions.z), config.DistanceRadius);
            int removedCount = 0;
            HashSet<string> exclusionSet = new HashSet<string>(config.Exclude);

            foreach (var collider in colliders)
            {
                var entity = collider.GetComponentInParent<BaseEntity>();
                if (entity != null && !entity.IsDestroyed && !(entity is BasePlayer))
                {
                    string entityName = entity.ShortPrefabName;
                    if (!exclusionSet.Contains(entityName) && (config.DeleteNPC || !(entity is NPCPlayer)) && (config.DeletePlayers || !(entity is BasePlayer)))
                    {
                        float distance = Vector3.Distance(new Vector3(config.PositionDeletions.x, config.PositionDeletions.y, config.PositionDeletions.z), entity.transform.position);
                        if (!entity.IsDestroyed)
                        {
                            entity.Kill();
                            removedCount++;
                            string removalMessage = $"Deleted {entityName} at a distance of {distance}m from position {config.PositionDeletions.x}, {config.PositionDeletions.y}, {config.PositionDeletions.z}";
                            PrintWarning(removalMessage);
                            LogToFile("removeTrash.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]: {removalMessage}", this);
                        }
                    }
                }
            }

            if (removedCount > 0)
            {
                string logMessage = $"Deleted {removedCount} objects within {config.DistanceRadius}m of position {config.PositionDeletions.x}, {config.PositionDeletions.y}, {config.PositionDeletions.z}.";
                PrintWarning(logMessage);
                LogToFile("removeTrash.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]: {logMessage}", this);
            }
        }
    }
}
