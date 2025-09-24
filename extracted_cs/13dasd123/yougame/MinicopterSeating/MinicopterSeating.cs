// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using UnityEngine;
using System;

namespace Oxide.Plugins
{
    [Info("Minicopter Seating", "Bazz3l", "1.1.1")]
    [Description("Spawns an extra seat each side of the minicopter.")]
    class MinicopterSeating : RustPlugin
    {
        #region Fields
        SeatingManager _manager = new SeatingManager();
        #endregion

        #region Oxide
        void OnEntitySpawned(MiniCopter mini)
        {
            if (mini.mountPoints.Length < 3 && mini.ShortPrefabName == "minicopter.entity")
            {
                _manager.Setup((BaseVehicle) mini);
            }
        }
        #endregion

        #region SeatingManger
        class SeatingManager
        {
            const string _chairPrefab = "assets/prefabs/vehicle/seats/passengerchair.prefab";

            public void Setup(BaseVehicle vehicle)
            {
                BaseVehicle.MountPointInfo pilot = vehicle.mountPoints[0];
                BaseVehicle.MountPointInfo passenger = vehicle.mountPoints[1];

                Array.Resize(ref vehicle.mountPoints, 3);

                vehicle.mountPoints[0] = pilot;
                vehicle.mountPoints[1] = passenger;
                vehicle.mountPoints[2] = MakeMount(vehicle, new Vector3(0.0f, 0.4f, -1.35f));


                MakeSeat(vehicle, new Vector3(0.0f, 0.4f, -1.35f));

            }

            void MakeSeat(BaseVehicle vehicle, Vector3 position)
            {
                BaseEntity entity = GameManager.server.CreateEntity(_chairPrefab, vehicle.transform.position);
                if (entity == null)
                {
                    return;
                }

                entity.SetParent(vehicle);
                entity.Spawn();
                entity.transform.localPosition = position;
                entity.SendNetworkUpdateImmediate(true);
            }

            BaseVehicle.MountPointInfo MakeMount(BaseVehicle vehicle, Vector3 position)
            {
                return new BaseVehicle.MountPointInfo
                {
                    pos       = position,
                    rot       = vehicle.mountPoints[1].rot,
                    prefab    = vehicle.mountPoints[1].prefab,
                    mountable = vehicle.mountPoints[1].mountable,
                };
            }
        }
        #endregion
    }
}