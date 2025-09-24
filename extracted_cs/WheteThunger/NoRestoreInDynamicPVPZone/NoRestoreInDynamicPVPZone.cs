using Oxide.Core.Plugins;

namespace Oxide.Plugins;

[Info("No Restore In Dynamic PVP Zone", "WhiteThunder", "1.0.1")]
[Description("Prevents player inventories from being restored after dying in a Dynamic PVP zone.")]
internal class NoRestoreInDynamicPVPZone : CovalencePlugin
{
    private readonly object False = false;

    [PluginReference]
    private Plugin DynamicPVP, ZoneManager;

    // This hook is exposed by Restore Upon Death.
    // It is called at the following times.
    // - After the player has died and their inventory is about to be saved.
    // - After the player has respawned and their saved inventory is about to be restored.
    private object OnRestoreUponDeath(BasePlayer player)
    {
        if (player.IsAlive())
        {
            // Called after respawn, so do nothing.
            // We don't care if the player happens to respawn into a dynamic PVP zone.
            return null;
        }

        if (DynamicPVP?.Call("IsPlayerInPVPDelay", (ulong)player.userID) is true)
        {
            // Player recently exited a PVP zone and can still be killed.
            return False;
        }

        if (ZoneManager?.Call("GetPlayerZoneIDs", player) is not string[] zoneIdList)
        {
            // Player is not in a zone, or Zone Manager is not loaded.
            return null;
        }

        foreach (var zoneId in zoneIdList)
        {
            if (DynamicPVP?.Call("IsDynamicPVPZone", zoneId) is true)
            {
                // Player is in a dynamic PVP zone, so block saving their inventory.
                return False;
            }
        }

        return null;
    }
}