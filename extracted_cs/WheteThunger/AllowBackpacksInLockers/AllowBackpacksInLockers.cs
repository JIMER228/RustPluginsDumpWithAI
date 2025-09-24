namespace Oxide.Plugins
{
    [Info("Allow Backpacks in Lockers", "WhiteThunder", "1.0.0")]
    [Description("Allows players to store non-empty backpacks in lockers.")]
    internal class AllowBackpacksInLockers : CovalencePlugin
    {
        private static readonly object CanAccept = ItemContainer.CanAcceptResult.CanAccept;

        private object CanAcceptItem(ItemContainer container, Item item, int targetPosition)
        {
            var locker = container.entityOwner as Locker;
            if (locker == null
                || locker.inventory != container
                || !item.IsBackpack()
                || !locker.IsBackpackSlot(targetPosition))
                return null;

            return CanAccept;
        }
    }
}
