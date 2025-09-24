using Oxide.Game.Rust;

namespace Oxide.Plugins
{
    [Info("CupboardAway", "Wulf/lukespragg", "0.1.0", ResourceId = 0)]
    [Description("Makes cupboard protection available only if the owner is offline")]

    class CupboardAway : RustPlugin
    {
        bool OnCupboardAuthorize(BuildingPrivlidge cupboard) => CanAuthorize(cupboard);

        bool OnCupboardDeauthorize(BuildingPrivlidge cupboard) => CanAuthorize(cupboard);

        void OnEntityEnter(TriggerBase trigger, BaseEntity entity)
        {
            var player = entity as BasePlayer;
            var cupboard = trigger as BuildPrivilegeTrigger;
            if (player == null || cupboard == null) return;

            var owner = cupboard.privlidgeEntity.authorizedPlayers[0];
            var ownerPlayer = RustCore.FindPlayerById(owner.userid);

            if (ownerPlayer != null && ownerPlayer.isConnected)
                timer.Once(0.1f, () => player.SetPlayerFlag(BasePlayer.PlayerFlags.HasBuildingPrivilege, true));
        }

        static bool CanAuthorize(BuildingPrivlidge cupboard)
        {
            var owner = cupboard.authorizedPlayers[0];
            var ownerPlayer = RustCore.FindPlayerById(owner.userid);

            return ownerPlayer?.isConnected ?? true;
        }
    }
}
