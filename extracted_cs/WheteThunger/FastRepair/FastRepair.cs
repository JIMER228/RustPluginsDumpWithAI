using System;

namespace Oxide.Plugins
{
    [Info("Fast Repair", "WhiteThunder", "1.0.0")]
    [Description("Repairs entities faster and for free.")]
    internal class FastRepair : CovalencePlugin
    {
        private const float RepairFraction = 0.1f;
        private const float MinRepairAmount = 100;

        private object OnStructureRepair(BaseCombatEntity entity, BasePlayer player)
        {
            if (!entity.repair.enabled)
                return false;

            if (entity.SecondsSinceAttacked <= 30)
                return null;

            var healthMissing = entity.MaxHealth() - entity.Health();
            var healthMissingFraction = 1 - entity.healthFraction;
            if (healthMissingFraction <= 0)
                return null;

            if (entity.BuildCost() == null)
                return null;

            var repairAmount = Math.Max(entity.MaxHealth() * RepairFraction, MinRepairAmount);
            repairAmount = Math.Min(repairAmount, healthMissing);

            entity.health += repairAmount;
            entity.SendNetworkUpdate();

            if (entity.Health() >= entity.MaxHealth())
                entity.OnRepairFinished();
            else
                entity.OnRepair();

            return false;
        }
    }
}
