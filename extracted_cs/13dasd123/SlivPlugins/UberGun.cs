// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("UberGun", "Synvy", "1.1.2")]
    [Description("Allows players with permission to shoot rockets from the Snowball Gun!")]
    public class UberGun : RustPlugin
    {
        #region Initialize

        private const string _perm = "ubergun.use";

        private void Init()
        {
            permission.RegisterPermission(_perm, this);
        }

        #endregion Initialize

        #region Configuration

        private Configuration _config;

        public class Configuration
        {
            [JsonProperty("Uber weapon (shortname)")]
            public string uberWeapon = "snowballgun";

            [JsonProperty("Unlimited ammo")]
            public bool unlimitedAmmo = true;

            [JsonProperty("Unlimited durability")]
            public bool unlimitedDurability = true;

            [JsonProperty("Rocket speed (higher = faster)")]
            public int rocketSpeed = 250;

            [JsonProperty("Rocket damage")]
            public float rocketDamage = 100f;

            [JsonProperty("Rocket type (prefab)")]
            public string rocketType = "assets/prefabs/ammo/rocket/rocket_basic.prefab";  

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        protected override void LoadDefaultConfig() => _config = new Configuration();

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<Configuration>();

                if (_config == null)
                {
                    throw new JsonException();
                }

                if (!_config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
                {
                    SaveConfig();
                }
            }
            catch
            {
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        #endregion Configuration

        #region Hooks

        private void OnWeaponFired(BaseProjectile weapon, BasePlayer player, ItemModProjectile ammo, ProtoBuf.ProjectileShoot projectiles)
        {
            if (player != null && HasPerm(player.UserIDString, _perm) && IsUberWeapon(player))
            {
                var rocket = GameManager.server.CreateEntity(_config.rocketType, player.eyes.position, new Quaternion());

                if (_config.rocketType != null)
                {
                    rocket.creatorEntity = player;
                    rocket.SendMessage("InitializeVelocity", player.eyes.HeadForward() * _config.rocketSpeed);
                    rocket.OwnerID = player.userID;
                    rocket.Spawn();
                    rocket.ClientRPC(null, "RPCFire");
                }

                if (_config.unlimitedDurability)
                {
                    weapon.GetItem().condition = weapon.GetItem().info.condition.max;
                }

                if (_config.unlimitedAmmo)
                {
                    weapon.primaryMagazine.contents = weapon.primaryMagazine.capacity;
                    weapon.SendNetworkUpdateImmediate();
                }    
            }

            return;
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info.damageTypes == null || info.InitiatorPlayer == null)
            {
                return;
            }

            var player = info.InitiatorPlayer as BasePlayer;

            if (player == null || !HasPerm(player.UserIDString, _perm) || !IsUberWeapon(player))
            {
                return;
            }

            var shortname = info.WeaponPrefab?.ShortPrefabName;

            if (string.IsNullOrEmpty(shortname))
            {
                return;
            }

            float damage = 0f;

            switch (shortname)
            {
                case "rocket_basic":
                    {
                        damage = _config.rocketDamage;
                        break;
                    }
                case "rocket_fire":
                    {
                        damage = _config.rocketDamage;
                        break;
                    }
            }

            info.damageTypes.ScaleAll(0.01f * damage);
        }

        #endregion Hooks

        #region Helpers

        private bool HasPerm(string id, string perm) => permission.UserHasPermission(id, perm);

        private bool IsUberWeapon(BasePlayer player)
        {
            var heldItem = player.GetActiveItem()?.info.shortname ?? "null";
            return heldItem == _config.uberWeapon;
        }

        #endregion Helpers
    }
}
