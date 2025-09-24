// Requires: PersonalNPC

using UnityEngine;
using Newtonsoft.Json;

namespace Oxide.Plugins 
{
    [Info("PNPC Raid Addon", "walkinrey", "0.0.5")]
    public class PNPCAddonRaid : RustPlugin 
    {
        #region Config

        private Configuration _config;

        public class Configuration 
        {
            [JsonProperty("Permission to use this addon on all bots of player (not required)")]
            public string permission = "pnpcaddonraid.override-setup";

            [JsonProperty("Multiple Grenade Launcher Setup")]
            public GrenadeLauncher grenadeLauncher = new GrenadeLauncher();

            [JsonProperty("Rocket Launcher Setup")]
            public RocketLauncher rocketLauncher = new RocketLauncher();

            [JsonProperty("Snowball Gun Setup")]
            public SnowballGun snowballGun = new SnowballGun();

            [JsonProperty("Safe distance between bot and target to use explosives")]
            public float SafeDistance = 10f;

            public class WeaponLauncher
            {
                [JsonProperty("Attack Cooldown (leave 0 to use default cooldown for current weapon)")]
                public float AttackCooldown = 0f;

                [JsonProperty("Default projectile prefab (used if infinite ammo is enabled)")]
                public string ProjectilePrefab = "";
            } 

            public class GrenadeLauncher : WeaponLauncher
            {
                public GrenadeLauncher()
                {
                    ProjectilePrefab = "assets/prefabs/ammo/40mmgrenade/40mm_grenade_he.prefab";
                }
            }

            public class RocketLauncher : WeaponLauncher
            {
                public RocketLauncher()
                {
                    ProjectilePrefab = "assets/prefabs/ammo/rocket/rocket_basic.prefab";
                }
            }

            public class SnowballGun : GrenadeLauncher
            {
                public SnowballGun()
                {
                    ProjectilePrefab = "assets/prefabs/misc/xmas/snowball/snowball.projectile.prefab";
                }
            }
        }

        protected override void LoadDefaultConfig() 
        {
            _config = new Configuration();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new System.Exception();

                SaveConfig();
            }
            catch (System.Exception ex)
            {
                PrintError("{0}", ex);
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        #endregion

        private void Loaded() => ((PersonalNPC)Manager.GetPlugin("PersonalNPC")).OnControllerCreated.AddListener(OnControllerCreated);

        private void OnControllerCreated(PersonalNPC.PlayerBotController controller)
        {
            var raidController = controller.bot.gameObject.AddComponent<BotRaidController>();

            raidController.Controller = controller;
            raidController.Config = _config;
            raidController.SaveDistance = _config.SafeDistance <= 0 ? 10f : _config.SafeDistance;

            controller.FireRocket = raidController.FireProjectile;
            controller.Throw = raidController.Throw;
        }

        public class BotRaidController : FacepunchBehaviour
        {
            public PersonalNPC.PlayerBotController Controller;
            public Configuration Config;
            public float SaveDistance;

            public virtual void FireProjectile(BaseLauncher launcher, BaseEntity target)
            {
                ItemModProjectile component1 = launcher.primaryMagazine.ammoType.GetComponent<ItemModProjectile>();
                if (!component1) return;

                if (launcher.primaryMagazine.contents <= 0)
                {
                    launcher.SignalBroadcast(BaseEntity.Signal.DryFire);
                    launcher.StartAttackCooldown(launcher.reloadTime);
                }
                else if(!launcher.HasAttackCooldown())
                {

                    var item = launcher.GetItem();
                    var shortname = item.info.shortname;

                    float attackCooldown = 0f;
                    string defaultProjectile = string.Empty;

                    if(shortname == "rocket.launcher" || shortname == "rocket.launcher.dragon")
                    {
                        if(!CheckPosition(target.transform.position - Controller.bot.transform.position, SaveDistance)) return;
                        defaultProjectile = Config.rocketLauncher.ProjectilePrefab;
                        attackCooldown = Config.rocketLauncher.AttackCooldown;
                    }
                    else 
                    {
                        if(shortname == "snowballgun")
                        {
                            defaultProjectile = Config.snowballGun.ProjectilePrefab;
                            attackCooldown = Config.snowballGun.AttackCooldown;
                        }
                        else if(shortname == "multiplegrenadelauncher")
                        {
                            if(!CheckPosition(target.transform.position - Controller.bot.transform.position, SaveDistance)) return;
                            defaultProjectile = Config.grenadeLauncher.ProjectilePrefab;
                            attackCooldown = Config.grenadeLauncher.AttackCooldown;
                        }
                    }

                    --launcher.primaryMagazine.contents;
                    if (launcher.primaryMagazine.contents < 0) launcher.primaryMagazine.contents = 0;

                    var player = Controller.bot;

                    var pos = player.eyes.position;
                    var forward = player.eyes.HeadForward();
                    var rot = player.transform.rotation;
                    var aim = Quaternion.Euler(player.serverInput.current.aimAngles);

                    BaseEntity rocket;

                    if(component1.projectileObject.resourcePath == "assets/prefabs/ammo/shotgun/shotgunbullet.prefab")
                    {
                        Controller.ShotTest(item);
                        return;
                    }

                    if(!string.IsNullOrEmpty(component1.projectileObject.resourcePath))
                    {
                        rocket = GameManager.server.CreateEntity(component1.projectileObject.resourcePath,
                            player.eyes.position + player.eyes.HeadForward(), player.transform.rotation);
                    }
                    else 
                    {
                        rocket = GameManager.server.CreateEntity(defaultProjectile,
                            player.eyes.position + player.eyes.HeadForward(), player.transform.rotation);
                    }

                    if (rocket == null) return;
                    var proj = rocket.GetComponent<ServerProjectile>();

                    if (proj == null) return;
                    proj.InitializeVelocity(aim * rocket.transform.forward * 22f);

                    rocket.Spawn();

                    if(attackCooldown == 0f) launcher.StartAttackCooldown(launcher.ScaleRepeatDelay(launcher.repeatDelay));
                    else launcher.StartAttackCooldown(attackCooldown);

                    launcher.SignalBroadcast(BaseEntity.Signal.Attack, string.Empty);
                    launcher.UpdateItemCondition();
                }
            }
            public void Throw(ThrownWeapon thrownWeapon, BaseEntity target)
            {
                if(!CheckPosition(target.transform.position - Controller.bot.transform.position, SaveDistance)) return;
                // Debug.Log($"Test {hitPoint}");
                // thrownWeapon.ServerThrow(GetPointTarget(target));
                ServerThrow(thrownWeapon, GetPointTarget(target));
            }
            private bool CheckPosition(Vector3 direction, float saveDistance)
            {
                float distance = direction.magnitude;
                if(distance < saveDistance)
                {
                    Controller.SetDestination(Controller.bot.transform.position + direction.normalized * -2f);
                    return false;
                }
                if(distance > saveDistance + 3f)
                {
                    Controller.SetDestination(Controller.bot.transform.position + direction.normalized * 2f);
                    return false;
                }
                Controller.Navigator.Pause();
                return true;
            }
            private Vector3 GetPointTarget(BaseEntity target)
            {
                // Debug.Log($"Test {target.transform.forward}, {target.transform.right}");
                // Controller.ShowArrow(target.transform.position + target.transform.up * (target.bounds.size.y / 2f));
                return target.transform.position + target.transform.up * (target.bounds.size.y / 2f);
            }
            public void ServerThrow(ThrownWeapon thrownWeapon, Vector3 targetPosition)
            {
                if (thrownWeapon.isClient || !HasItemAmount(thrownWeapon) || thrownWeapon.HasAttackCooldown())
                {
                    return;
                }

                BasePlayer ownerPlayer = thrownWeapon.GetOwnerPlayer();
                if (ownerPlayer == null || (!thrownWeapon.canThrowUnderwater && ownerPlayer.IsHeadUnderwater()))
                {
                    return;
                }

                Vector3 position = ownerPlayer.eyes.position;
                Vector3 vector = ownerPlayer.eyes.BodyForward();
                thrownWeapon.SignalBroadcast(BaseEntity.Signal.Throw, string.Empty);
                BaseEntity baseEntity = GameManager.server.CreateEntity(thrownWeapon.prefabToThrow.resourcePath, position, Quaternion.LookRotation((thrownWeapon.overrideAngle == Vector3.zero) ? (-vector) : thrownWeapon.overrideAngle));
                if (baseEntity == null)
                {
                    return;
                }
                var direction = targetPosition - baseEntity.transform.position;

                baseEntity.SetCreatorEntity(ownerPlayer);
                Vector3 vector2 = direction.normalized + Vector3.up;
                float num2 = GetThrowVelocity(position, targetPosition, vector2);
                if (float.IsNaN(num2))
                {
                    num2 = 5f;
                }
                baseEntity.SetVelocity(vector2 * num2);
                if (thrownWeapon.tumbleVelocity > 0f)
                {
                    baseEntity.SetAngularVelocity(new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)) * thrownWeapon.tumbleVelocity);
                }

                baseEntity.Spawn();
                thrownWeapon.StartAttackCooldown(thrownWeapon.repeatDelay);
                GetOwnerItem(thrownWeapon).UseItem(1);
                // if (baseEntity as TimedExplosive != null)
                // {
                //     Facepunch.Rust.Analytics.Azure.OnExplosiveLaunched(ownerPlayer, baseEntity);
                // }
            }
            private float GetThrowVelocity(Vector3 throwPos, Vector3 targetPos, Vector3 aimDir)
            {
                Vector3 vector = targetPos - throwPos;
                float magnitude = new Vector2(vector.x, vector.z).magnitude;
                float y = vector.y;
                float magnitude2 = new Vector2(aimDir.x, aimDir.z).magnitude;
                float y2 = aimDir.y;
                float y3 = UnityEngine.Physics.gravity.y;
                return Mathf.Sqrt(0.5f * y3 * magnitude * magnitude / (magnitude2 * (magnitude2 * y - y2 * magnitude)));
            }
            protected bool HasItemAmount(ThrownWeapon thrownWeapon)
            {
                Item ownerItem = GetOwnerItem(thrownWeapon);
                if (ownerItem != null)
                {
                    return ownerItem.amount > 0;
                }

                return false;
            }
            protected Item GetOwnerItem(ThrownWeapon thrownWeapon)
            {
                BasePlayer ownerPlayer = thrownWeapon.GetOwnerPlayer();
                if (ownerPlayer == null || ownerPlayer.inventory == null)
                {
                    return null;
                }

                return ownerPlayer.inventory.FindItemByUID(thrownWeapon.ownerItemUID);
            }
        }
    }
} 