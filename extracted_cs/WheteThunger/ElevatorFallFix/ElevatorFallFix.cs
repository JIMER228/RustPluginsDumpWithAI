using System.Linq;
using System.Reflection;

namespace Oxide.Plugins
{
    [Info("Elevator Fall Fix", "WhiteThunder", "0.1.0")]
    [Description("Fixes players falling through elevators.")]
    internal class ElevatorFallFix : CovalencePlugin
    {
        private readonly FieldInfo DoClippingCheckField = typeof(TriggerParent).GetField("doClippingCheck", BindingFlags.NonPublic | BindingFlags.Instance);

        private void OnServerInitialized()
        {
            foreach (var lift in BaseNetworkable.serverEntities.OfType<ElevatorLift>())
            {
                OnEntitySpawned(lift);
            }
        }

        private void OnEntitySpawned(ElevatorLift lift)
        {
            if (lift is ElevatorLiftStatic)
                return;

            var parentTrigger = lift.GetComponentInChildren<TriggerParent>();
            if (parentTrigger == null)
                return;

            DoClippingCheckField.SetValue(parentTrigger, false);
        }
        
        private void Unload()
        {
            foreach (var lift in BaseNetworkable.serverEntities.OfType<ElevatorLift>())
            {
                if (lift is ElevatorLiftStatic)
                    continue;

                var parentTrigger = lift.GetComponentInChildren<TriggerParent>();
                if (parentTrigger == null)
                    continue;

                DoClippingCheckField.SetValue(parentTrigger, true);
            }
        }
    }
}
