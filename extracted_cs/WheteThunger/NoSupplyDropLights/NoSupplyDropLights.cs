using System.Linq;

namespace Oxide.Plugins
{
    [Info("No Supply Drop Lights", "WhiteThunder", "1.0.0")]
    [Description("Disables lights on supply drops.")]
    internal class NoSupplyDropLights : CovalencePlugin
    {
        private void OnServerInitialized()
        {
            foreach (var supplyDrop in BaseNetworkable.serverEntities.OfType<SupplyDrop>())
            {
                OnEntitySpawned(supplyDrop);
                supplyDrop.SetFlag(SupplyDrop.FlagNightLight, false);
            }
        }

        private void OnEntitySpawned(SupplyDrop supplyDrop)
        {
            supplyDrop.CancelInvoke(supplyDrop.CheckNightLight);
        }
    }
} 
