namespace Oxide.Plugins
{
    [Info("CustomDrop", "Hougan, Ryamkk", "1.0.1")]
      //  Слив плагинов server-rust by Apolo YouGame
    public class CustomDrop : RustPlugin
    {
        private string Permission = "customdrop.use";
        private void OnServerInitialized() => permission.RegisterPermission(Permission, this);
        private void OnPlayerDie(BasePlayer player, HitInfo info)
      //  Слив плагинов server-rust by Apolo YouGame
        {
            if (!permission.UserHasPermission(player.UserIDString, Permission) || player.GetComponent<NPCPlayer>() != null)
                return;

            for (int i = 0; i < player.inventory.AllItems().Length; i++)
            {
                Item x = player.inventory.AllItems()[i];
                if (x.MaxStackable() != 1)
                    continue;
                
                x.name = $"{player.displayName.ToUpper()}'s {x.info.displayName.english}";
            }
        }
    }
}
