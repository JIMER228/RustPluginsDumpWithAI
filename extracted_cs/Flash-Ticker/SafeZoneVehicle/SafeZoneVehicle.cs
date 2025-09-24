using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SafeZoneVehicle", "RustFlash", "1.0.0")]
    [Description("Safezone settings for vehicles")]

    public class SafeZoneVehicle : RustPlugin
    {
        private bool disableDamageInSafezone = true;
        private bool disableVehiclePushWithPassengerInSafezone = true;
        private const string pluginPermission = "safezonevehicle.use";

        protected override void LoadDefaultConfig()
        {
            Config["DisableDamageInSafezone"] = disableDamageInSafezone.ToString();
            Config["DisableVehiclePushWithPassengerInSafezone"] = disableVehiclePushWithPassengerInSafezone.ToString();
            SaveConfig();
        }

        private void Init()
        {
            LoadConfigValues();
            permission.RegisterPermission(pluginPermission, this);
        }

        private void LoadConfigValues()
        {
            disableDamageInSafezone = Config.Get<bool>("DisableDamageInSafezone");
            disableVehiclePushWithPassengerInSafezone = Config.Get<bool>("DisableVehiclePushWithPassengerInSafezone");
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity is BaseVehicle)
            {
                if (IsEntityInSafezone(entity) && disableDamageInSafezone)
                {
                    hitInfo.damageTypes.ScaleAll(0);
                }
            }
        }

        private bool IsEntityInSafezone(BaseEntity entity)
        {
            foreach (TriggerSafeZone safezone in TriggerSafeZone.allSafeZones)
            {
                if (safezone.triggerCollider.bounds.Contains(entity.transform.position))
                {
                    return true;
                }
            }
            return false;
        }

        private object OnVehiclePush(BaseVehicle vehicle, BasePlayer player)
        {
            if (disableVehiclePushWithPassengerInSafezone && IsEntityInSafezone(vehicle) && vehicle.AnyMounted())
            {
                return false;
            }
            return null;
        }

        private void OnEntityMounted(BaseVehicle vehicle, BasePlayer player)
        {
            if (IsEntityInSafezone(vehicle))
            {
                vehicle.health = vehicle.MaxHealth();
            }
        }
    }
}
