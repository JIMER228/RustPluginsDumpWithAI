using Oxide.Core;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("NoPowerChristmasLights", "RustFlash", "1.0.2")]
    [Description("Allows you to use Christmas lights without a power connection")]
    public class NoPowerXmasLights : RustPlugin
    {
        private readonly object False = false;
        private readonly object Zero = 0;

        void OnServerInitialized()
        {
            UpdateAllLights(999999);
        }

        void OnEntitySpawned(AdvancedChristmasLights lightString)
        {
            if (lightString == null) return;
           
            NextTick(() => {
                lightString.UpdateHasPower(999999, 1);
                lightString.currentEnergy = 0;
                lightString.SetFlag(BaseEntity.Flags.On, true);
                lightString.SetFlag(BaseEntity.Flags.Reserved8, true);
            });
        }

        private void UpdateAllLights(int power)
        {
            foreach (var lightString in BaseNetworkable.serverEntities.OfType<AdvancedChristmasLights>())
            {
                if (lightString != null)
                {
                    lightString.UpdateHasPower(power, 1);
                    lightString.currentEnergy = 0;
                    lightString.SetFlag(BaseEntity.Flags.On, true);
                    lightString.SetFlag(BaseEntity.Flags.Reserved8, true);
                }
            }
        }

        object OnConsumptionAmount(AdvancedChristmasLights lightString)
        {
            return Zero;
        }

        object OnRequiresPower(AdvancedChristmasLights lightString)
        {
            return False;
        }

        object OnMaximalPowerOutput(IOEntity entity)
        {
            if (entity is AdvancedChristmasLights)
            {
                return 0;
            }
            return null;
        }
        
        void Unload()
        {
            UpdateAllLights(0);
        }
    }
}