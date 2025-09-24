using System;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Fix Unmountable Cars", "Substrata", "1.0.0")]
    [Description("Fix Unmountable Cars")]

    class FixUnmountableCars : RustPlugin
    {
        [ChatCommand("fixcars")]
		void cmdFixCars(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            FixCars();
        }

        void OnServerInitialized(bool initial)
        {
            if (initial) FixCars();
        }

        void FixCars()
        {
            foreach (var ent in BaseNetworkable.serverEntities)
            {
                if (ent is ModularCar) FixCar((ent as BaseVehicle));
            }
        }

        void FixCar(BaseVehicle vehicle)
        {
            if (vehicle == null || vehicle.IsDestroyed || vehicle.AnyMounted()) return;
            vehicle.DismountAllPlayers();
            vehicle.SetFlag(BaseEntity.Flags.Reserved11, false);
        }
    }
}