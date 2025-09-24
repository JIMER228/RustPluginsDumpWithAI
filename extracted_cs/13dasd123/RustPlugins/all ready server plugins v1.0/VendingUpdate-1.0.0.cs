using UnityEngine;

namespace Oxide.Plugins
{
    [Info("VendingUpdate", "playermodel", "1.0.0")]
    public class VendingUpdate : RustPlugin
    {
        void OnOpenVendingShop(VendingMachine machine, BasePlayer player)
        {
            machine.PostServerLoad();
            machine.UpdateMapMarker();
            machine.SendNetworkUpdate();
        }
    }
}