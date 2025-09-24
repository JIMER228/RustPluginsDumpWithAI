using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Heli Editor", "KibbeWater", "0.2.3")]
    [Description("Modify several characteristics of aircrafts")]
    class HeliEditor : RustPlugin
    {
        #region Variables
        private PluginConfig _config;

        #region Permissions
        public string permissionTakeoff = "helieditor.takeoff";
        #endregion

        #endregion

        #region Config
        protected override void LoadDefaultConfig() => _config = PluginConfig.DefaultConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<PluginConfig>();

                if (_config == null)
                {
                    throw new JsonException();
                }
            }
            catch
            {
                PrintWarning("Loaded default config.");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        //I just made 2 different if I wanna do something in the future

        private class ScrapheliSettings {
            public float maxHealth;
            public bool invincible;
            public bool blockExplosions;
            public bool instantTakeoff;
            public bool hydrophobic;
        }

        private class MinicopterSettings {
            public float maxHealth;
            public bool invincible;
            public bool blockExplosions;
            public bool instantTakeoff;
            public bool hydrophobic;
        }

        private class PluginConfig
        {
            public MinicopterSettings minicopter;
            public ScrapheliSettings scrapheli;

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig
                {
                    minicopter = new MinicopterSettings() {
                        maxHealth = 800,
                        invincible = false,
                        blockExplosions = false,
                        instantTakeoff = false,
                        hydrophobic = false
                    },
                    scrapheli = new ScrapheliSettings {
                        maxHealth = 1500,
                        invincible = false,
                        blockExplosions = false,
                        instantTakeoff = false,
                        hydrophobic = false
                    }
                };
            }
        }
        #endregion

        #region Hooks
        private void Init()
        {
            permission.RegisterPermission(permissionTakeoff, this);
        }

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (_config.minicopter.invincible)
                if (entity is MiniCopter)
                    if(!(entity is ScrapTransportHelicopter)) {
                        info.damageTypes.Clear();
                        return true;
                    }
            if (_config.scrapheli.invincible)
                if (entity is ScrapTransportHelicopter) {
                    info.damageTypes.Clear();
                    return true;
                }
            return null;
        }

        private void OnServerInitialized()
        {
            foreach (var entity in BaseNetworkable.serverEntities)
            {
                if (entity == null)
                    continue;
                if (entity is MiniCopter)
                    OnEntitySpawned((MiniCopter)entity);
                else if (entity is ScrapTransportHelicopter)
                    OnEntitySpawned((ScrapTransportHelicopter)entity);
            }
        }

        private void OnEntitySpawned(BaseHelicopterVehicle entity)
        {
            if (entity is MiniCopter && !(entity is ScrapTransportHelicopter)){
                var miniEntity = entity as MiniCopter;

                //Remove explosion effect references to not have it spawn
                if (_config.minicopter.blockExplosions) {
                    entity.explosionEffect.guid = null;
                    entity.serverGibs.guid = null;
                    entity.fireBall.guid = null;
                }

                //Set health set in config
                entity.SetMaxHealth(_config.minicopter.maxHealth);
                entity.health = _config.minicopter.maxHealth;

                //Unparent the water sample object to prevent it from moving with the minicopter
                if (_config.minicopter.hydrophobic) {
                    miniEntity.waterSample.transform.SetParent(null);
                    miniEntity.waterSample.position = new Vector3(1000,1000,1000);
                }

                //Remove killtriggers for invincibility
                if (_config.minicopter.invincible) //IDK what this is but it was scary so I removed it
                    entity.killTriggers = new GameObject[0]; //EDIT: I still have no idea what it does
            }
            if (entity is ScrapTransportHelicopter) {
                var miniEntity = entity as MiniCopter;

                //Remove explosion effect references to not have it spawn
                if (_config.scrapheli.blockExplosions) {
                    entity.explosionEffect.guid = null;
                    entity.serverGibs.guid = null;
                    entity.fireBall.guid = null;
                }

                //Set health set in config
                entity.SetMaxHealth(_config.scrapheli.maxHealth);
                entity.health = _config.scrapheli.maxHealth;

                //Unparent the water sample object to prevent it from moving with the minicopter
                if (_config.scrapheli.hydrophobic) {
                    miniEntity.waterSample.transform.SetParent(null);
                    miniEntity.waterSample.position = new Vector3(1000,1000,1000);
                }

                //Remove killtriggers for invincibility
                if (_config.scrapheli.invincible) //IDK what this is but it was scary so I removed it
                    entity.killTriggers = new GameObject[0]; //EDIT: I still have no idea what it does
                return;
            }
        }

        void OnEngineStarted(BaseVehicle vehicle, BasePlayer driver)
        {
            if (!permission.UserHasPermission(driver.UserIDString, permissionTakeoff))
                return;
            if (vehicle is ScrapTransportHelicopter && _config.scrapheli.instantTakeoff)
                (vehicle as MiniCopter).engineController.FinishStartingEngine();
            if (vehicle is MiniCopter && !(vehicle is ScrapTransportHelicopter) && _config.minicopter.instantTakeoff)
                (vehicle as MiniCopter).engineController.FinishStartingEngine();
        }
        #endregion
    }
}