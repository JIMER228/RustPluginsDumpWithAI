using System;

namespace Oxide.Plugins
{
    [Info("BaseProtector", "Lomarine", "1.0.0")]
      //  Слив плагинов server-rust by Apolo YouGame
    public class BaseProtector : RustPlugin
    {
        #region Oxide Hooks

        private void Init()
        {
            permission.RegisterPermission(perm1,this);
            permission.RegisterPermission(perm2,this);
            permission.RegisterPermission(perm3,this);
        }

        private void OnEntityTakeDamage(BaseEntity entity, HitInfo hit)
      //  Слив плагинов server-rust by Apolo YouGame
        {
            if (entity.OwnerID == 0 || hit.InitiatorPlayer == null) return;

            BasePlayer player = BasePlayer.FindByID(entity.OwnerID);
            if (player == null) return;
            
            if (permission.UserHasPermission(player.UserIDString, perm3)) // LVL 3
            {
                if(DateTime.Now.Hour > 22 || DateTime.Now.Hour < 8)
                {
                    hit.damageTypes.ScaleAll(0.40f);
                }
                
                return;
            }
            
            if (permission.UserHasPermission(player.UserIDString, perm2)) // LVL 2
            {
                if(DateTime.Now.Hour > 22 || DateTime.Now.Hour < 8)
                {
                    hit.damageTypes.ScaleAll(0.65f);
                }
                
                return;
            }
            
            if (permission.UserHasPermission(player.UserIDString, perm1)) // LVL 1
            {
                if(DateTime.Now.Hour > 22 || DateTime.Now.Hour < 8)
                {
                    hit.damageTypes.ScaleAll(0.85f);
                }
  
                return;
            }
        }

        #endregion

        #region Config

        private const string perm1 = "bp.1", perm2 = "bp.2", perm3 = "bp.3";
        
        #endregion
    }
}
