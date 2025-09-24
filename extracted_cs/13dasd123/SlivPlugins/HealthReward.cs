using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("HealthReward", "GAGA", "1.0.0")]
    [Description("ЕБЕТ МАМУ БИКСБИ В РОТ")]

    public class HealthReward : RustPlugin
    {
        private const int HealthRewardAmount = 25;

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (info == null || info.InitiatorPlayer == null || info.damageTypes == null)
                return;

            var attacker = info.InitiatorPlayer;

            var victim = entity as BasePlayer;
            if (victim != null && attacker != victim)
            {   
                attacker.health += HealthRewardAmount;
                attacker.ChatMessage($"<color=#F5AC32>[BEE RUST]\n</color>Вам начислено {HealthRewardAmount} за убийство игрока");  
            }
        }
    }
}
