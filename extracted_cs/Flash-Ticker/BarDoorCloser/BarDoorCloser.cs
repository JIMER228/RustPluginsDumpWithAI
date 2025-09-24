using Oxide.Core;
using Oxide.Core.Configuration;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BarDoorCloser", "RustFlash", "1.2.0")]
    class BarDoorCloser : RustPlugin
    {
        private ConfigData configData;
        private const string BarDoorPrefab = "assets/prefabs/misc/decor_dlc/bardoors/door.double.hinged.bardoors.prefab";

        class ConfigData
        {
            public float DoorCloseDelay = 2.3f;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                configData = Config.ReadObject<ConfigData>();
            }
            catch
            {
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            configData = new ConfigData();
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void OnDoorOpened(Door door, BasePlayer player)
        {
            if (door == null || string.IsNullOrEmpty(door.PrefabName))
            {
                return;
            }

            if (string.Equals(door.PrefabName, BarDoorPrefab, System.StringComparison.Ordinal))
            {
                timer.Once(configData.DoorCloseDelay, () => CloseDoor(door));
            }
        }

        private void CloseDoor(Door door)
        {
            if (door == null || !door.IsValid() || door.net == null)
            {
                return;
            }

            door.SetFlag(BaseEntity.Flags.Open, false);
            door.SendNetworkUpdateImmediate();
        }

        private void Init()
        {
            if (configData == null)
            {
                LoadDefaultConfig();
            }
        }
    }
}