using UnityEngine;

namespace Oxide.Plugins
{
    [Info("LaserPistol", "VSP", "1.0.0")]
    public class LaserPistol : RustPlugin
    {
        uint SkinSemiId = 3200038165;
        bool LightToggle = false;
        [ConsoleCommand("givepistol")]
        private void GivePistolCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (!player.IsAdmin)
            {
                player.ChatMessage("Недостоин!");
                return;
            }
            BasePlayer basePlayer = player as BasePlayer;
            if (basePlayer == null) return;
            var pistol = ItemManager.CreateByName("pistol.semiauto", 1, SkinSemiId);
            pistol.name = "Наказание модератора";
            if (pistol == null)  return; 
            var laserSight = ItemManager.CreateByName("weapon.mod.lasersight", 1);
            if (laserSight == null) return;
            pistol.contents.AddItem(laserSight.info, laserSight.amount);
            //player.LightToggle();
            basePlayer.inventory.GiveItem(pistol);
            player.ChatMessage("Воля ваша..."); 
        }
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (info.InitiatorPlayer == null) return;
            var localPlayer = info.InitiatorPlayer;
            if (info.Weapon)
            {
                localPlayer.ChatMessage(info.Weapon.ShortPrefabName);
                if (info.Weapon.ShortPrefabName == "pistol_semiauto.entity")
                {
                    if (info.Weapon.skinID == SkinSemiId)
                    {
                        if (entity.faction == BaseCombatEntity.Faction.Player)
                        {
                            var playerEnemy = entity.ToPlayer();
                            if (playerEnemy == null)
                            {
                                playerEnemy.Kick("Свергнут");
                            }
                            localPlayer.ChatMessage("Свергнут");
                        }
                        if (entity.faction == BaseCombatEntity.Faction.Default)
                        {
                            entity.Kill();
                            //entity.KillAsMapEntity();
                            localPlayer.ChatMessage("Изничтожено");
                        }
                    }

                }
            }

        }
        void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            player.ChatMessage(newItem.name);
            if (newItem.name == "Наказание модератора")
            {
                if (!LightToggle)
                {
                    player.LightToggle();
                    LightToggle = true;
                }
            }

        }
    }
}
