namespace Oxide.Plugins
{
    [Info("UnlimitedFuel", "headtapper", "1.0.0")]
    [Description("Unlimited fuel in minicopters and all other vehicles.")]

    public class UnlimitedFuel : RustPlugin
    {
        private object OnFuelCheck(EntityFuelSystem fuelSystem) => true;
    }
}
