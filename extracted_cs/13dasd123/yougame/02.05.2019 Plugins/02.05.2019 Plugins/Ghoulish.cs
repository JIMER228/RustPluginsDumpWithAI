// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Ghoulish perk","Lomarine","1.0.0")]
      //  Слив плагинов server-rust by Apolo YouGame
    public class Ghoulish : RustPlugin
    {
        private void Init()
        {
            permission.RegisterPermission("ghoulish.use", this);
        }

        private void OnRunPlayerMetabolism(PlayerMetabolism m, BaseCombatEntity entity)
        {
            BasePlayer player = entity.ToPlayer();
            if (player == null) return;

            if (!permission.UserHasPermission(player.UserIDString, "ghoulish.use")) return;
            
            if((int)Math.Round(m.radiation_poison.value) == 0) return;

            if (player.health != player._maxHealth)
            {
                player.health = Mathf.Clamp(player.health + 2f, 0f, 100f);
                return;
            }
        }
        
        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
      //  Слив плагинов server-rust by Apolo YouGame
        {
            BasePlayer player = entity.ToPlayer();
            if (player == null || info.damageTypes.GetMajorityDamageType() != DamageType.Radiation) return null;

            if (permission.UserHasPermission(player.UserIDString, "ghoulish.use"))
            {
                return false;
            }
            
            return null;
			}
    }
}
