namespace Oxide.Plugins
{
    [Info("ClearInventory", "headtapper", "1.0.0")]
    [Description("Clear your inventory conveniently using a command.")]

    public class ClearInventory : RustPlugin
    {
        string ClearInventoryPermission = "clearinventory.clear";

        private void Init()
        {
            permission.RegisterPermission(ClearInventoryPermission, this);
        }

        [ChatCommand("clearall")]
        private void ClearAllCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, ClearInventoryPermission)) return;
            player.inventory.Strip();
        }

        [ChatCommand("clearinv")]
        private void ClearInventoryCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, ClearInventoryPermission)) return;
            player.inventory.containerMain.Clear();
        }
    }
}
