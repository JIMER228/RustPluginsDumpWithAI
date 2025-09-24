using Oxide.Core.Plugins;
using UnityEngine;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("ChickenGun", "locks", "1.0.6")]
    [Description("Gives any gun unlimited ammo, does no damage, and spawns chickens where bullets hit. Chickens attack the closest player. Allows players to toggle this feature.")]

    public class ChickenGun : RustPlugin
    {
        private const ulong RainbowPonySkinID = 10125;
        private const string PermissionUse = "chickengun.use";

        private Dictionary<ulong, bool> chickenGunEnabled = new Dictionary<ulong, bool>();

        void Init()
        {
            permission.RegisterPermission(PermissionUse, this);
        }

        [ChatCommand("chook")]
        private void EnableChickenGun(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermissionUse))
            {
                SendReply(player, "You do not have permission to use this command.");
                return;
            }

            chickenGunEnabled[player.userID] = true;
            GiveChickenGun(player);
            SendReply(player, "Activated chicken gun, fowl play mode activated!");
        }

        [ChatCommand("nochook")]
        private void DisableChickenGun(BasePlayer player, string command, string[] args)
        {
            if (!chickenGunEnabled.ContainsKey(player.userID) || !chickenGunEnabled[player.userID])
            {
                SendReply(player, "Chicken Gun is not enabled.");
                return;
            }

            chickenGunEnabled[player.userID] = false;
            SendReply(player, "Chicken Gun disabled!");
        }

        private void GiveChickenGun(BasePlayer player)
        {
            foreach (var item in player.inventory.containerMain.itemList)
            {
                if (item.info.category == ItemCategory.Weapon && item.GetHeldEntity() is BaseProjectile weapon)
                {
                    item.condition = item.maxCondition;
                    item.skin = RainbowPonySkinID;

                    weapon.primaryMagazine.contents = weapon.primaryMagazine.capacity;
                    weapon.SendNetworkUpdate();
                }
            }
        }

        private void OnPlayerAttack(BasePlayer player, HitInfo info)
        {
            if (chickenGunEnabled.ContainsKey(player.userID) && chickenGunEnabled[player.userID])
            {
                // Nullify damage
                info.damageTypes.ScaleAll(0f);
                info.DoHitEffects = false;
                info.HitMaterial = 0;

                // Refill ammo
                var weapon = info.Weapon as BaseProjectile;
                if (weapon != null)
                {
                    weapon.primaryMagazine.contents = weapon.primaryMagazine.capacity;
                    weapon.SendNetworkUpdate(); // Ensure the client receives the updated ammo count
                }

                // Spawn a chicken at the hit position
                if (info?.HitPositionWorld != null)
                {
                    Vector3 hitPosition = info.HitPositionWorld;

                    BaseEntity chicken = GameManager.server.CreateEntity("assets/rust.ai/agents/chicken/chicken.prefab", hitPosition);
                    if (chicken != null)
                    {
                        chicken.Spawn();
                        timer.Once(5f, () => chicken.Kill()); // Despawn chicken after 5 seconds
                        SetChickenTarget(chicken, hitPosition);
                    }
                }
            }
        }

        private void SetChickenTarget(BaseEntity chicken, Vector3 position)
        {
            BasePlayer closestPlayer = FindClosestPlayer(position);
            if (closestPlayer != null)
            {
                var npc = chicken as BaseNpc;
                if (npc != null)
                {
                    npc.SetFact(BaseNpc.Facts.IsAggro, 1);
                    npc.AttackTarget = closestPlayer;
                }
            }
        }

        private BasePlayer FindClosestPlayer(Vector3 position)
        {
            BasePlayer closestPlayer = null;
            float closestDistance = float.MaxValue;

            foreach (var player in BasePlayer.activePlayerList)
            {
                float distance = Vector3.Distance(player.transform.position, position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = player;
                }
            }

            return closestPlayer;
        }
    }
}
