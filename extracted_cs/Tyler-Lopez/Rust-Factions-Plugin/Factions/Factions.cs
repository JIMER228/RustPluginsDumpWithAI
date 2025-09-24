// Requires: ZoneManager
namespace Oxide.Plugins
{
    [Info("Factions", "Rust Factions", "1.0.0")]
    [Description("Pending Description")]
    public partial class Factions : RustPlugin
    {
        #region Plugin Declaration and Global Variables
        private IZoneManagerRepository _zoneManagerRepository;
        private FactionsMapMarkerManager _factionsMapMarkerManager;
        #endregion
    }
}
