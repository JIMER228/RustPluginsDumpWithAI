namespace Oxide.Plugins
{
    [Info("NoDurability", "Wulf/lukespragg", "2.0.0", ResourceId = 0)]
    [Description("Allows players with permission to have no durability on every item")]

    class NoDurability : CovalencePlugin
    {
        #region Initialization

        const string permAllowed = "nodurability.allowed";

        void Loaded()
        {
            #if !RUST
            throw new NotSupportedException("This plugin does not support this game");
            #endif

            permission.RegisterPermission(permAllowed, this);
        }

        #endregion

        #region Durability Control

        #if RUST
        void OnLoseCondition(Item item, ref float amount)
        {
            var player = item?.GetOwnerPlayer();
            if (player == null) return;
            if (HasPermission(player.UserIDString, permAllowed)) item.condition = item.maxCondition;
            //Puts($"{item.info.shortname} was damaged by: {amount} | Condition is: {item.condition}/{item.maxCondition}");
        }
        #endif

        #endregion

        #region Helpers

        bool HasPermission(string id, string perm) => permission.UserHasPermission(id, perm);

        #endregion
    }
}
