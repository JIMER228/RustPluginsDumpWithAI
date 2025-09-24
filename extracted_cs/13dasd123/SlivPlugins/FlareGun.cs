using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("FlareGun", "k1lly0u", "0.1.17")]
    [Description("Shoot flares... from a gun...")]
    class FlareGun : RustPlugin
    {
        #region Fields  
        private Hash<ulong, double> fireRates = new Hash<ulong, double>();
        private HashSet<BaseEntity> outstandingFlares = new HashSet<BaseEntity>();

        private bool isInitialized;

        const string FLARE_PREFAB = "assets/prefabs/tools/flareold/flare.deployed.prefab";          
        const int FLARE_ID = 304481038;
        const ulong WEAPON_SKIN_ID = 1251600167;
        const int WEAPON_ID = 649912614;
        #endregion

        #region Oxide Hooks 
        private void Loaded()
        {
            permission.RegisterPermission("flaregun.noammo", this);
            permission.RegisterPermission("flaregun.spawn", this);
        }

        private void OnServerInitialized() => isInitialized = true;

        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["noAmmo"] = "You need flares in your inventory to fire the flare gun!",
                ["onHeld"] = "This is a flare gun, you can fire flares aslong as you have them in your inventory!\nThis weapon will not load normal ammunition",
                ["noWeapon"] = "You need to have a {0} in your hands to use this command!"
            }, this);
        }

        private void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (!isInitialized || player == null || newItem == null)
                return;

            if (newItem.info.itemid == WEAPON_ID && newItem.skin == WEAPON_SKIN_ID)
            {
                BaseProjectile projectile = newItem.GetHeldEntity() as BaseProjectile;
                if (projectile != null && projectile.primaryMagazine != null)
                {
                    if (projectile.primaryMagazine.contents > 0)
                    {
                        if (configData.ReturnAmmo && projectile.primaryMagazine.ammoType != null)
                            player.GiveItem(ItemManager.Create(projectile.primaryMagazine.ammoType, projectile.primaryMagazine.contents));
                        projectile.primaryMagazine.contents = 0;
                        projectile.SendNetworkUpdate();
                    }
                }
                player.ChatMessage(msg("onHeld", player.userID));
            }
        }

        private void OnEntityKill(BaseNetworkable networkable)
        {
            if (networkable != null)
            {
                BaseEntity entity = networkable as BaseEntity;
                if (entity != null && outstandingFlares.Contains(entity))
                    outstandingFlares.Remove(entity);
            }
        }

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (!isInitialized || player == null || input == null)
                return;

            if (input.WasJustPressed(BUTTON.FIRE_PRIMARY))
            {
                Item activeItem = player.GetActiveItem();
                if (activeItem == null || activeItem.info.itemid != WEAPON_ID || activeItem.skin != WEAPON_SKIN_ID)
                    return;

                bool unlimitedAmmo = permission.UserHasPermission(player.UserIDString, "flaregun.noammo");

                if (unlimitedAmmo || player.inventory.GetAmount(FLARE_ID) > 0)
                {
                    double currentTime = GrabCurrentTime();
                    double nextFire;
                    if (fireRates.TryGetValue(player.userID, out nextFire))
                    {
                        if (nextFire > currentTime)
                            return;
                    }
                    if (!unlimitedAmmo)
                        player.inventory.Take(null, FLARE_ID, 1);

                    BaseEntity baseEntity = GameManager.server.CreateEntity(FLARE_PREFAB, player.transform.position + (player.modelState.ducked ? Vector3.up * 0.7f : Vector3.up * 1.5f) + player.eyes.HeadForward());
                    baseEntity.creatorEntity = player;
                    baseEntity.SetVelocity((Quaternion.Euler(input.current.aimAngles) * Vector3.forward) * configData.Velocity);
                    baseEntity.SetAngularVelocity(new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)) * 40);
                    baseEntity.Spawn();
                    outstandingFlares.Add(baseEntity);

                    fireRates[player.userID] = currentTime + configData.Interval;
                }
                else player.ChatMessage(msg("noAmmo", player.userID));
            }
        }

        private object CanAcceptItem(ItemContainer container, Item item)
        {
            if (container == null || item == null)
                return null;

            if (item.info.itemid == WEAPON_ID && item.skin == WEAPON_SKIN_ID)
            {
                if (container.entityOwner?.GetComponent<RepairBench>())
                    return ItemContainer.CanAcceptResult.CannotAccept;

                if (container.entityOwner?.GetComponent<Recycler>())
                    return ItemContainer.CanAcceptResult.CannotAccept;
            }           
            return null;
        }

        private object OnReloadWeapon(BasePlayer player, BaseProjectile projectile)
        {
            Item item = player.GetActiveItem();
            if (item.info.itemid == WEAPON_ID && item.skin == WEAPON_SKIN_ID)            
                return false;            
            return null;
        }
        #endregion

        #region Commands
        [ChatCommand("flare")]
        private void cmdFlare(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "flaregun.spawn"))
                return;

            if (configData.RequireRevolver)
            {
                Item item = player.GetActiveItem();
                if (item == null || item.info.itemid != WEAPON_ID)
                {
                    SendReply(player, string.Format(msg("noWeapon", player.userID), ItemManager.itemList.First(x => x.itemid == WEAPON_ID).displayName.english));
                    return;
                }
                item.MarkDirty();
                item.RemoveFromContainer();

                GiveFlareGun(player);
            }
            else GiveFlareGun(player);
        }

        [ConsoleCommand("flare")]
        private void ccmdFlare(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
                return;

            BasePlayer player = covalence.Players.Connected.FirstOrDefault(x => x.Id == arg.GetString(0))?.Object as BasePlayer;
            if (player != null)
                GiveFlareGun(player);
        }

        [ConsoleCommand("clearflares")]
        private void ccmdClearFlares(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
                return;

            for (int i = outstandingFlares.Count - 1; i >= 0; i--)
            {
                BaseEntity flare = outstandingFlares.ElementAt(i);
                if (flare != null)
                {
                    outstandingFlares.Remove(flare);
                    flare.Kill();
                }
            }
        }
        #endregion

        #region Functions
        private void GiveFlareGun(BasePlayer player)
        {
            Item item = ItemManager.CreateByItemID(WEAPON_ID, 1, WEAPON_SKIN_ID);

            BaseProjectile baseProjectile = item.GetHeldEntity()?.GetComponent<BaseProjectile>();
            if (baseProjectile != null && baseProjectile.primaryMagazine.contents > 0)
                baseProjectile.primaryMagazine.contents = 0;

            player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
        }

        private double GrabCurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;

        private string msg(string key, ulong playerId = 0U) => lang.GetMessage(key, this, playerId == 0U ? null : playerId.ToString());
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            [JsonProperty(PropertyName = "Interval between flares fired")]
            public float Interval { get; set; }
            [JsonProperty(PropertyName = "Flare velocity")]
            public float Velocity { get; set; }
            [JsonProperty(PropertyName = "Require a the standard weapon in your hands when using the chat command")]
            public bool RequireRevolver { get; set; }
            [JsonProperty(PropertyName = "Return any ammo remaining in the weapon when the flaregun is created")]
            public bool ReturnAmmo { get; set; }
            public Oxide.Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            configData = Config.ReadObject<ConfigData>();

            if (configData.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(configData, true);
        }

        protected override void LoadDefaultConfig() => configData = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                Interval = 1f,
                Velocity = 20,
                RequireRevolver = true,
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (configData.Version < new Core.VersionNumber(0, 1, 15))
            {
                configData.ReturnAmmo = baseConfig.ReturnAmmo;
                configData.RequireRevolver = baseConfig.RequireRevolver;
            }

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion
    }
}
