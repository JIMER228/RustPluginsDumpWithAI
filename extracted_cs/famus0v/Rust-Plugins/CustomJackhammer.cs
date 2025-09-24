namespace Oxide.Plugins
{
    [Info("CustomJackhammer", "Kira", "1.0.0")]
    [Description("Кастомный отбойник и пила")]
    public class CustomJackhammer : RustPlugin
    {
        private const string Jackhammer = "customjackhammer.jackhammer";
        private const string Chainsaw = "customjackhammer.jackhammer";

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            if (!permission.PermissionExists(Jackhammer)) permission.RegisterPermission(Jackhammer, this);
            if (!permission.PermissionExists(Chainsaw)) permission.RegisterPermission(Chainsaw, this);
            PrintWarning("Developer - Kira\nSupport - -Kira#1920");
        }

        // ReSharper disable once UnusedMember.Local
        private object OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            var player = entity.ToPlayer();
            if (player == null) return null;
            if (dispenser.gatherType == ResourceDispenser.GatherType.Ore)
            {
                if (entity.ToPlayer().GetActiveItem().info.shortname == "jackhammer")
                {
                    if (permission.UserHasPermission(player.UserIDString, Jackhammer)) dispenser.DestroyFraction(10000);
                    return false;
                }
            }

            if (dispenser.gatherType != ResourceDispenser.GatherType.Tree) return null;
            if (entity.ToPlayer().GetActiveItem().info.shortname != "chainsaw") return null;
            if (permission.UserHasPermission(player.UserIDString, Chainsaw)) dispenser.DestroyFraction(10000);
            return false;
        }
    }
}