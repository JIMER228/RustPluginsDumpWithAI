using Facepunch;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Oxide.Core;
using Oxide.Core.Configuration;

namespace Oxide.Plugins
{
    [Info("WaterGunHeal", "jerky", "1.0.0")]
    [Description("Water Gun Heal plugin for Rust")]
    public class WaterGunHeal : RustPlugin
    {
        private const string HealPermission = "watergunheal.lv1";
        private const string RevivePermission = "watergunheal.lv2";
        private const ulong WaterGunSkinID = 3332607397;

        void Init()
        {
            permission.RegisterPermission(HealPermission, this);
            permission.RegisterPermission(RevivePermission, this);
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is BasePlayer && info.WeaponPrefab != null && info.WeaponPrefab.name.Contains("waterpistol.entity"))
            {
                BasePlayer shooter = info.InitiatorPlayer;
                if (shooter != null && permission.UserHasPermission(shooter.UserIDString, HealPermission))
                {
                    Item wp = shooter.GetActiveItem();
                    if (wp != null && wp.skin == WaterGunSkinID)
                    {
                        BasePlayer player = entity as BasePlayer;
                        if (shooter.UserIDString == player.UserIDString) return;
                        if (player.IsWounded() && permission.UserHasPermission(shooter.UserIDString, RevivePermission))
                        {
                            player.StopWounded();
                        }
                        player.Heal(10f); // Configure it later.
                    }
                }
            }
        }

        private void GiveWaterGunToPlayer(ulong steamID)
        {
            BasePlayer targetPlayer = BasePlayer.FindByID(steamID) ?? BasePlayer.FindSleeping(steamID);
            if (targetPlayer == null)
            {
                Puts("Player not found.");
                return;
            }

            Item waterGun = ItemManager.CreateByName("pistol.water", 1, WaterGunSkinID);
            waterGun.skin = WaterGunSkinID;
            waterGun.name = "Healing Water Pistol";
            targetPlayer.GiveItem(waterGun);
            Puts($"Water gun given to {targetPlayer.displayName}.");
        }


        [ConsoleCommand("givewatergun")]
        private void GiveWaterGunConsole(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length != 1)
            {
                arg.ReplyWith("givewatergun <steamID>");
                return;
            }

            ulong steamID;
            if (!ulong.TryParse(arg.Args[0], out steamID))
            {
                arg.ReplyWith("Invalid SteamID.");
                return;
            }

            GiveWaterGunToPlayer(steamID);
        }

    }
}
