// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Shark Launcher", "NooBlet", "1.0.1")]
    [Description("Shoot Sharks")]
    class SharkLauncher : CovalencePlugin
    {
        #region Fields
        private const ulong WEAPON_SKIN_ID = 1964228252;
        private const int WEAPON_ID = 442886268;
        private  bool DoLauncherDamage = true;
        private int fireCooldown = 5;
        private int ProjectileSpeed = 20;
        private const string RocketProjectile = "assets/prefabs/ammo/rocket/rocket_basic.prefab";
        private const string SnowBallProjectile = "assets/prefabs/ammo/rocket/rocket_smoke.prefab";
        private const string sharkPrefab = "assets/rust.ai/agents/fish/simpleshark.prefab";        
        string sfx_watersplash = "assets/bundled/prefabs/fx/explosions/water_bomb.prefab";
        private Dictionary<ulong, DateTime> fireCooldownData = new Dictionary<ulong, DateTime>();
        #endregion

        #region Oxide Hooks
        private void OnServerInitialized()
        {
            LoadConfiguration();
        }
        object OnEntityTakeDamage(SimpleShark shark, HitInfo info)
        {
            if(shark._name == "launchershark")
            {               
                global::Effect.server.Run(sfx_watersplash, shark.transform.position + Vector3.up, Vector3.forward, null, false);
                shark.Kill();
                return false;
            }
            return null;
        }
       
        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            Timer test = null;
            if (player == null || input == null)
                return;               
           
            if (input.IsDown(BUTTON.FIRE_PRIMARY))
            {
                if (CheckisCooldown(player.userID)) { return; }
                Item activeItem = player.GetActiveItem();
                if (activeItem == null || activeItem.info.itemid != WEAPON_ID || activeItem.skin != WEAPON_SKIN_ID)
                    return;
                var rocket = GameManager.server.CreateEntity(RocketProjectile,
                       player.eyes.position + player.eyes.HeadForward(), player.transform.rotation);
                if (!DoLauncherDamage)
                {
                    rocket = GameManager.server.CreateEntity(SnowBallProjectile,
                        player.eyes.position + player.eyes.HeadForward(), player.transform.rotation);
                }

                if (rocket == null) {  return; }
                    var proj = rocket.GetComponent<ServerProjectile>();
                    if (proj == null) {return; }
                proj.InitializeVelocity(Quaternion.Euler(player.serverInput.current.aimAngles) * rocket.transform.forward * ProjectileSpeed);
                    
                if (!fireCooldownData.ContainsKey(player.userID)) { fireCooldownData.Add(player.userID, DateTime.Now.AddSeconds(fireCooldown)); } else { fireCooldownData[player.userID] = DateTime.Now.AddSeconds(fireCooldown); }

                SimpleShark shark;
                BaseEntity entity = GameManager.server.CreateEntity(sharkPrefab,rocket.transform.position);

                shark = entity as SimpleShark;
                shark.enabled = false;
                rocket.limitNetworking = true;
                shark.limitNetworking = true;
                
                rocket.Spawn();
                rocket._name = "SLRocket";
                rocket.OwnerID = player.userID; 
              
               
                shark.transform.position = rocket.transform.position;
                shark.transform.rotation = rocket.transform.rotation;
                shark._name = "launchershark";
                entity.Spawn();
                timer.Once(0.05f, () =>
                {
                    global::Effect.server.Run("assets/content/vehicles/mlrs/effects/pfx_airburst.prefab", shark.transform.position, Vector3.forward, null, false);
                });

                shark.limitNetworking = false;




                test = timer.Every(0.01f, () =>
                {
                    if (!shark.IsAlive()) {test.Destroy();  return; }
                    if (rocket.IsDestroyed) {test.Destroy();  return; }
                   shark.transform.position = rocket.transform.position;
                    shark.transform.rotation = rocket.transform.rotation;
                });
               
            }
        }


        #endregion

        #region Commands
        [Command("sl")]
        private void cmdsharklauncher(IPlayer iplayer, string command, string[] args)
        {
            BasePlayer player = iplayer.Object as BasePlayer;
            if (!player.IsAdmin)
            {
                Puts("No permission to execute this command. You need auth level 1 or 2");
                return;
            }
            Item item = ItemManager.CreateByItemID(WEAPON_ID, 1, WEAPON_SKIN_ID);
            BaseProjectile baseProjectile = item.GetHeldEntity()?.GetComponent<BaseProjectile>();
            if (baseProjectile != null && baseProjectile.primaryMagazine.contents > 0)
                baseProjectile.primaryMagazine.contents = 0;

            player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);

        }
        #endregion

        #region Config

        protected override void LoadDefaultConfig()
        {
            LogWarning("Creating a new configuration file");
        }
       
        private void LoadConfiguration()
        {           
            CheckCfg<bool>("1. Launcher does Damage [when false damage only 5]", ref DoLauncherDamage);
            CheckCfg<int>("2. FireRateCooldown in Sec's", ref fireCooldown);
            CheckCfg<int>("3. Shark Launch velocity [default = 20]", ref ProjectileSpeed);

            SaveConfig();

           
        }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }


        #endregion Config

        #region Helpers
        private bool CheckisCooldown(ulong userID)
        {
            if (!fireCooldownData.ContainsKey(userID)) { return false; }
            if (fireCooldownData[userID] > DateTime.Now) { return true; }
            return false;
        }

        #endregion Helpers
    }
}
///Скачано с дискорд сервера The Rust Bay & Rust Edit [PRO+] Cooperative 
///https://notes.xxi2.com/ms8mr