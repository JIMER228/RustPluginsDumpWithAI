using UnityEngine;

namespace Oxide.Plugins
{
    //Define:FileOrder=60
    public partial class Trapper
    {
        private void Init()
        {
            LoadPermissions();
        }
        
        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (!_config.IgnoreProjectiles) return null;
            var trap = entity.GetComponent<BaseTrap>();
            if (!trap) return null;
            if (!info.IsProjectile()) return null;
            var bearTrap = entity.GetComponent<BearTrap>();
            if (bearTrap)
                bearTrap.Arm();
            return true;
        }

        private object OnTrapTrigger(BaseTrap trap, GameObject obj)
        {
            if (!(trap is BearTrap) && !(trap is Landmine))
                return null;
            var target = obj.GetComponent<BasePlayer>();
            if (target != null)
            {
                if (permission.UserHasPermission(target.UserIDString, PluginPermissions.PermissionNameAdmin))
                    return false;
		    }
            var player = FindPlayer(trap.OwnerID.ToString());
            if (target != null && player != null)
            {
                // Owner protection
                if (!_config.HurtOwner)
                    if (target == player){
                        if (permission.UserHasPermission(target.UserIDString, PluginPermissions.PermissionNameOwner)){
                            return false;
                        }
                    }
                // Building auth protection
                if (!_config.HurtAuthed)
                    if (permission.UserHasPermission(target.UserIDString, PluginPermissions.PermissionNameAuth))
                        if (target.IsBuildingAuthed()){
                            return false;
                        }

                // Friends protection
                if (!_config.HurtFriends)
                {
                    if (Friends!=null && Friends.IsLoaded)
                        if (Friends.Call<bool>("AreFriends", target.userID, player.userID))
                            if (permission.UserHasPermission(target.UserIDString, PluginPermissions.PermissionNameFriends))
                                return false;

                    if (RustIO!=null && RustIO.IsLoaded)
                        if (RustIO)
                            if (RustIO.Call<bool>("HasFriend", target.UserIDString, player.UserIDString))
                                if (permission.UserHasPermission(target.UserIDString, PluginPermissions.PermissionNameFriends))
                                    return false;
                }

                // ClanMates protection
                if (!_config.HurtClanMates)
                {
                    if (Clans)
                    {
                        var targetClan = Clans?.Call<string>("GetClanTag", target.UserIDString);
                        var playerClan = Clans?.Call<string>("GetClanTag", player.UserIDString);
                        if (targetClan == playerClan && !string.IsNullOrEmpty(playerClan ))
                        {
                            if (permission.UserHasPermission(target.UserIDString, PluginPermissions.PermissionNameClan))
                                return false;
                        }
                        else if (Clans?.Call<string>("GetClanOf", target.UserIDString) == Clans?.Call<string>("GetClanOf", player.UserIDString)){
                            if (permission.UserHasPermission(target.UserIDString, PluginPermissions.PermissionNameClan))
                                return false;
                        }
                    }
                }
            }
            
            // Automatic re-arming
            if (!(trap is BearTrap))
                return null;
            if (player == null)
                return null;
            if (permission.UserHasPermission(player.UserIDString, PluginPermissions.PermissionName))
                timer.Once(_config.ResetTime, () =>
                {
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    if (trap != null)
                        ((BearTrap)trap).Arm();
                });
            return null;
        }
    }
}
