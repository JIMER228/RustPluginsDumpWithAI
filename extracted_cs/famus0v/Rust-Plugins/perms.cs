namespace Oxide.Plugins
{
    [Info("perms", "Kira", "1.0.0")]
    [Description("asdasd")]
    public class perms : RustPlugin
    {
        private void OnServerInitialized()
        {
 
                foreach (var player in BasePlayer.activePlayerList)
                {
                    foreach (var p in permission.GetUserPermissions(player.UserIDString))
                    {
                        permission.RevokeUserPermission(player.UserIDString, p);
                    }
                }
            
                
                
                foreach (var perm in permission.GetGroupPermissions("default"))
                {
              
                        permission.RevokeGroupPermission("default", perm);
                    
                }
            
        }
    }
}