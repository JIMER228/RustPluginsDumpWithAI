namespace Oxide.Plugins
{
    //Define:FileOrder=40
    public partial class Trapper
    {
        public static class PluginPermissions
        {
            public const string PermissionNameAdmin = "trapper.admin";
            public const string PermissionName = "trapper.auto";
            public const string PermissionNameOwner = "trapper.owner";
            public const string PermissionNameFriends = "trapper.friends";
            public const string PermissionNameClan = "trapper.clan";
            public const string PermissionNameAuth = "trapper.auth";
        }
        

        private void LoadPermissions()
        {
            permission.RegisterPermission(PluginPermissions.PermissionName, this);
            permission.RegisterPermission(PluginPermissions.PermissionNameOwner, this);
            permission.RegisterPermission(PluginPermissions.PermissionNameFriends, this);
            permission.RegisterPermission(PluginPermissions.PermissionNameAdmin, this);
            permission.RegisterPermission(PluginPermissions.PermissionNameClan, this);
            permission.RegisterPermission(PluginPermissions.PermissionNameAuth, this);
        }
    }
}
