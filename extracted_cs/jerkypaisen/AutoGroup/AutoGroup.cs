using Oxide.Core;
using Oxide.Core.Plugins;
using System.ComponentModel;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("AutoGroup", "jerky", "1.0.0")]
    [Description("Automatically adds new users to a specific group, excluding those with specific permissions.")]
    public class AutoGroup : RustPlugin
    {
        private const string userGroup = "user";
        private const string excludePermission = "autogroup.exclude";

        private void Init()
        {
            permission.RegisterPermission(excludePermission, this);

            if (!permission.GroupExists(userGroup))
            {
                permission.CreateGroup(userGroup, "User Group", 0);
                Puts($"Group '{userGroup}' has been created.");
            }
        }

        private void OnUserConnected(IPlayer player)
        {
            if (permission.UserHasPermission(player.Id, excludePermission))
            {
                return;
            }

            if (!permission.UserHasGroup(player.Id, userGroup))
            {
                permission.AddUserGroup(player.Id, userGroup);
                Puts($"{player.Name} has been added to the {userGroup} group.");
            }
        }
    }
}